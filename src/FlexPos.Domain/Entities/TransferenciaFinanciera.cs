using FlexPos.Domain.Abstractions;
using FlexPos.Domain.Enums;
using FlexPos.Domain.Errors;
using FlexPos.Domain.ValueObjects;

namespace FlexPos.Domain.Entities;

public sealed class TransferenciaFinanciera : EntidadAuditable
{
    private TransferenciaFinanciera()
    {
    }

    private TransferenciaFinanciera(
        Guid cuentaOrigenId,
        Guid cuentaDestinoId,
        decimal importe,
        string codigoMoneda,
        string? concepto,
        string? referencia,
        DateTimeOffset fechaUtc)
    {
        CuentaOrigenId = cuentaOrigenId;
        CuentaDestinoId = cuentaDestinoId;
        Importe = importe;
        CodigoMoneda = codigoMoneda;
        Concepto = Limpiar(concepto);
        Referencia = Limpiar(referencia);
        FechaUtc = fechaUtc.ToUniversalTime();
    }

    public Guid CuentaOrigenId { get; private set; }
    public Guid CuentaDestinoId { get; private set; }
    public decimal Importe { get; private set; }
    public string CodigoMoneda { get; private set; } = string.Empty;
    public string? Concepto { get; private set; }
    public string? Referencia { get; private set; }
    public DateTimeOffset FechaUtc { get; private set; }

    public static Resultado<TransferenciaFinanciera> Crear(
        Guid cuentaOrigenId,
        Guid cuentaDestinoId,
        decimal importe,
        string? codigoMoneda,
        string? concepto,
        string? referencia,
        DateTimeOffset fechaUtc)
    {
        if (cuentaOrigenId == Guid.Empty ||
            cuentaDestinoId == Guid.Empty ||
            cuentaOrigenId == cuentaDestinoId ||
            importe <= 0m ||
            importe > 9_999_999_999_999_999.99m ||
            decimal.Round(importe, 2) != importe ||
            fechaUtc == default ||
            (!string.IsNullOrWhiteSpace(concepto) && concepto.Trim().Length > 250) ||
            (!string.IsNullOrWhiteSpace(referencia) && referencia.Trim().Length > 120))
        {
            return Resultado<TransferenciaFinanciera>.Fallo(new ErrorDominio(
                "transferencia_financiera.datos_invalidos",
                "La transferencia requiere cuentas distintas, importe y fecha válidos."));
        }

        var moneda = Dinero.Crear(0m, codigoMoneda);
        return moneda.EsExitoso
            ? Resultado<TransferenciaFinanciera>.Exito(new TransferenciaFinanciera(
                cuentaOrigenId, cuentaDestinoId, importe, moneda.Valor!.CodigoMoneda, concepto, referencia, fechaUtc))
            : Resultado<TransferenciaFinanciera>.Fallo(moneda.Error!);
    }

    public Resultado<bool> ValidarCuentas(
        CuentaFinanciera origen,
        CuentaFinanciera destino,
        decimal saldoOrigen,
        decimal saldoDestino)
    {
        var tiposPermitidos = new[]
        {
            TipoCuentaFinanciera.Efectivo,
            TipoCuentaFinanciera.Banco,
            TipoCuentaFinanciera.Billetera,
            TipoCuentaFinanciera.Otro
        };
        if (origen.Id != CuentaOrigenId || destino.Id != CuentaDestinoId ||
            origen.ConfiguracionNegocioId != destino.ConfiguracionNegocioId ||
            !origen.Activa || !destino.Activa ||
            origen.CodigoMoneda != destino.CodigoMoneda || origen.CodigoMoneda != CodigoMoneda ||
            !tiposPermitidos.Contains(origen.Tipo) ||
            destino.Tipo is not (TipoCuentaFinanciera.Efectivo or TipoCuentaFinanciera.Banco or
                TipoCuentaFinanciera.Billetera or TipoCuentaFinanciera.Otro or TipoCuentaFinanciera.TarjetaCredito))
        {
            return Resultado<bool>.Fallo(new ErrorDominio(
                "transferencia_financiera.conflicto",
                "Las cuentas deben estar activas, pertenecer al mismo negocio y moneda, y la tarjeta solo puede ser destino."));
        }

        if (destino.Tipo == TipoCuentaFinanciera.TarjetaCredito &&
            (saldoDestino >= 0m || saldoDestino + Importe > 0m))
        {
            return Resultado<bool>.Fallo(new ErrorDominio(
                "transferencia_financiera.conflicto",
                "La transferencia a tarjeta debe reducir una deuda existente y no puede excederla."));
        }

        var saldoOrigenValido = origen.ValidarSaldo(saldoOrigen - Importe);
        if (!saldoOrigenValido.EsExitoso)
        {
            return Resultado<bool>.Fallo(saldoOrigenValido.Error!);
        }

        var saldoDestinoValido = destino.ValidarSaldo(saldoDestino + Importe);
        return saldoDestinoValido.EsExitoso
            ? Resultado<bool>.Exito(true)
            : Resultado<bool>.Fallo(saldoDestinoValido.Error!);
    }

    private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
