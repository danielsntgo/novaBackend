using FlexPos.Application.DTOs.Proveedores;
using FlexPos.Application.Interfaces;
using FlexPos.Domain.Entities;
using FlexPos.Domain.Errors;
using FlexPos.Domain.ValueObjects;

namespace FlexPos.Application.Services;

public sealed class ServicioProveedores(IRepositorioProveedores repositorio)
{
    public async Task<Resultado<PaginaProveedoresDto>> ListarAsync(
        string? buscar, bool? activo, int pagina, int tamanoPagina, CancellationToken cancellationToken)
    {
        if (!ValidacionListado.EsValida(buscar, pagina, tamanoPagina))
        {
            return Resultado<PaginaProveedoresDto>.Fallo(new ErrorDominio(
                "proveedor.busqueda_invalida", "La paginacion o la busqueda no son validas."));
        }

        var consulta = await repositorio.ListarAsync(
            TextoNormalizado.Normalizar(buscar), activo,
            ValidacionListado.CalcularOmitir(pagina, tamanoPagina), tamanoPagina, cancellationToken);
        return Resultado<PaginaProveedoresDto>.Exito(new PaginaProveedoresDto(
            consulta.Elementos.Select(Convertir).ToArray(), pagina, tamanoPagina, consulta.Total));
    }

    public async Task<Resultado<ProveedorDto>> ObtenerAsync(Guid id, CancellationToken cancellationToken)
    {
        var proveedor = id == Guid.Empty ? null : await repositorio.ObtenerAsync(id, cancellationToken);
        return proveedor is null
            ? Resultado<ProveedorDto>.Fallo(NoEncontrado())
            : Resultado<ProveedorDto>.Exito(Convertir(proveedor));
    }

    public async Task<Resultado<ProveedorDto>> CrearAsync(
        SolicitudProveedor solicitud, CancellationToken cancellationToken)
    {
        var nuevo = Proveedor.Crear(
            solicitud.Nombre, solicitud.IdentificacionFiscal, solicitud.Telefono, solicitud.Correo);
        if (!nuevo.EsExitoso)
        {
            return Resultado<ProveedorDto>.Fallo(nuevo.Error!);
        }

        repositorio.Agregar(nuevo.Valor!);
        return await GuardarAsync(nuevo.Valor!, cancellationToken);
    }

    public async Task<Resultado<ProveedorDto>> ActualizarAsync(
        Guid id, SolicitudProveedor solicitud, CancellationToken cancellationToken)
    {
        var proveedor = id == Guid.Empty ? null : await repositorio.ObtenerAsync(id, cancellationToken);
        if (proveedor is null)
        {
            return Resultado<ProveedorDto>.Fallo(NoEncontrado());
        }

        var actualizacion = proveedor.Actualizar(
            solicitud.Nombre, solicitud.IdentificacionFiscal, solicitud.Telefono, solicitud.Correo);
        return actualizacion.EsExitoso
            ? await GuardarAsync(proveedor, cancellationToken)
            : Resultado<ProveedorDto>.Fallo(actualizacion.Error!);
    }

    public async Task<Resultado<ProveedorDto>> EstablecerEstadoAsync(
        Guid id, bool activo, CancellationToken cancellationToken)
    {
        var proveedor = id == Guid.Empty ? null : await repositorio.ObtenerAsync(id, cancellationToken);
        if (proveedor is null)
        {
            return Resultado<ProveedorDto>.Fallo(NoEncontrado());
        }

        proveedor.EstablecerEstado(activo);
        return await GuardarAsync(proveedor, cancellationToken);
    }

    private async Task<Resultado<ProveedorDto>> GuardarAsync(
        Proveedor proveedor, CancellationToken cancellationToken)
    {
        if (!await repositorio.GuardarCambiosAsync(cancellationToken))
        {
            return Resultado<ProveedorDto>.Fallo(new ErrorDominio(
                "proveedor.conflicto", "El proveedor ya existe o cambio en otra operacion."));
        }

        return Resultado<ProveedorDto>.Exito(Convertir(proveedor));
    }

    internal static ProveedorDto Convertir(Proveedor proveedor) => new(
        proveedor.Id, proveedor.Nombre, proveedor.IdentificacionFiscal, proveedor.Telefono,
        proveedor.Correo, proveedor.Activo, proveedor.FechaCreacionUtc, proveedor.FechaModificacionUtc);

    private static ErrorDominio NoEncontrado() => new(
        "proveedor.no_encontrado", "No se encontro el proveedor solicitado.");
}
