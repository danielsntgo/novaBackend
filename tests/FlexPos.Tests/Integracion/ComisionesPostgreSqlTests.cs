using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FlexPos.Application.DTOs.Autenticacion;
using FlexPos.Application.DTOs.Comisiones;
using FlexPos.Application.DTOs.Empleados;
using FlexPos.Application.DTOs.Servicios;
using FlexPos.Application.DTOs.Ventas;
using FlexPos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        using var crearEfectivo = CrearSolicitud(HttpMethod.Post, "/api/configuracion/metodos-pago", sesion.TokenAcceso,
            new { nombre = $"Efectivo {sufijo}", requiereReferencia = false, esEfectivo = true });
        using var respuestaEfectivo = await cliente.SendAsync(crearEfectivo);
        Assert.Equal(HttpStatusCode.Created, respuestaEfectivo.StatusCode);
        var efectivo = await respuestaEfectivo.Content.ReadFromJsonAsync<FlexPos.Application.DTOs.Configuracion.MetodoPagoDto>();
        Assert.NotNull(efectivo);

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
