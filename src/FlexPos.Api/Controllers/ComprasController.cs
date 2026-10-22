using FlexPos.Api.DTOs;
using FlexPos.Application.Authorization;
using FlexPos.Application.DTOs.Compras;
using FlexPos.Application.Services;
using FlexPos.Domain.Enums;
using FlexPos.Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlexPos.Api.Controllers;

[ApiController]
[Authorize(Policy = PoliticasAutorizacion.SoloAdministrador)]
[Route("api/compras")]
public sealed class ComprasController(ServicioCompras servicio) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginaComprasDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] string? buscar,
        [FromQuery] string? estado,
        [FromQuery] DateTimeOffset? desdeUtc,
        [FromQuery] DateTimeOffset? hastaUtc,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        EstadoCompra? estadoCompra = null;
        if (!string.IsNullOrWhiteSpace(estado))
        {
            if (!Enum.TryParse(estado, true, out EstadoCompra valor) || !Enum.IsDefined(valor))
            {
                return CrearProblema(new ErrorDominio("compra.busqueda_invalida", "El estado no es valido."));
            }

            estadoCompra = valor;
        }

        var resultado = await servicio.ListarAsync(
            buscar, estadoCompra, desdeUtc, hastaUtc, pagina, tamanoPagina, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CompraDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await servicio.ObtenerAsync(id, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CompraDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Crear(
        [FromBody] GuardarCompraRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.CrearAsync(Convertir(solicitud), cancellationToken);
        return resultado.EsExitoso
            ? CreatedAtAction(nameof(Obtener), new { id = resultado.Valor!.Id }, resultado.Valor)
            : CrearProblema(resultado.Error!);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CompraDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ActualizarBorrador(
        Guid id,
        [FromBody] GuardarCompraRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.ActualizarBorradorAsync(id, Convertir(solicitud), cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpPost("{id:guid}/confirmar")]
    [ProducesResponseType(typeof(CompraDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Confirmar(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await servicio.ConfirmarAsync(id, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpPost("{id:guid}/devoluciones")]
    [ProducesResponseType(typeof(CompraDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Devolver(
        Guid id,
        [FromBody] DevolucionCompraRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.DevolverAsync(id, new SolicitudDevolucionCompra(
            solicitud.Motivo,
            solicitud.Lineas?.Select(x => new SolicitudLineaDevolucionCompra(x.DetalleCompraId, x.Cantidad)).ToArray(),
            solicitud.Pagos?.Select(x => new SolicitudPagoCompra(x.MetodoPagoId, x.Importe, x.Referencia)).ToArray()),
            cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpPost("{id:guid}/anular")]
    [ProducesResponseType(typeof(CompraDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Anular(
        Guid id,
        [FromBody] AnularCompraRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.AnularAsync(id, new SolicitudAnularCompra(
            solicitud.Motivo,
            solicitud.Pagos?.Select(x => new SolicitudPagoCompra(x.MetodoPagoId, x.Importe, x.Referencia)).ToArray()),
            cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    private static SolicitudCompra Convertir(GuardarCompraRequest solicitud) => new(
        solicitud.ProveedorId,
        solicitud.FechaCompraUtc,
        solicitud.Referencia,
        solicitud.Observacion,
        solicitud.Detalles?.Select(x => new SolicitudLineaCompra(x.ArticuloId, x.Cantidad, x.CostoUnitario)).ToArray()
            ?? []);

    private ObjectResult CrearProblema(ErrorDominio error)
    {
        var estado = error.Codigo switch
        {
            "compra.no_encontrada" => StatusCodes.Status404NotFound,
            "compra.conflicto" or "compra.no_editable" or "compra.no_confirmable" or "compra.moneda_conflicto" =>
                StatusCodes.Status409Conflict,
            "compra.cantidad_devolucion_invalida" or "compra.importe_reintegro_invalido" or
                "compra.cuenta_financiera_invalida" or "caja.no_abierta" or "inventario.stock_insuficiente" =>
                StatusCodes.Status409Conflict,
            var codigo when codigo.EndsWith("conflicto", StringComparison.Ordinal) => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };
        return Problem(statusCode: estado, title: error.Mensaje, type: $"urn:flexpos:error:{error.Codigo}");
    }
}
