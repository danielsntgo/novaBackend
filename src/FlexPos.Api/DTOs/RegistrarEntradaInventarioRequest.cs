namespace FlexPos.Api.DTOs;

public sealed record RegistrarEntradaInventarioRequest(
    decimal Cantidad,
    decimal CostoUnitario,
    string? Motivo);
