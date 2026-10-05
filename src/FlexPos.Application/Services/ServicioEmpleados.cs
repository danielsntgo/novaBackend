using FlexPos.Application.DTOs.Empleados;
using FlexPos.Application.Interfaces;
using FlexPos.Domain.Entities;
using FlexPos.Domain.Errors;
using FlexPos.Domain.ValueObjects;

namespace FlexPos.Application.Services;

public sealed class ServicioEmpleados(IRepositorioEmpleados repositorio)
{
    public async Task<Resultado<PaginaEmpleadosDto>> ListarAsync(
        string? buscar,
        bool? activo,
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken)
    {
        if (!ValidacionListado.EsValida(buscar, pagina, tamanoPagina))
        {
            return Resultado<PaginaEmpleadosDto>.Fallo(new ErrorDominio(
                "empleado.busqueda_invalida", "La paginación o el texto de búsqueda no son válidos."));
        }

        var resultado = await repositorio.ListarAsync(
            TextoNormalizado.Normalizar(buscar), activo,
            ValidacionListado.CalcularOmitir(pagina, tamanoPagina), tamanoPagina, cancellationToken);
        return Resultado<PaginaEmpleadosDto>.Exito(new PaginaEmpleadosDto(
            resultado.Elementos.Select(Convertir).ToArray(), pagina, tamanoPagina, resultado.Total));
    }

    public async Task<Resultado<EmpleadoDto>> ObtenerAsync(Guid id, CancellationToken cancellationToken)
    {
        var empleado = id == Guid.Empty ? null : await repositorio.ObtenerAsync(id, cancellationToken);
        return empleado is null
            ? Resultado<EmpleadoDto>.Fallo(NoEncontrado())
            : Resultado<EmpleadoDto>.Exito(Convertir(empleado));
    }

    public async Task<Resultado<EmpleadoDto>> CrearAsync(
        SolicitudEmpleado solicitud,
        CancellationToken cancellationToken)
    {
        var nuevo = Empleado.Crear(
            solicitud.Nombre, solicitud.Cargo, solicitud.Documento, solicitud.Telefono, solicitud.Correo);
        if (!nuevo.EsExitoso)
        {
            return Resultado<EmpleadoDto>.Fallo(nuevo.Error!);
        }

        repositorio.Agregar(nuevo.Valor!);
        return await GuardarYConvertirAsync(nuevo.Valor!, cancellationToken);
    }

    public async Task<Resultado<EmpleadoDto>> ActualizarAsync(
        Guid id,
        SolicitudEmpleado solicitud,
        CancellationToken cancellationToken)
    {
        var empleado = id == Guid.Empty ? null : await repositorio.ObtenerAsync(id, cancellationToken);
        if (empleado is null)
        {
            return Resultado<EmpleadoDto>.Fallo(NoEncontrado());
        }

        var actualizacion = empleado.Actualizar(
            solicitud.Nombre, solicitud.Cargo, solicitud.Documento, solicitud.Telefono, solicitud.Correo);
        if (!actualizacion.EsExitoso)
        {
            return Resultado<EmpleadoDto>.Fallo(actualizacion.Error!);
        }

        return await GuardarYConvertirAsync(empleado, cancellationToken);
    }

    public async Task<Resultado<EmpleadoDto>> EstablecerEstadoAsync(
        Guid id,
        bool activo,
        CancellationToken cancellationToken)
    {
        var empleado = id == Guid.Empty ? null : await repositorio.ObtenerAsync(id, cancellationToken);
        if (empleado is null)
        {
            return Resultado<EmpleadoDto>.Fallo(NoEncontrado());
        }

        empleado.EstablecerEstado(activo);
        return await GuardarYConvertirAsync(empleado, cancellationToken);
    }

