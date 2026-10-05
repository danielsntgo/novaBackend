using FlexPos.Api.DTOs;
using FlexPos.Application.Authorization;
using FlexPos.Application.DTOs.Proveedores;
using FlexPos.Application.Services;
using FlexPos.Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlexPos.Api.Controllers;

[ApiController]
[Authorize(Policy = PoliticasAutorizacion.SoloAdministrador)]
[Route("api/proveedores")]
public sealed class ProveedoresController(ServicioProveedores servicio) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginaProveedoresDto), StatusCodes.Status200OK)]
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
    [ProducesResponseType(typeof(ProveedorDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await servicio.ObtenerAsync(id, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProveedorDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Crear(
        [FromBody] GuardarProveedorRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.CrearAsync(Convertir(solicitud), cancellationToken);
        return resultado.EsExitoso
            ? CreatedAtAction(nameof(Obtener), new { id = resultado.Valor!.Id }, resultado.Valor)
            : CrearProblema(resultado.Error!);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProveedorDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Actualizar(
        Guid id,
        [FromBody] GuardarProveedorRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.ActualizarAsync(id, Convertir(solicitud), cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpPatch("{id:guid}/estado")]
    [ProducesResponseType(typeof(ProveedorDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> EstablecerEstado(
        Guid id,
        [FromBody] EstablecerEstadoRegistroRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.EstablecerEstadoAsync(id, solicitud.Activo, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    private static SolicitudProveedor Convertir(GuardarProveedorRequest solicitud) => new(
        solicitud.Nombre, solicitud.IdentificacionFiscal, solicitud.Telefono, solicitud.Correo);

    private ObjectResult CrearProblema(ErrorDominio error)
    {
        var estado = error.Codigo == "proveedor.no_encontrado"
            ? StatusCodes.Status404NotFound
            : error.Codigo.EndsWith("conflicto", StringComparison.Ordinal)
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status400BadRequest;
        return Problem(statusCode: estado, title: error.Mensaje, type: $"urn:flexpos:error:{error.Codigo}");
    }
}
