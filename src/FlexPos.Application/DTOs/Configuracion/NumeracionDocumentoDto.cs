using FlexPos.Domain.Enums;

namespace FlexPos.Application.DTOs.Configuracion;

public sealed record NumeracionDocumentoDto(
    Guid Id,
    string TipoDocumento,
    string? Prefijo,
    long SiguienteNumero,
    DateTimeOffset FechaCreacionUtc,
    DateTimeOffset? FechaModificacionUtc);

public sealed record SolicitudNumeracionDocumento(
    TipoDocumentoVenta TipoDocumento,
    string? Prefijo,
    long SiguienteNumero);

public sealed record SolicitudPrefijoDocumento(string? Prefijo);
