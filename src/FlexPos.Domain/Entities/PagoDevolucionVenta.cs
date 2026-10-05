using FlexPos.Domain.Errors;

namespace FlexPos.Domain.Entities;

public sealed class PagoDevolucionVenta : FlexPos.Domain.Abstractions.EntidadAuditable
{
    private PagoDevolucionVenta()
    {
    }

    private PagoDevolucionVenta(Guid metodoPagoId, string metodoPagoNombre, bool esEfectivo, decimal importe, string? referencia)
    {
        MetodoPagoId = metodoPagoId;
        MetodoPagoNombre = metodoPagoNombre;
        EsEfectivo = esEfectivo;
        Importe = importe;
        Referencia = string.IsNullOrWhiteSpace(referencia) ? null : referencia.Trim();
    }

    public Guid DevolucionVentaId { get; internal set; }
    public Guid MetodoPagoId { get; private set; }
    public string MetodoPagoNombre { get; private set; } = string.Empty;
    public bool EsEfectivo { get; private set; }
    public decimal Importe { get; private set; }
    public string? Referencia { get; private set; }

    public static Resultado<PagoDevolucionVenta> Crear(
        MetodoPagoConfigurado metodo,
        decimal importe,
        string? referencia)
    {
        if (metodo.Id == Guid.Empty || !metodo.Activo || importe <= 0m ||
            importe > 9_999_999_999_999_999.99m || decimal.Round(importe, 2) != importe ||
            (metodo.RequiereReferencia && string.IsNullOrWhiteSpace(referencia)) ||
            (!string.IsNullOrWhiteSpace(referencia) && referencia.Trim().Length > 120))
        {
            return Resultado<PagoDevolucionVenta>.Fallo(new ErrorDominio(
                "venta.reembolso_invalido", "El método, importe o referencia del reembolso no son válidos."));
        }

        return Resultado<PagoDevolucionVenta>.Exito(new PagoDevolucionVenta(
            metodo.Id, metodo.Nombre, metodo.EsEfectivo, importe, referencia));
    }
}
