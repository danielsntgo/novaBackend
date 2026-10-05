namespace FlexPos.Application.DTOs.Configuracion;

public sealed record ImpuestoDto(
    Guid Id,
    string Nombre,
    decimal Porcentaje,
    bool Activo,
    DateTimeOffset FechaCreacionUtc,
    DateTimeOffset? FechaModificacionUtc);

public sealed record SolicitudImpuesto(string? Nombre, decimal Porcentaje);
