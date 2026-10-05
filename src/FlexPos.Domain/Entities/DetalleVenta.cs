using FlexPos.Domain.Errors;
using FlexPos.Domain.Enums;

namespace FlexPos.Domain.Entities;

public sealed class DetalleVenta
{
    private DetalleVenta()
    {
    }

    private DetalleVenta(
        Guid? articuloInventarioId,
        Guid? servicioId,
        Guid? empleadoId,
        TipoLineaVenta tipo,
        string? codigo,
        string nombre,
        string unidad,
        decimal cantidad,
        decimal precioUnitario,
        decimal? costoInventarioUnitario,
        TipoDescuento? tipoDescuento,
        decimal valorDescuento,
        string? impuestoNombre,
        decimal? impuestoPorcentaje)
    {
        Id = Guid.CreateVersion7();
        ArticuloInventarioId = articuloInventarioId;
        ServicioId = servicioId;
        EmpleadoId = empleadoId;
        Tipo = tipo;
        Codigo = Limpiar(codigo);
        Nombre = nombre.Trim();
        Unidad = unidad.Trim();
        Cantidad = cantidad;
        PrecioUnitario = precioUnitario;
        CostoInventarioUnitario = costoInventarioUnitario;
        TipoDescuentoLinea = tipoDescuento;
        ValorDescuento = valorDescuento;
        ImporteBruto = Redondear(cantidad * precioUnitario);
        DescuentoImporte = tipoDescuento switch
        {
            TipoDescuento.Porcentaje => Redondear(ImporteBruto * valorDescuento / 100m),
            TipoDescuento.ImporteFijo => valorDescuento,
            _ => 0m
        };
        ImpuestoNombre = Limpiar(impuestoNombre);
        ImpuestoPorcentaje = impuestoPorcentaje;
        Recalcular(0m);
    }

    public Guid Id { get; private set; }
    public Guid VentaId { get; internal set; }
    public Guid? ArticuloInventarioId { get; private set; }
    public Guid? ServicioId { get; private set; }
    public Guid? EmpleadoId { get; private set; }
    public TipoLineaVenta Tipo { get; private set; }
    public string? Codigo { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public string Unidad { get; private set; } = string.Empty;
    public decimal Cantidad { get; private set; }
    public decimal PrecioUnitario { get; private set; }
    public decimal? CostoInventarioUnitario { get; private set; }
    public TipoDescuento? TipoDescuentoLinea { get; private set; }
    public decimal ValorDescuento { get; private set; }
    public decimal ImporteBruto { get; private set; }
    public decimal DescuentoImporte { get; private set; }
    public decimal DescuentoGeneralImporte { get; private set; }
    public string? ImpuestoNombre { get; private set; }
    public decimal? ImpuestoPorcentaje { get; private set; }
    public decimal ImpuestoImporte { get; private set; }
    public decimal TotalImporte { get; private set; }
    public decimal BaseNetaAntesImpuestos => ImporteBruto - DescuentoImporte - DescuentoGeneralImporte;

    public static Resultado<DetalleVenta> Crear(
        TipoLineaVenta tipo,
        Guid? articuloInventarioId,
        Guid? servicioId,
        Guid? empleadoId,
        string? codigo,
        string? nombre,
        string? unidad,
        decimal cantidad,
        decimal precioUnitario,
        decimal? costoInventarioUnitario,
        bool manejaFraccion,
        TipoDescuento? tipoDescuento,
        decimal valorDescuento,
        string? impuestoNombre,
        decimal? impuestoPorcentaje)
    {
        var referenciaValida = tipo switch
        {
            TipoLineaVenta.Producto => articuloInventarioId.HasValue && articuloInventarioId != Guid.Empty && !servicioId.HasValue,
            TipoLineaVenta.Servicio => servicioId.HasValue && servicioId != Guid.Empty && !articuloInventarioId.HasValue,
            _ => false
        };
        var cantidadValida = tipo == TipoLineaVenta.Servicio
            ? cantidad > 0m && decimal.Truncate(cantidad) == cantidad
            : cantidad > 0m && decimal.Round(cantidad, 3) == cantidad &&
              (manejaFraccion || decimal.Truncate(cantidad) == cantidad);

        if (!referenciaValida || !Enum.IsDefined(tipo) ||
            (tipo == TipoLineaVenta.Producto && empleadoId.HasValue) || empleadoId == Guid.Empty ||
            string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 150 ||
            string.IsNullOrWhiteSpace(unidad) || unidad.Trim().Length > 30 ||
            (!string.IsNullOrWhiteSpace(codigo) && codigo.Trim().Length > 50) ||
            !cantidadValida || precioUnitario < 0m || decimal.Round(precioUnitario, 2) != precioUnitario ||
            (tipo == TipoLineaVenta.Producto && (costoInventarioUnitario is null || costoInventarioUnitario < 0m)) ||
            (tipo == TipoLineaVenta.Servicio && costoInventarioUnitario is not null) ||
            (tipoDescuento.HasValue && !Enum.IsDefined(tipoDescuento.Value)) ||
            (!tipoDescuento.HasValue && valorDescuento != 0m) ||
            decimal.Round(valorDescuento, 2) != valorDescuento ||
            (tipoDescuento == TipoDescuento.Porcentaje && valorDescuento is < 0m or > 100m) ||
            (tipoDescuento == TipoDescuento.ImporteFijo && valorDescuento < 0m) ||
            (impuestoPorcentaje.HasValue &&
             (string.IsNullOrWhiteSpace(impuestoNombre) || impuestoPorcentaje is < 0m or > 100m ||
              decimal.Round(impuestoPorcentaje.Value, 2) != impuestoPorcentaje.Value)) ||
            (!impuestoPorcentaje.HasValue && !string.IsNullOrWhiteSpace(impuestoNombre)))
        {
            return Resultado<DetalleVenta>.Fallo(new ErrorDominio(
                "venta.detalle_invalido", "Los datos de una línea de venta no son válidos."));
        }

        try
        {
            var detalle = new DetalleVenta(
                articuloInventarioId, servicioId, empleadoId, tipo, codigo, nombre, unidad,
                cantidad, precioUnitario, costoInventarioUnitario, tipoDescuento, valorDescuento,
                impuestoNombre, impuestoPorcentaje);
            if (detalle.DescuentoImporte > detalle.ImporteBruto)
            {
                return Resultado<DetalleVenta>.Fallo(new ErrorDominio(
                    "venta.descuento_invalido", "El descuento no puede superar el valor de la línea."));
            }

            return Resultado<DetalleVenta>.Exito(detalle);
        }
        catch (OverflowException)
        {
            return Resultado<DetalleVenta>.Fallo(new ErrorDominio(
                "venta.importe_invalido", "El importe de la línea excede el rango permitido."));
        }
    }

    internal decimal BaseAntesDescuentoGeneral => ImporteBruto - DescuentoImporte;

    internal void AplicarDescuentoGeneral(decimal importe)
    {
        DescuentoGeneralImporte = importe;
        Recalcular(importe);
    }

    private void Recalcular(decimal descuentoGeneral)
    {
        var baseGravable = ImporteBruto - DescuentoImporte - descuentoGeneral;
        ImpuestoImporte = ImpuestoPorcentaje.HasValue
            ? Redondear(baseGravable * ImpuestoPorcentaje.Value / 100m)
            : 0m;
        TotalImporte = baseGravable + ImpuestoImporte;
    }

    private static decimal Redondear(decimal importe) => decimal.Round(importe, 2, MidpointRounding.AwayFromZero);
    private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
