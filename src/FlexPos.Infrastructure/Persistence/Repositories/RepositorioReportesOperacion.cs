using FlexPos.Application.DTOs.Reportes;
using FlexPos.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FlexPos.Infrastructure.Persistence.Repositories;

public sealed partial class RepositorioReportes
{
    private Task<ReporteDto> ComprasAsync(FiltrosReporte filtros, CancellationToken cancellationToken)
    {
        var consulta = EnRango(contexto.Compras.AsNoTracking(), filtros.DesdeUtc, filtros.HastaUtc, x => x.FechaCompraUtc);
        if (filtros.ProveedorId.HasValue)
        {
            consulta = consulta.Where(x => x.ProveedorId == filtros.ProveedorId.Value);
        }

        if (filtros.Estado is not null)
        {
            var estado = Enum.Parse<EstadoCompra>(filtros.Estado, true);
            consulta = consulta.Where(x => x.Estado == estado);
        }

        var filas = from compra in consulta
                    join proveedor in contexto.Proveedores.AsNoTracking() on compra.ProveedorId equals proveedor.Id
                    orderby compra.FechaCompraUtc descending, compra.Id
                    select new
                    {
                        compra.Id,
                        compra.FechaCompraUtc,
                        Proveedor = proveedor.Nombre,
                        compra.Referencia,
                        Estado = compra.Estado,
                        compra.TotalImporte,
                        compra.CodigoMoneda,
                        Lineas = compra.Detalles.Count(x => x.Vigente)
                    };

        return CrearAsync(
            "compras", "Compras", filtros, filas,
            ["CompraId", "FechaCompraUtc", "Proveedor", "Referencia", "Estado", "Lineas", "Total", "Moneda"],
            x => Fila(("CompraId", x.Id), ("FechaCompraUtc", x.FechaCompraUtc), ("Proveedor", x.Proveedor),
                ("Referencia", x.Referencia), ("Estado", x.Estado.ToString()), ("Lineas", x.Lineas),
                ("Total", x.TotalImporte), ("Moneda", x.CodigoMoneda)),
            elementos =>
            [
                new TotalReporteDto("Compras", elementos.Count),
                new TotalReporteDto("Importe", elementos.Sum(x => x.TotalImporte), elementos.FirstOrDefault()?.CodigoMoneda)
            ], cancellationToken);
    }

