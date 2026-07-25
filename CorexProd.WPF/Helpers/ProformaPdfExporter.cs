using CorexProd.Entidad.Entidades;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;

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
            string tituloDocumento = EsOrdenCompra(proforma) ? "ORDEN DE COMPRA" : "PROFORMA";

            canvas.Text(Limpiar(nombreEmpresa).ToUpperInvariant(), Margin, y, 14, true);
            y -= 15;
            canvas.Text($"RUC: {empresa.Ruc}", Margin, y, 9);
            y -= 12;
            canvas.Text(Limpiar(empresa.Direccion), Margin, y, 8);
            y -= 11;
            canvas.Text(Limpiar(ubicacion), Margin, y, 8);
            y -= 11;
            canvas.Text(Limpiar(UnirPartes(empresa.Telefono, empresa.Correo)), Margin, y, 8);

            double boxWidth = 150;
            double boxHeight = 56;
            double boxX = PageWidth - Margin - boxWidth;
            double boxY = PageHeight - Margin - boxHeight - 4;
            canvas.Rectangle(boxX, boxY, boxWidth, boxHeight);
            canvas.CenterText(tituloDocumento, boxX + (boxWidth / 2), boxY + 34, EsOrdenCompra(proforma) ? 11 : 13, true);
            canvas.CenterText(proforma.SerieNumero, boxX + (boxWidth / 2), boxY + 17, 11, true);

            if (proforma.Estado.Equals("Anulado", StringComparison.OrdinalIgnoreCase))
            {
                canvas.CenterText("ANULADO", boxX + (boxWidth / 2), boxY + 5, 8, true, 190, 18, 60);
            }

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

            double colCodigo = 70;
            double colProducto = 230;
            double colCantidad = 55;
            double colPrecio = 60;
            double colDescuento = 55;

            canvas.Rectangle(x, y - headerHeight, w, headerHeight);
            canvas.Text("Codigo", x + 6, y - 14, 8, true);
            canvas.Text("Producto / Servicio", x + colCodigo + 6, y - 14, 8, true);
            canvas.RightText("Cant.", x + colCodigo + colProducto + colCantidad - 8, y - 14, 8, true);
            canvas.RightText("P. Unit.", x + colCodigo + colProducto + colCantidad + colPrecio - 8, y - 14, 8, true);
            canvas.RightText("Dscto.", x + colCodigo + colProducto + colCantidad + colPrecio + colDescuento - 8, y - 14, 8, true);
            canvas.RightText("Importe", x + w - 8, y - 14, 8, true);

            y -= headerHeight;

            foreach (ProformaDetalle detalle in proforma.Detalles)
            {
                if (y < 125)
                {
                    break;
                }

                string producto = detalle.NombreProducto;
                if (!string.IsNullOrWhiteSpace(detalle.Observacion))
                {
                    producto = $"{producto} (OBS. {detalle.Observacion})";
                }

                List<string> lineasProducto = EnvolverTexto(canvas, Limpiar(producto), colProducto - 12, 8);
                double rowHeight = Math.Max(21, 10 + (lineasProducto.Count * 9));

                canvas.Rectangle(x, y - rowHeight, w, rowHeight);
                canvas.Text(Truncar(Limpiar(detalle.CodigoProducto), 13), x + 6, y - 14, 8);

                for (int i = 0; i < lineasProducto.Count; i++)
                {
                    canvas.Text(lineasProducto[i], x + colCodigo + 6, y - 14 - (i * 9), 8);
                }

                canvas.RightText(FormatoCantidad(detalle.Cantidad), x + colCodigo + colProducto + colCantidad - 8, y - 14, 8);
                canvas.RightText(FormatoMoneda(detalle.PrecioUnitario), x + colCodigo + colProducto + colCantidad + colPrecio - 8, y - 14, 8);
                canvas.RightText(FormatoMoneda(detalle.Descuento), x + colCodigo + colProducto + colCantidad + colPrecio + colDescuento - 8, y - 14, 8);
                canvas.RightText(FormatoMoneda(detalle.Importe), x + w - 8, y - 14, 8);
                y -= rowHeight;
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
            string etiquetaIgv = proforma.IgvPorcentaje > 0
                && !proforma.CondicionTributaria.Equals("Exonerado de IGV", StringComparison.OrdinalIgnoreCase)
                ? $"IGV ({proforma.IgvPorcentaje:N2}%)"
                : "IGV";
            DibujarTotal(canvas, etiquetaIgv, proforma.Igv, xLabel, xValue, ref y, rowHeight, false);
            DibujarTotal(canvas, "Total", proforma.Total, xLabel, xValue, ref y, rowHeight, true);
            canvas.RightText(proforma.CondicionTributaria, xValue, y - 10, 7, false);
            y -= 24;
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

        private static bool EsOrdenCompra(Proforma proforma) =>
            proforma.SerieNumero.StartsWith("OC-", StringComparison.OrdinalIgnoreCase)
            || proforma.SerieNumero.StartsWith("OCI-", StringComparison.OrdinalIgnoreCase);

        private static string UnirPartes(params string[] partes) =>
            string.Join(" - ", partes.Where(p => !string.IsNullOrWhiteSpace(p)));

        private static List<string> EnvolverTexto(PdfCanvas canvas, string texto, double maxWidth, double fontSize)
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
            {
                lineas.Add(linea.ToString());
            }

            return lineas;
        }

        private static string FormatoMoneda(decimal value) => $"S/ {value:N2}";
        private static string FormatoCantidad(decimal value) => value.ToString("N2");
        private static string Truncar(string value, int maxLength) => value.Length <= maxLength ? value : value[..Math.Max(0, maxLength - 3)] + "...";

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

        internal sealed class SimplePdfDocument
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
                int imageCount = _pages.Sum(p => p.Images.Count);
                int objectCount = 4 + (pageCount * 2) + imageCount;
                byte[][] objects = new byte[objectCount + 1][];
                int[] pageObjectIds = new int[pageCount];
                int[] contentObjectIds = new int[pageCount];
                Dictionary<PdfImage, int> imageObjectIds = [];
                int nextId = 5;

                for (int i = 0; i < pageCount; i++)
                {
                    pageObjectIds[i] = nextId++;
                    contentObjectIds[i] = nextId++;
                }

                foreach (PdfCanvas page in _pages)
                {
                    foreach (PdfImage image in page.Images)
                    {
                        if (!imageObjectIds.ContainsKey(image))
                            imageObjectIds[image] = nextId++;
                    }
                }

                string kids = string.Join(" ", pageObjectIds.Select(id => $"{id} 0 R"));
                objects[1] = AsciiObject(1, "<< /Type /Catalog /Pages 2 0 R >>");
                objects[2] = AsciiObject(2, $"<< /Type /Pages /Count {pageCount} /Kids [{kids}] >>");
                objects[3] = AsciiObject(3, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
                objects[4] = AsciiObject(4, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>");

                for (int i = 0; i < pageCount; i++)
                {
                    PdfCanvas page = _pages[i];
                    string xObjects = page.Images.Count == 0
                        ? string.Empty
                        : " /XObject << " + string.Join(" ", page.Images.Select(img => $"/{img.Name} {imageObjectIds[img]} 0 R")) + " >>";
                    objects[pageObjectIds[i]] = AsciiObject(pageObjectIds[i], $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {N(page.Width)} {N(page.Height)}] /Resources << /Font << /F1 3 0 R /F2 4 0 R >>{xObjects} >> /Contents {contentObjectIds[i]} 0 R >>");
                    objects[contentObjectIds[i]] = StreamObject(contentObjectIds[i], page.Content);
                }

                foreach (KeyValuePair<PdfImage, int> item in imageObjectIds)
                    objects[item.Value] = ImageObject(item.Value, item.Key);

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

            private static byte[] AsciiObject(int id, string body) => Encoding.ASCII.GetBytes($"{id} 0 obj\n{body}\nendobj\n");
            private static byte[] StreamObject(int id, string content)
            {
                byte[] contentBytes = Encoding.ASCII.GetBytes(content);
                return Encoding.ASCII.GetBytes($"{id} 0 obj\n<< /Length {contentBytes.Length} >>\nstream\n{content}endstream\nendobj\n");
            }
            private static void WriteAscii(Stream stream, string value) => stream.Write(Encoding.ASCII.GetBytes(value));

            private static byte[] ImageObject(int id, PdfImage image)
            {
                byte[] header = Encoding.ASCII.GetBytes(
                    $"{id} 0 obj\n<< /Type /XObject /Subtype /Image /Width {image.Width} /Height {image.Height} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length {image.Bytes.Length} >>\nstream\n");
                byte[] footer = Encoding.ASCII.GetBytes("\nendstream\nendobj\n");
                byte[] result = new byte[header.Length + image.Bytes.Length + footer.Length];
                Buffer.BlockCopy(header, 0, result, 0, header.Length);
                Buffer.BlockCopy(image.Bytes, 0, result, header.Length, image.Bytes.Length);
                Buffer.BlockCopy(footer, 0, result, header.Length + image.Bytes.Length, footer.Length);
                return result;
            }
        }

        internal sealed class PdfCanvas
        {
            private readonly StringBuilder _content = new();
            private int _imageCounter;
            public PdfCanvas(double width, double height) { Width = width; Height = height; }
            public double Width { get; }
            public double Height { get; }
            public string Content => _content.ToString();
            internal List<PdfImage> Images { get; } = [];

            public void Text(string text, double x, double y, double size, bool bold = false) => Text(text, x, y, size, bold, 0, 0, 0);
            public void Text(string text, double x, double y, double size, bool bold, byte red, byte green, byte blue)
            {
                _content.Append($"{ColorValue(red)} {ColorValue(green)} {ColorValue(blue)} rg BT /");
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

            public void RightText(string text, double rightX, double y, double size, bool bold = false) => Text(text, rightX - ApproximateWidth(text, size, bold), y, size, bold);
            public void RightText(string text, double rightX, double y, double size, bool bold, byte red, byte green, byte blue) =>
                Text(text, rightX - ApproximateWidth(text, size, bold), y, size, bold, red, green, blue);
            public double MeasureText(string text, double size, bool bold = false) => ApproximateWidth(text, size, bold);
            public void CenterText(string text, double centerX, double y, double size, bool bold = false) => CenterText(text, centerX, y, size, bold, 0, 0, 0);
            public void CenterText(string text, double centerX, double y, double size, bool bold, byte red, byte green, byte blue) => Text(text, centerX - (ApproximateWidth(text, size, bold) / 2), y, size, bold, red, green, blue);
            public void RotatedCenterText(string text, double centerX, double centerY, double size, double angleDegrees, bool bold, byte red, byte green, byte blue)
            {
                double radians = angleDegrees * Math.PI / 180d;
                double cos = Math.Cos(radians);
                double sin = Math.Sin(radians);
                double textWidth = ApproximateWidth(text, size, bold);
                double x = centerX - (textWidth / 2 * cos);
                double y = centerY - (textWidth / 2 * sin);
                _content.Append("q ");
                _content.Append($"{ColorValue(red)} {ColorValue(green)} {ColorValue(blue)} rg BT /");
                _content.Append(bold ? "F2" : "F1");
                _content.Append(' ');
                _content.Append(N(size));
                _content.Append(" Tf ");
                _content.Append($"{N(cos)} {N(sin)} {N(-sin)} {N(cos)} {N(x)} {N(y)} Tm ");
                _content.Append(PdfString(text));
                _content.Append(" Tj ET Q\n");
            }
            public void Line(double x1, double y1, double x2, double y2) => _content.Append($"0 0 0 RG {N(x1)} {N(y1)} m {N(x2)} {N(y2)} l S\n");
            public void Line(double x1, double y1, double x2, double y2, byte red, byte green, byte blue) =>
                _content.Append($"{ColorValue(red)} {ColorValue(green)} {ColorValue(blue)} RG {N(x1)} {N(y1)} m {N(x2)} {N(y2)} l S\n");
            public void Rectangle(double x, double y, double width, double height) => _content.Append($"0 0 0 RG {N(x)} {N(y)} {N(width)} {N(height)} re S\n");
            public void Rectangle(double x, double y, double width, double height, byte red, byte green, byte blue) =>
                _content.Append($"{ColorValue(red)} {ColorValue(green)} {ColorValue(blue)} RG {N(x)} {N(y)} {N(width)} {N(height)} re S\n");
            public void FilledRectangle(double x, double y, double width, double height, byte red, byte green, byte blue)
            {
                _content.Append($"{ColorValue(red)} {ColorValue(green)} {ColorValue(blue)} rg {N(x)} {N(y)} {N(width)} {N(height)} re f\n");
                _content.Append($"0 0 0 RG {N(x)} {N(y)} {N(width)} {N(height)} re S\n");
            }

            public void RoundedRectangle(double x, double y, double width, double height, double radius, byte red, byte green, byte blue)
            {
                AppendRoundedRectanglePath(x, y, width, height, radius);
                _content.Append($"{ColorValue(red)} {ColorValue(green)} {ColorValue(blue)} RG S\n");
            }

            public void FilledRoundedRectangle(double x, double y, double width, double height, double radius, byte red, byte green, byte blue)
            {
                _content.Append($"{ColorValue(red)} {ColorValue(green)} {ColorValue(blue)} rg ");
                AppendRoundedRectanglePath(x, y, width, height, radius);
                _content.Append("f\n");
            }

            public void Circle(double centerX, double centerY, double radius, byte red, byte green, byte blue)
            {
                double c = radius * 0.5522847498;
                _content.Append($"{ColorValue(red)} {ColorValue(green)} {ColorValue(blue)} RG ");
                _content.Append($"{N(centerX + radius)} {N(centerY)} m ");
                _content.Append($"{N(centerX + radius)} {N(centerY + c)} {N(centerX + c)} {N(centerY + radius)} {N(centerX)} {N(centerY + radius)} c ");
                _content.Append($"{N(centerX - c)} {N(centerY + radius)} {N(centerX - radius)} {N(centerY + c)} {N(centerX - radius)} {N(centerY)} c ");
                _content.Append($"{N(centerX - radius)} {N(centerY - c)} {N(centerX - c)} {N(centerY - radius)} {N(centerX)} {N(centerY - radius)} c ");
                _content.Append($"{N(centerX + c)} {N(centerY - radius)} {N(centerX + radius)} {N(centerY - c)} {N(centerX + radius)} {N(centerY)} c S\n");
            }

            public void FilledCircle(double centerX, double centerY, double radius, byte red, byte green, byte blue)
            {
                double c = radius * 0.5522847498;
                _content.Append($"{ColorValue(red)} {ColorValue(green)} {ColorValue(blue)} rg ");
                _content.Append($"{N(centerX + radius)} {N(centerY)} m ");
                _content.Append($"{N(centerX + radius)} {N(centerY + c)} {N(centerX + c)} {N(centerY + radius)} {N(centerX)} {N(centerY + radius)} c ");
                _content.Append($"{N(centerX - c)} {N(centerY + radius)} {N(centerX - radius)} {N(centerY + c)} {N(centerX - radius)} {N(centerY)} c ");
                _content.Append($"{N(centerX - radius)} {N(centerY - c)} {N(centerX - c)} {N(centerY - radius)} {N(centerX)} {N(centerY - radius)} c ");
                _content.Append($"{N(centerX + c)} {N(centerY - radius)} {N(centerX + radius)} {N(centerY - c)} {N(centerX + radius)} {N(centerY)} c f\n");
            }

            private void AppendRoundedRectanglePath(double x, double y, double width, double height, double radius)
            {
                radius = Math.Max(0, Math.Min(radius, Math.Min(width, height) / 2));
                double c = radius * 0.5522847498;
                double right = x + width;
                double top = y + height;

                _content.Append($"{N(x + radius)} {N(y)} m ");
                _content.Append($"{N(right - radius)} {N(y)} l ");
                _content.Append($"{N(right - radius + c)} {N(y)} {N(right)} {N(y + radius - c)} {N(right)} {N(y + radius)} c ");
                _content.Append($"{N(right)} {N(top - radius)} l ");
                _content.Append($"{N(right)} {N(top - radius + c)} {N(right - radius + c)} {N(top)} {N(right - radius)} {N(top)} c ");
                _content.Append($"{N(x + radius)} {N(top)} l ");
                _content.Append($"{N(x + radius - c)} {N(top)} {N(x)} {N(top - radius + c)} {N(x)} {N(top - radius)} c ");
                _content.Append($"{N(x)} {N(y + radius)} l ");
                _content.Append($"{N(x)} {N(y + radius - c)} {N(x + radius - c)} {N(y)} {N(x + radius)} {N(y)} c h ");
            }
            public bool Image(string path, double x, double y, double maxWidth, double maxHeight)
            {
                if (!File.Exists(path))
                    return false;

                try
                {
                    PdfImage image = PdfImage.FromFile(path, $"Im{++_imageCounter}");
                    Images.Add(image);
                    double scale = Math.Min(maxWidth / image.Width, maxHeight / image.Height);
                    double width = image.Width * scale;
                    double height = image.Height * scale;
                    _content.Append($"q {N(width)} 0 0 {N(height)} {N(x)} {N(y)} cm /{image.Name} Do Q\n");
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            public bool CenteredImage(string path, double x, double y, double maxWidth, double maxHeight)
            {
                if (!File.Exists(path))
                    return false;

                try
                {
                    PdfImage image = PdfImage.FromFile(path, $"Im{++_imageCounter}");
                    Images.Add(image);
                    double scale = Math.Min(maxWidth / image.Width, maxHeight / image.Height);
                    double width = image.Width * scale;
                    double height = image.Height * scale;
                    double centeredX = x + ((maxWidth - width) / 2);
                    double centeredY = y + ((maxHeight - height) / 2);
                    _content.Append($"q {N(width)} 0 0 {N(height)} {N(centeredX)} {N(centeredY)} cm /{image.Name} Do Q\n");
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            public bool CenteredImage(byte[] bytes, double x, double y, double maxWidth, double maxHeight)
            {
                try
                {
                    PdfImage image = PdfImage.FromBytes(bytes, $"Im{++_imageCounter}");
                    Images.Add(image);
                    double scale = Math.Min(maxWidth / image.Width, maxHeight / image.Height);
                    double width = image.Width * scale;
                    double height = image.Height * scale;
                    double centeredX = x + ((maxWidth - width) / 2);
                    double centeredY = y + ((maxHeight - height) / 2);
                    _content.Append($"q {N(width)} 0 0 {N(height)} {N(centeredX)} {N(centeredY)} cm /{image.Name} Do Q\n");
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            public bool Image(byte[] bytes, double x, double y, double maxWidth, double maxHeight)
            {
                try
                {
                    PdfImage image = PdfImage.FromBytes(bytes, $"Im{++_imageCounter}");
                    Images.Add(image);
                    double scale = Math.Min(maxWidth / image.Width, maxHeight / image.Height);
                    double width = image.Width * scale;
                    double height = image.Height * scale;
                    _content.Append($"q {N(width)} 0 0 {N(height)} {N(x)} {N(y)} cm /{image.Name} Do Q\n");
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            private static double ApproximateWidth(string text, double size, bool bold)
            {
                if (string.IsNullOrEmpty(text)) return 0;
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
            private static string ColorValue(byte value) => (value / 255d).ToString("0.###", CultureInfo.InvariantCulture);
        }

        internal sealed class PdfImage
        {
            public required string Name { get; init; }
            public required byte[] Bytes { get; init; }
            public required int Width { get; init; }
            public required int Height { get; init; }

            public static PdfImage FromFile(string path, string name) => FromBytes(File.ReadAllBytes(path), name);

            public static PdfImage FromBytes(byte[] bytes, string name)
            {
                using MemoryStream input = new(bytes);
                BitmapDecoder decoder = BitmapDecoder.Create(input, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                BitmapFrame frame = decoder.Frames[0];
                byte[] jpgBytes;
                using (MemoryStream output = new())
                {
                    JpegBitmapEncoder encoder = new() { QualityLevel = 88 };
                    encoder.Frames.Add(BitmapFrame.Create(frame));
                    encoder.Save(output);
                    jpgBytes = output.ToArray();
                }

                return new PdfImage
                {
                    Name = name,
                    Bytes = jpgBytes,
                    Width = frame.PixelWidth,
                    Height = frame.PixelHeight
                };
            }
        }

        private static string PdfString(string text)
        {
            StringBuilder builder = new("(");
            foreach (char character in text)
            {
                if (character is '(' or ')' or '\\') builder.Append('\\');
                builder.Append(character);
            }
            builder.Append(')');
            return builder.ToString();
        }

        private static string N(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
