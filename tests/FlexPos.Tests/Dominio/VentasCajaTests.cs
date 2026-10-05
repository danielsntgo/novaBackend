using FlexPos.Domain.Entities;
using FlexPos.Domain.Enums;
using FlexPos.Infrastructure.Services;
using Xunit;

namespace FlexPos.Tests.Dominio;

public sealed class VentasCajaTests
{
    [Fact]
    public void Venta_CalculaDescuentosPorLineaYGeneralAntesDeImpuestos()
    {
        var linea = CrearLinea(cantidad: 2m, precio: 1000m, descuento: TipoDescuento.ImporteFijo, valorDescuento: 100m);

        var resultado = Venta.Crear(
            Guid.NewGuid(), null, null, null, DateTimeOffset.UtcNow, "COP",
            TipoDescuento.Porcentaje, 10m, [linea]);

        Assert.True(resultado.EsExitoso);
        var venta = resultado.Valor!;
        Assert.Equal(2000m, venta.Subtotal);
        Assert.Equal(100m, venta.DescuentoLineas);
        Assert.Equal(190m, venta.DescuentoGeneral);
        Assert.Equal(324.90m, venta.Impuestos);
        Assert.Equal(2034.90m, venta.Total);
        Assert.Equal(324.90m, venta.Detalles.Single().ImpuestoImporte);
    }

    [Fact]
    public void Venta_AceptaPagoParcialYDevolucionSinBorrarLaVenta()
    {
        var cajaId = Guid.NewGuid();
        var linea = CrearLinea(cantidad: 2m, precio: 100m, incluirImpuesto: false);
        var venta = Venta.Crear(
            cajaId, null, null, null, DateTimeOffset.UtcNow, "COP", null, 0m, [linea]).Valor!;
        var configuracion = ConfiguracionNegocio.Crear(
            "Negocio", null, null, null, null, null, "COP", false, false).Valor!;
        var efectivo = MetodoPagoConfigurado.Crear(configuracion.Id, "Efectivo", false, true).Valor!;
        var pago = PagoVenta.Crear(venta.Id, cajaId, efectivo, 100m, null).Valor!;

        Assert.True(venta.RegistrarPago(pago).EsExitoso);
        Assert.Equal(100m, venta.TotalPendiente);

        var detalle = DetalleDevolucionVenta.Crear(linea.Id, 1m, 100m).Valor!;
        var devolucion = DevolucionVenta.Crear(
            venta.Id, DateTimeOffset.UtcNow, "Devolución parcial", [detalle], []).Valor!;

        var resultado = venta.RegistrarDevolucion(
            devolucion, anular: false, Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.True(resultado.EsExitoso);
        Assert.Equal(EstadoVenta.ParcialmenteDevuelta, venta.Estado);
        Assert.Equal(100m, venta.TotalDevuelto);
        Assert.Equal(0m, venta.TotalPendiente);
        Assert.Single(venta.Devoluciones);
    }

    [Fact]
    public void Caja_ArqueaDiferenciaYNoPermiteCerrarDosVeces()
    {
        var caja = Caja.Abrir(50000m, DateTimeOffset.UtcNow, Guid.NewGuid()).Valor!;
        var resultado = caja.Cerrar(74900m, 75000m, DateTimeOffset.UtcNow, Guid.NewGuid());

        Assert.True(resultado.EsExitoso);
        Assert.Equal(-100m, caja.DiferenciaCierre);
        Assert.False(caja.Cerrar(75000m, 75000m, DateTimeOffset.UtcNow, Guid.NewGuid()).EsExitoso);
    }

    [Fact]
    public void GeneradorDocumentoVenta_GeneraPdfDeComprobante()
    {
        var venta = Venta.Crear(
            Guid.NewGuid(), null, null, null, DateTimeOffset.UtcNow, "COP", null, 0m,
            [CrearLinea(cantidad: 1m, precio: 2500m, incluirImpuesto: false)]).Valor!;
        var negocio = ConfiguracionNegocio.Crear(
            "FlexPOS", null, null, null, null, null, "COP", false, false).Valor!;
        var documento = DocumentoVenta.Crear(
            venta.Id, TipoDocumentoVenta.Comprobante, "RC-", 1, DateTimeOffset.UtcNow,
            negocio, null, null, "COP", venta.Subtotal, 0m, venta.Impuestos, venta.Total).Valor!;
        Assert.True(venta.RegistrarDocumento(documento).EsExitoso);

        var pdf = new GeneradorDocumentoVenta().Generar(venta, documento);

        Assert.StartsWith("%PDF-", System.Text.Encoding.ASCII.GetString(pdf, 0, 5));
    }

    private static DetalleVenta CrearLinea(
        decimal cantidad,
        decimal precio,
        TipoDescuento? descuento = null,
        decimal valorDescuento = 0m,
        bool incluirImpuesto = true) =>
        DetalleVenta.Crear(
            TipoLineaVenta.Producto,
            Guid.NewGuid(),
            null,
            null,
            "P-1",
            "Producto",
            "unidad",
            cantidad,
            precio,
            50m,
            manejaFraccion: false,
            descuento,
            valorDescuento,
            incluirImpuesto ? "IVA" : null,
            incluirImpuesto ? 19m : null).Valor!;
}
