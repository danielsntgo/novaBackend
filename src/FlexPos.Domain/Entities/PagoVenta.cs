using FlexPos.Domain.Abstractions;
using FlexPos.Domain.Errors;

namespace FlexPos.Domain.Entities;

public sealed class PagoVenta : EntidadAuditable
{
    private PagoVenta()
    {
    }

    private PagoVenta(
        Guid ventaId,
        Guid cajaId,
        Guid metodoPagoId,
        string metodoPagoNombre,
        bool esEfectivo,
        Guid cuentaFinancieraId,
        string cuentaFinancieraNombre,
        decimal importe,
        string? referencia)
    {
        VentaId = ventaId;
        CajaId = cajaId;
        MetodoPagoId = metodoPagoId;
        MetodoPagoNombre = metodoPagoNombre;
        EsEfectivo = esEfectivo;
        CuentaFinancieraId = cuentaFinancieraId;
        CuentaFinancieraNombre = cuentaFinancieraNombre;
        Importe = importe;
        Referencia = Limpiar(referencia);
    }

    public Guid VentaId { get; private set; }
    public Guid CajaId { get; private set; }
    public Guid MetodoPagoId { get; private set; }
    public string MetodoPagoNombre { get; private set; } = string.Empty;
    public bool EsEfectivo { get; private set; }
    public Guid CuentaFinancieraId { get; private set; }
    public string CuentaFinancieraNombre { get; private set; } = string.Empty;
    public decimal Importe { get; private set; }
    public string? Referencia { get; private set; }

    public static Resultado<PagoVenta> Crear(
        Guid ventaId,
        Guid cajaId,
        MetodoPagoConfigurado metodo,
        CuentaFinanciera cuenta,
        decimal importe,
        string? referencia)
    {
        if (ventaId == Guid.Empty || cajaId == Guid.Empty || metodo.Id == Guid.Empty || !metodo.Activo ||
            cuenta.Id == Guid.Empty || !cuenta.Activa ||
            !metodo.EsCompatibleConCuenta(cuenta) ||
            cuenta.CodigoMoneda.Length != 3 ||
            importe <= 0m || importe > 9_999_999_999_999_999.99m || decimal.Round(importe, 2) != importe ||
            (metodo.RequiereReferencia && string.IsNullOrWhiteSpace(referencia)) ||
            (!string.IsNullOrWhiteSpace(referencia) && referencia.Trim().Length > 120))
        {
            return Resultado<PagoVenta>.Fallo(new ErrorDominio(
                "venta.pago_invalido", "El método, importe o referencia del pago no son válidos."));
        }

        return Resultado<PagoVenta>.Exito(new PagoVenta(
            ventaId, cajaId, metodo.Id, metodo.Nombre, metodo.EsEfectivo,
            cuenta.Id, cuenta.Nombre, importe, referencia));
    }

    private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
