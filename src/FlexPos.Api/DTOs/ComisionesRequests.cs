namespace FlexPos.Api.DTOs;

public sealed record CrearReglaComisionRequest(
    Guid EmpleadoId,
    Guid ServicioId,
    string? Tipo,
    decimal Valor);

public sealed record ActualizarReglaComisionRequest(string? Tipo, decimal Valor);
public sealed record EstadoReglaComisionRequest(bool Activa);
public sealed record LiquidarComisionesRequest(
    IReadOnlyList<Guid>? ComisionIds,
    Guid MetodoPagoId,
    string? Referencia);
