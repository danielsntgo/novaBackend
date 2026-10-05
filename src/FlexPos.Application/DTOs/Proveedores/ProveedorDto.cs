namespace FlexPos.Application.DTOs.Proveedores;

public sealed record ProveedorDto(
    Guid Id,
    string Nombre,
    string? IdentificacionFiscal,
    string? Telefono,
    string? Correo,
    bool Activo,
    DateTimeOffset FechaCreacionUtc,
    DateTimeOffset? FechaModificacionUtc);

public sealed record PaginaProveedoresDto(
    IReadOnlyList<ProveedorDto> Elementos,
    int Pagina,
    int TamanoPagina,
    int Total);

public sealed record SolicitudProveedor(
    string? Nombre,
    string? IdentificacionFiscal,
    string? Telefono,
    string? Correo);
