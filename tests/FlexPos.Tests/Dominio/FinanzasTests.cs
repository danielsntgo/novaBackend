using FlexPos.Domain.Entities;
using FlexPos.Domain.Enums;
using Xunit;

namespace FlexPos.Tests.Dominio;

public sealed class FinanzasTests
{
    [Fact]
    public void MetodoPago_ValidaLaCuentaSegunElTipoDePago()
    {
        var configuracionId = Guid.CreateVersion7();
        var efectivo = CrearCuenta(configuracionId, "Caja principal", TipoCuentaFinanciera.Efectivo);
        var banco = CrearCuenta(configuracionId, "Banco", TipoCuentaFinanciera.Banco);
        var tarjeta = CrearCuenta(configuracionId, "Tarjeta", TipoCuentaFinanciera.TarjetaCredito, 1000m);

        Assert.True(MetodoPagoConfigurado.Crear(configuracionId, "Efectivo", false, true, efectivo).EsExitoso);
        Assert.True(MetodoPagoConfigurado.Crear(configuracionId, "Transferencia", true, false, banco).EsExitoso);
        Assert.False(MetodoPagoConfigurado.Crear(configuracionId, "Efectivo banco", false, true, banco).EsExitoso);
        Assert.False(MetodoPagoConfigurado.Crear(configuracionId, "Pago tarjeta", true, false, tarjeta).EsExitoso);
        var metodoEfectivo = MetodoPagoConfigurado.Crear(configuracionId, "Efectivo", false, true, efectivo).Valor!;
        Assert.False(metodoEfectivo.EsCompatibleConCuenta(CrearCuenta(
            configuracionId, "Caja duplicada", TipoCuentaFinanciera.Efectivo)));
    }

    [Theory]
    [InlineData(TipoCuentaFinanciera.Efectivo, -0.01)]
    [InlineData(TipoCuentaFinanciera.Banco, -0.01)]
    [InlineData(TipoCuentaFinanciera.Billetera, -0.01)]
    [InlineData(TipoCuentaFinanciera.Otro, -0.01)]
    public void CuentaNoCredito_RechazaSaldoNegativo(TipoCuentaFinanciera tipo, decimal saldo)
    {
        var cuenta = CrearCuenta(Guid.CreateVersion7(), "Cuenta", tipo);

        Assert.False(cuenta.ValidarSaldo(saldo).EsExitoso);
    }

    [Fact]
    public void CuentaTarjeta_ValidaLaDeudaContraElCupo()
    {
        var cuenta = CrearCuenta(Guid.CreateVersion7(), "Tarjeta", TipoCuentaFinanciera.TarjetaCredito, 500m);

        Assert.True(cuenta.ValidarSaldo(-500m).EsExitoso);
        Assert.False(cuenta.ValidarSaldo(-500.01m).EsExitoso);
    }

    [Fact]
    public void MovimientoFinanciero_ExponeTipoAbiertoYSentidoExplicito()
    {
        var fecha = DateTimeOffset.UtcNow;
        var resultado = MovimientoFinanciero.Crear(
            Guid.CreateVersion7(), "ComisionBancaria", SentidoMovimientoFinanciero.Salida,
            12.50m, "COP", fecha, "Cargo bancario");

        Assert.True(resultado.EsExitoso);
        Assert.Equal("ComisionBancaria", resultado.Valor!.Tipo);
        Assert.Equal(SentidoMovimientoFinanciero.Salida, resultado.Valor.Sentido);
        Assert.Equal(12.50m, resultado.Valor.Importe);
    }

    [Theory]
    [InlineData(TipoCuentaFinanciera.Efectivo, TipoCuentaFinanciera.Banco)]
    [InlineData(TipoCuentaFinanciera.Banco, TipoCuentaFinanciera.Efectivo)]
    [InlineData(TipoCuentaFinanciera.Banco, TipoCuentaFinanciera.Banco)]
    [InlineData(TipoCuentaFinanciera.Banco, TipoCuentaFinanciera.Billetera)]
    [InlineData(TipoCuentaFinanciera.Otro, TipoCuentaFinanciera.TarjetaCredito)]
    public void Transferencia_AdmiteCombinacionesPermitidas(
        TipoCuentaFinanciera tipoOrigen,
        TipoCuentaFinanciera tipoDestino)
    {
        var negocioId = Guid.CreateVersion7();
        var origen = CrearCuenta(negocioId, "Origen", tipoOrigen, tipoOrigen == TipoCuentaFinanciera.TarjetaCredito ? 1000m : null);
        var destino = CrearCuenta(negocioId, "Destino", tipoDestino, tipoDestino == TipoCuentaFinanciera.TarjetaCredito ? 1000m : null);
        var transferencia = CrearTransferencia(origen, destino, 100m);
        var saldoDestino = tipoDestino == TipoCuentaFinanciera.TarjetaCredito ? -500m : 100m;

        var resultado = transferencia.ValidarCuentas(origen, destino, 500m, saldoDestino);

        Assert.True(resultado.EsExitoso);
    }