    private async Task<ReporteDto> CajaAsync(FiltrosReporte filtros, CancellationToken cancellationToken)
    {
        var consulta = EnRango(contexto.Cajas.AsNoTracking(), filtros.DesdeUtc, filtros.HastaUtc, x => x.FechaAperturaUtc);
        if (filtros.Estado is not null)
        {
            var estado = Enum.Parse<EstadoCaja>(filtros.Estado, true);
            consulta = consulta.Where(x => x.Estado == estado);
        }

        var total = await consulta.CountAsync(cancellationToken);
        var cajas = await consulta.OrderByDescending(x => x.FechaAperturaUtc)
            .ThenByDescending(x => x.Id).Take(filtros.Limite + 1).ToArrayAsync(cancellationToken);
        var ids = cajas.Take(filtros.Limite).Select(x => x.Id).ToArray();
        var movimientos = await contexto.MovimientosCaja.AsNoTracking()
            .Where(x => ids.Contains(x.CajaId))
            .GroupBy(x => new { x.CajaId, x.Tipo })
            .Select(x => new { x.Key.CajaId, x.Key.Tipo, Importe = x.Sum(m => m.Importe) })
            .ToArrayAsync(cancellationToken);
        var moneda = await contexto.ConfiguracionesNegocio.AsNoTracking()
            .Where(x => x.EsPrincipal)
            .Select(x => x.CodigoMoneda)
            .SingleOrDefaultAsync(cancellationToken) ?? string.Empty;

        var filas = cajas.Take(filtros.Limite).Select(caja =>
        {
            decimal Importe(TipoMovimientoCaja tipo) => movimientos
                .Where(x => x.CajaId == caja.Id && x.Tipo == tipo).Sum(x => x.Importe);
            var ingresosVentas = Importe(TipoMovimientoCaja.PagoVenta);
            var ingresosManuales = Importe(TipoMovimientoCaja.IngresoManual);
            var reembolsos = Importe(TipoMovimientoCaja.Reembolso);
            var egresosManuales = Importe(TipoMovimientoCaja.EgresoManual);
            var pagoComisiones = Importe(TipoMovimientoCaja.PagoComision);
            var efectivoEsperado = caja.EfectivoEsperadoCierre ??
                caja.EfectivoApertura + ingresosVentas + ingresosManuales - reembolsos - egresosManuales - pagoComisiones;
            return new FilaCaja(
                caja.Id, caja.Estado.ToString(), caja.FechaAperturaUtc, caja.FechaCierreUtc,
                caja.EfectivoApertura, ingresosVentas, ingresosManuales, reembolsos, egresosManuales,
                pagoComisiones, efectivoEsperado, caja.EfectivoContadoCierre,
                caja.DiferenciaCierre, moneda);
        }).ToArray();

        return CrearDesdeLista(
            "caja", "Arqueos y movimientos de caja", filtros, total, filas,
            ["CajaId", "Estado", "AperturaUtc", "CierreUtc", "EfectivoInicial", "PagosVenta",
             "IngresosManuales", "Reembolsos", "EgresosManuales", "PagoComisiones",
             "EfectivoEsperado", "EfectivoContado", "Diferencia", "Moneda"],
            x => Fila(("CajaId", x.Id), ("Estado", x.Estado), ("AperturaUtc", x.AperturaUtc),
                ("CierreUtc", x.CierreUtc), ("EfectivoInicial", x.EfectivoInicial),
                ("PagosVenta", x.PagosVenta), ("IngresosManuales", x.IngresosManuales),
                ("Reembolsos", x.Reembolsos), ("EgresosManuales", x.EgresosManuales),
                ("PagoComisiones", x.PagoComisiones), ("EfectivoEsperado", x.EfectivoEsperado),
                ("EfectivoContado", x.EfectivoContado), ("Diferencia", x.Diferencia), ("Moneda", x.Moneda)),
            elementos =>
            [
                new TotalReporteDto("Cajas", elementos.Count),
                new TotalReporteDto("Efectivo inicial", elementos.Sum(x => x.EfectivoInicial), elementos.FirstOrDefault()?.Moneda),
                new TotalReporteDto("Diferencias de cierre", elementos.Sum(x => x.Diferencia ?? 0m), elementos.FirstOrDefault()?.Moneda)
            ]);
    }

