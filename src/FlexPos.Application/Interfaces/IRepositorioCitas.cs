using FlexPos.Domain.Entities;
using FlexPos.Domain.Enums;
using FlexPos.Domain.Errors;

namespace FlexPos.Application.Interfaces;

public interface IRepositorioCitas
{
    Task<Cita?> ObtenerAsync(Guid id, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Cita> Elementos, int Total)> ListarAsync(
        DateTimeOffset? desdeUtc,
        DateTimeOffset? hastaUtc,
        Guid? empleadoId,
        Guid? clienteId,
        EstadoCita? estado,
        int omitir,
        int tomar,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<Cita>> ListarReservasAsync(
        DateTimeOffset desdeUtc,
        DateTimeOffset hastaUtc,
        CancellationToken cancellationToken);
    Task<ErrorDominio?> GuardarEnAgendaAsync(
        Cita cita,
        DateTimeOffset inicioLocal,
        CancellationToken cancellationToken);
    Task<bool> GuardarCambiosAsync(CancellationToken cancellationToken);
}
