using CorexProd.Entidad.Entidades;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace CorexProd.WPF.Helpers
{
    internal static class ProformaPdfExporter
    {
        private const double PageWidth = 595;
        private const double PageHeight = 842;
        private const double Margin = 36;

        public static void Exportar(string ruta, Empresa empresa, Proforma proforma)
        {
            SimplePdfDocument document = new();
            PdfCanvas canvas = document.AddPage(PageWidth, PageHeight);

            double y = PageHeight - Margin;

            DibujarCabecera(canvas, empresa, proforma, ref y);
            DibujarCliente(canvas, proforma, ref y);
            DibujarDetalle(canvas, proforma, ref y);
            DibujarTotales(canvas, proforma, ref y);
            DibujarObservacion(canvas, proforma, ref y);

            document.Save(ruta);
        }

        private static void DibujarCabecera(PdfCanvas canvas, Empresa empresa, Proforma proforma, ref double y)
        {
            string nombreEmpresa = string.IsNullOrWhiteSpace(empresa.NombreComercial) ? empresa.Nombre : empresa.NombreComercial;
            string ubicacion = UnirPartes(empresa.Departamento, empresa.Provincia, empresa.Distrito);

            canvas.Text(Limpiar(nombreEmpresa).ToUpperInvariant(), Margin, y, 14, true);
            y -= 15;
            canvas.Text($"RUC: {empresa.Ruc}", Margin, y, 9);
            y -= 12;
            canvas.Text(Limpiar(empresa.Direccion), Margin, y, 8);
            y -= 11;
            canvas.Text(Limpiar(ubicacion), Margin, y, 8);
            y -= 11;
            canvas.Text(Limpiar(UnirPartes(empresa.Telefono, empresa.Correo)), Margin, y, 8);

            double boxX = PageWidth - Margin - 170;
            double boxY = PageHeight - Margin - 70;
            canvas.Rectangle(boxX, boxY, 170, 70);
            canvas.CenterText("PROFORMA", boxX + 85, boxY + 45, 14, true);
            canvas.CenterText(proforma.SerieNumero, boxX + 85, boxY + 25, 12, true);

            y -= 22;
            canvas.Line(Margin, y, PageWidth - Margin, y);
            y -= 20;
        }

        private static void DibujarCliente(PdfCanvas canvas, Proforma proforma, ref double y)
        {
            double boxHeight = 74;
            canvas.Rectangle(Margin, y - boxHeight, PageWidth - (Margin * 2), boxHeight);

            canvas.Text("CLIENTE", Margin + 8, y - 14, 8, true);
            canvas.Text(Limpiar(proforma.NombreCliente), Margin + 75, y - 14, 9);
            canvas.Text("FECHA", Margin + 8, y - 32, 8, true);
            canvas.Text(proforma.FechaEmision.ToString("dd/MM/yyyy"), Margin + 75, y - 32, 9);
            canvas.Text("VENCE", Margin + 210, y - 32, 8, true);
            canvas.Text(proforma.FechaVencimiento.ToString("dd/MM/yyyy"), Margin + 260, y - 32, 9);
            canvas.Text("OC CLIENTE", Margin + 8, y - 50, 8, true);
            canvas.Text(Limpiar(proforma.OrdenCompraCliente), Margin + 75, y - 50, 9);
            canvas.Text("ELABORADO", Margin + 320, y - 50, 8, true);
            canvas.Text(Limpiar(proforma.UsuarioGenerador), Margin + 390, y - 50, 9);

            y -= boxHeight + 20;
        }

        private static void DibujarDetalle(PdfCanvas canvas, Proforma proforma, ref double y)
        {
            double x = Margin;
            double w = PageWidth - (Margin * 2);
            double headerHeight = 22;
            double rowHeight = 21;

            double colProducto = 220;
            double colCantidad = 55;
            double colPrecio = 70;
            double colDescuento = 70;

            canvas.Rectangle(x, y - headerHeight, w, headerHeight);
            canvas.Text("Producto / Servicio", x + 6, y - 14, 8, true);
            canvas.RightText("Cant.", x + colProducto + colCantidad - 8, y - 14, 8, true);
            canvas.RightText("P. Unit.", x + colProducto + colCantidad + colPrecio - 8, y - 14, 8, true);
            canvas.RightText("Dscto.", x + colProducto + colCantidad + colPrecio + colDescuento - 8, y - 14, 8, true);
            canvas.RightText("Importe", x + w - 8, y - 14, 8, true);

            y -= headerHeight;

            foreach (ProformaDetalle detalle in proforma.Detalles)
            {
                if (y < 125)
                {
                    break;
                }

                canvas.Rectangle(x, y - rowHeight, w, rowHeight);
                canvas.Text(Truncar(Limpiar(detalle.NombreProducto), 42), x + 6, y - 14, 8);
                canvas.RightText(FormatoCantidad(detalle.Cantidad), x + colProducto + colCantidad - 8, y - 14, 8);
                canvas.RightText(FormatoMoneda(detalle.PrecioUnitario), x + colProducto + colCantidad + colPrecio - 8, y - 14, 8);
                canvas.RightText(FormatoMoneda(detalle.Descuento), x + colProducto + colCantidad + colPrecio + colDescuento - 8, y - 14, 8);
                canvas.RightText(FormatoMoneda(detalle.Importe), x + w - 8, y - 14, 8);
                y -= rowHeight;

                if (!string.IsNullOrWhiteSpace(detalle.Observacion) && y >= 125)
                {
                    canvas.Rectangle(x, y - rowHeight, w, rowHeight);
                    canvas.Text($"Obs: {Truncar(Limpiar(detalle.Observacion), 82)}", x + 6, y - 14, 7);
                    y -= rowHeight;
                }
            }

            y -= 15;
        }

        private static void DibujarTotales(PdfCanvas canvas, Proforma proforma, ref double y)
        {
            double xLabel = PageWidth - Margin - 170;
            double xValue = PageWidth - Margin - 8;
            double rowHeight = 18;

            DibujarTotal(canvas, "Subtotal", proforma.Subtotal, xLabel, xValue, ref y, rowHeight, false);
            DibujarTotal(canvas, "Descuento", proforma.Descuento, xLabel, xValue, ref y, rowHeight, false);
            DibujarTotal(canvas, "IGV", proforma.Igv, xLabel, xValue, ref y, rowHeight, false);
            DibujarTotal(canvas, "Total", proforma.Total, xLabel, xValue, ref y, rowHeight, true);
            y -= 10;
        }

        private static void DibujarTotal(PdfCanvas canvas, string label, decimal value, double xLabel, double xValue, ref double y, double rowHeight, bool bold)
        {
            canvas.Rectangle(xLabel, y - rowHeight, PageWidth - Margin - xLabel, rowHeight);
            canvas.Text(label, xLabel + 8, y - 12, 8, bold);
            canvas.RightText(FormatoMoneda(value), xValue, y - 12, 8, bold);
            y -= rowHeight;
        }

        private static void DibujarObservacion(PdfCanvas canvas, Proforma proforma, ref double y)
        {
            if (string.IsNullOrWhiteSpace(proforma.Observacion))
            {
                return;
            }

            canvas.Text("Observaciones", Margin, y, 8, true);
            y -= 12;
            canvas.Text(Truncar(Limpiar(proforma.Observacion), 110), Margin, y, 8);
        }

        private static string UnirPartes(params string[] partes)
        {
            return string.Join(" - ", partes.Where(p => !string.IsNullOrWhiteSpace(p)));
        }

        private static string FormatoMoneda(decimal value)
        {
            return $"S/ {value:N2}";
        }

        private static string FormatoCantidad(decimal value)
        {
            return value.ToString("N2");
        }

        private static string Truncar(string value, int maxLength)
        {
            if (value.Length <= maxLength)
            {
                return value;
            }

            return value[..Math.Max(0, maxLength - 3)] + "...";
        }

        private static string Limpiar(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            return text
                .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u")
                .Replace("Á", "A").Replace("É", "E").Replace("Í", "I").Replace("Ó", "O").Replace("Ú", "U")
                .Replace("ñ", "n").Replace("Ñ", "N").Replace("¿", "").Replace("¡", "");
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

            public void RightText(string text, double rightX, double y, double size, bool bold = false)
            {
                double textWidth = ApproximateWidth(text, size, bold);
                Text(text, rightX - textWidth, y, size, bold);
            }

            public void CenterText(string text, double centerX, double y, double size, bool bold = false)
            {
                double textWidth = ApproximateWidth(text, size, bold);
                Text(text, centerX - (textWidth / 2), y, size, bold);
            }

            public void Line(double x1, double y1, double x2, double y2)
            {
                _content.Append("0 0 0 RG ");
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
                _content.Append("0 0 0 RG ");
                _content.Append(N(x));
                _content.Append(' ');
                _content.Append(N(y));
                _content.Append(' ');
                _content.Append(N(width));
                _content.Append(' ');
                _content.Append(N(height));
                _content.Append(" re S\n");
            }

            private static double ApproximateWidth(string text, double size, bool bold)
            {
                if (string.IsNullOrEmpty(text))
                {
                    return 0;
                }

                double width = 0;
                foreach (char c in text)
                {
                    if (char.IsDigit(c)) width += size * 0.556;
                    else if (c == '.' || c == ',' || c == '/' || c == ' ' || c == '-') width += size * 0.278;
                    else if (c == 'i' || c == 'l' || c == 'I' || c == 't' || c == 'f') width += size * 0.278;
                    else if (char.IsUpper(c) || c == 'w' || c == 'm') width += size * 0.722;
                    else width += size * 0.556;
                }

                return bold ? width * 1.05 : width;
            }
        }

        private static string PdfString(string text)
        {
            StringBuilder builder = new("(");
            foreach (char character in text)
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