    public async Task<Resultado<IReadOnlyList<HorarioSemanalEmpleadoDto>>> ObtenerHorarioSemanalAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var empleado = id == Guid.Empty ? null : await repositorio.ObtenerAsync(id, cancellationToken);
        return empleado is null
            ? Resultado<IReadOnlyList<HorarioSemanalEmpleadoDto>>.Fallo(NoEncontrado())
            : Resultado<IReadOnlyList<HorarioSemanalEmpleadoDto>>.Exito(ConvertirHorario(empleado));
    }

    public async Task<Resultado<IReadOnlyList<HorarioSemanalEmpleadoDto>>> ConfigurarHorarioSemanalAsync(
        Guid id,
        IReadOnlyCollection<HorarioSemanalEmpleadoDto>? solicitud,
        CancellationToken cancellationToken)
    {
        var empleado = id == Guid.Empty ? null : await repositorio.ObtenerAsync(id, cancellationToken);
        if (empleado is null)
        {
            return Resultado<IReadOnlyList<HorarioSemanalEmpleadoDto>>.Fallo(NoEncontrado());
        }

        if (solicitud is null)
        {
            return Resultado<IReadOnlyList<HorarioSemanalEmpleadoDto>>.Fallo(new ErrorDominio(
                "empleado.horario_invalido", "Debe enviar una lista de intervalos horarios."));
        }

        var horarios = new List<HorarioSemanalEmpleado>(solicitud.Count);
        foreach (var intervalo in solicitud)
        {
            if (intervalo is null || !Enum.IsDefined((DayOfWeek)intervalo.DiaSemana))
            {
                return Resultado<IReadOnlyList<HorarioSemanalEmpleadoDto>>.Fallo(new ErrorDominio(
                    "empleado.horario_invalido", "El día de la semana no es válido."));
            }

            var horario = HorarioSemanalEmpleado.Crear(
                (DayOfWeek)intervalo.DiaSemana, intervalo.HoraInicio, intervalo.HoraFin);
            if (!horario.EsExitoso)
            {
                return Resultado<IReadOnlyList<HorarioSemanalEmpleadoDto>>.Fallo(horario.Error!);
            }

            horarios.Add(horario.Valor!);
        }

        var configuracion = empleado.ConfigurarHorarioSemanal(horarios);
        if (!configuracion.EsExitoso)
        {
            return Resultado<IReadOnlyList<HorarioSemanalEmpleadoDto>>.Fallo(configuracion.Error!);
        }

        var error = await repositorio.GuardarHorarioConBloqueoAsync(empleado, cancellationToken);
        return error is null
            ? Resultado<IReadOnlyList<HorarioSemanalEmpleadoDto>>.Exito(ConvertirHorario(empleado))
            : Resultado<IReadOnlyList<HorarioSemanalEmpleadoDto>>.Fallo(error);
    }

    private async Task<Resultado<EmpleadoDto>> GuardarYConvertirAsync(
        Empleado empleado,
        CancellationToken cancellationToken)
    {
        if (!await repositorio.GuardarCambiosAsync(cancellationToken))
        {
            return Resultado<EmpleadoDto>.Fallo(new ErrorDominio(
                "empleado.conflicto", "El empleado cambió en otra operación."));
        }

        return Resultado<EmpleadoDto>.Exito(Convertir(empleado));
    }

    private static EmpleadoDto Convertir(Empleado empleado) => new(
        empleado.Id, empleado.Nombre, empleado.Cargo, empleado.Documento, empleado.Telefono,
        empleado.Correo, empleado.Activo, empleado.FechaCreacionUtc, empleado.FechaModificacionUtc);

    private static IReadOnlyList<HorarioSemanalEmpleadoDto> ConvertirHorario(Empleado empleado) =>
        empleado.HorariosSemanales
            .OrderBy(horario => horario.DiaSemana)
            .ThenBy(horario => horario.HoraInicio)
            .Select(horario => new HorarioSemanalEmpleadoDto(
                (int)horario.DiaSemana, horario.HoraInicio, horario.HoraFin))
            .ToArray();

    private static ErrorDominio NoEncontrado() => new(
        "empleado.no_encontrado", "No se encontró el empleado solicitado.");
}
