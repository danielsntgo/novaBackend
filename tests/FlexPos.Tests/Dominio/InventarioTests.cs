using FlexPos.Domain.Entities;
using FlexPos.Domain.Enums;
using FlexPos.Domain.ValueObjects;
using Xunit;

namespace FlexPos.Tests.Dominio;

public sealed class InventarioTests
{
    [Fact]
    public void Articulo_PrecisionEnteraOFraccionariaSeConfiguraPorArticulo()
    {
        var entero = CrearArticulo("Producto", manejaFraccion: false);
        var fraccionario = CrearArticulo("Insumo", manejaFraccion: true, tipo: TipoArticuloInventario.Insumo);
        var productoFraccionario = CrearArticulo("Producto a granel", manejaFraccion: true);
        var insumoEntero = CrearArticulo("Guantes", manejaFraccion: false, tipo: TipoArticuloInventario.Insumo);
        var costo = Dinero.Crear(100m, "COP").Valor!;

        Assert.False(entero.AplicarEntrada(0.25m, costo).EsExitoso);
        Assert.True(fraccionario.AplicarEntrada(0.255m, costo).EsExitoso);
        Assert.True(productoFraccionario.AplicarEntrada(0.25m, costo).EsExitoso);
        Assert.True(insumoEntero.AplicarEntrada(3m, costo).EsExitoso);
        Assert.False(fraccionario.AplicarEntrada(0.0001m, costo).EsExitoso);
        Assert.Equal(0.255m, fraccionario.ExistenciaActual);
    }

    [Fact]
    public void Articulo_CalculaPromedioPonderadoYNoPermiteSaldoNegativo()
    {
        var articulo = CrearArticulo("Shampoo", manejaFraccion: true);

        Assert.True(articulo.AplicarEntrada(10m, Dinero.Crear(1000m, "COP").Valor!).EsExitoso);
        Assert.True(articulo.AplicarEntrada(10m, Dinero.Crear(1400m, "COP").Valor!).EsExitoso);

        Assert.Equal(20m, articulo.ExistenciaActual);
        Assert.Equal(1200m, articulo.CostoPromedio.Importe);
        Assert.False(articulo.AplicarSalida(20.001m).EsExitoso);
        Assert.Equal(20m, articulo.ExistenciaActual);
        Assert.True(articulo.AplicarSalida(1.5m).EsExitoso);
        Assert.Equal(18.5m, articulo.ExistenciaActual);
    }

    [Fact]
    public void Articulo_NoPermiteCambiarUnidadTrasTenerMovimientos()
    {
        var articulo = CrearArticulo("Tinte", manejaFraccion: true, tipo: TipoArticuloInventario.Insumo);

        var cambio = articulo.Actualizar(
            "TINTE", "Tinte", "botella", false, null, 0m, null, tieneMovimientos: true);

        Assert.False(cambio.EsExitoso);
        Assert.Equal("ml", articulo.UnidadBase);
        Assert.True(articulo.ManejaFraccion);
    }

    [Fact]
    public void Compra_SeConfirmaUnaVezYDespuesEsInmutable()
    {
        var proveedor = Proveedor.Crear("Distribuidora", null, null, null).Valor!;
        var articulo = CrearArticulo("Acondicionador", manejaFraccion: false);
        var linea = DetalleCompra.Crear(articulo, 4m, Dinero.Crear(2500m, "COP").Valor!).Valor!;
        var fecha = DateTimeOffset.UtcNow;
        var compra = Compra.Crear(proveedor.Id, fecha, "FAC-10", null, "COP").Valor!;

        Assert.True(compra.AgregarDetalle(linea).EsExitoso);
        Assert.Equal(10000m, compra.TotalImporte);
        Assert.True(compra.Confirmar(fecha).EsExitoso);
        Assert.False(compra.Confirmar(fecha.AddMinutes(1)).EsExitoso);
        Assert.False(compra.ActualizarBorrador(
            proveedor.Id, fecha, "FAC-11", null, [linea]).EsExitoso);
    }

    [Fact]
    public void InsumoNoAceptaPrecioDeVenta()
    {
        var insumo = ArticuloInventario.Crear(
            null, "Alcohol", TipoArticuloInventario.Insumo, "ml", true, null, 0m, "COP", 100m);

        Assert.False(insumo.EsExitoso);
    }

    private static ArticuloInventario CrearArticulo(
        string nombre,
        bool manejaFraccion,
        TipoArticuloInventario tipo = TipoArticuloInventario.Producto) =>
        ArticuloInventario.Crear(
            null,
            nombre,
            tipo,
            "ml",
            manejaFraccion,
            null,
            0m,
            "COP",
            tipo == TipoArticuloInventario.Producto ? 5000m : null).Valor!;
}
