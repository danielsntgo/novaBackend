using FlexPos.Api.DTOs;
using FlexPos.Application.Authorization;
using FlexPos.Application.DTOs.Inventario;
using FlexPos.Application.Services;
using FlexPos.Domain.Enums;
using FlexPos.Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlexPos.Api.Controllers;

[ApiController]
[Authorize(Policy = PoliticasAutorizacion.SoloAdministrador)]
[Route("api/inventario")]
public sealed class InventarioController(ServicioInventario servicio) : ControllerBase
{
    [HttpGet("articulos")]
    [ProducesResponseType(typeof(PaginaArticulosInventarioDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarArticulos(
        [FromQuery] string? buscar,
        [FromQuery] string? tipo,
        [FromQuery] bool? activo = true,
        [FromQuery] bool soloBajoMinimo = false,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        TipoArticuloInventario? tipoArticulo = null;
        if (!string.IsNullOrWhiteSpace(tipo))
        {
            if (!Enum.TryParse(tipo, true, out TipoArticuloInventario valor) || !Enum.IsDefined(valor))
            {
                return CrearProblema(new ErrorDominio("inventario.busqueda_invalida", "El tipo debe ser Producto o Insumo."));
            }

            tipoArticulo = valor;
        }

        var resultado = await servicio.ListarArticulosAsync(
            buscar, tipoArticulo, activo, soloBajoMinimo, pagina, tamanoPagina, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpGet("articulos/{id:guid}")]
    [ProducesResponseType(typeof(ArticuloInventarioDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObtenerArticulo(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await servicio.ObtenerArticuloAsync(id, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpPost("articulos")]
    [ProducesResponseType(typeof(ArticuloInventarioDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CrearArticulo(
        [FromBody] GuardarArticuloInventarioRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.CrearArticuloAsync(Convertir(solicitud), cancellationToken);
        return resultado.EsExitoso
            ? CreatedAtAction(nameof(ObtenerArticulo), new { id = resultado.Valor!.Id }, resultado.Valor)
            : CrearProblema(resultado.Error!);
    }

    [HttpPut("articulos/{id:guid}")]
    [ProducesResponseType(typeof(ArticuloInventarioDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ActualizarArticulo(
        Guid id,
        [FromBody] GuardarArticuloInventarioRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.ActualizarArticuloAsync(id, Convertir(solicitud), cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpPatch("articulos/{id:guid}/estado")]
    [ProducesResponseType(typeof(ArticuloInventarioDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> EstablecerEstadoArticulo(
        Guid id,
        [FromBody] EstablecerEstadoRegistroRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.EstablecerEstadoAsync(id, solicitud.Activo, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpPost("articulos/{id:guid}/entradas-ajuste")]
    [ProducesResponseType(typeof(MovimientoInventarioDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RegistrarEntrada(
        Guid id,
        [FromBody] RegistrarEntradaInventarioRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.RegistrarEntradaAsync(
            id, new SolicitudEntradaInventario(solicitud.Cantidad, solicitud.CostoUnitario, solicitud.Motivo),
            cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpPost("articulos/{id:guid}/salidas-ajuste")]
    [ProducesResponseType(typeof(MovimientoInventarioDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RegistrarSalida(
        Guid id,
        [FromBody] RegistrarSalidaInventarioRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.RegistrarSalidaAsync(
            id, new SolicitudSalidaInventario(solicitud.Cantidad, solicitud.Motivo), cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpGet("movimientos")]
    [ProducesResponseType(typeof(PaginaMovimientosInventarioDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarMovimientos(
        [FromQuery] Guid? articuloId,
        [FromQuery] DateTimeOffset? desdeUtc,
        [FromQuery] DateTimeOffset? hastaUtc,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        var resultado = await servicio.ListarMovimientosAsync(
            articuloId, desdeUtc, hastaUtc, pagina, tamanoPagina, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    private static SolicitudArticuloInventario Convertir(GuardarArticuloInventarioRequest solicitud)
    {
        var tipo = solicitud.Tipo?.Trim().ToLowerInvariant() switch
        {
            "producto" => TipoArticuloInventario.Producto,
            "insumo" => TipoArticuloInventario.Insumo,
            _ => (TipoArticuloInventario)(-1)
        };
        return new SolicitudArticuloInventario(
            solicitud.Codigo, solicitud.Nombre, tipo, solicitud.UnidadBase, solicitud.ManejaFraccion,
            solicitud.Categoria, solicitud.CantidadMinima, solicitud.PrecioVenta);
    }

    private ObjectResult CrearProblema(ErrorDominio error)
    {
        var estado = error.Codigo switch
        {
            "inventario.articulo_no_encontrado" => StatusCodes.Status404NotFound,
            "inventario.conflicto" or "inventario.unidad_inmutable" or "inventario.tipo_inmutable" or
                "inventario.stock_insuficiente" => StatusCodes.Status409Conflict,
            var codigo when codigo.EndsWith("conflicto", StringComparison.Ordinal) => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };
        return Problem(statusCode: estado, title: error.Mensaje, type: $"urn:flexpos:error:{error.Codigo}");
    }
}
