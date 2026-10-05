using FlexPos.Application.Interfaces;
using FlexPos.Domain.Entities;
using FlexPos.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FlexPos.Infrastructure.Persistence.Repositories;

public sealed class RepositorioCompras(FlexPosDbContext contexto) : IRepositorioCompras
{
    public Task<Compra?> ObtenerAsync(Guid id, CancellationToken cancellationToken) =>
        contexto.Compras
            .Include(x => x.Proveedor)
            .Include(x => x.Detalles)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Compra> Elementos, int Total)> ListarAsync(
        string? busquedaNormalizada,
        EstadoCompra? estado,
        DateTimeOffset? desdeUtc,
        DateTimeOffset? hastaUtc,
        int omitir,
        int tomar,
        CancellationToken cancellationToken)
    {
        var consulta = contexto.Compras.AsNoTracking()
            .Include(x => x.Proveedor)
            .Include(x => x.Detalles)
            .AsSplitQuery();
        if (estado.HasValue)
        {
            consulta = consulta.Where(x => x.Estado == estado.Value);
        }

        if (desdeUtc.HasValue)
        {
            consulta = consulta.Where(x => x.FechaCompraUtc >= desdeUtc.Value);
        }

        if (hastaUtc.HasValue)
        {
            consulta = consulta.Where(x => x.FechaCompraUtc <= hastaUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(busquedaNormalizada))
        {
            consulta = consulta.Where(x =>
                x.Proveedor.NombreNormalizado.StartsWith(busquedaNormalizada) ||
                x.ReferenciaNormalizada != null && x.ReferenciaNormalizada.StartsWith(busquedaNormalizada));
        }

        var total = await consulta.CountAsync(cancellationToken);
        var elementos = await consulta.OrderByDescending(x => x.FechaCompraUtc).ThenByDescending(x => x.Id)
            .Skip(omitir).Take(tomar).ToArrayAsync(cancellationToken);
        return (elementos, total);
    }

    public Task<bool> ExisteAlgunaAsync(CancellationToken cancellationToken) =>
        contexto.Compras.AnyAsync(cancellationToken);

    public void Agregar(Compra compra) => contexto.Compras.Add(compra);

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
