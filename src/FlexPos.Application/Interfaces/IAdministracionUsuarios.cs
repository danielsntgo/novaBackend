using FlexPos.Application.DTOs.Usuarios;
using FlexPos.Domain.Errors;

namespace FlexPos.Application.Interfaces;

public interface IAdministracionUsuarios
{
    Task<UsuarioAdministradoDto?> ObtenerRecepcionistaAsync(Guid usuarioId, CancellationToken cancellationToken);

    Task<PaginaUsuariosDto> ListarRecepcionistasAsync(
        string? buscar,
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken);

    Task<Resultado<RecepcionistaCreadaDto>> CrearRecepcionistaAsync(
        string correo,
        CancellationToken cancellationToken);

    Task<Resultado<UsuarioAdministradoDto>> EstablecerEstadoAsync(
        Guid usuarioId,
        bool activo,
        CancellationToken cancellationToken);
}
