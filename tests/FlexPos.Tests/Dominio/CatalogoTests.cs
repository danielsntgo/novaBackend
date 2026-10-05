using FlexPos.Domain.Entities;
using FlexPos.Domain.ValueObjects;
using Xunit;

namespace FlexPos.Tests.Dominio;

public sealed class CatalogoTests
{
    [Fact]
    public void Cliente_NormalizaTextoParaBusquedaSinAcentos()
    {
        var resultado = Cliente.Crear("  María   Gómez ", "AB-123", null, "MARIA@ejemplo.com");

        Assert.True(resultado.EsExitoso);
        Assert.Equal("María   Gómez", resultado.Valor!.Nombre);
        Assert.Equal("MARIA GOMEZ", resultado.Valor.NombreNormalizado);
        Assert.Equal("AB-123", resultado.Valor.DocumentoNormalizado);
        Assert.Equal("MARIA@EJEMPLO.COM", resultado.Valor.CorreoNormalizado);
    }

    [Fact]
    public void Cliente_ActualizacionInvalidaNoModificaElRegistro()
    {
        var cliente = Cliente.Crear("Ana Pérez", null, null, null).Valor!;

        var resultado = cliente.Actualizar("", "Documento", null, null);

        Assert.False(resultado.EsExitoso);
        Assert.Equal("Ana Pérez", cliente.Nombre);
        Assert.True(cliente.Activo);
    }

    [Fact]
    public void Empleado_RequiereCargoYSeDesactivaSinEliminarse()
    {
        var invalido = Empleado.Crear("Luis Ríos", " ", null, null, null);
        Assert.False(invalido.EsExitoso);

        var empleado = Empleado.Crear("Luis Ríos", "Estilista", null, null, null).Valor!;
        empleado.EstablecerEstado(false);

        Assert.False(empleado.Activo);
        Assert.Equal("ESTILISTA", empleado.CargoNormalizado);
    }

    [Fact]
    public void Servicio_RequierePrecioNoNegativoYDuracionPositiva()
    {
        var precioNegativo = Dinero.Crear(-1m, "COP").Valor!;
        var precio = Dinero.Crear(25000m, "COP").Valor!;

        Assert.False(Servicio.Crear("Corte", null, null, precioNegativo, 30).EsExitoso);
        Assert.False(Servicio.Crear("Corte", null, null, precio, 0).EsExitoso);
        Assert.True(Servicio.Crear("Corte", null, null, precio, 30).EsExitoso);
    }

    [Theory]
    [InlineData("CAFÉ", "CAFE")]
    [InlineData("  Ana   María ", "ANA MARIA")]
    public void TextoNormalizado_EliminaTildesYCompactaEspacios(string entrada, string esperado)
    {
        Assert.Equal(esperado, TextoNormalizado.Normalizar(entrada));
    }
}
