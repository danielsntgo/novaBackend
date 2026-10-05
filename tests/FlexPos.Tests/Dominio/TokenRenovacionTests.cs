using FlexPos.Domain.Entities;
using Xunit;

namespace FlexPos.Tests.Dominio;

public sealed class TokenRenovacionTests
{
    [Fact]
    public void Revocar_RegistraFechaYHashReemplazante()
    {
        var ahora = DateTimeOffset.UtcNow;
        var token = TokenRenovacion.Crear(
            Guid.CreateVersion7(),
            "hash-anterior",
            "sello",
            ahora,
            ahora.AddDays(7)).Valor!;

        var resultado = token.Revocar(ahora.AddMinutes(1), "hash-nuevo");

        Assert.True(resultado.EsExitoso);
        Assert.True(token.EstaRevocado);
        Assert.Equal("hash-nuevo", token.ReemplazadoPorHash);
        Assert.False(token.EstaVigente(ahora.AddMinutes(2)));
    }

    [Fact]
    public void Revocar_DosVecesDevuelveErrorDeDominio()
    {
        var ahora = DateTimeOffset.UtcNow;
        var token = TokenRenovacion.Crear(
            Guid.CreateVersion7(),
            "hash",
            "sello",
            ahora,
            ahora.AddDays(1)).Valor!;
        token.Revocar(ahora);

        var segundoIntento = token.Revocar(ahora.AddSeconds(1));

        Assert.False(segundoIntento.EsExitoso);
        Assert.Equal("token_renovacion.ya_revocado", segundoIntento.Error!.Codigo);
    }
}
