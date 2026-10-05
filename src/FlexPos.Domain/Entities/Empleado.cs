using System.Net.Mail;
using FlexPos.Domain.Abstractions;
using FlexPos.Domain.Errors;
using FlexPos.Domain.ValueObjects;

namespace FlexPos.Domain.Entities;

public sealed class Empleado : EntidadAuditable
{
    private readonly List<HorarioSemanalEmpleado> _horariosSemanales = [];

    private Empleado()
    {
    }

    private Empleado(string nombre, string cargo, string? identificacion, string? telefono, string? correo)
    {
        EstablecerDatos(nombre, cargo, identificacion, telefono, correo);
    }

    public string Nombre { get; private set; } = string.Empty;
    public string NombreNormalizado { get; private set; } = string.Empty;
    public string Cargo { get; private set; } = string.Empty;
    public string CargoNormalizado { get; private set; } = string.Empty;
    public string? Documento { get; private set; }
    public string? DocumentoNormalizado { get; private set; }
    public string? Telefono { get; private set; }
    public string? TelefonoNormalizado { get; private set; }
    public string? Correo { get; private set; }
    public string? CorreoNormalizado { get; private set; }
    public bool Activo { get; private set; } = true;
    public int RevisionHorario { get; private set; }
    public IReadOnlyCollection<HorarioSemanalEmpleado> HorariosSemanales => _horariosSemanales.AsReadOnly();

    public static Resultado<Empleado> Crear(
        string? nombre,
        string? cargo,
        string? identificacion,
        string? telefono,
        string? correo)
    {
        var error = Validar(nombre, cargo, identificacion, telefono, correo);
        return error is null
            ? Resultado<Empleado>.Exito(new Empleado(nombre!.Trim(), cargo!.Trim(), identificacion, telefono, correo))
            : Resultado<Empleado>.Fallo(error);
    }

    public Resultado<bool> Actualizar(
        string? nombre,
        string? cargo,
        string? identificacion,
        string? telefono,
        string? correo)
    {
        var error = Validar(nombre, cargo, identificacion, telefono, correo);
        if (error is not null)
        {
            return Resultado<bool>.Fallo(error);
        }

        EstablecerDatos(nombre!, cargo!, identificacion, telefono, correo);
        return Resultado<bool>.Exito(true);
    }

    public void EstablecerEstado(bool activo) => Activo = activo;

    public Resultado<bool> ConfigurarHorarioSemanal(IEnumerable<HorarioSemanalEmpleado>? horarios)
    {
        if (horarios is null)
        {
            return Resultado<bool>.Fallo(new ErrorDominio(
                "empleado.horario_invalido", "Debe enviar una lista de intervalos horarios."));
        }

        var nuevosHorarios = horarios
            .OrderBy(horario => horario.DiaSemana)
            .ThenBy(horario => horario.HoraInicio)
            .ToArray();
        foreach (var grupo in nuevosHorarios.GroupBy(horario => horario.DiaSemana))
        {
            for (var indice = 1; indice < grupo.Count(); indice++)
            {
                var anterior = grupo.ElementAt(indice - 1);
                var actual = grupo.ElementAt(indice);
                if (actual.HoraInicio < anterior.HoraFin)
                {
                    return Resultado<bool>.Fallo(new ErrorDominio(
                        "empleado.horario_solapado",
                        "Los intervalos horarios del empleado no pueden solaparse."));
                }
            }
        }

        if (_horariosSemanales.Count == nuevosHorarios.Length &&
            _horariosSemanales.Zip(nuevosHorarios).All(par =>
                par.First.DiaSemana == par.Second.DiaSemana &&
                par.First.HoraInicio == par.Second.HoraInicio &&
                par.First.HoraFin == par.Second.HoraFin))
        {
            return Resultado<bool>.Exito(true);
        }

        _horariosSemanales.Clear();
        _horariosSemanales.AddRange(nuevosHorarios);
        RevisionHorario++;
        return Resultado<bool>.Exito(true);
    }

    private void EstablecerDatos(
        string nombre,
        string cargo,
        string? identificacion,
        string? telefono,
        string? correo)
    {
        Nombre = nombre.Trim();
        NombreNormalizado = TextoNormalizado.Normalizar(Nombre)!;
        Cargo = cargo.Trim();
        CargoNormalizado = TextoNormalizado.Normalizar(Cargo)!;
        Documento = Limpiar(identificacion);
        DocumentoNormalizado = TextoNormalizado.Normalizar(Documento);
        Telefono = Limpiar(telefono);
        TelefonoNormalizado = TextoNormalizado.Normalizar(Telefono);
        Correo = Limpiar(correo)?.ToLowerInvariant();
        CorreoNormalizado = TextoNormalizado.Normalizar(Correo);
    }

    private static ErrorDominio? Validar(
        string? nombre,
        string? cargo,
        string? identificacion,
        string? telefono,
        string? correo)
    {
        if (!LongitudValida(nombre, 1, 150) ||
            !LongitudValida(cargo, 1, 100) ||
            !LongitudOpcionalValida(identificacion, 50) ||
            !LongitudOpcionalValida(telefono, 40) ||
            !LongitudOpcionalValida(correo, 256))
        {
            return new ErrorDominio("empleado.datos_invalidos", "Uno o más datos del empleado son inválidos o exceden la longitud permitida.");
        }

        if (!string.IsNullOrWhiteSpace(correo) &&
            (!MailAddress.TryCreate(correo.Trim(), out var direccion) ||
             !string.Equals(direccion.Address, correo.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return new ErrorDominio("empleado.correo_invalido", "El correo del empleado no es válido.");
        }

        return null;
    }

    private static bool LongitudValida(string? valor, int minimo, int maximo) =>
        valor is not null && valor.Trim().Length >= minimo && valor.Trim().Length <= maximo;

    private static bool LongitudOpcionalValida(string? valor, int maximo) =>
        valor is null || valor.Trim().Length <= maximo;

    private static string? Limpiar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
