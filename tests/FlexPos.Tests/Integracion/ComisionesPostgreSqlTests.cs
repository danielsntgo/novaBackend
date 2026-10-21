using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FlexPos.Application.DTOs.Autenticacion;
using FlexPos.Application.DTOs.Comisiones;
using FlexPos.Application.DTOs.Empleados;
using FlexPos.Application.DTOs.Finanzas;
using FlexPos.Application.DTOs.Servicios;
using FlexPos.Application.DTOs.Ventas;
using FlexPos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FlexPos.Tests.Integracion;

public sealed class ComisionesPostgreSqlTests
{
    [HechoPostgreSql]
    public async Task VentaConSaldo_DevolucionParcialYLiquidacionEnEfectivo_ConservanElLedger()
    {
        var conexionRuntime = Environment.GetEnvironmentVariable("ConnectionStrings__FlexPosTest")!;
        var conexionMigraciones = Environment.GetEnvironmentVariable("ConnectionStrings__FlexPosTestMigraciones")!;
        var opciones = new DbContextOptionsBuilder<FlexPosDbContext>()
            .UseNpgsql(conexionMigraciones, postgres =>
                postgres.MigrationsHistoryTable("historial_migraciones", "flexpos"))
            .Options;
        await using (var contexto = new FlexPosDbContext(opciones))
        {
            await contexto.Database.MigrateAsync();
        }

        var correo = $"comisiones-{Guid.NewGuid():N}@flexpos.invalid";
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
        var respuestaLogin = await cliente.PostAsJsonAsync("/api/autenticacion/login", new
        {
            correo,
            contrasena = "PruebaSegura!2026"
        });
        Assert.Equal(HttpStatusCode.OK, respuestaLogin.StatusCode);
        var sesion = await respuestaLogin.Content.ReadFromJsonAsync<RespuestaAcceso>();
        Assert.NotNull(sesion);

        using var guardarConfiguracion = CrearSolicitud(HttpMethod.Put, "/api/configuracion", sesion.TokenAcceso, new
        {
            nombreComercial = "Negocio comisiones de prueba",
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
            exigirEmpleadoVentaServicio = true
        });
        using var respuestaConfiguracion = await cliente.SendAsync(guardarConfiguracion);
        Assert.Equal(HttpStatusCode.OK, respuestaConfiguracion.StatusCode);

        var sufijo = Guid.NewGuid().ToString("N")[..10];
        Guid cuentaEfectivoId;
        await using (var scope = fabrica.Services.CreateAsyncScope())
        {
            var contexto = scope.ServiceProvider.GetRequiredService<FlexPosDbContext>();
            cuentaEfectivoId = await contexto.CuentasFinancieras
                .Where(x => x.Nombre == "Caja principal").Select(x => x.Id).SingleAsync();
        }

        using var crearEfectivoSinCuenta = CrearSolicitud(HttpMethod.Post,
            "/api/configuracion/metodos-pago", sesion.TokenAcceso,
            new { nombre = $"Efectivo sin cuenta {sufijo}", requiereReferencia = false, esEfectivo = true });
        using var respuestaEfectivoSinCuenta = await cliente.SendAsync(crearEfectivoSinCuenta);
        Assert.Equal(HttpStatusCode.BadRequest, respuestaEfectivoSinCuenta.StatusCode);

        using var crearEfectivo = CrearSolicitud(HttpMethod.Post, "/api/configuracion/metodos-pago", sesion.TokenAcceso,
            new
            {
                nombre = $"Efectivo {sufijo}", requiereReferencia = false,
                cuentaFinancieraId = cuentaEfectivoId, esEfectivo = true
            });
        using var respuestaEfectivo = await cliente.SendAsync(crearEfectivo);
        Assert.Equal(HttpStatusCode.Created, respuestaEfectivo.StatusCode);
        var efectivo = await respuestaEfectivo.Content.ReadFromJsonAsync<FlexPos.Application.DTOs.Configuracion.MetodoPagoDto>();
        Assert.NotNull(efectivo);

        using var actualizarEfectivoSinCuenta = CrearSolicitud(HttpMethod.Put,
            $"/api/configuracion/metodos-pago/{efectivo.Id}", sesion.TokenAcceso,
            new { nombre = $"Efectivo {sufijo}", requiereReferencia = false, esEfectivo = true });
        using var respuestaActualizarEfectivoSinCuenta = await cliente.SendAsync(actualizarEfectivoSinCuenta);
        Assert.Equal(HttpStatusCode.BadRequest, respuestaActualizarEfectivoSinCuenta.StatusCode);

        using var crearBanco = CrearSolicitud(HttpMethod.Post, "/api/finanzas/cuentas", sesion.TokenAcceso,
            new
            {
                nombre = $"Banco comisiones {sufijo}", tipo = "Banco", codigoMoneda = "COP",
                entidad = "Banco de prueba", ultimosDigitos = (string?)null, cupoCredito = (decimal?)null
            });
        using var respuestaBanco = await cliente.SendAsync(crearBanco);
        Assert.Equal(HttpStatusCode.Created, respuestaBanco.StatusCode);
        var banco = await respuestaBanco.Content.ReadFromJsonAsync<CuentaFinancieraDto>();
        Assert.NotNull(banco);

        using var crearTarjeta = CrearSolicitud(HttpMethod.Post, "/api/configuracion/metodos-pago", sesion.TokenAcceso,
            new
            {
                nombre = $"Tarjeta {sufijo}", requiereReferencia = true, esEfectivo = false,
                cuentaFinancieraId = banco.Id
            });
        using var respuestaTarjeta = await cliente.SendAsync(crearTarjeta);
        Assert.Equal(HttpStatusCode.Created, respuestaTarjeta.StatusCode);
        var tarjeta = await respuestaTarjeta.Content.ReadFromJsonAsync<FlexPos.Application.DTOs.Configuracion.MetodoPagoDto>();
        Assert.NotNull(tarjeta);

        using var crearNumeracion = CrearSolicitud(
            HttpMethod.Post, "/api/configuracion/numeraciones-documento", sesion.TokenAcceso,
            new { tipoDocumento = "Comprobante", prefijo = "RC", siguienteNumero = 1L });
        using var respuestaNumeracion = await cliente.SendAsync(crearNumeracion);
        Assert.Equal(HttpStatusCode.Created, respuestaNumeracion.StatusCode);

        using var apertura = CrearSolicitud(HttpMethod.Post, "/api/caja/aperturas", sesion.TokenAcceso,
            new { efectivoInicial = 1000m });
        using var respuestaApertura = await cliente.SendAsync(apertura);
        Assert.Equal(HttpStatusCode.Created, respuestaApertura.StatusCode);

        using var crearEmpleado = CrearSolicitud(HttpMethod.Post, "/api/empleados", sesion.TokenAcceso,
            new
            {
                nombre = $"Empleado {sufijo}", cargo = "Estilista", documento = (string?)null,
                telefono = (string?)null, correo = (string?)null
            });
        using var respuestaEmpleado = await cliente.SendAsync(crearEmpleado);
        Assert.Equal(HttpStatusCode.Created, respuestaEmpleado.StatusCode);
        var empleado = await respuestaEmpleado.Content.ReadFromJsonAsync<EmpleadoDto>();
        Assert.NotNull(empleado);

        using var crearServicio = CrearSolicitud(HttpMethod.Post, "/api/servicios", sesion.TokenAcceso,
            new
            {
                nombre = $"Servicio {sufijo}", descripcion = (string?)null, categoria = "Cuidado",
                precio = 100m, duracionMinutos = 30
            });
        using var respuestaServicio = await cliente.SendAsync(crearServicio);
        Assert.Equal(HttpStatusCode.Created, respuestaServicio.StatusCode);
        var servicio = await respuestaServicio.Content.ReadFromJsonAsync<ServicioDto>();
        Assert.NotNull(servicio);

        using var crearRegla = CrearSolicitud(HttpMethod.Post, "/api/comisiones/reglas", sesion.TokenAcceso,
            new { empleadoId = empleado.Id, servicioId = servicio.Id, tipo = "Porcentaje", valor = 20m });
        using var respuestaRegla = await cliente.SendAsync(crearRegla);
        Assert.Equal(HttpStatusCode.Created, respuestaRegla.StatusCode);

        using var crearVenta = CrearSolicitud(HttpMethod.Post, "/api/ventas", sesion.TokenAcceso, new
        {
            clienteId = (Guid?)null,
            solicitarFactura = false,
            descuentoGeneral = new { tipo = "Porcentaje", valor = 10m },
            lineas = new[]
            {
                new
                {
                    tipo = "Servicio",
                    articuloInventarioId = (Guid?)null,
                    servicioId = servicio.Id,
                    empleadoId = empleado.Id,
                    cantidad = 3m,
                    descuento = (object?)null,
                    impuestoId = (Guid?)null
                }
            },
            pagos = Array.Empty<object>()
        });
        using var respuestaVenta = await cliente.SendAsync(crearVenta);
        Assert.Equal(HttpStatusCode.Created, respuestaVenta.StatusCode);
        var venta = await respuestaVenta.Content.ReadFromJsonAsync<VentaDto>();
        Assert.NotNull(venta);
        Assert.Equal(270m, venta.Total);
        Assert.Equal(270m, venta.TotalPendiente);

        using var consultaDevengos = await cliente.GetAsync($"/api/comisiones?empleadoId={empleado.Id}");
        Assert.Equal(HttpStatusCode.OK, consultaDevengos.StatusCode);
        var paginaInicial = await consultaDevengos.Content.ReadFromJsonAsync<PaginaComisionesDto>();
        var devengo = Assert.Single(paginaInicial!.Elementos);
        Assert.Equal(54m, devengo.Importe);
        Assert.Equal(270m, devengo.BaseCalculo);
        Assert.Equal("COP", devengo.CodigoMoneda);
        Assert.Equal("Pendiente", devengo.Estado);

        using var devolver = CrearSolicitud(HttpMethod.Post, $"/api/ventas/{venta.Id}/devoluciones", sesion.TokenAcceso,
            new
            {
                motivo = "Devolución parcial de servicio",
                lineas = new[] { new { detalleVentaId = venta.Lineas.Single().Id, cantidad = 1m } },
                pagos = Array.Empty<object>()
            });
        using var respuestaDevolucion = await cliente.SendAsync(devolver);
        Assert.Equal(HttpStatusCode.OK, respuestaDevolucion.StatusCode);

        using var consultaAjustes = await cliente.GetAsync($"/api/comisiones?empleadoId={empleado.Id}");
        var movimientos = (await consultaAjustes.Content.ReadFromJsonAsync<PaginaComisionesDto>())!.Elementos;
        Assert.Equal(2, movimientos.Count);
        var ajuste = Assert.Single(movimientos, x => x.TipoMovimiento == "AjusteDevolucion");
        Assert.Equal(18m, ajuste.Importe);
        Assert.Equal(devengo.Id, ajuste.ComisionOriginalId);

        using var pagar = CrearSolicitud(HttpMethod.Post, "/api/comisiones/liquidaciones", sesion.TokenAcceso,
            new { comisionIds = new[] { devengo.Id }, metodoPagoId = efectivo.Id, referencia = (string?)null });
        using var respuestaLiquidacion = await cliente.SendAsync(pagar);
        Assert.Equal(HttpStatusCode.Created, respuestaLiquidacion.StatusCode);
        var liquidacion = await respuestaLiquidacion.Content.ReadFromJsonAsync<LiquidacionComisionDto>();
        Assert.NotNull(liquidacion);
        Assert.Equal(36m, liquidacion.Importe);
        Assert.True(liquidacion.EsEfectivo);
        Assert.Equal(2, liquidacion.ComisionIds.Count);

        using var crearVentaBanco = CrearSolicitud(HttpMethod.Post, "/api/ventas", sesion.TokenAcceso, new
        {
            clienteId = (Guid?)null,
            solicitarFactura = false,
            descuentoGeneral = (object?)null,
            lineas = new[]
            {
                new
                {
                    tipo = "Servicio",
                    articuloInventarioId = (Guid?)null,
                    servicioId = servicio.Id,
                    empleadoId = empleado.Id,
                    cantidad = 3m,
                    descuento = (object?)null,
                    impuestoId = (Guid?)null
                }
            },
            pagos = new[]
            {
                new { metodoPagoId = tarjeta.Id, importe = 300m, referencia = (string?)"TRX-COM-1" }
            }
        });
        using var respuestaVentaBanco = await cliente.SendAsync(crearVentaBanco);
        Assert.Equal(HttpStatusCode.Created, respuestaVentaBanco.StatusCode);
        var ventaBanco = await respuestaVentaBanco.Content.ReadFromJsonAsync<VentaDto>();
        Assert.NotNull(ventaBanco);
        var pagoBanco = Assert.Single(ventaBanco.Pagos);
        Assert.Equal(banco.Id, pagoBanco.CuentaFinancieraId);
        Assert.Equal(banco.Nombre, pagoBanco.CuentaFinancieraNombre);

        using var consultaComisionBanco = await cliente.GetAsync($"/api/comisiones?empleadoId={empleado.Id}");
        Assert.Equal(HttpStatusCode.OK, consultaComisionBanco.StatusCode);
        var comisionBanco = Assert.Single(
            (await consultaComisionBanco.Content.ReadFromJsonAsync<PaginaComisionesDto>())!.Elementos,
            x => x.VentaId == ventaBanco.Id && x.TipoMovimiento == "DevengoServicio");
        Assert.Equal(60m, comisionBanco.Importe);

        using var liquidarBanco = CrearSolicitud(HttpMethod.Post, "/api/comisiones/liquidaciones", sesion.TokenAcceso,
            new
            {
                comisionIds = new[] { comisionBanco.Id }, metodoPagoId = tarjeta.Id,
                referencia = (string?)"TRX-COM-2"
            });
        using var respuestaLiquidacionBanco = await cliente.SendAsync(liquidarBanco);
        Assert.Equal(HttpStatusCode.Created, respuestaLiquidacionBanco.StatusCode);
        var liquidacionBanco = await respuestaLiquidacionBanco.Content.ReadFromJsonAsync<LiquidacionComisionDto>();
        Assert.NotNull(liquidacionBanco);
        Assert.False(liquidacionBanco.EsEfectivo);
        Assert.Null(liquidacionBanco.CajaId);
        Assert.Equal(60m, liquidacionBanco.Importe);

        using var obtenerBanco = new HttpRequestMessage(HttpMethod.Get, $"/api/finanzas/cuentas/{banco.Id}");
        obtenerBanco.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sesion.TokenAcceso);
        using var respuestaBancoActual = await cliente.SendAsync(obtenerBanco);
        Assert.Equal(HttpStatusCode.OK, respuestaBancoActual.StatusCode);
        var bancoActual = await respuestaBancoActual.Content.ReadFromJsonAsync<CuentaFinancieraDto>();
        Assert.NotNull(bancoActual);
        Assert.Equal(240m, bancoActual.Saldo);

