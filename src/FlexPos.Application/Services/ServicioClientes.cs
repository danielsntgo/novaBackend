using FlexPos.Application.DTOs.Clientes;
using FlexPos.Application.Interfaces;
using FlexPos.Domain.Entities;
using FlexPos.Domain.Errors;
using FlexPos.Domain.ValueObjects;

namespace FlexPos.Application.Services;

public sealed class ServicioClientes(IRepositorioClientes repositorio)
{
    public async Task<Resultado<PaginaClientesDto>> ListarAsync(
        string? buscar,
        bool? activo,
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken)
    {
        if (!ValidacionListado.EsValida(buscar, pagina, tamanoPagina))
        {
            return Resultado<PaginaClientesDto>.Fallo(ErrorBusqueda());
        }

        var resultado = await repositorio.ListarAsync(
            TextoNormalizado.Normalizar(buscar), activo,
            ValidacionListado.CalcularOmitir(pagina, tamanoPagina), tamanoPagina, cancellationToken);
        return Resultado<PaginaClientesDto>.Exito(new PaginaClientesDto(
            resultado.Elementos.Select(Convertir).ToArray(), pagina, tamanoPagina, resultado.Total));
    }

    public async Task<Resultado<ClienteDto>> ObtenerAsync(Guid id, CancellationToken cancellationToken)
    {
        var cliente = id == Guid.Empty ? null : await repositorio.ObtenerAsync(id, cancellationToken);
        return cliente is null
            ? Resultado<ClienteDto>.Fallo(NoEncontrado())
            : Resultado<ClienteDto>.Exito(Convertir(cliente));
    }

    public async Task<Resultado<ClienteDto>> CrearAsync(
        SolicitudCliente solicitud,
        CancellationToken cancellationToken)
    {
        var nuevo = Cliente.Crear(solicitud.Nombre, solicitud.Documento, solicitud.Telefono, solicitud.Correo);
        if (!nuevo.EsExitoso)
        {
            return Resultado<ClienteDto>.Fallo(nuevo.Error!);
        }

        repositorio.Agregar(nuevo.Valor!);
        return await GuardarYConvertirAsync(nuevo.Valor!, cancellationToken);
    }

    public async Task<Resultado<ClienteDto>> ActualizarAsync(
        Guid id,
        SolicitudCliente solicitud,
        CancellationToken cancellationToken)
    {
        var cliente = id == Guid.Empty ? null : await repositorio.ObtenerAsync(id, cancellationToken);
        if (cliente is null)
        {
            return Resultado<ClienteDto>.Fallo(NoEncontrado());
        }

        var actualizacion = cliente.Actualizar(
            solicitud.Nombre, solicitud.Documento, solicitud.Telefono, solicitud.Correo);
        if (!actualizacion.EsExitoso)
        {
            return Resultado<ClienteDto>.Fallo(actualizacion.Error!);
        }

        return await GuardarYConvertirAsync(cliente, cancellationToken);
    }

    public async Task<Resultado<ClienteDto>> EstablecerEstadoAsync(
        Guid id,
        bool activo,
        CancellationToken cancellationToken)
    {
        var cliente = id == Guid.Empty ? null : await repositorio.ObtenerAsync(id, cancellationToken);
        if (cliente is null)
        {
            return Resultado<ClienteDto>.Fallo(NoEncontrado());
        }

        cliente.EstablecerEstado(activo);
        return await GuardarYConvertirAsync(cliente, cancellationToken);
    }

    private async Task<Resultado<ClienteDto>> GuardarYConvertirAsync(
        Cliente cliente,
        CancellationToken cancellationToken)
    {
        if (!await repositorio.GuardarCambiosAsync(cancellationToken))
        {
            return Resultado<ClienteDto>.Fallo(Conflicto());
        }

        return Resultado<ClienteDto>.Exito(Convertir(cliente));
    }

    private static ClienteDto Convertir(Cliente cliente) => new(
        cliente.Id, cliente.Nombre, cliente.Documento, cliente.Telefono, cliente.Correo,
        cliente.Activo, cliente.FechaCreacionUtc, cliente.FechaModificacionUtc);

    private static ErrorDominio ErrorBusqueda() => new(
        "cliente.busqueda_invalida", "La paginación o el texto de búsqueda no son válidos.");

    private static ErrorDominio NoEncontrado() => new(
        "cliente.no_encontrado", "No se encontró el cliente solicitado.");

    private static ErrorDominio Conflicto() => new(
        "cliente.conflicto", "El cliente cambió en otra operación.");
}
