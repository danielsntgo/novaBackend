using FlexPos.Api.DTOs;
using FlexPos.Application.Authorization;
using FlexPos.Application.DTOs.Clientes;
using FlexPos.Application.Services;
using FlexPos.Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlexPos.Api.Controllers;

[ApiController]
[Authorize(Policy = PoliticasAutorizacion.OperacionDiaria)]
[Route("api/clientes")]
public sealed class ClientesController(ServicioClientes servicio) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginaClientesDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] string? buscar,
        [FromQuery] bool? activo = true,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        var resultado = await servicio.ListarAsync(buscar, activo, pagina, tamanoPagina, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await servicio.ObtenerAsync(id, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Crear(
        [FromBody] GuardarClienteRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.CrearAsync(
            new SolicitudCliente(solicitud.Nombre, solicitud.Documento, solicitud.Telefono, solicitud.Correo),
            cancellationToken);
        return resultado.EsExitoso
            ? CreatedAtAction(nameof(Obtener), new { id = resultado.Valor!.Id }, resultado.Valor)
            : CrearProblema(resultado.Error!);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Actualizar(
        Guid id,
        [FromBody] GuardarClienteRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.ActualizarAsync(
            id, new SolicitudCliente(solicitud.Nombre, solicitud.Documento, solicitud.Telefono, solicitud.Correo),
            cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpPatch("{id:guid}/estado")]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> EstablecerEstado(
        Guid id,
        [FromBody] EstablecerEstadoRegistroRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.EstablecerEstadoAsync(id, solicitud.Activo, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    private ObjectResult CrearProblema(ErrorDominio error)
    {
        var estado = error.Codigo switch
        {
            "cliente.no_encontrado" => StatusCodes.Status404NotFound,
            var codigo when codigo.EndsWith("conflicto", StringComparison.Ordinal) => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return Problem(statusCode: estado, title: error.Mensaje, type: $"urn:flexpos:error:{error.Codigo}");
    }
}
