using FlexPos.Api.DTOs;
using FlexPos.Application.Authorization;
using FlexPos.Application.DTOs.Citas;
using FlexPos.Application.Services;
using FlexPos.Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlexPos.Api.Controllers;

[ApiController]
[Authorize(Policy = PoliticasAutorizacion.OperacionDiaria)]
[Route("api/citas")]
public sealed class CitasController(ServicioCitas servicio) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginaCitasDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Listar(
        [FromQuery] DateTimeOffset? desdeUtc,
        [FromQuery] DateTimeOffset? hastaUtc,
        [FromQuery] Guid? empleadoId,
        [FromQuery] Guid? clienteId,
        [FromQuery] string? estado,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        var resultado = await servicio.ListarAsync(
            desdeUtc, hastaUtc, empleadoId, clienteId, estado, pagina, tamanoPagina, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpGet("disponibilidad")]
    [ProducesResponseType(typeof(IReadOnlyList<EmpleadoDisponibilidadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConsultarDisponibilidad(
        [FromQuery] Guid servicioId,
        [FromQuery] DateTimeOffset fechaLocal,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.ConsultarDisponibilidadAsync(servicioId, fechaLocal, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CitaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await servicio.ObtenerAsync(id, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CitaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(
        [FromBody] CrearCitaRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.CrearAsync(new SolicitudCita(
            solicitud.ClienteId, solicitud.ServicioId, solicitud.EmpleadoId, solicitud.InicioLocal), cancellationToken);
        return resultado.EsExitoso
            ? CreatedAtAction(nameof(Obtener), new { id = resultado.Valor!.Id }, resultado.Valor)
            : CrearProblema(resultado.Error!);
    }

    [HttpPut("{id:guid}/reprogramar")]
    [ProducesResponseType(typeof(CitaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reprogramar(
        Guid id,
        [FromBody] ReprogramarCitaRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.ReprogramarAsync(
            id, solicitud.EmpleadoId, solicitud.InicioLocal, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpPatch("{id:guid}/estado")]
    [ProducesResponseType(typeof(CitaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CambiarEstado(
        Guid id,
        [FromBody] CambiarEstadoCitaRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.CambiarEstadoAsync(id, solicitud.Estado, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    private ObjectResult CrearProblema(ErrorDominio error)
    {
        var estado = error.Codigo switch
        {
            "cita.no_encontrada" => StatusCodes.Status404NotFound,
            "cita.referencia_inactiva" or "cita.servicio_inactivo" => StatusCodes.Status409Conflict,
            "cita.solapamiento" or "cita.fuera_horario" or "cita.estado_terminal" or "cita.transicion_invalida" or "cita.conflicto" =>
                StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return Problem(statusCode: estado, title: error.Mensaje, type: $"urn:flexpos:error:{error.Codigo}");
    }
}
