using FlexPos.Domain.Abstractions;
using FlexPos.Domain.Enums;
using FlexPos.Domain.Errors;
using FlexPos.Domain.ValueObjects;

namespace FlexPos.Domain.Entities;

public sealed class Compra : EntidadAuditable
{
    private readonly List<DetalleCompra> _detalles = [];
    private readonly List<DevolucionCompra> _devoluciones = [];

    private Compra()
    {
    }

    private Compra(
        Guid proveedorId,
        DateTimeOffset fechaCompraUtc,
        string? referencia,
        string? observacion,
        string codigoMoneda)
    {
        ProveedorId = proveedorId;
        FechaCompraUtc = fechaCompraUtc.ToUniversalTime();
        Referencia = Limpiar(referencia);
        ReferenciaNormalizada = TextoNormalizado.Normalizar(Referencia);
        Observacion = Limpiar(observacion);
        CodigoMoneda = codigoMoneda;
        TotalImporte = 0m;
        Estado = EstadoCompra.Borrador;
    }

    public Guid ProveedorId { get; private set; }
    public Proveedor Proveedor { get; private set; } = null!;
    public DateTimeOffset FechaCompraUtc { get; private set; }
    public string? Referencia { get; private set; }
    public string? ReferenciaNormalizada { get; private set; }
    public string? Observacion { get; private set; }
    public string CodigoMoneda { get; private set; } = string.Empty;
    public decimal TotalImporte { get; private set; }
    public decimal TotalDevuelto { get; private set; }
    public decimal SaldoNeto => Math.Max(0m, TotalImporte - TotalDevuelto);
    public EstadoCompra Estado { get; private set; }
    public DateTimeOffset? FechaConfirmacionUtc { get; private set; }
    public string? MotivoAnulacion { get; private set; }
    public DateTimeOffset? FechaAnulacionUtc { get; private set; }
    public IReadOnlyCollection<DetalleCompra> Detalles => _detalles;
    public IReadOnlyCollection<DevolucionCompra> Devoluciones => _devoluciones.AsReadOnly();

    public static Resultado<Compra> Crear(
        Guid proveedorId,
        DateTimeOffset fechaCompraUtc,
        string? referencia,
        string? observacion,
        string? codigoMoneda)
    {
        var error = Validar(proveedorId, fechaCompraUtc, referencia, observacion, codigoMoneda);
        if (error is not null)
        {
            return Resultado<Compra>.Fallo(error);
        }

        var moneda = Dinero.Crear(0m, codigoMoneda);
        return moneda.EsExitoso
            ? Resultado<Compra>.Exito(new Compra(
                proveedorId, fechaCompraUtc, referencia, observacion, moneda.Valor!.CodigoMoneda))
            : Resultado<Compra>.Fallo(moneda.Error!);
    }

    public Resultado<bool> AgregarDetalle(DetalleCompra detalle)
    {
        if (Estado != EstadoCompra.Borrador || detalle.CostoUnitario.CodigoMoneda != CodigoMoneda)
        {
            return Resultado<bool>.Fallo(new ErrorDominio(
                "compra.no_editable", "La compra no admite esta linea o ya fue confirmada."));
        }

        var total = Dinero.Crear(TotalImporte + detalle.TotalLinea.Importe, CodigoMoneda);
        if (!total.EsExitoso)
        {
            return Resultado<bool>.Fallo(total.Error!);
        }

        TotalImporte = total.Valor!.Importe;
        _detalles.Add(detalle);
        return Resultado<bool>.Exito(true);
    }

