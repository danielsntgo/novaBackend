namespace FlexPos.Api.DTOs;

public sealed record GuardarArticuloInventarioRequest(
    string? Codigo,
    string? Nombre,
    string? Tipo,
    string? UnidadBase,
    bool ManejaFraccion,
    string? Categoria,
    decimal CantidadMinima,
    decimal? PrecioVenta);
