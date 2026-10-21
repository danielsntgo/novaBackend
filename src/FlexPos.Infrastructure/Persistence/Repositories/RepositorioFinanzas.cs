using FlexPos.Application.Interfaces;
using FlexPos.Application.DTOs.Finanzas;
using FlexPos.Domain.Entities;
using FlexPos.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace FlexPos.Infrastructure.Persistence.Repositories;

public sealed class RepositorioFinanzas(FlexPosDbContext contexto) : IRepositorioFinanzas
{
    public Task<ConfiguracionNegocio?> ObtenerConfiguracionPrincipalAsync(CancellationToken cancellationToken) =>
        contexto.ConfiguracionesNegocio.SingleOrDefaultAsync(x => x.EsPrincipal, cancellationToken);

    public Task<CuentaFinanciera?> ObtenerCuentaAsync(
        Guid configuracionId,
        Guid cuentaId,
        CancellationToken cancellationToken) =>
        contexto.CuentasFinancieras
            .SingleOrDefaultAsync(x => x.ConfiguracionNegocioId == configuracionId && x.Id == cuentaId, cancellationToken);

    public Task<CuentaFinanciera?> ObtenerCuentaBaseAsync(
        Guid configuracionId,
        TipoCuentaFinanciera tipo,
        string nombre,
        CancellationToken cancellationToken) =>
        contexto.CuentasFinancieras.SingleOrDefaultAsync(
            x => x.ConfiguracionNegocioId == configuracionId && x.Tipo == tipo && x.Nombre == nombre,
            cancellationToken);

    public Task<MetodoPagoConfigurado?> ObtenerMetodoPagoAsync(
        Guid configuracionId,
        Guid metodoPagoId,
        CancellationToken cancellationToken) =>
        contexto.MetodosPagoConfigurados
            .AsNoTracking()
            .Include(x => x.CuentaFinanciera)
            .SingleOrDefaultAsync(x => x.ConfiguracionNegocioId == configuracionId && x.Id == metodoPagoId,
                cancellationToken);

    public async Task<(IReadOnlyList<CuentaFinanciera> Elementos, int Total)> ListarCuentasAsync(
        Guid configuracionId,
        TipoCuentaFinanciera? tipo,
        bool? activa,
        string? buscar,
        int omitir,
        int tomar,
        CancellationToken cancellationToken)
    {
        var consulta = contexto.CuentasFinancieras.AsNoTracking()
            .Where(x => x.ConfiguracionNegocioId == configuracionId);
        if (tipo.HasValue)
        {
            consulta = consulta.Where(x => x.Tipo == tipo.Value);
        }

        if (activa.HasValue)
        {
            consulta = consulta.Where(x => x.Activa == activa.Value);
        }

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            consulta = consulta.Where(x => x.NombreNormalizado.Contains(buscar));
        }

