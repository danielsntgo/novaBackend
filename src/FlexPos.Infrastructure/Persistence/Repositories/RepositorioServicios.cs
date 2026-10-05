using FlexPos.Application.Interfaces;
using FlexPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FlexPos.Infrastructure.Persistence.Repositories;

public sealed class RepositorioServicios(FlexPosDbContext contexto) : IRepositorioServicios
{
    public Task<Servicio?> ObtenerAsync(Guid id, CancellationToken cancellationToken) =>
        contexto.Servicios.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Servicio> Elementos, int Total)> ListarAsync(
        string? busquedaNormalizada,
        bool? activo,
        int omitir,
        int tomar,
        CancellationToken cancellationToken)
    {
        var consulta = contexto.Servicios.AsNoTracking();
        if (activo.HasValue)
        {
            consulta = consulta.Where(x => x.Activo == activo.Value);
        }

        if (!string.IsNullOrWhiteSpace(busquedaNormalizada))
        {
            consulta = consulta.Where(x =>
                x.NombreNormalizado.StartsWith(busquedaNormalizada) ||
                x.CategoriaNormalizada != null && x.CategoriaNormalizada.StartsWith(busquedaNormalizada));
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

    public Task<bool> ExisteAlgunoAsync(CancellationToken cancellationToken) =>
        contexto.Servicios.AnyAsync(cancellationToken);

    public void Agregar(Servicio servicio) => contexto.Servicios.Add(servicio);

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
