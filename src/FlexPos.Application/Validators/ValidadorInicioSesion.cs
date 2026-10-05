using System.Net.Mail;
using FlexPos.Application.DTOs.Autenticacion;
using FlexPos.Domain.Errors;

namespace FlexPos.Application.Validators;

public sealed class ValidadorInicioSesion
{
    public ErrorDominio? Validar(SolicitudInicioSesion solicitud)
    {
        if (string.IsNullOrWhiteSpace(solicitud.Correo) ||
            string.IsNullOrWhiteSpace(solicitud.Contrasena))
        {
            return new ErrorDominio(
                "autenticacion.solicitud_invalida",
                "El correo y la contraseña son obligatorios.");
        }

        try
        {
            _ = new MailAddress(solicitud.Correo);
        }
        catch (FormatException)
        {
            return new ErrorDominio(
                "autenticacion.correo_invalido",
                "El correo no tiene un formato válido.");
        }

        return null;
    }
}
