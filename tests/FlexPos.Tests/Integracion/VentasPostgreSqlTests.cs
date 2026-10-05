using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FlexPos.Application.DTOs.Autenticacion;
using FlexPos.Application.DTOs.Caja;
using FlexPos.Application.DTOs.Inventario;
using FlexPos.Application.DTOs.Ventas;
using FlexPos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FlexPos.Tests.Integracion;

public sealed class VentasPostgreSqlTests
{
    [HechoPostgreSql]
    public async Task CajaVentaPagosPendientesFacturaDevolucionYAnulacion_ConservanAuditoriaEInventario()
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

        var correo = $"ventas-{Guid.NewGuid():N}@flexpos.invalid";
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
            nombreComercial = "Negocio ventas de prueba",
            razonSocial = (string?)null,
            identificacionFiscal = (string?)null,
            direccion = (string?)null,
            telefono = (string?)null,
            correo = (string?)null,
            codigoMoneda = "COP",
            permitirVentaSinStock = false,
            comisionProductosHabilitada = false,
            permitirSaldosPendientes = true,
            facturacionHabilitada = true,
            exigirEmpleadoVentaServicio = false
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

        using var crearTransferencia = CrearSolicitud(HttpMethod.Post, "/api/configuracion/metodos-pago", sesion.TokenAcceso,
            new { nombre = $"Transferencia {sufijo}", requiereReferencia = true, esEfectivo = false });
        using var respuestaTransferencia = await cliente.SendAsync(crearTransferencia);
        Assert.Equal(HttpStatusCode.Created, respuestaTransferencia.StatusCode);
        var transferencia = await respuestaTransferencia.Content.ReadFromJsonAsync<FlexPos.Application.DTOs.Configuracion.MetodoPagoDto>();
        Assert.NotNull(transferencia);

        foreach (var tipo in new[] { "Comprobante", "Factura" })
        {
            using var crearNumeracion = CrearSolicitud(
                HttpMethod.Post, "/api/configuracion/numeraciones-documento", sesion.TokenAcceso,
                new { tipoDocumento = tipo, prefijo = $"T{tipo[..1]}", siguienteNumero = 1L });
            using var respuestaNumeracion = await cliente.SendAsync(crearNumeracion);
            Assert.Equal(HttpStatusCode.Created, respuestaNumeracion.StatusCode);
        }

        using var apertura = CrearSolicitud(HttpMethod.Post, "/api/caja/aperturas", sesion.TokenAcceso,
            new { efectivoInicial = 1000m });
        using var respuestaApertura = await cliente.SendAsync(apertura);
        Assert.Equal(HttpStatusCode.Created, respuestaApertura.StatusCode);
        var caja = await respuestaApertura.Content.ReadFromJsonAsync<CajaDto>();
        Assert.NotNull(caja);

        using var crearProducto = CrearSolicitud(HttpMethod.Post, "/api/inventario/articulos", sesion.TokenAcceso,
            new
            {
                codigo = $"SKU-{sufijo}",
                nombre = $"Producto de prueba {sufijo}",
                tipo = "Producto",
                unidadBase = "unidad",
                manejaFraccion = false,
                categoria = "Prueba",
                cantidadMinima = 1m,
                precioVenta = 10000m
            });
        using var respuestaProducto = await cliente.SendAsync(crearProducto);
        Assert.Equal(HttpStatusCode.Created, respuestaProducto.StatusCode);
        var producto = await respuestaProducto.Content.ReadFromJsonAsync<ArticuloInventarioDto>();
        Assert.NotNull(producto);

        using var entrada = CrearSolicitud(HttpMethod.Post,
            $"/api/inventario/articulos/{producto.Id}/entradas-ajuste", sesion.TokenAcceso,
            new { cantidad = 10m, costoUnitario = 5000m, motivo = "Stock inicial de integración" });
        using var respuestaEntrada = await cliente.SendAsync(entrada);
        Assert.Equal(HttpStatusCode.OK, respuestaEntrada.StatusCode);

        using var crearVenta = CrearSolicitud(HttpMethod.Post, "/api/ventas", sesion.TokenAcceso, new
        {
            clienteId = (Guid?)null,
            solicitarFactura = true,
            descuentoGeneral = new { tipo = "Porcentaje", valor = 10m },
            lineas = new[]
            {
                new
                {
                    tipo = "Producto",
                    articuloInventarioId = producto.Id,
                    servicioId = (Guid?)null,
                    empleadoId = (Guid?)null,
                    cantidad = 2m,
                    descuento = new { tipo = "ImporteFijo", valor = 1000m },
                    impuestoId = (Guid?)null
                }
            },
            pagos = new[]
            {
                new { metodoPagoId = efectivo.Id, importe = 10000m, referencia = (string?)null },
                new { metodoPagoId = transferencia.Id, importe = 5000m, referencia = (string?)"TRX-1" }
            }
        });
        using var respuestaVenta = await cliente.SendAsync(crearVenta);
        Assert.Equal(HttpStatusCode.Created, respuestaVenta.StatusCode);
        var venta = await respuestaVenta.Content.ReadFromJsonAsync<VentaDto>();
        Assert.NotNull(venta);
        Assert.Equal(17100m, venta.Total);
        Assert.Equal(2100m, venta.TotalPendiente);
        Assert.Equal(2, venta.Documentos.Count);
        Assert.Contains(venta.Documentos, x => x.TipoDocumento == "Comprobante");
        Assert.Contains(venta.Documentos, x => x.TipoDocumento == "Factura");
        Assert.Single(venta.Pagos, x => x.EsEfectivo);

