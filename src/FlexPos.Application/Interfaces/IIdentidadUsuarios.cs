using FlexPos.Application.DTOs.Autenticacion;
using FlexPos.Domain.Errors;

namespace FlexPos.Application.Interfaces;

public interface IIdentidadUsuarios
{
    Task<UsuarioSesion?> ValidarCredencialesAsync(
        string correo,
        string contrasena,
        CancellationToken cancellationToken);

    Task<UsuarioSesion?> ObtenerSesionAsync(Guid usuarioId, CancellationToken cancellationToken);

    Task<Resultado<UsuarioSesion>> CambiarContrasenaAsync(
        Guid usuarioId,
        string contrasenaActual,
        string contrasenaNueva,
        CancellationToken cancellationToken);
}
