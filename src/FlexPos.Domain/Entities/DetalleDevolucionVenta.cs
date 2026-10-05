using FlexPos.Domain.Errors;

namespace FlexPos.Domain.Entities;

public sealed class DetalleDevolucionVenta
{
    private DetalleDevolucionVenta()
    {
    }

    private DetalleDevolucionVenta(Guid detalleVentaId, decimal cantidad, decimal importe)
    {
        Id = Guid.CreateVersion7();
        DetalleVentaId = detalleVentaId;
        Cantidad = cantidad;
        Importe = importe;
    }

    public Guid Id { get; private set; }
    public Guid DevolucionVentaId { get; internal set; }
    public Guid DetalleVentaId { get; private set; }
    public decimal Cantidad { get; private set; }
    public decimal Importe { get; private set; }

    public static Resultado<DetalleDevolucionVenta> Crear(Guid detalleVentaId, decimal cantidad, decimal importe)
    {
        if (detalleVentaId == Guid.Empty || cantidad <= 0m || decimal.Round(cantidad, 3) != cantidad ||
            importe < 0m || importe > 9_999_999_999_999_999.99m || decimal.Round(importe, 2) != importe)
        {
            return Resultado<DetalleDevolucionVenta>.Fallo(new ErrorDominio(
                "venta.detalle_devolucion_invalido", "La línea de devolución o sus importes no son válidos."));
        }

        return Resultado<DetalleDevolucionVenta>.Exito(new DetalleDevolucionVenta(detalleVentaId, cantidad, importe));
    }
}
