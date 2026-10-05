using FlexPos.Domain.Entities;

namespace FlexPos.Application.Interfaces;

public interface IRepositorioClientes
{
    Task<Cliente?> ObtenerAsync(Guid id, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Cliente> Elementos, int Total)> ListarAsync(
        string? busquedaNormalizada,
        bool? activo,
        int omitir,
        int tomar,
        CancellationToken cancellationToken);
    void Agregar(Cliente cliente);
    Task<bool> GuardarCambiosAsync(CancellationToken cancellationToken);
}
