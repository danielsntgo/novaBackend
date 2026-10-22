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
    TimeProvider reloj,
    IRepositorioPuntoVenta puntoVenta,
    IUsuarioActual usuarioActual)
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

    public Task<Resultado<CompraDto>> DevolverAsync(
        Guid compraId,
        SolicitudDevolucionCompra solicitud,
        CancellationToken cancellationToken) =>
        ProcesarDevolucionAsync(compraId, solicitud.Motivo, solicitud.Lineas, solicitud.Pagos,
            anular: false, cancellationToken);

    public Task<Resultado<CompraDto>> AnularAsync(
        Guid compraId,
        SolicitudAnularCompra solicitud,
        CancellationToken cancellationToken) =>
        ProcesarDevolucionAsync(compraId, solicitud.Motivo, null, solicitud.Pagos,
            anular: true, cancellationToken);

    private async Task<Resultado<CompraDto>> ProcesarDevolucionAsync(
        Guid compraId,
        string? motivo,
        IReadOnlyList<SolicitudLineaDevolucionCompra>? lineasSolicitadas,
        IReadOnlyList<SolicitudPagoCompra>? pagosSolicitados,
        bool anular,
        CancellationToken cancellationToken)
    {
        var usuarioId = usuarioActual.ObtenerId();
        if (!usuarioId.HasValue || string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length > 250 ||
            !anular && (lineasSolicitadas is null || lineasSolicitadas.Count is < 1 or > 200) ||
            pagosSolicitados is { Count: > 10 })
        {
            return Resultado<CompraDto>.Fallo(ErrorDevolucion());
        }

        await using var unidad = await puntoVenta.IniciarTransaccionAsync(cancellationToken);
        var caja = await unidad.BloquearCajaActualAsync(cancellationToken);
        if (caja is null)
        {
            return Resultado<CompraDto>.Fallo(new ErrorDominio(
                "caja.no_abierta", "Debe haber una caja abierta para registrar el reintegro de una compra."));
        }

        var compra = await unidad.BloquearCompraAsync(compraId, cancellationToken);
        if (compra is null)
        {
            return Resultado<CompraDto>.Fallo(NoEncontrada());
        }

        if (compra.Estado is EstadoCompra.Borrador or EstadoCompra.Devuelta or EstadoCompra.Anulada)
        {
            return Resultado<CompraDto>.Fallo(NoEditable());
        }

        var configuracion = await unidad.ObtenerConfiguracionPrincipalAsync(cancellationToken);
        if (configuracion is null || configuracion.CodigoMoneda != compra.CodigoMoneda)
        {
            return Resultado<CompraDto>.Fallo(ErrorMoneda());
        }

        var cantidadesDevueltas = compra.Devoluciones.SelectMany(x => x.Detalles)
            .GroupBy(x => x.DetalleCompraId).ToDictionary(x => x.Key, x => x.Sum(detalle => detalle.Cantidad));
        var importesDevueltos = compra.Devoluciones.SelectMany(x => x.Detalles)
            .GroupBy(x => x.DetalleCompraId).ToDictionary(x => x.Key, x => x.Sum(detalle => detalle.Importe));
        var solicitudes = anular
            ? compra.Detalles.Select(linea => new SolicitudLineaDevolucionCompra(
                    linea.Id, linea.Cantidad - cantidadesDevueltas.GetValueOrDefault(linea.Id)))
                .Where(x => x.Cantidad > 0m).ToArray()
            : lineasSolicitadas!.ToArray();
        if (solicitudes.Length == 0 || solicitudes.Select(x => x.DetalleCompraId).Distinct().Count() != solicitudes.Length)
        {
            return Resultado<CompraDto>.Fallo(new ErrorDominio(
                "compra.devolucion_invalida", "No hay unidades pendientes de devolución o hay líneas repetidas."));
        }

        var lineasCompra = compra.Detalles.ToDictionary(x => x.Id);
        var articulos = await unidad.ObtenerArticulosAsync(
            lineasCompra.Values.Where(x => solicitudes.Any(s => s.DetalleCompraId == x.Id))
                .Select(x => x.ArticuloId).Distinct().ToArray(),
            bloquear: true, cancellationToken);
        var articulosPorId = articulos.ToDictionary(x => x.Id);
        var cantidadesPorLinea = new Dictionary<Guid, decimal>();
        var detalles = new List<DetalleDevolucionCompra>(solicitudes.Length);
        foreach (var solicitud in solicitudes)
        {
            if (!lineasCompra.TryGetValue(solicitud.DetalleCompraId, out var linea) ||
                solicitud.Cantidad <= 0m || decimal.Round(solicitud.Cantidad, 3) != solicitud.Cantidad)
            {
                return Resultado<CompraDto>.Fallo(ErrorDevolucion());
            }

            var cantidadRestante = linea.Cantidad - cantidadesDevueltas.GetValueOrDefault(linea.Id);
            if (solicitud.Cantidad > cantidadRestante)
            {
                return Resultado<CompraDto>.Fallo(new ErrorDominio(
                    "compra.cantidad_devolucion_invalida", "La cantidad supera las unidades que quedan por devolver."));
            }

            if (!articulosPorId.TryGetValue(linea.ArticuloId, out var articulo) ||
                !articulo.EsCantidadValida(solicitud.Cantidad))
            {
                return Resultado<CompraDto>.Fallo(ErrorDevolucion());
            }

            var cantidadYaDevuelta = cantidadesDevueltas.GetValueOrDefault(linea.Id);
            var importeYaDevuelto = importesDevueltos.GetValueOrDefault(linea.Id);
            var importeRestante = linea.TotalLinea.Importe - importeYaDevuelto;
            var importe = solicitud.Cantidad == cantidadRestante
                ? importeRestante
                : Math.Min(importeRestante, Redondear(linea.TotalLinea.Importe * solicitud.Cantidad / linea.Cantidad));
            var detalle = DetalleDevolucionCompra.Crear(linea.Id, solicitud.Cantidad, importe);
            if (!detalle.EsExitoso)
            {
                return Resultado<CompraDto>.Fallo(detalle.Error!);
            }

            detalles.Add(detalle.Valor!);
            cantidadesPorLinea.Add(linea.Id, solicitud.Cantidad);
        }

        var importeLineas = detalles.Sum(x => x.Importe);
        var metodos = await unidad.ObtenerMetodosPagoAsync(cancellationToken);
        var pagos = ConstruirPagosDevolucion(pagosSolicitados ?? [], metodos, configuracion.Id);
        if (!pagos.EsExitoso)
        {
            return Resultado<CompraDto>.Fallo(pagos.Error!);
        }

        if (pagos.Valor!.Sum(x => x.Importe) != importeLineas)
        {
            return Resultado<CompraDto>.Fallo(new ErrorDominio(
                "compra.importe_reintegro_invalido",
                $"El importe del reintegro debe ser exactamente {importeLineas:0.00}."));
        }

        var ahora = reloj.GetUtcNow();
        var devolucion = DevolucionCompra.Crear(compra.Id, ahora, motivo, detalles, pagos.Valor);
        if (!devolucion.EsExitoso)
        {
            return Resultado<CompraDto>.Fallo(devolucion.Error!);
        }

        var registro = compra.RegistrarDevolucion(devolucion.Valor!, anular, usuarioId.Value, ahora);
        if (!registro.EsExitoso)
        {
            return Resultado<CompraDto>.Fallo(registro.Error!);
        }

        foreach (var (lineaId, cantidad) in cantidadesPorLinea)
        {
            var linea = lineasCompra[lineaId];
            if (!articulosPorId.TryGetValue(linea.ArticuloId, out var articulo))
            {
                return Resultado<CompraDto>.Fallo(ErrorArticuloCompra());
            }

            var salida = articulo.AplicarSalida(cantidad);
            if (!salida.EsExitoso)
            {
                return Resultado<CompraDto>.Fallo(salida.Error!);
            }

            var movimiento = MovimientoInventario.Crear(
                articulo, TipoMovimientoInventario.SalidaDevolucionCompra, cantidad,
                articulo.ExistenciaActual, linea.CostoUnitario,
                motivo, compraId: compra.Id, detalleCompraId: linea.Id);
            if (!movimiento.EsExitoso)
            {
                return Resultado<CompraDto>.Fallo(movimiento.Error!);
            }

            unidad.Agregar(movimiento.Valor!);
        }

        foreach (var pago in devolucion.Valor!.Pagos.OrderBy(x => x.CuentaFinancieraId))
        {
            var cuenta = await unidad.BloquearCuentaFinancieraAsync(pago.CuentaFinancieraId, cancellationToken);
            if (cuenta is null || !cuenta.Activa ||
                cuenta.ConfiguracionNegocioId != configuracion.Id || cuenta.CodigoMoneda != configuracion.CodigoMoneda ||
                cuenta.Id != pago.CuentaFinancieraId || !metodos.Any(x => x.Id == pago.MetodoPagoId &&
                    x.Activo && x.CuentaFinancieraId == cuenta.Id && x.EsCompatibleConCuenta(cuenta)) ||
                pago.EsEfectivo && (caja.Estado != EstadoCaja.Abierta || caja.CuentaFinancieraId != cuenta.Id))
            {
                return Resultado<CompraDto>.Fallo(new ErrorDominio(
                    "compra.cuenta_financiera_invalida", "La cuenta de reintegro no coincide con el método de pago configurado."));
            }

            var movimiento = MovimientoFinanciero.Crear(
                cuenta.Id, TipoMovimientoFinanciero.ReembolsoCompra,
                SentidoMovimientoFinanciero.Entrada, pago.Importe, configuracion.CodigoMoneda,
                ahora, $"Reintegro de compra {compra.Referencia ?? compra.Id.ToString()}: {motivo}",
                pago.EsEfectivo ? caja.Id : null, pago.MetodoPagoId, compraId: compra.Id);
            if (!movimiento.EsExitoso)
            {
                return Resultado<CompraDto>.Fallo(movimiento.Error!);
            }

            unidad.Agregar(movimiento.Valor!);
        }

        unidad.Agregar(devolucion.Valor);
        if (!await unidad.GuardarCambiosAsync(cancellationToken))
        {
            return Resultado<CompraDto>.Fallo(new ErrorDominio(
                "compra.conflicto", "La compra o el inventario cambió en otra operación; vuelve a consultar."));
        }

        await unidad.ConfirmarAsync(cancellationToken);
        return Resultado<CompraDto>.Exito(Convertir(compra));
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
        var devoluciones = compra.Devoluciones.OrderByDescending(x => x.FechaUtc).Select(x => new DevolucionCompraDto(
            x.Id, x.FechaUtc, x.Motivo, x.ImporteLineas, x.ImporteReintegrado,
            x.Detalles.Select(d => new DetalleDevolucionCompraDto(d.DetalleCompraId, d.Cantidad, d.Importe)).ToArray(),
            x.Pagos.Select(p => new PagoDevolucionCompraDto(
                p.MetodoPagoId, p.MetodoPagoNombre, p.EsEfectivo, p.CuentaFinancieraId,
                p.CuentaFinancieraNombre, p.Importe, p.Referencia)).ToArray())).ToArray();
        return new CompraDto(
            compra.Id, compra.ProveedorId, compra.Proveedor.Nombre, compra.FechaCompraUtc,
            compra.Referencia, compra.Observacion, compra.CodigoMoneda, compra.Estado.ToString(),
            compra.FechaConfirmacionUtc, compra.TotalImporte, compra.TotalDevuelto, compra.SaldoNeto,
            compra.MotivoAnulacion, compra.FechaAnulacionUtc, lineas, devoluciones,
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

    private static Resultado<IReadOnlyList<PagoDevolucionCompra>> ConstruirPagosDevolucion(
        IReadOnlyList<SolicitudPagoCompra> solicitudes,
        IReadOnlyList<MetodoPagoConfigurado> metodos,
        Guid configuracionId)
    {
        var metodosPorId = metodos.Where(x => x.ConfiguracionNegocioId == configuracionId && x.Activo)
            .ToDictionary(x => x.Id);
        var pagos = new List<PagoDevolucionCompra>(solicitudes.Count);
        foreach (var solicitud in solicitudes)
        {
            if (!metodosPorId.TryGetValue(solicitud.MetodoPagoId, out var metodo))
            {
                return Resultado<IReadOnlyList<PagoDevolucionCompra>>.Fallo(new ErrorDominio(
                    "compra.metodo_pago_inactivo", "El método para recibir el reintegro no existe o no está activo."));
            }

            var pago = PagoDevolucionCompra.Crear(metodo, metodo.CuentaFinanciera,
                solicitud.Importe, solicitud.Referencia);
            if (!pago.EsExitoso)
            {
                return Resultado<IReadOnlyList<PagoDevolucionCompra>>.Fallo(pago.Error!);
            }

            pagos.Add(pago.Valor!);
        }

        return Resultado<IReadOnlyList<PagoDevolucionCompra>>.Exito(pagos);
    }

    private static decimal Redondear(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);

    private static ErrorDominio ErrorDevolucion() => new(
        "compra.devolucion_invalida", "Indica un motivo, líneas y métodos de reintegro válidos.");
}
