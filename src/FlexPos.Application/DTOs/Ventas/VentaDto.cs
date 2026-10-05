namespace FlexPos.Application.DTOs.Ventas;

public sealed record LineaVentaDto(
    Guid Id,
    string Tipo,
    Guid? ArticuloInventarioId,
    Guid? ServicioId,
    Guid? EmpleadoId,
    string? Codigo,
    string Nombre,
    string Unidad,
    decimal Cantidad,
    decimal PrecioUnitario,
    string? TipoDescuento,
    decimal ValorDescuento,
    decimal DescuentoImporte,
    decimal DescuentoGeneralImporte,
    string? ImpuestoNombre,
    decimal? ImpuestoPorcentaje,
    decimal ImpuestoImporte,
    decimal TotalImporte);

public sealed record PagoVentaDto(
    Guid Id,
    Guid MetodoPagoId,
    string MetodoPagoNombre,
    bool EsEfectivo,
    decimal Importe,
    string? Referencia,
    DateTimeOffset FechaUtc);

public sealed record DocumentoVentaDto(
    Guid Id,
    string TipoDocumento,
    string NumeroCompleto,
    DateTimeOffset FechaEmisionUtc,
    decimal Total);

public sealed record DetalleDevolucionDto(Guid DetalleVentaId, decimal Cantidad, decimal Importe);

public sealed record DevolucionVentaDto(
    Guid Id,
    DateTimeOffset FechaUtc,
    string Motivo,
    decimal ImporteLineas,
    decimal ImporteReintegrado,
    IReadOnlyList<DetalleDevolucionDto> Lineas,
    IReadOnlyList<PagoDevolucionVentaDto> Pagos);

public sealed record PagoDevolucionVentaDto(
    Guid MetodoPagoId,
    string MetodoPagoNombre,
    bool EsEfectivo,
    decimal Importe,
    string? Referencia);

public sealed record VentaDto(
    Guid Id,
    Guid CajaId,
    Guid? ClienteId,
    string? ClienteNombre,
    string? ClienteDocumento,
    DateTimeOffset FechaVentaUtc,
    string CodigoMoneda,
    string Estado,
    decimal Subtotal,
    string? TipoDescuentoGeneral,
    decimal ValorDescuentoGeneral,
    decimal DescuentoLineas,
    decimal DescuentoGeneral,
    decimal Impuestos,
    decimal Total,
    decimal TotalPagado,
    decimal TotalDevuelto,
    decimal TotalReintegrado,
    decimal TotalPendiente,
    string? MotivoAnulacion,
    IReadOnlyList<LineaVentaDto> Lineas,
    IReadOnlyList<PagoVentaDto> Pagos,
    IReadOnlyList<DocumentoVentaDto> Documentos,
    IReadOnlyList<DevolucionVentaDto> Devoluciones,
    DateTimeOffset FechaCreacionUtc,
    DateTimeOffset? FechaModificacionUtc);

public sealed record PaginaVentasDto(
    IReadOnlyList<VentaDto> Elementos,
    int Pagina,
    int TamanoPagina,
    int TotalElementos);

public sealed record SolicitudDescuentoVenta(string? Tipo, decimal Valor);

public sealed record SolicitudLineaVenta(
    string? Tipo,
    Guid? ArticuloInventarioId,
    Guid? ServicioId,
    Guid? EmpleadoId,
    decimal Cantidad,
    SolicitudDescuentoVenta? Descuento,
    Guid? ImpuestoId);

public sealed record SolicitudPagoVenta(Guid MetodoPagoId, decimal Importe, string? Referencia);

public sealed record SolicitudCrearVenta(
    Guid? ClienteId,
    bool SolicitarFactura,
    SolicitudDescuentoVenta? DescuentoGeneral,
    IReadOnlyList<SolicitudLineaVenta>? Lineas,
    IReadOnlyList<SolicitudPagoVenta>? Pagos);

public sealed record SolicitudAgregarPagos(IReadOnlyList<SolicitudPagoVenta>? Pagos);

public sealed record SolicitudDevolucionVenta(
    string? Motivo,
    IReadOnlyList<SolicitudLineaDevolucion>? Lineas,
    IReadOnlyList<SolicitudPagoVenta>? Pagos);

public sealed record SolicitudLineaDevolucion(Guid DetalleVentaId, decimal Cantidad);

public sealed record SolicitudAnularVenta(string? Motivo, IReadOnlyList<SolicitudPagoVenta>? Pagos);
