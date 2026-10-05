namespace FlexPos.Application.DTOs.Comisiones;

public sealed record ReglaComisionDto(
    Guid Id,
    Guid EmpleadoId,
    Guid ServicioId,
    string Tipo,
    decimal Valor,
    bool Activa,
    DateTimeOffset FechaCreacionUtc,
    DateTimeOffset? FechaModificacionUtc);

public sealed record PaginaReglasComisionDto(
    IReadOnlyList<ReglaComisionDto> Elementos,
    int Pagina,
    int TamanoPagina,
    int TotalElementos);

public sealed record ComisionDto(
    Guid Id,
    Guid EmpleadoId,
    Guid ServicioId,
    Guid VentaId,
    Guid DetalleVentaId,
    string TipoMovimiento,
    string TipoTarifa,
    decimal ValorTarifa,
    decimal BaseCalculo,
    decimal Cantidad,
    decimal Importe,
    string CodigoMoneda,
    string Estado,
    Guid? ComisionOriginalId,
    Guid? DevolucionVentaId,
    Guid? LiquidacionComisionId,
    DateTimeOffset FechaCreacionUtc);

public sealed record PaginaComisionesDto(
    IReadOnlyList<ComisionDto> Elementos,
    int Pagina,
    int TamanoPagina,
    int TotalElementos);

public sealed record LiquidacionComisionDto(
    Guid Id,
    Guid EmpleadoId,
    Guid MetodoPagoId,
    string MetodoPagoNombre,
    bool EsEfectivo,
    decimal Importe,
    string CodigoMoneda,
    string? Referencia,
    Guid? CajaId,
    DateTimeOffset FechaUtc,
    IReadOnlyList<Guid> ComisionIds);

public sealed record PaginaLiquidacionesComisionDto(
    IReadOnlyList<LiquidacionComisionDto> Elementos,
    int Pagina,
    int TamanoPagina,
    int TotalElementos);

public sealed record SolicitudCrearReglaComision(
    Guid EmpleadoId,
    Guid ServicioId,
    string? Tipo,
    decimal Valor);

public sealed record SolicitudActualizarReglaComision(string? Tipo, decimal Valor);
public sealed record SolicitudEstadoReglaComision(bool Activa);
public sealed record SolicitudLiquidarComisiones(
    IReadOnlyList<Guid>? ComisionIds,
    Guid MetodoPagoId,
    string? Referencia);
