using FlexPos.Api.DTOs;
using FlexPos.Application.Authorization;
using FlexPos.Application.DTOs.Reportes;
using FlexPos.Application.Services;
using FlexPos.Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FlexPos.Api.Controllers;

[ApiController]
[Authorize(Policy = PoliticasAutorizacion.SoloAdministrador)]
[Route("api/reportes")]
public sealed class ReportesController(ServicioReportes servicio) : ControllerBase
{
    [HttpGet("{tipo}")]
    [ProducesResponseType(typeof(ReporteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Obtener(
        string tipo,
        [FromQuery] DateTimeOffset? desdeUtc,
        [FromQuery] DateTimeOffset? hastaUtc,
        [FromQuery] Guid? clienteId,
        [FromQuery] Guid? empleadoId,
        [FromQuery] Guid? articuloId,
        [FromQuery] Guid? servicioId,
        [FromQuery] Guid? proveedorId,
        [FromQuery] Guid? metodoPagoId,
        [FromQuery] string? estado,
        [FromQuery] int limite = 5000,
        CancellationToken cancellationToken = default)
    {
        var resultado = await servicio.ObtenerAsync(tipo, CrearFiltros(
            desdeUtc, hastaUtc, clienteId, empleadoId, articuloId, servicioId, proveedorId,
            metodoPagoId, estado, limite), cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpGet("{tipo}/exportar")]
    [Produces("application/pdf", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "text/csv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Exportar(
        string tipo,
        [FromQuery, BindRequired] string formato,
        [FromQuery] DateTimeOffset? desdeUtc,
        [FromQuery] DateTimeOffset? hastaUtc,
        [FromQuery] Guid? clienteId,
        [FromQuery] Guid? empleadoId,
        [FromQuery] Guid? articuloId,
        [FromQuery] Guid? servicioId,
        [FromQuery] Guid? proveedorId,
        [FromQuery] Guid? metodoPagoId,
        [FromQuery] string? estado,
        [FromQuery] int limite = 5000,
        CancellationToken cancellationToken = default)
    {
        var resultado = await servicio.ExportarAsync(tipo, formato.ToLowerInvariant(), CrearFiltros(
            desdeUtc, hastaUtc, clienteId, empleadoId, articuloId, servicioId, proveedorId,
            metodoPagoId, estado, limite), cancellationToken);
        return resultado.EsExitoso
            ? File(resultado.Valor!.Contenido, resultado.Valor.TipoContenido, resultado.Valor.Nombre)
            : CrearProblema(resultado.Error!);
    }

    private static FiltrosReporte CrearFiltros(
        DateTimeOffset? desdeUtc,
        DateTimeOffset? hastaUtc,
        Guid? clienteId,
        Guid? empleadoId,
        Guid? articuloId,
        Guid? servicioId,
        Guid? proveedorId,
        Guid? metodoPagoId,
        string? estado,
        int limite) => new(
        desdeUtc, hastaUtc, clienteId, empleadoId, articuloId, servicioId,
        proveedorId, metodoPagoId, estado, limite);

    private ObjectResult CrearProblema(ErrorDominio error) =>
        Problem(statusCode: StatusCodes.Status400BadRequest,
            title: error.Mensaje,
            type: $"urn:flexpos:error:{error.Codigo}");
}
