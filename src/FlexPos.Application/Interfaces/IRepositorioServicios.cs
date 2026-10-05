using FlexPos.Domain.Entities;

namespace FlexPos.Application.Interfaces;

public interface IRepositorioServicios
{
    Task<Servicio?> ObtenerAsync(Guid id, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Servicio> Elementos, int Total)> ListarAsync(
        string? busquedaNormalizada,
        bool? activo,
        int omitir,
        int tomar,
        CancellationToken cancellationToken);
    Task<bool> ExisteAlgunoAsync(CancellationToken cancellationToken);
    void Agregar(Servicio servicio);
    Task<bool> GuardarCambiosAsync(CancellationToken cancellationToken);
}
