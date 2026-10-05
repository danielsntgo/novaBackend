using FlexPos.Application.Interfaces;
using FlexPos.Domain.Entities;
using FlexPos.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace FlexPos.Infrastructure.Persistence.Repositories;

public sealed class RepositorioPuntoVenta(FlexPosDbContext contexto) : IRepositorioPuntoVenta
{
    public async Task<IUnidadTrabajoPuntoVenta> IniciarTransaccionAsync(CancellationToken cancellationToken)
    {
        var transaccion = await contexto.Database.BeginTransactionAsync(cancellationToken);
        return new UnidadTrabajoPuntoVenta(contexto, transaccion);
    }

    public Task<Caja?> ObtenerCajaActualAsync(CancellationToken cancellationToken) =>
        contexto.Cajas.AsNoTracking().SingleOrDefaultAsync(x => x.Estado == EstadoCaja.Abierta, cancellationToken);

    public Task<Caja?> ObtenerCajaAsync(Guid id, CancellationToken cancellationToken) =>
        contexto.Cajas.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<MovimientoCaja>> ObtenerMovimientosCajaAsync(
        Guid cajaId,
        CancellationToken cancellationToken) =>
        await contexto.MovimientosCaja.AsNoTracking()
            .Where(x => x.CajaId == cajaId)
            .OrderBy(x => x.FechaCreacionUtc)
            .ThenBy(x => x.Id)
            .ToArrayAsync(cancellationToken);

    public async Task<(IReadOnlyList<Caja> Elementos, int Total)> ListarCajasAsync(
        DateTimeOffset? desdeUtc,
        DateTimeOffset? hastaUtc,
        int omitir,
        int tomar,
        CancellationToken cancellationToken)
    {
        var consulta = contexto.Cajas.AsNoTracking();
        if (desdeUtc.HasValue)
        {
            consulta = consulta.Where(x => x.FechaAperturaUtc >= desdeUtc.Value);
        }

        if (hastaUtc.HasValue)
        {
            consulta = consulta.Where(x => x.FechaAperturaUtc <= hastaUtc.Value);
        }

        var total = await consulta.CountAsync(cancellationToken);
        var elementos = await consulta
            .OrderByDescending(x => x.FechaAperturaUtc)
            .ThenByDescending(x => x.Id)
            .Skip(omitir)
            .Take(tomar)
            .ToArrayAsync(cancellationToken);
        return (elementos, total);
    }

