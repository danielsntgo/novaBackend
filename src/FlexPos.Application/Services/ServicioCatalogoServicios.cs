using FlexPos.Application.DTOs.Servicios;
using FlexPos.Application.Interfaces;
using FlexPos.Domain.Entities;
using FlexPos.Domain.Errors;
using FlexPos.Domain.ValueObjects;

namespace FlexPos.Application.Services;

public sealed class ServicioCatalogoServicios(
    IRepositorioServicios repositorio,
    IRepositorioConfiguracion configuracion)
{
    public Task<Resultado<PaginaServiciosDto>> ListarActivosAsync(
        string? buscar,
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken) =>
        ListarInternoAsync(buscar, true, pagina, tamanoPagina, cancellationToken);

    public Task<Resultado<PaginaServiciosDto>> ListarAdministracionAsync(
        string? buscar,
        bool? activo,
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken) =>
        ListarInternoAsync(buscar, activo, pagina, tamanoPagina, cancellationToken);

    public async Task<Resultado<ServicioDto>> ObtenerActivoAsync(Guid id, CancellationToken cancellationToken)
    {
        var servicio = id == Guid.Empty ? null : await repositorio.ObtenerAsync(id, cancellationToken);
        return servicio is null || !servicio.Activo
            ? Resultado<ServicioDto>.Fallo(NoEncontrado())
            : Resultado<ServicioDto>.Exito(Convertir(servicio));
    }

    public async Task<Resultado<ServicioDto>> ObtenerAdministracionAsync(Guid id, CancellationToken cancellationToken)
    {
        var servicio = id == Guid.Empty ? null : await repositorio.ObtenerAsync(id, cancellationToken);
        return servicio is null
            ? Resultado<ServicioDto>.Fallo(NoEncontrado())
            : Resultado<ServicioDto>.Exito(Convertir(servicio));
    }

    public async Task<Resultado<ServicioDto>> CrearAsync(
        SolicitudServicio solicitud,
        CancellationToken cancellationToken)
    {
        var dinero = await CrearPrecioAsync(solicitud.Precio, cancellationToken);
        if (!dinero.EsExitoso)
        {
            return Resultado<ServicioDto>.Fallo(dinero.Error!);
        }

        var nuevo = Servicio.Crear(
            solicitud.Nombre, solicitud.Descripcion, solicitud.Categoria,
            dinero.Valor, solicitud.DuracionMinutos);
        if (!nuevo.EsExitoso)
        {
            return Resultado<ServicioDto>.Fallo(nuevo.Error!);
        }

        repositorio.Agregar(nuevo.Valor!);
        return await GuardarYConvertirAsync(nuevo.Valor!, cancellationToken);
    }

    public async Task<Resultado<ServicioDto>> ActualizarAsync(
        Guid id,
        SolicitudServicio solicitud,
        CancellationToken cancellationToken)
    {
        var servicio = id == Guid.Empty ? null : await repositorio.ObtenerAsync(id, cancellationToken);
        if (servicio is null)
        {
            return Resultado<ServicioDto>.Fallo(NoEncontrado());
        }

        var dinero = await CrearPrecioAsync(solicitud.Precio, cancellationToken);
        if (!dinero.EsExitoso)
        {
            return Resultado<ServicioDto>.Fallo(dinero.Error!);
        }

        var actualizacion = servicio.Actualizar(
            solicitud.Nombre, solicitud.Descripcion, solicitud.Categoria,
            dinero.Valor, solicitud.DuracionMinutos);
        if (!actualizacion.EsExitoso)
        {
            return Resultado<ServicioDto>.Fallo(actualizacion.Error!);
        }

        return await GuardarYConvertirAsync(servicio, cancellationToken);
    }

    public async Task<Resultado<ServicioDto>> EstablecerEstadoAsync(
        Guid id,
        bool activo,
        CancellationToken cancellationToken)
    {
        var servicio = id == Guid.Empty ? null : await repositorio.ObtenerAsync(id, cancellationToken);
        if (servicio is null)
        {
            return Resultado<ServicioDto>.Fallo(NoEncontrado());
        }

        servicio.EstablecerEstado(activo);
        return await GuardarYConvertirAsync(servicio, cancellationToken);
    }

    private async Task<Resultado<PaginaServiciosDto>> ListarInternoAsync(
        string? buscar,
        bool? activo,
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken)
    {
        if (!ValidacionListado.EsValida(buscar, pagina, tamanoPagina))
        {
            return Resultado<PaginaServiciosDto>.Fallo(new ErrorDominio(
                "servicio.busqueda_invalida", "La paginación o el texto de búsqueda no son válidos."));
        }

        var resultado = await repositorio.ListarAsync(
            TextoNormalizado.Normalizar(buscar), activo,
            ValidacionListado.CalcularOmitir(pagina, tamanoPagina), tamanoPagina, cancellationToken);
        return Resultado<PaginaServiciosDto>.Exito(new PaginaServiciosDto(
            resultado.Elementos.Select(Convertir).ToArray(), pagina, tamanoPagina, resultado.Total));
    }

    private async Task<Resultado<Dinero>> CrearPrecioAsync(decimal importe, CancellationToken cancellationToken)
    {
        var principal = await configuracion.ObtenerPrincipalAsync(cancellationToken);
        if (principal is null)
        {
            return Resultado<Dinero>.Fallo(new ErrorDominio(
                "configuracion.no_configurada", "Primero debe registrar la configuración del establecimiento."));
        }

        return Dinero.Crear(importe, principal.CodigoMoneda);
    }

    private async Task<Resultado<ServicioDto>> GuardarYConvertirAsync(
        Servicio servicio,
        CancellationToken cancellationToken)
    {
        if (!await repositorio.GuardarCambiosAsync(cancellationToken))
        {
            return Resultado<ServicioDto>.Fallo(new ErrorDominio(
                "servicio.conflicto", "El servicio cambió en otra operación."));
        }

        return Resultado<ServicioDto>.Exito(Convertir(servicio));
    }

    private static ServicioDto Convertir(Servicio servicio) => new(
        servicio.Id, servicio.Nombre, servicio.Descripcion, servicio.Categoria,
        servicio.Precio.Importe, servicio.Precio.CodigoMoneda, servicio.DuracionMinutos,
        servicio.Activo, servicio.FechaCreacionUtc, servicio.FechaModificacionUtc);

    private static ErrorDominio NoEncontrado() => new(
        "servicio.no_encontrado", "No se encontró el servicio solicitado.");
}
