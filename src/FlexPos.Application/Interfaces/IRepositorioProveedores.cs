using FlexPos.Domain.Entities;

namespace FlexPos.Application.Interfaces;

public interface IRepositorioProveedores
{
    Task<Proveedor?> ObtenerAsync(Guid id, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Proveedor> Elementos, int Total)> ListarAsync(
        string? busquedaNormalizada,
        bool? activo,
        int omitir,
        int tomar,
        CancellationToken cancellationToken);
    void Agregar(Proveedor proveedor);
    Task<bool> GuardarCambiosAsync(CancellationToken cancellationToken);
}
