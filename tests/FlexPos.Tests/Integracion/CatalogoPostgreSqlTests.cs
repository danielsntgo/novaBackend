using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FlexPos.Application.DTOs.Autenticacion;
using FlexPos.Application.DTOs.Clientes;
using FlexPos.Application.DTOs.Empleados;
using FlexPos.Application.DTOs.Servicios;
using FlexPos.Application.DTOs.Compras;
using FlexPos.Application.DTOs.Inventario;
using FlexPos.Application.DTOs.Proveedores;
using FlexPos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FlexPos.Tests.Integracion;

public sealed class CatalogoPostgreSqlTests
{
    [HechoPostgreSql]
    public async Task GestionaCatalogosConBusquedaPaginadaYMonedaConfigurada()
    {
        var conexionRuntime = Environment.GetEnvironmentVariable("ConnectionStrings__FlexPosTest")!;
        var conexionMigraciones = Environment.GetEnvironmentVariable("ConnectionStrings__FlexPosTestMigraciones")!;
        var opciones = new DbContextOptionsBuilder<FlexPosDbContext>()
            .UseNpgsql(conexionMigraciones, postgres => postgres.MigrationsHistoryTable("historial_migraciones", "flexpos"))
            .Options;

        await using (var contexto = new FlexPosDbContext(opciones))
        {
            await contexto.Database.MigrateAsync();
        }

        var correoAdministrador = $"catalogo-{Guid.NewGuid():N}@flexpos.invalid";
        await using var fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuracion) => configuracion.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:FlexPos"] = conexionRuntime,
                    ["AdministradorInicial:Correo"] = correoAdministrador,
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

