using FlexPos.Domain.Entities;
using FlexPos.Domain.Enums;

namespace FlexPos.Application.Interfaces;

public interface IRepositorioPuntoVenta
{
    Task<IUnidadTrabajoPuntoVenta> IniciarTransaccionAsync(CancellationToken cancellationToken);
    Task<Caja?> ObtenerCajaActualAsync(CancellationToken cancellationToken);
    Task<Caja?> ObtenerCajaAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<MovimientoCaja>> ObtenerMovimientosCajaAsync(Guid cajaId, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Caja> Elementos, int Total)> ListarCajasAsync(
        DateTimeOffset? desdeUtc,
        DateTimeOffset? hastaUtc,
        int omitir,
        int tomar,
        CancellationToken cancellationToken);
    Task<Venta?> ObtenerVentaAsync(Guid id, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Venta> Elementos, int Total)> ListarVentasAsync(
        DateTimeOffset? desdeUtc,
        DateTimeOffset? hastaUtc,
        Guid? clienteId,
        EstadoVenta? estado,
        int omitir,
        int tomar,
        CancellationToken cancellationToken);
}

public interface IUnidadTrabajoPuntoVenta : IAsyncDisposable
{
    Task<ConfiguracionNegocio?> ObtenerConfiguracionPrincipalAsync(CancellationToken cancellationToken);
    Task<Caja?> ObtenerCajaActualAsync(CancellationToken cancellationToken);
    Task<Caja?> BloquearCajaActualAsync(CancellationToken cancellationToken);
    Task<Caja?> BloquearCajaAsync(Guid id, CancellationToken cancellationToken);
    Task<decimal> CalcularEfectivoEsperadoAsync(Guid cajaId, CancellationToken cancellationToken);
    Task<Cliente?> ObtenerClienteAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ArticuloInventario>> ObtenerArticulosAsync(
        IReadOnlyCollection<Guid> ids,
        bool bloquear,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<Servicio>> ObtenerServiciosAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<Empleado>> ObtenerEmpleadosAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<ReglaComision>> ObtenerReglasComisionAsync(
        IReadOnlyCollection<Guid> empleadoIds,
        IReadOnlyCollection<Guid> servicioIds,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<Comision>> ObtenerComisionesVentaAsync(
        IReadOnlyCollection<Guid> detalleVentaIds,
        bool bloquear,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<ImpuestoConfigurado>> ObtenerImpuestosAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<MetodoPagoConfigurado>> ObtenerMetodosPagoAsync(CancellationToken cancellationToken);
    Task<NumeracionDocumento?> BloquearNumeracionAsync(
        TipoDocumentoVenta tipo,
        CancellationToken cancellationToken);
    Task<Venta?> BloquearVentaAsync(Guid id, CancellationToken cancellationToken);
    void Agregar(Caja caja);
    void Agregar(Venta venta);
    void Agregar(MovimientoCaja movimiento);
    void Agregar(MovimientoInventario movimiento);
    void Agregar(Comision comision);
    Task<bool> GuardarCambiosAsync(CancellationToken cancellationToken);
    Task ConfirmarAsync(CancellationToken cancellationToken);
}

public interface IGeneradorDocumentoVenta
{
    byte[] Generar(Venta venta, DocumentoVenta documento);
}
