using FlexPos.Domain.Abstractions;
using FlexPos.Domain.Enums;
using FlexPos.Domain.Errors;

namespace FlexPos.Domain.Entities;

public sealed class DocumentoVenta : EntidadAuditable
{
    private DocumentoVenta()
    {
    }

    private DocumentoVenta(
        Guid ventaId,
        TipoDocumentoVenta tipo,
        string? prefijo,
        long numero,
        DateTimeOffset fechaEmisionUtc,
        ConfiguracionNegocio negocio,
        string? clienteNombre,
        string? clienteDocumento,
        string codigoMoneda,
        decimal subtotal,
        decimal descuentos,
        decimal impuestos,
        decimal total)
    {
        VentaId = ventaId;
        TipoDocumento = tipo;
        Prefijo = Limpiar(prefijo);
        Numero = numero;
        NumeroCompleto = $"{Prefijo}{numero}";
        FechaEmisionUtc = fechaEmisionUtc.ToUniversalTime();
        NombreComercial = negocio.NombreComercial;
        RazonSocial = negocio.RazonSocial;
        IdentificacionFiscal = negocio.IdentificacionFiscal;
        Direccion = negocio.Direccion;
        Telefono = negocio.Telefono;
        Correo = negocio.Correo;
        ClienteNombre = Limpiar(clienteNombre);
        ClienteDocumento = Limpiar(clienteDocumento);
        CodigoMoneda = codigoMoneda;
        Subtotal = subtotal;
        Descuentos = descuentos;
        Impuestos = impuestos;
        Total = total;
    }

    public Guid VentaId { get; private set; }
    public TipoDocumentoVenta TipoDocumento { get; private set; }
    public string? Prefijo { get; private set; }
    public long Numero { get; private set; }
    public string NumeroCompleto { get; private set; } = string.Empty;
    public DateTimeOffset FechaEmisionUtc { get; private set; }
    public string NombreComercial { get; private set; } = string.Empty;
    public string? RazonSocial { get; private set; }
    public string? IdentificacionFiscal { get; private set; }
    public string? Direccion { get; private set; }
    public string? Telefono { get; private set; }
    public string? Correo { get; private set; }
    public string? ClienteNombre { get; private set; }
    public string? ClienteDocumento { get; private set; }
    public string CodigoMoneda { get; private set; } = string.Empty;
    public decimal Subtotal { get; private set; }
    public decimal Descuentos { get; private set; }
    public decimal Impuestos { get; private set; }
    public decimal Total { get; private set; }

    public static Resultado<DocumentoVenta> Crear(
        Guid ventaId,
        TipoDocumentoVenta tipo,
        string? prefijo,
        long numero,
        DateTimeOffset fechaEmisionUtc,
        ConfiguracionNegocio negocio,
        string? clienteNombre,
        string? clienteDocumento,
        string codigoMoneda,
        decimal subtotal,
        decimal descuentos,
        decimal impuestos,
        decimal total)
    {
        if (ventaId == Guid.Empty || !Enum.IsDefined(tipo) || numero < 1 || fechaEmisionUtc == default ||
            (!string.IsNullOrWhiteSpace(prefijo) && prefijo.Trim().Length > 20) ||
            subtotal < 0m || descuentos < 0m || impuestos < 0m || total < 0m)
        {
            return Resultado<DocumentoVenta>.Fallo(new ErrorDominio(
                "venta.documento_invalido", "Los datos del documento de venta no son válidos."));
        }

        return Resultado<DocumentoVenta>.Exito(new DocumentoVenta(
            ventaId, tipo, prefijo, numero, fechaEmisionUtc, negocio, clienteNombre,
            clienteDocumento, codigoMoneda, subtotal, descuentos, impuestos, total));
    }

    private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
