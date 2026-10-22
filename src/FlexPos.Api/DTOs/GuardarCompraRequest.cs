namespace FlexPos.Api.DTOs;

public sealed record GuardarCompraRequest(
    Guid ProveedorId,
    DateTimeOffset FechaCompraUtc,
    string? Referencia,
    string? Observacion,
    IReadOnlyList<LineaCompraRequest>? Detalles);

public sealed record LineaCompraRequest(Guid ArticuloId, decimal Cantidad, decimal CostoUnitario);

public sealed record DevolucionCompraRequest(
    string? Motivo,
    IReadOnlyList<LineaDevolucionCompraRequest>? Lineas,
    IReadOnlyList<PagoCompraRequest>? Pagos);

public sealed record LineaDevolucionCompraRequest(Guid DetalleCompraId, decimal Cantidad);

public sealed record PagoCompraRequest(Guid MetodoPagoId, decimal Importe, string? Referencia);

public sealed record AnularCompraRequest(string? Motivo, IReadOnlyList<PagoCompraRequest>? Pagos);
