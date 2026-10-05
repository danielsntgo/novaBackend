namespace FlexPos.Application.DTOs.Citas;

public sealed record CitaDto(
    Guid Id,
    Guid ClienteId,
    string ClienteNombre,
    Guid ServicioId,
    string ServicioNombre,
    Guid EmpleadoId,
    string EmpleadoNombre,
    DateTimeOffset InicioUtc,
    DateTimeOffset FinUtc,
    int DuracionMinutos,
    string Estado,
    DateTimeOffset FechaCreacionUtc,
    DateTimeOffset? FechaModificacionUtc);

public sealed record PaginaCitasDto(
    IReadOnlyList<CitaDto> Elementos,
    int Pagina,
    int TamanoPagina,
    int TotalElementos);

public sealed record SolicitudCita(
    Guid ClienteId,
    Guid ServicioId,
    Guid EmpleadoId,
    DateTimeOffset InicioLocal);

public sealed record SolicitudReprogramarCita(
    Guid EmpleadoId,
    DateTimeOffset InicioLocal);

public sealed record EmpleadoDisponibilidadDto(
    Guid EmpleadoId,
    string EmpleadoNombre,
    IReadOnlyList<PeriodoDisponibleDto> Periodos);

public sealed record PeriodoDisponibleDto(DateTimeOffset Inicio, DateTimeOffset Fin);
