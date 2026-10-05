using FlexPos.Application.DTOs.Reportes;
using FlexPos.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlexPos.Infrastructure.Persistence.Repositories;

public sealed partial class RepositorioReportes(FlexPosDbContext contexto) : IRepositorioReportes
{
    public Task<ReporteDto> ObtenerAsync(string tipo, FiltrosReporte filtros, CancellationToken cancellationToken) =>
        tipo switch
        {
            "ventas" => VentasAsync(filtros, cancellationToken),
            "ingresos" => IngresosAsync(filtros, cancellationToken),
            "productos" => CatalogoAsync(false, filtros, cancellationToken),
            "servicios" => CatalogoAsync(true, filtros, cancellationToken),
            "inventario" => InventarioAsync(filtros, cancellationToken),
            "compras" => ComprasAsync(filtros, cancellationToken),
            "caja" => CajaAsync(filtros, cancellationToken),
            "clientes" => ClientesAsync(filtros, cancellationToken),
            "empleados" => EmpleadosAsync(filtros, cancellationToken),
            "comisiones" => ComisionesAsync(filtros, cancellationToken),
            _ => throw new InvalidOperationException("Tipo de reporte no validado.")
        };

    private async Task<ReporteDto> CrearAsync<T>(
        string tipo,
        string nombre,
        FiltrosReporte filtros,
        IQueryable<T> consulta,
        IReadOnlyList<string> columnas,
        Func<T, FilaReporteDto> mapear,
        Func<IReadOnlyList<T>, IReadOnlyList<TotalReporteDto>> totales,
        CancellationToken cancellationToken)
    {
        var total = await consulta.CountAsync(cancellationToken);
        var datos = await consulta.Take(filtros.Limite + 1).ToArrayAsync(cancellationToken);
        return CrearDesdeLista(tipo, nombre, filtros, columnas, datos, total, mapear, totales);
    }

    private ReporteDto CrearDesdeLista<T>(
        string tipo,
        string nombre,
        FiltrosReporte filtros,
        IReadOnlyList<string> columnas,
        IReadOnlyList<T> datos,
        int total,
        Func<T, FilaReporteDto> mapear,
        Func<IReadOnlyList<T>, IReadOnlyList<TotalReporteDto>> totales)
    {
        var elementos = datos.Take(filtros.Limite).ToArray();
        return new ReporteDto(
            tipo,
            nombre,
            DateTimeOffset.UtcNow,
            filtros.DesdeUtc,
            filtros.HastaUtc,
            columnas,
            elementos.Select(mapear).ToArray(),
            totales(elementos),
            total,
            filtros.Limite,
            total > filtros.Limite);
    }

    private static FilaReporteDto Fila(params (string Nombre, object? Valor)[] valores) =>
        new(valores.ToDictionary(x => x.Nombre, x => x.Valor));

    private static IReadOnlyList<TotalReporteDto> Suma<T>(
        IReadOnlyList<T> filas,
        Func<T, decimal> importe,
        string codigoMoneda,
        string etiqueta = "Total") =>
        [new TotalReporteDto(etiqueta, filas.Sum(importe), codigoMoneda)];

    private static IQueryable<T> EnRango<T>(
        IQueryable<T> consulta,
        DateTimeOffset? desdeUtc,
        DateTimeOffset? hastaUtc,
        System.Linq.Expressions.Expression<Func<T, DateTimeOffset>> fecha)
    {
        if (desdeUtc.HasValue)
        {
            var desde = desdeUtc.Value;
            consulta = consulta.Where(ConstruirComparacion(fecha, desde, true));
        }

        if (hastaUtc.HasValue)
        {
            var hasta = hastaUtc.Value;
            consulta = consulta.Where(ConstruirComparacion(fecha, hasta, false));
        }

        return consulta;
    }

    private static System.Linq.Expressions.Expression<Func<T, bool>> ConstruirComparacion<T>(
        System.Linq.Expressions.Expression<Func<T, DateTimeOffset>> selector,
        DateTimeOffset fecha,
        bool mayorOIgual)
    {
        var comparacion = mayorOIgual
            ? System.Linq.Expressions.Expression.GreaterThanOrEqual(selector.Body, System.Linq.Expressions.Expression.Constant(fecha))
            : System.Linq.Expressions.Expression.LessThanOrEqual(selector.Body, System.Linq.Expressions.Expression.Constant(fecha));
        return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(comparacion, selector.Parameters);
    }
}
