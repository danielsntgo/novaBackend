using FlexPos.Domain.Abstractions;
using FlexPos.Domain.Enums;
using FlexPos.Domain.Errors;
using FlexPos.Domain.ValueObjects;

namespace FlexPos.Domain.Entities;

public sealed class ArticuloInventario : EntidadAuditable
{
    private const decimal ImporteMaximo = 9_999_999_999_999_999.99m;
    private const decimal CantidadMaxima = 999_999_999_999_999.999m;

    private ArticuloInventario()
    {
    }

    private ArticuloInventario(
        string? codigo,
        string nombre,
        TipoArticuloInventario tipo,
        string unidadBase,
        bool manejaFraccion,
        string? categoria,
        decimal cantidadMinima,
        string codigoMoneda,
        Dinero? precioVenta)
    {
        Codigo = Limpiar(codigo);
        CodigoNormalizado = Normalizar(Codigo);
        Nombre = nombre.Trim();
        NombreNormalizado = TextoNormalizado.Normalizar(Nombre)!;
        Tipo = tipo;
        UnidadBase = unidadBase.Trim();
        UnidadBaseNormalizada = TextoNormalizado.Normalizar(UnidadBase)!;
        ManejaFraccion = manejaFraccion;
        Categoria = Limpiar(categoria);
        CategoriaNormalizada = TextoNormalizado.Normalizar(Categoria);
        CantidadMinima = cantidadMinima;
        CostoPromedio = Dinero.Crear(0m, codigoMoneda).Valor!;
        PrecioVenta = precioVenta;
        Activo = true;
    }

