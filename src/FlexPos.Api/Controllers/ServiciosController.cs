using FlexPos.Api.DTOs;
using FlexPos.Application.Authorization;
using FlexPos.Application.DTOs.Servicios;
using FlexPos.Application.Services;
using FlexPos.Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlexPos.Api.Controllers;

[ApiController]
[Route("api/servicios")]
public sealed class ServiciosController(ServicioCatalogoServicios servicio) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PoliticasAutorizacion.OperacionDiaria)]
    [ProducesResponseType(typeof(PaginaServiciosDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarActivos(
        [FromQuery] string? buscar,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        var resultado = await servicio.ListarActivosAsync(buscar, pagina, tamanoPagina, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PoliticasAutorizacion.OperacionDiaria)]
    [ProducesResponseType(typeof(ServicioDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObtenerActivo(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await servicio.ObtenerActivoAsync(id, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpGet("administracion")]
    [Authorize(Policy = PoliticasAutorizacion.SoloAdministrador)]
    [ProducesResponseType(typeof(PaginaServiciosDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarAdministracion(
        [FromQuery] string? buscar,
        [FromQuery] bool? activo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        var resultado = await servicio.ListarAdministracionAsync(
            buscar, activo, pagina, tamanoPagina, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpGet("administracion/{id:guid}")]
    [Authorize(Policy = PoliticasAutorizacion.SoloAdministrador)]
    [ProducesResponseType(typeof(ServicioDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObtenerAdministracion(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await servicio.ObtenerAdministracionAsync(id, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpPost]
    [Authorize(Policy = PoliticasAutorizacion.SoloAdministrador)]
    [ProducesResponseType(typeof(ServicioDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Crear(
        [FromBody] GuardarServicioRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.CrearAsync(Convertir(solicitud), cancellationToken);
        return resultado.EsExitoso
            ? CreatedAtAction(nameof(ObtenerAdministracion), new { id = resultado.Valor!.Id }, resultado.Valor)
            : CrearProblema(resultado.Error!);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PoliticasAutorizacion.SoloAdministrador)]
    [ProducesResponseType(typeof(ServicioDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Actualizar(
        Guid id,
        [FromBody] GuardarServicioRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.ActualizarAsync(id, Convertir(solicitud), cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpPatch("{id:guid}/estado")]
    [Authorize(Policy = PoliticasAutorizacion.SoloAdministrador)]
    [ProducesResponseType(typeof(ServicioDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> EstablecerEstado(
        Guid id,
        [FromBody] EstablecerEstadoRegistroRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.EstablecerEstadoAsync(id, solicitud.Activo, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    private static SolicitudServicio Convertir(GuardarServicioRequest solicitud) => new(
        solicitud.Nombre, solicitud.Descripcion, solicitud.Categoria, solicitud.Precio, solicitud.DuracionMinutos);

    private ObjectResult CrearProblema(ErrorDominio error)
    {
        var estado = error.Codigo switch
        {
            "servicio.no_encontrado" => StatusCodes.Status404NotFound,
            "configuracion.no_configurada" => StatusCodes.Status409Conflict,
            var codigo when codigo.EndsWith("conflicto", StringComparison.Ordinal) => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return Problem(statusCode: estado, title: error.Mensaje, type: $"urn:flexpos:error:{error.Codigo}");
    }
}
