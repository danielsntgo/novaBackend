using FlexPos.Application.DTOs.Citas;
using FlexPos.Application.Interfaces;
using FlexPos.Domain.Entities;
using FlexPos.Domain.Enums;
using FlexPos.Domain.Errors;

namespace FlexPos.Application.Services;

public sealed class ServicioCitas(
    IRepositorioCitas citas,
    IRepositorioClientes clientes,
    IRepositorioEmpleados empleados,
    IRepositorioServicios servicios)
{
    public async Task<Resultado<PaginaCitasDto>> ListarAsync(
        DateTimeOffset? desdeUtc,
        DateTimeOffset? hastaUtc,
        Guid? empleadoId,
        Guid? clienteId,
        string? estadoTexto,
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken)
    {
        if (!ValidacionListado.EsValida(null, pagina, tamanoPagina) ||
            desdeUtc.HasValue && hastaUtc.HasValue && hastaUtc <= desdeUtc ||
            empleadoId == Guid.Empty || clienteId == Guid.Empty)
        {
            return Resultado<PaginaCitasDto>.Fallo(new ErrorDominio(
                "cita.busqueda_invalida", "Los filtros o la paginación no son válidos."));
        }

        var estado = ParsearEstado(estadoTexto);
        if (estadoTexto is not null && estado is null)
        {
            return Resultado<PaginaCitasDto>.Fallo(new ErrorDominio(
                "cita.estado_invalido", "El estado de la cita no es válido."));
        }

        var resultado = await citas.ListarAsync(
            desdeUtc?.ToUniversalTime(), hastaUtc?.ToUniversalTime(), empleadoId, clienteId,
            estado, ValidacionListado.CalcularOmitir(pagina, tamanoPagina), tamanoPagina, cancellationToken);
        return Resultado<PaginaCitasDto>.Exito(new PaginaCitasDto(
            resultado.Elementos.Select(Convertir).ToArray(), pagina, tamanoPagina, resultado.Total));
    }

    public async Task<Resultado<CitaDto>> ObtenerAsync(Guid id, CancellationToken cancellationToken)
    {
        var cita = id == Guid.Empty ? null : await citas.ObtenerAsync(id, cancellationToken);
        return cita is null
            ? Resultado<CitaDto>.Fallo(NoEncontrada())
            : Resultado<CitaDto>.Exito(Convertir(cita));
    }

    public async Task<Resultado<CitaDto>> CrearAsync(
        SolicitudCita solicitud,
        CancellationToken cancellationToken)
    {
        if (solicitud.InicioLocal == default)
        {
            return Resultado<CitaDto>.Fallo(new ErrorDominio(
                "cita.fecha_invalida", "Debe indicar la fecha y hora local de la cita con su desfase UTC."));
        }

        var cliente = await clientes.ObtenerAsync(solicitud.ClienteId, cancellationToken);
        var servicio = await servicios.ObtenerAsync(solicitud.ServicioId, cancellationToken);
        var empleado = await empleados.ObtenerAsync(solicitud.EmpleadoId, cancellationToken);
        if (cliente is null || !cliente.Activo || servicio is null || !servicio.Activo || empleado is null || !empleado.Activo)
        {
            return Resultado<CitaDto>.Fallo(new ErrorDominio(
                "cita.referencia_inactiva", "El cliente, servicio o empleado no existe o está inactivo."));
        }

        var nueva = Cita.Crear(
            cliente.Id, servicio.Id, empleado.Id, solicitud.InicioLocal, servicio.DuracionMinutos);
        if (!nueva.EsExitoso)
        {
            return Resultado<CitaDto>.Fallo(nueva.Error!);
        }

        var error = await citas.GuardarEnAgendaAsync(nueva.Valor!, solicitud.InicioLocal, cancellationToken);
        if (error is not null)
        {
            return Resultado<CitaDto>.Fallo(error);
        }

        return await ObtenerAsync(nueva.Valor!.Id, cancellationToken);
    }

    public async Task<Resultado<CitaDto>> ReprogramarAsync(
        Guid id,
        Guid empleadoId,
        DateTimeOffset inicioLocal,
        CancellationToken cancellationToken)
    {
        if (inicioLocal == default)
        {
            return Resultado<CitaDto>.Fallo(new ErrorDominio(
                "cita.fecha_invalida", "Debe indicar la fecha y hora local con su desfase UTC."));
        }

        var cita = id == Guid.Empty ? null : await citas.ObtenerAsync(id, cancellationToken);
        if (cita is null)
        {
            return Resultado<CitaDto>.Fallo(NoEncontrada());
        }

        var empleado = empleadoId == Guid.Empty
            ? null
            : await empleados.ObtenerAsync(empleadoId, cancellationToken);
        if (empleado is null || !empleado.Activo)
        {
            return Resultado<CitaDto>.Fallo(new ErrorDominio(
                "cita.referencia_inactiva", "El empleado no existe o está inactivo."));
        }

        var reprogramacion = cita.Reprogramar(empleado.Id, inicioLocal);
        if (!reprogramacion.EsExitoso)
        {
            return Resultado<CitaDto>.Fallo(reprogramacion.Error!);
        }

        var error = await citas.GuardarEnAgendaAsync(cita, inicioLocal, cancellationToken);
        return error is not null
            ? Resultado<CitaDto>.Fallo(error)
            : await ObtenerAsync(cita.Id, cancellationToken);
    }

    public async Task<Resultado<CitaDto>> CambiarEstadoAsync(
        Guid id,
        string? estadoTexto,
        CancellationToken cancellationToken)
    {
        var estado = ParsearEstado(estadoTexto);
        if (estado is null)
        {
            return Resultado<CitaDto>.Fallo(new ErrorDominio(
                "cita.estado_invalido", "El estado solicitado no es válido."));
        }

        var cita = id == Guid.Empty ? null : await citas.ObtenerAsync(id, cancellationToken);
        if (cita is null)
        {
            return Resultado<CitaDto>.Fallo(NoEncontrada());
        }

        var cambio = cita.CambiarEstado(estado.Value);
        if (!cambio.EsExitoso)
        {
            return Resultado<CitaDto>.Fallo(cambio.Error!);
        }

        if (!await citas.GuardarCambiosAsync(cancellationToken))
        {
            return Resultado<CitaDto>.Fallo(new ErrorDominio(
                "cita.conflicto", "La cita cambió en otra operación."));
        }

        return await ObtenerAsync(cita.Id, cancellationToken);
    }

    public async Task<Resultado<IReadOnlyList<EmpleadoDisponibilidadDto>>> ConsultarDisponibilidadAsync(
        Guid servicioId,
        DateTimeOffset fechaLocal,
        CancellationToken cancellationToken)
    {
        if (servicioId == Guid.Empty || fechaLocal == default)
        {
            return Resultado<IReadOnlyList<EmpleadoDisponibilidadDto>>.Fallo(new ErrorDominio(
                "cita.busqueda_invalida", "El servicio o la fecha local con desfase UTC no son válidos."));
        }

        var servicio = await servicios.ObtenerAsync(servicioId, cancellationToken);
        if (servicio is null || !servicio.Activo)
        {
            return Resultado<IReadOnlyList<EmpleadoDisponibilidadDto>>.Fallo(new ErrorDominio(
                "cita.servicio_inactivo", "El servicio no existe o está inactivo."));
        }

        var fecha = DateOnly.FromDateTime(fechaLocal.DateTime);
        var desfase = fechaLocal.Offset;
        var inicioDiaLocal = new DateTimeOffset(fecha.ToDateTime(TimeOnly.MinValue), desfase);
        var inicioDiaUtc = inicioDiaLocal.ToUniversalTime();
        var finDiaUtc = inicioDiaLocal.AddDays(1).ToUniversalTime();
        var empleadosActivos = await empleados.ListarActivosConHorarioAsync(cancellationToken);
        var reservas = await citas.ListarReservasAsync(inicioDiaUtc, finDiaUtc, cancellationToken);
        var respuesta = new List<EmpleadoDisponibilidadDto>();

        foreach (var empleado in empleadosActivos)
        {
            var periodos = new List<PeriodoDisponibleDto>();
            var reservasEmpleado = reservas
                .Where(cita => cita.EmpleadoId == empleado.Id)
                .OrderBy(cita => cita.InicioUtc)
                .ToArray();

            foreach (var horario in empleado.HorariosSemanales.Where(horario => horario.DiaSemana == fecha.DayOfWeek))
            {
                var inicioTrabajo = new DateTimeOffset(fecha.ToDateTime(horario.HoraInicio), desfase).ToUniversalTime();
                var finTrabajo = new DateTimeOffset(fecha.ToDateTime(horario.HoraFin), desfase).ToUniversalTime();
                var cursor = inicioTrabajo;

                foreach (var reserva in reservasEmpleado)
                {
                    if (reserva.FinUtc <= cursor || reserva.InicioUtc >= finTrabajo)
                    {
                        continue;
                    }

                    var inicioReserva = reserva.InicioUtc > cursor ? reserva.InicioUtc : cursor;
                    AgregarPeriodoDisponible(periodos, cursor, inicioReserva, servicio.DuracionMinutos, desfase);
                    if (reserva.FinUtc > cursor)
                    {
                        cursor = reserva.FinUtc;
                    }

                    if (cursor >= finTrabajo)
                    {
                        break;
                    }
                }

                AgregarPeriodoDisponible(periodos, cursor, finTrabajo, servicio.DuracionMinutos, desfase);
            }

            if (periodos.Count > 0)
            {
                respuesta.Add(new EmpleadoDisponibilidadDto(empleado.Id, empleado.Nombre, periodos));
            }
        }

        return Resultado<IReadOnlyList<EmpleadoDisponibilidadDto>>.Exito(respuesta);
    }

    private static void AgregarPeriodoDisponible(
        ICollection<PeriodoDisponibleDto> periodos,
        DateTimeOffset inicioUtc,
        DateTimeOffset finUtc,
        int duracionMinutos,
        TimeSpan desfase)
    {
        if (finUtc - inicioUtc >= TimeSpan.FromMinutes(duracionMinutos))
        {
            periodos.Add(new PeriodoDisponibleDto(inicioUtc.ToOffset(desfase), finUtc.ToOffset(desfase)));
        }
    }

    private static CitaDto Convertir(Cita cita) => new(
        cita.Id,
        cita.ClienteId,
        cita.Cliente.Nombre,
        cita.ServicioId,
        cita.Servicio.Nombre,
        cita.EmpleadoId,
        cita.Empleado.Nombre,
        cita.InicioUtc,
        cita.FinUtc,
        cita.DuracionMinutos,
        cita.Estado.ToString(),
        cita.FechaCreacionUtc,
        cita.FechaModificacionUtc);

    private static EstadoCita? ParsearEstado(string? valor) =>
        Enum.TryParse<EstadoCita>(valor, ignoreCase: true, out var estado) && Enum.IsDefined(estado)
            ? estado
            : null;

    private static ErrorDominio NoEncontrada() => new(
        "cita.no_encontrada", "No se encontró la cita solicitada.");
}
