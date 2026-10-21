using System.ComponentModel.DataAnnotations;

namespace FlexPos.Api.DTOs;

public sealed record GuardarMetodoPagoRequest(
    string? Nombre,
    bool RequiereReferencia,
    [property: Required] Guid CuentaFinancieraId,
    bool EsEfectivo = false);
