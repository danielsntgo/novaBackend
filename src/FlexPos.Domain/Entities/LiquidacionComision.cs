using FlexPos.Domain.Abstractions;
using FlexPos.Domain.Errors;

namespace FlexPos.Domain.Entities;

public sealed class LiquidacionComision : EntidadAuditable
{
    private LiquidacionComision()
    {
    }

    private LiquidacionComision(
        Guid empleadoId,
        Guid metodoPagoId,
        string metodoPagoNombre,
        bool esEfectivo,
        decimal importe,
        string codigoMoneda,
        string? referencia,
        Guid? cajaId,
        DateTimeOffset fechaUtc)
    {
        EmpleadoId = empleadoId;
        MetodoPagoId = metodoPagoId;
        MetodoPagoNombre = metodoPagoNombre;
        EsEfectivo = esEfectivo;
        Importe = importe;
        CodigoMoneda = codigoMoneda;
        Referencia = string.IsNullOrWhiteSpace(referencia) ? null : referencia.Trim();
        CajaId = cajaId;
        FechaUtc = fechaUtc.ToUniversalTime();
    }

    public Guid EmpleadoId { get; private set; }
    public Guid MetodoPagoId { get; private set; }
    public string MetodoPagoNombre { get; private set; } = string.Empty;
    public bool EsEfectivo { get; private set; }
    public decimal Importe { get; private set; }
    public string CodigoMoneda { get; private set; } = string.Empty;
    public string? Referencia { get; private set; }
    public Guid? CajaId { get; private set; }
    public DateTimeOffset FechaUtc { get; private set; }

    public static Resultado<LiquidacionComision> Crear(
        Guid empleadoId,
        MetodoPagoConfigurado metodo,
        decimal importe,
        string codigoMoneda,
        string? referencia,
        Guid? cajaId,
        DateTimeOffset fechaUtc)
    {
        var moneda = codigoMoneda?.Trim().ToUpperInvariant();
        if (empleadoId == Guid.Empty || metodo.Id == Guid.Empty || !metodo.Activo ||
            importe <= 0m || decimal.Round(importe, 2) != importe ||
            moneda is null || moneda.Length != 3 || moneda.Any(x => x is < 'A' or > 'Z') ||
            (metodo.RequiereReferencia && string.IsNullOrWhiteSpace(referencia)) ||
            (!string.IsNullOrWhiteSpace(referencia) && referencia.Trim().Length > 120) ||
            (metodo.EsEfectivo && cajaId is null) || (cajaId.HasValue && cajaId == Guid.Empty) || fechaUtc == default)
        {
            return Resultado<LiquidacionComision>.Fallo(new ErrorDominio(
                "comision.liquidacion_invalida", "Los datos de la liquidación de comisión no son válidos."));
        }

        return Resultado<LiquidacionComision>.Exito(new LiquidacionComision(
            empleadoId, metodo.Id, metodo.Nombre, metodo.EsEfectivo, importe, moneda,
            referencia, cajaId, fechaUtc));
    }
}
