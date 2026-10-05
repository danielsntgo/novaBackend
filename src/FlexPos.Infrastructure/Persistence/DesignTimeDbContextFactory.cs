using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FlexPos.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FlexPosDbContext>
{
    public FlexPosDbContext CreateDbContext(string[] args)
    {
        var configuracion = new ConfigurationBuilder()
            .AddJsonFile("src/FlexPos.Api/appsettings.json", optional: true)
            .AddJsonFile("src/FlexPos.Api/appsettings.Development.json", optional: true)
            .AddUserSecrets<DesignTimeDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var cadena = configuracion.GetConnectionString("Migraciones")
                     ?? configuracion.GetConnectionString("FlexPos")
                     ?? "Host=localhost;Port=5432;Database=flexpos;Username=flexpos_migraciones";

        var opciones = new DbContextOptionsBuilder<FlexPosDbContext>()
            .UseNpgsql(cadena, postgres => postgres.MigrationsHistoryTable("historial_migraciones", "flexpos"))
            .Options;

        return new FlexPosDbContext(opciones);
    }
}
