using FlexPos.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FlexPos.Infrastructure.Services;

public sealed class ConfiguracionTokens(IConfiguration configuracion) : IConfiguracionTokens
{
    public int MinutosTokenAcceso => LeerEntero("Autenticacion:Jwt:MinutosTokenAcceso", 15);
    public int DiasTokenRenovacion => LeerEntero("Autenticacion:Jwt:DiasTokenRenovacion", 7);
    public string Emisor => configuracion["Autenticacion:Jwt:Emisor"] ?? string.Empty;
    public string Audiencia => configuracion["Autenticacion:Jwt:Audiencia"] ?? string.Empty;
    public string? ClaveFirma => configuracion["Autenticacion:Jwt:ClaveFirma"];

    private int LeerEntero(string clave, int valorPredeterminado) =>
        int.TryParse(configuracion[clave], out var valor) ? valor : valorPredeterminado;
}
