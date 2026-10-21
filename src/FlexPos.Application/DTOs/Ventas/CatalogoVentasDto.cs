namespace FlexPos.Application.DTOs.Ventas;

public sealed record MetodoPagoOperacionDto(Guid Id, string Nombre, bool RequiereReferencia, bool EsEfectivo);
public sealed record ImpuestoOperacionDto(Guid Id, string Nombre, decimal Porcentaje);
public sealed record EmpleadoOperacionDto(Guid Id, string Nombre, string Cargo);
public sealed record ProductoOperacionDto(
    Guid Id,
    string? Codigo,
    string Nombre,
    string UnidadBase,
    bool ManejaFraccion,
    decimal ExistenciaActual,
    decimal? PrecioVenta,
    string CodigoMoneda);

public sealed record CatalogoVentasDto(
    IReadOnlyList<MetodoPagoOperacionDto> MetodosPago,
    IReadOnlyList<ImpuestoOperacionDto> Impuestos,
    IReadOnlyList<EmpleadoOperacionDto> Empleados,
    string CodigoMoneda,
    bool PermitirVentaSinStock,
    bool PermitirSaldosPendientes,
    bool FacturacionHabilitada,
    bool ExigirEmpleadoVentaServicio);

public sealed record PaginaProductosOperacionDto(
    IReadOnlyList<ProductoOperacionDto> Elementos,
    int Pagina,
    int TamanoPagina,
    int TotalElementos);
