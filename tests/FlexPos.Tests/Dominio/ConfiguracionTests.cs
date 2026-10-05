using FlexPos.Domain.Entities;
using FlexPos.Domain.Enums;
using Xunit;

namespace FlexPos.Tests.Dominio;

public sealed class ConfiguracionTests
{
    [Fact]
    public void CrearConfiguracion_ConservaMonedaYNoSuponeValoresComerciales()
    {
        var resultado = ConfiguracionNegocio.Crear(
            "  Salón Central ", null, null, null, null, null, " cop ", false, false);

        Assert.True(resultado.EsExitoso);
        Assert.Equal("Salón Central", resultado.Valor!.NombreComercial);
        Assert.Equal("COP", resultado.Valor.CodigoMoneda);
        Assert.False(resultado.Valor.PermitirVentaSinStock);
        Assert.False(resultado.Valor.ComisionProductosHabilitada);
    }

    [Fact]
    public void ActualizarConfiguracion_NoCambiaDatosCuandoLaMonedaEsInvalida()
    {
        var configuracion = ConfiguracionNegocio.Crear(
            "Salón Central", null, null, null, null, null, "COP", false, false).Valor!;

        var resultado = configuracion.Actualizar(
            "Otro nombre", null, null, null, null, null, "12", true, true);

        Assert.False(resultado.EsExitoso);
        Assert.Equal("Salón Central", configuracion.NombreComercial);
        Assert.False(configuracion.PermitirVentaSinStock);
    }

    [Fact]
    public void Configuracion_NoPermiteHabilitarComisionesDeProductosEnEstaFase()
    {
        var resultado = ConfiguracionNegocio.Crear(
            "Negocio", null, null, null, null, null, "COP", false, true);

        Assert.False(resultado.EsExitoso);
        Assert.Equal("configuracion.comisiones_productos_pendiente", resultado.Error!.Codigo);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    [InlineData(12.345)]
    public void CrearImpuesto_RechazaPorcentajesFueraDeRango(decimal porcentaje)
    {
        var resultado = ImpuestoConfigurado.Crear(Guid.CreateVersion7(), "Impuesto", porcentaje);

        Assert.False(resultado.EsExitoso);
        Assert.Equal("impuesto.porcentaje_invalido", resultado.Error!.Codigo);
    }

    [Fact]
    public void Numeracion_TomaNumerosConsecutivos()
    {
        var numeracion = NumeracionDocumento.Crear(
            Guid.CreateVersion7(), TipoDocumentoVenta.Factura, "FAC", 120).Valor!;

        Assert.Equal(120, numeracion.TomarSiguienteNumero().Valor);
        Assert.Equal(121, numeracion.TomarSiguienteNumero().Valor);
        Assert.Equal(122, numeracion.SiguienteNumero);
    }

    [Fact]
    public void MetodoPago_PuedeDesactivarseSinEliminarse()
    {
        var metodo = MetodoPagoConfigurado.Crear(Guid.CreateVersion7(), "Transferencia", true).Valor!;

        var resultado = metodo.EstablecerEstado(false);

        Assert.True(resultado.EsExitoso);
        Assert.False(metodo.Activo);
        Assert.True(metodo.RequiereReferencia);
    }
}
