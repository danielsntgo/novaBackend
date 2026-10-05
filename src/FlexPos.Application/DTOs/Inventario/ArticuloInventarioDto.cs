namespace FlexPos.Application.DTOs.Inventario;

public sealed record ArticuloInventarioDto(
    Guid Id,
    string? Codigo,
    string Nombre,
    string Tipo,
    string UnidadBase,
    bool ManejaFraccion,
    string? Categoria,
    decimal ExistenciaActual,
    decimal CantidadMinima,
    decimal CostoPromedio,
    string CodigoMoneda,
    decimal? PrecioVenta,
    bool Activo,
    bool BajoMinimo,
    DateTimeOffset FechaCreacionUtc,
    DateTimeOffset? FechaModificacionUtc);

public sealed record PaginaArticulosInventarioDto(
    IReadOnlyList<ArticuloInventarioDto> Elementos,
    int Pagina,
    int TamanoPagina,
    int Total);

public sealed record MovimientoInventarioDto(
    Guid Id,
    Guid ArticuloId,
    string NombreArticulo,
    string Tipo,
    decimal Cantidad,
    decimal ExistenciaResultante,
    decimal CostoUnitario,
    string CodigoMoneda,
    string? Motivo,
    Guid? CompraId,
    Guid? VentaId,
    DateTimeOffset FechaCreacionUtc);

public sealed record PaginaMovimientosInventarioDto(
    IReadOnlyList<MovimientoInventarioDto> Elementos,
    int Pagina,
    int TamanoPagina,
    int Total);
