using FlexPos.Api.DTOs;
using FlexPos.Application.Authorization;
using FlexPos.Application.DTOs.Comisiones;
using FlexPos.Application.Services;
using FlexPos.Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlexPos.Api.Controllers;

[ApiController]
[Authorize(Policy = PoliticasAutorizacion.SoloAdministrador)]
[Route("api/comisiones")]
public sealed class ComisionesController(ServicioComisiones servicio) : ControllerBase
{
    [HttpGet("reglas")]
    [ProducesResponseType(typeof(PaginaReglasComisionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarReglas(
        [FromQuery] Guid? empleadoId,
        [FromQuery] Guid? servicioId,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        var resultado = await servicio.ListarReglasAsync(
            empleadoId, servicioId, pagina, tamanoPagina, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpPost("reglas")]
    [ProducesResponseType(typeof(ReglaComisionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CrearRegla(
        [FromBody] CrearReglaComisionRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.CrearReglaAsync(new SolicitudCrearReglaComision(
            solicitud.EmpleadoId, solicitud.ServicioId, solicitud.Tipo, solicitud.Valor), cancellationToken);
        return resultado.EsExitoso
            ? StatusCode(StatusCodes.Status201Created, resultado.Valor)
            : CrearProblema(resultado.Error!);
    }

    [HttpPut("reglas/{id:guid}")]
    [ProducesResponseType(typeof(ReglaComisionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActualizarRegla(
        Guid id,
        [FromBody] ActualizarReglaComisionRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.ActualizarReglaAsync(
            id, new SolicitudActualizarReglaComision(solicitud.Tipo, solicitud.Valor), cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpPatch("reglas/{id:guid}/estado")]
    [ProducesResponseType(typeof(ReglaComisionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EstablecerEstadoRegla(
        Guid id,
        [FromBody] EstadoReglaComisionRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.EstablecerEstadoReglaAsync(id, solicitud.Activa, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginaComisionesDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] Guid? empleadoId,
        [FromQuery] string? estado,
        [FromQuery] string? tipoMovimiento,
        [FromQuery] DateTimeOffset? desdeUtc,
        [FromQuery] DateTimeOffset? hastaUtc,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        var resultado = await servicio.ListarAsync(
            empleadoId, estado, tipoMovimiento, desdeUtc, hastaUtc, pagina, tamanoPagina, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpGet("liquidaciones")]
    [ProducesResponseType(typeof(PaginaLiquidacionesComisionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarLiquidaciones(
        [FromQuery] Guid? empleadoId,
        [FromQuery] DateTimeOffset? desdeUtc,
        [FromQuery] DateTimeOffset? hastaUtc,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        var resultado = await servicio.ListarLiquidacionesAsync(
            empleadoId, desdeUtc, hastaUtc, pagina, tamanoPagina, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpGet("liquidaciones/{id:guid}")]
    [ProducesResponseType(typeof(LiquidacionComisionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerLiquidacion(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await servicio.ObtenerLiquidacionAsync(id, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpPost("liquidaciones")]
    [ProducesResponseType(typeof(LiquidacionComisionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Liquidar(
        [FromBody] LiquidarComisionesRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.LiquidarAsync(new SolicitudLiquidarComisiones(
            solicitud.ComisionIds, solicitud.MetodoPagoId, solicitud.Referencia), cancellationToken);
        return resultado.EsExitoso
            ? CreatedAtAction(nameof(ObtenerLiquidacion), new { id = resultado.Valor!.Id }, resultado.Valor)
            : CrearProblema(resultado.Error!);
    }

    private ObjectResult CrearProblema(ErrorDominio error)
    {
        var estado = error.Codigo switch
        {
            "comision.regla_no_encontrada" or "comision.liquidacion_no_encontrada" => StatusCodes.Status404NotFound,
            "comision.regla_existente" or "comision.referencia_inactiva" or "comision.conflicto" or
                "comision.metodo_pago_inactivo" or "comision.seleccion_invalida" or
                "comision.saldo_no_pagable" or "caja.no_abierta" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };
        return Problem(statusCode: estado, title: error.Mensaje, type: $"urn:flexpos:error:{error.Codigo}");
    }
}
