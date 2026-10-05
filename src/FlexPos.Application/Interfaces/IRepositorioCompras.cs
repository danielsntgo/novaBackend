using FlexPos.Domain.Entities;
using FlexPos.Domain.Enums;

namespace FlexPos.Application.Interfaces;

public interface IRepositorioCompras
{
    Task<Compra?> ObtenerAsync(Guid id, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Compra> Elementos, int Total)> ListarAsync(
        string? busquedaNormalizada,
        EstadoCompra? estado,
        DateTimeOffset? desdeUtc,
        DateTimeOffset? hastaUtc,
        int omitir,
        int tomar,
        CancellationToken cancellationToken);
    Task<bool> ExisteAlgunaAsync(CancellationToken cancellationToken);
    void Agregar(Compra compra);
    Task<bool> GuardarCambiosAsync(CancellationToken cancellationToken);
}
