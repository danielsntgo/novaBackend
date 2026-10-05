using FlexPos.Domain.Enums;

namespace FlexPos.Application.DTOs.Inventario;

public sealed record SolicitudArticuloInventario(
    string? Codigo,
    string? Nombre,
    TipoArticuloInventario Tipo,
    string? UnidadBase,
    bool ManejaFraccion,
    string? Categoria,
    decimal CantidadMinima,
    decimal? PrecioVenta);

public sealed record SolicitudEntradaInventario(
    decimal Cantidad,
    decimal CostoUnitario,
    string? Motivo);

public sealed record SolicitudSalidaInventario(decimal Cantidad, string? Motivo);
