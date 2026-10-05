using FlexPos.Application.Interfaces;
using FlexPos.Domain.Entities;
using FlexPos.Domain.Enums;
using FlexPos.Domain.Errors;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FlexPos.Infrastructure.Persistence.Repositories;

public sealed class RepositorioCitas(FlexPosDbContext contexto) : IRepositorioCitas
{
    public Task<Cita?> ObtenerAsync(Guid id, CancellationToken cancellationToken) =>
        contexto.Citas
            .Include(x => x.Cliente)
            .Include(x => x.Servicio)
            .Include(x => x.Empleado)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Cita> Elementos, int Total)> ListarAsync(
        DateTimeOffset? desdeUtc,
        DateTimeOffset? hastaUtc,
        Guid? empleadoId,
        Guid? clienteId,
        EstadoCita? estado,
        int omitir,
        int tomar,
        CancellationToken cancellationToken)
    {
        var consulta = ConsultaConReferencias();
        if (desdeUtc.HasValue)
        {
            consulta = consulta.Where(x => x.FinUtc > desdeUtc.Value);
        }

        if (hastaUtc.HasValue)
        {
            consulta = consulta.Where(x => x.InicioUtc < hastaUtc.Value);
        }

        if (empleadoId.HasValue)
        {
            consulta = consulta.Where(x => x.EmpleadoId == empleadoId.Value);
        }

        if (clienteId.HasValue)
        {
            consulta = consulta.Where(x => x.ClienteId == clienteId.Value);
        }

        if (estado.HasValue)
        {
            consulta = consulta.Where(x => x.Estado == estado.Value);
        }

        var total = await consulta.CountAsync(cancellationToken);
        var elementos = await consulta
            .OrderBy(x => x.InicioUtc)
            .ThenBy(x => x.Id)
            .Skip(omitir)
            .Take(tomar)
            .ToArrayAsync(cancellationToken);
        return (elementos, total);
    }

    public async Task<IReadOnlyList<Cita>> ListarReservasAsync(
        DateTimeOffset desdeUtc,
        DateTimeOffset hastaUtc,
        CancellationToken cancellationToken) =>
        await contexto.Citas.AsNoTracking()
            .Where(x => x.Estado != EstadoCita.Cancelada &&
                        x.InicioUtc < hastaUtc && x.FinUtc > desdeUtc)
            .OrderBy(x => x.InicioUtc)
            .ToArrayAsync(cancellationToken);

    public async Task<ErrorDominio?> GuardarEnAgendaAsync(
        Cita cita,
        DateTimeOffset inicioLocal,
        CancellationToken cancellationToken)
    {
        await using var transaccion = await contexto.Database.BeginTransactionAsync(cancellationToken);
        await BloqueoAgendaPostgreSql.TomarAsync(contexto.Database, cita.EmpleadoId, cancellationToken);

        var empleado = await contexto.Empleados.AsNoTracking()
            .Include(x => x.HorariosSemanales)
            .SingleOrDefaultAsync(x => x.Id == cita.EmpleadoId, cancellationToken);
        if (empleado is null || !empleado.Activo)
        {
            return new ErrorDominio(
                "cita.referencia_inactiva", "El empleado no existe o está inactivo.");
        }

        if (!cita.EstaDentroDelHorario(inicioLocal, empleado))
        {
            return new ErrorDominio(
                "cita.fuera_horario", "La cita debe quedar completamente dentro del horario semanal del empleado.");
        }

        var existeCruce = await contexto.Citas.AsNoTracking().AnyAsync(x =>
            x.EmpleadoId == cita.EmpleadoId &&
            x.Id != cita.Id &&
            x.Estado != EstadoCita.Cancelada &&
            x.InicioUtc < cita.FinUtc &&
            x.FinUtc > cita.InicioUtc,
            cancellationToken);
        if (existeCruce)
        {
            return new ErrorDominio("cita.solapamiento", "El empleado ya tiene una cita en ese horario.");
        }

        try
        {
            await contexto.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateConcurrencyException)
        {
            contexto.ChangeTracker.Clear();
            return new ErrorDominio("cita.conflicto", "La cita cambió en otra operación.");
        }
        catch (DbUpdateException excepcion) when
            (excepcion.GetBaseException() is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            contexto.ChangeTracker.Clear();
            return new ErrorDominio("cita.conflicto", "La cita cambió en otra operación.");
        }
    }

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
    }

    private IQueryable<Cita> ConsultaConReferencias() => contexto.Citas.AsNoTracking()
        .Include(x => x.Cliente)
        .Include(x => x.Servicio)
        .Include(x => x.Empleado);
}