    private async Task<ReporteDto> ClientesAsync(FiltrosReporte filtros, CancellationToken cancellationToken)
    {
        var ventas = EnRango(contexto.Ventas.AsNoTracking(), filtros.DesdeUtc, filtros.HastaUtc, x => x.FechaVentaUtc)
            .Where(x => x.ClienteId.HasValue);
        var grupos = from venta in ventas
                     group venta by venta.ClienteId!.Value into grupo
                     select new
                     {
                         ClienteId = grupo.Key,
                         Ventas = grupo.Count(),
                         TotalVentas = grupo.Sum(x => x.Total),
                         TotalDevuelto = grupo.Sum(x => x.TotalDevuelto),
                         TotalNeto = grupo.Sum(x => x.Total - x.TotalDevuelto),
                         TotalPagado = grupo.Sum(x => x.TotalPagado),
                         TotalReintegrado = grupo.Sum(x => x.TotalReintegrado),
                         SaldoPendiente = grupo.Sum(x => x.TotalPendiente),
                         Moneda = grupo.Select(x => x.CodigoMoneda).First()
                     };
        var consulta = from grupo in grupos
                       join cliente in contexto.Clientes.AsNoTracking() on grupo.ClienteId equals cliente.Id
                       where !filtros.ClienteId.HasValue || cliente.Id == filtros.ClienteId.Value
                       orderby grupo.TotalVentas descending, cliente.NombreNormalizado
                       select new
                       {
                           cliente.Id,
                           cliente.Nombre,
                           cliente.Documento,
                           cliente.Telefono,
                           cliente.Correo,
                           cliente.Activo,
                           grupo.Ventas,
                           grupo.TotalVentas,
                           grupo.TotalDevuelto,
                           grupo.TotalNeto,
                           grupo.TotalPagado,
                           grupo.TotalReintegrado,
                           grupo.SaldoPendiente,
                           grupo.Moneda
                       };

        return await CrearAsync(
            "clientes", "Actividad de clientes", filtros, consulta,
            ["ClienteId", "Nombre", "Documento", "Telefono", "Correo", "Activo", "Ventas",
             "TotalVentas", "TotalDevuelto", "TotalNeto", "TotalPagado", "TotalReintegrado", "SaldoPendiente", "Moneda"],
            x => Fila(("ClienteId", x.Id), ("Nombre", x.Nombre), ("Documento", x.Documento),
                ("Telefono", x.Telefono), ("Correo", x.Correo), ("Activo", x.Activo), ("Ventas", x.Ventas),
                ("TotalVentas", x.TotalVentas), ("TotalDevuelto", x.TotalDevuelto), ("TotalNeto", x.TotalNeto),
                ("TotalPagado", x.TotalPagado), ("TotalReintegrado", x.TotalReintegrado),
                ("SaldoPendiente", x.SaldoPendiente), ("Moneda", x.Moneda)),
            elementos =>
            [
                new TotalReporteDto("Clientes con ventas", elementos.Count),
                new TotalReporteDto("Ventas", elementos.Sum(x => x.Ventas)),
                new TotalReporteDto("Total vendido", elementos.Sum(x => x.TotalVentas), elementos.FirstOrDefault()?.Moneda),
                new TotalReporteDto("Total devuelto", elementos.Sum(x => x.TotalDevuelto), elementos.FirstOrDefault()?.Moneda),
                new TotalReporteDto("Total neto", elementos.Sum(x => x.TotalNeto), elementos.FirstOrDefault()?.Moneda),
                new TotalReporteDto("Total pagado", elementos.Sum(x => x.TotalPagado), elementos.FirstOrDefault()?.Moneda)
            ], cancellationToken);
    }

