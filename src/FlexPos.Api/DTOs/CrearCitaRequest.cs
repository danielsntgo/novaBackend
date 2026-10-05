namespace FlexPos.Api.DTOs;

public sealed record CrearCitaRequest(
    Guid ClienteId,
    Guid ServicioId,
    Guid EmpleadoId,
    DateTimeOffset InicioLocal);

public sealed record ReprogramarCitaRequest(Guid EmpleadoId, DateTimeOffset InicioLocal);

public sealed record CambiarEstadoCitaRequest(string? Estado);
