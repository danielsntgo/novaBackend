using FlexPos.Api.DTOs;
using FlexPos.Application.Authorization;
using FlexPos.Application.DTOs.Ventas;
using FlexPos.Application.Services;
using FlexPos.Domain.Enums;
using FlexPos.Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlexPos.Api.Controllers;

[ApiController]
[Authorize(Policy = PoliticasAutorizacion.OperacionDiaria)]
[Route("api/ventas")]
public sealed class VentasController(ServicioVentas servicio) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginaVentasDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] DateTimeOffset? desdeUtc,
        [FromQuery] DateTimeOffset? hastaUtc,
        [FromQuery] Guid? clienteId,
        [FromQuery] string? estado,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        var resultado = await servicio.ListarAsync(
            desdeUtc, hastaUtc, clienteId, estado, pagina, tamanoPagina, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VentaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await servicio.ObtenerAsync(id, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpPost]
    [ProducesResponseType(typeof(VentaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(
        [FromBody] CrearVentaRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.CrearAsync(new SolicitudCrearVenta(
            solicitud.ClienteId,
            solicitud.SolicitarFactura,
            Convertir(solicitud.DescuentoGeneral),
            solicitud.Lineas?.Select(x => new SolicitudLineaVenta(
                x.Tipo, x.ArticuloInventarioId, x.ServicioId, x.EmpleadoId, x.Cantidad,
                Convertir(x.Descuento), x.ImpuestoId)).ToArray(),
            solicitud.Pagos?.Select(Convertir).ToArray()), cancellationToken);
        return resultado.EsExitoso
            ? CreatedAtAction(nameof(Obtener), new { id = resultado.Valor!.Id }, resultado.Valor)
            : CrearProblema(resultado.Error!);
    }

    [HttpPost("{id:guid}/pagos")]
    [ProducesResponseType(typeof(VentaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AgregarPagos(
        Guid id,
        [FromBody] AgregarPagosVentaRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.AgregarPagosAsync(
            id, new SolicitudAgregarPagos(solicitud.Pagos?.Select(Convertir).ToArray()), cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpPost("{id:guid}/devoluciones")]
    [Authorize(Policy = PoliticasAutorizacion.SoloAdministrador)]
    [ProducesResponseType(typeof(VentaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Devolver(
        Guid id,
        [FromBody] DevolucionVentaRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.DevolverAsync(id, new SolicitudDevolucionVenta(
            solicitud.Motivo,
            solicitud.Lineas?.Select(x => new SolicitudLineaDevolucion(x.DetalleVentaId, x.Cantidad)).ToArray(),
            solicitud.Pagos?.Select(Convertir).ToArray()), cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpPost("{id:guid}/anular")]
    [Authorize(Policy = PoliticasAutorizacion.SoloAdministrador)]
    [ProducesResponseType(typeof(VentaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Anular(
        Guid id,
        [FromBody] AnularVentaRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.AnularAsync(id, new SolicitudAnularVenta(
            solicitud.Motivo, solicitud.Pagos?.Select(Convertir).ToArray()), cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpGet("{id:guid}/comprobante.pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerComprobante(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await servicio.ObtenerDocumentoAsync(
            id, TipoDocumentoVenta.Comprobante, cancellationToken);
        return resultado.EsExitoso
            ? File(resultado.Valor!.Contenido, resultado.Valor.TipoMime, resultado.Valor.NombreArchivo)
            : CrearProblema(resultado.Error!);
    }

    [HttpGet("{id:guid}/factura.pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerFactura(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await servicio.ObtenerDocumentoAsync(
            id, TipoDocumentoVenta.Factura, cancellationToken);
        return resultado.EsExitoso
            ? File(resultado.Valor!.Contenido, resultado.Valor.TipoMime, resultado.Valor.NombreArchivo)
            : CrearProblema(resultado.Error!);
    }

    private ObjectResult CrearProblema(ErrorDominio error)
    {
        var estado = error.Codigo switch
        {
            "venta.no_encontrada" or "venta.documento_no_encontrado" =>
                StatusCodes.Status404NotFound,
            "venta.conflicto" or "caja.no_abierta" or "venta.cliente_inactivo" or
                "venta.producto_inactivo" or "venta.servicio_inactivo" or "venta.empleado_inactivo" or
                "venta.impuesto_inactivo" or "venta.metodo_pago_inactivo" or
                "venta.facturacion_deshabilitada" or "venta.saldo_pendiente_deshabilitado" or
                "venta.cantidad_devolucion_invalida" =>
                StatusCodes.Status409Conflict,
            var codigo when codigo.EndsWith("conflicto", StringComparison.Ordinal) =>
                StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };
        return Problem(statusCode: estado, title: error.Mensaje, type: $"urn:flexpos:error:{error.Codigo}");
    }

    private static SolicitudDescuentoVenta? Convertir(DescuentoVentaRequest? solicitud) =>
        solicitud is null ? null : new SolicitudDescuentoVenta(solicitud.Tipo, solicitud.Valor);

    private static SolicitudPagoVenta Convertir(PagoVentaRequest solicitud) =>
        new(solicitud.MetodoPagoId, solicitud.Importe, solicitud.Referencia);
}
