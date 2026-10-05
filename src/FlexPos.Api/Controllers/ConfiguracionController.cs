using FlexPos.Api.DTOs;
using FlexPos.Application.Authorization;
using FlexPos.Application.DTOs.Configuracion;
using FlexPos.Application.Services;
using FlexPos.Domain.Enums;
using FlexPos.Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlexPos.Api.Controllers;

[ApiController]
[Authorize(Policy = PoliticasAutorizacion.SoloAdministrador)]
[Route("api/configuracion")]
public sealed class ConfiguracionController(ServicioConfiguracion servicio) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ConfiguracionNegocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(CancellationToken cancellationToken)
    {
        var resultado = await servicio.ObtenerAsync(cancellationToken);
        return resultado.EsExitoso
            ? Ok(resultado.Valor)
            : NotFound(CrearProblema(resultado.Error!, StatusCodes.Status404NotFound));
    }

    [HttpPut]
    [ProducesResponseType(typeof(ConfiguracionNegocioDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Guardar(
        [FromBody] GuardarConfiguracionNegocioRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.GuardarAsync(new SolicitudConfiguracionNegocio(
            solicitud.NombreComercial,
            solicitud.RazonSocial,
            solicitud.IdentificacionFiscal,
            solicitud.Direccion,
            solicitud.Telefono,
            solicitud.Correo,
            solicitud.CodigoMoneda,
            solicitud.PermitirVentaSinStock,
            solicitud.ComisionProductosHabilitada,
            solicitud.PermitirSaldosPendientes,
            solicitud.FacturacionHabilitada,
            solicitud.ExigirEmpleadoVentaServicio), cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpGet("impuestos")]
    public async Task<IActionResult> ListarImpuestos(CancellationToken cancellationToken)
    {
        var resultado = await servicio.ListarImpuestosAsync(cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpGet("impuestos/{id:guid}")]
    public async Task<IActionResult> ObtenerImpuesto(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await servicio.ObtenerImpuestoAsync(id, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpPost("impuestos")]
    [ProducesResponseType(typeof(ImpuestoDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CrearImpuesto(
        [FromBody] GuardarImpuestoRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.CrearImpuestoAsync(
            new SolicitudImpuesto(solicitud.Nombre, solicitud.Porcentaje), cancellationToken);
        return resultado.EsExitoso
            ? Created($"/api/configuracion/impuestos/{resultado.Valor!.Id}", resultado.Valor)
            : CrearProblema(resultado.Error!);
    }

    [HttpPut("impuestos/{id:guid}")]
    public async Task<IActionResult> ActualizarImpuesto(
        Guid id,
        [FromBody] GuardarImpuestoRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.ActualizarImpuestoAsync(
            id, new SolicitudImpuesto(solicitud.Nombre, solicitud.Porcentaje), cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpPatch("impuestos/{id:guid}/estado")]
    public async Task<IActionResult> EstablecerEstadoImpuesto(
        Guid id,
        [FromBody] EstablecerEstadoUsuarioRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.EstablecerEstadoImpuestoAsync(id, solicitud.Activo, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpGet("metodos-pago")]
    public async Task<IActionResult> ListarMetodosPago(CancellationToken cancellationToken)
    {
        var resultado = await servicio.ListarMetodosPagoAsync(cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpGet("metodos-pago/{id:guid}")]
    public async Task<IActionResult> ObtenerMetodoPago(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await servicio.ObtenerMetodoPagoAsync(id, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpPost("metodos-pago")]
    [ProducesResponseType(typeof(MetodoPagoDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CrearMetodoPago(
        [FromBody] GuardarMetodoPagoRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.CrearMetodoPagoAsync(
            new SolicitudMetodoPago(solicitud.Nombre, solicitud.RequiereReferencia, solicitud.EsEfectivo), cancellationToken);
        return resultado.EsExitoso
            ? Created($"/api/configuracion/metodos-pago/{resultado.Valor!.Id}", resultado.Valor)
            : CrearProblema(resultado.Error!);
    }

    [HttpPut("metodos-pago/{id:guid}")]
    public async Task<IActionResult> ActualizarMetodoPago(
        Guid id,
        [FromBody] GuardarMetodoPagoRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.ActualizarMetodoPagoAsync(
            id, new SolicitudMetodoPago(solicitud.Nombre, solicitud.RequiereReferencia, solicitud.EsEfectivo), cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpPatch("metodos-pago/{id:guid}/estado")]
    public async Task<IActionResult> EstablecerEstadoMetodoPago(
        Guid id,
        [FromBody] EstablecerEstadoUsuarioRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicio.EstablecerEstadoMetodoPagoAsync(id, solicitud.Activo, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpGet("numeraciones-documento")]
    public async Task<IActionResult> ListarNumeraciones(CancellationToken cancellationToken)
    {
        var resultado = await servicio.ListarNumeracionesAsync(cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    [HttpPost("numeraciones-documento")]
    public async Task<IActionResult> CrearNumeracion(
        [FromBody] CrearNumeracionDocumentoRequest solicitud,
        CancellationToken cancellationToken)
    {
        if (!IntentarLeerTipoDocumento(solicitud.TipoDocumento, out var tipo))
        {
            return BadRequest(CrearProblema(new ErrorDominio(
                "numeracion.tipo_invalido",
                "El tipo debe ser Factura o Comprobante.")));
        }

        var resultado = await servicio.CrearNumeracionAsync(
            new SolicitudNumeracionDocumento(tipo, solicitud.Prefijo, solicitud.SiguienteNumero),
            cancellationToken);
        return resultado.EsExitoso
            ? Created("/api/configuracion/numeraciones-documento", resultado.Valor)
            : CrearProblema(resultado.Error!);
    }

    [HttpPut("numeraciones-documento/{tipoDocumento}")]
    public async Task<IActionResult> ActualizarPrefijo(
        string tipoDocumento,
        [FromBody] ActualizarPrefijoDocumentoRequest solicitud,
        CancellationToken cancellationToken)
    {
        if (!IntentarLeerTipoDocumento(tipoDocumento, out var tipo))
        {
            return BadRequest(CrearProblema(new ErrorDominio(
                "numeracion.tipo_invalido",
                "El tipo debe ser Factura o Comprobante.")));
        }

        var resultado = await servicio.ActualizarPrefijoAsync(tipo, solicitud.Prefijo, cancellationToken);
        return resultado.EsExitoso ? Ok(resultado.Valor) : CrearProblema(resultado.Error!);
    }

    private ObjectResult CrearProblema(ErrorDominio error)
    {
        var estado = error.Codigo.EndsWith("_no_encontrado", StringComparison.Ordinal)
            ? StatusCodes.Status404NotFound
            : error.Codigo == "configuracion.no_configurada"
                ? StatusCodes.Status409Conflict
                : error.Codigo.EndsWith("conflicto", StringComparison.Ordinal)
                    ? StatusCodes.Status409Conflict
                    : StatusCodes.Status400BadRequest;

        return CrearProblema(error, estado);
    }

    private ObjectResult CrearProblema(ErrorDominio error, int estado) => Problem(
        statusCode: estado,
        title: error.Mensaje,
        type: $"urn:flexpos:error:{error.Codigo}");

    private static bool IntentarLeerTipoDocumento(string? valor, out TipoDocumentoVenta tipo)
    {
        if (string.Equals(valor, nameof(TipoDocumentoVenta.Factura), StringComparison.OrdinalIgnoreCase))
        {
            tipo = TipoDocumentoVenta.Factura;
            return true;
        }

        if (string.Equals(valor, nameof(TipoDocumentoVenta.Comprobante), StringComparison.OrdinalIgnoreCase))
        {
            tipo = TipoDocumentoVenta.Comprobante;
            return true;
        }

        tipo = default;
        return false;
    }
}
