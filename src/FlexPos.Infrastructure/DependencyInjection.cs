using FlexPos.Application.Interfaces;
using FlexPos.Infrastructure.Identity;
using FlexPos.Infrastructure.Persistence;
using FlexPos.Infrastructure.Persistence.Repositories;
using FlexPos.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlexPos.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuracion)
    {
        var cadena = configuracion.GetConnectionString("FlexPos");
        services.AddDbContext<FlexPosDbContext>(opciones =>
        {
            if (string.IsNullOrWhiteSpace(cadena))
            {
                opciones.UseNpgsql(postgres =>
                    postgres.MigrationsHistoryTable("historial_migraciones", "flexpos"));
            }
            else
            {
                opciones.UseNpgsql(cadena, postgres =>
                    postgres.MigrationsHistoryTable("historial_migraciones", "flexpos"));
            }
        });

        services.AddIdentityCore<UsuarioIdentidad>(opciones =>
            {
                opciones.User.RequireUniqueEmail = true;
                opciones.Password.RequiredLength = 12;
                opciones.Password.RequireDigit = true;
                opciones.Password.RequireLowercase = true;
                opciones.Password.RequireUppercase = true;
                opciones.Password.RequireNonAlphanumeric = true;
                opciones.Lockout.AllowedForNewUsers = true;
                opciones.Lockout.MaxFailedAccessAttempts = 5;
                opciones.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<RolIdentidad>()
            .AddEntityFrameworkStores<FlexPosDbContext>()
            .AddSignInManager();

        services.AddScoped<IIdentidadUsuarios, ServicioIdentidadUsuarios>();
        services.AddScoped<IRepositorioTokensRenovacion, RepositorioTokensRenovacion>();
        services.AddScoped<IRepositorioConfiguracion, RepositorioConfiguracion>();
        services.AddScoped<IRepositorioClientes, RepositorioClientes>();
        services.AddScoped<IRepositorioEmpleados, RepositorioEmpleados>();
        services.AddScoped<IRepositorioServicios, RepositorioServicios>();
        services.AddScoped<IRepositorioInventario, RepositorioInventario>();
        services.AddScoped<IRepositorioProveedores, RepositorioProveedores>();
        services.AddScoped<IRepositorioCompras, RepositorioCompras>();
        services.AddScoped<IRepositorioCitas, RepositorioCitas>();
        services.AddScoped<IRepositorioPuntoVenta, RepositorioPuntoVenta>();
        services.AddScoped<IRepositorioComisiones, RepositorioComisiones>();
        services.AddScoped<IRepositorioReportes, RepositorioReportes>();
        services.AddSingleton<IExportadorReportes, ExportadorReportes>();
        services.AddSingleton<IGeneradorDocumentoVenta, GeneradorDocumentoVenta>();
        services.AddScoped<IAdministracionUsuarios, ServicioAdministracionUsuarios>();
        services.AddScoped<InicializadorDatos>();
        services.AddSingleton<IConfiguracionTokens, ConfiguracionTokens>();
        services.AddSingleton<IEmisorTokenAcceso, EmisorTokenAcceso>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
