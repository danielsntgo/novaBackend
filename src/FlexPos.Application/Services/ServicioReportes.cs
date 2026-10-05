using FlexPos.Application.DTOs.Reportes;
using FlexPos.Application.Interfaces;
using FlexPos.Domain.Enums;
using FlexPos.Domain.Errors;

namespace FlexPos.Application.Services;

public sealed class ServicioReportes(
    IRepositorioReportes repositorio,
    IExportadorReportes exportador)
{
    private static readonly HashSet<string> Tipos = new(StringComparer.OrdinalIgnoreCase)
    {
        "ventas", "ingresos", "productos", "servicios", "inventario",
        "compras", "caja", "clientes", "empleados", "comisiones"
    };

    public async Task<Resultado<ReporteDto>> ObtenerAsync(
        string tipo,
        FiltrosReporte filtros,
        CancellationToken cancellationToken)
    {
        var error = Validar(tipo, filtros);
        if (error is not null)
        {
            return Resultado<ReporteDto>.Fallo(error);
        }

        var normalizado = Normalizar(filtros);
        return Resultado<ReporteDto>.Exito(await repositorio.ObtenerAsync(
            tipo.ToLowerInvariant(), normalizado, cancellationToken));
    }

    public async Task<Resultado<ArchivoReporte>> ExportarAsync(
        string tipo,
        string formato,
        FiltrosReporte filtros,
        CancellationToken cancellationToken)
    {
        formato = formato.ToLowerInvariant();
        if (formato is not ("pdf" or "xlsx" or "csv"))
        {
            return Resultado<ArchivoReporte>.Fallo(new ErrorDominio(
                "reporte.formato_invalido", "El formato debe ser pdf, xlsx o csv."));
        }

        var reporte = await ObtenerAsync(tipo, filtros, cancellationToken);
        if (!reporte.EsExitoso)
        {
            return Resultado<ArchivoReporte>.Fallo(reporte.Error!);
        }

        return Resultado<ArchivoReporte>.Exito(exportador.Exportar(reporte.Valor!, formato));
    }

    private ErrorDominio? Validar(string tipo, FiltrosReporte filtros)
    {
        if (!Tipos.Contains(tipo) || filtros.DesdeUtc.HasValue && filtros.HastaUtc.HasValue &&
            filtros.DesdeUtc > filtros.HastaUtc ||
            filtros.Limite is < 1 or > 10000 ||
            filtros.ClienteId == Guid.Empty || filtros.EmpleadoId == Guid.Empty ||
            filtros.ArticuloId == Guid.Empty || filtros.ServicioId == Guid.Empty ||
            filtros.ProveedorId == Guid.Empty || filtros.MetodoPagoId == Guid.Empty)
        {
            return new ErrorDominio(
                "reporte.filtros_invalidos", "El tipo, los filtros o el límite del reporte no son válidos.");
        }

        var tipoNormalizado = tipo.ToLowerInvariant();
        var filtroIncompatible =
            filtros.ClienteId.HasValue && tipoNormalizado is not ("ventas" or "ingresos" or "clientes") ||
            filtros.EmpleadoId.HasValue && tipoNormalizado is not ("servicios" or "empleados" or "comisiones") ||
            filtros.ArticuloId.HasValue && tipoNormalizado is not ("productos" or "inventario") ||
            filtros.ServicioId.HasValue && tipoNormalizado is not ("servicios" or "comisiones") ||
            filtros.ProveedorId.HasValue && tipoNormalizado != "compras" ||
            filtros.MetodoPagoId.HasValue && tipoNormalizado != "ingresos";
        if (filtroIncompatible)
        {
            return new ErrorDominio(
                "reporte.filtro_no_aplicable", "Uno de los filtros no corresponde al tipo de reporte seleccionado.");
        }

        if (tipoNormalizado == "inventario" && (filtros.DesdeUtc.HasValue || filtros.HastaUtc.HasValue))
        {
            return new ErrorDominio(
                "reporte.filtro_no_aplicable", "El inventario es una fotografía actual y no admite filtros de fecha.");
        }

        if (string.IsNullOrWhiteSpace(filtros.Estado))
        {
            return null;
        }

        var estadoValido = tipo.ToLowerInvariant() switch
        {
            "ventas" => Enum.TryParse<EstadoVenta>(filtros.Estado, true, out var venta) && Enum.IsDefined(venta),
            "compras" => Enum.TryParse<EstadoCompra>(filtros.Estado, true, out var compra) && Enum.IsDefined(compra),
            "caja" => Enum.TryParse<EstadoCaja>(filtros.Estado, true, out var caja) && Enum.IsDefined(caja),
            "comisiones" => Enum.TryParse<EstadoComision>(filtros.Estado, true, out var comision) && Enum.IsDefined(comision),
            _ => false
        };
        return estadoValido
            ? null
            : new ErrorDominio("reporte.estado_invalido", "El filtro estado no es válido para este tipo de reporte.");
    }

    private FiltrosReporte Normalizar(FiltrosReporte filtros) => filtros with
    {
        DesdeUtc = filtros.DesdeUtc?.ToUniversalTime(),
        HastaUtc = filtros.HastaUtc?.ToUniversalTime(),
        Estado = string.IsNullOrWhiteSpace(filtros.Estado) ? null : filtros.Estado.Trim(),
        Limite = filtros.Limite == 0 ? 5000 : filtros.Limite
    };
}
