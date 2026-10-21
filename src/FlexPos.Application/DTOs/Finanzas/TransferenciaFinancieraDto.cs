namespace FlexPos.Application.DTOs.Finanzas;

public sealed record TransferenciaFinancieraDto(
    Guid Id,
    Guid CuentaOrigenId,
    string CuentaOrigenNombre,
    Guid CuentaDestinoId,
    string CuentaDestinoNombre,
    decimal Importe,
    string CodigoMoneda,
    string? Concepto,
    string? Referencia,
    DateTimeOffset FechaUtc,
    Guid? UsuarioId);

public sealed record PaginaTransferenciasFinancierasDto(
    IReadOnlyList<TransferenciaFinancieraDto> Elementos,
    int Pagina,
    int TamanoPagina,
    int TotalElementos);

public sealed record SolicitudTransferenciaFinanciera(
    Guid CuentaOrigenId,
    Guid CuentaDestinoId,
    decimal Importe,
    string? Concepto,
    string? Referencia);
