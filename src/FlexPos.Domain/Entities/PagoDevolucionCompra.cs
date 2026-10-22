using FlexPos.Domain.Abstractions;
using FlexPos.Domain.Errors;

namespace FlexPos.Domain.Entities;

public sealed class PagoDevolucionCompra : EntidadAuditable
{
    private PagoDevolucionCompra()
    {
    }

    private PagoDevolucionCompra(
        Guid metodoPagoId,
        string metodoPagoNombre,
        bool esEfectivo,
        Guid cuentaFinancieraId,
        string cuentaFinancieraNombre,
        decimal importe,
        string? referencia)
    {
        MetodoPagoId = metodoPagoId;
        MetodoPagoNombre = metodoPagoNombre;
        EsEfectivo = esEfectivo;
        CuentaFinancieraId = cuentaFinancieraId;
        CuentaFinancieraNombre = cuentaFinancieraNombre;
        Importe = importe;
        Referencia = string.IsNullOrWhiteSpace(referencia) ? null : referencia.Trim();
    }

    public Guid DevolucionCompraId { get; internal set; }
    public Guid MetodoPagoId { get; private set; }
    public string MetodoPagoNombre { get; private set; } = string.Empty;
    public bool EsEfectivo { get; private set; }
    public Guid CuentaFinancieraId { get; private set; }
    public string CuentaFinancieraNombre { get; private set; } = string.Empty;
    public decimal Importe { get; private set; }
    public string? Referencia { get; private set; }

    public static Resultado<PagoDevolucionCompra> Crear(
        MetodoPagoConfigurado metodo,
        CuentaFinanciera cuenta,
        decimal importe,
        string? referencia)
    {
        if (metodo.Id == Guid.Empty || !metodo.Activo || cuenta.Id == Guid.Empty || !cuenta.Activa ||
            !metodo.EsCompatibleConCuenta(cuenta) || cuenta.CodigoMoneda.Length != 3 || importe <= 0m ||
            importe > 9_999_999_999_999_999.99m || decimal.Round(importe, 2) != importe ||
            (metodo.RequiereReferencia && string.IsNullOrWhiteSpace(referencia)) ||
            (!string.IsNullOrWhiteSpace(referencia) && referencia.Trim().Length > 120))
        {
            return Resultado<PagoDevolucionCompra>.Fallo(new ErrorDominio(
                "compra.reembolso_invalido", "El método, importe o referencia del reintegro no son válidos."));
        }

        return Resultado<PagoDevolucionCompra>.Exito(new PagoDevolucionCompra(
            metodo.Id, metodo.Nombre, metodo.EsEfectivo, cuenta.Id, cuenta.Nombre, importe, referencia));
    }
}
