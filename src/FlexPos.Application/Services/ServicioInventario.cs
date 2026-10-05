using FlexPos.Application.DTOs.Inventario;
using FlexPos.Application.Interfaces;
using FlexPos.Domain.Entities;
using FlexPos.Domain.Enums;
using FlexPos.Domain.Errors;
using FlexPos.Domain.ValueObjects;

namespace FlexPos.Application.Services;

public sealed class ServicioInventario(
    IRepositorioInventario repositorio,
    IRepositorioConfiguracion configuracion)
{
    public async Task<Resultado<PaginaArticulosInventarioDto>> ListarArticulosAsync(
        string? buscar,
        TipoArticuloInventario? tipo,
        bool? activo,
        bool soloBajoMinimo,
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken)
    {
        if (!ValidacionListado.EsValida(buscar, pagina, tamanoPagina) ||
            (tipo.HasValue && !Enum.IsDefined(tipo.Value)))
        {
            return Resultado<PaginaArticulosInventarioDto>.Fallo(ErrorBusqueda());
        }

        var resultado = await repositorio.ListarArticulosAsync(
            TextoNormalizado.Normalizar(buscar), tipo, activo, soloBajoMinimo,
            ValidacionListado.CalcularOmitir(pagina, tamanoPagina), tamanoPagina, cancellationToken);
        return Resultado<PaginaArticulosInventarioDto>.Exito(new PaginaArticulosInventarioDto(
            resultado.Elementos.Select(Convertir).ToArray(), pagina, tamanoPagina, resultado.Total));
    }

    public async Task<Resultado<ArticuloInventarioDto>> ObtenerArticuloAsync(
        Guid id, CancellationToken cancellationToken)
    {
        var articulo = id == Guid.Empty ? null : await repositorio.ObtenerArticuloAsync(id, cancellationToken);
        return articulo is null
            ? Resultado<ArticuloInventarioDto>.Fallo(NoEncontradoArticulo())
            : Resultado<ArticuloInventarioDto>.Exito(Convertir(articulo));
    }

    public async Task<Resultado<ArticuloInventarioDto>> CrearArticuloAsync(
        SolicitudArticuloInventario solicitud,
        CancellationToken cancellationToken)
    {
        var principal = await configuracion.ObtenerPrincipalAsync(cancellationToken);
        if (principal is null)
        {
            return Resultado<ArticuloInventarioDto>.Fallo(ErrorConfiguracion());
        }

        var nuevo = ArticuloInventario.Crear(
            solicitud.Codigo, solicitud.Nombre, solicitud.Tipo, solicitud.UnidadBase,
            solicitud.ManejaFraccion, solicitud.Categoria, solicitud.CantidadMinima,
            principal.CodigoMoneda, solicitud.PrecioVenta);
        if (!nuevo.EsExitoso)
        {
            return Resultado<ArticuloInventarioDto>.Fallo(nuevo.Error!);
        }

        repositorio.AgregarArticulo(nuevo.Valor!);
        return await GuardarArticuloAsync(nuevo.Valor!, cancellationToken);
    }

    public async Task<Resultado<ArticuloInventarioDto>> ActualizarArticuloAsync(
        Guid id,
        SolicitudArticuloInventario solicitud,
        CancellationToken cancellationToken)
    {
        var articulo = id == Guid.Empty ? null : await repositorio.ObtenerArticuloAsync(id, cancellationToken);
        if (articulo is null)
        {
            return Resultado<ArticuloInventarioDto>.Fallo(NoEncontradoArticulo());
        }

        if (solicitud.Tipo != articulo.Tipo)
        {
            return Resultado<ArticuloInventarioDto>.Fallo(new ErrorDominio(
                "inventario.tipo_inmutable", "El tipo Producto/Insumo no se puede cambiar despues de crearlo."));
        }

        var actualizacion = articulo.Actualizar(
            solicitud.Codigo, solicitud.Nombre, solicitud.UnidadBase, solicitud.ManejaFraccion,
            solicitud.Categoria, solicitud.CantidadMinima, solicitud.PrecioVenta,
            await repositorio.TieneMovimientosAsync(id, cancellationToken));
        if (!actualizacion.EsExitoso)
        {
            return Resultado<ArticuloInventarioDto>.Fallo(actualizacion.Error!);
        }

        return await GuardarArticuloAsync(articulo, cancellationToken);
    }

    public async Task<Resultado<ArticuloInventarioDto>> EstablecerEstadoAsync(
        Guid id, bool activo, CancellationToken cancellationToken)
    {
        var articulo = id == Guid.Empty ? null : await repositorio.ObtenerArticuloAsync(id, cancellationToken);
        if (articulo is null)
        {
            return Resultado<ArticuloInventarioDto>.Fallo(NoEncontradoArticulo());
        }

        articulo.EstablecerEstado(activo);
        return await GuardarArticuloAsync(articulo, cancellationToken);
    }

    public async Task<Resultado<MovimientoInventarioDto>> RegistrarEntradaAsync(
        Guid articuloId,
        SolicitudEntradaInventario solicitud,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(solicitud.Motivo))
        {
            return Resultado<MovimientoInventarioDto>.Fallo(ErrorMotivo());
        }

        var articulo = articuloId == Guid.Empty
            ? null
            : await repositorio.ObtenerArticuloAsync(articuloId, cancellationToken);
        if (articulo is null || !articulo.Activo)
        {
            return Resultado<MovimientoInventarioDto>.Fallo(NoEncontradoArticulo());
        }

        var costo = Dinero.Crear(solicitud.CostoUnitario, articulo.CostoPromedio.CodigoMoneda);
        if (!costo.EsExitoso || costo.Valor!.Importe < 0m)
        {
            return Resultado<MovimientoInventarioDto>.Fallo(costo.Error ?? new ErrorDominio(
                "inventario.costo_invalido", "El costo debe ser no negativo."));
        }

        var entrada = articulo.AplicarEntrada(solicitud.Cantidad, costo.Valor);
        if (!entrada.EsExitoso)
        {
            return Resultado<MovimientoInventarioDto>.Fallo(entrada.Error!);
        }

        var movimiento = MovimientoInventario.Crear(
            articulo, TipoMovimientoInventario.EntradaAjuste, solicitud.Cantidad,
            articulo.ExistenciaActual, costo.Valor, solicitud.Motivo);
        if (!movimiento.EsExitoso)
        {
            return Resultado<MovimientoInventarioDto>.Fallo(movimiento.Error!);
        }

        repositorio.AgregarMovimiento(movimiento.Valor!);
        return await GuardarMovimientoAsync(movimiento.Valor!, cancellationToken);
    }

    public async Task<Resultado<MovimientoInventarioDto>> RegistrarSalidaAsync(
        Guid articuloId,
        SolicitudSalidaInventario solicitud,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(solicitud.Motivo))
        {
            return Resultado<MovimientoInventarioDto>.Fallo(ErrorMotivo());
        }

        var articulo = articuloId == Guid.Empty
            ? null
            : await repositorio.ObtenerArticuloAsync(articuloId, cancellationToken);
        if (articulo is null || !articulo.Activo)
        {
            return Resultado<MovimientoInventarioDto>.Fallo(NoEncontradoArticulo());
        }

        var salida = articulo.AplicarSalida(solicitud.Cantidad);
        if (!salida.EsExitoso)
        {
            return Resultado<MovimientoInventarioDto>.Fallo(salida.Error!);
        }

        var movimiento = MovimientoInventario.Crear(
            articulo, TipoMovimientoInventario.SalidaAjuste, solicitud.Cantidad,
            articulo.ExistenciaActual, articulo.CostoPromedio, solicitud.Motivo);
        if (!movimiento.EsExitoso)
        {
            return Resultado<MovimientoInventarioDto>.Fallo(movimiento.Error!);
        }

        repositorio.AgregarMovimiento(movimiento.Valor!);
        return await GuardarMovimientoAsync(movimiento.Valor!, cancellationToken);
    }

    public async Task<Resultado<PaginaMovimientosInventarioDto>> ListarMovimientosAsync(
        Guid? articuloId,
        DateTimeOffset? desdeUtc,
        DateTimeOffset? hastaUtc,
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken)
    {
        if (pagina < 1 || tamanoPagina is < 1 or > 100 ||
            (pagina - 1L) * tamanoPagina > int.MaxValue ||
            articuloId == Guid.Empty ||
            (desdeUtc.HasValue && hastaUtc.HasValue && desdeUtc.Value > hastaUtc.Value))
        {
            return Resultado<PaginaMovimientosInventarioDto>.Fallo(ErrorBusqueda());
        }

        var resultado = await repositorio.ListarMovimientosAsync(
            articuloId, desdeUtc?.ToUniversalTime(), hastaUtc?.ToUniversalTime(),
            ValidacionListado.CalcularOmitir(pagina, tamanoPagina), tamanoPagina, cancellationToken);
        return Resultado<PaginaMovimientosInventarioDto>.Exito(new PaginaMovimientosInventarioDto(
            resultado.Elementos.Select(Convertir).ToArray(), pagina, tamanoPagina, resultado.Total));
    }

    private async Task<Resultado<ArticuloInventarioDto>> GuardarArticuloAsync(
        ArticuloInventario articulo, CancellationToken cancellationToken)
    {
        if (!await repositorio.GuardarCambiosAsync(cancellationToken))
        {
            return Resultado<ArticuloInventarioDto>.Fallo(Conflicto());
        }

        return Resultado<ArticuloInventarioDto>.Exito(Convertir(articulo));
    }

    private async Task<Resultado<MovimientoInventarioDto>> GuardarMovimientoAsync(
        MovimientoInventario movimiento, CancellationToken cancellationToken)
    {
        if (!await repositorio.GuardarCambiosAsync(cancellationToken))
        {
            return Resultado<MovimientoInventarioDto>.Fallo(Conflicto());
        }

        return Resultado<MovimientoInventarioDto>.Exito(Convertir(movimiento));
    }

    internal static ArticuloInventarioDto Convertir(ArticuloInventario articulo) => new(
        articulo.Id, articulo.Codigo, articulo.Nombre, articulo.Tipo.ToString(), articulo.UnidadBase,
        articulo.ManejaFraccion, articulo.Categoria, articulo.ExistenciaActual, articulo.CantidadMinima,
        articulo.CostoPromedio.Importe, articulo.CostoPromedio.CodigoMoneda, articulo.PrecioVenta?.Importe,
        articulo.Activo, articulo.CantidadMinima > 0m && articulo.ExistenciaActual <= articulo.CantidadMinima,
        articulo.FechaCreacionUtc, articulo.FechaModificacionUtc);

    internal static MovimientoInventarioDto Convertir(MovimientoInventario movimiento) => new(
        movimiento.Id, movimiento.ArticuloId, movimiento.Articulo.Nombre, movimiento.Tipo.ToString(),
        movimiento.Cantidad, movimiento.ExistenciaResultante, movimiento.CostoUnitario.Importe,
        movimiento.CostoUnitario.CodigoMoneda, movimiento.Motivo, movimiento.CompraId, movimiento.VentaId,
        movimiento.FechaCreacionUtc);

    private static ErrorDominio ErrorBusqueda() => new(
        "inventario.busqueda_invalida", "La paginacion, el filtro o el rango de fechas no son validos.");

    private static ErrorDominio ErrorMotivo() => new(
        "inventario.motivo_requerido", "Indique el motivo del movimiento de ajuste.");

    private static ErrorDominio ErrorConfiguracion() => new(
        "configuracion.no_configurada", "Primero debe registrar la configuracion del establecimiento.");

    private static ErrorDominio NoEncontradoArticulo() => new(
        "inventario.articulo_no_encontrado", "No se encontro el articulo solicitado o esta inactivo.");

    private static ErrorDominio Conflicto() => new(
        "inventario.conflicto", "El articulo cambio en otra operacion. Vuelva a consultar e intente de nuevo.");
}
