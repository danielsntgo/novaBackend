using FlexPos.Api.DTOs;
using FlexPos.Application.Authorization;
using FlexPos.Application.DTOs.Finanzas;
using FlexPos.Application.Services;
using FlexPos.Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlexPos.Api.Controllers;

[ApiController]
[Authorize(Policy = PoliticasAutorizacion.SoloAdministrador)]
[Route("api/finanzas")]
public sealed class FinanzasController(ServicioFinanzas servicio) : ControllerBase
{
    [HttpGet("cuentas")]
    [ProducesResponseType(typeof(PaginaCuentasFinancierasDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarCuentas(
        [FromQuery] string? tipo,
        [FromQuery] bool? activa,
        [FromQuery] string? buscar,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        var resultado = await servicio.ListarCuentasAsync(tipo, activa, buscar, pagina, tamanoPagina, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpPost("cuentas")]
    [ProducesResponseType(typeof(CuentaFinancieraDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CrearCuenta(
        [FromBody] SolicitudCuentaFinanciera solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.CrearCuentaAsync(solicitud, cancellationToken);
        return resultado.EsExitoso
            ? CreatedAtAction(nameof(ObtenerCuenta), new { id = resultado.Valor!.Id }, resultado.Valor)
            : CrearProblema(resultado.Error!);
    }

    [HttpGet("cuentas/{id:guid}")]
    [ProducesResponseType(typeof(CuentaFinancieraDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObtenerCuenta(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await servicio.ObtenerCuentaAsync(id, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpPut("cuentas/{id:guid}")]
    [ProducesResponseType(typeof(CuentaFinancieraDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ActualizarCuenta(
        Guid id,
        [FromBody] SolicitudCuentaFinanciera solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.ActualizarCuentaAsync(id, solicitud, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpPatch("cuentas/{id:guid}/estado")]
    [ProducesResponseType(typeof(CuentaFinancieraDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> EstablecerEstado(
        Guid id,
        [FromBody] SolicitudEstadoCuentaFinanciera solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.EstablecerEstadoAsync(id, solicitud.Activa, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpGet("cuentas/{id:guid}/movimientos")]
    [ProducesResponseType(typeof(PaginaMovimientosFinancierosDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarMovimientos(
        Guid id,
        [FromQuery] DateTimeOffset? desdeUtc,
        [FromQuery] DateTimeOffset? hastaUtc,
        [FromQuery] string? tipo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        var resultado = await servicio.ListarMovimientosAsync(
            id, desdeUtc, hastaUtc, tipo, pagina, tamanoPagina, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpGet("resumen")]
    [ProducesResponseType(typeof(ResumenFinancieroDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObtenerResumen(CancellationToken cancellationToken)
    {
        var resultado = await servicio.ObtenerResumenAsync(cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpPost("movimientos")]
    [ProducesResponseType(typeof(MovimientoFinancieroDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegistrarMovimiento(
        [FromBody] SolicitudMovimientoMetodoPago solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.RegistrarMovimientoMetodoPagoAsync(solicitud, cancellationToken);
        return resultado.EsExitoso
            ? StatusCode(StatusCodes.Status201Created, resultado.Valor)
            : CrearProblema(resultado.Error!);
    }

    [HttpPost("transferencias")]
    [ProducesResponseType(typeof(TransferenciaFinancieraDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CrearTransferencia(
        [FromBody] SolicitudTransferenciaFinanciera solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.CrearTransferenciaAsync(solicitud, cancellationToken);
        return resultado.EsExitoso
            ? CreatedAtAction(nameof(ListarTransferencias), new { }, resultado.Valor)
            : CrearProblema(resultado.Error!);
    }

    [HttpGet("transferencias")]
    [ProducesResponseType(typeof(PaginaTransferenciasFinancierasDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListarTransferencias(
        [FromQuery] DateTimeOffset? desdeUtc,
        [FromQuery] DateTimeOffset? hastaUtc,
        [FromQuery] Guid? cuentaId,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        var resultado = await servicio.ListarTransferenciasAsync(
            desdeUtc, hastaUtc, cuentaId, pagina, tamanoPagina, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    private ObjectResult CrearProblema(ErrorDominio error)
    {
        var estado = error.Codigo switch
        {
            "cuenta_financiera.no_encontrada" or "metodo_pago.no_encontrado" => StatusCodes.Status404NotFound,
            var codigo when codigo.EndsWith("conflicto", StringComparison.Ordinal) ||
                            codigo is "cuenta_financiera.saldo_insuficiente" or "cuenta_financiera.cupo_excedido" or "caja.no_abierta" =>
                StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };
        return Problem(statusCode: estado, title: error.Mensaje, type: $"urn:flexpos:error:{error.Codigo}");
    }
}
