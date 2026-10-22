using FlexPos.Domain.Abstractions;
using FlexPos.Domain.Errors;

namespace FlexPos.Domain.Entities;

public sealed class DevolucionCompra : EntidadAuditable
{
    private readonly List<DetalleDevolucionCompra> _detalles = [];
    private readonly List<PagoDevolucionCompra> _pagos = [];

    private DevolucionCompra()
    {
    }

    private DevolucionCompra(
        Guid compraId,
        DateTimeOffset fechaUtc,
        string motivo,
        IReadOnlyCollection<DetalleDevolucionCompra> detalles,
        IReadOnlyCollection<PagoDevolucionCompra> pagos)
    {
        CompraId = compraId;
        FechaUtc = fechaUtc.ToUniversalTime();
        Motivo = motivo.Trim();
        ImporteLineas = detalles.Sum(x => x.Importe);
        ImporteReintegrado = pagos.Sum(x => x.Importe);
        _detalles.AddRange(detalles);
        _pagos.AddRange(pagos);
        foreach (var detalle in _detalles) detalle.DevolucionCompraId = Id;
        foreach (var pago in _pagos) pago.DevolucionCompraId = Id;
    }

    public Guid CompraId { get; private set; }
    public DateTimeOffset FechaUtc { get; private set; }
    public string Motivo { get; private set; } = string.Empty;
    public decimal ImporteLineas { get; private set; }
    public decimal ImporteReintegrado { get; private set; }
    public IReadOnlyCollection<DetalleDevolucionCompra> Detalles => _detalles.AsReadOnly();
    public IReadOnlyCollection<PagoDevolucionCompra> Pagos => _pagos.AsReadOnly();

    public static Resultado<DevolucionCompra> Crear(
        Guid compraId,
        DateTimeOffset fechaUtc,
        string? motivo,
        IReadOnlyCollection<DetalleDevolucionCompra>? detalles,
        IReadOnlyCollection<PagoDevolucionCompra>? pagos)
    {
        if (compraId == Guid.Empty || fechaUtc == default || string.IsNullOrWhiteSpace(motivo) ||
            motivo.Trim().Length > 250 || detalles is null || detalles.Count == 0 || pagos is null)
        {
            return Resultado<DevolucionCompra>.Fallo(new ErrorDominio(
                "compra.devolucion_invalida", "La devolución requiere un motivo y líneas válidas."));
        }

        try
        {
            var importe = detalles.Sum(x => x.Importe);
            if (importe < 0m || pagos.Sum(x => x.Importe) != importe)
            {
                return Resultado<DevolucionCompra>.Fallo(new ErrorDominio(
                    "compra.importe_reintegro_invalido", "El reintegro debe coincidir con el valor de las líneas devueltas."));
            }

            return Resultado<DevolucionCompra>.Exito(new DevolucionCompra(
                compraId, fechaUtc, motivo, detalles, pagos));
        }
        catch (OverflowException)
        {
            return Resultado<DevolucionCompra>.Fallo(new ErrorDominio(
                "compra.devolucion_invalida", "El importe de la devolución excede el rango permitido."));
        }
    }
}
