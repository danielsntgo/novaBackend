using FlexPos.Application.DTOs.Reportes;
using FlexPos.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FlexPos.Infrastructure.Persistence.Repositories;

public sealed partial class RepositorioReportes
{
    private async Task<ReporteDto> CuentasAsync(FiltrosReporte filtros, CancellationToken cancellationToken)
    {
        var configuracionId = await contexto.ConfiguracionesNegocio.AsNoTracking()
            .Where(x => x.EsPrincipal)
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);
        var cuentasConsulta = contexto.CuentasFinancieras.AsNoTracking()
            .Where(x => configuracionId.HasValue && x.ConfiguracionNegocioId == configuracionId.Value);
        if (filtros.CuentaFinancieraId.HasValue)
        {
            cuentasConsulta = cuentasConsulta.Where(x => x.Id == filtros.CuentaFinancieraId.Value);
        }

        var cuentas = await cuentasConsulta
            .OrderBy(x => x.Nombre).ThenBy(x => x.Id)
            .Select(x => new CuentaReporte(x.Id, x.Nombre, x.Tipo.ToString(), x.CodigoMoneda))
            .ToArrayAsync(cancellationToken);
        var cuentaIds = cuentas.Select(x => x.Id).ToArray();

        var movimientosBase = contexto.MovimientosFinancieros.AsNoTracking()
            .Where(x => cuentaIds.Contains(x.CuentaFinancieraId));
        var movimientosPeriodo = EnRango(
            movimientosBase, filtros.DesdeUtc, filtros.HastaUtc, x => x.FechaUtc);

        var total = await movimientosPeriodo.CountAsync(cancellationToken);
        var resumenPeriodo = await movimientosPeriodo
            .GroupBy(x => new { x.CuentaFinancieraId, x.Sentido })
            .Select(x => new
            {
                x.Key.CuentaFinancieraId,
                x.Key.Sentido,
                Importe = x.Sum(m => m.Importe)
            })
            .ToArrayAsync(cancellationToken);

        var saldosIniciales = new Dictionary<Guid, decimal>();
        if (filtros.DesdeUtc.HasValue && cuentaIds.Length > 0)
        {
            var anteriores = await movimientosBase
                .Where(x => x.FechaUtc < filtros.DesdeUtc.Value)
                .GroupBy(x => new { x.CuentaFinancieraId, x.Sentido })
                .Select(x => new
                {
                    x.Key.CuentaFinancieraId,
                    x.Key.Sentido,
                    Importe = x.Sum(m => m.Importe)
                })
                .ToArrayAsync(cancellationToken);
            foreach (var grupos in anteriores.GroupBy(x => x.CuentaFinancieraId))
            {
                saldosIniciales[grupos.Key] = grupos.Sum(x =>
                    x.Sentido == SentidoMovimientoFinanciero.Entrada ? x.Importe : -x.Importe);
            }
        }

        var movimientos = await (from movimiento in movimientosPeriodo
                                 join cuenta in contexto.CuentasFinancieras.AsNoTracking()
                                     on movimiento.CuentaFinancieraId equals cuenta.Id
                                 orderby movimiento.FechaUtc, movimiento.Id
                                 select new MovimientoReporte(
                                     cuenta.Id, cuenta.Nombre, cuenta.Tipo.ToString(),
                                     movimiento.Id, movimiento.FechaUtc, movimiento.Tipo, movimiento.Sentido,
                                     movimiento.Importe, movimiento.CodigoMoneda, movimiento.Concepto,
                                     movimiento.CajaId, movimiento.MetodoPagoId, movimiento.VentaId,
                                     movimiento.DevolucionVentaId, movimiento.CompraId,
                                     movimiento.LiquidacionComisionId, movimiento.TransferenciaFinancieraId,
                                     movimiento.Migrado))
            .Take(filtros.Limite)
            .ToArrayAsync(cancellationToken);