    public string? Codigo { get; private set; }
    public string? CodigoNormalizado { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public string NombreNormalizado { get; private set; } = string.Empty;
    public TipoArticuloInventario Tipo { get; private set; }
    public string UnidadBase { get; private set; } = string.Empty;
    public string UnidadBaseNormalizada { get; private set; } = string.Empty;
    public bool ManejaFraccion { get; private set; }
    public string? Categoria { get; private set; }
    public string? CategoriaNormalizada { get; private set; }
    public decimal ExistenciaActual { get; private set; }
    public decimal CantidadMinima { get; private set; }
    public Dinero CostoPromedio { get; private set; } = null!;
    public Dinero? PrecioVenta { get; private set; }
    public bool Activo { get; private set; }

    public static Resultado<ArticuloInventario> Crear(
        string? codigo,
        string? nombre,
        TipoArticuloInventario tipo,
        string? unidadBase,
        bool manejaFraccion,
        string? categoria,
        decimal cantidadMinima,
        string? codigoMoneda,
        decimal? precioVenta)
    {
        var moneda = Dinero.Crear(0m, codigoMoneda);
        if (!moneda.EsExitoso)
        {
            return Resultado<ArticuloInventario>.Fallo(moneda.Error!);
        }

        var precio = CrearPrecio(tipo, precioVenta, moneda.Valor!.CodigoMoneda);
        if (!precio.EsExitoso)
        {
            return Resultado<ArticuloInventario>.Fallo(precio.Error!);
        }

        var error = ValidarDatos(codigo, nombre, tipo, unidadBase, manejaFraccion, categoria, cantidadMinima);
        return error is null
            ? Resultado<ArticuloInventario>.Exito(new ArticuloInventario(
                codigo, nombre!, tipo, unidadBase!, manejaFraccion, categoria,
                cantidadMinima, moneda.Valor.CodigoMoneda, precio.Valor))
            : Resultado<ArticuloInventario>.Fallo(error);
    }

    public Resultado<bool> Actualizar(
        string? codigo,
        string? nombre,
        string? unidadBase,
        bool manejaFraccion,
        string? categoria,
        decimal cantidadMinima,
        decimal? precioVenta,
        bool tieneMovimientos)
    {
        var error = ValidarDatos(codigo, nombre, Tipo, unidadBase, manejaFraccion, categoria, cantidadMinima);
        if (error is not null)
        {
            return Resultado<bool>.Fallo(error);
        }

        if (tieneMovimientos &&
            (!string.Equals(UnidadBase, unidadBase!.Trim(), StringComparison.OrdinalIgnoreCase) ||
             ManejaFraccion != manejaFraccion))
        {
            return Resultado<bool>.Fallo(new ErrorDominio(
                "inventario.unidad_inmutable",
                "La unidad base y su precision no se pueden cambiar despues de registrar movimientos."));
        }

        var precio = CrearPrecio(Tipo, precioVenta, CostoPromedio.CodigoMoneda);
        if (!precio.EsExitoso)
        {
            return Resultado<bool>.Fallo(precio.Error!);
        }

        Codigo = Limpiar(codigo);
        CodigoNormalizado = Normalizar(Codigo);
        Nombre = nombre!.Trim();
        NombreNormalizado = TextoNormalizado.Normalizar(Nombre)!;
        UnidadBase = unidadBase!.Trim();
        UnidadBaseNormalizada = TextoNormalizado.Normalizar(UnidadBase)!;
        ManejaFraccion = manejaFraccion;
        Categoria = Limpiar(categoria);
        CategoriaNormalizada = TextoNormalizado.Normalizar(Categoria);
        CantidadMinima = cantidadMinima;
        PrecioVenta = precio.Valor;
        return Resultado<bool>.Exito(true);
    }

    public Resultado<bool> AplicarEntrada(decimal cantidad, Dinero costoUnitario)
    {
        var error = ValidarCantidad(cantidad, exigirPositiva: true);
        if (error is not null)
        {
            return Resultado<bool>.Fallo(error);
        }

        if (costoUnitario.CodigoMoneda != CostoPromedio.CodigoMoneda || costoUnitario.Importe < 0m)
        {
            return Resultado<bool>.Fallo(new ErrorDominio(
                "inventario.costo_invalido", "El costo debe ser no negativo y usar la moneda configurada."));
        }

        try
        {
            var cantidadNueva = checked(ExistenciaActual + cantidad);
            var costoNuevo = ExistenciaActual <= 0m
                ? costoUnitario.Importe
                : Math.Round(
                    (ExistenciaActual * CostoPromedio.Importe + cantidad * costoUnitario.Importe) / cantidadNueva,
                    2,
                    MidpointRounding.AwayFromZero);
            var dineroNuevo = Dinero.Crear(costoNuevo, CostoPromedio.CodigoMoneda);
            if (!dineroNuevo.EsExitoso || cantidadNueva > CantidadMaxima)
            {
                return Resultado<bool>.Fallo(new ErrorDominio(
                    "inventario.saldo_invalido", "El saldo o el costo promedio excede el rango permitido."));
            }

            ExistenciaActual = cantidadNueva;
            CostoPromedio = dineroNuevo.Valor!;
            return Resultado<bool>.Exito(true);
        }
        catch (OverflowException)
        {
            return Resultado<bool>.Fallo(new ErrorDominio(
                "inventario.saldo_invalido", "El saldo o el costo promedio excede el rango permitido."));
        }
    }

    public Resultado<bool> AplicarSalida(decimal cantidad, bool permitirStockNegativo = false)
    {
        var error = ValidarCantidad(cantidad, exigirPositiva: true);
        if (error is not null)
        {
            return Resultado<bool>.Fallo(error);
        }

        if (!permitirStockNegativo && cantidad > ExistenciaActual)
        {
            return Resultado<bool>.Fallo(new ErrorDominio(
                "inventario.stock_insuficiente", "La salida supera las existencias disponibles."));
        }

        var existenciaNueva = ExistenciaActual - cantidad;
        if (existenciaNueva < -CantidadMaxima)
        {
            return Resultado<bool>.Fallo(new ErrorDominio(
                "inventario.saldo_invalido", "El saldo excede el rango permitido."));
        }

        ExistenciaActual = existenciaNueva;
        return Resultado<bool>.Exito(true);
    }

    public Resultado<bool> EstablecerEstado(bool activo)
    {
        Activo = activo;
        return Resultado<bool>.Exito(true);
    }

    public bool EsCantidadValida(decimal cantidad, bool exigirPositiva = true) =>
        ValidarCantidad(cantidad, exigirPositiva) is null;

    private ErrorDominio? ValidarCantidad(decimal cantidad, bool exigirPositiva)
    {
        if ((exigirPositiva && cantidad <= 0m) || cantidad < 0m || cantidad > CantidadMaxima ||
            decimal.Round(cantidad, 3) != cantidad || (!ManejaFraccion && decimal.Truncate(cantidad) != cantidad))
        {
            return new ErrorDominio(
                "inventario.cantidad_invalida",
                ManejaFraccion
                    ? "La cantidad debe ser positiva y tener como maximo tres decimales."
                    : "La cantidad debe ser un numero entero positivo.");
        }

        return null;
    }

    private static ErrorDominio? ValidarDatos(
        string? codigo,
        string? nombre,
        TipoArticuloInventario tipo,
        string? unidadBase,
        bool manejaFraccion,
        string? categoria,
        decimal cantidadMinima)
    {
        if (!Enum.IsDefined(tipo) ||
            string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 120 ||
            (!string.IsNullOrWhiteSpace(codigo) && codigo.Trim().Length > 50) ||
            string.IsNullOrWhiteSpace(unidadBase) || unidadBase.Trim().Length > 20 ||
            (!string.IsNullOrWhiteSpace(categoria) && categoria.Trim().Length > 100))
        {
            return new ErrorDominio("inventario.datos_invalidos", "Los datos del articulo son invalidos.");
        }

        if (cantidadMinima < 0m || cantidadMinima > CantidadMaxima ||
            decimal.Round(cantidadMinima, 3) != cantidadMinima ||
            (!manejaFraccion && decimal.Truncate(cantidadMinima) != cantidadMinima))
        {
            return new ErrorDominio(
                "inventario.cantidad_minima_invalida", "La cantidad minima no coincide con la precision del articulo.");
        }

        return null;
    }

    private static Resultado<Dinero?> CrearPrecio(
        TipoArticuloInventario tipo,
        decimal? precioVenta,
        string codigoMoneda)
    {
        if (tipo == TipoArticuloInventario.Insumo)
        {
            return precioVenta is null
                ? Resultado<Dinero?>.Exito(null)
                : Resultado<Dinero?>.Fallo(new ErrorDominio(
                    "inventario.precio_invalido", "Los insumos no requieren precio de venta."));
        }

        if (precioVenta is null || precioVenta < 0m)
        {
            return Resultado<Dinero?>.Fallo(new ErrorDominio(
                "inventario.precio_invalido", "Los productos requieren un precio de venta no negativo."));
        }

        var dinero = Dinero.Crear(precioVenta.Value, codigoMoneda);
        return dinero.EsExitoso
            ? Resultado<Dinero?>.Exito(dinero.Valor)
            : Resultado<Dinero?>.Fallo(dinero.Error!);
    }

    private static string? Limpiar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static string? Normalizar(string? valor) =>
        valor is null ? null : TextoNormalizado.Normalizar(valor);
}
