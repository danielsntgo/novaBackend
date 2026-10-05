using FlexPos.Application.Interfaces;
using FlexPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlexPos.Infrastructure.Persistence.Repositories;

public sealed class RepositorioTokensRenovacion(FlexPosDbContext contexto)
    : IRepositorioTokensRenovacion
{
    public Task<TokenRenovacion?> BuscarPorHashAsync(
        string hash,
        CancellationToken cancellationToken) =>
        contexto.TokensRenovacion.SingleOrDefaultAsync(x => x.Hash == hash, cancellationToken);

    public async Task AgregarAsync(TokenRenovacion token, CancellationToken cancellationToken)
    {
        await contexto.TokensRenovacion.AddAsync(token, cancellationToken);
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RotarAsync(
        TokenRenovacion anterior,
        TokenRenovacion nuevo,
        CancellationToken cancellationToken)
    {
        await contexto.TokensRenovacion.AddAsync(nuevo, cancellationToken);
        try
        {
            await contexto.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            contexto.ChangeTracker.Clear();
            return false;
        }
    }

    public async Task GuardarAsync(TokenRenovacion token, CancellationToken cancellationToken)
    {
        if (contexto.Entry(token).State == EntityState.Detached)
        {
            contexto.TokensRenovacion.Update(token);
        }

        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task RevocarActivosAsync(
        Guid usuarioId,
        DateTimeOffset fechaUtc,
        CancellationToken cancellationToken)
    {
        var activos = await contexto.TokensRenovacion
            .Where(x => x.UsuarioId == usuarioId && x.RevocadoUtc == null)
            .ToListAsync(cancellationToken);

        if (activos.Count == 0)
        {
            return;
        }

        foreach (var token in activos)
        {
            token.Revocar(fechaUtc);
        }

        await contexto.SaveChangesAsync(cancellationToken);
    }
}
