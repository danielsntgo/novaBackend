using FlexPos.Application.Interfaces;
using FlexPos.Domain.Entities;
using FlexPos.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace FlexPos.Infrastructure.Persistence.Repositories;

public sealed class RepositorioComisiones(FlexPosDbContext contexto) : IRepositorioComisiones
{
    public async Task<(IReadOnlyList<ReglaComision> Elementos, int Total)> ListarReglasAsync(
        Guid? empleadoId,
        Guid? servicioId,
        int omitir,
        int tomar,
        CancellationToken cancellationToken)
    {
        var consulta = contexto.ReglasComision.AsNoTracking();
        if (empleadoId.HasValue)
        {
            consulta = consulta.Where(x => x.EmpleadoId == empleadoId.Value);
        }

        if (servicioId.HasValue)
        {
            consulta = consulta.Where(x => x.ServicioId == servicioId.Value);
        }

        var total = await consulta.CountAsync(cancellationToken);
        var elementos = await consulta.OrderBy(x => x.EmpleadoId).ThenBy(x => x.ServicioId)
            .Skip(omitir).Take(tomar).ToArrayAsync(cancellationToken);
        return (elementos, total);
    }

    public async Task<(IReadOnlyList<Comision> Elementos, int Total)> ListarComisionesAsync(
        Guid? empleadoId,
        EstadoComision? estado,
        TipoMovimientoComision? tipoMovimiento,
        DateTimeOffset? desdeUtc,
        DateTimeOffset? hastaUtc,
        int omitir,
        int tomar,
        CancellationToken cancellationToken)
    {
        var consulta = contexto.Comisiones.AsNoTracking();
        if (empleadoId.HasValue)
        {
            consulta = consulta.Where(x => x.EmpleadoId == empleadoId.Value);
        }

        if (estado.HasValue)
        {
            consulta = consulta.Where(x => x.Estado == estado.Value);
        }

        if (tipoMovimiento.HasValue)
        {
            consulta = consulta.Where(x => x.TipoMovimiento == tipoMovimiento.Value);
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
        var elementos = await consulta.OrderByDescending(x => x.FechaCreacionUtc).ThenByDescending(x => x.Id)
            .Skip(omitir).Take(tomar).ToArrayAsync(cancellationToken);
        return (elementos, total);
    }

    public async Task<(IReadOnlyList<LiquidacionComision> Elementos, int Total)> ListarLiquidacionesAsync(
        Guid? empleadoId,
        DateTimeOffset? desdeUtc,
        DateTimeOffset? hastaUtc,
        int omitir,
        int tomar,
        CancellationToken cancellationToken)
    {
        var consulta = contexto.LiquidacionesComision.AsNoTracking();
        if (empleadoId.HasValue)
        {
            consulta = consulta.Where(x => x.EmpleadoId == empleadoId.Value);
        }

        if (desdeUtc.HasValue)
        {
            consulta = consulta.Where(x => x.FechaUtc >= desdeUtc.Value);
        }

        if (hastaUtc.HasValue)
        {
            consulta = consulta.Where(x => x.FechaUtc <= hastaUtc.Value);
        }

        var total = await consulta.CountAsync(cancellationToken);
        var elementos = await consulta.OrderByDescending(x => x.FechaUtc).ThenByDescending(x => x.Id)
            .Skip(omitir).Take(tomar).ToArrayAsync(cancellationToken);
        return (elementos, total);
    }

    public Task<LiquidacionComision?> ObtenerLiquidacionAsync(Guid id, CancellationToken cancellationToken) =>
        contexto.LiquidacionesComision.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Comision>> ObtenerMovimientosLiquidacionAsync(
        Guid liquidacionId,
        CancellationToken cancellationToken) =>
        await contexto.Comisiones.AsNoTracking().Where(x => x.LiquidacionComisionId == liquidacionId)
            .OrderBy(x => x.Id).ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<Comision>> ObtenerMovimientosLiquidacionesAsync(
        IReadOnlyCollection<Guid> liquidacionIds,
        CancellationToken cancellationToken) =>
        liquidacionIds.Count == 0
            ? []
            : await contexto.Comisiones.AsNoTracking()
                .Where(x => x.LiquidacionComisionId.HasValue && liquidacionIds.Contains(x.LiquidacionComisionId.Value))
                .OrderBy(x => x.Id).ToArrayAsync(cancellationToken);

    public async Task<IUnidadTrabajoComisiones> IniciarTransaccionAsync(CancellationToken cancellationToken)
    {
        var transaccion = await contexto.Database.BeginTransactionAsync(cancellationToken);
        return new UnidadTrabajoComisiones(contexto, transaccion);
    }

    private sealed class UnidadTrabajoComisiones(
        FlexPosDbContext db,
        IDbContextTransaction transaccion) : IUnidadTrabajoComisiones
    {
        public Task<ConfiguracionNegocio?> ObtenerConfiguracionPrincipalAsync(CancellationToken cancellationToken) =>
            db.ConfiguracionesNegocio.SingleOrDefaultAsync(x => x.EsPrincipal, cancellationToken);

        public Task<Empleado?> ObtenerEmpleadoAsync(Guid id, CancellationToken cancellationToken) =>
            db.Empleados.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        public Task<Servicio?> ObtenerServicioAsync(Guid id, CancellationToken cancellationToken) =>
            db.Servicios.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        public Task<MetodoPagoConfigurado?> ObtenerMetodoPagoAsync(Guid id, CancellationToken cancellationToken) =>
            db.MetodosPagoConfigurados.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        public async Task<Caja?> BloquearCajaActualAsync(CancellationToken cancellationToken)
        {
            await BloquearFilasAsync(
                "SELECT id FROM flexpos.cajas WHERE estado = 'Abierta' FOR UPDATE", cancellationToken);
            return await db.Cajas.SingleOrDefaultAsync(x => x.Estado == EstadoCaja.Abierta, cancellationToken);
        }

        public async Task<ReglaComision?> BloquearReglaAsync(Guid id, CancellationToken cancellationToken)
        {
            await BloquearFilasAsync(
                "SELECT id FROM flexpos.reglas_comision WHERE id = @id FOR UPDATE", cancellationToken,
                ("id", id));
            return await db.ReglasComision.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<ReglaComision?> BloquearReglaEmpleadoServicioAsync(
            Guid empleadoId,
            Guid servicioId,
            CancellationToken cancellationToken)
        {
            await BloquearFilasAsync(
                "SELECT id FROM flexpos.reglas_comision WHERE empleado_id = @empleadoId AND servicio_id = @servicioId FOR UPDATE",
                cancellationToken, ("empleadoId", empleadoId), ("servicioId", servicioId));
            return await db.ReglasComision.SingleOrDefaultAsync(
                x => x.EmpleadoId == empleadoId && x.ServicioId == servicioId, cancellationToken);
        }

        public async Task<IReadOnlyList<Comision>> BloquearComisionesSeleccionadasAsync(
            IReadOnlyCollection<Guid> ids,
            CancellationToken cancellationToken)
        {
            if (ids.Count == 0)
            {
                return [];
            }

            var identificadores = ids.Distinct().Order().ToArray();
            await BloquearFilasAsync(
                "SELECT id FROM flexpos.comisiones WHERE id = ANY (@ids) ORDER BY id FOR UPDATE",
                cancellationToken, ("ids", identificadores));
            return await db.Comisiones.Where(x => identificadores.Contains(x.Id)).OrderBy(x => x.Id)
                .ToArrayAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Comision>> BloquearAjustesPendientesAsync(
            Guid empleadoId,
            CancellationToken cancellationToken)
        {
            await BloquearFilasAsync(
                "SELECT id FROM flexpos.comisiones WHERE empleado_id = @empleadoId AND tipo_movimiento = 'AjusteDevolucion' AND estado = 'Pendiente' ORDER BY id FOR UPDATE",
                cancellationToken, ("empleadoId", empleadoId));
            return await db.Comisiones.Include(x => x.ComisionOriginal)
                .Where(x => x.EmpleadoId == empleadoId &&
                    x.TipoMovimiento == TipoMovimientoComision.AjusteDevolucion && x.Estado == EstadoComision.Pendiente)
                .OrderBy(x => x.Id).ToArrayAsync(cancellationToken);
        }

        public void Agregar(ReglaComision regla) => db.ReglasComision.Add(regla);
        public void Agregar(LiquidacionComision liquidacion) => db.LiquidacionesComision.Add(liquidacion);
        public void Agregar(MovimientoCaja movimiento) => db.MovimientosCaja.Add(movimiento);

        public async Task<bool> GuardarCambiosAsync(CancellationToken cancellationToken)
        {
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                db.ChangeTracker.Clear();
                return false;
            }
            catch (DbUpdateException exception) when
                (exception.GetBaseException() is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                db.ChangeTracker.Clear();
                return false;
            }
        }

        public Task ConfirmarAsync(CancellationToken cancellationToken) => transaccion.CommitAsync(cancellationToken);
        public ValueTask DisposeAsync() => transaccion.DisposeAsync();

        private async Task BloquearFilasAsync(
            string sql,
            CancellationToken cancellationToken,
            params (string Nombre, object Valor)[] parametros)
        {
            await using var comando = db.Database.GetDbConnection().CreateCommand();
            comando.Transaction = transaccion.GetDbTransaction();
            comando.CommandText = sql;
            foreach (var (nombre, valor) in parametros)
            {
                var parametro = comando.CreateParameter();
                parametro.ParameterName = nombre;
                parametro.Value = valor;
                comando.Parameters.Add(parametro);
            }

            await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
            while (await lector.ReadAsync(cancellationToken))
            {
            }
        }
    }
}
