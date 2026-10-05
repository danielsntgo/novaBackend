using FlexPos.Domain.Abstractions;
using FlexPos.Domain.Enums;
using FlexPos.Domain.Errors;
using FlexPos.Domain.ValueObjects;

namespace FlexPos.Domain.Entities;

public sealed class Comision : EntidadAuditable
{
    private Comision()
    {
    }

    private Comision(
        Guid empleadoId,
        Guid servicioId,
        Guid ventaId,
        Guid detalleVentaId,
        TipoMovimientoComision tipoMovimiento,
        TipoTarifaComision tipoTarifa,
        decimal valorTarifa,
        decimal baseCalculo,
        decimal cantidad,
        decimal importe,
        string codigoMoneda,
        Guid? comisionOriginalId,
        Guid? devolucionVentaId)
    {
        EmpleadoId = empleadoId;
        ServicioId = servicioId;
        VentaId = ventaId;
        DetalleVentaId = detalleVentaId;
        TipoMovimiento = tipoMovimiento;
        TipoTarifa = tipoTarifa;
        ValorTarifa = valorTarifa;
        BaseCalculo = baseCalculo;
        Cantidad = cantidad;
        Importe = importe;
        CodigoMoneda = codigoMoneda;
        ComisionOriginalId = comisionOriginalId;
        DevolucionVentaId = devolucionVentaId;
    }

    public Guid EmpleadoId { get; private set; }
    public Guid ServicioId { get; private set; }
    public Guid VentaId { get; private set; }
    public Guid DetalleVentaId { get; private set; }
    public TipoMovimientoComision TipoMovimiento { get; private set; }
    public TipoTarifaComision TipoTarifa { get; private set; }
    public decimal ValorTarifa { get; private set; }
    public decimal BaseCalculo { get; private set; }
    public decimal Cantidad { get; private set; }
    public decimal Importe { get; private set; }
    public string CodigoMoneda { get; private set; } = string.Empty;
    public EstadoComision Estado { get; private set; } = EstadoComision.Pendiente;
    public Guid? ComisionOriginalId { get; private set; }
    public Comision? ComisionOriginal { get; private set; }
    public Guid? DevolucionVentaId { get; private set; }
    public Guid? LiquidacionComisionId { get; private set; }

    public Resultado<AjusteComisionCalculado> CalcularAjusteDevolucion(
        decimal cantidadSolicitada,
        IReadOnlyCollection<Comision> ajustesPrevios)
    {
        if (TipoMovimiento != TipoMovimientoComision.DevengoServicio || cantidadSolicitada <= 0m ||
            decimal.Round(cantidadSolicitada, 3) != cantidadSolicitada ||
            ajustesPrevios.Any(x => x.TipoMovimiento != TipoMovimientoComision.AjusteDevolucion ||
                                    x.ComisionOriginalId != Id))
        {
            return Resultado<AjusteComisionCalculado>.Fallo(new ErrorDominio(
                "comision.ajuste_invalido", "Los datos para calcular el ajuste de comisión no son válidos."));
        }

        try
        {
            var cantidadAjustada = ajustesPrevios.Sum(x => x.Cantidad);
            var importeAjustado = ajustesPrevios.Sum(x => x.Importe);
            var baseAjustada = ajustesPrevios.Sum(x => x.BaseCalculo);
            var cantidadRestante = Cantidad - cantidadAjustada;
            var importeRestante = Importe - importeAjustado;
            var baseRestante = BaseCalculo - baseAjustada;
            var cantidadAjuste = Math.Min(cantidadSolicitada, cantidadRestante);
            if (cantidadAjuste <= 0m || importeRestante <= 0m)
            {
                return Resultado<AjusteComisionCalculado>.Exito(SinAjuste());
            }

            var revierteTodo = cantidadAjuste >= cantidadRestante;
            var importeAjuste = revierteTodo
                ? importeRestante
                : Math.Min(importeRestante, Redondear(Importe * cantidadAjuste / Cantidad));
            var baseAjuste = revierteTodo
                ? baseRestante
                : Math.Min(baseRestante, Redondear(BaseCalculo * cantidadAjuste / Cantidad));
            return importeAjuste <= 0m
                ? Resultado<AjusteComisionCalculado>.Exito(SinAjuste())
                : Resultado<AjusteComisionCalculado>.Exito(new AjusteComisionCalculado(
                    true, baseAjuste, cantidadAjuste, importeAjuste, revierteTodo));
        }
        catch (OverflowException)
        {
            return Resultado<AjusteComisionCalculado>.Fallo(new ErrorDominio(
                "comision.ajuste_invalido", "El importe del ajuste de comisión excede el rango permitido."));
        }
    }