        using var obtenerMovimientosBanco = new HttpRequestMessage(HttpMethod.Get,
            $"/api/finanzas/cuentas/{banco.Id}/movimientos?tamanoPagina=100");
        obtenerMovimientosBanco.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sesion.TokenAcceso);
        using var respuestaMovimientosBanco = await cliente.SendAsync(obtenerMovimientosBanco);
        Assert.Equal(HttpStatusCode.OK, respuestaMovimientosBanco.StatusCode);
        var movimientosBanco = await respuestaMovimientosBanco.Content.ReadFromJsonAsync<PaginaMovimientosFinancierosDto>();
        Assert.NotNull(movimientosBanco);
        Assert.Equal(2, movimientosBanco.TotalElementos);
        Assert.Contains(movimientosBanco.Elementos, x => x.Tipo == "PagoVenta" && x.Sentido == "Entrada" &&
            x.VentaId == ventaBanco.Id && x.MetodoPagoId == tarjeta.Id && x.CajaId is null);
        Assert.Contains(movimientosBanco.Elementos, x => x.Tipo == "PagoComision" && x.Sentido == "Salida" &&
            x.LiquidacionComisionId == liquidacionBanco.Id && x.MetodoPagoId == tarjeta.Id && x.CajaId is null);

        using var efectivoActual = await cliente.GetAsync("/api/caja/actual");
        Assert.Equal(HttpStatusCode.OK, efectivoActual.StatusCode);
        var caja = await efectivoActual.Content.ReadFromJsonAsync<FlexPos.Application.DTOs.Caja.CajaDto>();
        Assert.Equal(964m, caja!.EfectivoEsperado);
    }

    private static HttpRequestMessage CrearSolicitud(HttpMethod metodo, string ruta, string token, object cuerpo)
    {
        var solicitud = new HttpRequestMessage(metodo, ruta) { Content = JsonContent.Create(cuerpo) };
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return solicitud;
    }
}
