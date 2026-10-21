using FlexPos.Domain.Entities;
using FlexPos.Domain.Enums;
using FlexPos.Application.DTOs.Finanzas;

namespace FlexPos.Application.Interfaces;

public interface IRepositorioFinanzas
{
    Task<ConfiguracionNegocio?> ObtenerConfiguracionPrincipalAsync(CancellationToken cancellationToken);
    Task<CuentaFinanciera?> ObtenerCuentaAsync(Guid configuracionId, Guid cuentaId, CancellationToken cancellationToken);
    Task<CuentaFinanciera?> ObtenerCuentaBaseAsync(
        Guid configuracionId, TipoCuentaFinanciera tipo, string nombre, CancellationToken cancellationToken);
    Task<MetodoPagoConfigurado?> ObtenerMetodoPagoAsync(
        Guid configuracionId, Guid metodoPagoId, CancellationToken cancellationToken);
    Task<(IReadOnlyList<CuentaFinanciera> Elementos, int Total)> ListarCuentasAsync(
        Guid configuracionId,
        TipoCuentaFinanciera? tipo,
        bool? activa,
        string? buscar,
        int omitir,
        int tomar,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<MovimientoFinanciero>> ListarMovimientosAsync(
        Guid cuentaId,
        DateTimeOffset? desdeUtc,
        DateTimeOffset? hastaUtc,
        string? tipo,
        int omitir,
        int tomar,
        CancellationToken cancellationToken);
    Task<int> ContarMovimientosAsync(
        Guid cuentaId,
        DateTimeOffset? desdeUtc,
        DateTimeOffset? hastaUtc,
        string? tipo,
        CancellationToken cancellationToken);
    Task<decimal> CalcularSaldoAntesAsync(
        Guid cuentaId,
        DateTimeOffset fechaUtc,
        Guid movimientoId,
        CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, decimal>> CalcularSaldosAsync(
        IReadOnlyCollection<Guid> cuentaIds,
        CancellationToken cancellationToken);
    Task<(IReadOnlyList<TransferenciaFinancieraDto> Elementos, int Total)> ListarTransferenciasAsync(
        Guid configuracionId,
        DateTimeOffset? desdeUtc,
        DateTimeOffset? hastaUtc,
        Guid? cuentaId,
        int omitir,
        int tomar,
        CancellationToken cancellationToken);
    Task<IUnidadTrabajoFinanzas> IniciarTransaccionAsync(CancellationToken cancellationToken);
    void Agregar(CuentaFinanciera cuenta);
    Task<bool> GuardarCambiosAsync(CancellationToken cancellationToken);
}

public interface IUnidadTrabajoFinanzas : IAsyncDisposable
{
    Task<Caja?> BloquearCajaActualAsync(CancellationToken cancellationToken);
    Task<CuentaFinanciera?> BloquearCuentaAsync(Guid configuracionId, Guid cuentaId, CancellationToken cancellationToken);
    Task<CuentaFinanciera?> BloquearCuentaBaseAsync(
        Guid configuracionId, TipoCuentaFinanciera tipo, string nombre, CancellationToken cancellationToken);
    Task<MetodoPagoConfigurado?> BloquearMetodoPagoAsync(
        Guid configuracionId, Guid metodoPagoId, CancellationToken cancellationToken);
    Task<bool> TieneMovimientosAsync(Guid cuentaId, CancellationToken cancellationToken);
    Task<bool> TieneMetodoPagoAsync(Guid cuentaId, CancellationToken cancellationToken);
    Task<bool> TieneMetodoPagoActivoAsync(Guid cuentaId, CancellationToken cancellationToken);
    Task<bool> TieneCajaAbiertaAsync(Guid cuentaId, CancellationToken cancellationToken);
    Task<decimal> CalcularSaldoAsync(Guid cuentaId, CancellationToken cancellationToken);
    void Agregar(TransferenciaFinanciera transferencia);
    void Agregar(MovimientoFinanciero movimiento);
    void Agregar(CuentaFinanciera cuenta);
    Task<bool> GuardarCambiosAsync(CancellationToken cancellationToken);
    Task ConfirmarAsync(CancellationToken cancellationToken);
}
