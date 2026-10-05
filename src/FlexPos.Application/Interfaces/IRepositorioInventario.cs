using FlexPos.Domain.Entities;
using FlexPos.Domain.Enums;

namespace FlexPos.Application.Interfaces;

public interface IRepositorioInventario
{
    Task<ArticuloInventario?> ObtenerArticuloAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ArticuloInventario>> ObtenerArticulosAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);
    Task<(IReadOnlyList<ArticuloInventario> Elementos, int Total)> ListarArticulosAsync(
        string? busquedaNormalizada,
        TipoArticuloInventario? tipo,
        bool? activo,
        bool soloBajoMinimo,
        int omitir,
        int tomar,
        CancellationToken cancellationToken);
    Task<bool> TieneMovimientosAsync(Guid articuloId, CancellationToken cancellationToken);
    Task<(IReadOnlyList<MovimientoInventario> Elementos, int Total)> ListarMovimientosAsync(
        Guid? articuloId,
        DateTimeOffset? desdeUtc,
        DateTimeOffset? hastaUtc,
        int omitir,
        int tomar,
        CancellationToken cancellationToken);
    Task<bool> ExisteAlgunoAsync(CancellationToken cancellationToken);
    void AgregarArticulo(ArticuloInventario articulo);
    void AgregarMovimiento(MovimientoInventario movimiento);
    Task<bool> GuardarCambiosAsync(CancellationToken cancellationToken);
}
