using FlexPos.Domain.Entities;
using FlexPos.Domain.Enums;
using Xunit;

namespace FlexPos.Tests.Dominio;

public sealed class ComisionesTests
{
    [Fact]
    public void ReglaComision_AceptaPorcentajeOFijoYRechazaPorcentajeMayorAlCien()
    {
        var empleadoId = Guid.NewGuid();
        var servicioId = Guid.NewGuid();

        Assert.True(ReglaComision.Crear(empleadoId, servicioId, TipoTarifaComision.Porcentaje, 25m).EsExitoso);
        Assert.True(ReglaComision.Crear(empleadoId, servicioId, TipoTarifaComision.ValorFijo, 5000m).EsExitoso);
        Assert.False(ReglaComision.Crear(empleadoId, servicioId, TipoTarifaComision.Porcentaje, 100.01m).EsExitoso);
    }

    [Fact]
    public void ReglaComision_CalculaSobreBaseConDescuentosAntesDeImpuestosOYTarifaFijaPorUnidad()
    {
        var empleadoId = Guid.NewGuid();
        var servicioId = Guid.NewGuid();
        var linea = DetalleVenta.Crear(
            TipoLineaVenta.Servicio, null, servicioId, empleadoId, null, "Servicio", "servicio",
            3m, 100m, null, false, TipoDescuento.ImporteFijo, 10m, "IVA", 19m).Valor!;
        var venta = Venta.Crear(
            Guid.NewGuid(), null, null, null, DateTimeOffset.UtcNow, "COP",
            TipoDescuento.Porcentaje, 10m, [linea]).Valor!;
        var lineaVenta = venta.Detalles.Single();
        var porcentaje = ReglaComision.Crear(
            empleadoId, servicioId, TipoTarifaComision.Porcentaje, 20m).Valor!;
        var valorFijo = ReglaComision.Crear(
            empleadoId, servicioId, TipoTarifaComision.ValorFijo, 12.50m).Valor!;

        Assert.Equal(261m, lineaVenta.BaseNetaAntesImpuestos);
        Assert.Equal(52.20m, porcentaje.CalcularImporte(lineaVenta).Valor);
        Assert.Equal(37.50m, valorFijo.CalcularImporte(lineaVenta).Valor);
        Assert.Equal(49.59m, lineaVenta.ImpuestoImporte);
    }

    [Fact]
    public void AjusteDevolucion_ConservaTrazaYSeLiquidaSinBorrarElDevengo()
    {
        var empleadoId = Guid.NewGuid();
        var servicioId = Guid.NewGuid();
        var ventaId = Guid.NewGuid();
        var detalleId = Guid.NewGuid();
        var devengo = Comision.CrearDevengo(
            empleadoId, servicioId, ventaId, detalleId,
            TipoTarifaComision.Porcentaje, 20m, 500m, 1m, 100m, "COP").Valor!;
        var devolucionId = Guid.NewGuid();
        var ajuste = Comision.CrearAjusteDevolucion(devengo, devolucionId, 250m, 0.5m, 50m);

        Assert.True(ajuste.EsExitoso);
        Assert.Equal(TipoMovimientoComision.AjusteDevolucion, ajuste.Valor!.TipoMovimiento);
        Assert.Equal(devengo.Id, ajuste.Valor.ComisionOriginalId);
        Assert.Equal(devolucionId, ajuste.Valor.DevolucionVentaId);
        Assert.Equal(EstadoComision.Pendiente, devengo.Estado);
        Assert.Equal(EstadoComision.Pendiente, ajuste.Valor.Estado);

        devengo.MarcarPagada(Guid.NewGuid());
        Assert.Equal(EstadoComision.Pagada, devengo.Estado);
        Assert.Equal(EstadoComision.Pendiente, ajuste.Valor.Estado);
    }

    [Fact]
    public void MovimientoCaja_PagoComisionRequiereLiquidacionYSeReferenciaAEsta()
    {
        var cajaId = Guid.NewGuid();
        var liquidacionId = Guid.NewGuid();

        var resultado = MovimientoCaja.Crear(
            cajaId, TipoMovimientoCaja.PagoComision, 250m, "COP", "Liquidación",
            liquidacionComisionId: liquidacionId);
        var sinReferencia = MovimientoCaja.Crear(
            cajaId, TipoMovimientoCaja.PagoComision, 250m, "COP", "Liquidación");

        Assert.True(resultado.EsExitoso);
        Assert.Equal(liquidacionId, resultado.Valor!.LiquidacionComisionId);
        Assert.False(sinReferencia.EsExitoso);
    }

    [Fact]
    public void AjusteComision_DistribuyeRedondeoYRevierteElRemanenteEnLaUltimaDevolucion()
    {
        var devengo = Comision.CrearDevengo(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            TipoTarifaComision.Porcentaje, 33.33m, 3m, 3m, 1m, "COP").Valor!;

        var primero = devengo.CalcularAjusteDevolucion(1m, []).Valor!;
        var ajusteUno = Comision.CrearAjusteDevolucion(
            devengo, Guid.NewGuid(), primero.BaseCalculo, primero.Cantidad, primero.Importe).Valor!;
        var segundo = devengo.CalcularAjusteDevolucion(1m, [ajusteUno]).Valor!;
        var ajusteDos = Comision.CrearAjusteDevolucion(
            devengo, Guid.NewGuid(), segundo.BaseCalculo, segundo.Cantidad, segundo.Importe).Valor!;
        var tercero = devengo.CalcularAjusteDevolucion(1m, [ajusteUno, ajusteDos]).Valor!;

        Assert.Equal(0.33m, primero.Importe);
        Assert.Equal(0.33m, segundo.Importe);
        Assert.Equal(0.34m, tercero.Importe);
        Assert.True(tercero.RevierteTodo);
        Assert.Equal(devengo.Importe, primero.Importe + segundo.Importe + tercero.Importe);
    }
}
