using FlexPos.Domain.Abstractions;
using FlexPos.Domain.Errors;

namespace FlexPos.Domain.Entities;

public sealed class MetodoPagoConfigurado : EntidadAuditable
{
    private MetodoPagoConfigurado()
    {
    }

    private MetodoPagoConfigurado(Guid configuracionNegocioId, string nombre, bool requiereReferencia, bool esEfectivo)
    {
        ConfiguracionNegocioId = configuracionNegocioId;
        Nombre = nombre.Trim();
        NombreNormalizado = nombre.Trim().ToUpperInvariant();
        RequiereReferencia = requiereReferencia;
        EsEfectivo = esEfectivo;
    }

    public Guid ConfiguracionNegocioId { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public string NombreNormalizado { get; private set; } = string.Empty;
    public bool RequiereReferencia { get; private set; }
    public bool EsEfectivo { get; private set; }
    public bool Activo { get; private set; } = true;

    public static Resultado<MetodoPagoConfigurado> Crear(
        Guid configuracionNegocioId,
        string? nombre,
        bool requiereReferencia,
        bool esEfectivo = false)
    {
        var error = Validar(configuracionNegocioId, nombre);
        return error is null
            ? Resultado<MetodoPagoConfigurado>.Exito(new MetodoPagoConfigurado(configuracionNegocioId, nombre!, requiereReferencia, esEfectivo))
            : Resultado<MetodoPagoConfigurado>.Fallo(error);
    }

    public Resultado<bool> Actualizar(string? nombre, bool requiereReferencia, bool esEfectivo = false)
    {
        var error = Validar(ConfiguracionNegocioId, nombre);
        if (error is not null)
        {
            return Resultado<bool>.Fallo(error);
        }

        Nombre = nombre!.Trim();
        NombreNormalizado = Nombre.ToUpperInvariant();
        RequiereReferencia = requiereReferencia;
        EsEfectivo = esEfectivo;
        return Resultado<bool>.Exito(true);
    }

    public Resultado<bool> Desactivar()
        => EstablecerEstado(false);

    public Resultado<bool> EstablecerEstado(bool activo)
    {
        Activo = activo;
        return Resultado<bool>.Exito(true);
    }

    private static ErrorDominio? Validar(Guid configuracionNegocioId, string? nombre) =>
        configuracionNegocioId == Guid.Empty || string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 80
            ? new ErrorDominio("metodo_pago.datos_invalidos", "El método de pago requiere configuración y un nombre válido.")
            : null;
}