        var saldosPosteriores = new Dictionary<Guid, decimal>(saldosIniciales);
        var filas = movimientos.Select(movimiento =>
        {
            var saldo = saldosPosteriores.GetValueOrDefault(movimiento.CuentaId);
            saldo = movimiento.Sentido == SentidoMovimientoFinanciero.Entrada
                ? checked(saldo + movimiento.Importe)
                : checked(saldo - movimiento.Importe);
            saldosPosteriores[movimiento.CuentaId] = saldo;
            return Fila(
                ("CuentaId", movimiento.CuentaId), ("Cuenta", movimiento.Cuenta),
                ("TipoCuenta", movimiento.TipoCuenta), ("MovimientoId", movimiento.MovimientoId),
                ("FechaUtc", movimiento.FechaUtc), ("Tipo", movimiento.Tipo),
                ("Sentido", movimiento.Sentido.ToString()),
                ("Entrada", movimiento.Sentido == SentidoMovimientoFinanciero.Entrada ? movimiento.Importe : 0m),
                ("Salida", movimiento.Sentido == SentidoMovimientoFinanciero.Salida ? movimiento.Importe : 0m),
                ("Importe", movimiento.Importe), ("SaldoPosterior", saldo),
                ("Moneda", movimiento.CodigoMoneda), ("Concepto", movimiento.Concepto),
                ("CajaId", movimiento.CajaId), ("MetodoPagoId", movimiento.MetodoPagoId),
                ("VentaId", movimiento.VentaId), ("DevolucionVentaId", movimiento.DevolucionVentaId),
                ("CompraId", movimiento.CompraId),
                ("LiquidacionComisionId", movimiento.LiquidacionComisionId),
                ("TransferenciaId", movimiento.TransferenciaFinancieraId),
                ("Migrado", movimiento.Migrado));
        }).ToArray();

        var totales = new List<TotalReporteDto> { new("Movimientos del periodo", total) };
        foreach (var cuenta in cuentas)
        {
            decimal Importe(SentidoMovimientoFinanciero sentido) => resumenPeriodo
                .Where(x => x.CuentaFinancieraId == cuenta.Id && x.Sentido == sentido)
                .Sum(x => x.Importe);
            var saldoInicial = saldosIniciales.GetValueOrDefault(cuenta.Id);
            var entradas = Importe(SentidoMovimientoFinanciero.Entrada);
            var salidas = Importe(SentidoMovimientoFinanciero.Salida);
            totales.Add(new TotalReporteDto($"Saldo inicial - {cuenta.Nombre}", saldoInicial, cuenta.Moneda));
            totales.Add(new TotalReporteDto($"Entradas - {cuenta.Nombre}", entradas, cuenta.Moneda));
            totales.Add(new TotalReporteDto($"Salidas - {cuenta.Nombre}", salidas, cuenta.Moneda));
            totales.Add(new TotalReporteDto($"Saldo final - {cuenta.Nombre}",
                checked(saldoInicial + entradas - salidas), cuenta.Moneda));
        }

        return new ReporteDto(
            "cuentas", "Saldos y movimientos por cuenta", DateTimeOffset.UtcNow,
            filtros.DesdeUtc, filtros.HastaUtc,
            ["CuentaId", "Cuenta", "TipoCuenta", "MovimientoId", "FechaUtc", "Tipo", "Sentido",
             "Entrada", "Salida", "Importe", "SaldoPosterior", "Moneda", "Concepto", "CajaId",
             "MetodoPagoId", "VentaId", "DevolucionVentaId", "CompraId", "LiquidacionComisionId",
             "TransferenciaId", "Migrado"],
            filas, totales, total, filtros.Limite, total > filtros.Limite);
    }

    private sealed record CuentaReporte(Guid Id, string Nombre, string Tipo, string Moneda);

    private sealed record MovimientoReporte(
        Guid CuentaId,
        string Cuenta,
        string TipoCuenta,
        Guid MovimientoId,
        DateTimeOffset FechaUtc,
        string Tipo,
        SentidoMovimientoFinanciero Sentido,
        decimal Importe,
        string CodigoMoneda,
        string? Concepto,
        Guid? CajaId,
        Guid? MetodoPagoId,
        Guid? VentaId,
        Guid? DevolucionVentaId,
        Guid? CompraId,
        Guid? LiquidacionComisionId,
        Guid? TransferenciaFinancieraId,
        bool Migrado);
}
