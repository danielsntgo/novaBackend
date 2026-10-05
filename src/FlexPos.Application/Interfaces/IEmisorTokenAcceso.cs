using FlexPos.Application.DTOs.Autenticacion;
using FlexPos.Domain.Errors;

namespace FlexPos.Application.Interfaces;

public interface IEmisorTokenAcceso
{
    Resultado<RespuestaAcceso> Crear(UsuarioSesion usuario, DateTimeOffset ahoraUtc);
}
