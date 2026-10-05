using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FlexPos.Application.DTOs.Autenticacion;
using FlexPos.Application.Interfaces;
using FlexPos.Domain.Errors;
using Microsoft.IdentityModel.Tokens;

namespace FlexPos.Infrastructure.Services;

public sealed class EmisorTokenAcceso(IConfiguracionTokens configuracion) : IEmisorTokenAcceso
{
    public Resultado<RespuestaAcceso> Crear(UsuarioSesion usuario, DateTimeOffset ahoraUtc)
    {
        var clave = configuracion.ClaveFirma;
        if (string.IsNullOrWhiteSpace(clave) || Encoding.UTF8.GetByteCount(clave) < 32 ||
            string.IsNullOrWhiteSpace(configuracion.Emisor) ||
            string.IsNullOrWhiteSpace(configuracion.Audiencia) ||
            configuracion.MinutosTokenAcceso <= 0)
        {
            return Resultado<RespuestaAcceso>.Fallo(new ErrorDominio(
                "configuracion.jwt_invalida",
                "La configuración JWT está incompleta o no cumple la longitud mínima de clave."));
        }

        var venceUtc = ahoraUtc.AddMinutes(configuracion.MinutosTokenAcceso);
        var reclamos = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new("sello_seguridad", usuario.SelloSeguridad)
        };
        reclamos.AddRange(usuario.Roles.Select(rol => new Claim("role", rol)));
        if (usuario.CambioContrasenaObligatorio)
        {
            reclamos.Add(new Claim("cambio_contrasena_obligatorio", "true"));
        }

        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            configuracion.Emisor,
            configuracion.Audiencia,
            reclamos,
            ahoraUtc.UtcDateTime,
            venceUtc.UtcDateTime,
            credenciales);

        return Resultado<RespuestaAcceso>.Exito(new RespuestaAcceso(
            new JwtSecurityTokenHandler().WriteToken(token),
            venceUtc,
            checked(configuracion.MinutosTokenAcceso * 60)));
    }
}
