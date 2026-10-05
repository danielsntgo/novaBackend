namespace FlexPos.Domain.ValueObjects;

public sealed record AjusteComisionCalculado(
    bool TieneAjuste,
    decimal BaseCalculo,
    decimal Cantidad,
    decimal Importe,
    bool RevierteTodo);
