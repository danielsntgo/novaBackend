using FlexPos.Domain.Errors;

namespace FlexPos.Domain.ValueObjects;

public sealed record Dinero
{
    private const decimal ImporteMaximo = 9_999_999_999_999_999.99m;

    private Dinero(decimal importe, string codigoMoneda)
    {
        Importe = importe;
        CodigoMoneda = codigoMoneda;
    }

    public decimal Importe { get; }
    public string CodigoMoneda { get; }

    public static Resultado<Dinero> Crear(decimal importe, string? codigoMoneda)
    {
        if (importe > ImporteMaximo || importe < -ImporteMaximo || decimal.Round(importe, 2) != importe)
        {
            return Resultado<Dinero>.Fallo(new ErrorDominio(
                "dinero.importe_invalido",
                "El importe debe caber en numeric(18,2)."));
        }

        var codigo = codigoMoneda?.Trim().ToUpperInvariant();
        if (codigo is null || codigo.Length != 3 || codigo.Any(caracter => caracter is < 'A' or > 'Z'))
        {
            return Resultado<Dinero>.Fallo(new ErrorDominio(
                "dinero.moneda_invalida",
                "La moneda debe ser un código ISO 4217 de tres letras."));
        }

        return Resultado<Dinero>.Exito(new Dinero(importe, codigo));
    }

    public Resultado<Dinero> Sumar(Dinero otro)
    {
        if (CodigoMoneda != otro.CodigoMoneda)
        {
            return Resultado<Dinero>.Fallo(new ErrorDominio(
                "dinero.moneda_diferente",
                "No se pueden sumar importes de monedas distintas."));
        }

        try
        {
            return Crear(Importe + otro.Importe, CodigoMoneda);
        }
        catch (OverflowException)
        {
            return Resultado<Dinero>.Fallo(new ErrorDominio(
                "dinero.importe_invalido",
                "El resultado excede la precisión permitida."));
        }
    }

    public Resultado<Dinero> Restar(Dinero otro)
    {
        if (CodigoMoneda != otro.CodigoMoneda)
        {
            return Resultado<Dinero>.Fallo(new ErrorDominio(
                "dinero.moneda_diferente",
                "No se pueden restar importes de monedas distintas."));
        }

        try
        {
            return Crear(Importe - otro.Importe, CodigoMoneda);
        }
        catch (OverflowException)
        {
            return Resultado<Dinero>.Fallo(new ErrorDominio(
                "dinero.importe_invalido",
                "El resultado excede la precisión permitida."));
        }
    }
}
