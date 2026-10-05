namespace FlexPos.Application.DTOs.Empleados;

public sealed record EmpleadoDto(
    Guid Id,
    string Nombre,
    string Cargo,
    string? Documento,
    string? Telefono,
    string? Correo,
    bool Activo,
    DateTimeOffset FechaCreacionUtc,
    DateTimeOffset? FechaModificacionUtc);

public sealed record PaginaEmpleadosDto(
    IReadOnlyList<EmpleadoDto> Elementos,
    int Pagina,
    int TamanoPagina,
    int TotalElementos);

public sealed record SolicitudEmpleado(
    string? Nombre,
    string? Cargo,
    string? Documento,
    string? Telefono,
    string? Correo);
