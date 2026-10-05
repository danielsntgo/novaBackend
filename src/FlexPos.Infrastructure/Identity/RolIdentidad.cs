using FlexPos.Domain.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace FlexPos.Infrastructure.Identity;

public sealed class RolIdentidad : IdentityRole<Guid>, IEntidadAuditable
{
    public RolIdentidad()
    {
        Id = Guid.CreateVersion7();
        FechaCreacionUtc = DateTimeOffset.UtcNow;
    }

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
