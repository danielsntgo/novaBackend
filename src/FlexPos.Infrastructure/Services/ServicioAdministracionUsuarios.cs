using System.Security.Cryptography;
using FlexPos.Application.Authorization;
using FlexPos.Application.DTOs.Usuarios;
using FlexPos.Application.Interfaces;
using FlexPos.Domain.Errors;
using FlexPos.Infrastructure.Identity;
using FlexPos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FlexPos.Infrastructure.Services;

public sealed class ServicioAdministracionUsuarios(
    UserManager<UsuarioIdentidad> usuarios,
    RoleManager<RolIdentidad> roles,
    FlexPosDbContext contexto) : IAdministracionUsuarios
{
    private const string RolRecepcionista = RolesSistema.Recepcionista;

    public async Task<UsuarioAdministradoDto?> ObtenerRecepcionistaAsync(
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var usuario = await usuarios.FindByIdAsync(usuarioId.ToString());
        return usuario is not null && await usuarios.IsInRoleAsync(usuario, RolRecepcionista)
            ? Convertir(usuario)
            : null;
    }

    public async Task<PaginaUsuariosDto> ListarRecepcionistasAsync(
        string? buscar,
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken)
    {
        var rol = await roles.FindByNameAsync(RolRecepcionista);
        if (rol is null)
        {
            return new PaginaUsuariosDto([], pagina, tamanoPagina, 0);
        }

        var consulta = usuarios.Users.AsNoTracking()
            .Where(usuario => contexto.UserRoles.Any(vinculo =>
                vinculo.UserId == usuario.Id && vinculo.RoleId == rol.Id));

        var texto = buscar?.Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(texto))
        {
            consulta = consulta.Where(usuario =>
                usuario.NormalizedEmail != null && usuario.NormalizedEmail.Contains(texto));
        }

        var total = await consulta.CountAsync(cancellationToken);
        var elementos = await consulta
            .OrderBy(usuario => usuario.NormalizedEmail)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(usuario => new UsuarioAdministradoDto(
                usuario.Id,
                usuario.Email ?? string.Empty,
                usuario.Activo,
                usuario.CambioContrasenaObligatorio,
                usuario.FechaCreacionUtc))
            .ToArrayAsync(cancellationToken);

        return new PaginaUsuariosDto(elementos, pagina, tamanoPagina, total);
    }

    public async Task<Resultado<RecepcionistaCreadaDto>> CrearRecepcionistaAsync(
        string correo,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rolExiste = await roles.RoleExistsAsync(RolRecepcionista);
        if (!rolExiste)
        {
            return Resultado<RecepcionistaCreadaDto>.Fallo(new ErrorDominio(
                "usuario.rol_no_configurado",
                "No está disponible el rol Recepcionista."));
        }

        if (await usuarios.FindByEmailAsync(correo) is not null)
        {
            return ErrorCorreoDuplicado();
        }

        var contrasenaTemporal = GenerarContrasenaTemporal();
        await using var transaccion = await contexto.Database.BeginTransactionAsync(cancellationToken);
        var usuario = new UsuarioIdentidad
        {
            Email = correo,
            UserName = correo,
            EmailConfirmed = false,
            CambioContrasenaObligatorio = true
        };

        try
        {
            var alta = await usuarios.CreateAsync(usuario, contrasenaTemporal);
            if (!alta.Succeeded)
            {
                await transaccion.RollbackAsync(cancellationToken);
                var duplicado = alta.Errors.Any(error => error.Code is "DuplicateEmail" or "DuplicateUserName");
                return duplicado
                    ? ErrorCorreoDuplicado()
                    : Resultado<RecepcionistaCreadaDto>.Fallo(new ErrorDominio(
                        "usuario.no_creado",
                        "No se pudo crear la cuenta de Recepcionista."));
            }

            var asignacion = await usuarios.AddToRoleAsync(usuario, RolRecepcionista);
            if (!asignacion.Succeeded)
            {
                await transaccion.RollbackAsync(cancellationToken);
                return Resultado<RecepcionistaCreadaDto>.Fallo(new ErrorDominio(
                    "usuario.rol_no_asignado",
                    "No se pudo asignar el rol Recepcionista."));
            }

            await transaccion.CommitAsync(cancellationToken);
            var administrado = Convertir(usuario);
            return Resultado<RecepcionistaCreadaDto>.Exito(
                new RecepcionistaCreadaDto(administrado, contrasenaTemporal));
        }
        catch (DbUpdateException excepcion) when
            (excepcion.GetBaseException() is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            await transaccion.RollbackAsync(cancellationToken);
            contexto.ChangeTracker.Clear();
            return ErrorCorreoDuplicado();
        }
    }

    public async Task<Resultado<UsuarioAdministradoDto>> EstablecerEstadoAsync(
        Guid usuarioId,
        bool activo,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var usuario = await usuarios.FindByIdAsync(usuarioId.ToString());
        if (usuario is null || !await usuarios.IsInRoleAsync(usuario, RolRecepcionista))
        {
            return Resultado<UsuarioAdministradoDto>.Fallo(new ErrorDominio(
                "usuario.no_encontrado",
                "No se encontró una cuenta de Recepcionista con ese identificador."));
        }

        if (usuario.Activo != activo)
        {
            usuario.Activo = activo;
            var actualizacion = await usuarios.UpdateSecurityStampAsync(usuario);
            if (!actualizacion.Succeeded)
            {
                return Resultado<UsuarioAdministradoDto>.Fallo(new ErrorDominio(
                    "usuario.no_actualizado",
                    "No se pudo actualizar el estado de la cuenta."));
            }
        }

        return Resultado<UsuarioAdministradoDto>.Exito(Convertir(usuario));
    }

    private static UsuarioAdministradoDto Convertir(UsuarioIdentidad usuario) => new(
        usuario.Id,
        usuario.Email ?? string.Empty,
        usuario.Activo,
        usuario.CambioContrasenaObligatorio,
        usuario.FechaCreacionUtc);

    private static Resultado<RecepcionistaCreadaDto> ErrorCorreoDuplicado() =>
        Resultado<RecepcionistaCreadaDto>.Fallo(new ErrorDominio(
            "usuario.correo_duplicado",
            "Ya existe una cuenta con ese correo."));

    private static string GenerarContrasenaTemporal()
    {
        var aleatorio = Convert.ToBase64String(RandomNumberGenerator.GetBytes(36))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        return $"F!{aleatorio}a1";
    }
}
