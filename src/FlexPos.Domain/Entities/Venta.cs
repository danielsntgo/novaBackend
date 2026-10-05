using FlexPos.Domain.Abstractions;
using FlexPos.Domain.Enums;
using FlexPos.Domain.Errors;

namespace FlexPos.Domain.Entities;

public sealed class Venta : EntidadAuditable
{
    private readonly List<DetalleVenta> _detalles = [];
    private readonly List<PagoVenta> _pagos = [];
    private readonly List<DocumentoVenta> _documentos = [];
    private readonly List<DevolucionVenta> _devoluciones = [];

    private Venta()
    {
    }

    private Venta(
        Guid cajaId,
        Guid? clienteId,
        string? clienteNombre,
        string? clienteDocumento,
        DateTimeOffset fechaVentaUtc,
        string codigoMoneda,
        TipoDescuento? tipoDescuentoGeneral,
        decimal valorDescuentoGeneral,
        IReadOnlyCollection<DetalleVenta> detalles)
    {
        CajaId = cajaId;
        ClienteId = clienteId;
        ClienteNombre = Limpiar(clienteNombre);
        ClienteDocumento = Limpiar(clienteDocumento);
        FechaVentaUtc = fechaVentaUtc.ToUniversalTime();
        CodigoMoneda = codigoMoneda;
        TipoDescuentoGeneral = tipoDescuentoGeneral;
        ValorDescuentoGeneral = valorDescuentoGeneral;
        Estado = EstadoVenta.Finalizada;
        _detalles.AddRange(detalles);
        foreach (var detalle in _detalles)
        {
            detalle.VentaId = Id;
        }

        Subtotal = _detalles.Sum(x => x.ImporteBruto);
        DescuentoLineas = _detalles.Sum(x => x.DescuentoImporte);
        var baseGeneral = _detalles.Sum(x => x.BaseAntesDescuentoGeneral);
        DescuentoGeneral = tipoDescuentoGeneral switch
        {
            TipoDescuento.Porcentaje => Redondear(baseGeneral * valorDescuentoGeneral / 100m),
            TipoDescuento.ImporteFijo => valorDescuentoGeneral,
            _ => 0m
        };

        decimal asignado = 0m;
        for (var indice = 0; indice < _detalles.Count; indice++)
        {
            var detalle = _detalles.ElementAt(indice);
            var descuento = indice == _detalles.Count - 1
                ? DescuentoGeneral - asignado
                : baseGeneral == 0m
                    ? 0m
                    : Redondear(DescuentoGeneral * detalle.BaseAntesDescuentoGeneral / baseGeneral);
            asignado += descuento;
            detalle.AplicarDescuentoGeneral(descuento);
        }

        Impuestos = _detalles.Sum(x => x.ImpuestoImporte);
        Total = _detalles.Sum(x => x.TotalImporte);
        TotalPendiente = Total;
    }

    public Guid CajaId { get; private set; }
    public Guid? ClienteId { get; private set; }
    public string? ClienteNombre { get; private set; }
    public string? ClienteDocumento { get; private set; }
    public DateTimeOffset FechaVentaUtc { get; private set; }
    public string CodigoMoneda { get; private set; } = string.Empty;
    public EstadoVenta Estado { get; private set; }
    public decimal Subtotal { get; private set; }
    public TipoDescuento? TipoDescuentoGeneral { get; private set; }
    public decimal ValorDescuentoGeneral { get; private set; }
    public decimal DescuentoLineas { get; private set; }
    public decimal DescuentoGeneral { get; private set; }
    public decimal Impuestos { get; private set; }
    public decimal Total { get; private set; }
    public decimal TotalPagado { get; private set; }
    public decimal TotalDevuelto { get; private set; }
    public decimal TotalReintegrado { get; private set; }
    public decimal TotalPendiente { get; private set; }
    public string? MotivoAnulacion { get; private set; }
    public Guid? AnuladaPorId { get; private set; }
    public DateTimeOffset? FechaAnulacionUtc { get; private set; }
    public IReadOnlyCollection<DetalleVenta> Detalles => _detalles.AsReadOnly();
    public IReadOnlyCollection<PagoVenta> Pagos => _pagos.AsReadOnly();
    public IReadOnlyCollection<DocumentoVenta> Documentos => _documentos.AsReadOnly();
    public IReadOnlyCollection<DevolucionVenta> Devoluciones => _devoluciones.AsReadOnly();

