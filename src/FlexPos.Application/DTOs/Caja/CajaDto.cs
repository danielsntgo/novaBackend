namespace FlexPos.Application.DTOs.Caja;

public sealed record MovimientoCajaDto(
    Guid Id,
    string Tipo,
    decimal Importe,
    string CodigoMoneda,
    string? Concepto,
    Guid? VentaId,
    Guid? DevolucionVentaId,
    Guid? LiquidacionComisionId,
    DateTimeOffset FechaUtc,
    Guid? UsuarioId);

public sealed record CajaDto(
    Guid Id,
    string Estado,
    decimal EfectivoApertura,
    DateTimeOffset FechaAperturaUtc,
    Guid UsuarioAperturaId,
    decimal? EfectivoEsperado,
    decimal? EfectivoContadoCierre,
    decimal? DiferenciaCierre,
    DateTimeOffset? FechaCierreUtc,
    Guid? UsuarioCierreId);

public sealed record CajaDetalleDto(CajaDto Caja, IReadOnlyList<MovimientoCajaDto> Movimientos);

public sealed record PaginaCajasDto(
    IReadOnlyList<CajaDto> Elementos,
    int Pagina,
    int TamanoPagina,
    int TotalElementos);

public sealed record SolicitudAbrirCaja(decimal EfectivoInicial);

public sealed record SolicitudCerrarCaja(decimal EfectivoContado);

public sealed record SolicitudMovimientoCaja(string? Tipo, decimal Importe, string? Concepto);
