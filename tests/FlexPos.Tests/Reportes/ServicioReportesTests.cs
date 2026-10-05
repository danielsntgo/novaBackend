using FlexPos.Application.DTOs.Reportes;
using FlexPos.Application.Interfaces;
using FlexPos.Application.Services;
using Xunit;

namespace FlexPos.Tests.Reportes;

public sealed class ServicioReportesTests
{
    private readonly ServicioReportes servicio = new(new RepositorioInalcanzable(), new ExportadorInalcanzable());

    [Fact]
    public async Task Obtener_RechazaRangoInvertidoAntesDeConsultarDatos()
    {
        var resultado = await servicio.ObtenerAsync("ventas", new FiltrosReporte(
            DesdeUtc: DateTimeOffset.Parse("2026-10-05T12:00:00Z"),
            HastaUtc: DateTimeOffset.Parse("2026-10-05T11:00:00Z")), CancellationToken.None);

        Assert.Equal("reporte.filtros_invalidos", resultado.Error?.Codigo);
    }

    [Fact]
    public async Task Obtener_RechazaFiltroQueNoCorrespondeAlInforme()
    {
        var resultado = await servicio.ObtenerAsync("compras", new FiltrosReporte(ClienteId: Guid.NewGuid()),
            CancellationToken.None);

        Assert.Equal("reporte.filtro_no_aplicable", resultado.Error?.Codigo);
    }

    [Fact]
    public async Task Obtener_RechazaFechasEnFotografiaActualDeInventario()
    {
        var resultado = await servicio.ObtenerAsync("inventario", new FiltrosReporte(
            DesdeUtc: DateTimeOffset.Parse("2026-10-05T00:00:00Z")), CancellationToken.None);

        Assert.Equal("reporte.filtro_no_aplicable", resultado.Error?.Codigo);
    }

    private sealed class RepositorioInalcanzable : IRepositorioReportes
    {
        public Task<ReporteDto> ObtenerAsync(string tipo, FiltrosReporte filtros, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("No se esperaba consultar el repositorio.");
    }

    private sealed class ExportadorInalcanzable : IExportadorReportes
    {
        public ArchivoReporte Exportar(ReporteDto reporte, string formato) =>
            throw new InvalidOperationException("No se esperaba exportar un informe.");
    }
}