    public Resultado<bool> ActualizarBorrador(
        Guid proveedorId,
        DateTimeOffset fechaCompraUtc,
        string? referencia,
        string? observacion,
        IReadOnlyCollection<DetalleCompra> nuevosDetalles)
    {
        if (Estado != EstadoCompra.Borrador)
        {
            return Resultado<bool>.Fallo(new ErrorDominio(
                "compra.no_editable", "Una compra confirmada no se puede modificar."));
        }

        var error = Validar(proveedorId, fechaCompraUtc, referencia, observacion, CodigoMoneda);
        if (error is not null)
        {
            return Resultado<bool>.Fallo(error);
        }

        if (nuevosDetalles.Count == 0 || nuevosDetalles.Any(x => x.CostoUnitario.CodigoMoneda != CodigoMoneda))
        {
            return Resultado<bool>.Fallo(new ErrorDominio(
                "compra.detalles_invalidos", "La compra debe tener lineas validas en la moneda del negocio."));
        }

        decimal nuevoTotal;
        try
        {
            nuevoTotal = nuevosDetalles.Sum(x => x.TotalLinea.Importe);
        }
        catch (OverflowException)
        {
            return Resultado<bool>.Fallo(new ErrorDominio(
                "compra.total_invalido", "El total de la compra excede la precision permitida."));
        }

        var total = Dinero.Crear(nuevoTotal, CodigoMoneda);
        if (!total.EsExitoso)
        {
            return Resultado<bool>.Fallo(total.Error!);
        }

        ProveedorId = proveedorId;
        FechaCompraUtc = fechaCompraUtc.ToUniversalTime();
        Referencia = Limpiar(referencia);
        ReferenciaNormalizada = TextoNormalizado.Normalizar(Referencia);
        Observacion = Limpiar(observacion);
        foreach (var detalle in _detalles.Where(x => x.Vigente))
        {
            detalle.Desactivar();
        }

        _detalles.AddRange(nuevosDetalles);
        TotalImporte = total.Valor!.Importe;
        return Resultado<bool>.Exito(true);
    }

    public Resultado<bool> Confirmar(DateTimeOffset fechaUtc)
    {
        if (Estado != EstadoCompra.Borrador || !_detalles.Any(x => x.Vigente))
        {
            return Resultado<bool>.Fallo(new ErrorDominio(
                "compra.no_confirmable", "La compra debe estar en borrador y contener lineas vigentes."));
        }

        Estado = EstadoCompra.Confirmada;
        FechaConfirmacionUtc = fechaUtc.ToUniversalTime();
        return Resultado<bool>.Exito(true);
    }

    public Resultado<bool> RegistrarDevolucion(
        DevolucionCompra devolucion,
        bool anular,
        Guid usuarioId,
        DateTimeOffset fechaUtc)
    {
        decimal totalDevuelto;
        try
        {
            totalDevuelto = checked(TotalDevuelto + devolucion.ImporteLineas);
        }
        catch (OverflowException)
        {
            return Resultado<bool>.Fallo(new ErrorDominio(
                "compra.devolucion_invalida", "El importe de la devolución excede el saldo de la compra."));
        }

        if (Estado is EstadoCompra.Borrador or EstadoCompra.Devuelta or EstadoCompra.Anulada ||
            devolucion.CompraId != Id || devolucion.ImporteLineas < 0m ||
            totalDevuelto > TotalImporte || usuarioId == Guid.Empty || fechaUtc == default ||
            anular && string.IsNullOrWhiteSpace(devolucion.Motivo))
        {
            return Resultado<bool>.Fallo(new ErrorDominio(
                "compra.devolucion_invalida", "La devolución no es válida para el estado, saldo o líneas de la compra."));
        }

        _devoluciones.Add(devolucion);
        TotalDevuelto = totalDevuelto;
        if (anular)
        {
            Estado = EstadoCompra.Anulada;
            MotivoAnulacion = devolucion.Motivo.Trim();
            FechaAnulacionUtc = fechaUtc.ToUniversalTime();
        }
        else
        {
            Estado = TotalDevuelto == TotalImporte
                ? EstadoCompra.Devuelta
                : EstadoCompra.ParcialmenteDevuelta;
        }

        return Resultado<bool>.Exito(true);
    }

    private static ErrorDominio? Validar(
        Guid proveedorId,
        DateTimeOffset fechaCompraUtc,
        string? referencia,
        string? observacion,
        string? codigoMoneda)
    {
        if (proveedorId == Guid.Empty || fechaCompraUtc == default ||
            (!string.IsNullOrWhiteSpace(referencia) && referencia.Trim().Length > 80) ||
            (!string.IsNullOrWhiteSpace(observacion) && observacion.Trim().Length > 500))
        {
            return new ErrorDominio("compra.datos_invalidos", "Los datos de la compra son invalidos.");
        }

        var moneda = Dinero.Crear(0m, codigoMoneda);
        return moneda.EsExitoso ? null : moneda.Error;
    }

    private static string? Limpiar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
