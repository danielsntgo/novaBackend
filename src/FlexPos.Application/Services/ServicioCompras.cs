using FlexPos.Application.DTOs.Compras;
using FlexPos.Application.DTOs.Inventario;
using FlexPos.Application.Interfaces;
using FlexPos.Domain.Entities;
using FlexPos.Domain.Enums;
using FlexPos.Domain.Errors;
using FlexPos.Domain.ValueObjects;

namespace FlexPos.Application.Services;

public sealed class ServicioCompras(
    IRepositorioCompras compras,
    IRepositorioInventario inventario,
    IRepositorioProveedores proveedores,
    IRepositorioConfiguracion configuracion,
    TimeProvider reloj)
{
    public async Task<Resultado<PaginaComprasDto>> ListarAsync(
        string? buscar,
        EstadoCompra? estado,
        DateTimeOffset? desdeUtc,
        DateTimeOffset? hastaUtc,
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken)
    {
        if (!ValidacionListado.EsValida(buscar, pagina, tamanoPagina) ||
            (estado.HasValue && !Enum.IsDefined(estado.Value)) ||
            (desdeUtc.HasValue && hastaUtc.HasValue && desdeUtc.Value > hastaUtc.Value))
        {
            return Resultado<PaginaComprasDto>.Fallo(ErrorBusqueda());
        }

        var consulta = await compras.ListarAsync(
            TextoNormalizado.Normalizar(buscar), estado, desdeUtc?.ToUniversalTime(), hastaUtc?.ToUniversalTime(),
            ValidacionListado.CalcularOmitir(pagina, tamanoPagina), tamanoPagina, cancellationToken);
        return Resultado<PaginaComprasDto>.Exito(new PaginaComprasDto(
            consulta.Elementos.Select(Convertir).ToArray(), pagina, tamanoPagina, consulta.Total));
    }

    public async Task<Resultado<CompraDto>> ObtenerAsync(Guid id, CancellationToken cancellationToken)
    {
        var compra = id == Guid.Empty ? null : await compras.ObtenerAsync(id, cancellationToken);
        return compra is null
            ? Resultado<CompraDto>.Fallo(NoEncontrada())
            : Resultado<CompraDto>.Exito(Convertir(compra));
    }

    public async Task<Resultado<CompraDto>> CrearAsync(
        SolicitudCompra solicitud,
        CancellationToken cancellationToken)
    {
        var moneda = await ObtenerMonedaAsync(cancellationToken);
        if (!moneda.EsExitoso)
        {
            return Resultado<CompraDto>.Fallo(moneda.Error!);
        }

        var proveedor = await ObtenerProveedorActivoAsync(solicitud.ProveedorId, cancellationToken);
        if (!proveedor.EsExitoso)
        {
            return Resultado<CompraDto>.Fallo(proveedor.Error!);
        }

        var detalles = await CrearDetallesAsync(solicitud.Detalles, moneda.Valor!, cancellationToken);
        if (!detalles.EsExitoso)
        {
            return Resultado<CompraDto>.Fallo(detalles.Error!);
        }

        var nueva = Compra.Crear(
            solicitud.ProveedorId, solicitud.FechaCompraUtc, solicitud.Referencia,
            solicitud.Observacion, moneda.Valor!);
        if (!nueva.EsExitoso)
        {
            return Resultado<CompraDto>.Fallo(nueva.Error!);
        }

        foreach (var detalle in detalles.Valor!)
        {
            var agregado = nueva.Valor!.AgregarDetalle(detalle);
            if (!agregado.EsExitoso)
            {
                return Resultado<CompraDto>.Fallo(agregado.Error!);
            }
        }

        compras.Agregar(nueva.Valor!);
        return await GuardarAsync(nueva.Valor!, cancellationToken);
    }

    public async Task<Resultado<CompraDto>> ActualizarBorradorAsync(
        Guid id,
        SolicitudCompra solicitud,
        CancellationToken cancellationToken)
    {
        var compra = id == Guid.Empty ? null : await compras.ObtenerAsync(id, cancellationToken);
        if (compra is null)
        {
            return Resultado<CompraDto>.Fallo(NoEncontrada());
        }

        if (compra.Estado != EstadoCompra.Borrador)
        {
            return Resultado<CompraDto>.Fallo(NoEditable());
        }

        var moneda = await ObtenerMonedaAsync(cancellationToken);
        if (!moneda.EsExitoso)
        {
            return Resultado<CompraDto>.Fallo(moneda.Error!);
        }

        if (moneda.Valor != compra.CodigoMoneda)
        {
            return Resultado<CompraDto>.Fallo(ErrorMoneda());
        }

        var proveedor = await ObtenerProveedorActivoAsync(solicitud.ProveedorId, cancellationToken);
        if (!proveedor.EsExitoso)
        {
            return Resultado<CompraDto>.Fallo(proveedor.Error!);
        }

        var detalles = await CrearDetallesAsync(solicitud.Detalles, moneda.Valor!, cancellationToken);
        if (!detalles.EsExitoso)
        {
            return Resultado<CompraDto>.Fallo(detalles.Error!);
        }

        var actualizacion = compra.ActualizarBorrador(
            solicitud.ProveedorId, solicitud.FechaCompraUtc, solicitud.Referencia,
            solicitud.Observacion, detalles.Valor!);
        return actualizacion.EsExitoso
            ? await GuardarAsync(compra, cancellationToken)
            : Resultado<CompraDto>.Fallo(actualizacion.Error!);
    }

    public async Task<Resultado<CompraDto>> ConfirmarAsync(Guid id, CancellationToken cancellationToken)
    {
        var compra = id == Guid.Empty ? null : await compras.ObtenerAsync(id, cancellationToken);
        if (compra is null)
        {
            return Resultado<CompraDto>.Fallo(NoEncontrada());
        }

        if (compra.Estado != EstadoCompra.Borrador)
        {
            return Resultado<CompraDto>.Fallo(NoEditable());
        }

        var moneda = await ObtenerMonedaAsync(cancellationToken);
        if (!moneda.EsExitoso)
        {
            return Resultado<CompraDto>.Fallo(moneda.Error!);
        }

        if (moneda.Valor != compra.CodigoMoneda)
        {
            return Resultado<CompraDto>.Fallo(ErrorMoneda());
        }

        var detalles = compra.Detalles.Where(x => x.Vigente).ToArray();
        var ids = detalles.Select(x => x.ArticuloId).Distinct().ToArray();
        var articulos = await inventario.ObtenerArticulosAsync(ids, cancellationToken);
        var porId = articulos.ToDictionary(x => x.Id);
        if (porId.Count != ids.Length)
        {
            return Resultado<CompraDto>.Fallo(ErrorArticuloCompra());
        }

        foreach (var detalle in detalles)
        {
            if (!porId.TryGetValue(detalle.ArticuloId, out var articulo) ||
                !articulo.Activo || articulo.CostoPromedio.CodigoMoneda != compra.CodigoMoneda ||
                !articulo.EsCantidadValida(detalle.Cantidad))
            {
                return Resultado<CompraDto>.Fallo(ErrorArticuloCompra());
            }

            var entrada = articulo.AplicarEntrada(detalle.Cantidad, detalle.CostoUnitario);
            if (!entrada.EsExitoso)
            {
                return Resultado<CompraDto>.Fallo(entrada.Error!);
            }

            var movimiento = MovimientoInventario.Crear(
                articulo, TipoMovimientoInventario.EntradaCompra, detalle.Cantidad,
                articulo.ExistenciaActual, detalle.CostoUnitario,
                string.IsNullOrWhiteSpace(compra.Referencia) ? $"Compra {compra.Id}" : compra.Referencia,
                compra.Id, detalle.Id);
            if (!movimiento.EsExitoso)
            {
                return Resultado<CompraDto>.Fallo(movimiento.Error!);
            }

            inventario.AgregarMovimiento(movimiento.Valor!);
        }

        var confirmacion = compra.Confirmar(reloj.GetUtcNow());
        if (!confirmacion.EsExitoso)
        {
            return Resultado<CompraDto>.Fallo(confirmacion.Error!);
        }

        return await GuardarAsync(compra, cancellationToken);
    }

    private async Task<Resultado<IReadOnlyList<DetalleCompra>>> CrearDetallesAsync(
        IReadOnlyList<SolicitudLineaCompra>? solicitudes,
        string codigoMoneda,
        CancellationToken cancellationToken)
    {
        if (solicitudes is null || solicitudes.Count is < 1 or > 100)
        {
            return Resultado<IReadOnlyList<DetalleCompra>>.Fallo(new ErrorDominio(
                "compra.detalles_invalidos", "La compra debe tener entre una y cien lineas."));
        }

        var ids = solicitudes.Select(x => x.ArticuloId).Distinct().ToArray();
        var articulos = await inventario.ObtenerArticulosAsync(ids, cancellationToken);
        var porId = articulos.ToDictionary(x => x.Id);
        if (porId.Count != ids.Length)
        {
            return Resultado<IReadOnlyList<DetalleCompra>>.Fallo(ErrorArticuloCompra());
        }

        var detalles = new List<DetalleCompra>(solicitudes.Count);
        foreach (var solicitud in solicitudes)
        {
            if (!porId.TryGetValue(solicitud.ArticuloId, out var articulo) ||
                !articulo.Activo || articulo.CostoPromedio.CodigoMoneda != codigoMoneda)
            {
                return Resultado<IReadOnlyList<DetalleCompra>>.Fallo(ErrorArticuloCompra());
            }

            var costo = Dinero.Crear(solicitud.CostoUnitario, codigoMoneda);
            if (!costo.EsExitoso || costo.Valor!.Importe < 0m)
            {
                return Resultado<IReadOnlyList<DetalleCompra>>.Fallo(costo.Error ?? new ErrorDominio(
                    "compra.costo_invalido", "El costo unitario debe ser no negativo."));
            }

            var detalle = DetalleCompra.Crear(articulo, solicitud.Cantidad, costo.Valor);
            if (!detalle.EsExitoso)
            {
                return Resultado<IReadOnlyList<DetalleCompra>>.Fallo(detalle.Error!);
            }

            detalles.Add(detalle.Valor!);
        }

        return Resultado<IReadOnlyList<DetalleCompra>>.Exito(detalles);
    }

    private async Task<Resultado<string>> ObtenerMonedaAsync(CancellationToken cancellationToken)
    {
        var principal = await configuracion.ObtenerPrincipalAsync(cancellationToken);
        return principal is null
            ? Resultado<string>.Fallo(new ErrorDominio(
                "configuracion.no_configurada", "Primero debe registrar la configuracion del establecimiento."))
            : Resultado<string>.Exito(principal.CodigoMoneda);
    }

    private async Task<Resultado<Proveedor>> ObtenerProveedorActivoAsync(
        Guid id, CancellationToken cancellationToken)
    {
        var proveedor = id == Guid.Empty ? null : await proveedores.ObtenerAsync(id, cancellationToken);
        return proveedor is { Activo: true }
            ? Resultado<Proveedor>.Exito(proveedor)
            : Resultado<Proveedor>.Fallo(new ErrorDominio(
                "compra.proveedor_invalido", "Seleccione un proveedor existente y activo."));
    }

    private async Task<Resultado<CompraDto>> GuardarAsync(Compra compra, CancellationToken cancellationToken)
    {
        if (!await compras.GuardarCambiosAsync(cancellationToken))
        {
            return Resultado<CompraDto>.Fallo(new ErrorDominio(
                "compra.conflicto", "La compra o el inventario cambio en otra operacion; vuelva a consultar."));
        }

        return Resultado<CompraDto>.Exito(Convertir(compra));
    }

    private static CompraDto Convertir(Compra compra)
    {
        var lineas = compra.Detalles.Where(x => x.Vigente).Select(x => new DetalleCompraDto(
            x.Id, x.ArticuloId, x.NombreArticulo, x.UnidadBase, x.Cantidad,
            x.CostoUnitario.Importe, x.TotalLinea.Importe, x.CostoUnitario.CodigoMoneda)).ToArray();
        return new CompraDto(
            compra.Id, compra.ProveedorId, compra.Proveedor.Nombre, compra.FechaCompraUtc,
            compra.Referencia, compra.Observacion, compra.CodigoMoneda, compra.Estado.ToString(),
            compra.FechaConfirmacionUtc, compra.TotalImporte, lineas,
            compra.FechaCreacionUtc, compra.FechaModificacionUtc);
    }

    private static ErrorDominio ErrorBusqueda() => new(
        "compra.busqueda_invalida", "La paginacion, estado o rango de fechas no son validos.");

    private static ErrorDominio NoEncontrada() => new(
        "compra.no_encontrada", "No se encontro la compra solicitada.");

    private static ErrorDominio NoEditable() => new(
        "compra.no_editable", "La compra confirmada no se puede editar ni confirmar de nuevo.");

    private static ErrorDominio ErrorMoneda() => new(
        "compra.moneda_conflicto", "La moneda de la compra ya no coincide con la configuracion del negocio.");

    private static ErrorDominio ErrorArticuloCompra() => new(
        "compra.articulo_invalido", "Una linea contiene un articulo inexistente, inactivo o incompatible.");
}
