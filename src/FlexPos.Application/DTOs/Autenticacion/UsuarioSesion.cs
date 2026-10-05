namespace FlexPos.Application.DTOs.Autenticacion;

public sealed record UsuarioSesion(
    Guid Id,
    string Correo,
    string SelloSeguridad,
    IReadOnlyList<string> Roles,
    bool CambioContrasenaObligatorio);
