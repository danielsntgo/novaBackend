using FlexPos.Domain.Abstractions;
using FlexPos.Domain.Enums;
using FlexPos.Domain.Errors;
using FlexPos.Domain.ValueObjects;

namespace FlexPos.Domain.Entities;

public sealed class MovimientoFinanciero : EntidadAuditable
{
    private MovimientoFinanciero()
    {
    }

    private MovimientoFinanciero(
        Guid cuentaFinancieraId,
        string tipo,
        SentidoMovimientoFinanciero sentido,
        decimal importe,
        string codigoMoneda,
        string? concepto,
        DateTimeOffset fechaUtc,
        Guid? cajaId,
        Guid? metodoPagoId,
        Guid? ventaId,
        Guid? devolucionVentaId,
        Guid? compraId,
        Guid? liquidacionComisionId,
        Guid? transferenciaFinancieraId,
        bool migrado)
    {
        CuentaFinancieraId = cuentaFinancieraId;
        Tipo = tipo.Trim();
        Sentido = sentido;
        Importe = importe;
        CodigoMoneda = codigoMoneda;
        Concepto = Limpiar(concepto);
        FechaUtc = fechaUtc.ToUniversalTime();
        CajaId = cajaId;
        MetodoPagoId = metodoPagoId;
        VentaId = ventaId;
        DevolucionVentaId = devolucionVentaId;
        CompraId = compraId;
        LiquidacionComisionId = liquidacionComisionId;
        TransferenciaFinancieraId = transferenciaFinancieraId;
        Migrado = migrado;
    }

    public Guid CuentaFinancieraId { get; private set; }
    public string Tipo { get; private set; } = string.Empty;
    public SentidoMovimientoFinanciero Sentido { get; private set; }
    public decimal Importe { get; private set; }
    public string CodigoMoneda { get; private set; } = string.Empty;
    public string? Concepto { get; private set; }
    public DateTimeOffset FechaUtc { get; private set; }
    public Guid? CajaId { get; private set; }
    public Guid? MetodoPagoId { get; private set; }
    public Guid? VentaId { get; private set; }
    public Guid? DevolucionVentaId { get; private set; }
    public Guid? CompraId { get; private set; }
    public Guid? LiquidacionComisionId { get; private set; }
    public Guid? TransferenciaFinancieraId { get; private set; }
    public bool Migrado { get; private set; }

    public static Resultado<MovimientoFinanciero> Crear(
        Guid cuentaFinancieraId,
        string? tipo,
        SentidoMovimientoFinanciero sentido,
        decimal importe,
        string? codigoMoneda,
        DateTimeOffset fechaUtc,
        string? concepto,
        Guid? cajaId = null,
        Guid? metodoPagoId = null,
        Guid? ventaId = null,
        Guid? devolucionVentaId = null,
        Guid? compraId = null,
        Guid? liquidacionComisionId = null,
        Guid? transferenciaFinancieraId = null,
        bool migrado = false)
    {
        var moneda = Dinero.Crear(0m, codigoMoneda);
        if (cuentaFinancieraId == Guid.Empty ||
            string.IsNullOrWhiteSpace(tipo) || tipo.Trim().Length > 50 ||
            !Enum.IsDefined(sentido) ||
            importe <= 0m || importe > 9_999_999_999_999_999.99m ||
            decimal.Round(importe, 2) != importe ||
            !moneda.EsExitoso || fechaUtc == default ||
            !ReferenciaValida(tipo.Trim(), sentido, cajaId, metodoPagoId, ventaId, devolucionVentaId,
                compraId, liquidacionComisionId, transferenciaFinancieraId) ||
            (tipo.Trim() is TipoMovimientoFinanciero.IngresoManual or TipoMovimientoFinanciero.EgresoManual) &&
                string.IsNullOrWhiteSpace(concepto) ||
            (!string.IsNullOrWhiteSpace(concepto) && concepto.Trim().Length > 250))
        {
            return Resultado<MovimientoFinanciero>.Fallo(new ErrorDominio(
                "movimiento_financiero.datos_invalidos", "Los datos del movimiento financiero no son válidos."));
        }

        return Resultado<MovimientoFinanciero>.Exito(new MovimientoFinanciero(
            cuentaFinancieraId, tipo, sentido, importe, moneda.Valor!.CodigoMoneda, concepto, fechaUtc,
            cajaId, metodoPagoId, ventaId, devolucionVentaId, compraId, liquidacionComisionId,
            transferenciaFinancieraId, migrado));
    }

    public decimal Aplicar(decimal saldo) => Sentido == SentidoMovimientoFinanciero.Entrada
        ? checked(saldo + Importe)
        : checked(saldo - Importe);

