using FlexPos.Domain.Abstractions;
using FlexPos.Domain.Errors;
using FlexPos.Domain.ValueObjects;

namespace FlexPos.Domain.Entities;

public sealed class Proveedor : EntidadAuditable
{
    private Proveedor()
    {
    }

    private Proveedor(string nombre, string? identificacionFiscal, string? telefono, string? correo)
    {
        Nombre = nombre.Trim();
        NombreNormalizado = TextoNormalizado.Normalizar(Nombre)!;
        IdentificacionFiscal = Limpiar(identificacionFiscal);
        Telefono = Limpiar(telefono);
        Correo = Limpiar(correo)?.ToLowerInvariant();
        Activo = true;
    }

    public string Nombre { get; private set; } = string.Empty;
    public string NombreNormalizado { get; private set; } = string.Empty;
    public string? IdentificacionFiscal { get; private set; }
    public string? Telefono { get; private set; }
    public string? Correo { get; private set; }
    public bool Activo { get; private set; }

    public static Resultado<Proveedor> Crear(
        string? nombre, string? identificacionFiscal, string? telefono, string? correo)
    {
        var error = Validar(nombre, identificacionFiscal, telefono, correo);
        return error is null
            ? Resultado<Proveedor>.Exito(new Proveedor(nombre!, identificacionFiscal, telefono, correo))
            : Resultado<Proveedor>.Fallo(error);
    }

    public Resultado<bool> Actualizar(
        string? nombre, string? identificacionFiscal, string? telefono, string? correo)
    {
        var error = Validar(nombre, identificacionFiscal, telefono, correo);
        if (error is not null)
        {
            return Resultado<bool>.Fallo(error);
        }

        Nombre = nombre!.Trim();
        NombreNormalizado = TextoNormalizado.Normalizar(Nombre)!;
        IdentificacionFiscal = Limpiar(identificacionFiscal);
        Telefono = Limpiar(telefono);
        Correo = Limpiar(correo)?.ToLowerInvariant();
        return Resultado<bool>.Exito(true);
    }

    public void EstablecerEstado(bool activo) => Activo = activo;

    private static ErrorDominio? Validar(
        string? nombre, string? identificacionFiscal, string? telefono, string? correo)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 160 ||
            (!string.IsNullOrWhiteSpace(identificacionFiscal) && identificacionFiscal.Trim().Length > 60) ||
            (!string.IsNullOrWhiteSpace(telefono) && telefono.Trim().Length > 40) ||
            (!string.IsNullOrWhiteSpace(correo) && correo.Trim().Length > 256))
        {
            return new ErrorDominio("proveedor.datos_invalidos", "Los datos del proveedor son invalidos.");
        }

        if (!string.IsNullOrWhiteSpace(correo) && !System.Net.Mail.MailAddress.TryCreate(correo, out _))
        {
            return new ErrorDominio("proveedor.correo_invalido", "El correo del proveedor no es valido.");
        }

        return null;
    }

    private static string? Limpiar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
