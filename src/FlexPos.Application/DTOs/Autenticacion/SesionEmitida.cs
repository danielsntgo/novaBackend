namespace FlexPos.Application.DTOs.Autenticacion;

public sealed record SesionEmitida(
    RespuestaAcceso Acceso,
    string TokenRenovacion,
    DateTimeOffset RenovacionExpiraUtc);
