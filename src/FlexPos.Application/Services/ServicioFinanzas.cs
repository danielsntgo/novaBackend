using FlexPos.Application.DTOs.Finanzas;
using FlexPos.Application.Interfaces;
using FlexPos.Domain.Entities;
using FlexPos.Domain.Enums;
using FlexPos.Domain.Errors;

namespace FlexPos.Application.Services;

public sealed class ServicioFinanzas(
    IRepositorioFinanzas repositorio,
    IUsuarioActual usuarioActual,
    TimeProvider reloj)
{
    public const string CuentaEfectivoBase = "Caja principal";
    private const string CuentaAsignarBase = "Cuenta por asignar";

    public async Task AsegurarCuentasBaseAsync(Guid configuracionId, string codigoMoneda, CancellationToken cancellationToken)
    {
        var efectivo = await repositorio.ObtenerCuentaBaseAsync(
            configuracionId, TipoCuentaFinanciera.Efectivo, CuentaEfectivoBase, cancellationToken);
        var porAsignar = await repositorio.ObtenerCuentaBaseAsync(
            configuracionId, TipoCuentaFinanciera.Otro, CuentaAsignarBase, cancellationToken);
        if (efectivo is null)
        {
            var creada = CuentaFinanciera.Crear(
                configuracionId, CuentaEfectivoBase, TipoCuentaFinanciera.Efectivo, codigoMoneda,
                null, null, null);
            if (!creada.EsExitoso)
            {
                throw new InvalidOperationException(creada.Error!.Mensaje);
            }

            repositorio.Agregar(creada.Valor!);
        }

        if (porAsignar is null)
        {
            var creada = CuentaFinanciera.Crear(
                configuracionId, CuentaAsignarBase, TipoCuentaFinanciera.Otro, codigoMoneda,
                null, null, null);
            if (!creada.EsExitoso)
            {
                throw new InvalidOperationException(creada.Error!.Mensaje);
            }

            repositorio.Agregar(creada.Valor!);
        }

        if ((efectivo is null || porAsignar is null) &&
            !await repositorio.GuardarCambiosAsync(cancellationToken))
        {
            throw new InvalidOperationException("No se pudieron crear las cuentas financieras iniciales.");
        }
    }

    public async Task<Resultado<PaginaCuentasFinancierasDto>> ListarCuentasAsync(
        string? tipoTexto,
        bool? activa,
        string? buscar,
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken)
    {
        if (!ValidarPaginacion(pagina, tamanoPagina) ||
            tipoTexto is not null && !TryTipoCuenta(tipoTexto, out _))
        {
            return Resultado<PaginaCuentasFinancierasDto>.Fallo(ErrorBusqueda());
        }

        var configuracion = await repositorio.ObtenerConfiguracionPrincipalAsync(cancellationToken);
        if (configuracion is null)
        {
            return Resultado<PaginaCuentasFinancierasDto>.Fallo(NoConfiguracion());
        }

        TipoCuentaFinanciera? tipo = tipoTexto is null ? null : ParsearTipoCuenta(tipoTexto);
        var (elementos, total) = await repositorio.ListarCuentasAsync(
            configuracion.Id, tipo, activa, Normalizar(buscar), CalcularOmitir(pagina, tamanoPagina),
            tamanoPagina, cancellationToken);
        var saldos = await repositorio.CalcularSaldosAsync(elementos.Select(x => x.Id).ToArray(), cancellationToken);
        return Resultado<PaginaCuentasFinancierasDto>.Exito(new PaginaCuentasFinancierasDto(
            elementos.Select(x => Convertir(x, saldos.GetValueOrDefault(x.Id))).ToArray(),
            pagina, tamanoPagina, total));
    }

    public async Task<Resultado<CuentaFinancieraDto>> ObtenerCuentaAsync(Guid id, CancellationToken cancellationToken)
    {
        var configuracion = await repositorio.ObtenerConfiguracionPrincipalAsync(cancellationToken);
        var cuenta = configuracion is null || id == Guid.Empty
            ? null
            : await repositorio.ObtenerCuentaAsync(configuracion.Id, id, cancellationToken);
        return cuenta is null
            ? Resultado<CuentaFinancieraDto>.Fallo(NoEncontrada())
            : Resultado<CuentaFinancieraDto>.Exito(Convertir(
                cuenta, (await repositorio.CalcularSaldosAsync([cuenta.Id], cancellationToken)).GetValueOrDefault(cuenta.Id)));
    }

    public async Task<Resultado<CuentaFinancieraDto>> CrearCuentaAsync(
        SolicitudCuentaFinanciera solicitud,
        CancellationToken cancellationToken)
    {
        var configuracion = await repositorio.ObtenerConfiguracionPrincipalAsync(cancellationToken);
        if (configuracion is null)
        {
            return Resultado<CuentaFinancieraDto>.Fallo(NoConfiguracion());
        }

        if (!TryTipoCuenta(solicitud.Tipo, out var tipo))
        {
            return Resultado<CuentaFinancieraDto>.Fallo(ErrorSolicitud());
        }

        var saldoInicial = solicitud.SaldoInicial ?? 0m;
        if (saldoInicial < 0m || saldoInicial > 9_999_999_999_999_999.99m ||
            decimal.Round(saldoInicial, 2) != saldoInicial)
        {
            return Resultado<CuentaFinancieraDto>.Fallo(ErrorSolicitud());
        }

        var cuenta = CuentaFinanciera.Crear(
            configuracion.Id, solicitud.Nombre, tipo, solicitud.CodigoMoneda,
            solicitud.Entidad, solicitud.UltimosDigitos, solicitud.CupoCredito);
        if (!cuenta.EsExitoso)
        {
            return Resultado<CuentaFinancieraDto>.Fallo(cuenta.Error!);
        }

        var saldoContable = tipo == TipoCuentaFinanciera.TarjetaCredito ? -saldoInicial : saldoInicial;
        var saldoValido = cuenta.Valor!.ValidarSaldo(saldoContable);
        if (!saldoValido.EsExitoso)
        {
            return Resultado<CuentaFinancieraDto>.Fallo(saldoValido.Error!);
        }

        MovimientoFinanciero? movimientoSaldoInicial = null;
        if (saldoInicial > 0m)
        {
            var usuarioId = usuarioActual.ObtenerId();
            if (!usuarioId.HasValue)
            {
                return Resultado<CuentaFinancieraDto>.Fallo(ErrorUsuario());
            }

            var ahora = reloj.GetUtcNow();
            var movimiento = MovimientoFinanciero.Crear(
                cuenta.Valor.Id, TipoMovimientoFinanciero.SaldoInicial,
                tipo == TipoCuentaFinanciera.TarjetaCredito
                    ? SentidoMovimientoFinanciero.Salida
                    : SentidoMovimientoFinanciero.Entrada,
                saldoInicial, cuenta.Valor.CodigoMoneda, ahora, "Saldo inicial");
            if (!movimiento.EsExitoso)
            {
                return Resultado<CuentaFinancieraDto>.Fallo(movimiento.Error!);
            }

            movimientoSaldoInicial = movimiento.Valor!;
            movimientoSaldoInicial.RegistrarCreacion(ahora, usuarioId.Value);
        }

        await using var unidad = await repositorio.IniciarTransaccionAsync(cancellationToken);
        var metodoSeleccionado = solicitud.MetodoPagoId.HasValue
            ? await unidad.BloquearMetodoPagoAsync(configuracion.Id, solicitud.MetodoPagoId.Value, cancellationToken)
            : null;
        if (solicitud.MetodoPagoId.HasValue &&
            (metodoSeleccionado is null || !metodoSeleccionado.Activo ||
             metodoSeleccionado.EsEfectivo != (tipo == TipoCuentaFinanciera.Efectivo)))
        {
            return Resultado<CuentaFinancieraDto>.Fallo(NoMetodoPago());
        }

        unidad.Agregar(cuenta.Valor!);
        if (movimientoSaldoInicial is not null)
        {
            unidad.Agregar(movimientoSaldoInicial);
        }

        if (metodoSeleccionado is not null)
        {
            var asociacion = metodoSeleccionado.Actualizar(
                metodoSeleccionado.Nombre, metodoSeleccionado.RequiereReferencia,
                metodoSeleccionado.EsEfectivo, cuenta.Valor);
            if (!asociacion.EsExitoso)
            {
                return Resultado<CuentaFinancieraDto>.Fallo(asociacion.Error!);
            }
        }

        if (!await unidad.GuardarCambiosAsync(cancellationToken))
        {
            return Resultado<CuentaFinancieraDto>.Fallo(Conflicto());
        }

        await unidad.ConfirmarAsync(cancellationToken);
        return Resultado<CuentaFinancieraDto>.Exito(Convertir(cuenta.Valor!, saldoContable));
    }

    public async Task<Resultado<MovimientoFinancieroDto>> RegistrarMovimientoMetodoPagoAsync(
        SolicitudMovimientoMetodoPago solicitud,
        CancellationToken cancellationToken)
    {
        if (solicitud.MetodoPagoId == Guid.Empty || !Enum.TryParse<SentidoMovimientoFinanciero>(
                solicitud.Sentido, true, out var sentido) || !Enum.IsDefined(sentido) ||
            solicitud.Importe <= 0m || solicitud.Importe > 9_999_999_999_999_999.99m ||
            decimal.Round(solicitud.Importe, 2) != solicitud.Importe ||
            string.IsNullOrWhiteSpace(solicitud.Concepto) || solicitud.Concepto.Trim().Length > 250)
        {
            return Resultado<MovimientoFinancieroDto>.Fallo(ErrorMovimiento());
        }

        var usuarioId = usuarioActual.ObtenerId();
        if (!usuarioId.HasValue)
        {
            return Resultado<MovimientoFinancieroDto>.Fallo(ErrorUsuario());
        }

        var configuracion = await repositorio.ObtenerConfiguracionPrincipalAsync(cancellationToken);
        if (configuracion is null)
        {
            return Resultado<MovimientoFinancieroDto>.Fallo(NoConfiguracion());
        }

        var metodoPrevio = await repositorio.ObtenerMetodoPagoAsync(
            configuracion.Id, solicitud.MetodoPagoId, cancellationToken);
        if (metodoPrevio is null || !metodoPrevio.Activo)
        {
            return Resultado<MovimientoFinancieroDto>.Fallo(NoMetodoPago());
        }

        await using var unidad = await repositorio.IniciarTransaccionAsync(cancellationToken);
        var caja = metodoPrevio.EsEfectivo
            ? await unidad.BloquearCajaActualAsync(cancellationToken)
            : null;
        if (metodoPrevio.EsEfectivo && caja is null)
        {
            return Resultado<MovimientoFinancieroDto>.Fallo(new ErrorDominio(
                "caja.no_abierta", "Abre la caja de efectivo antes de registrar este movimiento."));
        }

        var metodo = await unidad.BloquearMetodoPagoAsync(
            configuracion.Id, solicitud.MetodoPagoId, cancellationToken);
        if (metodo is null || !metodo.Activo)
        {
            return Resultado<MovimientoFinancieroDto>.Fallo(NoMetodoPago());
        }

        if (metodo.CuentaFinancieraId != metodoPrevio.CuentaFinancieraId ||
            metodo.EsEfectivo != metodoPrevio.EsEfectivo)
        {
            return Resultado<MovimientoFinancieroDto>.Fallo(Conflicto(
                "El método de pago cambió durante la operación. Actualiza e inténtalo de nuevo."));
        }

        var cuenta = await unidad.BloquearCuentaAsync(
            configuracion.Id, metodo.CuentaFinancieraId, cancellationToken);
        if (cuenta is null || !metodo.EsCompatibleConCuenta(cuenta) ||
            cuenta.CodigoMoneda != configuracion.CodigoMoneda)
        {
            return Resultado<MovimientoFinancieroDto>.Fallo(Conflicto(
                "La cuenta asociada al método de pago no está disponible."));
        }

        if (metodo.EsEfectivo && (caja is null || caja.Estado != EstadoCaja.Abierta ||
                                  caja.CuentaFinancieraId != cuenta.Id))
        {
            return Resultado<MovimientoFinancieroDto>.Fallo(Conflicto(
                "La caja abierta no corresponde a la cuenta asignada al método de efectivo."));
        }

        var tipoMovimiento = sentido == SentidoMovimientoFinanciero.Entrada
            ? TipoMovimientoFinanciero.IngresoManual
            : TipoMovimientoFinanciero.EgresoManual;
        var ahora = reloj.GetUtcNow();
        var movimiento = MovimientoFinanciero.Crear(
            cuenta.Id, tipoMovimiento, sentido, solicitud.Importe, cuenta.CodigoMoneda, ahora,
            solicitud.Concepto, metodo.EsEfectivo ? caja!.Id : null, metodo.Id);
        if (!movimiento.EsExitoso)
        {
            return Resultado<MovimientoFinancieroDto>.Fallo(movimiento.Error!);
        }

        var saldoActual = await unidad.CalcularSaldoAsync(cuenta.Id, cancellationToken);
        var saldoPosterior = movimiento.Valor!.Aplicar(saldoActual);
        var validacionSaldo = cuenta.ValidarSaldo(saldoPosterior);
        if (!validacionSaldo.EsExitoso)
        {
            return Resultado<MovimientoFinancieroDto>.Fallo(validacionSaldo.Error!);
        }

        movimiento.Valor.RegistrarCreacion(ahora, usuarioId.Value);
        unidad.Agregar(movimiento.Valor);
        if (!await unidad.GuardarCambiosAsync(cancellationToken))
        {
            return Resultado<MovimientoFinancieroDto>.Fallo(Conflicto());
        }

        await unidad.ConfirmarAsync(cancellationToken);
        return Resultado<MovimientoFinancieroDto>.Exito(Convertir(movimiento.Valor, saldoPosterior));
    }

    public async Task<Resultado<CuentaFinancieraDto>> ActualizarCuentaAsync(
        Guid id,
        SolicitudCuentaFinanciera solicitud,
        CancellationToken cancellationToken)
    {
        var configuracion = await repositorio.ObtenerConfiguracionPrincipalAsync(cancellationToken);
        if (configuracion is null)
        {
            return Resultado<CuentaFinancieraDto>.Fallo(NoConfiguracion());
        }

        if (!TryTipoCuenta(solicitud.Tipo, out var tipo))
        {
            return Resultado<CuentaFinancieraDto>.Fallo(ErrorSolicitud());
        }

        await using var unidad = await repositorio.IniciarTransaccionAsync(cancellationToken);
        var cuenta = await unidad.BloquearCuentaAsync(configuracion.Id, id, cancellationToken);
        if (cuenta is null)
        {
            return Resultado<CuentaFinancieraDto>.Fallo(NoEncontrada());
        }

        var metodoSeleccionado = solicitud.MetodoPagoId.HasValue
            ? await unidad.BloquearMetodoPagoAsync(configuracion.Id, solicitud.MetodoPagoId.Value, cancellationToken)
            : null;
        if (solicitud.MetodoPagoId.HasValue &&
            (metodoSeleccionado is null || !metodoSeleccionado.Activo ||
             metodoSeleccionado.EsEfectivo != (tipo == TipoCuentaFinanciera.Efectivo)))
        {
            return Resultado<CuentaFinancieraDto>.Fallo(NoMetodoPago());
        }

        var esCuentaBase =
            cuenta.Tipo == TipoCuentaFinanciera.Efectivo && cuenta.Nombre == CuentaEfectivoBase ||
            cuenta.Tipo == TipoCuentaFinanciera.Otro && cuenta.Nombre == CuentaAsignarBase;
        var tipoBase = cuenta.Tipo == TipoCuentaFinanciera.Efectivo
            ? TipoCuentaFinanciera.Efectivo
            : TipoCuentaFinanciera.Otro;
        var nombreBase = cuenta.Tipo == TipoCuentaFinanciera.Efectivo ? CuentaEfectivoBase : CuentaAsignarBase;
        if (esCuentaBase && (tipo != tipoBase ||
            !string.Equals(solicitud.Nombre?.Trim(), nombreBase, StringComparison.OrdinalIgnoreCase)))
        {
            return Resultado<CuentaFinancieraDto>.Fallo(Conflicto(
                "No se puede cambiar el nombre ni el tipo de las cuentas base del sistema."));
        }

        var saldo = await unidad.CalcularSaldoAsync(cuenta.Id, cancellationToken);
        var tieneDependencias = await unidad.TieneMovimientosAsync(cuenta.Id, cancellationToken) ||
                                await unidad.TieneMetodoPagoAsync(cuenta.Id, cancellationToken) ||
                                await unidad.TieneCajaAbiertaAsync(cuenta.Id, cancellationToken);
        var actualizacion = cuenta.Actualizar(
            solicitud.Nombre, tipo, solicitud.CodigoMoneda, solicitud.Entidad, solicitud.UltimosDigitos,
            solicitud.CupoCredito, tieneDependencias, saldo);
        if (!actualizacion.EsExitoso)
        {
            return Resultado<CuentaFinancieraDto>.Fallo(actualizacion.Error!);
        }

        if (metodoSeleccionado is not null)
        {
            var asociacion = metodoSeleccionado.Actualizar(
                metodoSeleccionado.Nombre, metodoSeleccionado.RequiereReferencia,
                metodoSeleccionado.EsEfectivo, cuenta);
            if (!asociacion.EsExitoso)
            {
                return Resultado<CuentaFinancieraDto>.Fallo(asociacion.Error!);
            }
        }

        if (!await unidad.GuardarCambiosAsync(cancellationToken))
        {
            return Resultado<CuentaFinancieraDto>.Fallo(Conflicto());
        }

        await unidad.ConfirmarAsync(cancellationToken);
        return Resultado<CuentaFinancieraDto>.Exito(Convertir(cuenta, saldo));
    }

    public async Task<Resultado<CuentaFinancieraDto>> EstablecerEstadoAsync(
        Guid id,
        bool activa,
        CancellationToken cancellationToken)
    {
        var configuracion = await repositorio.ObtenerConfiguracionPrincipalAsync(cancellationToken);
        if (configuracion is null)
        {
            return Resultado<CuentaFinancieraDto>.Fallo(NoConfiguracion());
        }

        await using var unidad = await repositorio.IniciarTransaccionAsync(cancellationToken);
        var cuenta = await unidad.BloquearCuentaAsync(configuracion.Id, id, cancellationToken);
        if (cuenta is null)
        {
            return Resultado<CuentaFinancieraDto>.Fallo(NoEncontrada());
        }

        if (!activa && (await unidad.TieneMetodoPagoActivoAsync(cuenta.Id, cancellationToken) ||
                        await unidad.TieneCajaAbiertaAsync(cuenta.Id, cancellationToken)))
        {
            return Resultado<CuentaFinancieraDto>.Fallo(Conflicto(
                "No se puede desactivar una cuenta con un método de pago activo o una caja abierta."));
        }

        cuenta.EstablecerEstado(activa);
        var saldo = await unidad.CalcularSaldoAsync(cuenta.Id, cancellationToken);
        if (!await unidad.GuardarCambiosAsync(cancellationToken))
        {
            return Resultado<CuentaFinancieraDto>.Fallo(Conflicto());
        }

        await unidad.ConfirmarAsync(cancellationToken);
        return Resultado<CuentaFinancieraDto>.Exito(Convertir(cuenta, saldo));
    }

    public async Task<Resultado<PaginaMovimientosFinancierosDto>> ListarMovimientosAsync(
        Guid cuentaId,
        DateTimeOffset? desdeUtc,
        DateTimeOffset? hastaUtc,
        string? tipo,
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken)
    {
        if (!ValidarPaginacion(pagina, tamanoPagina) ||
            cuentaId == Guid.Empty || desdeUtc.HasValue && hastaUtc.HasValue && desdeUtc > hastaUtc ||
            tipo is { Length: > 50 })
        {
            return Resultado<PaginaMovimientosFinancierosDto>.Fallo(ErrorBusqueda());
        }

        var configuracion = await repositorio.ObtenerConfiguracionPrincipalAsync(cancellationToken);
        var cuenta = configuracion is null
            ? null
            : await repositorio.ObtenerCuentaAsync(configuracion.Id, cuentaId, cancellationToken);
        if (cuenta is null)
        {
            return Resultado<PaginaMovimientosFinancierosDto>.Fallo(NoEncontrada());
        }

        var desde = desdeUtc?.ToUniversalTime();
        var hasta = hastaUtc?.ToUniversalTime();
        var omitir = CalcularOmitir(pagina, tamanoPagina);
        var movimientos = await repositorio.ListarMovimientosAsync(
            cuentaId, desde, hasta, string.IsNullOrWhiteSpace(tipo) ? null : tipo.Trim(),
            omitir, tamanoPagina, cancellationToken);
        var total = await repositorio.ContarMovimientosAsync(
            cuentaId, desde, hasta, string.IsNullOrWhiteSpace(tipo) ? null : tipo.Trim(), cancellationToken);
        var saldo = movimientos.Count == 0
            ? 0m
            : await repositorio.CalcularSaldoAntesAsync(cuentaId, movimientos[0].FechaUtc, movimientos[0].Id, cancellationToken);
        var elementos = new List<MovimientoFinancieroDto>(movimientos.Count);
        foreach (var movimiento in movimientos)
        {
            saldo = movimiento.Aplicar(saldo);
            elementos.Add(Convertir(movimiento, saldo));
        }

        return Resultado<PaginaMovimientosFinancierosDto>.Exito(new PaginaMovimientosFinancierosDto(
            elementos, pagina, tamanoPagina, total));
    }

    public async Task<Resultado<ResumenFinancieroDto>> ObtenerResumenAsync(CancellationToken cancellationToken)
    {
        var configuracion = await repositorio.ObtenerConfiguracionPrincipalAsync(cancellationToken);
        if (configuracion is null)
        {
            return Resultado<ResumenFinancieroDto>.Fallo(NoConfiguracion());
        }

        var (cuentas, _) = await repositorio.ListarCuentasAsync(
            configuracion.Id, null, true, null, 0, int.MaxValue, cancellationToken);
        var saldos = await repositorio.CalcularSaldosAsync(cuentas.Select(x => x.Id).ToArray(), cancellationToken);
        var disponible = cuentas.Where(x => x.Tipo is TipoCuentaFinanciera.Efectivo or TipoCuentaFinanciera.Banco or
                                            TipoCuentaFinanciera.Billetera or TipoCuentaFinanciera.Otro)
            .Sum(x => saldos.GetValueOrDefault(x.Id));
        var deuda = cuentas.Where(x => x.Tipo == TipoCuentaFinanciera.TarjetaCredito)
            .Sum(x => Math.Max(0m, -saldos.GetValueOrDefault(x.Id)));
        return Resultado<ResumenFinancieroDto>.Exito(new ResumenFinancieroDto(disponible, deuda, cuentas.Count));
    }

    public async Task<Resultado<TransferenciaFinancieraDto>> CrearTransferenciaAsync(
        SolicitudTransferenciaFinanciera solicitud,
        CancellationToken cancellationToken)
    {
        var usuarioId = usuarioActual.ObtenerId();
        if (!usuarioId.HasValue)
        {
            return Resultado<TransferenciaFinancieraDto>.Fallo(new ErrorDominio(
                "autenticacion.usuario_no_disponible", "No se pudo identificar al usuario actual."));
        }

        var configuracion = await repositorio.ObtenerConfiguracionPrincipalAsync(cancellationToken);
        if (configuracion is null)
        {
            return Resultado<TransferenciaFinancieraDto>.Fallo(NoConfiguracion());
        }

        if (solicitud.CuentaOrigenId == Guid.Empty || solicitud.CuentaDestinoId == Guid.Empty ||
            solicitud.CuentaOrigenId == solicitud.CuentaDestinoId)
        {
            return Resultado<TransferenciaFinancieraDto>.Fallo(ErrorTransferencia());
        }

        var origenPrevio = await repositorio.ObtenerCuentaAsync(
            configuracion.Id, solicitud.CuentaOrigenId, cancellationToken);
        var destinoPrevio = await repositorio.ObtenerCuentaAsync(
            configuracion.Id, solicitud.CuentaDestinoId, cancellationToken);
        if (origenPrevio is null || destinoPrevio is null)
        {
            return Resultado<TransferenciaFinancieraDto>.Fallo(NoEncontrada());
        }

        var requiereCaja = origenPrevio.Tipo == TipoCuentaFinanciera.Efectivo ||
                           destinoPrevio.Tipo == TipoCuentaFinanciera.Efectivo;
        await using var unidad = await repositorio.IniciarTransaccionAsync(cancellationToken);
        var caja = requiereCaja ? await unidad.BloquearCajaActualAsync(cancellationToken) : null;

        var cuentas = new Dictionary<Guid, CuentaFinanciera>();
        foreach (var id in new[] { solicitud.CuentaOrigenId, solicitud.CuentaDestinoId }.Order())
        {
            var cuenta = await unidad.BloquearCuentaAsync(configuracion.Id, id, cancellationToken);
            if (cuenta is null)
            {
                return Resultado<TransferenciaFinancieraDto>.Fallo(NoEncontrada());
            }

            cuentas.Add(id, cuenta);
        }

        var origen = cuentas[solicitud.CuentaOrigenId];
        var destino = cuentas[solicitud.CuentaDestinoId];
        var requiereCajaActual = origen.Tipo == TipoCuentaFinanciera.Efectivo ||
                                 destino.Tipo == TipoCuentaFinanciera.Efectivo;
        if (requiereCajaActual && !requiereCaja)
        {
            return Resultado<TransferenciaFinancieraDto>.Fallo(ConflictoTransferencia(
                "La cuenta cambió de tipo durante la operación; vuelva a intentarlo."));
        }

        if (requiereCajaActual && (caja is null || caja.Estado != EstadoCaja.Abierta ||
                                   caja.CuentaFinancieraId != (origen.Tipo == TipoCuentaFinanciera.Efectivo
                                       ? origen.Id : destino.Id)))
        {
            return Resultado<TransferenciaFinancieraDto>.Fallo(ConflictoTransferencia(
                "Las transferencias que incluyen efectivo requieren una caja abierta para esa cuenta."));
        }

        var transferencia = TransferenciaFinanciera.Crear(
            origen.Id, destino.Id, solicitud.Importe, origen.CodigoMoneda,
            solicitud.Concepto, solicitud.Referencia, reloj.GetUtcNow());
        if (!transferencia.EsExitoso)
        {
            return Resultado<TransferenciaFinancieraDto>.Fallo(transferencia.Error!);
        }

        var saldoOrigen = await unidad.CalcularSaldoAsync(origen.Id, cancellationToken);
        var saldoDestino = await unidad.CalcularSaldoAsync(destino.Id, cancellationToken);
        var validacion = transferencia.Valor!.ValidarCuentas(origen, destino, saldoOrigen, saldoDestino);
        if (!validacion.EsExitoso)
        {
            return Resultado<TransferenciaFinancieraDto>.Fallo(validacion.Error!);
        }

        transferencia.Valor.RegistrarCreacion(reloj.GetUtcNow(), usuarioId.Value);
        var cajaOrigen = origen.Tipo == TipoCuentaFinanciera.Efectivo ? caja!.Id : (Guid?)null;
        var cajaDestino = destino.Tipo == TipoCuentaFinanciera.Efectivo ? caja!.Id : (Guid?)null;
        var salida = MovimientoFinanciero.Crear(
            origen.Id, TipoMovimientoFinanciero.TransferenciaSalida, SentidoMovimientoFinanciero.Salida,
            solicitud.Importe, origen.CodigoMoneda, transferencia.Valor.FechaUtc,
            solicitud.Concepto, cajaId: cajaOrigen, transferenciaFinancieraId: transferencia.Valor.Id);
        var entrada = MovimientoFinanciero.Crear(
            destino.Id, TipoMovimientoFinanciero.TransferenciaEntrada, SentidoMovimientoFinanciero.Entrada,
            solicitud.Importe, destino.CodigoMoneda, transferencia.Valor.FechaUtc,
            solicitud.Concepto, cajaId: cajaDestino, transferenciaFinancieraId: transferencia.Valor.Id);
        if (!salida.EsExitoso || !entrada.EsExitoso)
        {
            return Resultado<TransferenciaFinancieraDto>.Fallo(new ErrorDominio(
                "transferencia_financiera.datos_invalidos", "No se pudieron generar los movimientos de la transferencia."));
        }

        salida.Valor!.RegistrarCreacion(transferencia.Valor.FechaUtc, usuarioId.Value);
        entrada.Valor!.RegistrarCreacion(transferencia.Valor.FechaUtc, usuarioId.Value);
        unidad.Agregar(transferencia.Valor);
        unidad.Agregar(salida.Valor);
        unidad.Agregar(entrada.Valor);
        if (!await unidad.GuardarCambiosAsync(cancellationToken))
        {
            return Resultado<TransferenciaFinancieraDto>.Fallo(ConflictoTransferencia(
                "La transferencia entró en conflicto con otra operación; vuelva a intentarlo."));
        }

        await unidad.ConfirmarAsync(cancellationToken);
        return Resultado<TransferenciaFinancieraDto>.Exito(Convertir(
            transferencia.Valor, origen.Nombre, destino.Nombre));
    }

    public async Task<Resultado<PaginaTransferenciasFinancierasDto>> ListarTransferenciasAsync(
        DateTimeOffset? desdeUtc,
        DateTimeOffset? hastaUtc,
        Guid? cuentaId,
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken)
    {
        if (!ValidarPaginacion(pagina, tamanoPagina) ||
            desdeUtc.HasValue && hastaUtc.HasValue && desdeUtc > hastaUtc ||
            cuentaId == Guid.Empty)
        {
            return Resultado<PaginaTransferenciasFinancierasDto>.Fallo(ErrorBusqueda());
        }

        var configuracion = await repositorio.ObtenerConfiguracionPrincipalAsync(cancellationToken);
        if (configuracion is null)
        {
            return Resultado<PaginaTransferenciasFinancierasDto>.Fallo(NoConfiguracion());
        }

        var (elementos, total) = await repositorio.ListarTransferenciasAsync(
            configuracion.Id, desdeUtc?.ToUniversalTime(), hastaUtc?.ToUniversalTime(), cuentaId,
            CalcularOmitir(pagina, tamanoPagina), tamanoPagina, cancellationToken);
        return Resultado<PaginaTransferenciasFinancierasDto>.Exito(new PaginaTransferenciasFinancierasDto(
            elementos, pagina, tamanoPagina, total));
    }

    private static CuentaFinancieraDto Convertir(CuentaFinanciera cuenta, decimal saldo) => new(
        cuenta.Id, cuenta.Nombre, cuenta.Tipo.ToString(), cuenta.CodigoMoneda, cuenta.Entidad,
        cuenta.UltimosDigitos, cuenta.CupoCredito, saldo, cuenta.Activa,
        cuenta.FechaCreacionUtc, cuenta.FechaModificacionUtc);

    private static MovimientoFinancieroDto Convertir(MovimientoFinanciero movimiento, decimal saldoPosterior) => new(
        movimiento.Id, movimiento.Tipo, movimiento.Sentido.ToString(), movimiento.Importe,
        movimiento.CodigoMoneda, movimiento.Concepto, movimiento.FechaUtc, movimiento.CajaId,
        movimiento.MetodoPagoId, movimiento.VentaId, movimiento.DevolucionVentaId,
        movimiento.LiquidacionComisionId, movimiento.TransferenciaFinancieraId,
        movimiento.CreadoPorId, movimiento.Migrado, saldoPosterior, movimiento.CompraId);

    private static TransferenciaFinancieraDto Convertir(
        TransferenciaFinanciera transferencia,
        string cuentaOrigenNombre,
        string cuentaDestinoNombre) => new(
        transferencia.Id, transferencia.CuentaOrigenId, cuentaOrigenNombre,
        transferencia.CuentaDestinoId, cuentaDestinoNombre, transferencia.Importe,
        transferencia.CodigoMoneda, transferencia.Concepto, transferencia.Referencia,
        transferencia.FechaUtc, transferencia.CreadoPorId);

    private static bool TryTipoCuenta(string? valor, out TipoCuentaFinanciera tipo)
    {
        if (Enum.TryParse(valor, true, out tipo) && Enum.IsDefined(tipo))
        {
            return true;
        }

        tipo = default;
        return false;
    }

    private static TipoCuentaFinanciera ParsearTipoCuenta(string valor)
    {
        _ = TryTipoCuenta(valor, out var tipo);
        return tipo;
    }

    private static string? Normalizar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim().ToUpperInvariant();

    private static bool ValidarPaginacion(int pagina, int tamanoPagina) =>
        pagina >= 1 && tamanoPagina is >= 1 and <= 100 && (pagina - 1L) * tamanoPagina <= int.MaxValue;

    private static int CalcularOmitir(int pagina, int tamanoPagina) => (pagina - 1) * tamanoPagina;

    private static ErrorDominio ErrorBusqueda() => new(
        "finanzas.busqueda_invalida", "Los filtros o la paginación no son válidos.");

    private static ErrorDominio NoConfiguracion() => new(
        "configuracion.no_configurada", "Primero debe registrar la configuración del establecimiento.");

    private static ErrorDominio NoEncontrada() => new(
        "cuenta_financiera.no_encontrada", "No se encontró la cuenta financiera solicitada.");

    private static ErrorDominio NoMetodoPago() => new(
        "metodo_pago.no_encontrado", "No se encontró un método de pago activo para este movimiento.");

    private static ErrorDominio Conflicto(string mensaje = "La cuenta cambió en otra operación o ya existe con esos datos.") =>
        new("cuenta_financiera.conflicto", mensaje);

    private static ErrorDominio ConflictoTransferencia(string mensaje) =>
        new("transferencia_financiera.conflicto", mensaje);

    private static ErrorDominio ErrorTransferencia() => new(
        "transferencia_financiera.datos_invalidos", "La transferencia requiere cuentas distintas e importe válido.");

    private static ErrorDominio ErrorSolicitud() => new(
        "cuenta_financiera.solicitud_invalida", "Los datos de la cuenta financiera no son válidos.");

    private static ErrorDominio ErrorMovimiento() => new(
        "movimiento_financiero.datos_invalidos", "El sentido, importe, método o concepto del movimiento no son válidos.");

    private static ErrorDominio ErrorUsuario() => new(
        "autenticacion.usuario_no_disponible", "No se pudo identificar al usuario actual.");
}
