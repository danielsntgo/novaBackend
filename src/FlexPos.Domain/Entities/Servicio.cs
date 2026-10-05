using FlexPos.Domain.Abstractions;
using FlexPos.Domain.Errors;
using FlexPos.Domain.ValueObjects;

namespace FlexPos.Domain.Entities;

public sealed class Servicio : EntidadAuditable
{
    private Servicio()
    {
    }

    private Servicio(
        string nombre,
        string? descripcion,
        string? categoria,
        Dinero precio,
        int duracionMinutos)
    {
        EstablecerDatos(nombre, descripcion, categoria, precio, duracionMinutos);
    }

    public string Nombre { get; private set; } = string.Empty;
    public string NombreNormalizado { get; private set; } = string.Empty;
    public string? Descripcion { get; private set; }
    public string? Categoria { get; private set; }
    public string? CategoriaNormalizada { get; private set; }
    public Dinero Precio { get; private set; } = null!;
    public int DuracionMinutos { get; private set; }
    public bool Activo { get; private set; } = true;

    public static Resultado<Servicio> Crear(
        string? nombre,
        string? descripcion,
        string? categoria,
        Dinero? precio,
        int duracionMinutos)
    {
        var error = Validar(nombre, descripcion, categoria, precio, duracionMinutos);
        return error is null
            ? Resultado<Servicio>.Exito(new Servicio(nombre!.Trim(), descripcion, categoria, precio!, duracionMinutos))
            : Resultado<Servicio>.Fallo(error);
    }

    public Resultado<bool> Actualizar(
        string? nombre,
        string? descripcion,
        string? categoria,
        Dinero? precio,
        int duracionMinutos)
    {
        var error = Validar(nombre, descripcion, categoria, precio, duracionMinutos);
        if (error is not null)
        {
            return Resultado<bool>.Fallo(error);
        }

        EstablecerDatos(nombre!, descripcion, categoria, precio!, duracionMinutos);
        return Resultado<bool>.Exito(true);
    }

    public void EstablecerEstado(bool activo) => Activo = activo;

    private void EstablecerDatos(
        string nombre,
        string? descripcion,
        string? categoria,
        Dinero precio,
        int duracionMinutos)
    {
        Nombre = nombre.Trim();
        NombreNormalizado = TextoNormalizado.Normalizar(Nombre)!;
        Descripcion = Limpiar(descripcion);
        Categoria = Limpiar(categoria);
        CategoriaNormalizada = TextoNormalizado.Normalizar(Categoria);
        Precio = precio;
        DuracionMinutos = duracionMinutos;
    }

    private static ErrorDominio? Validar(
        string? nombre,
        string? descripcion,
        string? categoria,
        Dinero? precio,
        int duracionMinutos)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 120 ||
            !LongitudOpcionalValida(descripcion, 500) ||
            !LongitudOpcionalValida(categoria, 100))
        {
            return new ErrorDominio("servicio.datos_invalidos", "Uno o más datos del servicio son inválidos o exceden la longitud permitida.");
        }

        if (precio is null || precio.Importe < 0)
        {
            return new ErrorDominio("servicio.precio_invalido", "El precio debe ser un importe no negativo con moneda válida.");
        }

        if (duracionMinutos <= 0)
        {
            return new ErrorDominio("servicio.duracion_invalida", "La duración debe ser mayor que cero minutos.");
        }

        return null;
    }

    private static bool LongitudOpcionalValida(string? valor, int maximo) =>
        valor is null || valor.Trim().Length <= maximo;

    private static string? Limpiar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
