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

public sealed record DetalleDevolucionCompraDto(Guid DetalleCompraId, decimal Cantidad, decimal Importe);

public sealed record PagoDevolucionCompraDto(
    Guid MetodoPagoId,
    string MetodoPagoNombre,
    bool EsEfectivo,
    Guid CuentaFinancieraId,
    string CuentaFinancieraNombre,
    decimal Importe,
    string? Referencia);

public sealed record DevolucionCompraDto(
    Guid Id,
    DateTimeOffset FechaUtc,
    string Motivo,
    decimal ImporteLineas,
    decimal ImporteReintegrado,
    IReadOnlyList<DetalleDevolucionCompraDto> Lineas,
    IReadOnlyList<PagoDevolucionCompraDto> Pagos);

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
    decimal TotalDevuelto,
    decimal SaldoNeto,
    string? MotivoAnulacion,
    DateTimeOffset? FechaAnulacionUtc,
    IReadOnlyList<DetalleCompraDto> Detalles,
    IReadOnlyList<DevolucionCompraDto> Devoluciones,
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

public sealed record SolicitudDevolucionCompra(
    string? Motivo,
    IReadOnlyList<SolicitudLineaDevolucionCompra>? Lineas,
    IReadOnlyList<SolicitudPagoCompra>? Pagos);

public sealed record SolicitudLineaDevolucionCompra(Guid DetalleCompraId, decimal Cantidad);

public sealed record SolicitudPagoCompra(Guid MetodoPagoId, decimal Importe, string? Referencia);

public sealed record SolicitudAnularCompra(string? Motivo, IReadOnlyList<SolicitudPagoCompra>? Pagos);
