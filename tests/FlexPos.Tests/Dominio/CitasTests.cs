using FlexPos.Domain.Entities;
using FlexPos.Domain.Enums;
using FlexPos.Domain.ValueObjects;
using Xunit;

namespace FlexPos.Tests.Dominio;

public sealed class CitasTests
{
    [Fact]
    public void Empleado_AceptaBloquesSemanalesConPausaYRechazaCruces()
    {
        var empleado = CrearEmpleado();
        var manana = Horario(DayOfWeek.Monday, 9, 0, 12, 0);
        var tarde = Horario(DayOfWeek.Monday, 13, 0, 17, 0);

        var configuracion = empleado.ConfigurarHorarioSemanal([tarde, manana]);

        Assert.True(configuracion.EsExitoso);
        Assert.Equal(2, empleado.HorariosSemanales.Count);
        var revision = empleado.RevisionHorario;

        var cruce = empleado.ConfigurarHorarioSemanal(
        [
            manana,
            Horario(DayOfWeek.Monday, 11, 30, 14, 0)
        ]);

        Assert.False(cruce.EsExitoso);
        Assert.Equal("empleado.horario_solapado", cruce.Error!.Codigo);
        Assert.Equal(2, empleado.HorariosSemanales.Count);
        Assert.Equal(revision, empleado.RevisionHorario);
    }

    [Fact]
    public void HorarioSemanal_RechazaDiaEIntervaloInvalidos()
    {
        Assert.False(HorarioSemanalEmpleado.Crear((DayOfWeek)8, new TimeOnly(9, 0), new TimeOnly(10, 0)).EsExitoso);
        Assert.False(HorarioSemanalEmpleado.Crear(DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(10, 0)).EsExitoso);
        Assert.False(HorarioSemanalEmpleado.Crear(DayOfWeek.Monday, new TimeOnly(11, 0), new TimeOnly(10, 0)).EsExitoso);
    }

    [Fact]
    public void Cita_ConservaDuracionYConvierteInicioLocalAUtc()
    {
        var inicioLocal = new DateTimeOffset(2026, 10, 5, 11, 15, 0, TimeSpan.FromHours(-5));
        var cita = Cita.Crear(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), inicioLocal, 45).Valor!;

        Assert.Equal(inicioLocal.ToUniversalTime(), cita.InicioUtc);
        Assert.Equal(inicioLocal.AddMinutes(45).ToUniversalTime(), cita.FinUtc);
        Assert.Equal(45, cita.DuracionMinutos);
        Assert.Equal(EstadoCita.Pendiente, cita.Estado);
    }

    [Fact]
    public void Cita_DebeQuedarDentroDeUnBloqueLaboralCompleto()
    {
        var empleado = CrearEmpleado();
        empleado.ConfigurarHorarioSemanal(
        [
            Horario(DayOfWeek.Monday, 9, 0, 12, 0),
            Horario(DayOfWeek.Monday, 13, 0, 17, 0)
        ]);
        var inicioEnLaManana = new DateTimeOffset(2026, 10, 5, 11, 15, 0, TimeSpan.FromHours(-5));
        var inicioEnLaPausa = new DateTimeOffset(2026, 10, 5, 11, 30, 0, TimeSpan.FromHours(-5));
        var cita = Cita.Crear(Guid.NewGuid(), Guid.NewGuid(), empleado.Id, inicioEnLaManana, 45).Valor!;

        Assert.True(cita.EstaDentroDelHorario(inicioEnLaManana, empleado));
        Assert.False(cita.EstaDentroDelHorario(inicioEnLaPausa, empleado));
    }

    [Fact]
    public void Cita_LosEstadosTerminalesNoSeReabrenNiSeReprograman()
    {
        var cita = Cita.Crear(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new DateTimeOffset(2026, 10, 5, 10, 0, 0, TimeSpan.FromHours(-5)), 30).Valor!;

        Assert.True(cita.CambiarEstado(EstadoCita.Confirmada).EsExitoso);
        Assert.True(cita.CambiarEstado(EstadoCita.Cancelada).EsExitoso);
        Assert.False(cita.CambiarEstado(EstadoCita.Confirmada).EsExitoso);
        Assert.False(cita.Reprogramar(Guid.NewGuid(), new DateTimeOffset(
            2026, 10, 6, 10, 0, 0, TimeSpan.FromHours(-5))).EsExitoso);
    }

    private static Empleado CrearEmpleado() =>
        Empleado.Crear("Ana Pérez", "Estilista", null, null, null).Valor!;

    private static HorarioSemanalEmpleado Horario(
        DayOfWeek dia,
        int horaInicio,
        int minutoInicio,
        int horaFin,
        int minutoFin) =>
        HorarioSemanalEmpleado.Crear(
            dia, new TimeOnly(horaInicio, minutoInicio), new TimeOnly(horaFin, minutoFin)).Valor!;
}
