using System.Text;
using ClosedXML.Excel;
using FlexPos.Application.DTOs.Reportes;
using FlexPos.Infrastructure.Services;
using Xunit;

namespace FlexPos.Tests.Reportes;

public sealed class ExportadorReportesTests
{
    private readonly ExportadorReportes exportador = new();

    [Fact]
    public void ExportarCsv_EscapaSeparadoresYNeutralizaFormulasEnTexto()
    {
        var reporte = CrearReporte(
            ["Nombre", "Cantidad"],
            ("Nombre", "=HYPERLINK(\"https://example.invalid\")"),
            ("Cantidad", 1.125m));

        var archivo = exportador.Exportar(reporte, "csv");
        var contenido = Encoding.UTF8.GetString(archivo.Contenido);

        Assert.Equal("text/csv; charset=utf-8", archivo.TipoContenido);
        Assert.Contains("\"'=HYPERLINK", contenido);
        Assert.Contains("1.125", contenido);
    }

    [Fact]
    public void ExportarExcel_CreaLibroConCeldasNumericas()
    {
        const string nombreRiesgoso = "=HYPERLINK(\"https://example.invalid\")";
        var reporte = CrearReporte(["Nombre", "Cantidad"], ("Nombre", nombreRiesgoso), ("Cantidad", 1.125m));
        var archivo = exportador.Exportar(reporte, "xlsx");

        using var libro = new XLWorkbook(new MemoryStream(archivo.Contenido));
        var hoja = libro.Worksheet("Reporte");
        Assert.Equal("Reporte de prueba", hoja.Cell(1, 1).GetString());
        Assert.Equal("Cantidad", hoja.Cell(5, 2).GetString());
        Assert.Equal(nombreRiesgoso, hoja.Cell(6, 1).GetString());
        Assert.Equal(XLDataType.Text, hoja.Cell(6, 1).DataType);
        Assert.False(hoja.Cell(6, 1).HasFormula);
        Assert.Equal(1.125m, hoja.Cell(6, 2).GetValue<decimal>());
    }

    [Fact]
    public void ExportarPdf_GeneraDocumentoPdfValido()
    {
        var reporte = CrearReporte(["Nombre"], ("Nombre", "Producto"));
        var archivo = exportador.Exportar(reporte, "pdf");

        Assert.Equal("application/pdf", archivo.TipoContenido);
        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(archivo.Contenido, 0, 5));
    }

    private static ReporteDto CrearReporte(
        IReadOnlyList<string> columnas,
        params (string Nombre, object Valor)[] valores)
    {
        var valoresFila = valores.ToDictionary(x => x.Nombre, x => (object?)x.Valor);
        return new ReporteDto(
            "prueba", "Reporte de prueba", DateTimeOffset.UtcNow, null, null, columnas,
            [new FilaReporteDto(valoresFila)], [], 1, 5000, false);
    }
}