    private static bool ReferenciaValida(
        string tipo,
        SentidoMovimientoFinanciero sentido,
        Guid? cajaId,
        Guid? metodoPagoId,
        Guid? ventaId,
        Guid? devolucionVentaId,
        Guid? compraId,
        Guid? liquidacionComisionId,
        Guid? transferenciaFinancieraId)
    {
        if (new[] { cajaId, metodoPagoId, ventaId, devolucionVentaId, compraId,
                    liquidacionComisionId, transferenciaFinancieraId }
            .Any(x => x == Guid.Empty))
        {
            return false;
        }

        var tieneSoloMetodo = metodoPagoId.HasValue && !ventaId.HasValue && !devolucionVentaId.HasValue &&
                              !compraId.HasValue && !liquidacionComisionId.HasValue && !transferenciaFinancieraId.HasValue;
        return tipo switch
        {
            TipoMovimientoFinanciero.PagoVenta =>
                sentido == SentidoMovimientoFinanciero.Entrada && metodoPagoId.HasValue && ventaId.HasValue &&
                !devolucionVentaId.HasValue && !compraId.HasValue && !liquidacionComisionId.HasValue &&
                !transferenciaFinancieraId.HasValue,
            TipoMovimientoFinanciero.Reembolso =>
                sentido == SentidoMovimientoFinanciero.Salida && metodoPagoId.HasValue && devolucionVentaId.HasValue &&
                !ventaId.HasValue && !compraId.HasValue && !liquidacionComisionId.HasValue &&
                !transferenciaFinancieraId.HasValue,
            TipoMovimientoFinanciero.PagoCompra =>
                sentido == SentidoMovimientoFinanciero.Salida && tieneSoloMetodo && compraId.HasValue,
            TipoMovimientoFinanciero.PagoComision =>
                sentido == SentidoMovimientoFinanciero.Salida && metodoPagoId.HasValue &&
                liquidacionComisionId.HasValue && !ventaId.HasValue && !devolucionVentaId.HasValue &&
                !compraId.HasValue && !transferenciaFinancieraId.HasValue,
            TipoMovimientoFinanciero.TransferenciaEntrada =>
                sentido == SentidoMovimientoFinanciero.Entrada && transferenciaFinancieraId.HasValue &&
                !metodoPagoId.HasValue && !ventaId.HasValue && !devolucionVentaId.HasValue &&
                !compraId.HasValue && !liquidacionComisionId.HasValue,
            TipoMovimientoFinanciero.TransferenciaSalida =>
                sentido == SentidoMovimientoFinanciero.Salida && transferenciaFinancieraId.HasValue &&
                !metodoPagoId.HasValue && !ventaId.HasValue && !devolucionVentaId.HasValue &&
                !compraId.HasValue && !liquidacionComisionId.HasValue,
            TipoMovimientoFinanciero.IngresoManual =>
                sentido == SentidoMovimientoFinanciero.Entrada &&
                (NoTieneOrigen(cajaId, metodoPagoId, ventaId, devolucionVentaId, compraId,
                     liquidacionComisionId, transferenciaFinancieraId) ||
                 tieneSoloMetodo && !ventaId.HasValue && !devolucionVentaId.HasValue && !compraId.HasValue &&
                 !liquidacionComisionId.HasValue && !transferenciaFinancieraId.HasValue),
            TipoMovimientoFinanciero.EgresoManual =>
                sentido == SentidoMovimientoFinanciero.Salida &&
                (NoTieneOrigen(cajaId, metodoPagoId, ventaId, devolucionVentaId, compraId,
                     liquidacionComisionId, transferenciaFinancieraId) ||
                 tieneSoloMetodo && !ventaId.HasValue && !devolucionVentaId.HasValue && !compraId.HasValue &&
                 !liquidacionComisionId.HasValue && !transferenciaFinancieraId.HasValue),
            TipoMovimientoFinanciero.AjusteApertura or TipoMovimientoFinanciero.AjusteCierre =>
                cajaId.HasValue && !metodoPagoId.HasValue && !ventaId.HasValue && !devolucionVentaId.HasValue &&
                !compraId.HasValue && !liquidacionComisionId.HasValue && !transferenciaFinancieraId.HasValue,
            TipoMovimientoFinanciero.SaldoInicial => !cajaId.HasValue && !metodoPagoId.HasValue &&
                NoTieneOrigen(cajaId, metodoPagoId, ventaId, devolucionVentaId, compraId,
                    liquidacionComisionId, transferenciaFinancieraId),
            _ => true
        };
    }

    private static bool NoTieneOrigen(
        Guid? cajaId,
        Guid? metodoPagoId,
        Guid? ventaId,
        Guid? devolucionVentaId,
        Guid? compraId,
        Guid? liquidacionComisionId,
        Guid? transferenciaFinancieraId) =>
        !metodoPagoId.HasValue && !ventaId.HasValue && !devolucionVentaId.HasValue &&
        !compraId.HasValue && !liquidacionComisionId.HasValue && !transferenciaFinancieraId.HasValue;

    private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
