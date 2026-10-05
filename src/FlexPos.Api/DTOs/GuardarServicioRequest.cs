namespace FlexPos.Api.DTOs;

public sealed record GuardarServicioRequest(
    string? Nombre,
    string? Descripcion,
    string? Categoria,
    decimal Precio,
    int DuracionMinutos);
