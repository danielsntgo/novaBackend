using System.Globalization;
using FlexPos.Application.Interfaces;
using FlexPos.Domain.Entities;
using FlexPos.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FlexPos.Infrastructure.Services;

public sealed class GeneradorDocumentoVenta : IGeneradorDocumentoVenta
{
    public GeneradorDocumentoVenta()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Generar(Venta venta, DocumentoVenta documento)
    {
        var cultura = CultureInfo.GetCultureInfo("es-CO");
        var esFactura = documento.TipoDocumento == TipoDocumentoVenta.Factura;
        return Document.Create(documentoPdf =>
        {
            documentoPdf.Page(pagina =>
            {
                pagina.Size(PageSizes.A4);
                pagina.Margin(38);
                pagina.DefaultTextStyle(estilo => estilo.FontSize(9).FontColor(Colors.Grey.Darken4));
                pagina.Header().Column(columna =>
                {
                    columna.Item().Text(documento.NombreComercial).FontSize(18).Bold();
                    if (!string.IsNullOrWhiteSpace(documento.RazonSocial))
                    {
                        columna.Item().Text(documento.RazonSocial).FontSize(10);
                    }

                    var identificacion = string.IsNullOrWhiteSpace(documento.IdentificacionFiscal)
                        ? null
                        : $"NIT/ID: {documento.IdentificacionFiscal}";
                    var contacto = string.Join(" | ",
                        new[] { documento.Telefono, documento.Correo }.Where(x => !string.IsNullOrWhiteSpace(x)));
                    if (identificacion is not null)
                    {
                        columna.Item().Text(identificacion);
                    }

                    if (!string.IsNullOrWhiteSpace(documento.Direccion))
                    {
                        columna.Item().Text(documento.Direccion);
                    }

                    if (!string.IsNullOrWhiteSpace(contacto))
                    {
                        columna.Item().Text(contacto);
                    }

                    columna.Item().PaddingTop(12).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    columna.Item().PaddingTop(8).Row(fila =>
                    {
                        fila.RelativeItem().Column(datos =>
                        {
                            datos.Item().Text(esFactura ? "FACTURA DE VENTA" : "COMPROBANTE DE VENTA")
                                .FontSize(14).Bold();
                            datos.Item().Text($"Número: {documento.NumeroCompleto}").Bold();
                            datos.Item().Text(
                                $"Fecha: {documento.FechaEmisionUtc.ToUniversalTime().ToString("dd/MM/yyyy HH:mm 'UTC'", CultureInfo.InvariantCulture)}");
                        });
                        fila.ConstantItem(170).AlignRight().Column(datos =>
                        {
                            datos.Item().Text($"Moneda: {documento.CodigoMoneda}");
                            if (!string.IsNullOrWhiteSpace(documento.ClienteNombre))
                            {
                                datos.Item().Text($"Cliente: {documento.ClienteNombre}");
                            }

                            if (!string.IsNullOrWhiteSpace(documento.ClienteDocumento))
                            {
                                datos.Item().Text($"Documento: {documento.ClienteDocumento}");
                            }
                        });
                    });
                });

                pagina.Content().PaddingVertical(16).Column(columna =>
                {
                    columna.Item().Table(tabla =>
                    {
                        tabla.ColumnsDefinition(columnas =>
                        {
                            columnas.RelativeColumn(4);
                            columnas.ConstantColumn(52);
                            columnas.ConstantColumn(82);
                            columnas.ConstantColumn(92);
                        });
                        tabla.Header(cabecera =>
                        {
                            CeldaTabla(cabecera.Cell(), "Descripción", true);
                            CeldaTabla(cabecera.Cell(), "Cant.", true);
                            CeldaTabla(cabecera.Cell(), "Precio", true, true);
                            CeldaTabla(cabecera.Cell(), "Total", true, true);
                        });

                        foreach (var linea in venta.Detalles)
                        {
                            tabla.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                .PaddingVertical(6).Column(celda =>
                                {
                                    celda.Item().Text(linea.Nombre).Bold();
                                    if (!string.IsNullOrWhiteSpace(linea.Codigo))
                                    {
                                        celda.Item().Text(linea.Codigo).FontSize(8).FontColor(Colors.Grey.Darken1);
                                    }

                                    if (linea.DescuentoImporte + linea.DescuentoGeneralImporte > 0m)
                                    {
                                        celda.Item().Text(
                                            $"Descuento: {Formato(linea.DescuentoImporte + linea.DescuentoGeneralImporte, cultura)}")
                                            .FontSize(8);
                                    }

                                    if (linea.ImpuestoPorcentaje.HasValue)
                                    {
                                        celda.Item().Text(
                                            $"{linea.ImpuestoNombre} {linea.ImpuestoPorcentaje.Value:0.##}%: {Formato(linea.ImpuestoImporte, cultura)}")
                                            .FontSize(8);
                                    }
                                });
                            CeldaTabla(tabla.Cell(), $"{linea.Cantidad:0.###} {linea.Unidad}");
                            CeldaTabla(tabla.Cell(), Formato(linea.PrecioUnitario, cultura), alinearDerecha: true);
                            CeldaTabla(tabla.Cell(), Formato(linea.TotalImporte, cultura), alinearDerecha: true);
                        }
                    });

                    columna.Item().PaddingTop(16).AlignRight().Width(260).Column(resumen =>
                    {
                        FilaTotal(resumen, "Subtotal", venta.Subtotal, cultura);
                        if (venta.DescuentoLineas + venta.DescuentoGeneral > 0m)
                        {
                            FilaTotal(resumen, "Descuentos", -(venta.DescuentoLineas + venta.DescuentoGeneral), cultura);
                        }

                        FilaTotal(resumen, "Impuestos", venta.Impuestos, cultura);
                        resumen.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Darken1);
                        FilaTotal(resumen, "TOTAL", venta.Total, cultura, resaltar: true);
                        FilaTotal(resumen, "Pagado", venta.TotalPagado - venta.TotalReintegrado, cultura);
                        FilaTotal(resumen, "Saldo pendiente", venta.TotalPendiente, cultura);
                    });

                    if (venta.Pagos.Count > 0)
                    {
                        columna.Item().PaddingTop(18).Text("Pagos").Bold();
                        foreach (var pago in venta.Pagos.OrderBy(x => x.FechaCreacionUtc))
                        {
                            columna.Item().Text(
                                $"{pago.MetodoPagoNombre}: {Formato(pago.Importe, cultura)}" +
                                (string.IsNullOrWhiteSpace(pago.Referencia) ? string.Empty : $" ({pago.Referencia})"));
                        }
                    }
                });

                pagina.Footer().Column(columna =>
                {
                    if (esFactura)
                    {
                        columna.Item().AlignCenter().Text(
                            "Documento interno de venta. No reemplaza la factura electrónica exigida por la DIAN.")
                            .FontSize(8).FontColor(Colors.Grey.Darken1);
                    }

                    columna.Item().PaddingTop(5).AlignCenter().Text(
                        $"{documento.NombreComercial} | {documento.NumeroCompleto}")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            });
        }).GeneratePdf();
    }

    private static void CeldaTabla(
        IContainer contenedor,
        string texto,
        bool cabecera = false,
        bool alinearDerecha = false)
    {
        var celda = contenedor
            .Background(cabecera ? Colors.Grey.Lighten3 : Colors.White)
            .BorderBottom(cabecera ? 1 : 0.5f)
            .BorderColor(Colors.Grey.Lighten1)
            .PaddingVertical(cabecera ? 6 : 5)
            .PaddingHorizontal(3);
        var textoCelda = alinearDerecha ? celda.AlignRight().Text(texto) : celda.Text(texto);
        if (cabecera)
        {
            textoCelda.Bold();
        }
    }

    private static void FilaTotal(
        ColumnDescriptor columna,
        string etiqueta,
        decimal importe,
        CultureInfo cultura,
        bool resaltar = false) =>
        columna.Item().PaddingVertical(2).Row(fila =>
        {
            var textoEtiqueta = fila.RelativeItem().Text(etiqueta);
            var textoImporte = fila.ConstantItem(120).AlignRight().Text(Formato(importe, cultura));
            if (resaltar)
            {
                textoEtiqueta.Bold();
                textoImporte.Bold();
            }
        });

    private static string Formato(decimal importe, CultureInfo cultura) =>
        $"{importe.ToString("N2", cultura)}";
}
