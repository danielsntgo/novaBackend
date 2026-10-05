namespace FlexPos.Application.DTOs.Compras;

public sealed record DetalleCompraDto(
    Guid Id,
    Guid ArticuloId,
    string NombreArticulo,
    string UnidadBase,
    decimal Cantidad,
    decimal CostoUnitario,
    decimal TotalLinea,
    string CodigoMoneda);

public sealed record CompraDto(
    Guid Id,
    Guid ProveedorId,
    string NombreProveedor,
    DateTimeOffset FechaCompraUtc,
    string? Referencia,
    string? Observacion,
    string CodigoMoneda,
    string Estado,
    DateTimeOffset? FechaConfirmacionUtc,
    decimal Total,
    IReadOnlyList<DetalleCompraDto> Detalles,
    DateTimeOffset FechaCreacionUtc,
    DateTimeOffset? FechaModificacionUtc);

public sealed record PaginaComprasDto(
    IReadOnlyList<CompraDto> Elementos,
    int Pagina,
    int TamanoPagina,
    int Total);

public sealed record SolicitudLineaCompra(Guid ArticuloId, decimal Cantidad, decimal CostoUnitario);

public sealed record SolicitudCompra(
    Guid ProveedorId,
    DateTimeOffset FechaCompraUtc,
    string? Referencia,
    string? Observacion,
    IReadOnlyList<SolicitudLineaCompra> Detalles);
