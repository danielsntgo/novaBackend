using FlexPos.Domain.Entities;
using FlexPos.Domain.Enums;

namespace FlexPos.Application.Interfaces;

public interface IRepositorioConfiguracion
{
    Task<ConfiguracionNegocio?> ObtenerPrincipalAsync(CancellationToken cancellationToken);
    void Agregar(ConfiguracionNegocio configuracion);
    Task<IReadOnlyList<ImpuestoConfigurado>> ListarImpuestosAsync(Guid configuracionId, CancellationToken cancellationToken);
    Task<ImpuestoConfigurado?> ObtenerImpuestoAsync(Guid configuracionId, Guid impuestoId, CancellationToken cancellationToken);
    void Agregar(ImpuestoConfigurado impuesto);
    Task<IReadOnlyList<MetodoPagoConfigurado>> ListarMetodosPagoAsync(Guid configuracionId, CancellationToken cancellationToken);
    Task<MetodoPagoConfigurado?> ObtenerMetodoPagoAsync(Guid configuracionId, Guid metodoId, CancellationToken cancellationToken);
    Task<bool> ExisteOtroMetodoEfectivoAsync(Guid configuracionId, Guid? excluirId, CancellationToken cancellationToken);
    void Agregar(MetodoPagoConfigurado metodo);
    Task<IReadOnlyList<NumeracionDocumento>> ListarNumeracionesAsync(Guid configuracionId, CancellationToken cancellationToken);
    Task<NumeracionDocumento?> ObtenerNumeracionAsync(Guid configuracionId, TipoDocumentoVenta tipoDocumento, CancellationToken cancellationToken);
    Task<bool> ExisteCajaOVentaAsync(CancellationToken cancellationToken);
    void Agregar(NumeracionDocumento numeracion);
    Task<bool> GuardarCambiosAsync(CancellationToken cancellationToken);
}
