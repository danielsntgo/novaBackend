namespace FlexPos.Api.DTOs;

public sealed record AbrirCajaRequest(decimal EfectivoInicial);

public sealed record CerrarCajaRequest(decimal EfectivoContado);

public sealed record MovimientoCajaRequest(string? Tipo, decimal Importe, string? Concepto);

public sealed record DescuentoVentaRequest(string? Tipo, decimal Valor);

public sealed record LineaVentaRequest(
    string? Tipo,
    Guid? ArticuloInventarioId,
    Guid? ServicioId,
    Guid? EmpleadoId,
    decimal Cantidad,
    DescuentoVentaRequest? Descuento,
    Guid? ImpuestoId);

public sealed record PagoVentaRequest(Guid MetodoPagoId, decimal Importe, string? Referencia);

public sealed record CrearVentaRequest(
    Guid? ClienteId,
    bool SolicitarFactura,
    DescuentoVentaRequest? DescuentoGeneral,
    IReadOnlyList<LineaVentaRequest>? Lineas,
    IReadOnlyList<PagoVentaRequest>? Pagos);

public sealed record AgregarPagosVentaRequest(IReadOnlyList<PagoVentaRequest>? Pagos);

public sealed record LineaDevolucionVentaRequest(Guid DetalleVentaId, decimal Cantidad);

public sealed record DevolucionVentaRequest(
    string? Motivo,
    IReadOnlyList<LineaDevolucionVentaRequest>? Lineas,
    IReadOnlyList<PagoVentaRequest>? Pagos);

public sealed record AnularVentaRequest(string? Motivo, IReadOnlyList<PagoVentaRequest>? Pagos);
