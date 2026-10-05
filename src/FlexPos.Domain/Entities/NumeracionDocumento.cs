using FlexPos.Domain.Abstractions;
using FlexPos.Domain.Enums;
using FlexPos.Domain.Errors;

namespace FlexPos.Domain.Entities;

public sealed class NumeracionDocumento : EntidadAuditable
{
    private NumeracionDocumento()
    {
    }

    private NumeracionDocumento(
        Guid configuracionNegocioId,
        TipoDocumentoVenta tipoDocumento,
        string? prefijo,
        long siguienteNumero)
    {
        ConfiguracionNegocioId = configuracionNegocioId;
        TipoDocumento = tipoDocumento;
        Prefijo = Limpiar(prefijo);
        SiguienteNumero = siguienteNumero;
    }

    public Guid ConfiguracionNegocioId { get; private set; }
    public TipoDocumentoVenta TipoDocumento { get; private set; }
    public string? Prefijo { get; private set; }
    public long SiguienteNumero { get; private set; }

    public static Resultado<NumeracionDocumento> Crear(
        Guid configuracionNegocioId,
        TipoDocumentoVenta tipoDocumento,
        string? prefijo,
        long siguienteNumero)
    {
        var error = Validar(configuracionNegocioId, tipoDocumento, prefijo, siguienteNumero);
        return error is null
            ? Resultado<NumeracionDocumento>.Exito(new NumeracionDocumento(
                configuracionNegocioId, tipoDocumento, prefijo, siguienteNumero))
            : Resultado<NumeracionDocumento>.Fallo(error);
    }

    public Resultado<bool> ActualizarPrefijo(string? prefijo)
    {
        if (!PrefijoValido(prefijo))
        {
            return Resultado<bool>.Fallo(new ErrorDominio("numeracion.prefijo_invalido", "El prefijo no puede superar 20 caracteres."));
        }

        Prefijo = Limpiar(prefijo);
        return Resultado<bool>.Exito(true);
    }

    public Resultado<long> TomarSiguienteNumero()
    {
        if (SiguienteNumero == long.MaxValue)
        {
            return Resultado<long>.Fallo(new ErrorDominio("numeracion.limite_alcanzado", "La numeración alcanzó su límite."));
        }

        var asignado = SiguienteNumero;
        SiguienteNumero++;
        return Resultado<long>.Exito(asignado);
    }

    private static ErrorDominio? Validar(
        Guid configuracionNegocioId,
        TipoDocumentoVenta tipoDocumento,
        string? prefijo,
        long siguienteNumero)
    {
        if (configuracionNegocioId == Guid.Empty || !Enum.IsDefined(tipoDocumento) || siguienteNumero < 1)
        {
            return new ErrorDominio("numeracion.datos_invalidos", "La numeración requiere una configuración, tipo y número inicial válidos.");
        }

        return PrefijoValido(prefijo)
            ? null
            : new ErrorDominio("numeracion.prefijo_invalido", "El prefijo no puede superar 20 caracteres.");
    }

    private static bool PrefijoValido(string? prefijo) => prefijo is null || prefijo.Trim().Length <= 20;

    private static string? Limpiar(string? prefijo) =>
        string.IsNullOrWhiteSpace(prefijo) ? null : prefijo.Trim();
}
