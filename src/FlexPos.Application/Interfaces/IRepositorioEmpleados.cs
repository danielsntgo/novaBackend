using FlexPos.Domain.Entities;
using FlexPos.Domain.Errors;

namespace FlexPos.Application.Interfaces;

public interface IRepositorioEmpleados
{
    Task<Empleado?> ObtenerAsync(Guid id, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Empleado> Elementos, int Total)> ListarAsync(
        string? busquedaNormalizada,
        bool? activo,
        int omitir,
        int tomar,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<Empleado>> ListarActivosConHorarioAsync(CancellationToken cancellationToken);
    void Agregar(Empleado empleado);
    Task<bool> GuardarCambiosAsync(CancellationToken cancellationToken);
    Task<ErrorDominio?> GuardarHorarioConBloqueoAsync(Empleado empleado, CancellationToken cancellationToken);
}
