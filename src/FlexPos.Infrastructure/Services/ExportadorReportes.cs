using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using FlexPos.Application.DTOs.Reportes;
using FlexPos.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FlexPos.Infrastructure.Services;

public sealed class ExportadorReportes : IExportadorReportes
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-CO");

    public ExportadorReportes() => QuestPDF.Settings.License = LicenseType.Community;

    public ArchivoReporte Exportar(ReporteDto reporte, string formato)
    {
        var marcaTiempo = reporte.GeneradoUtc.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var nombreBase = $"reporte_{reporte.Tipo}_{marcaTiempo}";
        return formato switch
        {
            "pdf" => new ArchivoReporte(GenerarPdf(reporte), $"{nombreBase}.pdf", "application/pdf"),
            "xlsx" => new ArchivoReporte(GenerarExcel(reporte), $"{nombreBase}.xlsx",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
            "csv" => new ArchivoReporte(GenerarCsv(reporte), $"{nombreBase}.csv", "text/csv; charset=utf-8"),
            _ => throw new ArgumentOutOfRangeException(nameof(formato))
        };
    }

    private static byte[] GenerarCsv(ReporteDto reporte)
    {
        var texto = new StringBuilder();
        texto.AppendLine(string.Join(',', reporte.Columnas.Select(EscaparCsv)));
        foreach (var fila in reporte.Filas)
        {
            texto.AppendLine(string.Join(',', reporte.Columnas.Select(columna =>
            {
                var valor = fila.Valores.GetValueOrDefault(columna);
                var textoCelda = Formatear(valor, EsImporte(columna), invariant: true);
                if (valor is string && EsTextoRiesgoso(textoCelda))
                {
                    textoCelda = $"'{textoCelda}";
                }

                return EscaparCsv(textoCelda);
            })));
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(texto.ToString())).ToArray();
    }

    private static byte[] GenerarExcel(ReporteDto reporte)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Reporte");
        hoja.Cell(1, 1).Value = reporte.Nombre;
        hoja.Cell(1, 1).Style.Font.Bold = true;
        hoja.Cell(2, 1).Value = $"Generado UTC: {reporte.GeneradoUtc:O}";
        if (reporte.DesdeUtc.HasValue || reporte.HastaUtc.HasValue)
        {
            hoja.Cell(3, 1).Value = $"Rango UTC: {reporte.DesdeUtc:O} - {reporte.HastaUtc:O}";
        }

        const int filaCabecera = 5;
        for (var columna = 0; columna < reporte.Columnas.Count; columna++)
        {
            var celda = hoja.Cell(filaCabecera, columna + 1);
            celda.Value = reporte.Columnas[columna];
            celda.Style.Font.Bold = true;
            celda.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        for (var indiceFila = 0; indiceFila < reporte.Filas.Count; indiceFila++)
        {
            for (var indiceColumna = 0; indiceColumna < reporte.Columnas.Count; indiceColumna++)
            {
                var columna = reporte.Columnas[indiceColumna];
                EstablecerValor(hoja.Cell(filaCabecera + indiceFila + 1, indiceColumna + 1),
                    reporte.Filas[indiceFila].Valores.GetValueOrDefault(columna), EsImporte(columna));
            }
        }

        var filaTotal = filaCabecera + reporte.Filas.Count + 2;
        hoja.Cell(filaTotal, 1).Value = "Resumen";
        hoja.Cell(filaTotal, 1).Style.Font.Bold = true;
        foreach (var total in reporte.Totales)
        {
            filaTotal++;
            hoja.Cell(filaTotal, 1).Value = total.Nombre;
            EstablecerValor(hoja.Cell(filaTotal, 2), total.Valor, total.CodigoMoneda is not null);
            if (!string.IsNullOrWhiteSpace(total.CodigoMoneda))
            {
                hoja.Cell(filaTotal, 3).Value = total.CodigoMoneda;
            }
        }

        hoja.Columns().AdjustToContents(10, 48);
        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return memoria.ToArray();
    }

    private static byte[] GenerarPdf(ReporteDto reporte) => Document.Create(documento =>
    {
        documento.Page(pagina =>
        {
            pagina.Size(PageSizes.A4.Landscape());
            pagina.Margin(24);
            pagina.DefaultTextStyle(estilo => estilo.FontSize(7).FontColor(Colors.Grey.Darken4));
            pagina.Header().Column(columna =>
            {
                columna.Item().Text(reporte.Nombre).FontSize(16).Bold();
                columna.Item().Text(
                    $"Rango UTC: {reporte.DesdeUtc?.ToString("O", CultureInfo.InvariantCulture) ?? "sin inicio"} - " +
                    $"{reporte.HastaUtc?.ToString("O", CultureInfo.InvariantCulture) ?? "sin fin"} | " +
                    $"Generado: {reporte.GeneradoUtc:dd/MM/yyyy HH:mm} UTC");
            });

            pagina.Content().PaddingVertical(10).Column(columna =>
            {
                columna.Item().Table(tabla =>
                {
                    tabla.ColumnsDefinition(definicion =>
                    {
                        foreach (var _ in reporte.Columnas)
                        {
                            definicion.RelativeColumn();
                        }
                    });

                    tabla.Header(cabecera =>
                    {
                        foreach (var nombre in reporte.Columnas)
                        {
                            Celda(cabecera.Cell(), nombre, true);
                        }
                    });

                    foreach (var fila in reporte.Filas)
                    {
                        foreach (var nombre in reporte.Columnas)
                        {
                            Celda(tabla.Cell(), Formatear(
                                fila.Valores.GetValueOrDefault(nombre), EsImporte(nombre)));
                        }
                    }
                });

                if (reporte.Totales.Count > 0)
                {
                    columna.Item().PaddingTop(10).Text("Resumen").Bold();
                    foreach (var total in reporte.Totales)
                    {
                        var sufijo = total.CodigoMoneda is null ? string.Empty : $" {total.CodigoMoneda}";
                        columna.Item().Text(
                            $"{total.Nombre}: {Formatear(total.Valor, total.CodigoMoneda is not null)}{sufijo}");
                    }
                }

                if (reporte.Truncado)
                {
                    columna.Item().PaddingTop(6).Text($"Se muestran {reporte.Limite} de {reporte.TotalRegistros} registros.");
                }
            });

            pagina.Footer().AlignRight().Text(texto =>
            {
                texto.Span("Página ");
                texto.CurrentPageNumber();
                texto.Span(" de ");
                texto.TotalPages();
            });
        });
    }).GeneratePdf();

    private static void EstablecerValor(IXLCell celda, object? valor, bool esImporte)
    {
        switch (valor)
        {
            case null:
                celda.Clear();
                break;
            case DateTimeOffset fecha:
                celda.Value = fecha.UtcDateTime;
                celda.Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
                break;
            case DateTime fecha:
                celda.Value = fecha;
                celda.Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
                break;
            case decimal numero:
                celda.Value = numero;
                celda.Style.NumberFormat.Format = esImporte ? "#,##0.00" : "#,##0.###";
                break;
            case int numero:
                celda.Value = numero;
                break;
            case long numero:
                celda.Value = numero;
                break;
            case bool booleano:
                celda.Value = booleano;
                break;
            default:
                var texto = Convert.ToString(valor, CultureInfo.InvariantCulture) ?? string.Empty;
                celda.Value = EsTextoRiesgoso(texto) ? $"'{texto}" : texto;
                break;
        }
    }

    private static void Celda(QuestPDF.Infrastructure.IContainer celda, string texto, bool cabecera = false)
    {
        var elemento = celda.BorderBottom(0.5f)
            .BorderColor(Colors.Grey.Lighten1)
            .Background(cabecera ? Colors.Grey.Lighten3 : Colors.White)
            .PaddingVertical(cabecera ? 4 : 3)
            .PaddingHorizontal(2)
            .Text(texto);
        if (cabecera)
        {
            elemento.Bold();
        }
    }

    private static string EscaparCsv(string texto) =>
        texto.IndexOfAny([',', '"', '\r', '\n']) < 0 ? texto : $"\"{texto.Replace("\"", "\"\"")}\"";

    private static string Formatear(object? valor, bool esImporte = false, bool invariant = false) => valor switch
    {
        null => string.Empty,
        DateTimeOffset fecha => fecha.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture),
        DateTime fecha => fecha.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture),
        decimal numero => numero.ToString(
            esImporte ? "N2" : "0.###", invariant ? CultureInfo.InvariantCulture : Cultura),
        IFormattable formateable => formateable.ToString(null, CultureInfo.InvariantCulture),
        _ => Convert.ToString(valor, CultureInfo.InvariantCulture) ?? string.Empty
    };

    private static bool EsImporte(string columna) => columna is
        "Subtotal" or "Descuentos" or "Impuestos" or "Total" or "Pagado" or "Devuelto" or "Reintegrado" or
        "Pendiente" or "Importe" or "ValorInventario" or "CostoPromedio" or "PrecioVenta" or "TotalVentas" or
        "TotalPagado" or "TotalDevuelto" or "TotalReintegrado" or "TotalNeto" or "SaldoPendiente" or
        "TotalServicios" or "ComisionesDevengadas" or "AjustesDevolucion" or
        "ComisionesNetas" or "EfectivoInicial" or "PagosVenta" or "IngresosManuales" or "Reembolsos" or
        "EgresosManuales" or "PagoComisiones" or "EfectivoEsperado" or "EfectivoContado" or "Diferencia" or
        "BaseCalculo";

    private static bool EsTextoRiesgoso(string texto) =>
        texto.Length > 0 && texto[0] is '=' or '+' or '-' or '@' or '\t' or '\r' or '\n';
}
