using System.Net.Mail;
using FlexPos.Domain.Abstractions;
using FlexPos.Domain.Errors;
using FlexPos.Domain.ValueObjects;

namespace FlexPos.Domain.Entities;

public sealed class Cliente : EntidadAuditable
{
    private Cliente()
    {
    }

    private Cliente(string nombre, string? identificacion, string? telefono, string? correo)
    {
        EstablecerDatos(nombre, identificacion, telefono, correo);
    }

    public string Nombre { get; private set; } = string.Empty;
    public string NombreNormalizado { get; private set; } = string.Empty;
    public string? Documento { get; private set; }
    public string? DocumentoNormalizado { get; private set; }
    public string? Telefono { get; private set; }
    public string? TelefonoNormalizado { get; private set; }
    public string? Correo { get; private set; }
    public string? CorreoNormalizado { get; private set; }
    public bool Activo { get; private set; } = true;

    public static Resultado<Cliente> Crear(
        string? nombre,
        string? identificacion,
        string? telefono,
        string? correo)
    {
        var error = Validar(nombre, identificacion, telefono, correo);
        return error is null
            ? Resultado<Cliente>.Exito(new Cliente(nombre!.Trim(), identificacion, telefono, correo))
            : Resultado<Cliente>.Fallo(error);
    }

    public Resultado<bool> Actualizar(
        string? nombre,
        string? identificacion,
        string? telefono,
        string? correo)
    {
        var error = Validar(nombre, identificacion, telefono, correo);
        if (error is not null)
        {
            return Resultado<bool>.Fallo(error);
        }

        EstablecerDatos(nombre!, identificacion, telefono, correo);
        return Resultado<bool>.Exito(true);
    }

    public void EstablecerEstado(bool activo) => Activo = activo;

    private void EstablecerDatos(string nombre, string? identificacion, string? telefono, string? correo)
    {
        Nombre = nombre.Trim();
        NombreNormalizado = TextoNormalizado.Normalizar(Nombre)!;
        Documento = Limpiar(identificacion);
        DocumentoNormalizado = TextoNormalizado.Normalizar(Documento);
        Telefono = Limpiar(telefono);
        TelefonoNormalizado = TextoNormalizado.Normalizar(Telefono);
        Correo = Limpiar(correo)?.ToLowerInvariant();
        CorreoNormalizado = TextoNormalizado.Normalizar(Correo);
    }

    private static ErrorDominio? Validar(
        string? nombre,
        string? identificacion,
        string? telefono,
        string? correo)
    {
        if (!LongitudValida(nombre, 1, 150) ||
            !LongitudOpcionalValida(identificacion, 50) ||
            !LongitudOpcionalValida(telefono, 40) ||
            !LongitudOpcionalValida(correo, 256))
        {
            return new ErrorDominio("cliente.datos_invalidos", "Uno o más datos del cliente son inválidos o exceden la longitud permitida.");
        }

        if (!string.IsNullOrWhiteSpace(correo) &&
            (!MailAddress.TryCreate(correo.Trim(), out var direccion) ||
             !string.Equals(direccion.Address, correo.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return new ErrorDominio("cliente.correo_invalido", "El correo del cliente no es válido.");
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
