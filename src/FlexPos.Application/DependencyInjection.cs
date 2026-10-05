using FlexPos.Application.Services;
using FlexPos.Application.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace FlexPos.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ServicioAutenticacion>();
        services.AddScoped<ServicioConfiguracion>();
        services.AddScoped<ServicioUsuarios>();
        services.AddScoped<ServicioClientes>();
        services.AddScoped<ServicioEmpleados>();
        services.AddScoped<ServicioCatalogoServicios>();
        services.AddScoped<ServicioInventario>();
        services.AddScoped<ServicioProveedores>();
        services.AddScoped<ServicioCompras>();
        services.AddScoped<ServicioCitas>();
        services.AddScoped<ServicioCaja>();
        services.AddScoped<ServicioVentas>();
        services.AddScoped<ServicioComisiones>();
        services.AddScoped<ServicioReportes>();
        services.AddSingleton<ValidadorInicioSesion>();
        return services;
    }
}
