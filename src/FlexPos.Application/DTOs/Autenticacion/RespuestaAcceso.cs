namespace FlexPos.Application.DTOs.Autenticacion;

public sealed record RespuestaAcceso(
    string TokenAcceso,
    DateTimeOffset ExpiraUtc,
    int SegundosVigencia);
