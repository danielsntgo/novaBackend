using FlexPos.Domain.ValueObjects;
using Xunit;

namespace FlexPos.Tests.Dominio;

public sealed class DineroTests
{
    [Fact]
    public void Crear_ConservaImporteYMonedaNormalizada()
    {
        var resultado = Dinero.Crear(100_000m, " cop ");

        Assert.True(resultado.EsExitoso);
        Assert.Equal(100_000m, resultado.Valor!.Importe);
        Assert.Equal("COP", resultado.Valor.CodigoMoneda);
    }

    [Theory]
    [InlineData(12.345)]
    [InlineData(10_000_000_000_000_000d)]
    public void Crear_RechazaImportesQueNoCabenEnNumeric18_2(decimal importe)
    {
        var resultado = Dinero.Crear(importe, "COP");

        Assert.False(resultado.EsExitoso);
        Assert.Equal("dinero.importe_invalido", resultado.Error!.Codigo);
    }

    [Fact]
    public void Sumar_RechazaMonedasDistintas()
    {
        var cop = Dinero.Crear(100m, "COP").Valor!;
        var usd = Dinero.Crear(1m, "USD").Valor!;

        var resultado = cop.Sumar(usd);

        Assert.False(resultado.EsExitoso);
        Assert.Equal("dinero.moneda_diferente", resultado.Error!.Codigo);
    }

    [Fact]
    public void Sumar_AceptaMonedasIguales()
    {
        var primero = Dinero.Crear(12.25m, "COP").Valor!;
        var segundo = Dinero.Crear(7.75m, "COP").Valor!;

        var resultado = primero.Sumar(segundo);

        Assert.True(resultado.EsExitoso);
        Assert.Equal(20m, resultado.Valor!.Importe);
        Assert.Equal("COP", resultado.Valor.CodigoMoneda);
    }
}
