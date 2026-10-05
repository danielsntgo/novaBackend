namespace FlexPos.Api.DTOs;

public sealed record GuardarEmpleadoRequest(
    string? Nombre,
    string? Cargo,
    string? Documento,
    string? Telefono,
    string? Correo);