        using var pdf = await cliente.GetAsync($"/api/ventas/{venta.Id}/comprobante.pdf");
        Assert.Equal(HttpStatusCode.OK, pdf.StatusCode);
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);

        using var agregarPago = CrearSolicitud(HttpMethod.Post, $"/api/ventas/{venta.Id}/pagos", sesion.TokenAcceso,
            new
            {
                pagos = new[]
                {
                    new { metodoPagoId = transferencia.Id, importe = 2100m, referencia = (string?)"TRX-2" }
                }
            });
        using var respuestaPago = await cliente.SendAsync(agregarPago);
        Assert.Equal(HttpStatusCode.OK, respuestaPago.StatusCode);
        var ventaPagada = await respuestaPago.Content.ReadFromJsonAsync<VentaDto>();
        Assert.NotNull(ventaPagada);
        Assert.Equal(0m, ventaPagada.TotalPendiente);

        var idLinea = venta.Lineas.Single().Id;
        using var devolucionParcial = CrearSolicitud(
            HttpMethod.Post, $"/api/ventas/{venta.Id}/devoluciones", sesion.TokenAcceso,
            new
            {
                motivo = "Devolución parcial de prueba",
                lineas = new[] { new { detalleVentaId = idLinea, cantidad = 1m } },
                pagos = new[]
                {
                    new { metodoPagoId = efectivo.Id, importe = 5000m, referencia = (string?)null },
                    new { metodoPagoId = transferencia.Id, importe = 3550m, referencia = (string?)"DEV-1" }
                }
            });
        using var respuestaDevolucion = await cliente.SendAsync(devolucionParcial);
        Assert.Equal(HttpStatusCode.OK, respuestaDevolucion.StatusCode);
        var ventaDevuelta = await respuestaDevolucion.Content.ReadFromJsonAsync<VentaDto>();
        Assert.NotNull(ventaDevuelta);
        Assert.Equal("ParcialmenteDevuelta", ventaDevuelta.Estado);
        Assert.Equal(8550m, ventaDevuelta.TotalDevuelto);

        using var anular = CrearSolicitud(HttpMethod.Post, $"/api/ventas/{venta.Id}/anular", sesion.TokenAcceso,
            new
            {
                motivo = "Anulación de la unidad restante",
                pagos = new[]
                {
                    new { metodoPagoId = efectivo.Id, importe = 5000m, referencia = (string?)null },
                    new { metodoPagoId = transferencia.Id, importe = 3550m, referencia = (string?)"DEV-2" }
                }
            });
        using var respuestaAnulacion = await cliente.SendAsync(anular);
        Assert.Equal(HttpStatusCode.OK, respuestaAnulacion.StatusCode);
        var ventaAnulada = await respuestaAnulacion.Content.ReadFromJsonAsync<VentaDto>();
        Assert.NotNull(ventaAnulada);
        Assert.Equal("Anulada", ventaAnulada.Estado);
        Assert.Equal(17100m, ventaAnulada.TotalDevuelto);
        Assert.Equal(2, ventaAnulada.Devoluciones.Count);

        var inventarioFinal = await cliente.GetFromJsonAsync<PaginaArticulosInventarioDto>(
            $"/api/inventario/articulos?buscar=SKU-{sufijo}");
        Assert.NotNull(inventarioFinal);
        Assert.Equal(10m, Assert.Single(inventarioFinal.Elementos).ExistenciaActual);

        using var cerrarCaja = CrearSolicitud(HttpMethod.Post, $"/api/caja/{caja.Id}/cierre", sesion.TokenAcceso,
            new { efectivoContado = 1000m });
        using var respuestaCierre = await cliente.SendAsync(cerrarCaja);
        Assert.Equal(HttpStatusCode.OK, respuestaCierre.StatusCode);
    }

    private static HttpRequestMessage CrearSolicitud(HttpMethod metodo, string ruta, string token, object cuerpo)
    {
        var solicitud = new HttpRequestMessage(metodo, ruta) { Content = JsonContent.Create(cuerpo) };
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return solicitud;
    }
}
