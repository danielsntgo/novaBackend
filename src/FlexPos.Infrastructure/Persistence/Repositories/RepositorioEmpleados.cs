using FlexPos.Application.Interfaces;
using FlexPos.Domain.Entities;
using FlexPos.Domain.Errors;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FlexPos.Infrastructure.Persistence.Repositories;

public sealed class RepositorioEmpleados(FlexPosDbContext contexto) : IRepositorioEmpleados
{
    public Task<Empleado?> ObtenerAsync(Guid id, CancellationToken cancellationToken) =>
        contexto.Empleados.Include(x => x.HorariosSemanales)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Empleado>> ListarActivosConHorarioAsync(CancellationToken cancellationToken) =>
        await contexto.Empleados.AsNoTracking()
            .Include(x => x.HorariosSemanales)
            .Where(x => x.Activo && x.HorariosSemanales.Any())
            .OrderBy(x => x.NombreNormalizado)
            .ThenBy(x => x.Id)
            .ToArrayAsync(cancellationToken);

    public async Task<(IReadOnlyList<Empleado> Elementos, int Total)> ListarAsync(
        string? busquedaNormalizada,
        bool? activo,
        int omitir,
        int tomar,
        CancellationToken cancellationToken)
    {
        var consulta = contexto.Empleados.AsNoTracking();
        if (activo.HasValue)
        {
            consulta = consulta.Where(x => x.Activo == activo.Value);
        }

        if (!string.IsNullOrWhiteSpace(busquedaNormalizada))
        {
            consulta = consulta.Where(x =>
                x.NombreNormalizado.StartsWith(busquedaNormalizada) ||
                x.CargoNormalizado.StartsWith(busquedaNormalizada) ||
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

    public void Agregar(Empleado empleado) => contexto.Empleados.Add(empleado);

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

    public async Task<ErrorDominio?> GuardarHorarioConBloqueoAsync(
        Empleado empleado,
        CancellationToken cancellationToken)
    {
        await using var transaccion = await contexto.Database.BeginTransactionAsync(cancellationToken);
        await BloqueoAgendaPostgreSql.TomarAsync(contexto.Database, empleado.Id, cancellationToken);
        try
        {
            await contexto.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateConcurrencyException)
        {
            contexto.ChangeTracker.Clear();
            return new ErrorDominio("empleado.conflicto", "El horario del empleado cambió en otra operación.");
        }
        catch (DbUpdateException excepcion) when
            (excepcion.GetBaseException() is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            contexto.ChangeTracker.Clear();
            return new ErrorDominio("empleado.horario_conflicto", "No se pudo guardar el horario del empleado.");
        }
    }
}
