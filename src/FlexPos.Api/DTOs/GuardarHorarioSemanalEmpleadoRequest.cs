using FlexPos.Application.DTOs.Empleados;

namespace FlexPos.Api.DTOs;

public sealed record GuardarHorarioSemanalEmpleadoRequest(
    IReadOnlyList<HorarioSemanalEmpleadoDto>? Intervalos);
