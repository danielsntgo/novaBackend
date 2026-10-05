using System.Net;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using FlexPos.Application.DTOs.Autenticacion;
using FlexPos.Application.DTOs.Usuarios;
using FlexPos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FlexPos.Tests.Integracion;

public sealed class AutenticacionPostgreSqlTests
{
    [HechoPostgreSql]
    public async Task CompletarFlujoDeContrasenaTemporalYReutilizarRefreshRevocaLaFamilia()
    {
        var conexionRuntime = Environment.GetEnvironmentVariable("ConnectionStrings__FlexPosTest")!;
        var conexionMigraciones = Environment.GetEnvironmentVariable("ConnectionStrings__FlexPosTestMigraciones")!;
        var opciones = new DbContextOptionsBuilder<FlexPosDbContext>()
            .UseNpgsql(conexionMigraciones, postgres => postgres.MigrationsHistoryTable("historial_migraciones", "flexpos"))
            .Options;

        await using (var contexto = new FlexPosDbContext(opciones))
        {
            await contexto.Database.MigrateAsync();
        }

        var correo = $"prueba-{Guid.NewGuid():N}@flexpos.invalid";
        await using var fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuracion) => configuracion.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:FlexPos"] = conexionRuntime,
                    ["AdministradorInicial:Correo"] = correo,
                    ["AdministradorInicial:Contrasena"] = "PruebaSegura!2026",
                    ["Autenticacion:Jwt:ClaveFirma"] = "clave-de-pruebas-de-al-menos-treinta-y-dos-bytes",
                    ["Autenticacion:Jwt:Emisor"] = "FlexPos.Tests",
                    ["Autenticacion:Jwt:Audiencia"] = "FlexPos.Tests",
                    ["Autenticacion:CookieRefresh:Nombre"] = "flexpos_refresh",
                    ["Autenticacion:CookieRefresh:Ruta"] = "/api/autenticacion",
                    ["Autenticacion:CookieRefresh:Segura"] = "true",
                    ["Autenticacion:CookieRefresh:MismoSitio"] = "None"
                }));
        });

        using var cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });

        var login = await cliente.PostAsJsonAsync("/api/autenticacion/login", new
        {
            correo,
            contrasena = "PruebaSegura!2026"
        });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.DoesNotContain("tokenRenovacion", await login.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        var accesoAdministrador = await login.Content.ReadFromJsonAsync<RespuestaAcceso>();
        Assert.NotNull(accesoAdministrador);
        var tokenAnterior = LeerCookie(login);

        var correoRecepcionista = $"recepcionista-{Guid.NewGuid():N}@flexpos.invalid";
        using var alta = new HttpRequestMessage(HttpMethod.Post, "/api/usuarios/recepcionistas")
        {
            Content = JsonContent.Create(new { correo = correoRecepcionista })
        };
        alta.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", accesoAdministrador.TokenAcceso);
        using var respuestaAlta = await cliente.SendAsync(alta);

        Assert.Equal(HttpStatusCode.Created, respuestaAlta.StatusCode);
        var recepcionistaCreada = await respuestaAlta.Content.ReadFromJsonAsync<RecepcionistaCreadaDto>();
        Assert.NotNull(recepcionistaCreada);
        Assert.True(recepcionistaCreada.Usuario.CambioContrasenaObligatorio);
        Assert.NotEmpty(recepcionistaCreada.ContrasenaTemporal);

        var loginTemporal = await cliente.PostAsJsonAsync("/api/autenticacion/login", new
        {
            correo = correoRecepcionista,
            contrasena = recepcionistaCreada.ContrasenaTemporal
        });
        Assert.Equal(HttpStatusCode.OK, loginTemporal.StatusCode);
        var accesoTemporal = await loginTemporal.Content.ReadFromJsonAsync<RespuestaAcceso>();
        Assert.NotNull(accesoTemporal);
        Assert.Equal("true", new JwtSecurityTokenHandler()
            .ReadJwtToken(accesoTemporal.TokenAcceso)
            .Claims.Single(claim => claim.Type == "cambio_contrasena_obligatorio").Value);
        var refreshTemporal = LeerCookie(loginTemporal);

        using var accesoBloqueado = new HttpRequestMessage(HttpMethod.Get, "/api/configuracion");
        accesoBloqueado.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", accesoTemporal.TokenAcceso);
        using var respuestaBloqueada = await cliente.SendAsync(accesoBloqueado);
        Assert.Equal(HttpStatusCode.Forbidden, respuestaBloqueada.StatusCode);

        using var cambioContrasena = new HttpRequestMessage(HttpMethod.Post, "/api/autenticacion/cambiar-contrasena")
        {
            Content = JsonContent.Create(new
            {
                contrasenaActual = recepcionistaCreada.ContrasenaTemporal,
                contrasenaNueva = "NuevaSegura!2026"
            })
        };
        cambioContrasena.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", accesoTemporal.TokenAcceso);
        using var respuestaCambio = await cliente.SendAsync(cambioContrasena);
        Assert.Equal(HttpStatusCode.OK, respuestaCambio.StatusCode);
        var accesoNuevo = await respuestaCambio.Content.ReadFromJsonAsync<RespuestaAcceso>();
        Assert.NotNull(accesoNuevo);
        Assert.DoesNotContain(new JwtSecurityTokenHandler()
            .ReadJwtToken(accesoNuevo.TokenAcceso)
            .Claims, claim => claim.Type == "cambio_contrasena_obligatorio");

        var refreshRevocado = await cliente.SendAsync(CrearSolicitudRenovacion(refreshTemporal));
        Assert.Equal(HttpStatusCode.Unauthorized, refreshRevocado.StatusCode);

        using var accesoAnterior = new HttpRequestMessage(HttpMethod.Get, "/api/usuarios");
        accesoAnterior.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", accesoTemporal.TokenAcceso);
        using var respuestaAccesoAnterior = await cliente.SendAsync(accesoAnterior);
        Assert.Equal(HttpStatusCode.Unauthorized, respuestaAccesoAnterior.StatusCode);

        var loginConTemporal = await cliente.PostAsJsonAsync("/api/autenticacion/login", new
        {
            correo = correoRecepcionista,
            contrasena = recepcionistaCreada.ContrasenaTemporal
        });
        Assert.Equal(HttpStatusCode.Unauthorized, loginConTemporal.StatusCode);

        var lecturaUsuario = new HttpRequestMessage(
            HttpMethod.Get, $"/api/usuarios/{recepcionistaCreada.Usuario.Id}");
        lecturaUsuario.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", accesoAdministrador.TokenAcceso);
        using var respuestaUsuario = await cliente.SendAsync(lecturaUsuario);
        Assert.Equal(HttpStatusCode.OK, respuestaUsuario.StatusCode);
        var datosUsuario = await respuestaUsuario.Content.ReadAsStringAsync();
        Assert.DoesNotContain("contrasenaTemporal", datosUsuario, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("passwordHash", datosUsuario, StringComparison.OrdinalIgnoreCase);

        var loginConNueva = await cliente.PostAsJsonAsync("/api/autenticacion/login", new
        {
            correo = correoRecepcionista,
            contrasena = "NuevaSegura!2026"
        });
        Assert.Equal(HttpStatusCode.OK, loginConNueva.StatusCode);

        var accesoRecepcionista = await loginConNueva.Content.ReadFromJsonAsync<RespuestaAcceso>();
        Assert.NotNull(accesoRecepcionista);
        using var desactivar = new HttpRequestMessage(
            HttpMethod.Patch, $"/api/usuarios/{recepcionistaCreada.Usuario.Id}/estado")
        {
            Content = JsonContent.Create(new { activo = false })
        };
        desactivar.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", accesoAdministrador.TokenAcceso);
        using var respuestaDesactivar = await cliente.SendAsync(desactivar);
        Assert.Equal(HttpStatusCode.OK, respuestaDesactivar.StatusCode);

        using var sesionDesactivada = new HttpRequestMessage(HttpMethod.Get, "/api/usuarios");
        sesionDesactivada.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", accesoRecepcionista.TokenAcceso);
        using var respuestaSesionDesactivada = await cliente.SendAsync(sesionDesactivada);
        Assert.Equal(HttpStatusCode.Unauthorized, respuestaSesionDesactivada.StatusCode);

        var renovar = CrearSolicitudRenovacion(tokenAnterior);
        var respuestaRenovar = await cliente.SendAsync(renovar);

        Assert.Equal(HttpStatusCode.OK, respuestaRenovar.StatusCode);
        var tokenActual = LeerCookie(respuestaRenovar);
        Assert.NotEqual(tokenAnterior, tokenActual);

        var reuso = CrearSolicitudRenovacion(tokenAnterior);
        var respuestaReuso = await cliente.SendAsync(reuso);

        Assert.Equal(HttpStatusCode.Unauthorized, respuestaReuso.StatusCode);

        var tokenActualRevocado = CrearSolicitudRenovacion(tokenActual);
        var respuestaTokenActual = await cliente.SendAsync(tokenActualRevocado);
        Assert.Equal(HttpStatusCode.Unauthorized, respuestaTokenActual.StatusCode);
    }

    private static HttpRequestMessage CrearSolicitudRenovacion(string token)
    {
        var solicitud = new HttpRequestMessage(HttpMethod.Post, "/api/autenticacion/refrescar");
        solicitud.Headers.Add("X-Requested-With", "XMLHttpRequest");
        solicitud.Headers.Add("Cookie", $"flexpos_refresh={token}");
        return solicitud;
    }

    private static string LeerCookie(HttpResponseMessage respuesta)
    {
        var encabezado = Assert.Single(respuesta.Headers.GetValues("Set-Cookie"));
        return encabezado.Split(';', 2)[0].Split('=', 2)[1];
    }
}
