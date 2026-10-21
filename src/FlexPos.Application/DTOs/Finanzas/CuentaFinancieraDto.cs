namespace FlexPos.Application.DTOs.Finanzas;

public sealed record CuentaFinancieraDto(
    Guid Id,
    string Nombre,
    string Tipo,
    string CodigoMoneda,
    string? Entidad,
    string? UltimosDigitos,
    decimal? CupoCredito,
    decimal Saldo,
    bool Activa,
    DateTimeOffset FechaCreacionUtc,
    DateTimeOffset? FechaModificacionUtc);

public sealed record PaginaCuentasFinancierasDto(
    IReadOnlyList<CuentaFinancieraDto> Elementos,
    int Pagina,
    int TamanoPagina,
    int TotalElementos);

public sealed record MovimientoFinancieroDto(
    Guid Id,
    string Tipo,
    string Sentido,
    decimal Importe,
    string CodigoMoneda,
    string? Concepto,
    DateTimeOffset FechaUtc,
    Guid? CajaId,
    Guid? MetodoPagoId,
    Guid? VentaId,
    Guid? DevolucionVentaId,
    Guid? LiquidacionComisionId,
    Guid? TransferenciaFinancieraId,
    Guid? UsuarioId,
    bool Migrado,
    decimal SaldoPosterior);

public sealed record PaginaMovimientosFinancierosDto(
    IReadOnlyList<MovimientoFinancieroDto> Elementos,
    int Pagina,
    int TamanoPagina,
    int TotalElementos);

public sealed record ResumenFinancieroDto(
    decimal DisponibleTotal,
    decimal DeudaTarjetasTotal,
    int CuentasActivas);

public sealed record SolicitudCuentaFinanciera(
    string? Nombre,
    string? Tipo,
    string? CodigoMoneda,
    string? Entidad,
    string? UltimosDigitos,
    decimal? CupoCredito);

public sealed record SolicitudEstadoCuentaFinanciera(bool Activa);
