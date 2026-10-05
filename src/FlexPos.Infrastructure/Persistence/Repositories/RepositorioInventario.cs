using FlexPos.Application.Interfaces;
using FlexPos.Domain.Entities;
using FlexPos.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FlexPos.Infrastructure.Persistence.Repositories;

public sealed class RepositorioInventario(FlexPosDbContext contexto) : IRepositorioInventario
{
    public Task<ArticuloInventario?> ObtenerArticuloAsync(Guid id, CancellationToken cancellationToken) =>
        contexto.ArticulosInventario.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ArticuloInventario>> ObtenerArticulosAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken) =>
        await contexto.ArticulosInventario.Where(x => ids.Contains(x.Id)).ToArrayAsync(cancellationToken);

    public async Task<(IReadOnlyList<ArticuloInventario> Elementos, int Total)> ListarArticulosAsync(
        string? busquedaNormalizada,
        TipoArticuloInventario? tipo,
        bool? activo,
        bool soloBajoMinimo,
        int omitir,
        int tomar,
        CancellationToken cancellationToken)
    {
        var consulta = contexto.ArticulosInventario.AsNoTracking();
        if (tipo.HasValue)
        {
            consulta = consulta.Where(x => x.Tipo == tipo.Value);
        }

        if (activo.HasValue)
        {
            consulta = consulta.Where(x => x.Activo == activo.Value);
        }

        if (soloBajoMinimo)
        {
            consulta = consulta.Where(x => x.CantidadMinima > 0m && x.ExistenciaActual <= x.CantidadMinima);
        }

        if (!string.IsNullOrWhiteSpace(busquedaNormalizada))
        {
            consulta = consulta.Where(x =>
                x.NombreNormalizado.StartsWith(busquedaNormalizada) ||
                x.CodigoNormalizado != null && x.CodigoNormalizado.StartsWith(busquedaNormalizada) ||
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

    public async Task<bool> TieneMovimientosAsync(Guid articuloId, CancellationToken cancellationToken)
    {
        if (await contexto.MovimientosInventario.AnyAsync(x => x.ArticuloId == articuloId, cancellationToken))
        {
            return true;
        }

        return await contexto.DetallesCompra.AnyAsync(x => x.ArticuloId == articuloId, cancellationToken);
    }

    public async Task<(IReadOnlyList<MovimientoInventario> Elementos, int Total)> ListarMovimientosAsync(
        Guid? articuloId,
        DateTimeOffset? desdeUtc,
        DateTimeOffset? hastaUtc,
        int omitir,
        int tomar,
        CancellationToken cancellationToken)
    {
        var consulta = contexto.MovimientosInventario.AsNoTracking().Include(x => x.Articulo).AsQueryable();
        if (articuloId.HasValue)
        {
            consulta = consulta.Where(x => x.ArticuloId == articuloId.Value);
        }

        if (desdeUtc.HasValue)
        {
            consulta = consulta.Where(x => x.FechaCreacionUtc >= desdeUtc.Value);
        }

        if (hastaUtc.HasValue)
        {
            consulta = consulta.Where(x => x.FechaCreacionUtc <= hastaUtc.Value);
        }

        var total = await consulta.CountAsync(cancellationToken);
        var elementos = await consulta
            .OrderByDescending(x => x.FechaCreacionUtc)
            .ThenByDescending(x => x.Id)
            .Skip(omitir)
            .Take(tomar)
            .ToArrayAsync(cancellationToken);
        return (elementos, total);
    }

    public Task<bool> ExisteAlgunoAsync(CancellationToken cancellationToken) =>
        contexto.ArticulosInventario.AnyAsync(cancellationToken);

    public void AgregarArticulo(ArticuloInventario articulo) => contexto.ArticulosInventario.Add(articulo);

    public void AgregarMovimiento(MovimientoInventario movimiento) => contexto.MovimientosInventario.Add(movimiento);

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
