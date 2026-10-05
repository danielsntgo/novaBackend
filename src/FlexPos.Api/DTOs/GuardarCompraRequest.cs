namespace FlexPos.Api.DTOs;

public sealed record GuardarCompraRequest(
    Guid ProveedorId,
    DateTimeOffset FechaCompraUtc,
    string? Referencia,
    string? Observacion,
    IReadOnlyList<LineaCompraRequest>? Detalles);

public sealed record LineaCompraRequest(Guid ArticuloId, decimal Cantidad, decimal CostoUnitario);
