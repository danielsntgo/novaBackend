using FlexPos.Api.DTOs;
using FlexPos.Application.Authorization;
using FlexPos.Application.DTOs.Caja;
using FlexPos.Application.Services;
using FlexPos.Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlexPos.Api.Controllers;

[ApiController]
[Authorize(Policy = PoliticasAutorizacion.OperacionDiaria)]
[Route("api/caja")]
public sealed class CajaController(ServicioCaja servicio) : ControllerBase
{
    [HttpGet("actual")]
    [ProducesResponseType(typeof(CajaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ObtenerActual(CancellationToken cancellationToken)
    {
        var resultado = await servicio.ObtenerActualAsync(cancellationToken);
        if (!resultado.EsExitoso)
        {
            return CrearProblema(resultado.Error!);
        }

        return resultado.Valor is null ? NoContent() : Ok(resultado.Valor);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginaCajasDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] DateTimeOffset? desdeUtc,
        [FromQuery] DateTimeOffset? hastaUtc,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        var resultado = await servicio.ListarAsync(
            desdeUtc, hastaUtc, pagina, tamanoPagina, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CajaDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await servicio.ObtenerAsync(id, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpPost("aperturas")]
    [ProducesResponseType(typeof(CajaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Abrir(
        [FromBody] AbrirCajaRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.AbrirAsync(
            new SolicitudAbrirCaja(solicitud.EfectivoInicial), cancellationToken);
        return resultado.EsExitoso
            ? CreatedAtAction(nameof(Obtener), new { id = resultado.Valor!.Id }, resultado.Valor)
            : CrearProblema(resultado.Error!);
    }

    [HttpPost("{id:guid}/movimientos")]
    [ProducesResponseType(typeof(MovimientoCajaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegistrarMovimiento(
        Guid id,
        [FromBody] MovimientoCajaRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.RegistrarMovimientoAsync(
            id, new SolicitudMovimientoCaja(solicitud.Tipo, solicitud.Importe, solicitud.Concepto),
            cancellationToken);
        return resultado.EsExitoso
            ? StatusCode(StatusCodes.Status201Created, resultado.Valor)
            : CrearProblema(resultado.Error!);
    }

    [HttpPost("{id:guid}/cierre")]
    [ProducesResponseType(typeof(CajaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cerrar(
        Guid id,
        [FromBody] CerrarCajaRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.CerrarAsync(
            id, new SolicitudCerrarCaja(solicitud.EfectivoContado), cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    private ObjectResult CrearProblema(ErrorDominio error)
    {
        var estado = error.Codigo switch
        {
            "caja.no_encontrada" => StatusCodes.Status404NotFound,
            "caja.conflicto" or "caja.no_abierta" => StatusCodes.Status409Conflict,
            var codigo when codigo.EndsWith("conflicto", StringComparison.Ordinal) =>
                StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };
        return Problem(statusCode: estado, title: error.Mensaje, type: $"urn:flexpos:error:{error.Codigo}");
    }
}
