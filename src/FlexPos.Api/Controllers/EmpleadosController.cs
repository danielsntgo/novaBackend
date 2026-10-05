using FlexPos.Api.DTOs;
using FlexPos.Application.Authorization;
using FlexPos.Application.DTOs.Empleados;
using FlexPos.Application.Services;
using FlexPos.Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlexPos.Api.Controllers;

[ApiController]
[Authorize(Policy = PoliticasAutorizacion.SoloAdministrador)]
[Route("api/empleados")]
public sealed class EmpleadosController(ServicioEmpleados servicio) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginaEmpleadosDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] string? buscar,
        [FromQuery] bool? activo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        var resultado = await servicio.ListarAsync(buscar, activo, pagina, tamanoPagina, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EmpleadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await servicio.ObtenerAsync(id, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EmpleadoDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Crear(
        [FromBody] GuardarEmpleadoRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.CrearAsync(
            new SolicitudEmpleado(
                solicitud.Nombre, solicitud.Cargo, solicitud.Documento, solicitud.Telefono, solicitud.Correo),
            cancellationToken);
        return resultado.EsExitoso
            ? CreatedAtAction(nameof(Obtener), new { id = resultado.Valor!.Id }, resultado.Valor)
            : CrearProblema(resultado.Error!);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(EmpleadoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Actualizar(
        Guid id,
        [FromBody] GuardarEmpleadoRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.ActualizarAsync(
            id,
            new SolicitudEmpleado(
                solicitud.Nombre, solicitud.Cargo, solicitud.Documento, solicitud.Telefono, solicitud.Correo),
            cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpPatch("{id:guid}/estado")]
    [ProducesResponseType(typeof(EmpleadoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> EstablecerEstado(
        Guid id,
        [FromBody] EstablecerEstadoRegistroRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.EstablecerEstadoAsync(id, solicitud.Activo, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpGet("{id:guid}/horario-semanal")]
    [ProducesResponseType(typeof(IReadOnlyList<HorarioSemanalEmpleadoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerHorarioSemanal(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await servicio.ObtenerHorarioSemanalAsync(id, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpPut("{id:guid}/horario-semanal")]
    [ProducesResponseType(typeof(IReadOnlyList<HorarioSemanalEmpleadoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfigurarHorarioSemanal(
        Guid id,
        [FromBody] GuardarHorarioSemanalEmpleadoRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.ConfigurarHorarioSemanalAsync(id, solicitud.Intervalos, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    private ObjectResult CrearProblema(ErrorDominio error)
    {
        var estado = error.Codigo switch
        {
            "empleado.no_encontrado" => StatusCodes.Status404NotFound,
            var codigo when codigo.EndsWith("conflicto", StringComparison.Ordinal) => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return Problem(statusCode: estado, title: error.Mensaje, type: $"urn:flexpos:error:{error.Codigo}");
    }
}
