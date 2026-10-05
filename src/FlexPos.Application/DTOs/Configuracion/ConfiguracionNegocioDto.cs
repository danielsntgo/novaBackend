namespace FlexPos.Application.DTOs.Configuracion;

public sealed record ConfiguracionNegocioDto(
    Guid Id,
    string NombreComercial,
    string? RazonSocial,
    string? IdentificacionFiscal,
    string? Direccion,
    string? Telefono,
    string? Correo,
    string CodigoMoneda,
    bool PermitirVentaSinStock,
    bool ComisionProductosHabilitada,
    bool PermitirSaldosPendientes,
    bool FacturacionHabilitada,
    bool ExigirEmpleadoVentaServicio,
    DateTimeOffset FechaCreacionUtc,
    DateTimeOffset? FechaModificacionUtc);

public sealed record SolicitudConfiguracionNegocio(
    string? NombreComercial,
    string? RazonSocial,
    string? IdentificacionFiscal,
    string? Direccion,
    string? Telefono,
    string? Correo,
    string? CodigoMoneda,
    bool PermitirVentaSinStock,
    bool ComisionProductosHabilitada,
    bool PermitirSaldosPendientes = false,
    bool FacturacionHabilitada = false,
    bool ExigirEmpleadoVentaServicio = false);