        using var clienteHttp = fabrica.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var login = await clienteHttp.PostAsJsonAsync("/api/autenticacion/login", new
        {
            correo = correoAdministrador,
            contrasena = "PruebaSegura!2026"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var acceso = await login.Content.ReadFromJsonAsync<RespuestaAcceso>();
        Assert.NotNull(acceso);

        var configuracion = CrearSolicitud(HttpMethod.Put, "/api/configuracion", acceso.TokenAcceso, new
        {
            nombreComercial = "Salón de prueba",
            razonSocial = (string?)null,
            identificacionFiscal = (string?)null,
            direccion = (string?)null,
            telefono = (string?)null,
            correo = (string?)null,
            codigoMoneda = "COP",
            permitirVentaSinStock = false,
            comisionProductosHabilitada = false
        });
        using var respuestaConfiguracion = await clienteHttp.SendAsync(configuracion);
        Assert.Equal(HttpStatusCode.OK, respuestaConfiguracion.StatusCode);

        var sufijo = Guid.NewGuid().ToString("N")[..10];
        using var altaCliente = CrearSolicitud(HttpMethod.Post, "/api/clientes", acceso.TokenAcceso, new
        {
            nombre = $"María{sufijo} Gómez",
            documento = $"DOC-{sufijo}",
            telefono = $"300{sufijo}",
            correo = $"cliente-{sufijo}@flexpos.invalid"
        });
        using var respuestaCliente = await clienteHttp.SendAsync(altaCliente);
        Assert.Equal(HttpStatusCode.Created, respuestaCliente.StatusCode);
        var clienteCreado = await respuestaCliente.Content.ReadFromJsonAsync<ClienteDto>();
        Assert.NotNull(clienteCreado);

        var paginaClientes = await clienteHttp.GetAsync($"/api/clientes?buscar=maria{sufijo}&pagina=1&tamanoPagina=5");
        Assert.Equal(HttpStatusCode.OK, paginaClientes.StatusCode);
        var clientes = await paginaClientes.Content.ReadFromJsonAsync<PaginaClientesDto>();
        Assert.NotNull(clientes);
        Assert.Equal(1, clientes.TotalElementos);
        Assert.Equal(clienteCreado.Id, Assert.Single(clientes.Elementos).Id);

        using var altaEmpleado = CrearSolicitud(HttpMethod.Post, "/api/empleados", acceso.TokenAcceso, new
        {
            nombre = $"José{sufijo} Díaz",
            cargo = "Estilista",
            documento = (string?)null,
            telefono = (string?)null,
            correo = (string?)null
        });
        using var respuestaEmpleado = await clienteHttp.SendAsync(altaEmpleado);
        Assert.Equal(HttpStatusCode.Created, respuestaEmpleado.StatusCode);
        var empleado = await respuestaEmpleado.Content.ReadFromJsonAsync<EmpleadoDto>();
        Assert.NotNull(empleado);

        using var altaServicio = CrearSolicitud(HttpMethod.Post, "/api/servicios", acceso.TokenAcceso, new
        {
            nombre = $"Corte{sufijo}",
            descripcion = (string?)null,
            categoria = "Cabello",
            precio = 25000m,
            duracionMinutos = 45
        });
        using var respuestaServicio = await clienteHttp.SendAsync(altaServicio);
        Assert.Equal(HttpStatusCode.Created, respuestaServicio.StatusCode);
        var servicio = await respuestaServicio.Content.ReadFromJsonAsync<ServicioDto>();
        Assert.NotNull(servicio);
        Assert.Equal("COP", servicio.CodigoMoneda);

        using var altaProveedor = CrearSolicitud(HttpMethod.Post, "/api/proveedores", acceso.TokenAcceso, new
        {
            nombre = $"Distribuidora{sufijo}",
            identificacionFiscal = (string?)null,
            telefono = (string?)null,
            correo = (string?)null
        });
        using var respuestaProveedor = await clienteHttp.SendAsync(altaProveedor);
        Assert.Equal(HttpStatusCode.Created, respuestaProveedor.StatusCode);
        var proveedor = await respuestaProveedor.Content.ReadFromJsonAsync<ProveedorDto>();
        Assert.NotNull(proveedor);

        using var altaProducto = CrearSolicitud(HttpMethod.Post, "/api/inventario/articulos", acceso.TokenAcceso, new
        {
            codigo = $"SKU-{sufijo}",
            nombre = $"Shampoo{sufijo}",
            tipo = "Producto",
            unidadBase = "unidad",
            manejaFraccion = false,
            categoria = "Cuidado",
            cantidadMinima = 2m,
            precioVenta = 3000m
        });
        using var respuestaProducto = await clienteHttp.SendAsync(altaProducto);
        Assert.Equal(HttpStatusCode.Created, respuestaProducto.StatusCode);
        var producto = await respuestaProducto.Content.ReadFromJsonAsync<ArticuloInventarioDto>();
        Assert.NotNull(producto);

        using var altaInsumo = CrearSolicitud(HttpMethod.Post, "/api/inventario/articulos", acceso.TokenAcceso, new
        {
            codigo = (string?)null,
            nombre = $"Tinte{sufijo}",
            tipo = "Insumo",
            unidadBase = "ml",
            manejaFraccion = true,
            categoria = "Coloracion",
            cantidadMinima = 25m,
            precioVenta = (decimal?)null
        });
        using var respuestaInsumo = await clienteHttp.SendAsync(altaInsumo);
        Assert.Equal(HttpStatusCode.Created, respuestaInsumo.StatusCode);
        var insumo = await respuestaInsumo.Content.ReadFromJsonAsync<ArticuloInventarioDto>();
        Assert.NotNull(insumo);
        Assert.True(insumo.ManejaFraccion);

        using var cambioMonedaInventario = CrearSolicitud(HttpMethod.Put, "/api/configuracion", acceso.TokenAcceso, new
        {
            nombreComercial = "Salón de prueba",
            razonSocial = (string?)null,
            identificacionFiscal = (string?)null,
            direccion = (string?)null,
            telefono = (string?)null,
            correo = (string?)null,
            codigoMoneda = "USD",
            permitirVentaSinStock = false,
            comisionProductosHabilitada = false
        });
        using var respuestaCambioMonedaInventario = await clienteHttp.SendAsync(cambioMonedaInventario);
        Assert.Equal(HttpStatusCode.Conflict, respuestaCambioMonedaInventario.StatusCode);

        var solicitudCompra = new
        {
            proveedorId = proveedor.Id,
            fechaCompraUtc = DateTimeOffset.UtcNow,
            referencia = $"FC-{sufijo}",
            observacion = (string?)null,
            detalles = new[]
            {
                new { articuloId = producto.Id, cantidad = 10m, costoUnitario = 1000m },
                new { articuloId = insumo.Id, cantidad = 200.5m, costoUnitario = 50m }
            }
        };
        using var altaCompra = CrearSolicitud(HttpMethod.Post, "/api/compras", acceso.TokenAcceso, solicitudCompra);
        using var respuestaCompra = await clienteHttp.SendAsync(altaCompra);
        Assert.Equal(HttpStatusCode.Created, respuestaCompra.StatusCode);
        var compra = await respuestaCompra.Content.ReadFromJsonAsync<CompraDto>();
        Assert.NotNull(compra);
        Assert.Equal("Borrador", compra.Estado);
        Assert.Equal(20025m, compra.Total);

        using var confirmarCompra = new HttpRequestMessage(HttpMethod.Post, $"/api/compras/{compra.Id}/confirmar");
        confirmarCompra.Headers.Authorization = new AuthenticationHeaderValue("Bearer", acceso.TokenAcceso);
        using var respuestaConfirmacion = await clienteHttp.SendAsync(confirmarCompra);
        Assert.Equal(HttpStatusCode.OK, respuestaConfirmacion.StatusCode);
        var compraConfirmada = await respuestaConfirmacion.Content.ReadFromJsonAsync<CompraDto>();
        Assert.NotNull(compraConfirmada);
        Assert.Equal("Confirmada", compraConfirmada.Estado);

        var articulos = await clienteHttp.GetFromJsonAsync<PaginaArticulosInventarioDto>(
            $"/api/inventario/articulos?buscar=shampoo{sufijo}");
        Assert.NotNull(articulos);
        var productoConStock = Assert.Single(articulos.Elementos);
        Assert.Equal(10m, productoConStock.ExistenciaActual);
        Assert.Equal(1000m, productoConStock.CostoPromedio);

        var segundaCompra = new
        {
            proveedorId = proveedor.Id,
            fechaCompraUtc = DateTimeOffset.UtcNow,
            referencia = $"FC2-{sufijo}",
            observacion = (string?)null,
            detalles = new[] { new { articuloId = producto.Id, cantidad = 10m, costoUnitario = 1400m } }
        };
        using var altaSegundaCompra = CrearSolicitud(HttpMethod.Post, "/api/compras", acceso.TokenAcceso, segundaCompra);
        using var respuestaSegundaCompra = await clienteHttp.SendAsync(altaSegundaCompra);
        Assert.Equal(HttpStatusCode.Created, respuestaSegundaCompra.StatusCode);
        var compraDos = await respuestaSegundaCompra.Content.ReadFromJsonAsync<CompraDto>();
        Assert.NotNull(compraDos);
        using var confirmarCompraDos = new HttpRequestMessage(HttpMethod.Post, $"/api/compras/{compraDos.Id}/confirmar");
        confirmarCompraDos.Headers.Authorization = new AuthenticationHeaderValue("Bearer", acceso.TokenAcceso);
        using var respuestaConfirmacionDos = await clienteHttp.SendAsync(confirmarCompraDos);
        Assert.Equal(HttpStatusCode.OK, respuestaConfirmacionDos.StatusCode);

        var articuloActualizado = await clienteHttp.GetFromJsonAsync<PaginaArticulosInventarioDto>(
            $"/api/inventario/articulos?buscar=shampoo{sufijo}");
        Assert.NotNull(articuloActualizado);
        Assert.Equal(1200m, Assert.Single(articuloActualizado.Elementos).CostoPromedio);

        using var salidaAjuste = CrearSolicitud(
            HttpMethod.Post, $"/api/inventario/articulos/{producto.Id}/salidas-ajuste", acceso.TokenAcceso,
            new { cantidad = 2m, motivo = "Consumo interno de prueba" });
        using var respuestaSalida = await clienteHttp.SendAsync(salidaAjuste);
        Assert.Equal(HttpStatusCode.OK, respuestaSalida.StatusCode);

        using var confirmarDeNuevo = new HttpRequestMessage(HttpMethod.Post, $"/api/compras/{compra.Id}/confirmar");
        confirmarDeNuevo.Headers.Authorization = new AuthenticationHeaderValue("Bearer", acceso.TokenAcceso);
        using var respuestaReconfirmacion = await clienteHttp.SendAsync(confirmarDeNuevo);
        Assert.Equal(HttpStatusCode.Conflict, respuestaReconfirmacion.StatusCode);

        using var editarConfirmada = CrearSolicitud(HttpMethod.Put, $"/api/compras/{compra.Id}", acceso.TokenAcceso, solicitudCompra);
        using var respuestaEditarConfirmada = await clienteHttp.SendAsync(editarConfirmada);
        Assert.Equal(HttpStatusCode.Conflict, respuestaEditarConfirmada.StatusCode);

        using var cambioMoneda = CrearSolicitud(HttpMethod.Put, "/api/configuracion", acceso.TokenAcceso, new
        {
            nombreComercial = "Salón de prueba",
            razonSocial = (string?)null,
            identificacionFiscal = (string?)null,
            direccion = (string?)null,
            telefono = (string?)null,
            correo = (string?)null,
            codigoMoneda = "USD",
            permitirVentaSinStock = false,
            comisionProductosHabilitada = false
        });
        using var respuestaCambioMoneda = await clienteHttp.SendAsync(cambioMoneda);
        Assert.Equal(HttpStatusCode.Conflict, respuestaCambioMoneda.StatusCode);

        var serviciosActivos = await clienteHttp.GetAsync("/api/servicios?pagina=1&tamanoPagina=5");
        Assert.Equal(HttpStatusCode.OK, serviciosActivos.StatusCode);
        var paginaServicios = await serviciosActivos.Content.ReadFromJsonAsync<PaginaServiciosDto>();
        Assert.NotNull(paginaServicios);
        Assert.Contains(paginaServicios.Elementos, elemento => elemento.Id == servicio.Id);

        using var desactivarServicio = CrearSolicitud(
            HttpMethod.Patch, $"/api/servicios/{servicio.Id}/estado", acceso.TokenAcceso, new { activo = false });
        using var respuestaDesactivarServicio = await clienteHttp.SendAsync(desactivarServicio);
        Assert.Equal(HttpStatusCode.OK, respuestaDesactivarServicio.StatusCode);

        var serviciosAdministrativos = await clienteHttp.GetAsync("/api/servicios/administracion?activo=false");
        Assert.Equal(HttpStatusCode.OK, serviciosAdministrativos.StatusCode);
        var paginaAdministrativa = await serviciosAdministrativos.Content.ReadFromJsonAsync<PaginaServiciosDto>();
        Assert.NotNull(paginaAdministrativa);
        Assert.Contains(paginaAdministrativa.Elementos, elemento => elemento.Id == servicio.Id);
    }

    private static HttpRequestMessage CrearSolicitud(HttpMethod metodo, string ruta, string token, object cuerpo)
    {
        var solicitud = new HttpRequestMessage(metodo, ruta) { Content = JsonContent.Create(cuerpo) };
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return solicitud;
    }
}
