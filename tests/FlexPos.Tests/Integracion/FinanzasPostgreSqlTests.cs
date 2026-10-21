using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FlexPos.Application.DTOs.Autenticacion;
using FlexPos.Application.DTOs.Caja;
using FlexPos.Application.DTOs.Finanzas;
using FlexPos.Domain.Entities;
using FlexPos.Domain.Enums;
using FlexPos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FlexPos.Tests.Integracion;

public sealed class FinanzasPostgreSqlTests
{
    [HechoPostgreSql]
    public async Task Migracion_DejaCajaPrincipalEnElEfectivoDelUltimoCierreODeLaCajaAbierta()
    {
        var cadena = Environment.GetEnvironmentVariable("ConnectionStrings__FlexPosTestMigraciones")!;
        var opciones = new DbContextOptionsBuilder<FlexPosDbContext>()
            .UseNpgsql(cadena, postgres => postgres.MigrationsHistoryTable("historial_migraciones", "flexpos"))
            .Options;
        await using var contexto = new FlexPosDbContext(opciones);
        await contexto.Database.MigrateAsync();

        var cajaAbierta = await contexto.Cajas.AsNoTracking()
            .Where(x => x.Estado == EstadoCaja.Abierta)
            .OrderByDescending(x => x.FechaAperturaUtc).ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync();
        var cajaCerrada = await contexto.Cajas.AsNoTracking()
            .Where(x => x.Estado == EstadoCaja.Cerrada)
            .OrderByDescending(x => x.FechaCierreUtc).ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync();
        var cajaReferencia = cajaAbierta ?? cajaCerrada;
        var efectivo = await contexto.CuentasFinancieras.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Tipo == TipoCuentaFinanciera.Efectivo && x.Nombre == "Caja principal");

        Assert.NotNull(efectivo);
        var movimientos = await contexto.MovimientosFinancieros.AsNoTracking()
            .Where(x => x.CuentaFinancieraId == efectivo!.Id)
            .Select(x => new { x.Sentido, x.Importe })
            .ToArrayAsync();
        var saldoLibro = movimientos.Sum(x => x.Sentido == SentidoMovimientoFinanciero.Entrada
            ? x.Importe : -x.Importe);
        if (cajaReferencia is null)
        {
            Assert.Equal(0m, saldoLibro);
        }
        else if (cajaAbierta is not null)
        {
            var eventos = await contexto.MovimientosFinancieros.AsNoTracking()
                .Where(x => x.CajaId == cajaAbierta.Id &&
                    x.Tipo != "AjusteApertura" && x.Tipo != "AjusteCierre" && x.Tipo != "AjusteMigracion")
                .Select(x => new { x.Sentido, x.Importe })
                .ToArrayAsync();
            var esperado = cajaAbierta.EfectivoApertura + eventos.Sum(x =>
                x.Sentido == SentidoMovimientoFinanciero.Entrada ? x.Importe : -x.Importe);
            Assert.Equal(esperado, saldoLibro);
        }
        else
        {
            Assert.Equal(cajaCerrada!.EfectivoContadoCierre ?? cajaCerrada.EfectivoEsperadoCierre, saldoLibro);
        }

