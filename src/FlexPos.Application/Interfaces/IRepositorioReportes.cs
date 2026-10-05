using FlexPos.Application.DTOs.Reportes;

namespace FlexPos.Application.Interfaces;

public interface IRepositorioReportes
{
    Task<ReporteDto> ObtenerAsync(string tipo, FiltrosReporte filtros, CancellationToken cancellationToken);
}

public interface IExportadorReportes
{
    ArchivoReporte Exportar(ReporteDto reporte, string formato);
}
