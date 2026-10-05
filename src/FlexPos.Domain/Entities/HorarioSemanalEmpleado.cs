using FlexPos.Domain.Errors;

namespace FlexPos.Domain.Entities;

public sealed class HorarioSemanalEmpleado
{
    private HorarioSemanalEmpleado()
    {
    }

    private HorarioSemanalEmpleado(DayOfWeek diaSemana, TimeOnly horaInicio, TimeOnly horaFin)
    {
        DiaSemana = diaSemana;
        HoraInicio = horaInicio;
        HoraFin = horaFin;
    }

    public DayOfWeek DiaSemana { get; private set; }
    public TimeOnly HoraInicio { get; private set; }
    public TimeOnly HoraFin { get; private set; }

    public static Resultado<HorarioSemanalEmpleado> Crear(
        DayOfWeek diaSemana,
        TimeOnly horaInicio,
        TimeOnly horaFin)
    {
        if (!Enum.IsDefined(diaSemana) || horaFin <= horaInicio)
        {
            return Resultado<HorarioSemanalEmpleado>.Fallo(new ErrorDominio(
                "empleado.horario_invalido",
                "El día o el intervalo horario del empleado no es válido."));
        }

        return Resultado<HorarioSemanalEmpleado>.Exito(
            new HorarioSemanalEmpleado(diaSemana, horaInicio, horaFin));
    }

    public bool Incluye(TimeOnly inicio, TimeOnly fin) =>
        HoraInicio <= inicio && HoraFin >= fin;
}
