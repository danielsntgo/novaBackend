using FlexPos.Application.DTOs.Ventas;
using FlexPos.Application.Interfaces;
using FlexPos.Domain.Enums;
using FlexPos.Domain.Errors;
using FlexPos.Domain.ValueObjects;

namespace FlexPos.Application.Services;

public sealed class ServicioCatalogoVentas(
    IRepositorioConfiguracion configuraciones,
    IRepositorioEmpleados empleados,
    IRepositorioInventario inventario)
{
    public async Task<Resultado<CatalogoVentasDto>> ObtenerAsync(CancellationToken cancellationToken)
    {
        var configuracion = await configuraciones.ObtenerPrincipalAsync(cancellationToken);
        if (configuracion is null)
        {
            return Resultado<CatalogoVentasDto>.Fallo(new ErrorDominio(
                "configuracion.no_configurada", "Primero debe registrar la configuración del establecimiento."));
        }

        var metodos = await configuraciones.ListarMetodosPagoAsync(configuracion.Id, cancellationToken);
        var impuestos = await configuraciones.ListarImpuestosAsync(configuracion.Id, cancellationToken);
        var (empleadosActivos, _) = await empleados.ListarAsync(null, true, 0, int.MaxValue, cancellationToken);
        return Resultado<CatalogoVentasDto>.Exito(new CatalogoVentasDto(
            metodos.Where(x => x.Activo).OrderBy(x => x.NombreNormalizado)
                .Select(x => new MetodoPagoOperacionDto(x.Id, x.Nombre, x.RequiereReferencia, x.EsEfectivo)).ToArray(),
            impuestos.Where(x => x.Activo).OrderBy(x => x.NombreNormalizado)
                .Select(x => new ImpuestoOperacionDto(x.Id, x.Nombre, x.Porcentaje)).ToArray(),
            empleadosActivos.Select(x => new EmpleadoOperacionDto(x.Id, x.Nombre, x.Cargo)).ToArray(),
            configuracion.CodigoMoneda, configuracion.PermitirVentaSinStock,
            configuracion.PermitirSaldosPendientes, configuracion.FacturacionHabilitada,
            configuracion.ExigirEmpleadoVentaServicio));
    }

    public async Task<Resultado<PaginaProductosOperacionDto>> ListarProductosAsync(
        string? buscar,
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken)
    {
        if (pagina < 1 || tamanoPagina is < 1 or > 100 || (pagina - 1L) * tamanoPagina > int.MaxValue)
        {
            return Resultado<PaginaProductosOperacionDto>.Fallo(new ErrorDominio(
                "venta.catalogo_busqueda_invalida", "La búsqueda o la paginación no son válidas."));
        }

        var (productos, total) = await inventario.ListarArticulosAsync(
            TextoNormalizado.Normalizar(buscar), TipoArticuloInventario.Producto, true, false,
            (pagina - 1) * tamanoPagina, tamanoPagina, cancellationToken);
        return Resultado<PaginaProductosOperacionDto>.Exito(new PaginaProductosOperacionDto(
            productos.Select(x => new ProductoOperacionDto(
                x.Id, x.Codigo, x.Nombre, x.UnidadBase, x.ManejaFraccion, x.ExistenciaActual,
                x.PrecioVenta?.Importe, x.CostoPromedio.CodigoMoneda)).ToArray(),
            pagina, tamanoPagina, total));
    }
}