    private async Task<ReporteDto> EmpleadosAsync(FiltrosReporte filtros, CancellationToken cancellationToken)
    {
        var lineas = from detalle in contexto.DetallesVenta.AsNoTracking()
                     join venta in contexto.Ventas.AsNoTracking() on detalle.VentaId equals venta.Id
                     where detalle.Tipo == TipoLineaVenta.Servicio && detalle.EmpleadoId.HasValue
                     select new
                     {
                         detalle.EmpleadoId,
                         detalle.Cantidad,
                         detalle.TotalImporte,
                         venta.FechaVentaUtc,
                         venta.CodigoMoneda
                     };
        lineas = EnRango(lineas, filtros.DesdeUtc, filtros.HastaUtc, x => x.FechaVentaUtc);
        if (filtros.EmpleadoId.HasValue)
        {
            lineas = lineas.Where(x => x.EmpleadoId == filtros.EmpleadoId.Value);
        }

        var ventaPorEmpleado = await (from linea in lineas
                                       group linea by linea.EmpleadoId!.Value into grupo
                                       select new
                                       {
                                           EmpleadoId = grupo.Key,
                                           Servicios = grupo.Count(),
                                           Cantidad = grupo.Sum(x => x.Cantidad),
                                           TotalServicios = grupo.Sum(x => x.TotalImporte),
                                           Moneda = grupo.Select(x => x.CodigoMoneda).First()
                                       }).ToArrayAsync(cancellationToken);

        var comisiones = EnRango(contexto.Comisiones.AsNoTracking(), filtros.DesdeUtc, filtros.HastaUtc, x => x.FechaCreacionUtc);
        if (filtros.EmpleadoId.HasValue)
        {
            comisiones = comisiones.Where(x => x.EmpleadoId == filtros.EmpleadoId.Value);
        }

        var comisionPorEmpleado = await (from comision in comisiones
                                          group comision by comision.EmpleadoId into grupo
                                          select new
                                          {
                                              EmpleadoId = grupo.Key,
                                              Devengado = grupo.Where(x => x.TipoMovimiento == TipoMovimientoComision.DevengoServicio)
                                                  .Sum(x => x.Importe),
                                              Ajustes = grupo.Where(x => x.TipoMovimiento == TipoMovimientoComision.AjusteDevolucion)
                                                  .Sum(x => x.Importe),
                                              Moneda = grupo.Select(x => x.CodigoMoneda).First()
                                          }).ToArrayAsync(cancellationToken);

        var ids = ventaPorEmpleado.Select(x => x.EmpleadoId)
            .Union(comisionPorEmpleado.Select(x => x.EmpleadoId)).Distinct().ToArray();
        var empleados = await contexto.Empleados.AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .Select(x => new { x.Id, x.Nombre, x.Cargo, x.Activo })
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var ventasMap = ventaPorEmpleado.ToDictionary(x => x.EmpleadoId);
        var comisionesMap = comisionPorEmpleado.ToDictionary(x => x.EmpleadoId);
        var filas = ids.Where(empleados.ContainsKey)
            .Select(id =>
            {
                var venta = ventasMap.GetValueOrDefault(id);
                var comision = comisionesMap.GetValueOrDefault(id);
                return new FilaEmpleado(
                    id, empleados[id].Nombre, empleados[id].Cargo, empleados[id].Activo,
                    venta?.Servicios ?? 0, venta?.Cantidad ?? 0m, venta?.TotalServicios ?? 0m,
                    comision?.Devengado ?? 0m, comision?.Ajustes ?? 0m,
                    (comision?.Devengado ?? 0m) - (comision?.Ajustes ?? 0m),
                    comision?.Moneda ?? venta?.Moneda ?? string.Empty);
            })
            .OrderByDescending(x => x.TotalServicios)
            .ThenBy(x => x.Nombre)
            .ToArray();

        return CrearDesdeLista(
            "empleados", "Servicios y comisiones por empleado", filtros, filas.Length, filas,
            ["EmpleadoId", "Nombre", "Cargo", "Activo", "LineasServicio", "CantidadServicios",
             "TotalServicios", "ComisionesDevengadas", "AjustesDevolucion", "ComisionesNetas", "Moneda"],
            x => Fila(("EmpleadoId", x.Id), ("Nombre", x.Nombre), ("Cargo", x.Cargo), ("Activo", x.Activo),
                ("LineasServicio", x.LineasServicio), ("CantidadServicios", x.Cantidad),
                ("TotalServicios", x.TotalServicios), ("ComisionesDevengadas", x.Devengado),
                ("AjustesDevolucion", x.Ajustes), ("ComisionesNetas", x.Neto), ("Moneda", x.Moneda)),
            elementos =>
            [
                new TotalReporteDto("Empleados", elementos.Count),
                new TotalReporteDto("Servicios vendidos", elementos.Sum(x => x.Cantidad)),
                new TotalReporteDto("Total servicios", elementos.Sum(x => x.TotalServicios), elementos.FirstOrDefault()?.Moneda),
                new TotalReporteDto("Comisiones netas", elementos.Sum(x => x.Neto), elementos.FirstOrDefault()?.Moneda)
            ]);
    }

