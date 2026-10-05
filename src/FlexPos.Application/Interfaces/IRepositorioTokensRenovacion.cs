using FlexPos.Domain.Entities;

namespace FlexPos.Application.Interfaces;

public interface IRepositorioTokensRenovacion
{
    Task<TokenRenovacion?> BuscarPorHashAsync(string hash, CancellationToken cancellationToken);
    Task AgregarAsync(TokenRenovacion token, CancellationToken cancellationToken);
    Task<bool> RotarAsync(
        TokenRenovacion anterior,
        TokenRenovacion nuevo,
        CancellationToken cancellationToken);
    Task GuardarAsync(TokenRenovacion token, CancellationToken cancellationToken);
    Task RevocarActivosAsync(Guid usuarioId, DateTimeOffset fechaUtc, CancellationToken cancellationToken);
}
