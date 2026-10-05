namespace FlexPos.Application.DTOs.Clientes;

public sealed record ClienteDto(
    Guid Id,
    string Nombre,
    string? Documento,
    string? Telefono,
    string? Correo,
    bool Activo,
    DateTimeOffset FechaCreacionUtc,
    DateTimeOffset? FechaModificacionUtc);

public sealed record PaginaClientesDto(
    IReadOnlyList<ClienteDto> Elementos,
    int Pagina,
    int TamanoPagina,
    int TotalElementos);

public sealed record SolicitudCliente(
    string? Nombre,
    string? Documento,
    string? Telefono,
    string? Correo);
