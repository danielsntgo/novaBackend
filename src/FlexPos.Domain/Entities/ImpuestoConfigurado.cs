using FlexPos.Domain.Abstractions;
using FlexPos.Domain.Errors;

namespace FlexPos.Domain.Entities;

public sealed class ImpuestoConfigurado : EntidadAuditable
{
    private ImpuestoConfigurado()
    {
    }

    private ImpuestoConfigurado(Guid configuracionNegocioId, string nombre, decimal porcentaje)
    {
        ConfiguracionNegocioId = configuracionNegocioId;
        Nombre = nombre.Trim();
        NombreNormalizado = nombre.Trim().ToUpperInvariant();
        Porcentaje = porcentaje;
    }

    public Guid ConfiguracionNegocioId { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public string NombreNormalizado { get; private set; } = string.Empty;
    public decimal Porcentaje { get; private set; }
    public bool Activo { get; private set; } = true;

    public static Resultado<ImpuestoConfigurado> Crear(
        Guid configuracionNegocioId,
        string? nombre,
        decimal porcentaje)
    {
        var error = Validar(configuracionNegocioId, nombre, porcentaje);
        return error is null
            ? Resultado<ImpuestoConfigurado>.Exito(new ImpuestoConfigurado(configuracionNegocioId, nombre!, porcentaje))
            : Resultado<ImpuestoConfigurado>.Fallo(error);
    }

    public Resultado<bool> Actualizar(string? nombre, decimal porcentaje)
    {
        var error = Validar(ConfiguracionNegocioId, nombre, porcentaje);
        if (error is not null)
        {
            return Resultado<bool>.Fallo(error);
        }

        Nombre = nombre!.Trim();
        NombreNormalizado = Nombre.ToUpperInvariant();
        Porcentaje = porcentaje;
        return Resultado<bool>.Exito(true);
    }

    public Resultado<bool> Desactivar()
        => EstablecerEstado(false);

    public Resultado<bool> EstablecerEstado(bool activo)
    {
        Activo = activo;
        return Resultado<bool>.Exito(true);
    }

    private static ErrorDominio? Validar(Guid configuracionNegocioId, string? nombre, decimal porcentaje)
    {
        if (configuracionNegocioId == Guid.Empty || string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 100)
        {
            return new ErrorDominio("impuesto.datos_invalidos", "El impuesto requiere configuración y un nombre válido.");
        }

        if (porcentaje is < 0 or > 100 || decimal.Round(porcentaje, 2) != porcentaje)
        {
            return new ErrorDominio("impuesto.porcentaje_invalido", "El porcentaje debe estar entre 0 y 100, con máximo dos decimales.");
        }

        return null;
    }
}
