using CorexProd.Entidad.Entidades;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Resources;

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
            bool logoDibujado = false;

            foreach (byte[] logoBytes in ObtenerLogosBytes(empresa))
            {
                if (canvas.Image(logoBytes, Margin, y - 44, 86, 44))
                {
                    logoDibujado = true;
                    break;
                }
            }

            double infoX = logoDibujado ? Margin + 98 : Margin;

            canvas.Text(Limpiar(nombreEmpresa).ToUpperInvariant(), infoX, y, 14, true);
            y -= 15;
            canvas.Text($"RUC: {empresa.Ruc}", infoX, y, 9);
            y -= 12;
            canvas.Text(Limpiar(empresa.Direccion), infoX, y, 8);
            y -= 11;
            canvas.Text(Limpiar(ubicacion), infoX, y, 8);
            y -= 11;
            canvas.Text(Limpiar(UnirPartes(empresa.Telefono, empresa.Correo)), infoX, y, 8);

            double boxWidth = 140;
            double boxHeight = 56;
            double boxX = PageWidth - Margin - boxWidth;
            double boxY = PageHeight - Margin - boxHeight - 4;
            canvas.Rectangle(boxX, boxY, boxWidth, boxHeight);
            canvas.CenterText("PROFORMA", boxX + (boxWidth / 2), boxY + 34, 13, true);
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
            y -= 14;
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

        private static List<string> EnvolverTexto(PdfCanvas canvas, string texto, double maxWidth, double fontSize)
        {
            List<string> lineas = [];

            if (string.IsNullOrWhiteSpace(texto))
            {
                lineas.Add(string.Empty);
                return lineas;
            }

            string[] palabras = texto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            StringBuilder linea = new();

            foreach (string palabra in palabras)
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

                if (canvas.MeasureText(palabra, fontSize) <= maxWidth)
                {
                    linea.Append(palabra);
                }
                else
                {
                    foreach (string parte in DividirPalabra(canvas, palabra, maxWidth, fontSize))
                    {
                        if (canvas.MeasureText(parte, fontSize) <= maxWidth && parte != palabra)
                        {
                            lineas.Add(parte);
                        }
                        else
                        {
                            linea.Append(parte);
                        }
                    }
                }
            }

            if (linea.Length > 0)
            {
                lineas.Add(linea.ToString());
            }

            return lineas;
        }

        private static IEnumerable<string> DividirPalabra(PdfCanvas canvas, string palabra, double maxWidth, double fontSize)
        {
            StringBuilder parte = new();

            foreach (char caracter in palabra)
            {
                string candidata = parte.ToString() + caracter;

                if (parte.Length > 0 && canvas.MeasureText(candidata, fontSize) > maxWidth)
                {
                    yield return parte.ToString();
                    parte.Clear();
                }

                parte.Append(caracter);
            }

            if (parte.Length > 0)
            {
                yield return parte.ToString();
            }
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

        private static IEnumerable<byte[]> ObtenerLogosBytes(Empresa empresa)
        {
            if (empresa.Logo is { Length: > 0 } logoEmpresa && EsImagenValida(logoEmpresa))
            {
                yield return logoEmpresa;
            }

            string baseDirectory = AppContext.BaseDirectory;
            string[] rutas =
            [
                Path.Combine(baseDirectory, "Images", "LOGO.png"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "LOGO.png"),
                Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", "Images", "LOGO.png")),
                Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", "..", "CorexProd.WPF", "Images", "LOGO.png")),
                Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "Images", "LOGO.png")),
                Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "CorexProd.WPF", "Images", "LOGO.png"))
            ];

            foreach (string logoPath in rutas.Where(path => File.Exists(path) && EsImagenValida(path)))
            {
                yield return File.ReadAllBytes(logoPath);
            }

            StreamResourceInfo? resource = Application.GetResourceStream(new Uri("pack://application:,,,/Images/LOGO.png", UriKind.Absolute));

            if (resource?.Stream != null)
            {
                using MemoryStream stream = new();
                resource.Stream.CopyTo(stream);
                yield return stream.ToArray();
            }
        }

        private static bool EsImagenValida(string path)
        {
            try
            {
                using FileStream stream = File.OpenRead(path);
                BitmapFrame.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool EsImagenValida(byte[]? bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return false;
            }

            try
            {
                using MemoryStream stream = new(bytes);
                BitmapFrame.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                return true;
            }
            catch
            {
                return false;
            }
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
                List<PdfImage> images = _pages.SelectMany(page => page.Images).ToList();
                int objectCount = 4 + (pageCount * 2) + images.Count;
                byte[][] objects = new byte[objectCount + 1][];

                int[] pageObjectIds = new int[pageCount];
                int[] contentObjectIds = new int[pageCount];
                int nextId = 5;

                for (int i = 0; i < pageCount; i++)
                {
                    pageObjectIds[i] = nextId++;
                    contentObjectIds[i] = nextId++;
                }

                foreach (PdfImage image in images)
                {
                    image.ObjectId = nextId++;
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
                        : $" /XObject << {string.Join(" ", page.Images.Select(image => $"/{image.Name} {image.ObjectId} 0 R"))} >>";
                    objects[pageObjectIds[i]] = AsciiObject(
                        pageObjectIds[i],
                        $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {N(page.Width)} {N(page.Height)}] /Resources << /Font << /F1 3 0 R /F2 4 0 R >>{xObjects} >> /Contents {contentObjectIds[i]} 0 R >>");

                    objects[contentObjectIds[i]] = StreamObject(contentObjectIds[i], page.Content);
                }

                foreach (PdfImage image in images)
                {
                    objects[image.ObjectId] = ImageObject(image);
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

            private static byte[] ImageObject(PdfImage image)
            {
                byte[] header = Encoding.ASCII.GetBytes(
                    $"{image.ObjectId} 0 obj\n<< /Type /XObject /Subtype /Image /Width {image.Width} /Height {image.Height} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /{image.Filter} /Length {image.Data.Length} >>\nstream\n");
                byte[] footer = Encoding.ASCII.GetBytes("endstream\nendobj\n");

                using MemoryStream stream = new();
                stream.Write(header);
                stream.Write(image.Data);
                stream.Write(footer);
                return stream.ToArray();
            }

            private static void WriteAscii(Stream stream, string value)
            {
                stream.Write(Encoding.ASCII.GetBytes(value));
            }
        }

        internal sealed class PdfCanvas
        {
            private readonly StringBuilder _content = new();
            private int _imageCounter;

            public PdfCanvas(double width, double height)
            {
                Width = width;
                Height = height;
            }

            public double Width { get; }
            public double Height { get; }
            public List<PdfImage> Images { get; } = [];
            public string Content => _content.ToString();

            public void Text(string text, double x, double y, double size, bool bold = false)
            {
                Text(text, x, y, size, bold, 0, 0, 0);
            }

            public void Text(string text, double x, double y, double size, bool bold, byte red, byte green, byte blue)
            {
                _content.Append($"{ColorValue(red)} {ColorValue(green)} {ColorValue(blue)} rg ");
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

            public double MeasureText(string text, double size, bool bold = false)
            {
                return ApproximateWidth(text, size, bold);
            }

            public void CenterText(string text, double centerX, double y, double size, bool bold = false)
            {
                CenterText(text, centerX, y, size, bold, 0, 0, 0);
            }

            public void CenterText(string text, double centerX, double y, double size, bool bold, byte red, byte green, byte blue)
            {
                double textWidth = ApproximateWidth(text, size, bold);
                Text(text, centerX - (textWidth / 2), y, size, bold, red, green, blue);
            }

            public bool Image(string path, double x, double y, double maxWidth, double maxHeight)
            {
                PdfImage? image = PdfImage.FromFile(path, $"Im{++_imageCounter}");
                return DrawImage(image, x, y, maxWidth, maxHeight);
            }

            public bool Image(byte[] bytes, double x, double y, double maxWidth, double maxHeight)
            {
                PdfImage? image = PdfImage.FromBytes(bytes, $"Im{++_imageCounter}");
                return DrawImage(image, x, y, maxWidth, maxHeight);
            }

            private bool DrawImage(PdfImage? image, double x, double y, double maxWidth, double maxHeight)
            {
                if (image == null)
                {
                    return false;
                }

                double ratio = Math.Min(maxWidth / image.Width, maxHeight / image.Height);
                double width = image.Width * ratio;
                double height = image.Height * ratio;

                Images.Add(image);
                _content.Append("q ");
                _content.Append(N(width));
                _content.Append(" 0 0 ");
                _content.Append(N(height));
                _content.Append(' ');
                _content.Append(N(x));
                _content.Append(' ');
                _content.Append(N(y + (maxHeight - height) / 2));
                _content.Append(" cm /");
                _content.Append(image.Name);
                _content.Append(" Do Q\n");
                return true;
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

            private static string ColorValue(byte value)
            {
                return (value / 255d).ToString("0.###", CultureInfo.InvariantCulture);
            }
        }

        internal sealed class PdfImage
        {
            private PdfImage(string name, int width, int height, byte[] data, string filter)
            {
                Name = name;
                Width = width;
                Height = height;
                Data = data;
                Filter = filter;
            }

            public string Name { get; }
            public int Width { get; }
            public int Height { get; }
            public byte[] Data { get; }
            public string Filter { get; }
            public int ObjectId { get; set; }

            public static PdfImage? FromFile(string path, string name)
            {
                try
                {
                    BitmapImage bitmap = new();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(path, UriKind.Absolute);
                    bitmap.EndInit();
                    bitmap.Freeze();

                    return FromBitmapSource(bitmap, name);
                }
                catch
                {
                    return null;
                }
            }

            public static PdfImage? FromBytes(byte[] bytes, string name)
            {
                try
                {
                    using MemoryStream input = new(bytes);
                    BitmapImage bitmap = new();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = input;
                    bitmap.EndInit();
                    bitmap.Freeze();

                    return FromBitmapSource(bitmap, name);
                }
                catch
                {
                    return null;
                }
            }

            private static PdfImage FromBitmapSource(BitmapSource bitmap, string name)
            {
                BitmapSource source = bitmap.Format == PixelFormats.Pbgra32
                    ? bitmap
                    : new FormatConvertedBitmap(bitmap, PixelFormats.Pbgra32, null, 0);

                int width = source.PixelWidth;
                int height = source.PixelHeight;
                int sourceStride = width * 4;
                byte[] sourcePixels = new byte[sourceStride * height];

                source.CopyPixels(sourcePixels, sourceStride, 0);

                Int32Rect contentBounds = ObtenerAreaVisible(sourcePixels, width, height, sourceStride);
                int outputWidth = contentBounds.Width;
                int outputHeight = contentBounds.Height;
                byte[] rgbPixels = new byte[outputWidth * outputHeight * 3];

                for (int y = 0, rgbIndex = 0; y < outputHeight; y++)
                {
                    int sourceIndex = ((contentBounds.Y + y) * sourceStride) + (contentBounds.X * 4);

                    for (int x = 0; x < outputWidth; x++, sourceIndex += 4, rgbIndex += 3)
                    {
                        byte blue = sourcePixels[sourceIndex];
                        byte green = sourcePixels[sourceIndex + 1];
                        byte red = sourcePixels[sourceIndex + 2];
                        byte alpha = sourcePixels[sourceIndex + 3];

                        rgbPixels[rgbIndex] = BlendWithWhite(red, alpha);
                        rgbPixels[rgbIndex + 1] = BlendWithWhite(green, alpha);
                        rgbPixels[rgbIndex + 2] = BlendWithWhite(blue, alpha);
                    }
                }

                BitmapSource rgbSource = BitmapSource.Create(
                    outputWidth,
                    outputHeight,
                    96,
                    96,
                    PixelFormats.Rgb24,
                    null,
                    rgbPixels,
                    outputWidth * 3);

                JpegBitmapEncoder encoder = new()
                {
                    QualityLevel = 92
                };
                encoder.Frames.Add(BitmapFrame.Create(rgbSource));

                using MemoryStream output = new();
                encoder.Save(output);

                return new PdfImage(name, outputWidth, outputHeight, output.ToArray(), "DCTDecode");
            }

            private static Int32Rect ObtenerAreaVisible(byte[] pixels, int width, int height, int stride)
            {
                int left = width;
                int top = height;
                int right = -1;
                int bottom = -1;

                for (int y = 0; y < height; y++)
                {
                    int index = y * stride;

                    for (int x = 0; x < width; x++, index += 4)
                    {
                        byte blue = pixels[index];
                        byte green = pixels[index + 1];
                        byte red = pixels[index + 2];
                        byte alpha = pixels[index + 3];

                        if (!EsPixelVisible(red, green, blue, alpha))
                        {
                            continue;
                        }

                        left = Math.Min(left, x);
                        top = Math.Min(top, y);
                        right = Math.Max(right, x);
                        bottom = Math.Max(bottom, y);
                    }
                }

                if (right < left || bottom < top)
                {
                    return new Int32Rect(0, 0, width, height);
                }

                return new Int32Rect(left, top, right - left + 1, bottom - top + 1);
            }

            private static bool EsPixelVisible(byte premultipliedRed, byte premultipliedGreen, byte premultipliedBlue, byte alpha)
            {
                if (alpha < 16)
                {
                    return false;
                }

                byte red = BlendWithWhite(premultipliedRed, alpha);
                byte green = BlendWithWhite(premultipliedGreen, alpha);
                byte blue = BlendWithWhite(premultipliedBlue, alpha);

                return red < 245 || green < 245 || blue < 245;
            }

            private static byte BlendWithWhite(byte premultipliedColor, byte alpha)
            {
                if (alpha == 255)
                {
                    return premultipliedColor;
                }

                int value = premultipliedColor + 255 - alpha;
                return (byte)Math.Clamp(value, 0, 255);
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