    public static Resultado<Venta> Crear(
        Guid cajaId,
        Guid? clienteId,
        string? clienteNombre,
        string? clienteDocumento,
        DateTimeOffset fechaVentaUtc,
        string? codigoMoneda,
        TipoDescuento? tipoDescuentoGeneral,
        decimal valorDescuentoGeneral,
        IReadOnlyCollection<DetalleVenta>? detalles)
    {
        var moneda = codigoMoneda?.Trim().ToUpperInvariant();
        if (cajaId == Guid.Empty || clienteId == Guid.Empty || fechaVentaUtc == default ||
            (!clienteId.HasValue && (clienteNombre is not null || clienteDocumento is not null)) ||
            (!string.IsNullOrWhiteSpace(clienteNombre) && clienteNombre.Trim().Length > 150) ||
            (!string.IsNullOrWhiteSpace(clienteDocumento) && clienteDocumento.Trim().Length > 50) ||
            moneda is null || moneda.Length != 3 || moneda.Any(x => x is < 'A' or > 'Z') ||
            detalles is null || detalles.Count == 0 || detalles.Count > 200 ||
            (tipoDescuentoGeneral.HasValue && !Enum.IsDefined(tipoDescuentoGeneral.Value)) ||
            (!tipoDescuentoGeneral.HasValue && valorDescuentoGeneral != 0m) ||
            decimal.Round(valorDescuentoGeneral, 2) != valorDescuentoGeneral ||
            (tipoDescuentoGeneral == TipoDescuento.Porcentaje && valorDescuentoGeneral is < 0m or > 100m) ||
            (tipoDescuentoGeneral == TipoDescuento.ImporteFijo && valorDescuentoGeneral < 0m))
        {
            return Resultado<Venta>.Fallo(new ErrorDominio(
                "venta.solicitud_invalida", "Los datos de la venta no son válidos."));
        }

        try
        {
            var venta = new Venta(
                cajaId, clienteId, clienteNombre, clienteDocumento, fechaVentaUtc, moneda,
                tipoDescuentoGeneral, valorDescuentoGeneral, detalles);
            var baseGeneral = venta._detalles.Sum(x => x.BaseAntesDescuentoGeneral);
            if (venta.DescuentoGeneral > baseGeneral || venta.Total > 9_999_999_999_999_999.99m)
            {
                return Resultado<Venta>.Fallo(new ErrorDominio(
                    "venta.descuento_invalido", "El descuento general no puede superar el subtotal disponible."));
            }

            return Resultado<Venta>.Exito(venta);
        }
        catch (OverflowException)
        {
            return Resultado<Venta>.Fallo(new ErrorDominio(
                "venta.importe_invalido", "El total de la venta excede el rango permitido."));
        }
    }

    public Resultado<bool> RegistrarPago(PagoVenta pago)
    {
        if (Estado is EstadoVenta.Anulada or EstadoVenta.Devuelta || pago.VentaId != Id ||
            pago.Importe <= 0m || pago.Importe > TotalPendiente)
        {
            return Resultado<bool>.Fallo(new ErrorDominio(
                "venta.pago_invalido", "El pago no corresponde a la venta o supera el saldo pendiente."));
        }

        _pagos.Add(pago);
        TotalPagado = Redondear(TotalPagado + pago.Importe);
        ActualizarSaldo();
        return Resultado<bool>.Exito(true);
    }

    public Resultado<bool> RegistrarDocumento(DocumentoVenta documento)
    {
        if (documento.VentaId != Id || _documentos.Any(x => x.TipoDocumento == documento.TipoDocumento))
        {
            return Resultado<bool>.Fallo(new ErrorDominio(
                "venta.documento_duplicado", "La venta ya tiene un documento de ese tipo."));
        }

        _documentos.Add(documento);
        return Resultado<bool>.Exito(true);
    }

    public Resultado<bool> RegistrarDevolucion(DevolucionVenta devolucion, bool anular, Guid usuarioId, DateTimeOffset fechaUtc)
    {
        var montoDevuelto = devolucion.ImporteLineas;
        var totalDevueltoNuevo = TotalDevuelto + montoDevuelto;
        var totalPendienteNuevo = Math.Max(0m, Total - totalDevueltoNuevo - TotalPagado + TotalReintegrado);
        var reembolsoEsperado = Math.Max(0m, TotalPagado - TotalReintegrado - (Total - totalDevueltoNuevo));
        if (Estado is EstadoVenta.Anulada or EstadoVenta.Devuelta || devolucion.VentaId != Id ||
            usuarioId == Guid.Empty || fechaUtc == default ||
            montoDevuelto < 0m || totalDevueltoNuevo > Total || devolucion.ImporteReintegrado != reembolsoEsperado ||
            anular && string.IsNullOrWhiteSpace(devolucion.Motivo))
        {
            return Resultado<bool>.Fallo(new ErrorDominio(
                "venta.devolucion_invalida", "La devolución no es válida para el estado y los pagos de la venta."));
        }

        _devoluciones.Add(devolucion);
        TotalDevuelto = totalDevueltoNuevo;
        TotalReintegrado += devolucion.ImporteReintegrado;
        TotalPendiente = totalPendienteNuevo;
        var cantidadesDevueltas = _devoluciones
            .SelectMany(x => x.Detalles)
            .GroupBy(x => x.DetalleVentaId)
            .ToDictionary(x => x.Key, x => x.Sum(detalle => detalle.Cantidad));
        var todasLasUnidadesDevueltas = _detalles.All(detalle =>
            cantidadesDevueltas.GetValueOrDefault(detalle.Id) >= detalle.Cantidad);
        Estado = anular
            ? EstadoVenta.Anulada
            : todasLasUnidadesDevueltas ? EstadoVenta.Devuelta : EstadoVenta.ParcialmenteDevuelta;
        if (anular)
        {
            MotivoAnulacion = devolucion.Motivo.Trim();
            AnuladaPorId = usuarioId;
            FechaAnulacionUtc = fechaUtc.ToUniversalTime();
        }

        return Resultado<bool>.Exito(true);
    }

    private void ActualizarSaldo() =>
        TotalPendiente = Math.Max(0m, Total - TotalDevuelto - TotalPagado + TotalReintegrado);

    private static decimal Redondear(decimal importe) => decimal.Round(importe, 2, MidpointRounding.AwayFromZero);
    private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
