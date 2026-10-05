namespace FlexPos.Application.Interfaces;

public interface IConfiguracionTokens
{
    int MinutosTokenAcceso { get; }
    int DiasTokenRenovacion { get; }
    string Emisor { get; }
    string Audiencia { get; }
    string? ClaveFirma { get; }
}
