using FlexPos.Domain.Abstractions;
using FlexPos.Domain.Enums;
using FlexPos.Domain.Errors;

namespace FlexPos.Domain.Entities;

public sealed class MovimientoCaja : EntidadAuditable
{
    private MovimientoCaja()
    {
    }

    private MovimientoCaja(
        Guid cajaId,
        TipoMovimientoCaja tipo,
        decimal importe,
        string codigoMoneda,
        string? concepto,
        Guid? ventaId,
        Guid? devolucionVentaId,
        Guid? liquidacionComisionId)
    {
        CajaId = cajaId;
        Tipo = tipo;
        Importe = importe;
        CodigoMoneda = codigoMoneda;
        Concepto = Limpiar(concepto);
        VentaId = ventaId;
        DevolucionVentaId = devolucionVentaId;
        LiquidacionComisionId = liquidacionComisionId;
    }

    public Guid CajaId { get; private set; }
    public TipoMovimientoCaja Tipo { get; private set; }
    public decimal Importe { get; private set; }
    public string CodigoMoneda { get; private set; } = string.Empty;
    public string? Concepto { get; private set; }
    public Guid? VentaId { get; private set; }
    public Guid? DevolucionVentaId { get; private set; }
    public Guid? LiquidacionComisionId { get; private set; }

    public static Resultado<MovimientoCaja> Crear(
        Guid cajaId,
        TipoMovimientoCaja tipo,
        decimal importe,
        string? codigoMoneda,
        string? concepto,
        Guid? ventaId = null,
        Guid? devolucionVentaId = null,
        Guid? liquidacionComisionId = null)
    {
        var moneda = codigoMoneda?.Trim().ToUpperInvariant();
        var manual = tipo is TipoMovimientoCaja.IngresoManual or TipoMovimientoCaja.EgresoManual;
        var referenciaAutomaticaValida = tipo switch
        {
            TipoMovimientoCaja.PagoVenta => ventaId.HasValue && ventaId != Guid.Empty &&
                                            !devolucionVentaId.HasValue && !liquidacionComisionId.HasValue,
            TipoMovimientoCaja.PagoComision => liquidacionComisionId.HasValue && liquidacionComisionId != Guid.Empty &&
                                               !ventaId.HasValue && !devolucionVentaId.HasValue,
            TipoMovimientoCaja.Reembolso => devolucionVentaId.HasValue && devolucionVentaId != Guid.Empty &&
                                            !ventaId.HasValue && !liquidacionComisionId.HasValue,
            _ => !ventaId.HasValue && !devolucionVentaId.HasValue && !liquidacionComisionId.HasValue
        };
        if (cajaId == Guid.Empty || !Enum.IsDefined(tipo) || importe <= 0m ||
            importe > 9_999_999_999_999_999.99m || decimal.Round(importe, 2) != importe ||
            moneda is null || moneda.Length != 3 || moneda.Any(x => x is < 'A' or > 'Z') ||
            !referenciaAutomaticaValida ||
            (manual && string.IsNullOrWhiteSpace(concepto)) ||
            (!string.IsNullOrWhiteSpace(concepto) && concepto.Trim().Length > 250))
        {
            return Resultado<MovimientoCaja>.Fallo(new ErrorDominio(
                "caja.movimiento_invalido", "Los datos del movimiento de caja no son válidos."));
        }

        return Resultado<MovimientoCaja>.Exito(new MovimientoCaja(
            cajaId, tipo, importe, moneda, concepto, ventaId, devolucionVentaId, liquidacionComisionId));
    }

    private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
