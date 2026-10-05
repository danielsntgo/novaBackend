using FlexPos.Domain.Abstractions;
using FlexPos.Domain.Errors;

namespace FlexPos.Domain.Entities;

public sealed class DevolucionVenta : EntidadAuditable
{
    private readonly List<DetalleDevolucionVenta> _detalles = [];
    private readonly List<PagoDevolucionVenta> _pagos = [];

    private DevolucionVenta()
    {
    }

    private DevolucionVenta(
        Guid ventaId,
        DateTimeOffset fechaUtc,
        string motivo,
        decimal importeLineas,
        decimal importeReintegrado,
        IReadOnlyCollection<DetalleDevolucionVenta> detalles,
        IReadOnlyCollection<PagoDevolucionVenta> pagos)
    {
        VentaId = ventaId;
        FechaUtc = fechaUtc.ToUniversalTime();
        Motivo = motivo.Trim();
        ImporteLineas = importeLineas;
        ImporteReintegrado = importeReintegrado;
        _detalles.AddRange(detalles);
        _pagos.AddRange(pagos);
        foreach (var detalle in _detalles)
        {
            detalle.DevolucionVentaId = Id;
        }

        foreach (var pago in _pagos)
        {
            pago.DevolucionVentaId = Id;
        }
    }

    public Guid VentaId { get; private set; }
    public DateTimeOffset FechaUtc { get; private set; }
    public string Motivo { get; private set; } = string.Empty;
    public decimal ImporteLineas { get; private set; }
    public decimal ImporteReintegrado { get; private set; }
    public IReadOnlyCollection<DetalleDevolucionVenta> Detalles => _detalles.AsReadOnly();
    public IReadOnlyCollection<PagoDevolucionVenta> Pagos => _pagos.AsReadOnly();

    public static Resultado<DevolucionVenta> Crear(
        Guid ventaId,
        DateTimeOffset fechaUtc,
        string? motivo,
        IReadOnlyCollection<DetalleDevolucionVenta>? detalles,
        IReadOnlyCollection<PagoDevolucionVenta>? pagos)
    {
        if (ventaId == Guid.Empty || fechaUtc == default || string.IsNullOrWhiteSpace(motivo) ||
            motivo.Trim().Length > 250 || detalles is null || detalles.Count == 0 || pagos is null)
        {
            return Resultado<DevolucionVenta>.Fallo(new ErrorDominio(
                "venta.devolucion_invalida", "La devolución requiere un motivo y líneas válidas."));
        }

        try
        {
            return Resultado<DevolucionVenta>.Exito(new DevolucionVenta(
                ventaId, fechaUtc, motivo, detalles.Sum(x => x.Importe), pagos.Sum(x => x.Importe), detalles, pagos));
        }
        catch (OverflowException)
        {
            return Resultado<DevolucionVenta>.Fallo(new ErrorDominio(
                "venta.devolucion_invalida", "El importe de la devolución excede el rango permitido."));
        }
    }
}
