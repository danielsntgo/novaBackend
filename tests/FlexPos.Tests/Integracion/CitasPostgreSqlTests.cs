using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FlexPos.Application.DTOs.Autenticacion;
using FlexPos.Application.DTOs.Citas;
using FlexPos.Application.DTOs.Empleados;
using FlexPos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FlexPos.Tests.Integracion;

public sealed class CitasPostgreSqlTests
{
    [HechoPostgreSql]
    public async Task Agenda_ValidaHorarioSolapamientoEstadosYReservasConcurrentes()
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

        var correoAdministrador = $"citas-{Guid.NewGuid():N}@flexpos.invalid";
        await using var fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuracion) => configuracion.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:FlexPos"] = conexionRuntime,
                    ["AdministradorInicial:Correo"] = correoAdministrador,
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

        using var clienteHttp = fabrica.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var login = await clienteHttp.PostAsJsonAsync("/api/autenticacion/login", new
        {
            correo = correoAdministrador,
            contrasena = "PruebaSegura!2026"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var acceso = await login.Content.ReadFromJsonAsync<RespuestaAcceso>();
        Assert.NotNull(acceso);

        using var configuracion = CrearSolicitud(HttpMethod.Put, "/api/configuracion", acceso.TokenAcceso, new
        {
            nombreComercial = "Salón citas prueba",
            razonSocial = (string?)null,
            identificacionFiscal = (string?)null,
            direccion = (string?)null,
            telefono = (string?)null,
            correo = (string?)null,
            codigoMoneda = "COP",
            permitirVentaSinStock = false,
            comisionProductosHabilitada = false
        });
        using var respuestaConfiguracion = await clienteHttp.SendAsync(configuracion);
        Assert.Equal(HttpStatusCode.OK, respuestaConfiguracion.StatusCode);

        var sufijo = Guid.NewGuid().ToString("N")[..10];
        using var altaCliente = CrearSolicitud(HttpMethod.Post, "/api/clientes", acceso.TokenAcceso, new
        {
            nombre = $"Cliente {sufijo}",
            documento = (string?)null,
            telefono = (string?)null,
            correo = (string?)null
        });
        using var respuestaCliente = await clienteHttp.SendAsync(altaCliente);
        Assert.Equal(HttpStatusCode.Created, respuestaCliente.StatusCode);
        var cliente = await respuestaCliente.Content.ReadFromJsonAsync<FlexPos.Application.DTOs.Clientes.ClienteDto>();
        Assert.NotNull(cliente);

        using var altaEmpleado = CrearSolicitud(HttpMethod.Post, "/api/empleados", acceso.TokenAcceso, new
        {
            nombre = $"Estilista {sufijo}",
            cargo = "Estilista",
            documento = (string?)null,
            telefono = (string?)null,
            correo = (string?)null
        });
        using var respuestaEmpleado = await clienteHttp.SendAsync(altaEmpleado);
        Assert.Equal(HttpStatusCode.Created, respuestaEmpleado.StatusCode);
        var empleado = await respuestaEmpleado.Content.ReadFromJsonAsync<EmpleadoDto>();
        Assert.NotNull(empleado);

        using var horario = CrearSolicitud(HttpMethod.Put,
            $"/api/empleados/{empleado.Id}/horario-semanal", acceso.TokenAcceso, new
            {
                intervalos = new[]
                {
                    new { diaSemana = 1, horaInicio = "09:00:00", horaFin = "12:00:00" },
                    new { diaSemana = 1, horaInicio = "13:00:00", horaFin = "17:00:00" }
                }
            });
        using var respuestaHorario = await clienteHttp.SendAsync(horario);
        Assert.Equal(HttpStatusCode.OK, respuestaHorario.StatusCode);
        var horarios = await respuestaHorario.Content.ReadFromJsonAsync<IReadOnlyList<HorarioSemanalEmpleadoDto>>();
        Assert.NotNull(horarios);
        Assert.Equal(2, horarios.Count);

        using var altaServicio = CrearSolicitud(HttpMethod.Post, "/api/servicios", acceso.TokenAcceso, new
        {
            nombre = $"Corte {sufijo}",
            descripcion = (string?)null,
            categoria = "Cabello",
            precio = 25000m,
            duracionMinutos = 45
        });
        using var respuestaServicio = await clienteHttp.SendAsync(altaServicio);
        Assert.Equal(HttpStatusCode.Created, respuestaServicio.StatusCode);
        var servicio = await respuestaServicio.Content.ReadFromJsonAsync<FlexPos.Application.DTOs.Servicios.ServicioDto>();
        Assert.NotNull(servicio);

        var inicio = DateTimeOffset.Parse("2026-10-05T10:00:00-05:00");
        using var altaCita = CrearSolicitud(HttpMethod.Post, "/api/citas", acceso.TokenAcceso, new
        {
            clienteId = cliente.Id,
            servicioId = servicio.Id,
            empleadoId = empleado.Id,
            inicioLocal = inicio
        });
        using var respuestaCita = await clienteHttp.SendAsync(altaCita);
        Assert.Equal(HttpStatusCode.Created, respuestaCita.StatusCode);
        var cita = await respuestaCita.Content.ReadFromJsonAsync<CitaDto>();
        Assert.NotNull(cita);
        Assert.Equal("Pendiente", cita.Estado);
        Assert.Equal(inicio.ToUniversalTime(), cita.InicioUtc);
        Assert.Equal(inicio.AddMinutes(45).ToUniversalTime(), cita.FinUtc);

        var fechaLocal = Uri.EscapeDataString("2026-10-05T00:00:00-05:00");
        var disponibilidad = await clienteHttp.GetFromJsonAsync<IReadOnlyList<EmpleadoDisponibilidadDto>>(
            $"/api/citas/disponibilidad?servicioId={servicio.Id}&fechaLocal={fechaLocal}");
        Assert.NotNull(disponibilidad);
        var agendaEmpleado = Assert.Single(disponibilidad, elemento => elemento.EmpleadoId == empleado.Id);
        Assert.Contains(agendaEmpleado.Periodos, periodo =>
            periodo.Inicio == DateTimeOffset.Parse("2026-10-05T10:45:00-05:00") &&
            periodo.Fin == DateTimeOffset.Parse("2026-10-05T12:00:00-05:00"));

        using var cruce = CrearSolicitud(HttpMethod.Post, "/api/citas", acceso.TokenAcceso, new
        {
            clienteId = cliente.Id,
            servicioId = servicio.Id,
            empleadoId = empleado.Id,
            inicioLocal = DateTimeOffset.Parse("2026-10-05T10:30:00-05:00")
        });
        using var respuestaCruce = await clienteHttp.SendAsync(cruce);
        Assert.Equal(HttpStatusCode.Conflict, respuestaCruce.StatusCode);

        using var fueraHorario = CrearSolicitud(HttpMethod.Post, "/api/citas", acceso.TokenAcceso, new
        {
            clienteId = cliente.Id,
            servicioId = servicio.Id,
            empleadoId = empleado.Id,
            inicioLocal = DateTimeOffset.Parse("2026-10-05T12:30:00-05:00")
        });
        using var respuestaFueraHorario = await clienteHttp.SendAsync(fueraHorario);
        Assert.Equal(HttpStatusCode.Conflict, respuestaFueraHorario.StatusCode);

        using var reprogramar = CrearSolicitud(
            HttpMethod.Put, $"/api/citas/{cita.Id}/reprogramar", acceso.TokenAcceso, new
            {
                empleadoId = empleado.Id,
                inicioLocal = DateTimeOffset.Parse("2026-10-05T10:15:00-05:00")
            });
        using var respuestaReprogramar = await clienteHttp.SendAsync(reprogramar);
        Assert.Equal(HttpStatusCode.OK, respuestaReprogramar.StatusCode);

        var solicitudesSimultaneas = Enumerable.Range(0, 2).Select(_ =>
            clienteHttp.SendAsync(CrearSolicitud(HttpMethod.Post, "/api/citas", acceso.TokenAcceso, new
            {
                clienteId = cliente.Id,
                servicioId = servicio.Id,
                empleadoId = empleado.Id,
                inicioLocal = DateTimeOffset.Parse("2026-10-05T14:00:00-05:00")
            })));
        var respuestasSimultaneas = await Task.WhenAll(solicitudesSimultaneas);
        try
        {
            Assert.Equal(1, respuestasSimultaneas.Count(respuesta => respuesta.StatusCode == HttpStatusCode.Created));
            Assert.Equal(1, respuestasSimultaneas.Count(respuesta => respuesta.StatusCode == HttpStatusCode.Conflict));
        }
        finally
        {
            foreach (var respuesta in respuestasSimultaneas)
            {
                respuesta.Dispose();
            }
        }

        using var confirmarCita = CrearSolicitud(
            HttpMethod.Patch, $"/api/citas/{cita.Id}/estado", acceso.TokenAcceso, new { estado = "Confirmada" });
        using var respuestaConfirmacion = await clienteHttp.SendAsync(confirmarCita);
        Assert.Equal(HttpStatusCode.OK, respuestaConfirmacion.StatusCode);

        using var cancelarCita = CrearSolicitud(
            HttpMethod.Patch, $"/api/citas/{cita.Id}/estado", acceso.TokenAcceso, new { estado = "Cancelada" });
        using var respuestaCancelacion = await clienteHttp.SendAsync(cancelarCita);
        Assert.Equal(HttpStatusCode.OK, respuestaCancelacion.StatusCode);
    }

    private static HttpRequestMessage CrearSolicitud(HttpMethod metodo, string ruta, string token, object cuerpo)
    {
        var solicitud = new HttpRequestMessage(metodo, ruta) { Content = JsonContent.Create(cuerpo) };
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return solicitud;
    }
}
