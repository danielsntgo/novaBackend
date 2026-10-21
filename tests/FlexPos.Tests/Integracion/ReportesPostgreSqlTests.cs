using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FlexPos.Application.DTOs.Autenticacion;
using FlexPos.Domain.Entities;
using FlexPos.Domain.Enums;
using FlexPos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FlexPos.Tests.Integracion;

public sealed class ReportesPostgreSqlTests
{
    [HechoPostgreSql]
    public async Task InformesTodosLosTipos_SeEjecutanContraPostgreSql()
    {
        var conexionRuntime = Environment.GetEnvironmentVariable("ConnectionStrings__FlexPosTest")!;
        var conexionMigraciones = Environment.GetEnvironmentVariable("ConnectionStrings__FlexPosTestMigraciones")!;
        var opciones = new DbContextOptionsBuilder<FlexPosDbContext>()
            .UseNpgsql(conexionMigraciones, postgres =>
                postgres.MigrationsHistoryTable("historial_migraciones", "flexpos"))
            .Options;
        await using (var contexto = new FlexPosDbContext(opciones))
        {
            await contexto.Database.MigrateAsync();
        }

        var correo = $"reportes-{Guid.NewGuid():N}@flexpos.invalid";
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

        using var cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var respuestaLogin = await cliente.PostAsJsonAsync("/api/autenticacion/login", new
        {
            correo,
            contrasena = "PruebaSegura!2026"
        });
        Assert.Equal(HttpStatusCode.OK, respuestaLogin.StatusCode);
        var sesion = await respuestaLogin.Content.ReadFromJsonAsync<RespuestaAcceso>();
        Assert.NotNull(sesion);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sesion.TokenAcceso);

        var tipos = new[]
        {
            "ventas", "ingresos", "productos", "servicios", "inventario",
            "compras", "caja", "clientes", "empleados", "comisiones", "cuentas"
        };
        foreach (var tipo in tipos)
        {
            using var respuesta = await cliente.GetAsync($"/api/reportes/{tipo}");
            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        }

        Guid cuentaId;
        var nombreCuenta = $"Reporte {Guid.NewGuid():N}";
        await using (var scope = fabrica.Services.CreateAsyncScope())
        {
            var contexto = scope.ServiceProvider.GetRequiredService<FlexPosDbContext>();
            var configuracionId = await contexto.ConfiguracionesNegocio
                .Where(x => x.EsPrincipal).Select(x => x.Id).SingleAsync();
            var cuenta = CuentaFinanciera.Crear(configuracionId, nombreCuenta, TipoCuentaFinanciera.Otro,
                "COP", null, null, null).Valor!;
            contexto.CuentasFinancieras.Add(cuenta);
            await contexto.SaveChangesAsync();
            cuentaId = cuenta.Id;
            var inicio = new DateTimeOffset(2026, 10, 20, 0, 0, 0, TimeSpan.Zero);
            var fechaEntrada = inicio.AddDays(1);
            var fechaSalida = inicio.AddDays(2);
            var movimientos = new[]
            {
                MovimientoFinanciero.Crear(cuentaId, TipoMovimientoFinanciero.AjusteMigracion,
                    SentidoMovimientoFinanciero.Entrada, 100m, "COP", inicio, "saldo anterior", migrado: true).Valor!,
                MovimientoFinanciero.Crear(cuentaId, TipoMovimientoFinanciero.AjusteMigracion,
                    SentidoMovimientoFinanciero.Entrada, 50m, "COP", fechaEntrada, "entrada periodo", migrado: true).Valor!,
                MovimientoFinanciero.Crear(cuentaId, TipoMovimientoFinanciero.AjusteMigracion,
                    SentidoMovimientoFinanciero.Salida, 20m, "COP", fechaSalida, "salida periodo", migrado: true).Valor!
            };
            contexto.MovimientosFinancieros.AddRange(movimientos);
            await contexto.SaveChangesAsync();
        }

        var desdeUtc = new DateTimeOffset(2026, 10, 21, 0, 0, 0, TimeSpan.Zero).ToString("O");
        var hastaUtc = new DateTimeOffset(2026, 10, 23, 0, 0, 0, TimeSpan.Zero).ToString("O");
        using var respuestaCuentas = await cliente.GetAsync(
            $"/api/reportes/cuentas?cuentaFinancieraId={cuentaId}&desdeUtc={Uri.EscapeDataString(desdeUtc)}&hastaUtc={Uri.EscapeDataString(hastaUtc)}");
        Assert.Equal(HttpStatusCode.OK, respuestaCuentas.StatusCode);
        using var documento = System.Text.Json.JsonDocument.Parse(await respuestaCuentas.Content.ReadAsStringAsync());
        var raiz = documento.RootElement;
        Assert.Equal(2, raiz.GetProperty("totalRegistros").GetInt32());
        var filas = raiz.GetProperty("filas").EnumerateArray().ToArray();
        Assert.Equal(150m, filas[0].GetProperty("valores").GetProperty("SaldoPosterior").GetDecimal());
        Assert.Equal(130m, filas[1].GetProperty("valores").GetProperty("SaldoPosterior").GetDecimal());
        Assert.Contains(raiz.GetProperty("totales").EnumerateArray(), total =>
            total.GetProperty("nombre").GetString() == $"Saldo final - {nombreCuenta}" &&
            total.GetProperty("valor").GetDecimal() == 130m);

        foreach (var formato in new[] { "pdf", "xlsx", "csv" })
        {
            using var exportacion = await cliente.GetAsync(
                $"/api/reportes/cuentas/exportar?formato={formato}&cuentaFinancieraId={cuentaId}&desdeUtc={Uri.EscapeDataString(desdeUtc)}&hastaUtc={Uri.EscapeDataString(hastaUtc)}");
            Assert.Equal(HttpStatusCode.OK, exportacion.StatusCode);
            Assert.NotEmpty(await exportacion.Content.ReadAsByteArrayAsync());
        }
    }
}
