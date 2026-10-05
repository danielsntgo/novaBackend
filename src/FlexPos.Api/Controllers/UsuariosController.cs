using FlexPos.Api.DTOs;
using FlexPos.Application.Authorization;
using FlexPos.Application.DTOs.Usuarios;
using FlexPos.Application.Services;
using FlexPos.Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlexPos.Api.Controllers;

[ApiController]
[Authorize(Policy = PoliticasAutorizacion.SoloAdministrador)]
[Route("api/usuarios")]
public sealed class UsuariosController(ServicioUsuarios servicio) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginaUsuariosDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] string? buscar,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        var resultado = await servicio.ListarRecepcionistasAsync(
            buscar, pagina, tamanoPagina, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UsuarioAdministradoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await servicio.ObtenerRecepcionistaAsync(id, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpPost("recepcionistas")]
    [ProducesResponseType(typeof(RecepcionistaCreadaDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CrearRecepcionista(
        [FromBody] CrearRecepcionistaRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.CrearRecepcionistaAsync(solicitud.Correo, cancellationToken);
        return resultado.EsExitoso
            ? CreatedAtAction(nameof(Obtener), new { id = resultado.Valor!.Usuario.Id }, resultado.Valor)
            : CrearProblema(resultado.Error!);
    }

    [HttpPatch("{id:guid}/estado")]
    [ProducesResponseType(typeof(UsuarioAdministradoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> EstablecerEstado(
        Guid id,
        [FromBody] EstablecerEstadoUsuarioRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.EstablecerEstadoAsync(id, solicitud.Activo, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    private ObjectResult CrearProblema(ErrorDominio error)
    {
        var estado = error.Codigo switch
        {
            "usuario.no_encontrado" => StatusCodes.Status404NotFound,
            "usuario.correo_duplicado" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return Problem(statusCode: estado, title: error.Mensaje, type: $"urn:flexpos:error:{error.Codigo}");
    }
}
