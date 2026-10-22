using FlexPos.Domain.Errors;

namespace FlexPos.Domain.Entities;

public sealed class DetalleDevolucionCompra
{
    private DetalleDevolucionCompra()
    {
    }

    private DetalleDevolucionCompra(Guid detalleCompraId, decimal cantidad, decimal importe)
    {
        Id = Guid.CreateVersion7();
        DetalleCompraId = detalleCompraId;
        Cantidad = cantidad;
        Importe = importe;
    }

    public Guid Id { get; private set; }
    public Guid DevolucionCompraId { get; internal set; }
    public Guid DetalleCompraId { get; private set; }
    public decimal Cantidad { get; private set; }
    public decimal Importe { get; private set; }

    public static Resultado<DetalleDevolucionCompra> Crear(Guid detalleCompraId, decimal cantidad, decimal importe)
    {
        if (detalleCompraId == Guid.Empty || cantidad <= 0m || decimal.Round(cantidad, 3) != cantidad ||
            importe < 0m || importe > 9_999_999_999_999_999.99m || decimal.Round(importe, 2) != importe)
        {
            return Resultado<DetalleDevolucionCompra>.Fallo(new ErrorDominio(
                "compra.detalle_devolucion_invalido", "La línea de devolución o sus importes no son válidos."));
        }

        return Resultado<DetalleDevolucionCompra>.Exito(new DetalleDevolucionCompra(detalleCompraId, cantidad, importe));
    }
}
