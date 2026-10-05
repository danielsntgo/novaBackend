namespace FlexPos.Application.DTOs.Usuarios;

public sealed record UsuarioAdministradoDto(
    Guid Id,
    string Correo,
    bool Activo,
    bool CambioContrasenaObligatorio,
    DateTimeOffset FechaCreacionUtc);

public sealed record PaginaUsuariosDto(
    IReadOnlyList<UsuarioAdministradoDto> Elementos,
    int Pagina,
    int TamanoPagina,
    int TotalElementos);

public sealed record RecepcionistaCreadaDto(
    UsuarioAdministradoDto Usuario,
    string ContrasenaTemporal);

public sealed record SolicitudEstadoUsuario(bool Activo);
