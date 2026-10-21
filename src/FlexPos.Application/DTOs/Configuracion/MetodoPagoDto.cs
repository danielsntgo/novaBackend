namespace FlexPos.Application.DTOs.Configuracion;

public sealed record MetodoPagoDto(
    Guid Id,
    string Nombre,
    bool RequiereReferencia,
    bool EsEfectivo,
    bool Activo,
    Guid CuentaFinancieraId,
    string CuentaFinancieraNombre,
    DateTimeOffset FechaCreacionUtc,
    DateTimeOffset? FechaModificacionUtc);

public sealed record SolicitudMetodoPago(
    string? Nombre,
    bool RequiereReferencia,
    Guid CuentaFinancieraId,
    bool EsEfectivo = false);
