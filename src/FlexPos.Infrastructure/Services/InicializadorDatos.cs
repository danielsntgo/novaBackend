using FlexPos.Application.Authorization;
using FlexPos.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FlexPos.Infrastructure.Services;

public sealed class InicializadorDatos(
    RoleManager<RolIdentidad> roles,
    UserManager<UsuarioIdentidad> usuarios,
    IConfiguration configuracion,
    ILogger<InicializadorDatos> logger)
{
    private static readonly string[] RolesIniciales = [RolesSistema.Administrador, RolesSistema.Recepcionista];

    public async Task InicializarAsync(CancellationToken cancellationToken = default)
    {
        foreach (var nombreRol in RolesIniciales)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await roles.RoleExistsAsync(nombreRol))
            {
                continue;
            }

            var resultado = await roles.CreateAsync(new RolIdentidad { Name = nombreRol });
            AsegurarExito(resultado, $"No se pudo crear el rol {nombreRol}.");
        }

        var correo = configuracion["AdministradorInicial:Correo"];
        var contrasena = configuracion["AdministradorInicial:Contrasena"];
        if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(contrasena))
        {
            logger.LogWarning(
                "Se crearon los roles iniciales, pero no se creó el administrador. Configure AdministradorInicial:Correo y AdministradorInicial:Contrasena en un proveedor seguro.");
            return;
        }

        if (await usuarios.FindByEmailAsync(correo) is not null)
        {
            return;
        }

        var administrador = new UsuarioIdentidad
        {
            Email = correo.Trim(),
            UserName = correo.Trim(),
            EmailConfirmed = true
        };
        var alta = await usuarios.CreateAsync(administrador, contrasena);
        AsegurarExito(alta, "No se pudo crear el administrador inicial.");

        var asignacion = await usuarios.AddToRoleAsync(administrador, RolesSistema.Administrador);
        AsegurarExito(asignacion, "No se pudo asignar el rol Administrador al usuario inicial.");
    }

    private static void AsegurarExito(IdentityResult resultado, string mensaje)
    {
        if (!resultado.Succeeded)
        {
            var errores = string.Join(", ", resultado.Errors.Select(error => error.Code));
            throw new InvalidOperationException($"{mensaje} Códigos: {errores}");
        }
    }
}
