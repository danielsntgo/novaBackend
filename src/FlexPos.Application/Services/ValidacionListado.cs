namespace FlexPos.Application.Services;

internal static class ValidacionListado
{
    public static bool EsValida(string? buscar, int pagina, int tamanoPagina) =>
        pagina >= 1 && tamanoPagina is >= 1 and <= 100 &&
        (pagina - 1L) * tamanoPagina <= int.MaxValue &&
        (buscar?.Trim().Length ?? 0) <= 120;

    public static int CalcularOmitir(int pagina, int tamanoPagina) =>
        checked((pagina - 1) * tamanoPagina);
}
