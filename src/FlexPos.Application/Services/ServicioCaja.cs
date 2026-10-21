using FlexPos.Application.DTOs.Caja;
using FlexPos.Application.Interfaces;
using FlexPos.Domain.Entities;
using FlexPos.Domain.Enums;
using FlexPos.Domain.Errors;

namespace FlexPos.Application.Services;

public sealed class ServicioCaja(
    IRepositorioPuntoVenta repositorio,
    IUsuarioActual usuarioActual,
    TimeProvider reloj)
{
    public async Task<Resultado<CajaDto?>> ObtenerActualAsync(CancellationToken cancellationToken)
    {
        var caja = await repositorio.ObtenerCajaActualAsync(cancellationToken);
        if (caja is null)
        {
            return Resultado<CajaDto?>.Exito(null);
         }

        await using var unidad = await repositorio.IniciarTransaccionAsync(cancellationToken);
        var efectivo = await unidad.CalcularEfectivoEsperadoAsync(caja.Id, cancellationToken);
        return Resultado<CajaDto?>.Exito(Convertir(caja, efectivo));
    }

    public async Task<Resultado<PaginaCajasDto>> ListarAsync(
        DateTimeOffset? desdeUtc,
        DateTimeOffset? hastaUtc,
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken)
    {
        if (!ValidarPaginacion(pagina, tamanoPagina) ||
            desdeUtc.HasValue && hastaUtc.HasValue && desdeUtc > hastaUtc)
        {
            return Resultado<PaginaCajasDto>.Fallo(ErrorBusqueda());
        }

        var (elementos, total) = await repositorio.ListarCajasAsync(
            desdeUtc?.ToUniversalTime(), hastaUtc?.ToUniversalTime(),
            ValidacionListado.CalcularOmitir(pagina, tamanoPagina), tamanoPagina, cancellationToken);
        decimal? efectivoActual = null;
        var abierta = elementos.SingleOrDefault(x => x.Estado == EstadoCaja.Abierta);
        if (abierta is not null)
        {
            await using var unidad = await repositorio.IniciarTransaccionAsync(cancellationToken);
            efectivoActual = await unidad.CalcularEfectivoEsperadoAsync(abierta.Id, cancellationToken);
        }

        return Resultado<PaginaCajasDto>.Exito(new PaginaCajasDto(
            elementos.Select(x => Convertir(x, x.Estado == EstadoCaja.Abierta ? efectivoActual : null)).ToArray(),
            pagina, tamanoPagina, total));
    }

    public async Task<Resultado<CajaDetalleDto>> ObtenerAsync(Guid id, CancellationToken cancellationToken)
    {
        var caja = id == Guid.Empty ? null : await repositorio.ObtenerCajaAsync(id, cancellationToken);
        if (caja is null)
        {
            return Resultado<CajaDetalleDto>.Fallo(NoEncontrada());
        }

        decimal? efectivo = null;
        if (caja.Estado == EstadoCaja.Abierta)
        {
            await using var unidad = await repositorio.IniciarTransaccionAsync(cancellationToken);
            efectivo = await unidad.CalcularEfectivoEsperadoAsync(caja.Id, cancellationToken);
        }

        var movimientos = await repositorio.ObtenerMovimientosCajaAsync(id, cancellationToken);
        return Resultado<CajaDetalleDto>.Exito(new CajaDetalleDto(
            Convertir(caja, efectivo), movimientos.Select(Convertir).ToArray()));
    }

    public async Task<Resultado<CajaDto>> AbrirAsync(
        SolicitudAbrirCaja solicitud,
        CancellationToken cancellationToken)
    {
        var usuarioId = usuarioActual.ObtenerId();
        if (!usuarioId.HasValue)
        {
            return Resultado<CajaDto>.Fallo(ErrorUsuario());
        }

        await using var unidad = await repositorio.IniciarTransaccionAsync(cancellationToken);
        if (await unidad.BloquearCajaActualAsync(cancellationToken) is not null)
        {
            return Resultado<CajaDto>.Fallo(Conflicto("Ya existe una caja abierta."));
        }

        var configuracion = await unidad.ObtenerConfiguracionPrincipalAsync(cancellationToken);
        if (configuracion is null)
        {
            return Resultado<CajaDto>.Fallo(new ErrorDominio(
                "configuracion.no_configurada", "Primero debe registrar la configuración del establecimiento."));
        }

        var cuenta = await unidad.BloquearCuentaBaseAsync(
            configuracion.Id, TipoCuentaFinanciera.Efectivo, ServicioFinanzas.CuentaEfectivoBase, cancellationToken);
        if (cuenta is null || !cuenta.Activa || cuenta.CodigoMoneda != configuracion.CodigoMoneda)
        {
            return Resultado<CajaDto>.Fallo(Conflicto("No se encontró una cuenta de efectivo activa para abrir caja."));
        }

        var saldoLibro = await unidad.CalcularSaldoCuentaAsync(cuenta.Id, cancellationToken);
        var nueva = Caja.Abrir(cuenta.Id, solicitud.EfectivoInicial, reloj.GetUtcNow(), usuarioId.Value);
        if (!nueva.EsExitoso)
        {
            return Resultado<CajaDto>.Fallo(nueva.Error!);
        }

        unidad.Agregar(nueva.Valor!);
        if (solicitud.EfectivoInicial != saldoLibro)
        {
            var diferencia = solicitud.EfectivoInicial - saldoLibro;
            var ajuste = CrearMovimientoAjuste(
                cuenta.Id, TipoMovimientoFinanciero.AjusteApertura, nueva.Valor!.Id,
                diferencia, configuracion.CodigoMoneda, nueva.Valor.FechaAperturaUtc, usuarioId.Value);
            if (!ajuste.EsExitoso)
            {
                return Resultado<CajaDto>.Fallo(ajuste.Error!);
            }

            unidad.Agregar(ajuste.Valor!);
        }

        if (!await unidad.GuardarCambiosAsync(cancellationToken))
        {
            return Resultado<CajaDto>.Fallo(Conflicto("La caja cambió en otra operación."));
        }

        await unidad.ConfirmarAsync(cancellationToken);
        return Resultado<CajaDto>.Exito(Convertir(nueva.Valor!, nueva.Valor!.EfectivoApertura));
    }

    public async Task<Resultado<CajaDto>> CerrarAsync(
        Guid id,
        SolicitudCerrarCaja solicitud,
        CancellationToken cancellationToken)
    {
        var usuarioId = usuarioActual.ObtenerId();
        if (!usuarioId.HasValue)
        {
            return Resultado<CajaDto>.Fallo(ErrorUsuario());
        }

        await using var unidad = await repositorio.IniciarTransaccionAsync(cancellationToken);
        var caja = await unidad.BloquearCajaAsync(id, cancellationToken);
        if (caja is null)
        {
            return Resultado<CajaDto>.Fallo(NoEncontrada());
        }

        var efectivoEsperado = await unidad.CalcularEfectivoEsperadoAsync(id, cancellationToken);
        var cuenta = await unidad.BloquearCuentaFinancieraAsync(caja.CuentaFinancieraId, cancellationToken);
        if (cuenta is null)
        {
            return Resultado<CajaDto>.Fallo(Conflicto("No se encontró la cuenta financiera asociada a la caja."));
        }

        var cierre = caja.Cerrar(solicitud.EfectivoContado, efectivoEsperado, reloj.GetUtcNow(), usuarioId.Value);
        if (!cierre.EsExitoso)
        {
            return Resultado<CajaDto>.Fallo(cierre.Error!);
        }

        if (caja.DiferenciaCierre is decimal diferencia && diferencia != 0m)
        {
            var ajuste = CrearMovimientoAjuste(
                cuenta.Id, TipoMovimientoFinanciero.AjusteCierre, caja.Id, diferencia,
                cuenta.CodigoMoneda, caja.FechaCierreUtc!.Value, usuarioId.Value);
            if (!ajuste.EsExitoso)
            {
                return Resultado<CajaDto>.Fallo(ajuste.Error!);
            }

            unidad.Agregar(ajuste.Valor!);
        }

        if (!await unidad.GuardarCambiosAsync(cancellationToken))
        {
            return Resultado<CajaDto>.Fallo(Conflicto("La caja cambió en otra operación."));
        }

        await unidad.ConfirmarAsync(cancellationToken);
        return Resultado<CajaDto>.Exito(Convertir(caja, caja.EfectivoEsperadoCierre));
    }

    public async Task<Resultado<MovimientoCajaDto>> RegistrarMovimientoAsync(
        Guid id,
        SolicitudMovimientoCaja solicitud,
        CancellationToken cancellationToken)
    {
        var usuarioId = usuarioActual.ObtenerId();
        if (!usuarioId.HasValue)
        {
            return Resultado<MovimientoCajaDto>.Fallo(ErrorUsuario());
        }

        if (!Enum.TryParse<TipoMovimientoCaja>(solicitud.Tipo, true, out var tipo) ||
            tipo is not (TipoMovimientoCaja.IngresoManual or TipoMovimientoCaja.EgresoManual))
        {
            return Resultado<MovimientoCajaDto>.Fallo(new ErrorDominio(
                "caja.tipo_movimiento_invalido", "El tipo manual debe ser IngresoManual o EgresoManual."));
        }

        await using var unidad = await repositorio.IniciarTransaccionAsync(cancellationToken);
        var caja = await unidad.BloquearCajaAsync(id, cancellationToken);
        if (caja is null)
        {
            return Resultado<MovimientoCajaDto>.Fallo(NoEncontrada());
        }

        if (caja.Estado != EstadoCaja.Abierta)
        {
            return Resultado<MovimientoCajaDto>.Fallo(Conflicto("No se pueden registrar movimientos en una caja cerrada."));
        }

        var configuracion = await unidad.ObtenerConfiguracionPrincipalAsync(cancellationToken);
        if (configuracion is null)
        {
            return Resultado<MovimientoCajaDto>.Fallo(new ErrorDominio(
                "configuracion.no_configurada", "Primero debe registrar la configuración del establecimiento."));
        }

        var cuenta = await unidad.BloquearCuentaFinancieraAsync(caja.CuentaFinancieraId, cancellationToken);
        if (cuenta is null || !cuenta.Activa)
        {
            return Resultado<MovimientoCajaDto>.Fallo(Conflicto("La cuenta de efectivo no está disponible."));
        }

        var sentido = tipo == TipoMovimientoCaja.IngresoManual
            ? SentidoMovimientoFinanciero.Entrada
            : SentidoMovimientoFinanciero.Salida;
        var saldo = await unidad.CalcularSaldoCuentaAsync(cuenta.Id, cancellationToken);
        if (sentido == SentidoMovimientoFinanciero.Salida &&
            !cuenta.ValidarSaldo(saldo - solicitud.Importe).EsExitoso)
        {
            return Resultado<MovimientoCajaDto>.Fallo(Conflicto("La cuenta no tiene saldo suficiente para este egreso."));
        }

        var movimiento = MovimientoFinanciero.Crear(
            cuenta.Id, tipo.ToString(), sentido, solicitud.Importe, configuracion.CodigoMoneda,
            reloj.GetUtcNow(), solicitud.Concepto, cajaId: id);
        if (!movimiento.EsExitoso)
        {
            return Resultado<MovimientoCajaDto>.Fallo(movimiento.Error!);
        }

        movimiento.Valor!.RegistrarCreacion(reloj.GetUtcNow(), usuarioId.Value);
        unidad.Agregar(movimiento.Valor!);
        if (!await unidad.GuardarCambiosAsync(cancellationToken))
        {
            return Resultado<MovimientoCajaDto>.Fallo(Conflicto("La caja cambió en otra operación."));
        }

        await unidad.ConfirmarAsync(cancellationToken);
        return Resultado<MovimientoCajaDto>.Exito(Convertir(movimiento.Valor!));
    }

    private static CajaDto Convertir(Caja caja, decimal? efectivoActual) => new(
        caja.Id,
        caja.Estado.ToString(),
        caja.EfectivoApertura,
        caja.FechaAperturaUtc,
        caja.UsuarioAperturaId,
        caja.Estado == EstadoCaja.Abierta ? efectivoActual : caja.EfectivoEsperadoCierre,
        caja.EfectivoContadoCierre,
        caja.DiferenciaCierre,
        caja.FechaCierreUtc,
        caja.UsuarioCierreId);

    private static MovimientoCajaDto Convertir(MovimientoFinanciero movimiento) => new(
        movimiento.Id,
        movimiento.Tipo,
        movimiento.Importe,
        movimiento.CodigoMoneda,
        movimiento.Concepto,
        movimiento.VentaId,
        movimiento.DevolucionVentaId,
        movimiento.LiquidacionComisionId,
        movimiento.FechaUtc,
        movimiento.CreadoPorId);

    private static FlexPos.Domain.Errors.Resultado<MovimientoFinanciero> CrearMovimientoAjuste(
        Guid cuentaId,
        string tipo,
        Guid cajaId,
        decimal diferencia,
        string codigoMoneda,
        DateTimeOffset fechaUtc,
        Guid usuarioId)
    {
        var movimiento = MovimientoFinanciero.Crear(
            cuentaId, tipo,
            diferencia >= 0m ? SentidoMovimientoFinanciero.Entrada : SentidoMovimientoFinanciero.Salida,
            Math.Abs(diferencia), codigoMoneda, fechaUtc, tipo, cajaId: cajaId);
        if (movimiento.EsExitoso)
        {
            movimiento.Valor!.RegistrarCreacion(fechaUtc, usuarioId);
        }

        return movimiento;
    }

    private static bool ValidarPaginacion(int pagina, int tamanoPagina) =>
        pagina >= 1 && tamanoPagina is >= 1 and <= 100 && (pagina - 1L) * tamanoPagina <= int.MaxValue;

    private static ErrorDominio ErrorBusqueda() => new(
        "caja.busqueda_invalida", "Los filtros de fecha o la paginación no son válidos.");

    private static ErrorDominio NoEncontrada() => new(
        "caja.no_encontrada", "No se encontró la caja solicitada.");

    private static ErrorDominio ErrorUsuario() => new(
        "usuario.no_autenticado", "No se pudo identificar al usuario de la operación.");

    private static ErrorDominio Conflicto(string mensaje) => new("caja.conflicto", mensaje);
}
