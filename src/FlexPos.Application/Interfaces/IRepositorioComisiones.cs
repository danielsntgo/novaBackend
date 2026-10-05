using FlexPos.Domain.Entities;
using FlexPos.Domain.Enums;

namespace FlexPos.Application.Interfaces;

public interface IRepositorioComisiones
{
    Task<(IReadOnlyList<ReglaComision> Elementos, int Total)> ListarReglasAsync(
        Guid? empleadoId,
        Guid? servicioId,
        int omitir,
        int tomar,
        CancellationToken cancellationToken);
    Task<(IReadOnlyList<Comision> Elementos, int Total)> ListarComisionesAsync(
        Guid? empleadoId,
        EstadoComision? estado,
        TipoMovimientoComision? tipoMovimiento,
        DateTimeOffset? desdeUtc,
        DateTimeOffset? hastaUtc,
        int omitir,
        int tomar,
        CancellationToken cancellationToken);
    Task<(IReadOnlyList<LiquidacionComision> Elementos, int Total)> ListarLiquidacionesAsync(
        Guid? empleadoId,
        DateTimeOffset? desdeUtc,
        DateTimeOffset? hastaUtc,
        int omitir,
        int tomar,
        CancellationToken cancellationToken);
    Task<LiquidacionComision?> ObtenerLiquidacionAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Comision>> ObtenerMovimientosLiquidacionAsync(
        Guid liquidacionId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<Comision>> ObtenerMovimientosLiquidacionesAsync(
        IReadOnlyCollection<Guid> liquidacionIds,
        CancellationToken cancellationToken);
    Task<IUnidadTrabajoComisiones> IniciarTransaccionAsync(CancellationToken cancellationToken);
}

public interface IUnidadTrabajoComisiones : IAsyncDisposable
{
    Task<ConfiguracionNegocio?> ObtenerConfiguracionPrincipalAsync(CancellationToken cancellationToken);
    Task<Empleado?> ObtenerEmpleadoAsync(Guid id, CancellationToken cancellationToken);
    Task<Servicio?> ObtenerServicioAsync(Guid id, CancellationToken cancellationToken);
    Task<MetodoPagoConfigurado?> ObtenerMetodoPagoAsync(Guid id, CancellationToken cancellationToken);
    Task<Caja?> BloquearCajaActualAsync(CancellationToken cancellationToken);
    Task<ReglaComision?> BloquearReglaAsync(Guid id, CancellationToken cancellationToken);
    Task<ReglaComision?> BloquearReglaEmpleadoServicioAsync(
        Guid empleadoId,
        Guid servicioId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<Comision>> BloquearComisionesSeleccionadasAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<Comision>> BloquearAjustesPendientesAsync(
        Guid empleadoId,
        CancellationToken cancellationToken);
    void Agregar(ReglaComision regla);
    void Agregar(LiquidacionComision liquidacion);
    void Agregar(MovimientoCaja movimiento);
    Task<bool> GuardarCambiosAsync(CancellationToken cancellationToken);
    Task ConfirmarAsync(CancellationToken cancellationToken);
}
