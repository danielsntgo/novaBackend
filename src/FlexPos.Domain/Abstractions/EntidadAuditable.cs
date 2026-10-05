namespace FlexPos.Domain.Abstractions;

public interface IEntidadAuditable
{
    DateTimeOffset FechaCreacionUtc { get; }
    DateTimeOffset? FechaModificacionUtc { get; }
    Guid? CreadoPorId { get; }
    Guid? ModificadoPorId { get; }

    void RegistrarCreacion(DateTimeOffset fechaUtc, Guid? usuarioId);
    void RegistrarModificacion(DateTimeOffset fechaUtc, Guid? usuarioId);
}

public abstract class EntidadAuditable : IEntidadAuditable
{
    protected EntidadAuditable()
    {
        Id = Guid.CreateVersion7();
        FechaCreacionUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; protected set; }
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
