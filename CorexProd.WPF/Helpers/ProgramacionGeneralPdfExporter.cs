using CorexProd.Entidad.Entidades;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace CorexProd.WPF.Helpers
{
    public static class ProgramacionGeneralPdfExporter
    {
        private const double PageWidth = 595;
        private const double PageHeight = 842;
        private const double Margin = 26;
        private const double ContentWidth = PageWidth - (Margin * 2);
        private const double HeaderStartY = 798;
        private const double FirstContentY = 742;
        private const double ContinuationContentY = 762;
        private const double FooterLimitY = 32;
        private const double SectionHeaderHeight = 17;
        private const double TableHeaderHeight = 17;
        private const double TotalRowHeight = 16;
        private const double EmptyRowHeight = 20;
        private const double FontSize = 6.2;
        private const double HeaderFontSize = 6.2;
        private static readonly CultureInfo Cultura = new("es-PE");
        private static readonly string[] Meses =
        [
            "ENERO", "FEBRERO", "MARZO", "ABRIL", "MAYO", "JUNIO",
            "JULIO", "AGOSTO", "SETIEMBRE", "OCTUBRE", "NOVIEMBRE", "DICIEMBRE"
        ];

        public static void Exportar(
            string ruta,
            IEnumerable<ProgramacionPagoGeneralItem> pagos,
            DateTime fechaDesde,
            DateTime fechaHasta,
            bool incluyePendientesAnteriores)
        {
            if (string.IsNullOrWhiteSpace(ruta))
                throw new ArgumentException("Debe indicar la ruta del archivo.");

            List<ProgramacionPagoGeneralItem> lista = pagos
                .Where(p => EsCategoriaSoportada(p.Categoria))
                .OrderBy(p => p.FechaVencimiento)
                .ThenBy(p => p.Categoria)
                .ThenBy(p => p.EntidadProveedor)
                .ThenBy(p => p.NumeroCuota ?? 0)
                .ToList();

            List<ProgramacionPagoGeneralItem> facturasCredito = lista.Where(p => EsFacturaCredito(p.Categoria)).ToList();
            List<ProgramacionPagoGeneralItem> letras = lista.Where(p => EsLetra(p.Categoria)).ToList();
            List<ProgramacionPagoGeneralItem> otrasObligaciones = lista.Where(p => EsOtraObligacion(p.Categoria)).ToList();

            ProformaPdfExporter.SimplePdfDocument document = new();
            int pagina = 1;
            ProformaPdfExporter.PdfCanvas canvas = NuevaPagina(document, fechaDesde, fechaHasta, pagina, cabeceraCompleta: true);
            double y = FirstContentY;

            DibujarBloqueSuperior(document, ref canvas, fechaDesde, fechaHasta, ref pagina, ref y, "FACTURAS A CREDITO", facturasCredito, "SIN FACTURAS A CREDITO PARA EL RANGO");
            y -= 8;
            DibujarBloqueSuperior(document, ref canvas, fechaDesde, fechaHasta, ref pagina, ref y, "LETRAS POR PAGAR", letras, "SIN LETRAS POR PAGAR PARA EL RANGO");
            y -= 8;
            DibujarBloqueOtras(document, ref canvas, fechaDesde, fechaHasta, incluyePendientesAnteriores, ref pagina, ref y, otrasObligaciones);
            y -= 4;
            DibujarResumenGeneral(document, ref canvas, fechaDesde, fechaHasta, ref pagina, ref y, lista);

            Directory.CreateDirectory(Path.GetDirectoryName(ruta) ?? ".");
            document.Save(ruta);
        }

        private static ProformaPdfExporter.PdfCanvas NuevaPagina(
            ProformaPdfExporter.SimplePdfDocument document,
            DateTime fechaDesde,
            DateTime fechaHasta,
            int pagina,
            bool cabeceraCompleta)
        {
            ProformaPdfExporter.PdfCanvas canvas = document.AddPage(PageWidth, PageHeight);
            if (cabeceraCompleta)
            {
                string anio = fechaDesde.Year == fechaHasta.Year ? fechaDesde.Year.ToString() : $"{fechaDesde.Year}-{fechaHasta.Year}";
                canvas.CenterText(Limpiar($"PROGRAMACION GENERAL DE PAGOS {anio}"), PageWidth / 2, HeaderStartY, 15, true, 15, 23, 42);
                canvas.Text(Limpiar($"Periodo: {FormatearFecha(fechaDesde)} al {FormatearFecha(fechaHasta)}"), Margin, 774, 8.5, true, 15, 23, 42);
                canvas.RightText(Limpiar($"Emitido: {DateTime.Now:dd/MM/yyyy HH:mm}"), PageWidth - Margin, 774, 8, true, 71, 85, 105);
                canvas.Line(Margin, 762, PageWidth - Margin, 762, 148, 163, 184);
            }
            else
            {
                canvas.Text("PROGRAMACION GENERAL DE PAGOS", Margin, 800, 10, true, 15, 23, 42);
                canvas.RightText(Limpiar($"Periodo: {FormatearFecha(fechaDesde)} al {FormatearFecha(fechaHasta)}"), PageWidth - Margin, 800, 7.2, true, 71, 85, 105);
                canvas.Line(Margin, 790, PageWidth - Margin, 790, 148, 163, 184);
            }

            canvas.Text($"Pagina {pagina}", PageWidth - Margin - 45, 18, 7, false, 100, 116, 139);
            return canvas;
        }

        private static void DibujarBloqueSuperior(
            ProformaPdfExporter.SimplePdfDocument document,
            ref ProformaPdfExporter.PdfCanvas canvas,
            DateTime fechaDesde,
            DateTime fechaHasta,
            ref int pagina,
            ref double y,
            string titulo,
            IReadOnlyList<ProgramacionPagoGeneralItem> pagos,
            string mensajeSinDatos)
        {
            double[] widths = [96, 36, 55, 62, 52, 116, 36, 82];
            string[] headers = ["PROVEEDOR", "MON.", "IMPORTE", "REFERENCIA", "FECHA DOC.", "CONCEPTO", "CUOTA", "VENCIMIENTO"];

            AsegurarEspacio(document, ref canvas, fechaDesde, fechaHasta, ref pagina, ref y, SectionHeaderHeight + TableHeaderHeight + (pagos.Count == 0 ? EmptyRowHeight : 18), titulo, headers, widths);
            DibujarTituloSeccion(canvas, ref y, titulo);
            DibujarEncabezadoTabla(canvas, ref y, headers, widths);

            if (pagos.Count == 0)
            {
                DibujarMensajeVacio(canvas, ref y, mensajeSinDatos);
                return;
            }

            foreach (ProgramacionPagoGeneralItem pago in pagos)
            {
                string[] valores =
                [
                    pago.EntidadProveedor,
                    ObtenerMonedaTexto(pago.Moneda),
                    FormatoImporte(pago.Importe),
                    pago.Referencia,
                    pago.FechaDocumento.HasValue ? FormatearFecha(pago.FechaDocumento.Value) : string.Empty,
                    pago.Concepto,
                    ObtenerCuotaTexto(pago),
                    FormatearFecha(pago.FechaVencimiento)
                ];

                double rowHeight = CalcularAltoFila(canvas, valores, widths, [0, 3, 5], FontSize);
                AsegurarEspacio(document, ref canvas, fechaDesde, fechaHasta, ref pagina, ref y, rowHeight, titulo, headers, widths);
                DibujarFila(canvas, ref y, valores, widths, rowHeight, pago, [2]);
            }

            DibujarTotalesMoneda(document, ref canvas, fechaDesde, fechaHasta, ref pagina, ref y, titulo, headers, widths, pagos);
        }

        private static void DibujarBloqueOtras(
            ProformaPdfExporter.SimplePdfDocument document,
            ref ProformaPdfExporter.PdfCanvas canvas,
            DateTime fechaDesde,
            DateTime fechaHasta,
            bool incluyePendientesAnteriores,
            ref int pagina,
            ref double y,
            IReadOnlyList<ProgramacionPagoGeneralItem> pagos)
        {
            double[] widths = [150, 62, 55, 44, 60, 65, 99];
            string[] headers = ["ENTIDAD / CONCEPTO", "IMPORTE", "BANCO", "CUOTA", "DIA", "VENCIMIENTO", "N UNICO"];
            const string titulo = "OTRAS OBLIGACIONES";

            AsegurarEspacio(document, ref canvas, fechaDesde, fechaHasta, ref pagina, ref y, SectionHeaderHeight + TableHeaderHeight + (pagos.Count == 0 ? EmptyRowHeight : 18), titulo, headers, widths);
            DibujarTituloSeccion(canvas, ref y, titulo);

            if (pagos.Count == 0)
            {
                DibujarEncabezadoTabla(canvas, ref y, headers, widths);
                DibujarMensajeVacio(canvas, ref y, "SIN OTRAS OBLIGACIONES PARA EL RANGO");
                return;
            }

            foreach (IGrouping<string, ProgramacionPagoGeneralItem> grupoMes in pagos.GroupBy(p => $"{p.FechaVencimiento:yyyy-MM}").OrderBy(g => g.Min(p => p.FechaVencimiento)))
            {
                DateTime fechaMes = grupoMes.Min(p => p.FechaVencimiento);
                AsegurarEspacio(document, ref canvas, fechaDesde, fechaHasta, ref pagina, ref y, 15 + TableHeaderHeight + 18, titulo, headers, widths);
                DibujarSubtitulo(canvas, ref y, $"(VENCIMIENTO: {Meses[fechaMes.Month - 1]} {fechaMes:yyyy})");

                foreach (IGrouping<DateTime, ProgramacionPagoGeneralItem> grupoSemana in grupoMes.GroupBy(p => ObtenerInicioSemana(p.FechaVencimiento)).OrderBy(g => g.Key))
                {
                    DateTime inicioSemana = grupoSemana.Key;
                    DateTime finSemana = inicioSemana.AddDays(6);
                    string tituloSemana = incluyePendientesAnteriores && grupoSemana.Any(p => p.FechaVencimiento < fechaDesde)
                        ? "PENDIENTES ANTERIORES"
                        : $"SEMANA {inicioSemana:dd/MM/yyyy} AL {finSemana:dd/MM/yyyy}";

                    AsegurarEspacio(document, ref canvas, fechaDesde, fechaHasta, ref pagina, ref y, 15 + TableHeaderHeight + 18, titulo, headers, widths);
                    DibujarFranja(canvas, ref y, tituloSemana);
                    DibujarEncabezadoTabla(canvas, ref y, headers, widths);

                    foreach (ProgramacionPagoGeneralItem pago in grupoSemana.OrderBy(p => p.FechaVencimiento).ThenBy(p => p.EntidadProveedor))
                    {
                        string[] valores =
                        [
                            ProgramacionGeneralExcelExporter.ObtenerEntidadOtraObligacion(pago),
                            FormatoImporte(pago.Importe),
                            string.Empty,
                            ObtenerCuotaTexto(pago),
                            Cultura.TextInfo.ToTitleCase(Cultura.DateTimeFormat.GetDayName(pago.FechaVencimiento.DayOfWeek)),
                            FormatearFecha(pago.FechaVencimiento),
                            string.Empty
                        ];

                        double rowHeight = CalcularAltoFila(canvas, valores, widths, [0], FontSize);
                        AsegurarEspacio(document, ref canvas, fechaDesde, fechaHasta, ref pagina, ref y, rowHeight, titulo, headers, widths);
                        DibujarFila(canvas, ref y, valores, widths, rowHeight, pago, [1]);
                    }

                    DibujarTotalesMoneda(document, ref canvas, fechaDesde, fechaHasta, ref pagina, ref y, titulo, headers, widths, grupoSemana.ToList());
                    y -= 4;
                }
            }
        }

        private static void DibujarResumenGeneral(
            ProformaPdfExporter.SimplePdfDocument document,
            ref ProformaPdfExporter.PdfCanvas canvas,
            DateTime fechaDesde,
            DateTime fechaHasta,
            ref int pagina,
            ref double y,
            IReadOnlyList<ProgramacionPagoGeneralItem> pagos)
        {
            if (pagos.Count == 0)
                return;

            double[] widths = [170, 82, 283];
            string[] headers = ["RESUMEN", "IMPORTE", string.Empty];
            AsegurarEspacio(document, ref canvas, fechaDesde, fechaHasta, ref pagina, ref y, SectionHeaderHeight + TotalRowHeight, "RESUMEN GENERAL", headers, widths, repetirEncabezado: false);
            DibujarTituloSeccion(canvas, ref y, "RESUMEN GENERAL");

            foreach (IGrouping<string, ProgramacionPagoGeneralItem> totalMoneda in pagos.GroupBy(p => NormalizarMoneda(p.Moneda)).OrderBy(g => g.Key))
            {
                AsegurarEspacio(document, ref canvas, fechaDesde, fechaHasta, ref pagina, ref y, TotalRowHeight, "RESUMEN GENERAL", headers, widths, repetirEncabezado: false);
                DibujarTotal(canvas, ref y, $"TOTAL GENERAL {totalMoneda.Key}", totalMoneda.Sum(p => p.Importe), widths);
            }
        }

        private static void DibujarTotalesMoneda(
            ProformaPdfExporter.SimplePdfDocument document,
            ref ProformaPdfExporter.PdfCanvas canvas,
            DateTime fechaDesde,
            DateTime fechaHasta,
            ref int pagina,
            ref double y,
            string titulo,
            string[] headers,
            double[] widths,
            IReadOnlyCollection<ProgramacionPagoGeneralItem> pagos)
        {
            foreach (IGrouping<string, ProgramacionPagoGeneralItem> totalMoneda in pagos.GroupBy(p => NormalizarMoneda(p.Moneda)).OrderBy(g => g.Key))
            {
                AsegurarEspacio(document, ref canvas, fechaDesde, fechaHasta, ref pagina, ref y, TotalRowHeight, titulo, headers, widths);
                DibujarTotal(canvas, ref y, $"TOTAL {totalMoneda.Key}", totalMoneda.Sum(p => p.Importe), widths);
            }
        }

        private static void AsegurarEspacio(
            ProformaPdfExporter.SimplePdfDocument document,
            ref ProformaPdfExporter.PdfCanvas canvas,
            DateTime fechaDesde,
            DateTime fechaHasta,
            ref int pagina,
            ref double y,
            double requerido,
            string titulo,
            string[] headers,
            double[] widths,
            bool repetirEncabezado = true)
        {
            if (y - requerido >= FooterLimitY)
                return;

            pagina++;
            canvas = NuevaPagina(document, fechaDesde, fechaHasta, pagina, cabeceraCompleta: false);
            y = ContinuationContentY;

            if (repetirEncabezado)
            {
                DibujarTituloSeccion(canvas, ref y, $"{titulo} (CONT.)");
                DibujarEncabezadoTabla(canvas, ref y, headers, widths);
            }
        }

        private static void DibujarTituloSeccion(ProformaPdfExporter.PdfCanvas canvas, ref double y, string titulo)
        {
            canvas.FilledRectangle(Margin, y - SectionHeaderHeight, ContentWidth, SectionHeaderHeight, 182, 215, 168);
            canvas.Text(Limpiar(titulo), Margin + 6, y - 11.5, 8.2, true, 15, 23, 42);
            y -= SectionHeaderHeight;
        }

        private static void DibujarSubtitulo(ProformaPdfExporter.PdfCanvas canvas, ref double y, string titulo)
        {
            canvas.FilledRectangle(Margin, y - 15, ContentWidth, 15, 182, 215, 168);
            canvas.Text(Limpiar(titulo), Margin + 6, y - 10.5, 7.4, true, 15, 23, 42);
            y -= 15;
        }

        private static void DibujarFranja(ProformaPdfExporter.PdfCanvas canvas, ref double y, string titulo)
        {
            canvas.FilledRectangle(Margin, y - 15, ContentWidth, 15, 226, 240, 217);
            canvas.Text(Limpiar(titulo), Margin + 6, y - 10.5, 7, true, 15, 23, 42);
            y -= 15;
        }

        private static void DibujarEncabezadoTabla(ProformaPdfExporter.PdfCanvas canvas, ref double y, string[] headers, double[] widths)
        {
            canvas.FilledRectangle(Margin, y - TableHeaderHeight, ContentWidth, TableHeaderHeight, 217, 226, 243);
            double x = Margin;
            for (int i = 0; i < headers.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(headers[i]))
                    canvas.CenterText(Limpiar(headers[i]), x + (widths[i] / 2), y - 11.2, HeaderFontSize, true, 15, 23, 42);
                x += widths[i];
            }
            y -= TableHeaderHeight;
        }

        private static void DibujarFila(
            ProformaPdfExporter.PdfCanvas canvas,
            ref double y,
            string[] valores,
            double[] widths,
            double rowHeight,
            ProgramacionPagoGeneralItem pago,
            IReadOnlyCollection<int> columnasNumericas)
        {
            if (pago.Estado.Equals("CANCELADA", StringComparison.OrdinalIgnoreCase))
                canvas.FilledRectangle(Margin, y - rowHeight, ContentWidth, rowHeight, 196, 215, 155);
            else
                canvas.Rectangle(Margin, y - rowHeight, ContentWidth, rowHeight, 226, 232, 240);

            double x = Margin;
            for (int i = 0; i < valores.Length; i++)
            {
                List<string> lineas = EnvolverTexto(canvas, Limpiar(valores[i]), widths[i] - 6, FontSize).Take(3).ToList();
                double textY = y - 10.5;
                if (columnasNumericas.Contains(i))
                {
                    canvas.RightText(lineas.FirstOrDefault() ?? string.Empty, x + widths[i] - 4, textY, FontSize);
                }
                else
                {
                    for (int line = 0; line < lineas.Count; line++)
                        canvas.Text(lineas[line], x + 3, textY - (line * 7.2), FontSize);
                }

                x += widths[i];
            }

            y -= rowHeight;
        }

        private static void DibujarMensajeVacio(ProformaPdfExporter.PdfCanvas canvas, ref double y, string mensaje)
        {
            canvas.Rectangle(Margin, y - EmptyRowHeight, ContentWidth, EmptyRowHeight, 226, 232, 240);
            canvas.CenterText(Limpiar(mensaje), PageWidth / 2, y - 13, 7, false, 100, 116, 139);
            y -= EmptyRowHeight;
        }

        private static void DibujarTotal(ProformaPdfExporter.PdfCanvas canvas, ref double y, string etiqueta, decimal total, double[] widths)
        {
            canvas.Rectangle(Margin, y - TotalRowHeight, ContentWidth, TotalRowHeight, 71, 85, 105);
            canvas.Text(Limpiar(etiqueta), Margin + 6, y - 10.8, 7, true, 15, 23, 42);
            double rightX = Margin + widths.Take(Math.Min(3, widths.Length)).Sum() - 5;
            canvas.RightText(FormatoImporte(total), rightX, y - 10.8, 7, true, 15, 23, 42);
            y -= TotalRowHeight;
        }

        private static double CalcularAltoFila(
            ProformaPdfExporter.PdfCanvas canvas,
            string[] valores,
            double[] widths,
            IReadOnlyCollection<int> columnasWrap,
            double fontSize)
        {
            int maxLineas = 1;
            foreach (int index in columnasWrap)
            {
                if (index < valores.Length)
                    maxLineas = Math.Max(maxLineas, Math.Min(3, EnvolverTexto(canvas, Limpiar(valores[index]), widths[index] - 6, fontSize).Count));
            }

            return Math.Max(17, 9 + (maxLineas * 7.2));
        }

        private static List<string> EnvolverTexto(ProformaPdfExporter.PdfCanvas canvas, string texto, double maxWidth, double fontSize)
        {
            List<string> lineas = [];
            if (string.IsNullOrWhiteSpace(texto))
            {
                lineas.Add(string.Empty);
                return lineas;
            }

            StringBuilder linea = new();
            foreach (string palabra in texto.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                string candidata = linea.Length == 0 ? palabra : $"{linea} {palabra}";
                if (canvas.MeasureText(candidata, fontSize) <= maxWidth)
                {
                    linea.Clear();
                    linea.Append(candidata);
                    continue;
                }

                if (linea.Length > 0)
                {
                    lineas.Add(linea.ToString());
                    linea.Clear();
                }

                linea.Append(palabra);
            }

            if (linea.Length > 0)
                lineas.Add(linea.ToString());

            return lineas;
        }

        private static string FormatoImporte(decimal value) => value.ToString("N2", Cultura);

        private static string ObtenerMonedaTexto(string moneda)
        {
            return NormalizarMoneda(moneda) switch
            {
                "PEN" => "Soles",
                "USD" => "US$",
                _ => NormalizarMoneda(moneda)
            };
        }

        private static string ObtenerCuotaTexto(ProgramacionPagoGeneralItem pago)
        {
            if (EsTarjeta(pago.Categoria))
                return string.Empty;

            if (!pago.NumeroCuota.HasValue || !pago.TotalCuotas.HasValue)
                return string.Empty;

            return EsLetra(pago.Categoria) || EsFacturaCredito(pago.Categoria)
                ? $"{pago.NumeroCuota}/{pago.TotalCuotas}"
                : $"{pago.NumeroCuota} de {pago.TotalCuotas}";
        }

        private static DateTime ObtenerInicioSemana(DateTime fecha)
        {
            int diferencia = ((int)fecha.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            return fecha.Date.AddDays(-diferencia);
        }

        private static string FormatearFecha(DateTime fecha)
        {
            string texto = fecha.ToString("d-MMM-yy", Cultura);
            texto = texto.Replace("septiembre", "Set", StringComparison.OrdinalIgnoreCase)
                .Replace("sept", "Set", StringComparison.OrdinalIgnoreCase)
                .Replace("sep", "Set", StringComparison.OrdinalIgnoreCase)
                .Replace("sept.", "Set", StringComparison.OrdinalIgnoreCase)
                .Replace("set.", "Set", StringComparison.OrdinalIgnoreCase)
                .Replace(".", string.Empty);
            return Cultura.TextInfo.ToTitleCase(texto);
        }

        private static string NormalizarMoneda(string moneda)
        {
            return string.IsNullOrWhiteSpace(moneda) ? "SIN MONEDA" : moneda.Trim().ToUpperInvariant();
        }

        private static bool EsCategoriaSoportada(string categoria)
        {
            return EsFacturaCredito(categoria) || EsLetra(categoria) || EsOtraObligacion(categoria);
        }

        private static bool EsFacturaCredito(string categoria)
        {
            return categoria.Equals("FACTURA_CREDITO", StringComparison.OrdinalIgnoreCase);
        }

        private static bool EsLetra(string categoria)
        {
            return categoria.Equals("LETRA", StringComparison.OrdinalIgnoreCase);
        }

        private static bool EsTarjeta(string categoria)
        {
            return categoria.Equals("TARJETA", StringComparison.OrdinalIgnoreCase);
        }

        private static bool EsOtraObligacion(string categoria)
        {
            return categoria.Equals("PRESTAMO", StringComparison.OrdinalIgnoreCase)
                || EsTarjeta(categoria)
                || categoria.Equals("SEGURO", StringComparison.OrdinalIgnoreCase)
                || categoria.Equals("OTRO", StringComparison.OrdinalIgnoreCase);
        }

        private static string Limpiar(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            string normalized = text.Normalize(NormalizationForm.FormD);
            StringBuilder builder = new();
            foreach (char c in normalized)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category == UnicodeCategory.NonSpacingMark)
                    continue;

                builder.Append(c switch
                {
                    'ñ' => 'n',
                    'Ñ' => 'N',
                    '°' => ' ',
                    _ => c <= 127 ? c : ' '
                });
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
