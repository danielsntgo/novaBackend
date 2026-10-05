using FlexPos.Application.Interfaces;
using FlexPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FlexPos.Infrastructure.Persistence.Repositories;

public sealed class RepositorioClientes(FlexPosDbContext contexto) : IRepositorioClientes
{
    public Task<Cliente?> ObtenerAsync(Guid id, CancellationToken cancellationToken) =>
        contexto.Clientes.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Cliente> Elementos, int Total)> ListarAsync(
        string? busquedaNormalizada,
        bool? activo,
        int omitir,
        int tomar,
        CancellationToken cancellationToken)
    {
        var consulta = contexto.Clientes.AsNoTracking();
        if (activo.HasValue)
        {
            consulta = consulta.Where(x => x.Activo == activo.Value);
        }

        if (!string.IsNullOrWhiteSpace(busquedaNormalizada))
        {
            consulta = consulta.Where(x =>
                x.NombreNormalizado.StartsWith(busquedaNormalizada) ||
                x.DocumentoNormalizado != null && x.DocumentoNormalizado.StartsWith(busquedaNormalizada) ||
                x.TelefonoNormalizado != null && x.TelefonoNormalizado.StartsWith(busquedaNormalizada) ||
                x.CorreoNormalizado != null && x.CorreoNormalizado.StartsWith(busquedaNormalizada));
        }

        var total = await consulta.CountAsync(cancellationToken);
        var elementos = await consulta
            .OrderBy(x => x.NombreNormalizado)
            .ThenBy(x => x.Id)
            .Skip(omitir)
            .Take(tomar)
            .ToArrayAsync(cancellationToken);
        return (elementos, total);
    }

    public void Agregar(Cliente cliente) => contexto.Clientes.Add(cliente);

    public async Task<bool> GuardarCambiosAsync(CancellationToken cancellationToken)
    {
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
        catch (DbUpdateException excepcion) when
            (excepcion.GetBaseException() is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            contexto.ChangeTracker.Clear();
            return false;
        }
    }
}
