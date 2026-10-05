using FlexPos.Domain.Abstractions;
using FlexPos.Domain.Errors;

namespace FlexPos.Domain.Entities;

public sealed class ConfiguracionNegocio : EntidadAuditable
{
    private ConfiguracionNegocio()
    {
    }

    private ConfiguracionNegocio(
        string nombreComercial,
        string? razonSocial,
        string? identificacionFiscal,
        string? direccion,
        string? telefono,
        string? correo,
        string codigoMoneda,
        bool permitirVentaSinStock,
        bool comisionProductosHabilitada,
        bool permitirSaldosPendientes,
        bool facturacionHabilitada,
        bool exigirEmpleadoVentaServicio)
    {
        NombreComercial = nombreComercial.Trim();
        RazonSocial = Limpiar(razonSocial);
        IdentificacionFiscal = Limpiar(identificacionFiscal);
        Direccion = Limpiar(direccion);
        Telefono = Limpiar(telefono);
        Correo = Limpiar(correo)?.ToLowerInvariant();
        CodigoMoneda = codigoMoneda.Trim().ToUpperInvariant();
        PermitirVentaSinStock = permitirVentaSinStock;
        ComisionProductosHabilitada = comisionProductosHabilitada;
        PermitirSaldosPendientes = permitirSaldosPendientes;
        FacturacionHabilitada = facturacionHabilitada;
        ExigirEmpleadoVentaServicio = exigirEmpleadoVentaServicio;
    }

    public string NombreComercial { get; private set; } = string.Empty;
    public string? RazonSocial { get; private set; }
    public string? IdentificacionFiscal { get; private set; }
    public string? Direccion { get; private set; }
    public string? Telefono { get; private set; }
    public string? Correo { get; private set; }
    public string CodigoMoneda { get; private set; } = string.Empty;
    public bool PermitirVentaSinStock { get; private set; }
    public bool ComisionProductosHabilitada { get; private set; }
    public bool PermitirSaldosPendientes { get; private set; }
    public bool FacturacionHabilitada { get; private set; }
    public bool ExigirEmpleadoVentaServicio { get; private set; }
    public bool EsPrincipal { get; private set; } = true;

    public static Resultado<ConfiguracionNegocio> Crear(
        string? nombreComercial,
        string? razonSocial,
        string? identificacionFiscal,
        string? direccion,
        string? telefono,
        string? correo,
        string? codigoMoneda,
        bool permitirVentaSinStock,
        bool comisionProductosHabilitada,
        bool permitirSaldosPendientes = false,
        bool facturacionHabilitada = false,
        bool exigirEmpleadoVentaServicio = false)
    {
        var error = Validar(
            nombreComercial,
            razonSocial,
            identificacionFiscal,
            direccion,
            telefono,
            correo,
            codigoMoneda,
            comisionProductosHabilitada);

        return error is null
            ? Resultado<ConfiguracionNegocio>.Exito(new ConfiguracionNegocio(
                nombreComercial!, razonSocial, identificacionFiscal, direccion, telefono,
                correo, codigoMoneda!, permitirVentaSinStock, comisionProductosHabilitada,
                permitirSaldosPendientes, facturacionHabilitada, exigirEmpleadoVentaServicio))
            : Resultado<ConfiguracionNegocio>.Fallo(error);
    }

    public Resultado<bool> Actualizar(
        string? nombreComercial,
        string? razonSocial,
        string? identificacionFiscal,
        string? direccion,
        string? telefono,
        string? correo,
        string? codigoMoneda,
        bool permitirVentaSinStock,
        bool comisionProductosHabilitada,
        bool permitirSaldosPendientes = false,
        bool facturacionHabilitada = false,
        bool exigirEmpleadoVentaServicio = false)
    {
        var error = Validar(
            nombreComercial,
            razonSocial,
            identificacionFiscal,
            direccion,
            telefono,
            correo,
            codigoMoneda,
            comisionProductosHabilitada);

        if (error is not null)
        {
            return Resultado<bool>.Fallo(error);
        }

        NombreComercial = nombreComercial!.Trim();
        RazonSocial = Limpiar(razonSocial);
        IdentificacionFiscal = Limpiar(identificacionFiscal);
        Direccion = Limpiar(direccion);
        Telefono = Limpiar(telefono);
        Correo = Limpiar(correo)?.ToLowerInvariant();
        CodigoMoneda = codigoMoneda!.Trim().ToUpperInvariant();
        PermitirVentaSinStock = permitirVentaSinStock;
        ComisionProductosHabilitada = comisionProductosHabilitada;
        PermitirSaldosPendientes = permitirSaldosPendientes;
        FacturacionHabilitada = facturacionHabilitada;
        ExigirEmpleadoVentaServicio = exigirEmpleadoVentaServicio;
        return Resultado<bool>.Exito(true);
    }

    private static ErrorDominio? Validar(
        string? nombreComercial,
        string? razonSocial,
        string? identificacionFiscal,
        string? direccion,
        string? telefono,
        string? correo,
        string? codigoMoneda,
        bool comisionProductosHabilitada)
    {
        if (comisionProductosHabilitada)
        {
            return new ErrorDominio(
                "configuracion.comisiones_productos_pendiente",
                "Las comisiones de producto aún no están disponibles; por ahora solo se comisionan servicios.");
        }

        if (!LongitudValida(nombreComercial, 1, 150) ||
            !LongitudOpcionalValida(razonSocial, 180) ||
            !LongitudOpcionalValida(identificacionFiscal, 40) ||
            !LongitudOpcionalValida(direccion, 250) ||
            !LongitudOpcionalValida(telefono, 40) ||
            !LongitudOpcionalValida(correo, 256))
        {
            return new ErrorDominio(
                "configuracion.datos_invalidos",
                "Uno o más datos del establecimiento exceden la longitud permitida o son inválidos.");
        }

        if (!string.IsNullOrWhiteSpace(correo) && !System.Net.Mail.MailAddress.TryCreate(correo, out _))
        {
            return new ErrorDominio("configuracion.correo_invalido", "El correo del establecimiento no es válido.");
        }

        var moneda = codigoMoneda?.Trim();
        if (moneda is null || moneda.Length != 3 || moneda.Any(caracter =>
                !(caracter is >= 'A' and <= 'Z' or >= 'a' and <= 'z')))
        {
            return new ErrorDominio("configuracion.moneda_invalida", "La moneda debe usar un código ISO 4217 de tres letras.");
        }

        return null;
    }

    private static bool LongitudValida(string? valor, int minimo, int maximo) =>
        valor is not null && valor.Trim().Length >= minimo && valor.Trim().Length <= maximo;

    private static bool LongitudOpcionalValida(string? valor, int maximo) =>
        valor is null || valor.Trim().Length <= maximo;

    private static string? Limpiar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
