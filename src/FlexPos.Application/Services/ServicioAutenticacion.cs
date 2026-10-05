using System.Security.Cryptography;
using System.Text;
using FlexPos.Application.DTOs.Autenticacion;
using FlexPos.Application.Interfaces;
using FlexPos.Application.Validators;
using FlexPos.Domain.Entities;
using FlexPos.Domain.Errors;

namespace FlexPos.Application.Services;

public sealed class ServicioAutenticacion(
    IIdentidadUsuarios identidad,
    IEmisorTokenAcceso emisorTokenAcceso,
    IRepositorioTokensRenovacion repositorioTokens,
    IConfiguracionTokens configuracion,
    ValidadorInicioSesion validador,
    TimeProvider reloj)
{
    private static readonly ErrorDominio ErrorCredenciales = new(
        "autenticacion.no_autorizado",
        "Las credenciales o el token de renovación no son válidos.");

    public async Task<Resultado<SesionEmitida>> IniciarSesionAsync(
        SolicitudInicioSesion solicitud,
        CancellationToken cancellationToken)
    {
        var errorValidacion = validador.Validar(solicitud);
        if (errorValidacion is not null)
        {
            return Resultado<SesionEmitida>.Fallo(errorValidacion);
        }

        var usuario = await identidad.ValidarCredencialesAsync(
            solicitud.Correo,
            solicitud.Contrasena,
            cancellationToken);

        if (usuario is null)
        {
            return Resultado<SesionEmitida>.Fallo(ErrorCredenciales);
        }

        return await CrearSesionAsync(usuario, cancellationToken);
    }

    public async Task<Resultado<SesionEmitida>> RenovarAsync(
        string? tokenPlano,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tokenPlano))
        {
            return Resultado<SesionEmitida>.Fallo(ErrorCredenciales);
        }

        var ahora = reloj.GetUtcNow();
        var hash = CalcularHash(tokenPlano);
        var anterior = await repositorioTokens.BuscarPorHashAsync(hash, cancellationToken);
        if (anterior is null)
        {
            return Resultado<SesionEmitida>.Fallo(ErrorCredenciales);
        }

        if (anterior.EstaRevocado)
        {
            await repositorioTokens.RevocarActivosAsync(anterior.UsuarioId, ahora, cancellationToken);
            return Resultado<SesionEmitida>.Fallo(ErrorCredenciales);
        }

        if (!anterior.EstaVigente(ahora))
        {
            anterior.Revocar(ahora);
            await repositorioTokens.GuardarAsync(anterior, cancellationToken);
            return Resultado<SesionEmitida>.Fallo(ErrorCredenciales);
        }

        var usuario = await identidad.ObtenerSesionAsync(anterior.UsuarioId, cancellationToken);
        if (usuario is null || usuario.SelloSeguridad != anterior.SelloSeguridad)
        {
            await repositorioTokens.RevocarActivosAsync(anterior.UsuarioId, ahora, cancellationToken);
            return Resultado<SesionEmitida>.Fallo(ErrorCredenciales);
        }

        var nuevoTokenPlano = GenerarTokenRenovacion();
        var nuevoHash = CalcularHash(nuevoTokenPlano);
        var nuevo = CrearTokenPersistible(usuario, nuevoHash, ahora);
        if (!nuevo.EsExitoso)
        {
            return Resultado<SesionEmitida>.Fallo(nuevo.Error!);
        }

        var revocacion = anterior.Revocar(ahora, nuevoHash);
        if (!revocacion.EsExitoso)
        {
            await repositorioTokens.RevocarActivosAsync(anterior.UsuarioId, ahora, cancellationToken);
            return Resultado<SesionEmitida>.Fallo(ErrorCredenciales);
        }

        var acceso = emisorTokenAcceso.Crear(usuario, ahora);
        if (!acceso.EsExitoso)
        {
            return Resultado<SesionEmitida>.Fallo(acceso.Error!);
        }

        var rotacionExitosa = await repositorioTokens.RotarAsync(
            anterior,
            nuevo.Valor!,
            cancellationToken);

        if (!rotacionExitosa)
        {
            await repositorioTokens.RevocarActivosAsync(anterior.UsuarioId, ahora, cancellationToken);
            return Resultado<SesionEmitida>.Fallo(ErrorCredenciales);
        }

        return Resultado<SesionEmitida>.Exito(new SesionEmitida(
            acceso.Valor!,
            nuevoTokenPlano,
            nuevo.Valor!.VenceUtc));
    }

    public async Task CerrarSesionAsync(string? tokenPlano, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tokenPlano))
        {
            return;
        }

        var token = await repositorioTokens.BuscarPorHashAsync(CalcularHash(tokenPlano), cancellationToken);
        if (token is null || token.EstaRevocado)
        {
            return;
        }

        token.Revocar(reloj.GetUtcNow());
        await repositorioTokens.GuardarAsync(token, cancellationToken);
    }

    public async Task<Resultado<SesionEmitida>> CambiarContrasenaAsync(
        Guid usuarioId,
        string? contrasenaActual,
        string? contrasenaNueva,
        CancellationToken cancellationToken)
    {
        if (usuarioId == Guid.Empty || string.IsNullOrEmpty(contrasenaActual) ||
            string.IsNullOrEmpty(contrasenaNueva) || contrasenaNueva.Length > 256)
        {
            return Resultado<SesionEmitida>.Fallo(new ErrorDominio(
                "usuario.solicitud_contrasena_invalida",
                "La solicitud de cambio de contraseña no es válida."));
        }

        var cambio = await identidad.CambiarContrasenaAsync(
            usuarioId, contrasenaActual, contrasenaNueva, cancellationToken);
        if (!cambio.EsExitoso)
        {
            return Resultado<SesionEmitida>.Fallo(cambio.Error!);
        }

        await repositorioTokens.RevocarActivosAsync(
            usuarioId,
            reloj.GetUtcNow(),
            cancellationToken);

        return await CrearSesionAsync(cambio.Valor!, cancellationToken);
    }

    public Task RevocarSesionesUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken) =>
        repositorioTokens.RevocarActivosAsync(usuarioId, reloj.GetUtcNow(), cancellationToken);

    private async Task<Resultado<SesionEmitida>> CrearSesionAsync(
        UsuarioSesion usuario,
        CancellationToken cancellationToken)
    {
        var ahora = reloj.GetUtcNow();
        var acceso = emisorTokenAcceso.Crear(usuario, ahora);
        if (!acceso.EsExitoso)
        {
            return Resultado<SesionEmitida>.Fallo(acceso.Error!);
        }

        var tokenPlano = GenerarTokenRenovacion();
        var token = CrearTokenPersistible(usuario, CalcularHash(tokenPlano), ahora);
        if (!token.EsExitoso)
        {
            return Resultado<SesionEmitida>.Fallo(token.Error!);
        }

        await repositorioTokens.AgregarAsync(token.Valor!, cancellationToken);
        return Resultado<SesionEmitida>.Exito(new SesionEmitida(
            acceso.Valor!,
            tokenPlano,
            token.Valor!.VenceUtc));
    }

    private Resultado<TokenRenovacion> CrearTokenPersistible(
        UsuarioSesion usuario,
        string hash,
        DateTimeOffset ahora) =>
        TokenRenovacion.Crear(
            usuario.Id,
            hash,
            usuario.SelloSeguridad,
            ahora,
            ahora.AddDays(configuracion.DiasTokenRenovacion));

    private static string GenerarTokenRenovacion() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static string CalcularHash(string tokenPlano) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(tokenPlano)));
}
