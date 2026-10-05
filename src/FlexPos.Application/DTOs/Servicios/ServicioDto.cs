namespace FlexPos.Application.DTOs.Servicios;

public sealed record ServicioDto(
    Guid Id,
    string Nombre,
    string? Descripcion,
    string? Categoria,
    decimal Precio,
    string CodigoMoneda,
    int DuracionMinutos,
    bool Activo,
    DateTimeOffset FechaCreacionUtc,
    DateTimeOffset? FechaModificacionUtc);

public sealed record PaginaServiciosDto(
    IReadOnlyList<ServicioDto> Elementos,
    int Pagina,
    int TamanoPagina,
    int TotalElementos);

public sealed record SolicitudServicio(
    string? Nombre,
    string? Descripcion,
    string? Categoria,
    decimal Precio,
    int DuracionMinutos);
