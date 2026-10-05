namespace FlexPos.Api.DTOs;

public sealed record GuardarProveedorRequest(
    string? Nombre,
    string? IdentificacionFiscal,
    string? Telefono,
    string? Correo);