    public Task<Venta?> ObtenerVentaAsync(Guid id, CancellationToken cancellationToken) =>
        ConsultaVenta(contexto.Ventas.AsNoTracking())
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Venta> Elementos, int Total)> ListarVentasAsync(
        DateTimeOffset? desdeUtc,
        DateTimeOffset? hastaUtc,
        Guid? clienteId,
        EstadoVenta? estado,
        int omitir,
        int tomar,
        CancellationToken cancellationToken)
    {
        var consulta = contexto.Ventas.AsNoTracking();
        if (desdeUtc.HasValue)
        {
            consulta = consulta.Where(x => x.FechaVentaUtc >= desdeUtc.Value);
        }

        if (hastaUtc.HasValue)
        {
            consulta = consulta.Where(x => x.FechaVentaUtc <= hastaUtc.Value);
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
        var elementos = await ConsultaVenta(consulta)
            .OrderByDescending(x => x.FechaVentaUtc)
            .ThenByDescending(x => x.Id)
            .Skip(omitir)
            .Take(tomar)
            .ToArrayAsync(cancellationToken);
        return (elementos, total);
    }

    private static IQueryable<Venta> ConsultaVenta(IQueryable<Venta> consulta) => consulta
        .Include(x => x.Detalles)
        .Include(x => x.Pagos)
        .Include(x => x.Documentos)
        .Include(x => x.Devoluciones).ThenInclude(x => x.Detalles)
        .Include(x => x.Devoluciones).ThenInclude(x => x.Pagos)
        .AsSplitQuery();

    private sealed class UnidadTrabajoPuntoVenta(
        FlexPosDbContext db,
        IDbContextTransaction transaccion) : IUnidadTrabajoPuntoVenta
    {
        public Task<ConfiguracionNegocio?> ObtenerConfiguracionPrincipalAsync(CancellationToken cancellationToken) =>
            db.ConfiguracionesNegocio.SingleOrDefaultAsync(x => x.EsPrincipal, cancellationToken);

        public Task<Caja?> ObtenerCajaActualAsync(CancellationToken cancellationToken) =>
            db.Cajas.SingleOrDefaultAsync(x => x.Estado == EstadoCaja.Abierta, cancellationToken);

        public async Task<Caja?> BloquearCajaActualAsync(CancellationToken cancellationToken)
        {
            await BloquearFilasAsync(
                "SELECT id FROM flexpos.cajas WHERE estado = 'Abierta' FOR UPDATE",
                cancellationToken);
            return await ObtenerCajaActualAsync(cancellationToken);
        }

        public async Task<Caja?> BloquearCajaAsync(Guid id, CancellationToken cancellationToken)
        {
            await BloquearFilasAsync(
                "SELECT id FROM flexpos.cajas WHERE id = @id FOR UPDATE",
                cancellationToken,
                ("id", id));
            return await db.Cajas.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<decimal> CalcularEfectivoEsperadoAsync(Guid cajaId, CancellationToken cancellationToken)
        {
            var movimientos = await db.MovimientosCaja.AsNoTracking()
                .Where(x => x.CajaId == cajaId)
                .Select(x => new { x.Tipo, x.Importe })
                .ToArrayAsync(cancellationToken);
            var caja = await db.Cajas.AsNoTracking().SingleAsync(x => x.Id == cajaId, cancellationToken);
            var entradas = movimientos
                .Where(x => x.Tipo is TipoMovimientoCaja.IngresoManual or TipoMovimientoCaja.PagoVenta)
                .Sum(x => x.Importe);
            var salidas = movimientos
                .Where(x => x.Tipo is TipoMovimientoCaja.EgresoManual or TipoMovimientoCaja.Reembolso or TipoMovimientoCaja.PagoComision)
                .Sum(x => x.Importe);
            return caja.EfectivoApertura + entradas - salidas;
        }

        public Task<Cliente?> ObtenerClienteAsync(Guid id, CancellationToken cancellationToken) =>
            db.Clientes.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        public async Task<IReadOnlyList<ArticuloInventario>> ObtenerArticulosAsync(
            IReadOnlyCollection<Guid> ids,
            bool bloquear,
            CancellationToken cancellationToken)
        {
            if (ids.Count == 0)
            {
                return [];
            }

            var identificadores = ids.Distinct().Order().ToArray();
            if (bloquear)
            {
                await BloquearFilasAsync(
                    "SELECT id FROM flexpos.articulos_inventario WHERE id = ANY (@ids) ORDER BY id FOR UPDATE",
                    cancellationToken,
                    ("ids", identificadores));
            }

            return await db.ArticulosInventario.Where(x => identificadores.Contains(x.Id))
                .OrderBy(x => x.Id).ToArrayAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Servicio>> ObtenerServiciosAsync(
            IReadOnlyCollection<Guid> ids,
            CancellationToken cancellationToken) =>
            await db.Servicios.Where(x => ids.Contains(x.Id)).ToArrayAsync(cancellationToken);

        public async Task<IReadOnlyList<Empleado>> ObtenerEmpleadosAsync(
            IReadOnlyCollection<Guid> ids,
            CancellationToken cancellationToken) =>
            await db.Empleados.Where(x => ids.Contains(x.Id)).ToArrayAsync(cancellationToken);

        public async Task<IReadOnlyList<ReglaComision>> ObtenerReglasComisionAsync(
            IReadOnlyCollection<Guid> empleadoIds,
            IReadOnlyCollection<Guid> servicioIds,
            CancellationToken cancellationToken) =>
            empleadoIds.Count == 0 || servicioIds.Count == 0
                ? []
                : await db.ReglasComision.Where(x => x.Activa &&
                    empleadoIds.Contains(x.EmpleadoId) && servicioIds.Contains(x.ServicioId))
                    .ToArrayAsync(cancellationToken);

        public async Task<IReadOnlyList<Comision>> ObtenerComisionesVentaAsync(
            IReadOnlyCollection<Guid> detalleVentaIds,
            bool bloquear,
            CancellationToken cancellationToken)
        {
            if (detalleVentaIds.Count == 0)
            {
                return [];
            }

            var identificadores = detalleVentaIds.Distinct().Order().ToArray();
            if (bloquear)
            {
                await BloquearFilasAsync(
                    "SELECT id FROM flexpos.comisiones WHERE detalle_venta_id = ANY (@ids) ORDER BY id FOR UPDATE",
                    cancellationToken,
                    ("ids", identificadores));
            }

            return await db.Comisiones.Where(x => identificadores.Contains(x.DetalleVentaId))
                .OrderBy(x => x.Id).ToArrayAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ImpuestoConfigurado>> ObtenerImpuestosAsync(CancellationToken cancellationToken) =>
            await db.ImpuestosConfigurados.ToArrayAsync(cancellationToken);

        public async Task<IReadOnlyList<MetodoPagoConfigurado>> ObtenerMetodosPagoAsync(CancellationToken cancellationToken) =>
            await db.MetodosPagoConfigurados.ToArrayAsync(cancellationToken);

        public async Task<NumeracionDocumento?> BloquearNumeracionAsync(
            TipoDocumentoVenta tipo,
            CancellationToken cancellationToken)
        {
            var configuracion = await ObtenerConfiguracionPrincipalAsync(cancellationToken);
            if (configuracion is null)
            {
                return null;
            }

            await BloquearFilasAsync(
                "SELECT id FROM flexpos.numeraciones_documento WHERE configuracion_negocio_id = @configuracionId AND tipo_documento = @tipo FOR UPDATE",
                cancellationToken,
                ("configuracionId", configuracion.Id),
                ("tipo", tipo.ToString()));
            return await db.NumeracionesDocumento.SingleOrDefaultAsync(
                x => x.ConfiguracionNegocioId == configuracion.Id && x.TipoDocumento == tipo,
                cancellationToken);
        }

        public async Task<Venta?> BloquearVentaAsync(Guid id, CancellationToken cancellationToken)
        {
            await BloquearFilasAsync(
                "SELECT id FROM flexpos.ventas WHERE id = @id FOR UPDATE",
                cancellationToken,
                ("id", id));
            return await ConsultaVenta(db.Ventas).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public void Agregar(Caja caja) => db.Cajas.Add(caja);
        public void Agregar(Venta venta) => db.Ventas.Add(venta);
        public void Agregar(MovimientoCaja movimiento) => db.MovimientosCaja.Add(movimiento);
        public void Agregar(MovimientoInventario movimiento) => db.MovimientosInventario.Add(movimiento);
        public void Agregar(Comision comision) => db.Comisiones.Add(comision);

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

        public Task ConfirmarAsync(CancellationToken cancellationToken) =>
            transaccion.CommitAsync(cancellationToken);

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
