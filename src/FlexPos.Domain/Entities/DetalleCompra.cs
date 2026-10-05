using FlexPos.Domain.Abstractions;
using FlexPos.Domain.Errors;
using FlexPos.Domain.ValueObjects;

namespace FlexPos.Domain.Entities;

public sealed class DetalleCompra : EntidadAuditable
{
    private DetalleCompra()
    {
    }

    private DetalleCompra(
        Guid articuloId,
        string nombreArticulo,
        string unidadBase,
        decimal cantidad,
        Dinero costoUnitario,
        Dinero totalLinea)
    {
        ArticuloId = articuloId;
        NombreArticulo = nombreArticulo;
        UnidadBase = unidadBase;
        Cantidad = cantidad;
        CostoUnitario = costoUnitario;
        TotalLinea = totalLinea;
        Vigente = true;
    }

    public Guid CompraId { get; private set; }
    public Compra Compra { get; private set; } = null!;
    public Guid ArticuloId { get; private set; }
    public string NombreArticulo { get; private set; } = string.Empty;
    public string UnidadBase { get; private set; } = string.Empty;
    public decimal Cantidad { get; private set; }
    public Dinero CostoUnitario { get; private set; } = null!;
    public Dinero TotalLinea { get; private set; } = null!;
    public bool Vigente { get; private set; }

    public static Resultado<DetalleCompra> Crear(
        ArticuloInventario articulo,
        decimal cantidad,
        Dinero costoUnitario)
    {
        if (articulo.Id == Guid.Empty || !articulo.EsCantidadValida(cantidad) ||
            costoUnitario.CodigoMoneda != articulo.CostoPromedio.CodigoMoneda || costoUnitario.Importe < 0m)
        {
            return Resultado<DetalleCompra>.Fallo(new ErrorDominio(
                "compra.detalle_invalido", "La cantidad o el costo de la linea de compra no son validos."));
        }

        try
        {
            var total = Dinero.Crear(
                Math.Round(cantidad * costoUnitario.Importe, 2, MidpointRounding.AwayFromZero),
                costoUnitario.CodigoMoneda);
            return total.EsExitoso
                ? Resultado<DetalleCompra>.Exito(new DetalleCompra(
                    articulo.Id, articulo.Nombre, articulo.UnidadBase, cantidad, costoUnitario, total.Valor!))
                : Resultado<DetalleCompra>.Fallo(total.Error!);
        }
        catch (OverflowException)
        {
            return Resultado<DetalleCompra>.Fallo(new ErrorDominio(
                "compra.detalle_invalido", "El total de la linea excede la precision permitida."));
        }
    }

    internal void Desactivar() => Vigente = false;
}
