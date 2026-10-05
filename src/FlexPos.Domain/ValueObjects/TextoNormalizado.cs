using System.Globalization;
using System.Text;

namespace FlexPos.Domain.ValueObjects;

public static class TextoNormalizado
{
    public static string? Normalizar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var descompuesto = valor.Trim().Normalize(NormalizationForm.FormD);
        var resultado = new StringBuilder(descompuesto.Length);
        foreach (var caracter in descompuesto)
        {
            var categoria = CharUnicodeInfo.GetUnicodeCategory(caracter);
            if (categoria is not (UnicodeCategory.NonSpacingMark or
                UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark))
            {
                resultado.Append(caracter);
            }
        }

        return string.Join(' ', resultado.ToString()
            .Normalize(NormalizationForm.FormC)
            .ToUpperInvariant()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