    private async Task<ReporteDto> ComisionesAsync(FiltrosReporte filtros, CancellationToken cancellationToken)
    {
        var consulta = EnRango(contexto.Comisiones.AsNoTracking(), filtros.DesdeUtc, filtros.HastaUtc, x => x.FechaCreacionUtc);
        if (filtros.EmpleadoId.HasValue)
        {
            consulta = consulta.Where(x => x.EmpleadoId == filtros.EmpleadoId.Value);
        }

        if (filtros.ServicioId.HasValue)
        {
            consulta = consulta.Where(x => x.ServicioId == filtros.ServicioId.Value);
        }

        if (filtros.Estado is not null)
        {
            var estado = Enum.Parse<EstadoComision>(filtros.Estado, true);
            consulta = consulta.Where(x => x.Estado == estado);
        }

        var filas = from comision in consulta
                    join empleado in contexto.Empleados.AsNoTracking() on comision.EmpleadoId equals empleado.Id
                    join servicio in contexto.Servicios.AsNoTracking() on comision.ServicioId equals servicio.Id
                    orderby comision.FechaCreacionUtc descending, comision.Id
                    select new
                    {
                        comision.Id,
                        comision.FechaCreacionUtc,
                        Empleado = empleado.Nombre,
                        Servicio = servicio.Nombre,
                        Movimiento = comision.TipoMovimiento,
                        Estado = comision.Estado,
                        comision.Cantidad,
                        comision.BaseCalculo,
                        comision.Importe,
                        comision.CodigoMoneda,
                        comision.VentaId,
                        comision.DevolucionVentaId,
                        comision.LiquidacionComisionId
                    };

        return await CrearAsync(
            "comisiones", "Comisiones y ajustes", filtros, filas,
            ["ComisionId", "FechaUtc", "Empleado", "Servicio", "Movimiento", "Estado", "Cantidad",
             "BaseCalculo", "Importe", "Moneda", "VentaId", "DevolucionVentaId", "LiquidacionId"],
            x => Fila(("ComisionId", x.Id), ("FechaUtc", x.FechaCreacionUtc), ("Empleado", x.Empleado),
                ("Servicio", x.Servicio), ("Movimiento", x.Movimiento.ToString()), ("Estado", x.Estado.ToString()),
                ("Cantidad", x.Cantidad), ("BaseCalculo", x.BaseCalculo), ("Importe", x.Importe),
                ("Moneda", x.CodigoMoneda), ("VentaId", x.VentaId), ("DevolucionVentaId", x.DevolucionVentaId),
                ("LiquidacionId", x.LiquidacionComisionId)),
            elementos =>
            [
                new TotalReporteDto("Movimientos", elementos.Count),
                new TotalReporteDto("Importe movimientos", elementos.Sum(x => x.Importe), elementos.FirstOrDefault()?.CodigoMoneda)
            ], cancellationToken);
    }

    private ReporteDto CrearDesdeLista<T>(
        string tipo,
        string nombre,
        FiltrosReporte filtros,
        int total,
        IReadOnlyList<T> filas,
        IReadOnlyList<string> columnas,
        Func<T, FilaReporteDto> mapear,
        Func<IReadOnlyList<T>, IReadOnlyList<TotalReporteDto>> totales) =>
        CrearDesdeLista(tipo, nombre, filtros, columnas, filas, total, mapear, totales);

    private sealed record FilaCaja(
        Guid Id,
        string Estado,
        DateTimeOffset AperturaUtc,
        DateTimeOffset? CierreUtc,
        decimal EfectivoInicial,
        decimal PagosVenta,
        decimal IngresosManuales,
        decimal Reembolsos,
        decimal EgresosManuales,
        decimal PagoComisiones,
        decimal? EfectivoEsperado,
        decimal? EfectivoContado,
        decimal? Diferencia,
        string Moneda);

    private sealed record FilaEmpleado(
        Guid Id,
        string Nombre,
        string Cargo,
        bool Activo,
        int LineasServicio,
        decimal Cantidad,
        decimal TotalServicios,
        decimal Devengado,
        decimal Ajustes,
        decimal Neto,
        string Moneda);
}
