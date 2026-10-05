namespace FlexPos.Application.DTOs.Configuracion;

public sealed record MetodoPagoDto(
    Guid Id,
    string Nombre,
    bool RequiereReferencia,
    bool EsEfectivo,
    bool Activo,
    DateTimeOffset FechaCreacionUtc,
    DateTimeOffset? FechaModificacionUtc);

public sealed record SolicitudMetodoPago(string? Nombre, bool RequiereReferencia, bool EsEfectivo = false);
