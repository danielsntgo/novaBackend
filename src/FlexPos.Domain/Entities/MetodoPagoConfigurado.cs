using FlexPos.Domain.Abstractions;
using FlexPos.Domain.Enums;
using FlexPos.Domain.Errors;

namespace FlexPos.Domain.Entities;

public sealed class MetodoPagoConfigurado : EntidadAuditable
{
    private MetodoPagoConfigurado()
    {
    }

    private MetodoPagoConfigurado(
        Guid configuracionNegocioId,
        string nombre,
        bool requiereReferencia,
        bool esEfectivo,
        CuentaFinanciera cuentaFinanciera)
    {
        ConfiguracionNegocioId = configuracionNegocioId;
        Nombre = nombre.Trim();
        NombreNormalizado = Nombre.ToUpperInvariant();
        RequiereReferencia = requiereReferencia;
        EsEfectivo = esEfectivo;
        CuentaFinancieraId = cuentaFinanciera.Id;
        CuentaFinanciera = cuentaFinanciera;
    }

    public Guid ConfiguracionNegocioId { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public string NombreNormalizado { get; private set; } = string.Empty;
    public bool RequiereReferencia { get; private set; }
    public bool EsEfectivo { get; private set; }
    public Guid CuentaFinancieraId { get; private set; }
    public CuentaFinanciera CuentaFinanciera { get; private set; } = null!;
    public bool Activo { get; private set; } = true;

    public bool EsCompatibleConCuenta(CuentaFinanciera cuenta) =>
        cuenta.Activa && cuenta.Id == CuentaFinancieraId &&
        cuenta.ConfiguracionNegocioId == ConfiguracionNegocioId &&
        (EsEfectivo
            ? cuenta.Tipo == TipoCuentaFinanciera.Efectivo
            : cuenta.Tipo is TipoCuentaFinanciera.Banco or TipoCuentaFinanciera.Billetera or TipoCuentaFinanciera.Otro);

    public static Resultado<MetodoPagoConfigurado> Crear(
        Guid configuracionNegocioId,
        string? nombre,
        bool requiereReferencia,
        bool esEfectivo,
        CuentaFinanciera? cuentaFinanciera)
    {
        var error = Validar(configuracionNegocioId, nombre, esEfectivo, cuentaFinanciera);
        return error is null
            ? Resultado<MetodoPagoConfigurado>.Exito(new MetodoPagoConfigurado(
                configuracionNegocioId, nombre!, requiereReferencia, esEfectivo, cuentaFinanciera!))
            : Resultado<MetodoPagoConfigurado>.Fallo(error);
    }

    public Resultado<bool> Actualizar(
        string? nombre,
        bool requiereReferencia,
        bool esEfectivo,
        CuentaFinanciera? cuentaFinanciera)
    {
        var error = Validar(ConfiguracionNegocioId, nombre, esEfectivo, cuentaFinanciera);
        if (error is not null)
        {
            return Resultado<bool>.Fallo(error);
        }

        Nombre = nombre!.Trim();
        NombreNormalizado = Nombre.ToUpperInvariant();
        RequiereReferencia = requiereReferencia;
        EsEfectivo = esEfectivo;
        CuentaFinancieraId = cuentaFinanciera!.Id;
        CuentaFinanciera = cuentaFinanciera;
        return Resultado<bool>.Exito(true);
    }

    public Resultado<bool> Desactivar() => EstablecerEstado(false);

    public Resultado<bool> EstablecerEstado(bool activo)
    {
        Activo = activo;
        return Resultado<bool>.Exito(true);
    }

    private static ErrorDominio? Validar(
        Guid configuracionNegocioId,
        string? nombre,
        bool esEfectivo,
        CuentaFinanciera? cuentaFinanciera)
    {
        if (configuracionNegocioId == Guid.Empty || string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 80)
        {
            return new ErrorDominio(
                "metodo_pago.datos_invalidos", "El método de pago requiere configuración y un nombre válido.");
        }

        if (cuentaFinanciera is null || cuentaFinanciera.Id == Guid.Empty || !cuentaFinanciera.Activa ||
            cuentaFinanciera.ConfiguracionNegocioId != configuracionNegocioId ||
            (esEfectivo && cuentaFinanciera.Tipo != TipoCuentaFinanciera.Efectivo) ||
            (!esEfectivo && cuentaFinanciera.Tipo is not (
                TipoCuentaFinanciera.Banco or TipoCuentaFinanciera.Billetera or TipoCuentaFinanciera.Otro)))
        {
            return new ErrorDominio(
                "metodo_pago.cuenta_invalida",
                "La cuenta debe estar activa, pertenecer al negocio y ser compatible con el método de pago.");
        }

        return null;
    }
}
