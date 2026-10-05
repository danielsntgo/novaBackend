namespace FlexPos.Api.DTOs;

public sealed class ConfiguracionCookieRefresh
{
    public string Nombre { get; set; } = "flexpos_refresh";
    public string Ruta { get; set; } = "/api/autenticacion";
    public bool Segura { get; set; } = true;
    public SameSiteMode MismoSitio { get; set; } = SameSiteMode.Lax;
}
