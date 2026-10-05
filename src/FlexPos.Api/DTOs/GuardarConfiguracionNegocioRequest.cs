namespace FlexPos.Api.DTOs;

public sealed record GuardarConfiguracionNegocioRequest(
    string? NombreComercial,
    string? RazonSocial,
    string? IdentificacionFiscal,
    string? Direccion,
    string? Telefono,
    string? Correo,
    string? CodigoMoneda,
    bool PermitirVentaSinStock,
    bool ComisionProductosHabilitada,
    bool PermitirSaldosPendientes,
    bool FacturacionHabilitada,
    bool ExigirEmpleadoVentaServicio);
