using FlexPos.Application.DTOs.Ventas;
using FlexPos.Application.Interfaces;
using FlexPos.Domain.Entities;
using FlexPos.Domain.Enums;
using FlexPos.Domain.Errors;
using FlexPos.Domain.ValueObjects;

namespace FlexPos.Application.Services;

public sealed class ServicioVentas(
    IRepositorioPuntoVenta repositorio,
    IUsuarioActual usuarioActual,
    IGeneradorDocumentoVenta generadorDocumento,
    TimeProvider reloj)
{
    public async Task<Resultado<PaginaVentasDto>> ListarAsync(
        DateTimeOffset? desdeUtc,
        DateTimeOffset? hastaUtc,
        Guid? clienteId,
        string? estadoTexto,
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken)
    {
        if (!ValidarPaginacion(pagina, tamanoPagina) ||
            desdeUtc.HasValue && hastaUtc.HasValue && desdeUtc > hastaUtc ||
            clienteId == Guid.Empty)
        {
            return Resultado<PaginaVentasDto>.Fallo(ErrorBusqueda());
        }

        var estado = ParsearEstado<EstadoVenta>(estadoTexto);
        if (estadoTexto is not null && estado is null)
        {
            return Resultado<PaginaVentasDto>.Fallo(ErrorBusqueda());
        }

        var (elementos, total) = await repositorio.ListarVentasAsync(
            desdeUtc?.ToUniversalTime(), hastaUtc?.ToUniversalTime(), clienteId, estado,
            ValidacionListado.CalcularOmitir(pagina, tamanoPagina), tamanoPagina, cancellationToken);
        return Resultado<PaginaVentasDto>.Exito(new PaginaVentasDto(
            elementos.Select(Convertir).ToArray(), pagina, tamanoPagina, total));
    }

    public async Task<Resultado<VentaDto>> ObtenerAsync(Guid id, CancellationToken cancellationToken)
    {
        var venta = id == Guid.Empty ? null : await repositorio.ObtenerVentaAsync(id, cancellationToken);
        return venta is null
            ? Resultado<VentaDto>.Fallo(NoEncontrada())
            : Resultado<VentaDto>.Exito(Convertir(venta));
    }

    public async Task<Resultado<DocumentoVentaArchivoDto>> ObtenerDocumentoAsync(
        Guid ventaId,
        TipoDocumentoVenta tipo,
        CancellationToken cancellationToken)
    {
        var venta = ventaId == Guid.Empty ? null : await repositorio.ObtenerVentaAsync(ventaId, cancellationToken);
        if (venta is null)
        {
            return Resultado<DocumentoVentaArchivoDto>.Fallo(NoEncontrada());
        }

        var documento = venta.Documentos.SingleOrDefault(x => x.TipoDocumento == tipo);
        if (documento is null)
        {
            return Resultado<DocumentoVentaArchivoDto>.Fallo(new ErrorDominio(
                "venta.documento_no_encontrado", "La venta no tiene registrado ese documento."));
        }

        var nombre = tipo == TipoDocumentoVenta.Factura ? "factura" : "comprobante";
        return Resultado<DocumentoVentaArchivoDto>.Exito(new DocumentoVentaArchivoDto(
            $"{nombre}-{LimpiarNombreArchivo(documento.NumeroCompleto)}.pdf",
            "application/pdf",
            generadorDocumento.Generar(venta, documento)));
    }

    public async Task<Resultado<VentaDto>> CrearAsync(
        SolicitudCrearVenta solicitud,
        CancellationToken cancellationToken)
    {
        var usuarioId = usuarioActual.ObtenerId();
        if (!usuarioId.HasValue)
        {
            return Resultado<VentaDto>.Fallo(ErrorUsuario());
        }

        if (solicitud.Lineas is null || solicitud.Lineas.Count is < 1 or > 200 ||
            solicitud.Pagos is { Count: > 10 } ||
            solicitud.ClienteId == Guid.Empty ||
            !TryDescuento(solicitud.DescuentoGeneral, out var tipoDescuentoGeneral, out var valorDescuentoGeneral))
        {
            return Resultado<VentaDto>.Fallo(ErrorSolicitud());
        }

        await using var unidad = await repositorio.IniciarTransaccionAsync(cancellationToken);
        var configuracion = await unidad.ObtenerConfiguracionPrincipalAsync(cancellationToken);
        if (configuracion is null)
        {
            return Resultado<VentaDto>.Fallo(ErrorConfiguracion());
        }

        var caja = await unidad.BloquearCajaActualAsync(cancellationToken);
        if (caja is null)
        {
            return Resultado<VentaDto>.Fallo(new ErrorDominio(
                "caja.no_abierta", "Debe haber una caja abierta para registrar una venta."));
        }

        Cliente? cliente = null;
        if (solicitud.ClienteId.HasValue)
        {
            cliente = await unidad.ObtenerClienteAsync(solicitud.ClienteId.Value, cancellationToken);
            if (cliente is null || !cliente.Activo)
            {
                return Resultado<VentaDto>.Fallo(new ErrorDominio(
                    "venta.cliente_inactivo", "El cliente no existe o está inactivo."));
            }
        }

        var referencias = ValidarReferencias(solicitud.Lineas);
        if (referencias is null)
        {
            return Resultado<VentaDto>.Fallo(ErrorSolicitud());
        }

        var productos = await unidad.ObtenerArticulosAsync(referencias.Value.Productos, bloquear: true, cancellationToken);
        var servicios = await unidad.ObtenerServiciosAsync(referencias.Value.Servicios, cancellationToken);
        var empleados = await unidad.ObtenerEmpleadosAsync(referencias.Value.Empleados, cancellationToken);
        var impuestos = await unidad.ObtenerImpuestosAsync(cancellationToken);
        var metodos = await unidad.ObtenerMetodosPagoAsync(cancellationToken);
        var productoPorId = productos.ToDictionary(x => x.Id);
        var servicioPorId = servicios.ToDictionary(x => x.Id);
        var empleadoPorId = empleados.ToDictionary(x => x.Id);
        var impuestoPorId = impuestos
            .Where(x => x.ConfiguracionNegocioId == configuracion.Id)
            .ToDictionary(x => x.Id);
        var lineas = new List<DetalleVenta>(solicitud.Lineas.Count);

        foreach (var lineaSolicitud in solicitud.Lineas)
        {
            if (!Enum.TryParse<TipoLineaVenta>(lineaSolicitud.Tipo, true, out var tipoLinea) ||
                !Enum.IsDefined(tipoLinea) ||
                !TryDescuento(lineaSolicitud.Descuento, out var tipoDescuento, out var valorDescuento))
            {
                return Resultado<VentaDto>.Fallo(ErrorSolicitud());
            }

            var impuestoNombre = default(string?);
            decimal? impuestoPorcentaje = null;
            if (lineaSolicitud.ImpuestoId.HasValue)
            {
                if (!impuestoPorId.TryGetValue(lineaSolicitud.ImpuestoId.Value, out var impuesto) || !impuesto.Activo)
                {
                    return Resultado<VentaDto>.Fallo(new ErrorDominio(
                        "venta.impuesto_inactivo", "El impuesto no existe o está inactivo."));
                }

                impuestoNombre = impuesto.Nombre;
                impuestoPorcentaje = impuesto.Porcentaje;
            }

            DetalleVenta? detalle;
            if (tipoLinea == TipoLineaVenta.Producto)
            {
                if (!lineaSolicitud.ArticuloInventarioId.HasValue ||
                    !productoPorId.TryGetValue(lineaSolicitud.ArticuloInventarioId.Value, out var producto) ||
                    producto.Tipo != TipoArticuloInventario.Producto || !producto.Activo ||
                    producto.PrecioVenta is null ||
                    producto.PrecioVenta.CodigoMoneda != configuracion.CodigoMoneda)
                {
                    return Resultado<VentaDto>.Fallo(new ErrorDominio(
                        "venta.producto_inactivo", "El producto no existe, no está disponible para venta o usa otra moneda."));
                }

                var creada = DetalleVenta.Crear(
                    tipoLinea, producto.Id, null, null, producto.Codigo, producto.Nombre, producto.UnidadBase,
                    lineaSolicitud.Cantidad, producto.PrecioVenta.Importe, producto.CostoPromedio.Importe,
                    producto.ManejaFraccion, tipoDescuento, valorDescuento, impuestoNombre, impuestoPorcentaje);
                if (!creada.EsExitoso)
                {
                    return Resultado<VentaDto>.Fallo(creada.Error!);
                }

                detalle = creada.Valor!;
            }
            else
            {
                if (!lineaSolicitud.ServicioId.HasValue ||
                    !servicioPorId.TryGetValue(lineaSolicitud.ServicioId.Value, out var servicio) ||
                    !servicio.Activo || servicio.Precio.CodigoMoneda != configuracion.CodigoMoneda)
                {
                    return Resultado<VentaDto>.Fallo(new ErrorDominio(
                        "venta.servicio_inactivo", "El servicio no existe, está inactivo o usa otra moneda."));
                }

                if (configuracion.ExigirEmpleadoVentaServicio && !lineaSolicitud.EmpleadoId.HasValue)
                {
                    return Resultado<VentaDto>.Fallo(new ErrorDominio(
                        "venta.empleado_requerido", "La configuración del negocio exige asociar un empleado a cada servicio."));
                }

                Empleado? empleado = null;
                if (lineaSolicitud.EmpleadoId.HasValue &&
                    (!empleadoPorId.TryGetValue(lineaSolicitud.EmpleadoId.Value, out empleado) || !empleado.Activo))
                {
                    return Resultado<VentaDto>.Fallo(new ErrorDominio(
                        "venta.empleado_inactivo", "El empleado no existe o está inactivo."));
                }

                var creada = DetalleVenta.Crear(
                    tipoLinea, null, servicio.Id, empleado?.Id, null, servicio.Nombre, "servicio",
                    lineaSolicitud.Cantidad, servicio.Precio.Importe, null, false,
                    tipoDescuento, valorDescuento, impuestoNombre, impuestoPorcentaje);
                if (!creada.EsExitoso)
                {
                    return Resultado<VentaDto>.Fallo(creada.Error!);
                }

                detalle = creada.Valor!;
            }

            lineas.Add(detalle);
        }

        if (!TryDescuento(solicitud.DescuentoGeneral, out tipoDescuentoGeneral, out valorDescuentoGeneral))
        {
            return Resultado<VentaDto>.Fallo(ErrorSolicitud());
        }

        var ventaCreada = Venta.Crear(
            caja.Id,
            cliente?.Id,
            cliente?.Nombre,
            cliente?.Documento,
            reloj.GetUtcNow(),
            configuracion.CodigoMoneda,
            tipoDescuentoGeneral,
            valorDescuentoGeneral,
            lineas);
        if (!ventaCreada.EsExitoso)
        {
            return Resultado<VentaDto>.Fallo(ventaCreada.Error!);
        }

        var lineasServicioAsignadas = ventaCreada.Valor!.Detalles
            .Where(x => x.Tipo == TipoLineaVenta.Servicio && x.EmpleadoId.HasValue)
            .ToArray();
        var reglasComision = await unidad.ObtenerReglasComisionAsync(
            lineasServicioAsignadas.Select(x => x.EmpleadoId!.Value).Distinct().ToArray(),
            lineasServicioAsignadas.Select(x => x.ServicioId!.Value).Distinct().ToArray(),
            cancellationToken);
        var reglaPorAsignacion = reglasComision.ToDictionary(x => (x.EmpleadoId, x.ServicioId));
        foreach (var linea in lineasServicioAsignadas)
        {
            if (!reglaPorAsignacion.TryGetValue((linea.EmpleadoId!.Value, linea.ServicioId!.Value), out var regla))
            {
                continue;
            }

            var calculo = regla.CalcularImporte(linea);
            if (!calculo.EsExitoso)
            {
                return Resultado<VentaDto>.Fallo(calculo.Error!);
            }

            if (calculo.Valor <= 0m)
            {
                continue;
            }

            var devengo = Comision.CrearDevengo(
                linea.EmpleadoId.Value, linea.ServicioId.Value, ventaCreada.Valor.Id, linea.Id,
                regla.Tipo, regla.Valor, linea.BaseNetaAntesImpuestos, linea.Cantidad, calculo.Valor,
                configuracion.CodigoMoneda);
            if (!devengo.EsExitoso)
            {
                return Resultado<VentaDto>.Fallo(devengo.Error!);
            }

            unidad.Agregar(devengo.Valor!);
        }

        if (solicitud.SolicitarFactura && !configuracion.FacturacionHabilitada)
        {
            return Resultado<VentaDto>.Fallo(new ErrorDominio(
                "venta.facturacion_deshabilitada", "La facturación no está habilitada para este negocio."));
        }

        var pagos = ConstruirPagos(
            solicitud.Pagos ?? [],
            ventaCreada.Valor!.Id,
            caja.Id,
            metodos,
            configuracion.Id);
        if (!pagos.EsExitoso)
        {
            return Resultado<VentaDto>.Fallo(pagos.Error!);
        }

        foreach (var pago in pagos.Valor!)
        {
            var resultadoPago = ventaCreada.Valor!.RegistrarPago(pago);
            if (!resultadoPago.EsExitoso)
            {
                return Resultado<VentaDto>.Fallo(resultadoPago.Error!);
            }

            if (pago.EsEfectivo)
            {
                var movimientoCaja = MovimientoCaja.Crear(
                    caja.Id, TipoMovimientoCaja.PagoVenta, pago.Importe, configuracion.CodigoMoneda,
                    "Pago en efectivo de la venta", ventaCreada.Valor.Id);
                if (!movimientoCaja.EsExitoso)
                {
                    return Resultado<VentaDto>.Fallo(movimientoCaja.Error!);
                }

                unidad.Agregar(movimientoCaja.Valor!);
            }
        }

        if (ventaCreada.Valor!.TotalPendiente > 0m && !configuracion.PermitirSaldosPendientes)
        {
            return Resultado<VentaDto>.Fallo(new ErrorDominio(
                "venta.saldo_pendiente_deshabilitado", "El negocio no permite dejar saldo pendiente."));
        }

        var numeracionComprobante = await unidad.BloquearNumeracionAsync(
            TipoDocumentoVenta.Comprobante, cancellationToken);
        if (numeracionComprobante is null)
        {
            return Resultado<VentaDto>.Fallo(new ErrorDominio(
                "venta.numeracion_comprobante_pendiente", "Debe configurar la numeración del comprobante antes de vender."));
        }

        var numeroComprobante = numeracionComprobante.TomarSiguienteNumero();
        if (!numeroComprobante.EsExitoso)
        {
            return Resultado<VentaDto>.Fallo(numeroComprobante.Error!);
        }

        var comprobante = DocumentoVenta.Crear(
            ventaCreada.Valor.Id, TipoDocumentoVenta.Comprobante, numeracionComprobante.Prefijo,
            numeroComprobante.Valor, reloj.GetUtcNow(), configuracion, cliente?.Nombre, cliente?.Documento,
            configuracion.CodigoMoneda, ventaCreada.Valor.Subtotal,
            ventaCreada.Valor.DescuentoLineas + ventaCreada.Valor.DescuentoGeneral,
            ventaCreada.Valor.Impuestos, ventaCreada.Valor.Total);
        if (!comprobante.EsExitoso)
        {
            return Resultado<VentaDto>.Fallo(comprobante.Error!);
        }

        var agregarComprobante = ventaCreada.Valor.RegistrarDocumento(comprobante.Valor!);
        if (!agregarComprobante.EsExitoso)
        {
            return Resultado<VentaDto>.Fallo(agregarComprobante.Error!);
        }

        if (solicitud.SolicitarFactura)
        {
            var numeracionFactura = await unidad.BloquearNumeracionAsync(
                TipoDocumentoVenta.Factura, cancellationToken);
            if (numeracionFactura is null)
            {
                return Resultado<VentaDto>.Fallo(new ErrorDominio(
                    "venta.numeracion_factura_pendiente", "Debe configurar la numeración de facturas para emitir una factura."));
            }

            var numeroFactura = numeracionFactura.TomarSiguienteNumero();
            if (!numeroFactura.EsExitoso)
            {
                return Resultado<VentaDto>.Fallo(numeroFactura.Error!);
            }

            var factura = DocumentoVenta.Crear(
                ventaCreada.Valor.Id, TipoDocumentoVenta.Factura, numeracionFactura.Prefijo,
                numeroFactura.Valor, reloj.GetUtcNow(), configuracion, cliente?.Nombre, cliente?.Documento,
                configuracion.CodigoMoneda, ventaCreada.Valor.Subtotal,
                ventaCreada.Valor.DescuentoLineas + ventaCreada.Valor.DescuentoGeneral,
                ventaCreada.Valor.Impuestos, ventaCreada.Valor.Total);
            if (!factura.EsExitoso)
            {
                return Resultado<VentaDto>.Fallo(factura.Error!);
            }

            var agregarFactura = ventaCreada.Valor.RegistrarDocumento(factura.Valor!);
            if (!agregarFactura.EsExitoso)
            {
                return Resultado<VentaDto>.Fallo(agregarFactura.Error!);
            }
        }

        foreach (var linea in ventaCreada.Valor.Detalles.Where(x => x.Tipo == TipoLineaVenta.Producto))
        {
            var producto = productoPorId[linea.ArticuloInventarioId!.Value];
            var salida = producto.AplicarSalida(linea.Cantidad, configuracion.PermitirVentaSinStock);
            if (!salida.EsExitoso)
            {
                return Resultado<VentaDto>.Fallo(salida.Error!);
            }

            var costo = Dinero.Crear(linea.CostoInventarioUnitario!.Value, configuracion.CodigoMoneda);
            if (!costo.EsExitoso)
            {
                return Resultado<VentaDto>.Fallo(costo.Error!);
            }

            var movimiento = MovimientoInventario.Crear(
                producto, TipoMovimientoInventario.SalidaVenta, linea.Cantidad,
                producto.ExistenciaActual, costo.Valor!, null, ventaId: ventaCreada.Valor.Id,
                detalleVentaId: linea.Id);
            if (!movimiento.EsExitoso)
            {
                return Resultado<VentaDto>.Fallo(movimiento.Error!);
            }

            unidad.Agregar(movimiento.Valor!);
        }

        unidad.Agregar(ventaCreada.Valor);
        if (!await unidad.GuardarCambiosAsync(cancellationToken))
        {
            return Resultado<VentaDto>.Fallo(Conflicto());
        }

        await unidad.ConfirmarAsync(cancellationToken);
        return Resultado<VentaDto>.Exito(Convertir(ventaCreada.Valor));
    }

    public async Task<Resultado<VentaDto>> AgregarPagosAsync(
        Guid ventaId,
        SolicitudAgregarPagos solicitud,
        CancellationToken cancellationToken)
    {
        var usuarioId = usuarioActual.ObtenerId();
        if (!usuarioId.HasValue || solicitud.Pagos is null || solicitud.Pagos.Count is < 1 or > 10)
        {
            return Resultado<VentaDto>.Fallo(ErrorSolicitud());
        }

        await using var unidad = await repositorio.IniciarTransaccionAsync(cancellationToken);
        var caja = await unidad.BloquearCajaActualAsync(cancellationToken);
        if (caja is null)
        {
            return Resultado<VentaDto>.Fallo(new ErrorDominio(
                "caja.no_abierta", "Debe haber una caja abierta para registrar pagos."));
        }

        var venta = await unidad.BloquearVentaAsync(ventaId, cancellationToken);
        if (venta is null)
        {
            return Resultado<VentaDto>.Fallo(NoEncontrada());
        }

        var configuracion = await unidad.ObtenerConfiguracionPrincipalAsync(cancellationToken);
        if (configuracion is null)
        {
            return Resultado<VentaDto>.Fallo(ErrorConfiguracion());
        }

        var metodos = await unidad.ObtenerMetodosPagoAsync(cancellationToken);
        var pagos = ConstruirPagos(
            solicitud.Pagos, venta.Id, caja.Id, metodos, configuracion.Id);
        if (!pagos.EsExitoso)
        {
            return Resultado<VentaDto>.Fallo(pagos.Error!);
        }

        foreach (var pago in pagos.Valor!)
        {
            var resultadoPago = venta.RegistrarPago(pago);
            if (!resultadoPago.EsExitoso)
            {
                return Resultado<VentaDto>.Fallo(resultadoPago.Error!);
            }

            if (pago.EsEfectivo)
            {
                var movimientoCaja = MovimientoCaja.Crear(
                    caja.Id, TipoMovimientoCaja.PagoVenta, pago.Importe, configuracion.CodigoMoneda,
                    "Pago de saldo pendiente", venta.Id);
                if (!movimientoCaja.EsExitoso)
                {
                    return Resultado<VentaDto>.Fallo(movimientoCaja.Error!);
                }

                unidad.Agregar(movimientoCaja.Valor!);
            }
        }

        if (!await unidad.GuardarCambiosAsync(cancellationToken))
        {
            return Resultado<VentaDto>.Fallo(Conflicto());
        }

        await unidad.ConfirmarAsync(cancellationToken);
        return Resultado<VentaDto>.Exito(Convertir(venta));
    }

    public Task<Resultado<VentaDto>> DevolverAsync(
        Guid ventaId,
        SolicitudDevolucionVenta solicitud,
        CancellationToken cancellationToken) =>
        ProcesarDevolucionAsync(ventaId, solicitud.Motivo, solicitud.Lineas, solicitud.Pagos, anular: false,
            cancellationToken);

    public Task<Resultado<VentaDto>> AnularAsync(
        Guid ventaId,
        SolicitudAnularVenta solicitud,
        CancellationToken cancellationToken) =>
        ProcesarDevolucionAsync(ventaId, solicitud.Motivo, null, solicitud.Pagos, anular: true,
            cancellationToken);

    private async Task<Resultado<VentaDto>> ProcesarDevolucionAsync(
        Guid ventaId,
        string? motivo,
        IReadOnlyList<SolicitudLineaDevolucion>? lineasSolicitadas,
        IReadOnlyList<SolicitudPagoVenta>? pagosSolicitados,
        bool anular,
        CancellationToken cancellationToken)
    {
        var usuarioId = usuarioActual.ObtenerId();
        if (!usuarioId.HasValue || string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length > 250 ||
            !anular && (lineasSolicitadas is null || lineasSolicitadas.Count is < 1 or > 200) ||
            pagosSolicitados is { Count: > 10 })
        {
            return Resultado<VentaDto>.Fallo(ErrorSolicitud());
        }

        await using var unidad = await repositorio.IniciarTransaccionAsync(cancellationToken);
        var caja = await unidad.BloquearCajaActualAsync(cancellationToken);
        if (caja is null)
        {
            return Resultado<VentaDto>.Fallo(new ErrorDominio(
                "caja.no_abierta", "Debe haber una caja abierta para registrar una devolución."));
        }

        var venta = await unidad.BloquearVentaAsync(ventaId, cancellationToken);
        if (venta is null)
        {
            return Resultado<VentaDto>.Fallo(NoEncontrada());
        }

        var configuracion = await unidad.ObtenerConfiguracionPrincipalAsync(cancellationToken);
        if (configuracion is null)
        {
            return Resultado<VentaDto>.Fallo(ErrorConfiguracion());
        }

        var cantidadesDevueltas = venta.Devoluciones
            .SelectMany(x => x.Detalles)
            .GroupBy(x => x.DetalleVentaId)
            .ToDictionary(x => x.Key, x => x.Sum(detalle => detalle.Cantidad));
        var importesDevueltos = venta.Devoluciones
            .SelectMany(x => x.Detalles)
            .GroupBy(x => x.DetalleVentaId)
            .ToDictionary(x => x.Key, x => x.Sum(detalle => detalle.Importe));
        var solicitudes = new List<SolicitudLineaDevolucion>();

        if (anular)
        {
            solicitudes.AddRange(venta.Detalles
                .Select(linea => new SolicitudLineaDevolucion(
                    linea.Id, linea.Cantidad - cantidadesDevueltas.GetValueOrDefault(linea.Id)))
                .Where(x => x.Cantidad > 0m));
        }
        else
        {
            solicitudes.AddRange(lineasSolicitadas!);
        }

        if (solicitudes.Count == 0 || solicitudes.Select(x => x.DetalleVentaId).Distinct().Count() != solicitudes.Count)
        {
            return Resultado<VentaDto>.Fallo(new ErrorDominio(
                "venta.devolucion_invalida", "No hay unidades pendientes de devolución o hay líneas repetidas."));
        }

        var lineasVenta = venta.Detalles.ToDictionary(x => x.Id);
        var detallesDevolucion = new List<DetalleDevolucionVenta>(solicitudes.Count);
        var cantidadesPorLinea = new Dictionary<Guid, decimal>();
        foreach (var solicitudLinea in solicitudes)
        {
            if (!lineasVenta.TryGetValue(solicitudLinea.DetalleVentaId, out var linea) ||
                solicitudLinea.Cantidad <= 0m ||
                decimal.Round(solicitudLinea.Cantidad, 3) != solicitudLinea.Cantidad)
            {
                return Resultado<VentaDto>.Fallo(ErrorSolicitud());
            }

            var cantidadYaDevuelta = cantidadesDevueltas.GetValueOrDefault(linea.Id);
            var importeYaDevuelto = importesDevueltos.GetValueOrDefault(linea.Id);
            var cantidadRestante = linea.Cantidad - cantidadYaDevuelta;
            if (solicitudLinea.Cantidad > cantidadRestante ||
                linea.Tipo == TipoLineaVenta.Servicio && decimal.Truncate(solicitudLinea.Cantidad) != solicitudLinea.Cantidad)
            {
                return Resultado<VentaDto>.Fallo(new ErrorDominio(
                    "venta.cantidad_devolucion_invalida", "La cantidad supera las unidades aún disponibles para devolver."));
            }

            var importeRestante = linea.TotalImporte - importeYaDevuelto;
            var importe = solicitudLinea.Cantidad == cantidadRestante
                ? importeRestante
                : Math.Min(importeRestante, Redondear(linea.TotalImporte * solicitudLinea.Cantidad / linea.Cantidad));
            var detalle = DetalleDevolucionVenta.Crear(linea.Id, solicitudLinea.Cantidad, importe);
            if (!detalle.EsExitoso)
            {
                return Resultado<VentaDto>.Fallo(detalle.Error!);
            }

            detallesDevolucion.Add(detalle.Valor!);
            cantidadesPorLinea.Add(linea.Id, solicitudLinea.Cantidad);
        }

        var comisionesVenta = await unidad.ObtenerComisionesVentaAsync(
            cantidadesPorLinea.Keys.ToArray(), bloquear: true, cancellationToken);

        var importeLineas = detallesDevolucion.Sum(x => x.Importe);
        var totalDevueltoNuevo = venta.TotalDevuelto + importeLineas;
        var saldoVentaRestante = Math.Max(0m, venta.Total - totalDevueltoNuevo);
        var reembolsoEsperado = Math.Max(
            0m, venta.TotalPagado - venta.TotalReintegrado - saldoVentaRestante);
        var metodos = await unidad.ObtenerMetodosPagoAsync(cancellationToken);
        var pagos = ConstruirPagosDevolucion(pagosSolicitados ?? [], metodos, configuracion.Id);
        if (!pagos.EsExitoso)
        {
            return Resultado<VentaDto>.Fallo(pagos.Error!);
        }

        var devolucionCreada = DevolucionVenta.Crear(
            venta.Id, reloj.GetUtcNow(), motivo, detallesDevolucion, pagos.Valor);
        if (!devolucionCreada.EsExitoso)
        {
            return Resultado<VentaDto>.Fallo(devolucionCreada.Error!);
        }

        var resultadoDevolucion = venta.RegistrarDevolucion(
            devolucionCreada.Valor!, anular, usuarioId.Value, reloj.GetUtcNow());
        if (!resultadoDevolucion.EsExitoso)
        {
            return Resultado<VentaDto>.Fallo(resultadoDevolucion.Error!);
        }

        var devengosPorDetalle = comisionesVenta
            .Where(x => x.TipoMovimiento == TipoMovimientoComision.DevengoServicio)
            .ToDictionary(x => x.DetalleVentaId);
        foreach (var (detalleId, cantidadSolicitada) in cantidadesPorLinea)
        {
            if (!lineasVenta.TryGetValue(detalleId, out var linea) || linea.Tipo != TipoLineaVenta.Servicio ||
                !devengosPorDetalle.TryGetValue(detalleId, out var devengo) ||
                devengo.Estado == EstadoComision.Revertida)
            {
                continue;
            }

            var ajustesPrevios = comisionesVenta
                .Where(x => x.ComisionOriginalId == devengo.Id)
                .ToArray();
            var calculoAjuste = devengo.CalcularAjusteDevolucion(cantidadSolicitada, ajustesPrevios);
            if (!calculoAjuste.EsExitoso)
            {
                return Resultado<VentaDto>.Fallo(calculoAjuste.Error!);
            }

            var ajusteCalculado = calculoAjuste.Valor!;
            if (!ajusteCalculado.TieneAjuste)
            {
                continue;
            }

            var ajuste = Comision.CrearAjusteDevolucion(
                devengo, devolucionCreada.Valor!.Id, ajusteCalculado.BaseCalculo,
                ajusteCalculado.Cantidad, ajusteCalculado.Importe);
            if (!ajuste.EsExitoso)
            {
                return Resultado<VentaDto>.Fallo(ajuste.Error!);
            }

            unidad.Agregar(ajuste.Valor!);
            if (ajusteCalculado.RevierteTodo && devengo.Estado == EstadoComision.Pendiente)
            {
                devengo.MarcarRevertida();
                foreach (var ajustePrevio in ajustesPrevios)
                {
                    ajustePrevio.MarcarRevertida();
                }

                ajuste.Valor!.MarcarRevertida();
            }
        }

        foreach (var pago in devolucionCreada.Valor!.Pagos.Where(x => x.EsEfectivo))
        {
            var movimientoCaja = MovimientoCaja.Crear(
                caja.Id, TipoMovimientoCaja.Reembolso, pago.Importe, configuracion.CodigoMoneda,
                motivo, devolucionVentaId: devolucionCreada.Valor.Id);
            if (!movimientoCaja.EsExitoso)
            {
                return Resultado<VentaDto>.Fallo(movimientoCaja.Error!);
            }

            unidad.Agregar(movimientoCaja.Valor!);
        }

        var productosPorDevolver = venta.Detalles
            .Where(x => cantidadesPorLinea.ContainsKey(x.Id) && x.Tipo == TipoLineaVenta.Producto)
            .Select(x => x.ArticuloInventarioId!.Value)
            .Distinct()
            .ToArray();
        var productos = await unidad.ObtenerArticulosAsync(
            productosPorDevolver, bloquear: true, cancellationToken);
        var productoPorId = productos.ToDictionary(x => x.Id);
        foreach (var linea in venta.Detalles.Where(x => cantidadesPorLinea.ContainsKey(x.Id) &&
                                                        x.Tipo == TipoLineaVenta.Producto))
        {
            if (!productoPorId.TryGetValue(linea.ArticuloInventarioId!.Value, out var producto))
            {
                return Resultado<VentaDto>.Fallo(new ErrorDominio(
                    "venta.producto_no_encontrado", "No se puede reponer el producto asociado a la venta."));
            }

            var cantidad = cantidadesPorLinea[linea.Id];
            var costo = Dinero.Crear(linea.CostoInventarioUnitario!.Value, configuracion.CodigoMoneda);
            if (!costo.EsExitoso)
            {
                return Resultado<VentaDto>.Fallo(costo.Error!);
            }

            var entrada = producto.AplicarEntrada(cantidad, costo.Valor!);
            if (!entrada.EsExitoso)
            {
                return Resultado<VentaDto>.Fallo(entrada.Error!);
            }

            var movimiento = MovimientoInventario.Crear(
                producto, TipoMovimientoInventario.EntradaDevolucion, cantidad,
                producto.ExistenciaActual, costo.Valor!, motivo, ventaId: venta.Id,
                detalleVentaId: linea.Id);
            if (!movimiento.EsExitoso)
            {
                return Resultado<VentaDto>.Fallo(movimiento.Error!);
            }

            unidad.Agregar(movimiento.Valor!);
        }

        if (!await unidad.GuardarCambiosAsync(cancellationToken))
        {
            return Resultado<VentaDto>.Fallo(Conflicto());
        }

        await unidad.ConfirmarAsync(cancellationToken);
        return Resultado<VentaDto>.Exito(Convertir(venta));
    }

    private static Resultado<IReadOnlyList<PagoVenta>> ConstruirPagos(
        IReadOnlyList<SolicitudPagoVenta> solicitudes,
        Guid ventaId,
        Guid cajaId,
        IReadOnlyList<MetodoPagoConfigurado> metodos,
        Guid configuracionId)
    {
        var metodosPorId = metodos
            .Where(x => x.ConfiguracionNegocioId == configuracionId)
            .ToDictionary(x => x.Id);
        var pagos = new List<PagoVenta>(solicitudes.Count);
        foreach (var solicitud in solicitudes)
        {
            if (!metodosPorId.TryGetValue(solicitud.MetodoPagoId, out var metodo))
            {
                return Resultado<IReadOnlyList<PagoVenta>>.Fallo(new ErrorDominio(
                    "venta.metodo_pago_inactivo", "El método de pago no existe o no pertenece al negocio."));
            }

            var pago = PagoVenta.Crear(ventaId, cajaId, metodo, solicitud.Importe, solicitud.Referencia);
            if (!pago.EsExitoso)
            {
                return Resultado<IReadOnlyList<PagoVenta>>.Fallo(pago.Error!);
            }

            pagos.Add(pago.Valor!);
        }

        return Resultado<IReadOnlyList<PagoVenta>>.Exito(pagos);
    }

    private static Resultado<IReadOnlyList<PagoDevolucionVenta>> ConstruirPagosDevolucion(
        IReadOnlyList<SolicitudPagoVenta> solicitudes,
        IReadOnlyList<MetodoPagoConfigurado> metodos,
        Guid configuracionId)
    {
        var metodosPorId = metodos
            .Where(x => x.ConfiguracionNegocioId == configuracionId)
            .ToDictionary(x => x.Id);
        var pagos = new List<PagoDevolucionVenta>(solicitudes.Count);
        foreach (var solicitud in solicitudes)
        {
            if (!metodosPorId.TryGetValue(solicitud.MetodoPagoId, out var metodo))
            {
                return Resultado<IReadOnlyList<PagoDevolucionVenta>>.Fallo(new ErrorDominio(
                    "venta.metodo_pago_inactivo", "El método de reembolso no existe o no pertenece al negocio."));
            }

            var pago = PagoDevolucionVenta.Crear(metodo, solicitud.Importe, solicitud.Referencia);
            if (!pago.EsExitoso)
            {
                return Resultado<IReadOnlyList<PagoDevolucionVenta>>.Fallo(pago.Error!);
            }

            pagos.Add(pago.Valor!);
        }

        return Resultado<IReadOnlyList<PagoDevolucionVenta>>.Exito(pagos);
    }

    private static (Guid[] Productos, Guid[] Servicios, Guid[] Empleados)? ValidarReferencias(
        IReadOnlyList<SolicitudLineaVenta> solicitudes)
    {
        var productos = new List<Guid>();
        var servicios = new List<Guid>();
        var empleados = new List<Guid>();
        foreach (var solicitud in solicitudes)
        {
            if (!Enum.TryParse<TipoLineaVenta>(solicitud.Tipo, true, out var tipo) || !Enum.IsDefined(tipo) ||
                solicitud.ArticuloInventarioId == Guid.Empty || solicitud.ServicioId == Guid.Empty ||
                solicitud.EmpleadoId == Guid.Empty || solicitud.ImpuestoId == Guid.Empty)
            {
                return null;
            }

            if (tipo == TipoLineaVenta.Producto)
            {
                if (!solicitud.ArticuloInventarioId.HasValue || solicitud.ServicioId.HasValue ||
                    solicitud.EmpleadoId.HasValue)
                {
                    return null;
                }

                productos.Add(solicitud.ArticuloInventarioId.Value);
            }
            else
            {
                if (!solicitud.ServicioId.HasValue || solicitud.ArticuloInventarioId.HasValue)
                {
                    return null;
                }

                servicios.Add(solicitud.ServicioId.Value);
                if (solicitud.EmpleadoId.HasValue)
                {
                    empleados.Add(solicitud.EmpleadoId.Value);
                }
            }
        }

        return (productos.Distinct().ToArray(), servicios.Distinct().ToArray(), empleados.Distinct().ToArray());
    }

    private static bool TryDescuento(
        SolicitudDescuentoVenta? solicitud,
        out TipoDescuento? tipo,
        out decimal valor)
    {
        tipo = null;
        valor = 0m;
        if (solicitud is null)
        {
            return true;
        }

        if (!Enum.TryParse<TipoDescuento>(solicitud.Tipo, true, out var tipoParseado) ||
            !Enum.IsDefined(tipoParseado))
        {
            return false;
        }

        tipo = tipoParseado;
        valor = solicitud.Valor;
        return true;
    }

    private static T? ParsearEstado<T>(string? texto) where T : struct, Enum =>
        Enum.TryParse<T>(texto, true, out var estado) && Enum.IsDefined(estado) ? estado : null;

    private static bool ValidarPaginacion(int pagina, int tamanoPagina) =>
        pagina >= 1 && tamanoPagina is >= 1 and <= 100 && (pagina - 1L) * tamanoPagina <= int.MaxValue;

    private static decimal Redondear(decimal importe) => decimal.Round(importe, 2, MidpointRounding.AwayFromZero);

    private static string LimpiarNombreArchivo(string valor) =>
        string.Concat(valor.Where(caracter => char.IsAsciiLetterOrDigit(caracter) || caracter is '-' or '_'));

    private static VentaDto Convertir(Venta venta) => new(
        venta.Id,
        venta.CajaId,
        venta.ClienteId,
        venta.ClienteNombre,
        venta.ClienteDocumento,
        venta.FechaVentaUtc,
        venta.CodigoMoneda,
        venta.Estado.ToString(),
        venta.Subtotal,
        venta.TipoDescuentoGeneral?.ToString(),
        venta.ValorDescuentoGeneral,
        venta.DescuentoLineas,
        venta.DescuentoGeneral,
        venta.Impuestos,
        venta.Total,
        venta.TotalPagado,
        venta.TotalDevuelto,
        venta.TotalReintegrado,
        venta.TotalPendiente,
        venta.MotivoAnulacion,
        venta.Detalles.Select(x => new LineaVentaDto(
            x.Id, x.Tipo.ToString(), x.ArticuloInventarioId, x.ServicioId, x.EmpleadoId, x.Codigo,
            x.Nombre, x.Unidad, x.Cantidad, x.PrecioUnitario, x.TipoDescuentoLinea?.ToString(),
            x.ValorDescuento, x.DescuentoImporte, x.DescuentoGeneralImporte, x.ImpuestoNombre,
            x.ImpuestoPorcentaje, x.ImpuestoImporte, x.TotalImporte)).ToArray(),
        venta.Pagos.OrderBy(x => x.FechaCreacionUtc).Select(x => new PagoVentaDto(
            x.Id, x.MetodoPagoId, x.MetodoPagoNombre, x.EsEfectivo, x.Importe, x.Referencia,
            x.FechaCreacionUtc)).ToArray(),
        venta.Documentos.OrderBy(x => x.TipoDocumento).Select(x => new DocumentoVentaDto(
            x.Id, x.TipoDocumento.ToString(), x.NumeroCompleto, x.FechaEmisionUtc, x.Total)).ToArray(),
        venta.Devoluciones.OrderBy(x => x.FechaUtc).Select(x => new DevolucionVentaDto(
            x.Id, x.FechaUtc, x.Motivo, x.ImporteLineas, x.ImporteReintegrado,
            x.Detalles.Select(detalle => new DetalleDevolucionDto(
                detalle.DetalleVentaId, detalle.Cantidad, detalle.Importe)).ToArray(),
            x.Pagos.Select(pago => new PagoDevolucionVentaDto(
                pago.MetodoPagoId, pago.MetodoPagoNombre, pago.EsEfectivo, pago.Importe, pago.Referencia)).ToArray()))
            .ToArray(),
        venta.FechaCreacionUtc,
        venta.FechaModificacionUtc);

    private static ErrorDominio ErrorBusqueda() => new(
        "venta.busqueda_invalida", "Los filtros de venta o la paginación no son válidos.");

    private static ErrorDominio ErrorSolicitud() => new(
        "venta.solicitud_invalida", "Los datos de la operación de venta no son válidos.");

    private static ErrorDominio ErrorConfiguracion() => new(
        "configuracion.no_configurada", "Primero debe registrar la configuración del establecimiento.");

    private static ErrorDominio ErrorUsuario() => new(
        "usuario.no_autenticado", "No se pudo identificar al usuario de la operación.");

    private static ErrorDominio NoEncontrada() => new(
        "venta.no_encontrada", "No se encontró la venta solicitada.");

    private static ErrorDominio Conflicto() => new(
        "venta.conflicto", "La venta, caja o inventario cambió en otra operación. Vuelva a consultar e intente de nuevo.");
}

public sealed record DocumentoVentaArchivoDto(string NombreArchivo, string TipoMime, byte[] Contenido);
