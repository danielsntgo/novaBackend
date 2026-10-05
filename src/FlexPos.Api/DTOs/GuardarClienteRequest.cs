namespace FlexPos.Api.DTOs;

public sealed record GuardarClienteRequest(
    string? Nombre,
    string? Documento,
    string? Telefono,
    string? Correo);
