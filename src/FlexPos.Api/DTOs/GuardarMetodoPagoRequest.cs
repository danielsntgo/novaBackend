namespace FlexPos.Api.DTOs;

public sealed record GuardarMetodoPagoRequest(string? Nombre, bool RequiereReferencia, bool EsEfectivo = false);
