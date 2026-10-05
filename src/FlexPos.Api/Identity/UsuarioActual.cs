using System.Security.Claims;
using FlexPos.Application.Interfaces;

namespace FlexPos.Api.Identity;

public sealed class UsuarioActual(IHttpContextAccessor contexto) : IUsuarioActual
{
    public Guid? ObtenerId()
    {
        var identificador = contexto.HttpContext?.User.FindFirstValue("sub");
        return Guid.TryParse(identificador, out var usuarioId) ? usuarioId : null;
    }
}
