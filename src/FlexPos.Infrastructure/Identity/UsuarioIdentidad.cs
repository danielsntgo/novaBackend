using FlexPos.Domain.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace FlexPos.Infrastructure.Identity;

public sealed class UsuarioIdentidad : IdentityUser<Guid>, IEntidadAuditable
{
    public UsuarioIdentidad()
    {
        Id = Guid.CreateVersion7();
        FechaCreacionUtc = DateTimeOffset.UtcNow;
    }

    public bool Activo { get; set; } = true;
    public bool CambioContrasenaObligatorio { get; set; }
    public DateTimeOffset FechaCreacionUtc { get; private set; }
    public DateTimeOffset? FechaModificacionUtc { get; private set; }
    public Guid? CreadoPorId { get; private set; }
    public Guid? ModificadoPorId { get; private set; }
    public uint Version { get; private set; }

    public void RegistrarCreacion(DateTimeOffset fechaUtc, Guid? usuarioId)
    {
        FechaCreacionUtc = fechaUtc.ToUniversalTime();
        CreadoPorId = usuarioId;
    }

    public void RegistrarModificacion(DateTimeOffset fechaUtc, Guid? usuarioId)
    {
        FechaModificacionUtc = fechaUtc.ToUniversalTime();
        ModificadoPorId = usuarioId;
    }
}
