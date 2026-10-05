using FlexPos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlexPos.Tests.Integracion;

public sealed class ModeloPuntoVentaTests
{
    [Fact]
    public void ModeloEf_ContieneAgregadosDeCajaVentaYDevolucionSinConectarPostgres()
    {
        var opciones = new DbContextOptionsBuilder<FlexPosDbContext>()
            .UseNpgsql("Host=localhost;Database=flexpos;Username=flexpos;Password=not-used")
            .Options;
        using var contexto = new FlexPosDbContext(opciones);

        var tablas = contexto.Model.GetEntityTypes()
            .Select(x => x.GetTableName())
            .Where(x => x is not null)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("cajas", tablas);
        Assert.Contains("movimientos_caja", tablas);
        Assert.Contains("ventas", tablas);
        Assert.Contains("detalles_venta", tablas);
        Assert.Contains("documentos_venta", tablas);
        Assert.Contains("devoluciones_venta", tablas);
        Assert.Contains("pagos_devolucion_venta", tablas);
        Assert.Contains("reglas_comision", tablas);
        Assert.Contains("comisiones", tablas);
        Assert.Contains("liquidaciones_comision", tablas);
    }
}
