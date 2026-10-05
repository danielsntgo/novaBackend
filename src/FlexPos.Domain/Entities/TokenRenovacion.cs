using FlexPos.Domain.Abstractions;
using FlexPos.Domain.Errors;

namespace FlexPos.Domain.Entities;

public sealed class TokenRenovacion : EntidadAuditable
{
    private TokenRenovacion()
    {
    }

    private TokenRenovacion(
        Guid usuarioId,
        string hash,
        string selloSeguridad,
        DateTimeOffset creadoUtc,
        DateTimeOffset venceUtc)
    {
        UsuarioId = usuarioId;
        Hash = hash;
        SelloSeguridad = selloSeguridad;
        CreadoUtc = creadoUtc.ToUniversalTime();
        VenceUtc = venceUtc.ToUniversalTime();
    }

    public Guid UsuarioId { get; private set; }
    public string Hash { get; private set; } = string.Empty;
    public string SelloSeguridad { get; private set; } = string.Empty;
    public DateTimeOffset CreadoUtc { get; private set; }
    public DateTimeOffset VenceUtc { get; private set; }
    public DateTimeOffset? RevocadoUtc { get; private set; }
    public string? ReemplazadoPorHash { get; private set; }
    public bool EstaRevocado => RevocadoUtc.HasValue;

    public bool EstaVigente(DateTimeOffset ahoraUtc) =>
        !EstaRevocado && VenceUtc > ahoraUtc.ToUniversalTime();

    public static Resultado<TokenRenovacion> Crear(
        Guid usuarioId,
        string hash,
        string selloSeguridad,
        DateTimeOffset creadoUtc,
        DateTimeOffset venceUtc)
    {
        if (usuarioId == Guid.Empty || string.IsNullOrWhiteSpace(hash) ||
            string.IsNullOrWhiteSpace(selloSeguridad) || venceUtc <= creadoUtc)
        {
            return Resultado<TokenRenovacion>.Fallo(new ErrorDominio(
                "token_renovacion.datos_invalidos",
                "Los datos del token de renovación no son válidos."));
        }

        return Resultado<TokenRenovacion>.Exito(
            new TokenRenovacion(usuarioId, hash, selloSeguridad, creadoUtc, venceUtc));
    }

    public Resultado<bool> Revocar(DateTimeOffset fechaUtc, string? reemplazadoPorHash = null)
    {
        if (EstaRevocado)
        {
            return Resultado<bool>.Fallo(new ErrorDominio(
                "token_renovacion.ya_revocado",
                "El token ya fue revocado."));
        }

        RevocadoUtc = fechaUtc.ToUniversalTime();
        ReemplazadoPorHash = reemplazadoPorHash;
        return Resultado<bool>.Exito(true);
    }
}
