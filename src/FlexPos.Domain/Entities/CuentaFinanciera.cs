using FlexPos.Domain.Abstractions;
using FlexPos.Domain.Enums;
using FlexPos.Domain.Errors;
using FlexPos.Domain.ValueObjects;

namespace FlexPos.Domain.Entities;

public sealed class CuentaFinanciera : EntidadAuditable
{
    private CuentaFinanciera()
    {
    }

    private CuentaFinanciera(
        Guid configuracionNegocioId,
        string nombre,
        TipoCuentaFinanciera tipo,
        string codigoMoneda,
        string? entidad,
        string? ultimosDigitos,
        decimal? cupoCredito)
    {
        ConfiguracionNegocioId = configuracionNegocioId;
        Nombre = nombre.Trim();
        NombreNormalizado = Nombre.ToUpperInvariant();
        Tipo = tipo;
        CodigoMoneda = codigoMoneda;
        Entidad = Limpiar(entidad);
        UltimosDigitos = Limpiar(ultimosDigitos);
        CupoCredito = cupoCredito;
    }

    public Guid ConfiguracionNegocioId { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public string NombreNormalizado { get; private set; } = string.Empty;
    public TipoCuentaFinanciera Tipo { get; private set; }
    public string CodigoMoneda { get; private set; } = string.Empty;
    public string? Entidad { get; private set; }
    public string? UltimosDigitos { get; private set; }
    public decimal? CupoCredito { get; private set; }
    public bool Activa { get; private set; } = true;

    public static Resultado<CuentaFinanciera> Crear(
        Guid configuracionNegocioId,
        string? nombre,
        TipoCuentaFinanciera tipo,
        string? codigoMoneda,
        string? entidad,
        string? ultimosDigitos,
        decimal? cupoCredito)
    {
        var error = Validar(configuracionNegocioId, nombre, tipo, codigoMoneda, entidad, ultimosDigitos, cupoCredito);
        if (error is not null)
        {
            return Resultado<CuentaFinanciera>.Fallo(error);
        }

        var moneda = Dinero.Crear(0m, codigoMoneda);
        return Resultado<CuentaFinanciera>.Exito(new CuentaFinanciera(
            configuracionNegocioId, nombre!, tipo, moneda.Valor!.CodigoMoneda,
            entidad, ultimosDigitos, cupoCredito));
    }

    public Resultado<bool> Actualizar(
        string? nombre,
        TipoCuentaFinanciera tipo,
        string? codigoMoneda,
        string? entidad,
        string? ultimosDigitos,
        decimal? cupoCredito,
        bool tieneDependencias,
        decimal saldoActual)
    {
        var error = Validar(
            ConfiguracionNegocioId, nombre, tipo, codigoMoneda, entidad, ultimosDigitos, cupoCredito);
        if (error is not null)
        {
            return Resultado<bool>.Fallo(error);
        }

        var monedaPropuesta = Dinero.Crear(0m, codigoMoneda).Valor!.CodigoMoneda;
        if (tieneDependencias && (tipo != Tipo || !string.Equals(monedaPropuesta, CodigoMoneda, StringComparison.Ordinal)))
        {
            return Resultado<bool>.Fallo(new ErrorDominio(
                "cuenta_financiera.tipo_moneda_inmutables",
                "No se puede cambiar el tipo ni la moneda de una cuenta con movimientos, métodos asociados o una caja abierta."));
        }

        var cuentaPropuesta = new CuentaFinanciera(
            ConfiguracionNegocioId, nombre!, tipo, CodigoMoneda, entidad, ultimosDigitos, cupoCredito);
        var saldoValido = cuentaPropuesta.ValidarSaldo(saldoActual);
        if (!saldoValido.EsExitoso)
        {
            return Resultado<bool>.Fallo(saldoValido.Error!);
        }

        Nombre = nombre!.Trim();
        NombreNormalizado = Nombre.ToUpperInvariant();
        Tipo = tipo;
        CodigoMoneda = Dinero.Crear(0m, codigoMoneda).Valor!.CodigoMoneda;
        Entidad = Limpiar(entidad);
        UltimosDigitos = Limpiar(ultimosDigitos);
        CupoCredito = cupoCredito;
        return Resultado<bool>.Exito(true);
    }

    public Resultado<bool> EstablecerEstado(bool activa)
    {
        Activa = activa;
        return Resultado<bool>.Exito(true);
    }

    public Resultado<bool> ValidarSaldo(decimal saldo)
    {
        if (Tipo == TipoCuentaFinanciera.TarjetaCredito)
        {
            if (CupoCredito.HasValue && saldo < -CupoCredito.Value)
            {
                return Resultado<bool>.Fallo(new ErrorDominio(
                    "cuenta_financiera.cupo_excedido", "La deuda supera el cupo de crédito de la tarjeta."));
            }

            return Resultado<bool>.Exito(true);
        }

        return saldo < 0m
            ? Resultado<bool>.Fallo(new ErrorDominio(
                "cuenta_financiera.saldo_insuficiente", "La cuenta no puede quedar con saldo negativo."))
            : Resultado<bool>.Exito(true);
    }

    private static ErrorDominio? Validar(
        Guid configuracionNegocioId,
        string? nombre,
        TipoCuentaFinanciera tipo,
        string? codigoMoneda,
        string? entidad,
        string? ultimosDigitos,
        decimal? cupoCredito)
    {
        if (configuracionNegocioId == Guid.Empty ||
            string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 80 ||
            !Enum.IsDefined(tipo) ||
            !string.IsNullOrWhiteSpace(entidad) && entidad.Trim().Length > 100 ||
            !string.IsNullOrWhiteSpace(ultimosDigitos) &&
                (ultimosDigitos.Trim().Length > 8 || ultimosDigitos.Trim().Any(x => !char.IsDigit(x))) ||
            cupoCredito.HasValue &&
                (tipo != TipoCuentaFinanciera.TarjetaCredito || cupoCredito <= 0m ||
                 decimal.Round(cupoCredito.Value, 2) != cupoCredito.Value ||
                 cupoCredito.Value > 9_999_999_999_999_999.99m))
        {
            return new ErrorDominio(
                "cuenta_financiera.datos_invalidos", "Los datos de la cuenta financiera no son válidos.");
        }

        var moneda = Dinero.Crear(0m, codigoMoneda);
        return moneda.EsExitoso ? null : moneda.Error;
    }

    private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
