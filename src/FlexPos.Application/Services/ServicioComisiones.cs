using FlexPos.Application.DTOs.Comisiones;
using FlexPos.Application.Interfaces;
using FlexPos.Domain.Entities;
using FlexPos.Domain.Enums;
using FlexPos.Domain.Errors;

namespace FlexPos.Application.Services;

public sealed class ServicioComisiones(
    IRepositorioComisiones repositorio,
    IUsuarioActual usuarioActual,
    TimeProvider reloj)
{
    public async Task<Resultado<PaginaReglasComisionDto>> ListarReglasAsync(
        Guid? empleadoId,
        Guid? servicioId,
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken)
    {
        if (!ValidarBusqueda(empleadoId, servicioId, pagina, tamanoPagina))
        {
            return Resultado<PaginaReglasComisionDto>.Fallo(ErrorBusqueda());
        }

        var (elementos, total) = await repositorio.ListarReglasAsync(
            empleadoId, servicioId, Omitir(pagina, tamanoPagina), tamanoPagina, cancellationToken);
        return Resultado<PaginaReglasComisionDto>.Exito(new PaginaReglasComisionDto(
            elementos.Select(Convertir).ToArray(), pagina, tamanoPagina, total));
    }

    public async Task<Resultado<ReglaComisionDto>> CrearReglaAsync(
        SolicitudCrearReglaComision solicitud,
        CancellationToken cancellationToken)
    {
        if (!usuarioActual.ObtenerId().HasValue || solicitud.EmpleadoId == Guid.Empty ||
            solicitud.ServicioId == Guid.Empty || !TryTipo(solicitud.Tipo, out var tipo))
        {
            return Resultado<ReglaComisionDto>.Fallo(ErrorSolicitud());
        }

        await using var unidad = await repositorio.IniciarTransaccionAsync(cancellationToken);
        var empleado = await unidad.ObtenerEmpleadoAsync(solicitud.EmpleadoId, cancellationToken);
        var servicio = await unidad.ObtenerServicioAsync(solicitud.ServicioId, cancellationToken);
        if (empleado is null || !empleado.Activo || servicio is null || !servicio.Activo)
        {
            return Resultado<ReglaComisionDto>.Fallo(new ErrorDominio(
                "comision.referencia_inactiva", "El empleado o servicio no existe o está inactivo."));
        }

        var existente = await unidad.BloquearReglaEmpleadoServicioAsync(
            solicitud.EmpleadoId, solicitud.ServicioId, cancellationToken);
        if (existente is not null)
        {
            return Resultado<ReglaComisionDto>.Fallo(new ErrorDominio(
                "comision.regla_existente", "Ya existe una regla para este empleado y servicio; actualícela o actívela."));
        }

        var regla = ReglaComision.Crear(solicitud.EmpleadoId, solicitud.ServicioId, tipo, solicitud.Valor);
        if (!regla.EsExitoso)
        {
            return Resultado<ReglaComisionDto>.Fallo(regla.Error!);
        }

        unidad.Agregar(regla.Valor!);
        if (!await unidad.GuardarCambiosAsync(cancellationToken))
        {
            return Resultado<ReglaComisionDto>.Fallo(Conflicto("La regla cambió en otra operación."));
        }

        await unidad.ConfirmarAsync(cancellationToken);
        return Resultado<ReglaComisionDto>.Exito(Convertir(regla.Valor!));
    }

    public async Task<Resultado<ReglaComisionDto>> ActualizarReglaAsync(
        Guid id,
        SolicitudActualizarReglaComision solicitud,
        CancellationToken cancellationToken)
    {
        if (!usuarioActual.ObtenerId().HasValue || id == Guid.Empty || !TryTipo(solicitud.Tipo, out var tipo))
        {
            return Resultado<ReglaComisionDto>.Fallo(ErrorSolicitud());
        }

        await using var unidad = await repositorio.IniciarTransaccionAsync(cancellationToken);
        var regla = await unidad.BloquearReglaAsync(id, cancellationToken);
        if (regla is null)
        {
            return Resultado<ReglaComisionDto>.Fallo(NoEncontrada());
        }

        var resultado = regla.Actualizar(tipo, solicitud.Valor);
        if (!resultado.EsExitoso)
        {
            return Resultado<ReglaComisionDto>.Fallo(resultado.Error!);
        }

        if (!await unidad.GuardarCambiosAsync(cancellationToken))
        {
            return Resultado<ReglaComisionDto>.Fallo(Conflicto("La regla cambió en otra operación."));
        }

        await unidad.ConfirmarAsync(cancellationToken);
        return Resultado<ReglaComisionDto>.Exito(Convertir(regla));
    }

    public async Task<Resultado<ReglaComisionDto>> EstablecerEstadoReglaAsync(
        Guid id,
        bool activa,
        CancellationToken cancellationToken)
    {
        if (!usuarioActual.ObtenerId().HasValue || id == Guid.Empty)
        {
            return Resultado<ReglaComisionDto>.Fallo(ErrorSolicitud());
        }

        await using var unidad = await repositorio.IniciarTransaccionAsync(cancellationToken);
        var regla = await unidad.BloquearReglaAsync(id, cancellationToken);
        if (regla is null)
        {
            return Resultado<ReglaComisionDto>.Fallo(NoEncontrada());
        }

        regla.EstablecerEstado(activa);
        if (!await unidad.GuardarCambiosAsync(cancellationToken))
        {
            return Resultado<ReglaComisionDto>.Fallo(Conflicto("La regla cambió en otra operación."));
        }

        await unidad.ConfirmarAsync(cancellationToken);
        return Resultado<ReglaComisionDto>.Exito(Convertir(regla));
    }

    public async Task<Resultado<PaginaComisionesDto>> ListarAsync(
        Guid? empleadoId,
        string? estadoTexto,
        string? tipoMovimientoTexto,
        DateTimeOffset? desdeUtc,
        DateTimeOffset? hastaUtc,
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken)
    {
        if (!ValidarBusqueda(empleadoId, null, pagina, tamanoPagina) ||
            desdeUtc.HasValue && hastaUtc.HasValue && desdeUtc > hastaUtc ||
            !TryFiltro(estadoTexto, out EstadoComision? estado) ||
            !TryFiltro(tipoMovimientoTexto, out TipoMovimientoComision? tipoMovimiento))
        {
            return Resultado<PaginaComisionesDto>.Fallo(ErrorBusqueda());
        }

        var (elementos, total) = await repositorio.ListarComisionesAsync(
            empleadoId, estado, tipoMovimiento, desdeUtc?.ToUniversalTime(), hastaUtc?.ToUniversalTime(),
            Omitir(pagina, tamanoPagina), tamanoPagina, cancellationToken);
        return Resultado<PaginaComisionesDto>.Exito(new PaginaComisionesDto(
            elementos.Select(Convertir).ToArray(), pagina, tamanoPagina, total));
    }

    public async Task<Resultado<PaginaLiquidacionesComisionDto>> ListarLiquidacionesAsync(
        Guid? empleadoId,
        DateTimeOffset? desdeUtc,
        DateTimeOffset? hastaUtc,
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken)
    {
        if (!ValidarBusqueda(empleadoId, null, pagina, tamanoPagina) ||
            desdeUtc.HasValue && hastaUtc.HasValue && desdeUtc > hastaUtc)
        {
            return Resultado<PaginaLiquidacionesComisionDto>.Fallo(ErrorBusqueda());
        }

        var (elementos, total) = await repositorio.ListarLiquidacionesAsync(
            empleadoId, desdeUtc?.ToUniversalTime(), hastaUtc?.ToUniversalTime(),
            Omitir(pagina, tamanoPagina), tamanoPagina, cancellationToken);
        var idsLiquidacion = elementos.Select(x => x.Id).ToArray();
        var detallePorId = (await repositorio.ObtenerMovimientosLiquidacionesAsync(idsLiquidacion, cancellationToken))
            .GroupBy(x => x.LiquidacionComisionId!.Value)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<Guid>)x.Select(comision => comision.Id).ToArray());

        return Resultado<PaginaLiquidacionesComisionDto>.Exito(new PaginaLiquidacionesComisionDto(
            elementos.Select(x => Convertir(x, detallePorId.GetValueOrDefault(x.Id, Array.Empty<Guid>()))).ToArray(),
            pagina, tamanoPagina, total));
    }

    public async Task<Resultado<LiquidacionComisionDto>> ObtenerLiquidacionAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var liquidacion = id == Guid.Empty ? null : await repositorio.ObtenerLiquidacionAsync(id, cancellationToken);
        if (liquidacion is null)
        {
            return Resultado<LiquidacionComisionDto>.Fallo(NoEncontradaLiquidacion());
        }

        var movimientos = await repositorio.ObtenerMovimientosLiquidacionAsync(id, cancellationToken);
        return Resultado<LiquidacionComisionDto>.Exito(Convertir(liquidacion, movimientos.Select(x => x.Id).ToArray()));
    }

    public async Task<Resultado<LiquidacionComisionDto>> LiquidarAsync(
        SolicitudLiquidarComisiones solicitud,
        CancellationToken cancellationToken)
    {
        if (!usuarioActual.ObtenerId().HasValue || solicitud.ComisionIds is null ||
            solicitud.ComisionIds.Count is < 1 or > 200 || solicitud.ComisionIds.Any(x => x == Guid.Empty) ||
            solicitud.ComisionIds.Distinct().Count() != solicitud.ComisionIds.Count || solicitud.MetodoPagoId == Guid.Empty)
        {
            return Resultado<LiquidacionComisionDto>.Fallo(ErrorSolicitud());
        }

        await using var unidad = await repositorio.IniciarTransaccionAsync(cancellationToken);
        var configuracion = await unidad.ObtenerConfiguracionPrincipalAsync(cancellationToken);
        var metodo = await unidad.ObtenerMetodoPagoAsync(solicitud.MetodoPagoId, cancellationToken);
        if (configuracion is null)
        {
            return Resultado<LiquidacionComisionDto>.Fallo(new ErrorDominio(
                "configuracion.no_configurada", "Primero debe registrar la configuración del establecimiento."));
        }

        if (metodo is null || !metodo.Activo || metodo.ConfiguracionNegocioId != configuracion.Id)
        {
            return Resultado<LiquidacionComisionDto>.Fallo(new ErrorDominio(
                "comision.metodo_pago_inactivo", "El método de pago no existe o no está activo para este negocio."));
        }

        Caja? caja = null;
        if (metodo.EsEfectivo)
        {
            caja = await unidad.BloquearCajaActualAsync(cancellationToken);
            if (caja is null)
            {
                return Resultado<LiquidacionComisionDto>.Fallo(new ErrorDominio(
                    "caja.no_abierta", "Debe haber una caja abierta para pagar comisiones en efectivo."));
            }
        }

        var seleccionadas = await unidad.BloquearComisionesSeleccionadasAsync(
            solicitud.ComisionIds, cancellationToken);
        if (seleccionadas.Count != solicitud.ComisionIds.Count ||
            seleccionadas.Any(x => x.Estado != EstadoComision.Pendiente ||
                                   x.TipoMovimiento != TipoMovimientoComision.DevengoServicio) ||
            seleccionadas.Select(x => x.EmpleadoId).Distinct().Count() != 1)
        {
            return Resultado<LiquidacionComisionDto>.Fallo(new ErrorDominio(
                "comision.seleccion_invalida", "Seleccione comisiones de servicio pendientes de un solo empleado."));
        }

        var empleadoId = seleccionadas[0].EmpleadoId;
        var ajustesPendientes = await unidad.BloquearAjustesPendientesAsync(empleadoId, cancellationToken);
        var idsSeleccionados = seleccionadas.Select(x => x.Id).ToHashSet();
        var ajustes = ajustesPendientes.Where(x =>
                x.ComisionOriginalId.HasValue &&
                (idsSeleccionados.Contains(x.ComisionOriginalId.Value) ||
                 x.ComisionOriginal?.Estado == EstadoComision.Pagada))
            .ToArray();
        var importeBruto = seleccionadas.Sum(x => x.Importe);
        var importeAjustes = ajustes.Sum(x => x.Importe);
        var importeNeto = Redondear(importeBruto - importeAjustes);
        if (importeNeto <= 0m)
        {
            return Resultado<LiquidacionComisionDto>.Fallo(new ErrorDominio(
                "comision.saldo_no_pagable", "Los ajustes pendientes igualan o superan las comisiones seleccionadas."));
        }

        var liquidacion = LiquidacionComision.Crear(
            empleadoId, metodo, importeNeto, configuracion.CodigoMoneda, solicitud.Referencia,
            caja?.Id, reloj.GetUtcNow());
        if (!liquidacion.EsExitoso)
        {
            return Resultado<LiquidacionComisionDto>.Fallo(liquidacion.Error!);
        }

        unidad.Agregar(liquidacion.Valor!);
        foreach (var comision in seleccionadas.Concat(ajustes))
        {
            comision.MarcarPagada(liquidacion.Valor!.Id);
        }

        if (metodo.EsEfectivo)
        {
            var movimiento = MovimientoCaja.Crear(
                caja!.Id, TipoMovimientoCaja.PagoComision, importeNeto, configuracion.CodigoMoneda,
                "Liquidación de comisiones", liquidacionComisionId: liquidacion.Valor!.Id);
            if (!movimiento.EsExitoso)
            {
                return Resultado<LiquidacionComisionDto>.Fallo(movimiento.Error!);
            }

            unidad.Agregar(movimiento.Valor!);
        }

        if (!await unidad.GuardarCambiosAsync(cancellationToken))
        {
            return Resultado<LiquidacionComisionDto>.Fallo(Conflicto("Las comisiones cambiaron en otra operación."));
        }

        await unidad.ConfirmarAsync(cancellationToken);
        var movimientosLiquidados = seleccionadas.Concat(ajustes).ToArray();
        return Resultado<LiquidacionComisionDto>.Exito(Convertir(
            liquidacion.Valor!, movimientosLiquidados.Select(x => x.Id).ToArray()));
    }

    private static bool ValidarBusqueda(Guid? empleadoId, Guid? servicioId, int pagina, int tamanoPagina) =>
        empleadoId != Guid.Empty && servicioId != Guid.Empty && pagina >= 1 &&
        tamanoPagina is >= 1 and <= 100 && (pagina - 1L) * tamanoPagina <= int.MaxValue;

    private static int Omitir(int pagina, int tamanoPagina) => (pagina - 1) * tamanoPagina;
    private static decimal Redondear(decimal valor) => decimal.Round(valor, 2, MidpointRounding.AwayFromZero);

    private static bool TryTipo(string? texto, out TipoTarifaComision tipo) =>
        Enum.TryParse(texto, true, out tipo) && Enum.IsDefined(tipo);

    private static bool TryFiltro<T>(string? texto, out T? valor) where T : struct, Enum
    {
        valor = null;
        if (texto is null)
        {
            return true;
        }

        if (!Enum.TryParse<T>(texto, true, out var parseado) || !Enum.IsDefined(parseado))
        {
            return false;
        }

        valor = parseado;
        return true;
    }

    private static ReglaComisionDto Convertir(ReglaComision regla) => new(
        regla.Id, regla.EmpleadoId, regla.ServicioId, regla.Tipo.ToString(), regla.Valor,
        regla.Activa, regla.FechaCreacionUtc, regla.FechaModificacionUtc);

    private static ComisionDto Convertir(Comision comision) => new(
        comision.Id, comision.EmpleadoId, comision.ServicioId, comision.VentaId, comision.DetalleVentaId,
        comision.TipoMovimiento.ToString(), comision.TipoTarifa.ToString(), comision.ValorTarifa,
        comision.BaseCalculo, comision.Cantidad, comision.Importe, comision.CodigoMoneda, comision.Estado.ToString(),
        comision.ComisionOriginalId, comision.DevolucionVentaId, comision.LiquidacionComisionId,
        comision.FechaCreacionUtc);

    private static LiquidacionComisionDto Convertir(LiquidacionComision liquidacion, IReadOnlyList<Guid> comisionIds) => new(
        liquidacion.Id, liquidacion.EmpleadoId, liquidacion.MetodoPagoId, liquidacion.MetodoPagoNombre,
        liquidacion.EsEfectivo, liquidacion.Importe, liquidacion.CodigoMoneda, liquidacion.Referencia,
        liquidacion.CajaId, liquidacion.FechaUtc, comisionIds);

    private static ErrorDominio ErrorBusqueda() => new(
        "comision.busqueda_invalida", "Los filtros de comisión o la paginación no son válidos.");

    private static ErrorDominio ErrorSolicitud() => new(
        "comision.solicitud_invalida", "Los datos de la solicitud de comisión no son válidos.");

    private static ErrorDominio NoEncontrada() => new(
        "comision.regla_no_encontrada", "No se encontró la regla de comisión solicitada.");

    private static ErrorDominio NoEncontradaLiquidacion() => new(
        "comision.liquidacion_no_encontrada", "No se encontró la liquidación solicitada.");

    private static ErrorDominio Conflicto(string mensaje) => new("comision.conflicto", mensaje);
}