        var cuentaPorAsignar = await contexto.CuentasFinancieras.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Tipo == TipoCuentaFinanciera.Otro && x.Nombre == "Cuenta por asignar");
        if (cuentaPorAsignar is not null)
        {
            var saldoNoEfectivo = await contexto.MovimientosFinancieros.AsNoTracking()
                .Where(x => x.CuentaFinancieraId == cuentaPorAsignar.Id)
                .GroupBy(x => x.Sentido)
                .Select(x => new { x.Key, Importe = x.Sum(m => m.Importe) })
                .ToArrayAsync();
            Assert.True(saldoNoEfectivo.Sum(x => x.Key == SentidoMovimientoFinanciero.Entrada
                ? x.Importe : -x.Importe) >= 0m);
        }
    }

    [HechoPostgreSql]
    public async Task Transferencias_ConcilianCajaValidanSaldosYSoportanConcurrencia()
    {
        var conexionRuntime = Environment.GetEnvironmentVariable("ConnectionStrings__FlexPosTest")!;
        var conexionMigraciones = Environment.GetEnvironmentVariable("ConnectionStrings__FlexPosTestMigraciones")!;
        var opcionesMigraciones = new DbContextOptionsBuilder<FlexPosDbContext>()
            .UseNpgsql(conexionMigraciones, postgres =>
                postgres.MigrationsHistoryTable("historial_migraciones", "flexpos"))
            .Options;
        await using (var contexto = new FlexPosDbContext(opcionesMigraciones))
        {
            await contexto.Database.MigrateAsync();
        }

        var correo = $"transferencias-{Guid.NewGuid():N}@flexpos.invalid";
        await using var fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuracion) => configuracion.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:FlexPos"] = conexionRuntime,
                    ["AdministradorInicial:Correo"] = correo,
                    ["AdministradorInicial:Contrasena"] = "PruebaSegura!2026",
                    ["Autenticacion:Jwt:ClaveFirma"] = "clave-de-pruebas-de-al-menos-treinta-y-dos-bytes",
                    ["Autenticacion:Jwt:Emisor"] = "FlexPos.Tests",
                    ["Autenticacion:Jwt:Audiencia"] = "FlexPos.Tests",
                    ["Autenticacion:CookieRefresh:Nombre"] = "flexpos_refresh",
                    ["Autenticacion:CookieRefresh:Ruta"] = "/api/autenticacion",
                    ["Autenticacion:CookieRefresh:Segura"] = "true",
                    ["Autenticacion:CookieRefresh:MismoSitio"] = "None"
                }));
        });

        using var cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var acceso = await cliente.PostAsJsonAsync("/api/autenticacion/login", new
        {
            correo,
            contrasena = "PruebaSegura!2026"
        });
        Assert.Equal(HttpStatusCode.OK, acceso.StatusCode);
        var sesion = await acceso.Content.ReadFromJsonAsync<RespuestaAcceso>();
        Assert.NotNull(sesion);

        using var guardarConfiguracion = CrearSolicitud(HttpMethod.Put, "/api/configuracion", sesion.TokenAcceso, new
        {
            nombreComercial = "Negocio de transferencias",
            razonSocial = (string?)null,
            identificacionFiscal = (string?)null,
            direccion = (string?)null,
            telefono = (string?)null,
            correo = (string?)null,
            codigoMoneda = "COP",
            permitirVentaSinStock = false,
            comisionProductosHabilitada = false,
            permitirSaldosPendientes = true,
            facturacionHabilitada = false,
            exigirEmpleadoVentaServicio = false
        });
        using var respuestaConfiguracion = await cliente.SendAsync(guardarConfiguracion);
        Assert.Equal(HttpStatusCode.OK, respuestaConfiguracion.StatusCode);

        using var listarEfectivo = CrearSolicitud(HttpMethod.Get,
            "/api/finanzas/cuentas?tipo=Efectivo&buscar=Caja%20principal&tamanoPagina=100", sesion.TokenAcceso);
        using var respuestaEfectivo = await cliente.SendAsync(listarEfectivo);
        Assert.Equal(HttpStatusCode.OK, respuestaEfectivo.StatusCode);
        var cuentasEfectivo = await respuestaEfectivo.Content.ReadFromJsonAsync<PaginaCuentasFinancierasDto>();
        Assert.NotNull(cuentasEfectivo);
        var efectivo = Assert.Single(cuentasEfectivo!.Elementos);

        var sufijo = Guid.NewGuid().ToString("N")[..8];
        var banco = await CrearCuentaAsync(cliente, sesion.TokenAcceso, $"Banco {sufijo}", "Banco");
        var bancoDestino = await CrearCuentaAsync(cliente, sesion.TokenAcceso, $"Banco destino {sufijo}", "Banco");
        var billetera = await CrearCuentaAsync(cliente, sesion.TokenAcceso, $"Billetera {sufijo}", "Billetera");
        var tarjeta = await CrearCuentaAsync(cliente, sesion.TokenAcceso, $"Tarjeta {sufijo}", "TarjetaCredito", 500m);
        using var apertura = CrearSolicitud(HttpMethod.Post, "/api/caja/aperturas", sesion.TokenAcceso,
            new { efectivoInicial = 1000m });
        using var respuestaApertura = await cliente.SendAsync(apertura);
        Assert.Equal(HttpStatusCode.Created, respuestaApertura.StatusCode);
        var caja = await respuestaApertura.Content.ReadFromJsonAsync<CajaDto>();
        Assert.NotNull(caja);

        using (var cajaABanco = await TransferirAsync(cliente, sesion.TokenAcceso, efectivo.Id, banco.Id, 700m))
        {
            Assert.Equal(HttpStatusCode.Created, cajaABanco.StatusCode);
            var transferencia = await cajaABanco.Content.ReadFromJsonAsync<TransferenciaFinancieraDto>();
            Assert.NotNull(transferencia);
            Assert.Equal(efectivo.Id, transferencia.CuentaOrigenId);
            Assert.Equal(banco.Id, transferencia.CuentaDestinoId);
        }

        using (var efectivoInsuficiente = await TransferirAsync(
                   cliente, sesion.TokenAcceso, efectivo.Id, banco.Id, 301m))
        {
            Assert.Equal(HttpStatusCode.Conflict, efectivoInsuficiente.StatusCode);
        }
        using (var bancoACaja = await TransferirAsync(cliente, sesion.TokenAcceso, banco.Id, efectivo.Id, 100m))
        {
            Assert.Equal(HttpStatusCode.Created, bancoACaja.StatusCode);
        }
        using (var bancoABanco = await TransferirAsync(cliente, sesion.TokenAcceso, banco.Id, bancoDestino.Id, 50m))
        {
            Assert.Equal(HttpStatusCode.Created, bancoABanco.StatusCode);
        }
        using (var bancoABilletera = await TransferirAsync(cliente, sesion.TokenAcceso, banco.Id, billetera.Id, 200m))
        {
            Assert.Equal(HttpStatusCode.Created, bancoABilletera.StatusCode);
        }

        var opcionesRuntime = new DbContextOptionsBuilder<FlexPosDbContext>().UseNpgsql(conexionRuntime).Options;
        await using (var contexto = new FlexPosDbContext(opcionesRuntime))
        {
            var deudaExistente = MovimientoFinanciero.Crear(
                tarjeta.Id, "AjusteMigracion", SentidoMovimientoFinanciero.Salida, 100m,
                "COP", DateTimeOffset.UtcNow, "Deuda previa", migrado: true);
            Assert.True(deudaExistente.EsExitoso);
            await contexto.MovimientosFinancieros.AddAsync(deudaExistente.Valor!);
            await contexto.SaveChangesAsync();
        }

        using (var pagoTarjeta = await TransferirAsync(cliente, sesion.TokenAcceso, banco.Id, tarjeta.Id, 100m))
        {
            Assert.Equal(HttpStatusCode.Created, pagoTarjeta.StatusCode);
        }
        using (var sobrepagoTarjeta = await TransferirAsync(cliente, sesion.TokenAcceso, banco.Id, tarjeta.Id, 1m))
        {
            Assert.Equal(HttpStatusCode.Conflict, sobrepagoTarjeta.StatusCode);
        }
        using (var tarjetaComoOrigen = await TransferirAsync(cliente, sesion.TokenAcceso, tarjeta.Id, banco.Id, 1m))
        {
            Assert.Equal(HttpStatusCode.Conflict, tarjetaComoOrigen.StatusCode);
        }

        var concurrentes = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ =>
            TransferirAsync(cliente, sesion.TokenAcceso, banco.Id, bancoDestino.Id, 200m)));
        try
        {
            Assert.Single(concurrentes, x => x.StatusCode == HttpStatusCode.Created);
            Assert.Single(concurrentes, x => x.StatusCode == HttpStatusCode.Conflict);
        }
        finally
        {
            foreach (var respuesta in concurrentes)
            {
                respuesta.Dispose();
            }
        }

        using var obtenerEfectivo = CrearSolicitud(HttpMethod.Get, $"/api/finanzas/cuentas/{efectivo.Id}", sesion.TokenAcceso);
        using var respuestaSaldoEfectivo = await cliente.SendAsync(obtenerEfectivo);
        var saldoEfectivo = await respuestaSaldoEfectivo.Content.ReadFromJsonAsync<CuentaFinancieraDto>();
        Assert.NotNull(saldoEfectivo);
        Assert.Equal(400m, saldoEfectivo!.Saldo);
        using var obtenerBanco = CrearSolicitud(HttpMethod.Get, $"/api/finanzas/cuentas/{banco.Id}", sesion.TokenAcceso);
        using var respuestaSaldoBanco = await cliente.SendAsync(obtenerBanco);
        var saldoBanco = await respuestaSaldoBanco.Content.ReadFromJsonAsync<CuentaFinancieraDto>();
        Assert.NotNull(saldoBanco);
        Assert.Equal(50m, saldoBanco!.Saldo);
        using var obtenerTarjeta = CrearSolicitud(HttpMethod.Get, $"/api/finanzas/cuentas/{tarjeta.Id}", sesion.TokenAcceso);
        using var respuestaSaldoTarjeta = await cliente.SendAsync(obtenerTarjeta);
        var saldoTarjeta = await respuestaSaldoTarjeta.Content.ReadFromJsonAsync<CuentaFinancieraDto>();
        Assert.NotNull(saldoTarjeta);
        Assert.Equal(0m, saldoTarjeta!.Saldo);

        using var obtenerCajaActual = CrearSolicitud(HttpMethod.Get, "/api/caja/actual", sesion.TokenAcceso);
        using var respuestaCajaActual = await cliente.SendAsync(obtenerCajaActual);
        Assert.Equal(HttpStatusCode.OK, respuestaCajaActual.StatusCode);
        var cajaActual = await respuestaCajaActual.Content.ReadFromJsonAsync<CajaDto>();
        Assert.NotNull(cajaActual);
        Assert.Equal(400m, cajaActual!.EfectivoEsperado!.Value);

        using var listarTransferencias = CrearSolicitud(HttpMethod.Get,
            $"/api/finanzas/transferencias?cuentaId={banco.Id}&tamanoPagina=100", sesion.TokenAcceso);
        using var respuestaTransferencias = await cliente.SendAsync(listarTransferencias);
        Assert.Equal(HttpStatusCode.OK, respuestaTransferencias.StatusCode);
        var transferencias = await respuestaTransferencias.Content.ReadFromJsonAsync<PaginaTransferenciasFinancierasDto>();
        Assert.NotNull(transferencias);
        Assert.Equal(6, transferencias!.TotalElementos);
        var idsTransferencias = transferencias.Elementos.Select(x => x.Id).ToArray();
        await using (var contexto = new FlexPosDbContext(opcionesRuntime))
        {
            var movimientosPorTransferencia = await contexto.MovimientosFinancieros.AsNoTracking()
                .Where(x => x.TransferenciaFinancieraId.HasValue && idsTransferencias.Contains(x.TransferenciaFinancieraId.Value))
                .GroupBy(x => x.TransferenciaFinancieraId)
                .Select(x => new { Id = x.Key, Total = x.Count() })
                .ToArrayAsync();
            Assert.Equal(6, movimientosPorTransferencia.Length);
            Assert.All(movimientosPorTransferencia, x => Assert.Equal(2, x.Total));
        }

        using var obtenerCaja = CrearSolicitud(HttpMethod.Get, $"/api/caja/{caja.Id}", sesion.TokenAcceso);
        using var respuestaCaja = await cliente.SendAsync(obtenerCaja);
        var detalleCaja = await respuestaCaja.Content.ReadFromJsonAsync<CajaDetalleDto>();
        Assert.NotNull(detalleCaja);
        Assert.Equal(2, detalleCaja.Movimientos.Count(x => x.Tipo is "TransferenciaEntrada" or "TransferenciaSalida"));
        using var cierre = CrearSolicitud(HttpMethod.Post, $"/api/caja/{caja.Id}/cierre", sesion.TokenAcceso,
            new { efectivoContado = 400m });
        using var respuestaCierre = await cliente.SendAsync(cierre);
        Assert.Equal(HttpStatusCode.OK, respuestaCierre.StatusCode);
    }

    private static async Task<CuentaFinancieraDto> CrearCuentaAsync(
        HttpClient cliente,
        string token,
        string nombre,
        string tipo,
        decimal? cupoCredito = null)
    {
        using var solicitud = CrearSolicitud(HttpMethod.Post, "/api/finanzas/cuentas", token, new
        {
            nombre,
            tipo,
            codigoMoneda = "COP",
            entidad = (string?)null,
            ultimosDigitos = (string?)null,
            cupoCredito
        });
        using var respuesta = await cliente.SendAsync(solicitud);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        return Assert.IsType<CuentaFinancieraDto>(await respuesta.Content.ReadFromJsonAsync<CuentaFinancieraDto>());
    }

    private static async Task<HttpResponseMessage> TransferirAsync(
        HttpClient cliente,
        string token,
        Guid cuentaOrigenId,
        Guid cuentaDestinoId,
        decimal importe)
    {
        using var solicitud = CrearSolicitud(HttpMethod.Post, "/api/finanzas/transferencias", token, new
        {
            cuentaOrigenId,
            cuentaDestinoId,
            importe,
            concepto = "Prueba transferencia",
            referencia = (string?)null
        });
        return await cliente.SendAsync(solicitud);
    }

    private static HttpRequestMessage CrearSolicitud(HttpMethod metodo, string ruta, string token)
    {
        var solicitud = new HttpRequestMessage(metodo, ruta);
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return solicitud;
    }

    private static HttpRequestMessage CrearSolicitud(HttpMethod metodo, string ruta, string token, object cuerpo)
    {
        var solicitud = CrearSolicitud(metodo, ruta, token);
        solicitud.Content = JsonContent.Create(cuerpo);
        return solicitud;
    }
}
