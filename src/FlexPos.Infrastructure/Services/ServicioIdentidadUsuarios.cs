using FlexPos.Application.DTOs.Autenticacion;
using FlexPos.Application.Interfaces;
using FlexPos.Infrastructure.Identity;
using FlexPos.Infrastructure.Persistence;
using FlexPos.Domain.Errors;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FlexPos.Infrastructure.Services;

public sealed class ServicioIdentidadUsuarios(
    UserManager<UsuarioIdentidad> usuarios,
    SignInManager<UsuarioIdentidad> inicioSesion,
    TimeProvider reloj,
    FlexPosDbContext contexto) : IIdentidadUsuarios
{
    public async Task<UsuarioSesion?> ValidarCredencialesAsync(
        string correo,
        string contrasena,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var usuario = await usuarios.FindByEmailAsync(correo);
        if (usuario is null || !PuedeIniciarSesion(usuario))
        {
            return null;
        }

        var resultado = await inicioSesion.CheckPasswordSignInAsync(
            usuario,
            contrasena,
            lockoutOnFailure: true);

        return resultado.Succeeded ? await CrearSesionAsync(usuario) : null;
    }

    public async Task<UsuarioSesion?> ObtenerSesionAsync(
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var usuario = await usuarios.FindByIdAsync(usuarioId.ToString());
        return usuario is not null && PuedeIniciarSesion(usuario)
            ? await CrearSesionAsync(usuario)
            : null;
    }

    public async Task<Resultado<UsuarioSesion>> CambiarContrasenaAsync(
        Guid usuarioId,
        string contrasenaActual,
        string contrasenaNueva,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var usuario = await usuarios.FindByIdAsync(usuarioId.ToString());
        if (usuario is null || !usuario.Activo)
        {
            return Resultado<UsuarioSesion>.Fallo(new ErrorDominio(
                "usuario.no_encontrado",
                "No se encontró un usuario activo para cambiar la contraseña."));
        }

        var cambioObligatorioAnterior = usuario.CambioContrasenaObligatorio;
        usuario.CambioContrasenaObligatorio = false;
        await using var transaccion = await contexto.Database.BeginTransactionAsync(cancellationToken);
        var cambio = await usuarios.ChangePasswordAsync(usuario, contrasenaActual, contrasenaNueva);
        if (!cambio.Succeeded)
        {
            await transaccion.RollbackAsync(cancellationToken);
            usuario.CambioContrasenaObligatorio = cambioObligatorioAnterior;
            var codigo = cambio.Errors.Any(error => error.Code == "PasswordMismatch")
                ? "usuario.contrasena_actual_invalida"
                : "usuario.contrasena_no_cumple_politica";
            var mensaje = codigo == "usuario.contrasena_actual_invalida"
                ? "La contraseña actual no es correcta."
                : "La nueva contraseña no cumple la política de seguridad.";
            return Resultado<UsuarioSesion>.Fallo(new ErrorDominio(codigo, mensaje));
        }

        var selloActualizado = await usuarios.UpdateSecurityStampAsync(usuario);
        if (!selloActualizado.Succeeded)
        {
            await transaccion.RollbackAsync(cancellationToken);
            return Resultado<UsuarioSesion>.Fallo(new ErrorDominio(
                "usuario.no_actualizado",
                "La contraseña cambió, pero no se pudo invalidar la sesión anterior."));
        }

        await transaccion.CommitAsync(cancellationToken);
        return Resultado<UsuarioSesion>.Exito(await CrearSesionAsync(usuario));
    }

    private bool PuedeIniciarSesion(UsuarioIdentidad usuario) =>
        usuario.Activo && (usuario.LockoutEnd is null || usuario.LockoutEnd <= reloj.GetUtcNow());

    private async Task<UsuarioSesion> CrearSesionAsync(UsuarioIdentidad usuario)
    {
        var roles = await usuarios.GetRolesAsync(usuario);
        return new UsuarioSesion(
            usuario.Id,
            usuario.Email ?? usuario.UserName ?? string.Empty,
            usuario.SecurityStamp ?? string.Empty,
            roles.ToArray(),
            usuario.CambioContrasenaObligatorio);
}
}
