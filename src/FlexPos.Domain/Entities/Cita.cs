using FlexPos.Domain.Abstractions;
using FlexPos.Domain.Enums;
using FlexPos.Domain.Errors;

namespace FlexPos.Domain.Entities;

public sealed class Cita : EntidadAuditable
{
    private Cita()
    {
    }

    private Cita(Guid clienteId, Guid servicioId, Guid empleadoId, DateTimeOffset inicioUtc, int duracionMinutos)
    {
        ClienteId = clienteId;
        ServicioId = servicioId;
        EmpleadoId = empleadoId;
        InicioUtc = inicioUtc.ToUniversalTime();
        DuracionMinutos = duracionMinutos;
        FinUtc = InicioUtc.AddMinutes(DuracionMinutos);
        Estado = EstadoCita.Pendiente;
    }

    public Guid ClienteId { get; private set; }
    public Guid ServicioId { get; private set; }
    public Guid EmpleadoId { get; private set; }
    public DateTimeOffset InicioUtc { get; private set; }
    public int DuracionMinutos { get; private set; }
    public DateTimeOffset FinUtc { get; private set; }
    public EstadoCita Estado { get; private set; }

    public Cliente Cliente { get; private set; } = null!;
    public Servicio Servicio { get; private set; } = null!;
    public Empleado Empleado { get; private set; } = null!;

    public static Resultado<Cita> Crear(
        Guid clienteId,
        Guid servicioId,
        Guid empleadoId,
        DateTimeOffset inicioLocal,
        int duracionMinutos)
    {
        if (clienteId == Guid.Empty || servicioId == Guid.Empty || empleadoId == Guid.Empty ||
            !DuracionValida(duracionMinutos))
        {
            return Resultado<Cita>.Fallo(new ErrorDominio(
                "cita.datos_invalidos",
                "Los datos de la cita o su duración no son válidos."));
        }

        return Resultado<Cita>.Exito(new Cita(
            clienteId, servicioId, empleadoId, inicioLocal, duracionMinutos));
    }

    public Resultado<bool> Reprogramar(Guid empleadoId, DateTimeOffset inicioLocal)
    {
        if (Estado is EstadoCita.Atendida or EstadoCita.Cancelada or EstadoCita.NoAsistio)
        {
            return Resultado<bool>.Fallo(new ErrorDominio(
                "cita.estado_terminal",
                "No se puede reprogramar una cita atendida, cancelada o marcada como no asistida."));
        }

        if (empleadoId == Guid.Empty)
        {
            return Resultado<bool>.Fallo(new ErrorDominio(
                "cita.datos_invalidos", "Debe seleccionar un empleado activo."));
        }

        EmpleadoId = empleadoId;
        InicioUtc = inicioLocal.ToUniversalTime();
        FinUtc = InicioUtc.AddMinutes(DuracionMinutos);
        return Resultado<bool>.Exito(true);
    }

    public Resultado<bool> CambiarEstado(EstadoCita nuevoEstado)
    {
        var permitido = Estado switch
        {
            EstadoCita.Pendiente => nuevoEstado is EstadoCita.Confirmada or EstadoCita.Atendida or
                EstadoCita.Cancelada or EstadoCita.NoAsistio,
            EstadoCita.Confirmada => nuevoEstado is EstadoCita.Atendida or EstadoCita.Cancelada or
                EstadoCita.NoAsistio,
            _ => false
        };

        if (!Enum.IsDefined(nuevoEstado) || !permitido)
        {
            return Resultado<bool>.Fallo(new ErrorDominio(
                "cita.transicion_invalida",
                "La cita no puede cambiar desde su estado actual al estado solicitado."));
        }

        Estado = nuevoEstado;
        return Resultado<bool>.Exito(true);
    }

    public bool EstaDentroDelHorario(DateTimeOffset inicioLocal, Empleado empleado)
    {
        var horaInicio = TimeOnly.FromDateTime(inicioLocal.DateTime);
        var horaFin = inicioLocal.TimeOfDay + TimeSpan.FromMinutes(DuracionMinutos);
        if (horaFin >= TimeSpan.FromDays(1))
        {
            return false;
        }

        var fin = TimeOnly.FromTimeSpan(horaFin);
        return empleado.HorariosSemanales.Any(horario =>
            horario.DiaSemana == inicioLocal.DayOfWeek && horario.Incluye(horaInicio, fin));
    }

    private static bool DuracionValida(int duracionMinutos) =>
        duracionMinutos is > 0 and <= 1440;
}
