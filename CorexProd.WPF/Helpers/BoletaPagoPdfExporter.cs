using CorexProd.Entidad.Entidades;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace CorexProd.WPF.Helpers
{
    internal static class BoletaPagoPdfExporter
    {
        private const double PageWidth = 595;
        private const double PageHeight = 842;
        private const double PageMarginX = 29;
        private const double PageMarginY = 28;
        private const double SlotGap = 22;
        private const double SlotHeight = (PageHeight - (PageMarginY * 2) - SlotGap) / 2;
        private const double TableWidth = 537;
        private const double TableHeaderHeight = 16;
        private const double TableTotalsReserve = 84;
        private const double TableContinuationReserve = 24;
        private const double RowFontSize = 6.2;
        private const double RowLineHeight = 7.2;

        private static readonly PdfColumn[] Columns =
        [
            new("Fecha", 45, false, 1),
            new("Concepto", 78, false, 2),
            new("Descripcion", 164, false, 2),
            new("Cant.", 38, true, 1),
            new("Tarifa", 42, true, 1),
            new("Ingreso", 57, true, 1),
            new("Desc.", 57, true, 1),
            new("Pago", 56, true, 1)
        ];

        private static readonly BoletaSlot[] Slots =
        [
            new(PageMarginX, PageHeight - PageMarginY - SlotHeight, TableWidth, SlotHeight),
            new(PageMarginX, PageMarginY, TableWidth, SlotHeight)
        ];

        public static void Exportar(
            string ruta,
            PeriodoPago periodo,
            IReadOnlyList<ResumenPagoTrabajador> resumenes,
            IReadOnlyList<MovimientoTrabajador> movimientos)
        {
            SimplePdfDocument document = new();
            BoletaPaginator paginator = new(document);

            foreach (ResumenPagoTrabajador resumen in resumenes.OrderBy(r => r.NombreTrabajador))
            {
                List<MovimientoTrabajador> detalle = movimientos
                    .Where(m => m.IdPeriodoPago == periodo.IdPeriodoPago
                        && m.IdTrabajadorOperativo == resumen.IdTrabajadorOperativo)
                    .OrderBy(m => m.Fecha)
                    .ThenBy(m => m.NombreConcepto)
                    .ToList();

                AgregarBoleta(paginator, periodo, resumen, detalle);
            }

            document.Save(ruta);
        }

        private static void AgregarBoleta(
            BoletaPaginator paginator,
            PeriodoPago periodo,
            ResumenPagoTrabajador resumen,
            IReadOnlyList<MovimientoTrabajador> movimientos)
        {
            List<TableRow> rows = movimientos
                .Select(CrearFila)
                .ToList();

            if (rows.Count == 0)
            {
                rows.Add(new TableRow(
                [
                    string.Empty,
                    "Sin movimientos",
                    "No hay detalle registrado para este trabajador.",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty
                ]));
            }

            int index = 0;
            bool continuacion = false;

            while (index < rows.Count)
            {
                PdfCanvas canvas = paginator.NextSlot(out BoletaSlot slot);
                double tableTop = DibujarEncabezado(canvas, slot, periodo, resumen, continuacion);
                DibujarCabeceraTabla(canvas, slot, tableTop);

                double currentTop = tableTop - TableHeaderHeight;
                int firstIndexInSlot = index;

                while (index < rows.Count)
                {
                    double rowHeight = CalcularAltoFila(rows[index]);
                    double bottomReserve = index == rows.Count - 1
                        ? TableTotalsReserve
                        : TableContinuationReserve;

                    if (currentTop - rowHeight < slot.Bottom + bottomReserve)
                    {
                        break;
                    }

                    DibujarFila(canvas, slot, rows[index], currentTop, rowHeight);
                    currentTop -= rowHeight;
                    index++;
                }

                if (index == firstIndexInSlot && index < rows.Count)
                {
                    double bottomReserve = index == rows.Count - 1
                        ? TableTotalsReserve
                        : TableContinuationReserve;
                    double rowHeight = Math.Min(
                        CalcularAltoFila(rows[index]),
                        currentTop - (slot.Bottom + bottomReserve));

                    DibujarFila(canvas, slot, rows[index], currentTop, rowHeight);
                    index++;
                }

                if (index >= rows.Count)
                {
                    DibujarTotales(canvas, slot, resumen);
                }
                else
                {
                    canvas.Text("Continua en la siguiente boleta...", slot.X + slot.Width - 148, slot.Bottom + 16, 6.5);
                    continuacion = true;
                }
            }
        }

        private static double DibujarEncabezado(
            PdfCanvas canvas,
            BoletaSlot slot,
            PeriodoPago periodo,
            ResumenPagoTrabajador resumen,
            bool continuacion)
        {
            double top = slot.Top;
            string titulo = continuacion ? "BOLETA DE PAGO (CONT.)" : "BOLETA DE PAGO";

            canvas.Rectangle(slot.X, slot.Bottom, slot.Width, slot.Height);
            canvas.Text("CorexProd", slot.X + 8, top - 20, 11, true);
            canvas.Text(titulo, slot.X + slot.Width - 136, top - 20, 10.5, true);
            canvas.Line(slot.X + 8, top - 29, slot.X + slot.Width - 8, top - 29);

            canvas.Text($"Trabajador: {resumen.NombreTrabajador}", slot.X + 8, top - 45, 8.5, true);
            canvas.Text($"Periodo: {periodo.CodigoPeriodo}", slot.X + 8, top - 59, 7.2);
            canvas.Text($"Fechas: {periodo.FechaInicio:dd/MM/yyyy} - {periodo.FechaFin:dd/MM/yyyy}", slot.X + 160, top - 59, 7.2);
            canvas.Text($"Estado: {resumen.EstadoPeriodo}", slot.X + 380, top - 59, 7.2);
            canvas.Text($"Tipo: {resumen.TipoTrabajador}", slot.X + 8, top - 72, 7.2);
            canvas.Text($"Medio de pago: {resumen.MedioPagoPreferido}", slot.X + 160, top - 72, 7.2);

            double boxY = top - 112;
            double gap = 4;
            double boxWidth = (slot.Width - 16 - (gap * 4)) / 5;
            double x = slot.X + 8;

            DibujarCajaTotal(canvas, x, boxY, boxWidth, "Saldo ant.", resumen.SaldoAnterior);
            DibujarCajaTotal(canvas, x + (boxWidth + gap), boxY, boxWidth, "Ingresos", resumen.TotalIngresos);
            DibujarCajaTotal(canvas, x + 2 * (boxWidth + gap), boxY, boxWidth, "Descuentos", resumen.TotalDescuentos);
            DibujarCajaTotal(canvas, x + 3 * (boxWidth + gap), boxY, boxWidth, "Neto", resumen.NetoCalculado);
            DibujarCajaTotal(canvas, x + 4 * (boxWidth + gap), boxY, boxWidth, "Saldo", resumen.SaldoPendiente);

            canvas.Text($"Pagado: {FormatoMoneda(resumen.TotalPagado)}", slot.X + 8, top - 126, 7.2, true);

            return top - 142;
        }

        private static void DibujarCajaTotal(PdfCanvas canvas, double x, double y, double width, string label, decimal value)
        {
            canvas.FillRectangle(x, y, width, 32, 0.95, 0.96, 0.98);
            canvas.Rectangle(x, y, width, 32);
            canvas.Text(label, x + 5, y + 20, 5.8);
            canvas.Text(FormatoMoneda(value), x + 5, y + 7, 8, true);
        }

        private static void DibujarCabeceraTabla(PdfCanvas canvas, BoletaSlot slot, double top)
        {
            canvas.FillRectangle(slot.X, top - TableHeaderHeight, slot.Width, TableHeaderHeight, 0.91, 0.93, 0.96);
            canvas.Rectangle(slot.X, top - TableHeaderHeight, slot.Width, TableHeaderHeight);

            double x = slot.X;

            foreach (PdfColumn column in Columns)
            {
                canvas.Text(column.Header, x + 3, top - 10.5, 6.3, true);
                x += column.Width;
            }
        }

        private static void DibujarFila(PdfCanvas canvas, BoletaSlot slot, TableRow row, double top, double rowHeight)
        {
            canvas.Rectangle(slot.X, top - rowHeight, slot.Width, rowHeight);

            double x = slot.X;

            for (int i = 0; i < Columns.Length; i++)
            {
                PdfColumn column = Columns[i];
                List<string> lines = DividirTexto(row.Cells[i], column.Width, RowFontSize, column.MaxLines);

                for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
                {
                    string line = lines[lineIndex];
                    double textX = column.AlignRight
                        ? x + column.Width - EstimarAnchoTexto(line, RowFontSize) - 3
                        : x + 3;

                    canvas.Text(line, textX, top - 10 - (lineIndex * RowLineHeight), RowFontSize);
                }

                x += column.Width;
            }
        }

        private static void DibujarTotales(PdfCanvas canvas, BoletaSlot slot, ResumenPagoTrabajador resumen)
        {
            double boxX = slot.X + slot.Width - 174;
            double boxY = slot.Bottom + 18;
            double labelX = boxX + 6;
            double valueX = boxX + 158;

            canvas.Rectangle(boxX, boxY, 166, 58);
            DibujarLineaTotal(canvas, "Saldo anterior", resumen.SaldoAnterior, labelX, valueX, boxY + 46);
            DibujarLineaTotal(canvas, "Total ingresos", resumen.TotalIngresos, labelX, valueX, boxY + 37);
            DibujarLineaTotal(canvas, "Total descuentos", resumen.TotalDescuentos, labelX, valueX, boxY + 28);
            DibujarLineaTotal(canvas, "Neto calculado", resumen.NetoCalculado, labelX, valueX, boxY + 19);
            DibujarLineaTotal(canvas, "Total pagado", resumen.TotalPagado, labelX, valueX, boxY + 10);
            DibujarLineaTotal(canvas, "Saldo pendiente", resumen.SaldoPendiente, labelX, valueX, boxY + 1, true);

            double firmaY = slot.Bottom + 19;
            canvas.Line(slot.X + 14, firmaY, slot.X + 150, firmaY);
            canvas.Text("Recibi conforme", slot.X + 44, slot.Bottom + 7, 6.5);
            canvas.Line(slot.X + 174, firmaY, slot.X + 310, firmaY);
            canvas.Text("Responsable", slot.X + 217, slot.Bottom + 7, 6.5);
        }

        private static void DibujarLineaTotal(
            PdfCanvas canvas,
            string label,
            decimal value,
            double labelX,
            double valueX,
            double y,
            bool bold = false)
        {
            string amount = FormatoMoneda(value);
            canvas.Text(label, labelX, y, 6.2, bold);
            canvas.Text(amount, valueX - EstimarAnchoTexto(amount, 6.2), y, 6.2, bold);
        }

        private static TableRow CrearFila(MovimientoTrabajador movimiento)
        {
            bool esPago = movimiento.TipoMovimiento.Equals("Pago", StringComparison.OrdinalIgnoreCase);
            bool esDescuento = movimiento.EsDescuento
                || movimiento.TipoMovimiento.Equals("Descuento", StringComparison.OrdinalIgnoreCase);

            decimal ingreso = !esPago && !esDescuento ? movimiento.Importe : 0;
            decimal descuento = esDescuento ? movimiento.Importe : 0;
            decimal pago = esPago ? movimiento.Importe : 0;

            return new TableRow(
            [
                movimiento.Fecha.ToString("dd/MM/yy", CultureInfo.InvariantCulture),
                movimiento.NombreConcepto,
                movimiento.Descripcion,
                FormatoNumero(movimiento.Cantidad),
                FormatoNumero(movimiento.Tarifa),
                FormatoMonedaVacio(ingreso),
                FormatoMonedaVacio(descuento),
                FormatoMonedaVacio(pago)
            ]);
        }

        private static double CalcularAltoFila(TableRow row)
        {
            int lineCount = 1;

            for (int i = 0; i < Columns.Length; i++)
            {
                lineCount = Math.Max(
                    lineCount,
                    DividirTexto(row.Cells[i], Columns[i].Width, RowFontSize, Columns[i].MaxLines).Count);
            }

            return Math.Max(15, 6 + (lineCount * RowLineHeight));
        }

        private static List<string> DividirTexto(string text, double width, double fontSize, int maxLines)
        {
            string cleaned = NormalizarTexto(text);
            int maxChars = Math.Max(6, (int)Math.Floor((width - 6) / (fontSize * 0.56)));
            List<string> lines = [];

            foreach (string word in cleaned
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .SelectMany(w => DividirPalabra(w, maxChars)))
            {
                if (lines.Count == 0)
                {
                    lines.Add(word);
                    continue;
                }

                string candidate = $"{lines[^1]} {word}";

                if (candidate.Length <= maxChars)
                {
                    lines[^1] = candidate;
                }
                else
                {
                    lines.Add(word);
                }

                if (lines.Count == maxLines)
                {
                    break;
                }
            }

            if (lines.Count == 0)
                lines.Add(string.Empty);

            if (lines.Count == maxLines && cleaned.Length > string.Join(' ', lines).Length)
            {
                string last = lines[^1];
                lines[^1] = last.Length > 3
                    ? $"{last[..Math.Min(last.Length, Math.Max(0, maxChars - 3))]}..."
                    : last;
            }

            return lines;
        }

        private static IEnumerable<string> DividirPalabra(string word, int maxChars)
        {
            if (word.Length <= maxChars)
            {
                yield return word;
                yield break;
            }

            for (int start = 0; start < word.Length; start += maxChars)
            {
                yield return word.Substring(start, Math.Min(maxChars, word.Length - start));
            }
        }

        private static string FormatoMoneda(decimal value)
        {
            return $"S/ {value:N2}";
        }

        private static string FormatoMonedaVacio(decimal value)
        {
            return value == 0 ? string.Empty : FormatoMoneda(value);
        }

        private static string FormatoNumero(decimal value)
        {
            return value == 0 ? string.Empty : value.ToString("N2", CultureInfo.CurrentCulture);
        }

        private static double EstimarAnchoTexto(string text, double fontSize)
        {
            return NormalizarTexto(text).Length * fontSize * 0.52;
        }

        private static string NormalizarTexto(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            string normalized = text.Normalize(NormalizationForm.FormD);
            StringBuilder builder = new();

            foreach (char character in normalized)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);

                if (category == UnicodeCategory.NonSpacingMark)
                    continue;

                if (character is >= ' ' and <= '~')
                {
                    builder.Append(character);
                }
                else
                {
                    builder.Append(' ');
                }
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        private sealed record PdfColumn(string Header, double Width, bool AlignRight, int MaxLines);

        private sealed record TableRow(string[] Cells);

        private sealed record BoletaSlot(double X, double Bottom, double Width, double Height)
        {
            public double Top => Bottom + Height;
        }

        private sealed class BoletaPaginator
        {
            private readonly SimplePdfDocument _document;
            private PdfCanvas? _currentPage;
            private int _slotIndex = Slots.Length;

            public BoletaPaginator(SimplePdfDocument document)
            {
                _document = document;
            }

            public PdfCanvas NextSlot(out BoletaSlot slot)
            {
                if (_currentPage == null || _slotIndex >= Slots.Length)
                {
                    _currentPage = _document.AddPage(PageWidth, PageHeight);
                    _slotIndex = 0;
                }

                slot = Slots[_slotIndex];
                _slotIndex++;

                return _currentPage;
            }
        }

        private sealed class SimplePdfDocument
        {
            private readonly List<PdfCanvas> _pages = [];

            public PdfCanvas AddPage(double width, double height)
            {
                PdfCanvas page = new(width, height);
                _pages.Add(page);
                return page;
            }

            public void Save(string path)
            {
                int pageCount = _pages.Count;
                int objectCount = 4 + (pageCount * 2);
                byte[][] objects = new byte[objectCount + 1][];

                int[] pageObjectIds = new int[pageCount];
                int[] contentObjectIds = new int[pageCount];
                int nextId = 5;

                for (int i = 0; i < pageCount; i++)
                {
                    pageObjectIds[i] = nextId++;
                    contentObjectIds[i] = nextId++;
                }

                string kids = string.Join(" ", pageObjectIds.Select(id => $"{id} 0 R"));

                objects[1] = AsciiObject(1, "<< /Type /Catalog /Pages 2 0 R >>");
                objects[2] = AsciiObject(2, $"<< /Type /Pages /Count {pageCount} /Kids [{kids}] >>");
                objects[3] = AsciiObject(3, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
                objects[4] = AsciiObject(4, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>");

                for (int i = 0; i < pageCount; i++)
                {
                    PdfCanvas page = _pages[i];
                    objects[pageObjectIds[i]] = AsciiObject(
                        pageObjectIds[i],
                        $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {N(page.Width)} {N(page.Height)}] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {contentObjectIds[i]} 0 R >>");

                    objects[contentObjectIds[i]] = StreamObject(contentObjectIds[i], page.Content);
                }

                using FileStream stream = File.Create(path);
                WriteAscii(stream, "%PDF-1.4\n");

                long[] offsets = new long[objectCount + 1];

                for (int id = 1; id <= objectCount; id++)
                {
                    offsets[id] = stream.Position;
                    stream.Write(objects[id]);
                }

                long xrefOffset = stream.Position;
                WriteAscii(stream, $"xref\n0 {objectCount + 1}\n");
                WriteAscii(stream, "0000000000 65535 f \n");

                for (int id = 1; id <= objectCount; id++)
                {
                    WriteAscii(stream, $"{offsets[id]:0000000000} 00000 n \n");
                }

                WriteAscii(stream, $"trailer\n<< /Size {objectCount + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");
            }

            private static byte[] AsciiObject(int id, string body)
            {
                return Encoding.ASCII.GetBytes($"{id} 0 obj\n{body}\nendobj\n");
            }

            private static byte[] StreamObject(int id, string content)
            {
                byte[] contentBytes = Encoding.ASCII.GetBytes(content);
                byte[] header = Encoding.ASCII.GetBytes($"{id} 0 obj\n<< /Length {contentBytes.Length} >>\nstream\n");
                byte[] footer = Encoding.ASCII.GetBytes("endstream\nendobj\n");

                using MemoryStream stream = new();
                stream.Write(header);
                stream.Write(contentBytes);
                stream.Write(footer);
                return stream.ToArray();
            }

            private static void WriteAscii(Stream stream, string value)
            {
                stream.Write(Encoding.ASCII.GetBytes(value));
            }
        }

        private sealed class PdfCanvas
        {
            private readonly StringBuilder _content = new();

            public PdfCanvas(double width, double height)
            {
                Width = width;
                Height = height;
            }

            public double Width { get; }

            public double Height { get; }

            public string Content => _content.ToString();

            public void Text(string text, double x, double y, double size, bool bold = false)
            {
                _content.Append("0 0 0 rg ");
                _content.Append("BT /");
                _content.Append(bold ? "F2" : "F1");
                _content.Append(' ');
                _content.Append(N(size));
                _content.Append(" Tf ");
                _content.Append(N(x));
                _content.Append(' ');
                _content.Append(N(y));
                _content.Append(" Td ");
                _content.Append(PdfString(text));
                _content.Append(" Tj ET\n");
            }

            public void Line(double x1, double y1, double x2, double y2)
            {
                _content.Append("0.82 0.85 0.9 RG ");
                _content.Append(N(x1));
                _content.Append(' ');
                _content.Append(N(y1));
                _content.Append(" m ");
                _content.Append(N(x2));
                _content.Append(' ');
                _content.Append(N(y2));
                _content.Append(" l S\n");
            }

            public void Rectangle(double x, double y, double width, double height)
            {
                _content.Append("0.82 0.85 0.9 RG ");
                AppendRectangle(x, y, width, height);
                _content.Append("S\n");
            }

            public void FillRectangle(double x, double y, double width, double height, double r, double g, double b)
            {
                _content.Append(N(r));
                _content.Append(' ');
                _content.Append(N(g));
                _content.Append(' ');
                _content.Append(N(b));
                _content.Append(" rg ");
                AppendRectangle(x, y, width, height);
                _content.Append("f\n");
            }

            private void AppendRectangle(double x, double y, double width, double height)
            {
                _content.Append(N(x));
                _content.Append(' ');
                _content.Append(N(y));
                _content.Append(' ');
                _content.Append(N(width));
                _content.Append(' ');
                _content.Append(N(height));
                _content.Append(" re ");
            }
        }

        private static string PdfString(string text)
        {
            string cleaned = NormalizarTexto(text);
            StringBuilder builder = new("(");

            foreach (char character in cleaned)
            {
                if (character is '(' or ')' or '\\')
                {
                    builder.Append('\\');
                }

                builder.Append(character);
            }

            builder.Append(')');
            return builder.ToString();
        }

        private static string N(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