    [Fact]
    public void Transferencia_RechazaTarjetaComoOrigen()
    {
        var negocioId = Guid.CreateVersion7();
        var origen = CrearCuenta(negocioId, "Tarjeta", TipoCuentaFinanciera.TarjetaCredito, 1000m);
        var destino = CrearCuenta(negocioId, "Banco", TipoCuentaFinanciera.Banco);
        var transferencia = CrearTransferencia(origen, destino, 100m);

        var resultado = transferencia.ValidarCuentas(origen, destino, -500m, 0m);

        Assert.False(resultado.EsExitoso);
        Assert.Equal("transferencia_financiera.conflicto", resultado.Error!.Codigo);
    }

    [Fact]
    public void Transferencia_RechazaSobrepasarDeudaDeTarjeta()
    {
        var negocioId = Guid.CreateVersion7();
        var origen = CrearCuenta(negocioId, "Banco", TipoCuentaFinanciera.Banco);
        var tarjeta = CrearCuenta(negocioId, "Tarjeta", TipoCuentaFinanciera.TarjetaCredito, 1000m);
        var transferencia = CrearTransferencia(origen, tarjeta, 101m);

        var resultado = transferencia.ValidarCuentas(origen, tarjeta, 500m, -100m);

        Assert.False(resultado.EsExitoso);
        Assert.Equal("transferencia_financiera.conflicto", resultado.Error!.Codigo);
    }

    [Fact]
    public void Transferencia_RechazaSaldoInsuficienteYMonedaDistinta()
    {
        var negocioId = Guid.CreateVersion7();
        var origen = CrearCuenta(negocioId, "Banco", TipoCuentaFinanciera.Banco);
        var destino = CrearCuenta(negocioId, "Billetera", TipoCuentaFinanciera.Billetera);
        var transferencia = CrearTransferencia(origen, destino, 101m);
        var sinSaldo = transferencia.ValidarCuentas(origen, destino, 100m, 0m);
        var euros = CuentaFinanciera.Crear(negocioId, "Euros", TipoCuentaFinanciera.Banco, "EUR", null, null, null).Valor!;
        var monedaDistinta = transferencia.ValidarCuentas(origen, euros, 500m, 0m);

        Assert.False(sinSaldo.EsExitoso);
        Assert.Equal("cuenta_financiera.saldo_insuficiente", sinSaldo.Error!.Codigo);
        Assert.False(monedaDistinta.EsExitoso);
        Assert.Equal("transferencia_financiera.conflicto", monedaDistinta.Error!.Codigo);
    }

    [Fact]
    public void Transferencia_RechazaCuentasInactivas()
    {
        var negocioId = Guid.CreateVersion7();
        var origen = CrearCuenta(negocioId, "Banco", TipoCuentaFinanciera.Banco);
        var destino = CrearCuenta(negocioId, "Otro", TipoCuentaFinanciera.Otro);
        destino.EstablecerEstado(false);
        var transferencia = CrearTransferencia(origen, destino, 10m);

        var resultado = transferencia.ValidarCuentas(origen, destino, 20m, 0m);

        Assert.False(resultado.EsExitoso);
        Assert.Equal("transferencia_financiera.conflicto", resultado.Error!.Codigo);
    }

    private static CuentaFinanciera CrearCuenta(
        Guid configuracionId,
        string nombre,
        TipoCuentaFinanciera tipo,
        decimal? cupo = null) =>
        CuentaFinanciera.Crear(configuracionId, nombre, tipo, "COP", null, null, cupo).Valor!;

    private static TransferenciaFinanciera CrearTransferencia(
        CuentaFinanciera origen,
        CuentaFinanciera destino,
        decimal importe) =>
        TransferenciaFinanciera.Crear(
            origen.Id, destino.Id, importe, origen.CodigoMoneda, "Prueba", null, DateTimeOffset.UtcNow).Valor!;
}
