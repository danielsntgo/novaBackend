using FlexPos.Application.DTOs.Reportes;
using FlexPos.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FlexPos.Infrastructure.Persistence.Repositories;

public sealed partial class RepositorioReportes
{
    private Task<ReporteDto> VentasAsync(FiltrosReporte filtros, CancellationToken cancellationToken)
    {
        var consulta = EnRango(contexto.Ventas.AsNoTracking(), filtros.DesdeUtc, filtros.HastaUtc, x => x.FechaVentaUtc);
        if (filtros.ClienteId.HasValue)
        {
            consulta = consulta.Where(x => x.ClienteId == filtros.ClienteId);
        }

        if (filtros.Estado is not null)
        {
            var estado = Enum.Parse<EstadoVenta>(filtros.Estado, true);
            consulta = consulta.Where(x => x.Estado == estado);
        }

        var filas = consulta
            .OrderByDescending(x => x.FechaVentaUtc)
            .ThenByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.FechaVentaUtc,
                x.ClienteNombre,
                x.ClienteDocumento,
                Estado = x.Estado,
                x.Subtotal,
                Descuentos = x.DescuentoLineas + x.DescuentoGeneral,
                x.Impuestos,
                x.Total,
                x.TotalPagado,
                x.TotalDevuelto,
                x.TotalReintegrado,
                x.TotalPendiente,
                x.CodigoMoneda
            });

        return CrearAsync(
            "ventas", "Ventas", filtros, filas,
            ["VentaId", "FechaVentaUtc", "Cliente", "DocumentoCliente", "Estado", "Subtotal",
             "Descuentos", "Impuestos", "Total", "Pagado", "Devuelto", "Reintegrado", "Pendiente", "Moneda"],
            x => Fila(("VentaId", x.Id), ("FechaVentaUtc", x.FechaVentaUtc), ("Cliente", x.ClienteNombre),
                ("DocumentoCliente", x.ClienteDocumento), ("Estado", x.Estado.ToString()), ("Subtotal", x.Subtotal),
                ("Descuentos", x.Descuentos), ("Impuestos", x.Impuestos), ("Total", x.Total),
                ("Pagado", x.TotalPagado), ("Devuelto", x.TotalDevuelto), ("Reintegrado", x.TotalReintegrado),
                ("Pendiente", x.TotalPendiente), ("Moneda", x.CodigoMoneda)),
            elementos =>
            [
                new TotalReporteDto("Ventas", elementos.Count),
                new TotalReporteDto("Total", elementos.Sum(x => x.Total), elementos.FirstOrDefault()?.CodigoMoneda),
                new TotalReporteDto("Pagado", elementos.Sum(x => x.TotalPagado), elementos.FirstOrDefault()?.CodigoMoneda),
                new TotalReporteDto("Pendiente", elementos.Sum(x => x.TotalPendiente), elementos.FirstOrDefault()?.CodigoMoneda)
            ], cancellationToken);
    }

    private async Task<ReporteDto> IngresosAsync(FiltrosReporte filtros, CancellationToken cancellationToken)
    {
        var pagos = EnRango(contexto.PagosVenta.AsNoTracking(), filtros.DesdeUtc, filtros.HastaUtc, x => x.FechaCreacionUtc);
        var reembolsos = EnRango(contexto.PagosDevolucionVenta.AsNoTracking(), filtros.DesdeUtc, filtros.HastaUtc, x => x.FechaCreacionUtc);
        if (filtros.MetodoPagoId.HasValue)
        {
            pagos = pagos.Where(x => x.MetodoPagoId == filtros.MetodoPagoId);
            reembolsos = reembolsos.Where(x => x.MetodoPagoId == filtros.MetodoPagoId);
        }

        var ventasQuery = from pago in pagos
                          join venta in contexto.Ventas.AsNoTracking() on pago.VentaId equals venta.Id
                          where !filtros.ClienteId.HasValue || venta.ClienteId == filtros.ClienteId
                          select new EventoIngreso(
                              pago.FechaCreacionUtc, "PagoVenta", venta.Id, pago.MetodoPagoNombre,
                              pago.EsEfectivo, pago.Importe, venta.CodigoMoneda, venta.ClienteNombre);
        var reembolsosQuery = from pago in reembolsos
                              join devolucion in contexto.DevolucionesVenta.AsNoTracking()
                                  on pago.DevolucionVentaId equals devolucion.Id
                              join venta in contexto.Ventas.AsNoTracking() on devolucion.VentaId equals venta.Id
                              where !filtros.ClienteId.HasValue || venta.ClienteId == filtros.ClienteId
                              select new EventoIngreso(
                                  pago.FechaCreacionUtc, "Reembolso", venta.Id, pago.MetodoPagoNombre,
                                  pago.EsEfectivo, -pago.Importe, venta.CodigoMoneda, venta.ClienteNombre);
        var consulta = ventasQuery.Concat(reembolsosQuery)
            .OrderByDescending(x => x.FechaUtc)
            .ThenBy(x => x.VentaId);

        return await CrearAsync(
            "ingresos", "Ingresos y reembolsos", filtros, consulta,
            ["FechaUtc", "Tipo", "VentaId", "Cliente", "MetodoPago", "EsEfectivo", "Importe", "Moneda"],
            x => Fila(("FechaUtc", x.FechaUtc), ("Tipo", x.Tipo), ("VentaId", x.VentaId),
                ("Cliente", x.ClienteNombre), ("MetodoPago", x.MetodoPagoNombre),
                ("EsEfectivo", x.EsEfectivo), ("Importe", x.Importe), ("Moneda", x.CodigoMoneda)),
            elementos =>
            [
                new TotalReporteDto("Movimientos", elementos.Count),
                new TotalReporteDto("Neto recaudado", elementos.Sum(x => x.Importe), elementos.FirstOrDefault()?.CodigoMoneda)
            ], cancellationToken);
    }

    private async Task<ReporteDto> CatalogoAsync(
        bool esServicio,
        FiltrosReporte filtros,
        CancellationToken cancellationToken)
    {
        var tipoLinea = esServicio ? TipoLineaVenta.Servicio : TipoLineaVenta.Producto;
        var ventas = from detalle in contexto.DetallesVenta.AsNoTracking()
                     join venta in contexto.Ventas.AsNoTracking() on detalle.VentaId equals venta.Id
                     where detalle.Tipo == tipoLinea
                     select new EventoCatalogo(
                         venta.FechaVentaUtc, "Venta", detalle.ArticuloInventarioId, detalle.ServicioId,
                         detalle.EmpleadoId, detalle.Codigo, detalle.Nombre, detalle.Unidad,
                         detalle.Cantidad, detalle.TotalImporte, venta.CodigoMoneda);
        ventas = EnRango(ventas, filtros.DesdeUtc, filtros.HastaUtc, x => x.FechaUtc);

        var devoluciones = from detalleDevolucion in contexto.DetallesDevolucionVenta.AsNoTracking()
                           join detalle in contexto.DetallesVenta.AsNoTracking()
                               on detalleDevolucion.DetalleVentaId equals detalle.Id
                           join devolucion in contexto.DevolucionesVenta.AsNoTracking()
                               on detalleDevolucion.DevolucionVentaId equals devolucion.Id
                           join venta in contexto.Ventas.AsNoTracking() on devolucion.VentaId equals venta.Id
                           where detalle.Tipo == tipoLinea
                           select new EventoCatalogo(
                               devolucion.FechaUtc, "Devolucion", detalle.ArticuloInventarioId, detalle.ServicioId,
                               detalle.EmpleadoId, detalle.Codigo, detalle.Nombre, detalle.Unidad,
                               -detalleDevolucion.Cantidad, -detalleDevolucion.Importe, venta.CodigoMoneda);
        devoluciones = EnRango(devoluciones, filtros.DesdeUtc, filtros.HastaUtc, x => x.FechaUtc);

        if (esServicio)
        {
            if (filtros.ServicioId.HasValue)
            {
                ventas = ventas.Where(x => x.ServicioId == filtros.ServicioId);
                devoluciones = devoluciones.Where(x => x.ServicioId == filtros.ServicioId);
            }

            if (filtros.EmpleadoId.HasValue)
            {
                ventas = ventas.Where(x => x.EmpleadoId == filtros.EmpleadoId);
                devoluciones = devoluciones.Where(x => x.EmpleadoId == filtros.EmpleadoId);
            }
        }
        else if (filtros.ArticuloId.HasValue)
        {
            ventas = ventas.Where(x => x.ArticuloId == filtros.ArticuloId);
            devoluciones = devoluciones.Where(x => x.ArticuloId == filtros.ArticuloId);
        }

        var consulta = ventas.Concat(devoluciones)
            .OrderByDescending(x => x.FechaUtc)
            .ThenBy(x => x.Nombre);
        return await CrearAsync(
            esServicio ? "servicios" : "productos",
            esServicio ? "Servicios vendidos y devueltos" : "Productos vendidos y devueltos",
            filtros,
            consulta,
            ["FechaUtc", "Movimiento", "ArticuloId", "ServicioId", "EmpleadoId", "Codigo",
             "Nombre", "Unidad", "Cantidad", "Importe", "Moneda"],
            x => Fila(("FechaUtc", x.FechaUtc), ("Movimiento", x.Tipo), ("ArticuloId", x.ArticuloId),
                ("ServicioId", x.ServicioId), ("EmpleadoId", x.EmpleadoId), ("Codigo", x.Codigo),
                ("Nombre", x.Nombre), ("Unidad", x.Unidad), ("Cantidad", x.Cantidad),
                ("Importe", x.Importe), ("Moneda", x.CodigoMoneda)),
            elementos =>
            [
                new TotalReporteDto("Movimientos", elementos.Count),
                new TotalReporteDto("Cantidad neta", elementos.Sum(x => x.Cantidad)),
                new TotalReporteDto("Importe neto", elementos.Sum(x => x.Importe), elementos.FirstOrDefault()?.CodigoMoneda)
            ], cancellationToken);
    }

    private Task<ReporteDto> InventarioAsync(FiltrosReporte filtros, CancellationToken cancellationToken)
    {
        var consulta = contexto.ArticulosInventario.AsNoTracking();
        if (filtros.ArticuloId.HasValue)
        {
            consulta = consulta.Where(x => x.Id == filtros.ArticuloId.Value);
        }

        var filas = consulta
            .OrderBy(x => x.NombreNormalizado)
            .Select(x => new
            {
                x.Id,
                x.Codigo,
                x.Nombre,
                Tipo = x.Tipo,
                x.Categoria,
                x.UnidadBase,
                x.ManejaFraccion,
                x.ExistenciaActual,
                x.CantidadMinima,
                CostoUnitario = x.CostoPromedio.Importe,
                Moneda = x.CostoPromedio.CodigoMoneda,
                ValorInventario = x.ExistenciaActual * x.CostoPromedio.Importe,
                PrecioVenta = x.PrecioVenta == null ? (decimal?)null : x.PrecioVenta.Importe,
                x.Activo,
                BajoMinimo = x.ExistenciaActual <= x.CantidadMinima
            });

        return CrearAsync(
            "inventario", "Existencias actuales", filtros with { DesdeUtc = null, HastaUtc = null }, filas,
            ["ArticuloId", "Codigo", "Nombre", "Tipo", "Categoria", "Unidad", "ManejaFraccion",
             "Existencia", "Minimo", "CostoPromedio", "ValorInventario", "PrecioVenta", "Activo", "BajoMinimo", "Moneda"],
            x => Fila(("ArticuloId", x.Id), ("Codigo", x.Codigo), ("Nombre", x.Nombre), ("Tipo", x.Tipo.ToString()),
                ("Categoria", x.Categoria), ("Unidad", x.UnidadBase), ("ManejaFraccion", x.ManejaFraccion),
                ("Existencia", x.ExistenciaActual), ("Minimo", x.CantidadMinima),
                ("CostoPromedio", x.CostoUnitario), ("ValorInventario", x.ValorInventario),
                ("PrecioVenta", x.PrecioVenta), ("Activo", x.Activo), ("BajoMinimo", x.BajoMinimo), ("Moneda", x.Moneda)),
            elementos =>
            [
                new TotalReporteDto("Articulos", elementos.Count),
                new TotalReporteDto("Existencia total", elementos.Sum(x => x.ExistenciaActual)),
                new TotalReporteDto("Valor inventario", elementos.Sum(x => x.ValorInventario), elementos.FirstOrDefault()?.Moneda)
            ], cancellationToken);
    }

    private sealed record EventoIngreso(
        DateTimeOffset FechaUtc,
        string Tipo,
        Guid VentaId,
        string MetodoPagoNombre,
        bool EsEfectivo,
        decimal Importe,
        string CodigoMoneda,
        string? ClienteNombre);

    private sealed record EventoCatalogo(
        DateTimeOffset FechaUtc,
        string Tipo,
        Guid? ArticuloId,
        Guid? ServicioId,
        Guid? EmpleadoId,
        string? Codigo,
        string Nombre,
        string Unidad,
        decimal Cantidad,
        decimal Importe,
        string CodigoMoneda);
}
