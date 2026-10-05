namespace FlexPos.Api.DTOs;

public sealed record CrearNumeracionDocumentoRequest(
    string? TipoDocumento,
    string? Prefijo,
    long SiguienteNumero);
