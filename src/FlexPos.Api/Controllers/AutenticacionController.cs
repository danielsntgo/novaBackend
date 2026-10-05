using FlexPos.Api.DTOs;
using FlexPos.Application.Authorization;
using FlexPos.Application.DTOs.Autenticacion;
using FlexPos.Application.Interfaces;
using FlexPos.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FlexPos.Api.Controllers;

[ApiController]
[Route("api/autenticacion")]
public sealed class AutenticacionController(
    ServicioAutenticacion autenticacion,
    IOptions<ConfiguracionCookieRefresh> opcionesCookie,
    IUsuarioActual usuarioActual) : ControllerBase
{
    private const string CabeceraAntiCsrf = "X-Requested-With";
    private const string ValorAntiCsrf = "XMLHttpRequest";

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RespuestaAcceso), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> IniciarSesion(
        [FromBody] InicioSesionRequest solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await autenticacion.IniciarSesionAsync(
            new SolicitudInicioSesion(solicitud.Correo ?? string.Empty, solicitud.Contrasena ?? string.Empty),
            cancellationToken);

        if (!resultado.EsExitoso)
        {
            return CrearProblema(resultado.Error!);
        }

        FijarCookie(resultado.Valor!);
        return Ok(resultado.Valor!.Acceso);
    }

    [HttpPost("refrescar")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RespuestaAcceso), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Renovar(CancellationToken cancellationToken)
    {
        if (!TieneCabeceraAntiCsrf())
        {
            return Problem(statusCode: StatusCodes.Status403Forbidden, title: "Solicitud no permitida.");
        }

        var cookie = opcionesCookie.Value;
        var resultado = await autenticacion.RenovarAsync(
            Request.Cookies[cookie.Nombre],
            cancellationToken);

        if (!resultado.EsExitoso)
        {
            BorrarCookie(cookie);
            return CrearProblema(resultado.Error!);
        }

        FijarCookie(resultado.Valor!);
        return Ok(resultado.Valor!.Acceso);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CerrarSesion(CancellationToken cancellationToken)
    {
        var cookie = opcionesCookie.Value;
        if (!TieneCabeceraAntiCsrf())
        {
            return Problem(statusCode: StatusCodes.Status403Forbidden, title: "Solicitud no permitida.");
        }

        await autenticacion.CerrarSesionAsync(Request.Cookies[cookie.Nombre], cancellationToken);
        BorrarCookie(cookie);
        return NoContent();
    }

    [HttpPost("cambiar-contrasena")]
    [Authorize(Policy = PoliticasAutorizacion.UsuarioAutenticado)]
    [ProducesResponseType(typeof(RespuestaAcceso), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CambiarContrasena(
        [FromBody] CambiarContrasenaRequest solicitud,
        CancellationToken cancellationToken)
    {
        var usuarioId = usuarioActual.ObtenerId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var resultado = await autenticacion.CambiarContrasenaAsync(
            usuarioId.Value,
            solicitud.ContrasenaActual,
            solicitud.ContrasenaNueva,
            cancellationToken);

        if (!resultado.EsExitoso)
        {
            return CrearProblema(resultado.Error!);
        }

        FijarCookie(resultado.Valor!);
        return Ok(resultado.Valor!.Acceso);
    }

    private bool TieneCabeceraAntiCsrf() =>
        string.Equals(Request.Headers[CabeceraAntiCsrf], ValorAntiCsrf, StringComparison.Ordinal);

    private void FijarCookie(SesionEmitida sesion)
    {
        var cookie = opcionesCookie.Value;
        Response.Cookies.Append(cookie.Nombre, sesion.TokenRenovacion, new CookieOptions
        {
            HttpOnly = true,
            Secure = cookie.Segura,
            SameSite = cookie.MismoSitio,
            Path = cookie.Ruta,
            Expires = sesion.RenovacionExpiraUtc,
            IsEssential = true
        });
    }

    private void BorrarCookie(ConfiguracionCookieRefresh cookie) =>
        Response.Cookies.Delete(cookie.Nombre, new CookieOptions
        {
            HttpOnly = true,
            Secure = cookie.Segura,
            SameSite = cookie.MismoSitio,
            Path = cookie.Ruta
        });

    private ObjectResult CrearProblema(FlexPos.Domain.Errors.ErrorDominio error)
    {
        var estado = error.Codigo switch
        {
            "autenticacion.no_autorizado" => StatusCodes.Status401Unauthorized,
            "configuracion.jwt_invalida" => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status400BadRequest
        };

        return Problem(statusCode: estado, title: error.Mensaje, type: $"urn:flexpos:error:{error.Codigo}");
    }
}