    public static Resultado<Comision> CrearDevengo(
        Guid empleadoId,
        Guid servicioId,
        Guid ventaId,
        Guid detalleVentaId,
        TipoTarifaComision tipoTarifa,
        decimal valorTarifa,
        decimal baseCalculo,
        decimal cantidad,
        decimal importe,
        string codigoMoneda)
    {
        var error = Validar(empleadoId, servicioId, ventaId, detalleVentaId, tipoTarifa,
            valorTarifa, baseCalculo, cantidad, importe, codigoMoneda);
        return error is null
            ? Resultado<Comision>.Exito(new Comision(
                empleadoId, servicioId, ventaId, detalleVentaId, TipoMovimientoComision.DevengoServicio,
                tipoTarifa, valorTarifa, baseCalculo, cantidad, importe,
                NormalizarMoneda(codigoMoneda), null, null))
            : Resultado<Comision>.Fallo(error);
    }

    public static Resultado<Comision> CrearAjusteDevolucion(
        Comision original,
        Guid devolucionVentaId,
        decimal baseCalculo,
        decimal cantidad,
        decimal importe)
    {
        if (original.TipoMovimiento != TipoMovimientoComision.DevengoServicio ||
            devolucionVentaId == Guid.Empty || baseCalculo < 0m || cantidad <= 0m || importe <= 0m ||
            decimal.Round(baseCalculo, 2) != baseCalculo || decimal.Round(importe, 2) != importe ||
            decimal.Round(cantidad, 3) != cantidad || baseCalculo > original.BaseCalculo || importe > original.Importe)
        {
            return Resultado<Comision>.Fallo(new ErrorDominio(
                "comision.ajuste_invalido", "El ajuste de devolución de comisión no es válido."));
        }

        return Resultado<Comision>.Exito(new Comision(
            original.EmpleadoId, original.ServicioId, original.VentaId, original.DetalleVentaId,
            TipoMovimientoComision.AjusteDevolucion, original.TipoTarifa, original.ValorTarifa,
            baseCalculo, cantidad, importe, original.CodigoMoneda, original.Id, devolucionVentaId));
    }

    public void MarcarPagada(Guid liquidacionId)
    {
        if (liquidacionId == Guid.Empty || Estado != EstadoComision.Pendiente)
        {
            throw new InvalidOperationException("Solo las comisiones pendientes pueden liquidarse.");
        }

        Estado = EstadoComision.Pagada;
        LiquidacionComisionId = liquidacionId;
    }

    public void MarcarRevertida()
    {
        if (Estado == EstadoComision.Pendiente)
        {
            Estado = EstadoComision.Revertida;
        }
    }

    private static ErrorDominio? Validar(
        Guid empleadoId,
        Guid servicioId,
        Guid ventaId,
        Guid detalleVentaId,
        TipoTarifaComision tipoTarifa,
        decimal valorTarifa,
        decimal baseCalculo,
        decimal cantidad,
        decimal importe,
        string codigoMoneda)
    {
        var moneda = codigoMoneda?.Trim().ToUpperInvariant();
        if (empleadoId == Guid.Empty || servicioId == Guid.Empty || ventaId == Guid.Empty ||
            detalleVentaId == Guid.Empty || !Enum.IsDefined(tipoTarifa) || valorTarifa <= 0m ||
            baseCalculo < 0m || cantidad <= 0m || importe <= 0m ||
            decimal.Round(valorTarifa, 2) != valorTarifa || decimal.Round(baseCalculo, 2) != baseCalculo ||
            decimal.Round(cantidad, 3) != cantidad || decimal.Round(importe, 2) != importe ||
            moneda is null || moneda.Length != 3 || moneda.Any(x => x is < 'A' or > 'Z'))
        {
            return new ErrorDominio("comision.devengo_invalido", "Los datos del devengo de comisión no son válidos.");
        }

        return null;
    }

    private static string NormalizarMoneda(string moneda) => moneda.Trim().ToUpperInvariant();
    private static decimal Redondear(decimal importe) => decimal.Round(importe, 2, MidpointRounding.AwayFromZero);
    private static AjusteComisionCalculado SinAjuste() => new(false, 0m, 0m, 0m, false);
}
