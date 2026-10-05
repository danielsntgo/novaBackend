using System.Net.Mail;
using FlexPos.Application.DTOs.Usuarios;
using FlexPos.Application.Interfaces;
using FlexPos.Domain.Errors;

namespace FlexPos.Application.Services;

public sealed class ServicioUsuarios(
    IAdministracionUsuarios administracion,
    IRepositorioTokensRenovacion repositorioTokens,
    IUsuarioActual usuarioActual,
    TimeProvider reloj)
{
    public async Task<Resultado<UsuarioAdministradoDto>> ObtenerRecepcionistaAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return Resultado<UsuarioAdministradoDto>.Fallo(new ErrorDominio(
                "usuario.identificador_invalido",
                "El identificador del usuario no es válido."));
        }

        var usuario = await administracion.ObtenerRecepcionistaAsync(id, cancellationToken);
        return usuario is null
            ? Resultado<UsuarioAdministradoDto>.Fallo(new ErrorDominio(
                "usuario.no_encontrado",
                "No se encontró una cuenta de Recepcionista con ese identificador."))
            : Resultado<UsuarioAdministradoDto>.Exito(usuario);
    }

    public async Task<Resultado<PaginaUsuariosDto>> ListarRecepcionistasAsync(
        string? buscar,
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken)
    {
        if (pagina < 1 || tamanoPagina is < 1 or > 100 ||
            (pagina - 1L) * tamanoPagina > int.MaxValue ||
            (buscar?.Trim().Length ?? 0) > 120)
        {
            return Resultado<PaginaUsuariosDto>.Fallo(new ErrorDominio(
                "usuario.busqueda_invalida",
                "La paginación o el texto de búsqueda no son válidos."));
        }

        var paginaUsuarios = await administracion.ListarRecepcionistasAsync(
            buscar, pagina, tamanoPagina, cancellationToken);
        return Resultado<PaginaUsuariosDto>.Exito(paginaUsuarios);
    }

    public Task<Resultado<RecepcionistaCreadaDto>> CrearRecepcionistaAsync(
        string? correo,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(correo) || !MailAddress.TryCreate(correo.Trim(), out var direccion) ||
            !string.Equals(direccion.Address, correo.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(Resultado<RecepcionistaCreadaDto>.Fallo(new ErrorDominio(
                "usuario.correo_invalido",
                "Debe indicar un correo electrónico válido.")));
        }

        return administracion.CrearRecepcionistaAsync(direccion.Address, cancellationToken);
    }

    public async Task<Resultado<UsuarioAdministradoDto>> EstablecerEstadoAsync(
        Guid id,
        bool activo,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return Resultado<UsuarioAdministradoDto>.Fallo(new ErrorDominio(
                "usuario.identificador_invalido",
                "El identificador del usuario no es válido."));
        }

        if (!activo && usuarioActual.ObtenerId() == id)
        {
            return Resultado<UsuarioAdministradoDto>.Fallo(new ErrorDominio(
                "usuario.no_puede_desactivarse",
                "No puede desactivar la cuenta con la que inició sesión."));
        }

        var resultado = await administracion.EstablecerEstadoAsync(id, activo, cancellationToken);
        if (resultado.EsExitoso && !activo)
        {
            await repositorioTokens.RevocarActivosAsync(id, reloj.GetUtcNow(), cancellationToken);
        }

        return resultado;
    }
}
