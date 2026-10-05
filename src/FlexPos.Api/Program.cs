using System.Text;
using FlexPos.Api.DTOs;
using FlexPos.Api.Identity;
using FlexPos.Api.Middleware;
using FlexPos.Application;
using FlexPos.Application.Authorization;
using FlexPos.Application.Interfaces;
using FlexPos.Infrastructure;
using FlexPos.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioActual, UsuarioActual>();
builder.Services.Configure<ConfiguracionCookieRefresh>(
    builder.Configuration.GetSection("Autenticacion:CookieRefresh"));

var configuracionJwt = builder.Configuration.GetSection("Autenticacion:Jwt");
var claveFirma = configuracionJwt["ClaveFirma"];
var emisor = configuracionJwt["Emisor"] ?? "FlexPos";
var audiencia = configuracionJwt["Audiencia"] ?? "FlexPos.Api";
var origenesPermitidos = builder.Configuration
    .GetSection("Cors:OrigenesPermitidos")
    .Get<string[]>() ?? [];

builder.Services.AddCors(opciones => opciones.AddPolicy("Frontend", politica =>
{
    politica.SetIsOriginAllowed(origen => origenesPermitidos.Contains(
            origen,
            StringComparer.OrdinalIgnoreCase))
        .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
        .WithHeaders("Content-Type", "Authorization", "X-Requested-With")
        .AllowCredentials();
}));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.MapInboundClaims = false;
        opciones.Events = new JwtBearerEvents
        {
            OnTokenValidated = async contexto =>
            {
                var principal = contexto.Principal;
                var idTexto = principal?.FindFirst("sub")?.Value;
                var selloToken = principal?.FindFirst("sello_seguridad")?.Value;
                if (!Guid.TryParse(idTexto, out var usuarioId) || string.IsNullOrWhiteSpace(selloToken))
                {
                    contexto.Fail("El token no contiene una identidad válida.");
                    return;
                }

                var identidad = contexto.HttpContext.RequestServices.GetRequiredService<IIdentidadUsuarios>();
                var sesion = await identidad.ObtenerSesionAsync(usuarioId, contexto.HttpContext.RequestAborted);
                var cambioContrasenaToken = principal!.HasClaim("cambio_contrasena_obligatorio", "true");
                var rolesToken = principal.FindAll("role").Select(claim => claim.Value).Order().ToArray();
                var rolesVigentes = sesion?.Roles.Order().ToArray();
                if (sesion is null ||
                    !string.Equals(sesion.SelloSeguridad, selloToken, StringComparison.Ordinal) ||
                    sesion.CambioContrasenaObligatorio != cambioContrasenaToken ||
                    rolesVigentes is null || !rolesToken.SequenceEqual(rolesVigentes, StringComparer.Ordinal))
                {
                    contexto.Fail("La sesión ya no está vigente.");
                }
            }
        };
        opciones.IncludeErrorDetails = false;
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = emisor,
            ValidateAudience = true,
            ValidAudience = audiencia,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = string.IsNullOrWhiteSpace(claveFirma)
                ? null
                : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(claveFirma)),
            NameClaimType = "sub",
            RoleClaimType = "role",
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

var politicaAccesoNormal = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .RequireRole(RolesSistema.Administrador)
    .RequireAssertion(context => !context.User.HasClaim("cambio_contrasena_obligatorio", "true"))
    .Build();

builder.Services.AddAuthorization(opciones =>
{
    opciones.FallbackPolicy = politicaAccesoNormal;
    opciones.AddPolicy(PoliticasAutorizacion.SoloAdministrador, politica =>
    {
        politica.RequireAuthenticatedUser();
        politica.RequireRole(RolesSistema.Administrador);
        politica.RequireAssertion(context => !context.User.HasClaim("cambio_contrasena_obligatorio", "true"));
    });
    opciones.AddPolicy(PoliticasAutorizacion.OperacionDiaria, politica =>
    {
        politica.RequireAuthenticatedUser();
        politica.RequireRole(RolesSistema.Administrador, RolesSistema.Recepcionista);
        politica.RequireAssertion(context => !context.User.HasClaim("cambio_contrasena_obligatorio", "true"));
    });
    opciones.AddPolicy(PoliticasAutorizacion.UsuarioAutenticado, politica => politica.RequireAuthenticatedUser());
});

var app = builder.Build();

app.UseMiddleware<MiddlewareErrores>();
app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.MapControllers();

if (!string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("FlexPos")))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<InicializadorDatos>().InicializarAsync();
}
else
{
    app.Logger.LogInformation(
        "PostgreSQL no está configurado. La API inicia sin base de datos; configure ConnectionStrings:FlexPos y aplique las migraciones para habilitar persistencia.");
}

app.Run();

public partial class Program;