        var total = await consulta.CountAsync(cancellationToken);
        var elementos = await consulta.OrderBy(x => x.NombreNormalizado).ThenBy(x => x.Id)
            .Skip(omitir).Take(tomar).ToArrayAsync(cancellationToken);
        return (elementos, total);
    }

    public async Task<IReadOnlyList<MovimientoFinanciero>> ListarMovimientosAsync(
        Guid cuentaId,
        DateTimeOffset? desdeUtc,
        DateTimeOffset? hastaUtc,
        string? tipo,
        int omitir,
        int tomar,
        CancellationToken cancellationToken)
    {
        var consulta = ConsultaMovimientos(cuentaId, desdeUtc, hastaUtc, tipo);
        return await consulta.OrderBy(x => x.FechaUtc).ThenBy(x => x.Id)
            .Skip(omitir).Take(tomar).ToArrayAsync(cancellationToken);
    }

    public Task<int> ContarMovimientosAsync(
        Guid cuentaId,
        DateTimeOffset? desdeUtc,
        DateTimeOffset? hastaUtc,
        string? tipo,
        CancellationToken cancellationToken) =>
        ConsultaMovimientos(cuentaId, desdeUtc, hastaUtc, tipo).CountAsync(cancellationToken);

    public async Task<decimal> CalcularSaldoAntesAsync(
        Guid cuentaId,
        DateTimeOffset fechaUtc,
        Guid movimientoId,
        CancellationToken cancellationToken)
    {
        var anteriores = contexto.MovimientosFinancieros.AsNoTracking()
            .Where(x => x.CuentaFinancieraId == cuentaId &&
                (x.FechaUtc < fechaUtc || x.FechaUtc == fechaUtc && x.Id.CompareTo(movimientoId) < 0));
        var valores = await anteriores.Select(x => new { x.Sentido, x.Importe }).ToArrayAsync(cancellationToken);
        return valores.Sum(x => x.Sentido == SentidoMovimientoFinanciero.Entrada ? x.Importe : -x.Importe);
    }

    public async Task<IReadOnlyDictionary<Guid, decimal>> CalcularSaldosAsync(
        IReadOnlyCollection<Guid> cuentaIds,
        CancellationToken cancellationToken)
    {
        if (cuentaIds.Count == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        var agrupados = await contexto.MovimientosFinancieros.AsNoTracking()
            .Where(x => cuentaIds.Contains(x.CuentaFinancieraId))
            .GroupBy(x => new { x.CuentaFinancieraId, x.Sentido })
            .Select(x => new
            {
                x.Key.CuentaFinancieraId,
                x.Key.Sentido,
                Importe = x.Sum(m => m.Importe)
            })
            .ToArrayAsync(cancellationToken);
        return agrupados.GroupBy(x => x.CuentaFinancieraId).ToDictionary(
            x => x.Key,
            x => x.Sum(y => y.Sentido == SentidoMovimientoFinanciero.Entrada ? y.Importe : -y.Importe));
    }

    public async Task<(IReadOnlyList<TransferenciaFinancieraDto> Elementos, int Total)> ListarTransferenciasAsync(
        Guid configuracionId,
        DateTimeOffset? desdeUtc,
        DateTimeOffset? hastaUtc,
        Guid? cuentaId,
        int omitir,
        int tomar,
        CancellationToken cancellationToken)
    {
        var consulta =
            from transferencia in contexto.TransferenciasFinancieras.AsNoTracking()
            join origen in contexto.CuentasFinancieras.AsNoTracking()
                on transferencia.CuentaOrigenId equals origen.Id
            join destino in contexto.CuentasFinancieras.AsNoTracking()
                on transferencia.CuentaDestinoId equals destino.Id
            where origen.ConfiguracionNegocioId == configuracionId && destino.ConfiguracionNegocioId == configuracionId
            select new { Transferencia = transferencia, Origen = origen, Destino = destino };
        if (desdeUtc.HasValue)
        {
            consulta = consulta.Where(x => x.Transferencia.FechaUtc >= desdeUtc.Value);
        }

        if (hastaUtc.HasValue)
        {
            consulta = consulta.Where(x => x.Transferencia.FechaUtc <= hastaUtc.Value);
        }

        if (cuentaId.HasValue)
        {
            consulta = consulta.Where(x => x.Transferencia.CuentaOrigenId == cuentaId.Value ||
                                           x.Transferencia.CuentaDestinoId == cuentaId.Value);
        }

        var total = await consulta.CountAsync(cancellationToken);
        var elementos = await consulta.OrderByDescending(x => x.Transferencia.FechaUtc)
            .ThenByDescending(x => x.Transferencia.Id)
            .Select(x => new TransferenciaFinancieraDto(
                x.Transferencia.Id, x.Transferencia.CuentaOrigenId, x.Origen.Nombre,
                x.Transferencia.CuentaDestinoId, x.Destino.Nombre, x.Transferencia.Importe,
                x.Transferencia.CodigoMoneda, x.Transferencia.Concepto, x.Transferencia.Referencia,
                x.Transferencia.FechaUtc, x.Transferencia.CreadoPorId))
            .Skip(omitir).Take(tomar).ToArrayAsync(cancellationToken);
        return (elementos, total);
    }

    public async Task<IUnidadTrabajoFinanzas> IniciarTransaccionAsync(CancellationToken cancellationToken)
    {
        var transaccion = await contexto.Database.BeginTransactionAsync(cancellationToken);
        return new UnidadTrabajoFinanzas(contexto, transaccion);
    }

    public void Agregar(CuentaFinanciera cuenta) => contexto.CuentasFinancieras.Add(cuenta);

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
        catch (DbUpdateException exception) when
            (exception.GetBaseException() is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            contexto.ChangeTracker.Clear();
            return false;
        }
    }

    private IQueryable<MovimientoFinanciero> ConsultaMovimientos(
        Guid cuentaId,
        DateTimeOffset? desdeUtc,
        DateTimeOffset? hastaUtc,
        string? tipo)
    {
        var consulta = contexto.MovimientosFinancieros.AsNoTracking()
            .Where(x => x.CuentaFinancieraId == cuentaId);
        if (desdeUtc.HasValue)
        {
            consulta = consulta.Where(x => x.FechaUtc >= desdeUtc.Value);
        }

        if (hastaUtc.HasValue)
        {
            consulta = consulta.Where(x => x.FechaUtc <= hastaUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(tipo))
        {
            consulta = consulta.Where(x => x.Tipo == tipo);
        }

        return consulta;
    }

    private sealed class UnidadTrabajoFinanzas(
        FlexPosDbContext db,
        IDbContextTransaction transaccion) : IUnidadTrabajoFinanzas
    {
        public async Task<Caja?> BloquearCajaActualAsync(CancellationToken cancellationToken)
        {
            await BloquearFilasAsync(
                "SELECT id FROM flexpos.cajas WHERE estado = 'Abierta' FOR UPDATE", cancellationToken);
            var caja = await db.Cajas.SingleOrDefaultAsync(x => x.Estado == EstadoCaja.Abierta, cancellationToken);
            if (caja is not null)
            {
                await db.Entry(caja).ReloadAsync(cancellationToken);
            }

            return caja;
        }

        public async Task<CuentaFinanciera?> BloquearCuentaAsync(
            Guid configuracionId,
            Guid cuentaId,
            CancellationToken cancellationToken)
        {
            await BloquearFilasAsync(
                "SELECT id FROM flexpos.cuentas_financieras WHERE configuracion_negocio_id = @configuracionId AND id = @id FOR UPDATE",
                cancellationToken, ("configuracionId", configuracionId), ("id", cuentaId));
            var cuenta = await db.CuentasFinancieras.SingleOrDefaultAsync(
                x => x.ConfiguracionNegocioId == configuracionId && x.Id == cuentaId, cancellationToken);
            if (cuenta is not null)
            {
                await db.Entry(cuenta).ReloadAsync(cancellationToken);
            }

            return cuenta;
        }

        public async Task<CuentaFinanciera?> BloquearCuentaBaseAsync(
            Guid configuracionId,
            TipoCuentaFinanciera tipo,
            string nombre,
            CancellationToken cancellationToken)
        {
            await BloquearFilasAsync(
                "SELECT id FROM flexpos.cuentas_financieras WHERE configuracion_negocio_id = @configuracionId AND tipo = @tipo AND nombre = @nombre FOR UPDATE",
                cancellationToken, ("configuracionId", configuracionId), ("tipo", tipo.ToString()), ("nombre", nombre));
            return await db.CuentasFinancieras.SingleOrDefaultAsync(
                x => x.ConfiguracionNegocioId == configuracionId && x.Tipo == tipo && x.Nombre == nombre,
                cancellationToken);
        }

        public async Task<MetodoPagoConfigurado?> BloquearMetodoPagoAsync(
            Guid configuracionId,
            Guid metodoPagoId,
            CancellationToken cancellationToken)
        {
            await BloquearFilasAsync(
                "SELECT id FROM flexpos.metodos_pago WHERE configuracion_negocio_id = @configuracionId AND id = @id FOR UPDATE",
                cancellationToken, ("configuracionId", configuracionId), ("id", metodoPagoId));
            var metodo = await db.MetodosPagoConfigurados
                .Include(x => x.CuentaFinanciera)
                .SingleOrDefaultAsync(x => x.ConfiguracionNegocioId == configuracionId && x.Id == metodoPagoId,
                    cancellationToken);
            if (metodo is not null)
            {
                await db.Entry(metodo).ReloadAsync(cancellationToken);
                db.Entry(metodo).Reference(x => x.CuentaFinanciera).IsLoaded = false;
                await db.Entry(metodo).Reference(x => x.CuentaFinanciera).LoadAsync(cancellationToken);
            }

            return metodo;
        }

        public Task<bool> TieneMovimientosAsync(Guid cuentaId, CancellationToken cancellationToken) =>
            db.MovimientosFinancieros.AnyAsync(x => x.CuentaFinancieraId == cuentaId, cancellationToken);

        public Task<bool> TieneMetodoPagoActivoAsync(Guid cuentaId, CancellationToken cancellationToken) =>
            db.MetodosPagoConfigurados.AnyAsync(x => x.CuentaFinancieraId == cuentaId && x.Activo, cancellationToken);

        public Task<bool> TieneMetodoPagoAsync(Guid cuentaId, CancellationToken cancellationToken) =>
            db.MetodosPagoConfigurados.AnyAsync(x => x.CuentaFinancieraId == cuentaId, cancellationToken);

        public Task<bool> TieneCajaAbiertaAsync(Guid cuentaId, CancellationToken cancellationToken) =>
            db.Cajas.AnyAsync(x => x.CuentaFinancieraId == cuentaId && x.Estado == EstadoCaja.Abierta, cancellationToken);

        public async Task<decimal> CalcularSaldoAsync(Guid cuentaId, CancellationToken cancellationToken)
        {
            var movimientos = await db.MovimientosFinancieros.AsNoTracking()
                .Where(x => x.CuentaFinancieraId == cuentaId)
                .Select(x => new { x.Sentido, x.Importe }).ToArrayAsync(cancellationToken);
            return movimientos.Sum(x => x.Sentido == SentidoMovimientoFinanciero.Entrada
                ? x.Importe : -x.Importe);
        }

        public void Agregar(CuentaFinanciera cuenta) => db.CuentasFinancieras.Add(cuenta);
        public void Agregar(TransferenciaFinanciera transferencia) => db.TransferenciasFinancieras.Add(transferencia);
        public void Agregar(MovimientoFinanciero movimiento) => db.MovimientosFinancieros.Add(movimiento);

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
