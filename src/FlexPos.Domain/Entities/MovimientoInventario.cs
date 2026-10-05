using FlexPos.Domain.Abstractions;
using FlexPos.Domain.Enums;
using FlexPos.Domain.Errors;
using FlexPos.Domain.ValueObjects;

namespace FlexPos.Domain.Entities;

public sealed class MovimientoInventario : EntidadAuditable
{
    private MovimientoInventario()
    {
    }

    private MovimientoInventario(
        Guid articuloId,
        TipoMovimientoInventario tipo,
        decimal cantidad,
        decimal existenciaResultante,
        Dinero costoUnitario,
        string? motivo,
        Guid? compraId,
        Guid? detalleCompraId,
        Guid? ventaId,
        Guid? detalleVentaId)
    {
        ArticuloId = articuloId;
        Tipo = tipo;
        Cantidad = cantidad;
        ExistenciaResultante = existenciaResultante;
        CostoUnitario = costoUnitario;
        Motivo = Limpiar(motivo);
        CompraId = compraId;
        DetalleCompraId = detalleCompraId;
        VentaId = ventaId;
        DetalleVentaId = detalleVentaId;
    }

    public Guid ArticuloId { get; private set; }
    public ArticuloInventario Articulo { get; private set; } = null!;
    public TipoMovimientoInventario Tipo { get; private set; }
    public decimal Cantidad { get; private set; }
    public decimal ExistenciaResultante { get; private set; }
    public Dinero CostoUnitario { get; private set; } = null!;
    public string? Motivo { get; private set; }
    public Guid? CompraId { get; private set; }
    public Compra? Compra { get; private set; }
    public Guid? DetalleCompraId { get; private set; }
    public DetalleCompra? DetalleCompra { get; private set; }
    public Guid? VentaId { get; private set; }
    public Guid? DetalleVentaId { get; private set; }

    public static Resultado<MovimientoInventario> Crear(
        ArticuloInventario articulo,
        TipoMovimientoInventario tipo,
        decimal cantidad,
        decimal existenciaResultante,
        Dinero costoUnitario,
        string? motivo,
        Guid? compraId = null,
        Guid? detalleCompraId = null,
        Guid? ventaId = null,
        Guid? detalleVentaId = null)
    {
        var referenciaCompraInvalida = tipo == TipoMovimientoInventario.EntradaCompra
            ? !compraId.HasValue || !detalleCompraId.HasValue
            : compraId.HasValue || detalleCompraId.HasValue;
        var movimientoVenta = tipo is TipoMovimientoInventario.SalidaVenta or TipoMovimientoInventario.EntradaDevolucion;
        var referenciaVentaInvalida = movimientoVenta
            ? !ventaId.HasValue || !detalleVentaId.HasValue || compraId.HasValue || detalleCompraId.HasValue
            : ventaId.HasValue || detalleVentaId.HasValue;
        if (!Enum.IsDefined(tipo) || !articulo.EsCantidadValida(cantidad) ||
            (existenciaResultante < 0m && tipo != TipoMovimientoInventario.SalidaVenta) ||
            decimal.Round(existenciaResultante, 3) != existenciaResultante ||
            costoUnitario.CodigoMoneda != articulo.CostoPromedio.CodigoMoneda || costoUnitario.Importe < 0m ||
            (!string.IsNullOrWhiteSpace(motivo) && motivo.Trim().Length > 250) ||
            referenciaCompraInvalida || referenciaVentaInvalida)
        {
            return Resultado<MovimientoInventario>.Fallo(new ErrorDominio(
                "inventario.movimiento_invalido", "Los datos del movimiento de inventario son invalidos."));
        }

        return Resultado<MovimientoInventario>.Exito(new MovimientoInventario(
            articulo.Id, tipo, cantidad, existenciaResultante, costoUnitario,
            motivo, compraId, detalleCompraId, ventaId, detalleVentaId));
    }

    private static string? Limpiar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
