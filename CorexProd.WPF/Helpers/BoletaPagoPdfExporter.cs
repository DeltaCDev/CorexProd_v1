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

        private const double PageMarginX = 14;
        private const double SlotWidth = PageWidth - (PageMarginX * 2);

        // Se aumentó el alto a 345 para ganar espacio vital arriba y abajo
        private const double SlotHeight = 345;

        // Se amplió la columna de Remuneraciones para que quepa más texto
        private const double Col1Width = 215;
        private const double Col2Width = 195;
        private const double Col3Width = SlotWidth - Col1Width - Col2Width;

        // Posiciones optimizadas (márgenes más holgados)
        private static readonly BoletaSlot[] Slots =
        [
            new(PageMarginX, 465, SlotWidth, SlotHeight),
            new(PageMarginX, 55, SlotWidth, SlotHeight)
        ];

        public static void Exportar(
            string ruta,
            Empresa empresa,
            PeriodoPago periodo,
            IReadOnlyList<ResumenPagoTrabajador> resumenes,
            IReadOnlyList<MovimientoTrabajador> movimientos,
            IReadOnlyList<PagoTrabajador> pagos,
            bool imprimirConCopia)
        {
            SimplePdfDocument document = new();
            BoletaPaginator paginator = new(document);

            foreach (var resumen in resumenes.OrderBy(r => r.NombreTrabajador))
            {
                var detalle = movimientos
                    .Where(m => m.IdPeriodoPago == periodo.IdPeriodoPago && m.IdTrabajadorOperativo == resumen.IdTrabajadorOperativo)
                    .OrderBy(m => m.Fecha)
                    .ToList();
                var pagosTrabajador = pagos
                    .Where(p => p.IdPeriodoPago == periodo.IdPeriodoPago
                        && p.IdTrabajadorOperativo == resumen.IdTrabajadorOperativo
                        && !p.Estado.Equals("Anulado", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(p => p.FechaPago)
                    .ToList();

                if (imprimirConCopia)
                {
                    AgregarBoleta(paginator, empresa, periodo, resumen, detalle, pagosTrabajador);
                    AgregarBoleta(paginator, empresa, periodo, resumen, detalle, pagosTrabajador);
                }
                else
                {
                    AgregarBoleta(paginator, empresa, periodo, resumen, detalle, pagosTrabajador);
                }
            }
            document.Save(ruta);
        }

        private static void AgregarBoleta(BoletaPaginator paginator, Empresa empresa, PeriodoPago periodo, ResumenPagoTrabajador resumen, IReadOnlyList<MovimientoTrabajador> movimientos, IReadOnlyList<PagoTrabajador> pagos)
        {
            PdfCanvas canvas = paginator.NextSlot(out BoletaSlot slot);

            // Subimos el inicio del cuadro 4 puntos para pegarlo más arriba
            double tableTop = slot.Top - 21;
            double currentY = DibujarCabecera(canvas, slot, empresa, periodo, resumen, movimientos, pagos);
            double topLineasVerticales = currentY;

            currentY = DibujarTitulosColumnas(canvas, slot, currentY);
            currentY = DibujarCuerpo(canvas, slot, movimientos, currentY);

            double tableBottom = DibujarTotalesYFirmas(canvas, slot, resumen);

            canvas.Rectangle(slot.X, tableBottom, slot.Width, tableTop - tableBottom);

            canvas.Line(slot.X + Col1Width, tableBottom, slot.X + Col1Width, topLineasVerticales);
            canvas.Line(slot.X + Col1Width + Col2Width, tableBottom, slot.X + Col1Width + Col2Width, topLineasVerticales);
        }

        private static double DibujarCabecera(PdfCanvas canvas, BoletaSlot slot, Empresa empresa, PeriodoPago periodo, ResumenPagoTrabajador resumen, IReadOnlyList<MovimientoTrabajador> movimientos, IReadOnlyList<PagoTrabajador> pagos)
        {
            double x = slot.X;
            double w = slot.Width;
            double y = slot.Top;

            canvas.Text(LimpiarYTruncar(EmpresaNombre(empresa), 90), x + 2, y - 9, 7, true);
            canvas.Text(LimpiarYTruncar(EmpresaDireccion(empresa), 110), x + 2, y - 18, 6);

            y -= 21; // Inicio del cuadro más pegado a la empresa

            y -= 10;
            canvas.Text($"RUC  {QuitarTildes(empresa.Ruc)}", x + 2, y, 6);
            canvas.Text("BOLETA DE PAGO SEMANAL", x + (w / 2) - 50, y, 8, true);
            canvas.RightText($"Semana {periodo.CodigoPeriodo} - Del {periodo.FechaInicio:dd/MM/yyyy}  Al  {periodo.FechaFin:dd/MM/yyyy}", x + w - 2, y, 6);

            y -= 4; canvas.Line(x, y, x + w, y);

            y -= 10;
            canvas.Text("Codigo", x + 2, y, 6);
            canvas.Text(resumen.IdTrabajadorOperativo.ToString(CultureInfo.InvariantCulture), x + 50, y, 7, true);
            canvas.Text("Nombre", x + 190, y, 6, true);
            canvas.Text(QuitarTildes(resumen.NombreTrabajador), x + 230, y, 7, true);
            canvas.Text("Tipo", x + 400, y, 6);
            canvas.Text(LimpiarYTruncar(resumen.TipoTrabajador, 18), x + 440, y, 6);

            y -= 10;
            canvas.Text("DNI", x + 2, y, 6);
            canvas.Text(QuitarTildes(resumen.Documento), x + 50, y, 7, true);
            canvas.Text("Ocupacion", x + 190, y, 6);
            canvas.Text(LimpiarYTruncar(resumen.TipoTrabajador, 28), x + 230, y, 6);
            canvas.Text("Medio", x + 400, y, 6);
            canvas.Text(LimpiarYTruncar(TextoMediosPago(pagos, resumen), 22), x + 440, y, 6);

            y -= 10;
            canvas.Text("Operacion", x + 2, y, 6);
            canvas.Text(LimpiarYTruncar(TextoOperacionesPago(pagos), 35), x + 50, y, 6);
            canvas.Text("Fecha pago", x + 270, y, 6);
            canvas.Text(TextoFechasPago(pagos), x + 330, y, 6);
            canvas.Text("Pagado", x + 450, y, 6);
            canvas.RightText(FormatoNumero(resumen.TotalPagado), x + w - 5, y, 6, true);

            y -= 4; canvas.Line(x, y, x + w, y);

            y -= 10;
            canvas.Text("PAGO POR PRODUCCION (DESTAJO)", x + 2, y, 6);
            canvas.Text("UNIDADES PRODUCIDAS TOTALES", x + Col1Width + 2, y, 6);
            canvas.Text(movimientos.Where(m => EsProduccion(m)).Sum(m => m.Cantidad).ToString("0.##", CultureInfo.InvariantCulture), x + Col1Width + Col2Width - 35, y, 7, true);
            canvas.Text("SALDO PENDIENTE", x + Col1Width + Col2Width + 2, y, 6);
            canvas.RightText(FormatoNumero(resumen.SaldoPendiente), x + w - 5, y, 7, true);

            y -= 4; canvas.Line(x, y, x + w, y);

            return y;
        }

        private static double DibujarTitulosColumnas(PdfCanvas canvas, BoletaSlot slot, double y)
        {
            double x = slot.X;
            double w = slot.Width;

            y -= 10;
            canvas.Text("Remuneraciones", x + (Col1Width / 2) - 25, y, 6);
            canvas.Text("Descuentos del trabajador", x + Col1Width + (Col2Width / 2) - 40, y, 6);
            canvas.Text("Aportaciones del empleador", x + Col1Width + Col2Width + (Col3Width / 2) - 45, y, 6);

            y -= 4; canvas.Line(x, y, x + w, y);

            y -= 10;
            canvas.Text("Descripcion", x + 2, y, 6);
            canvas.RightText("Monto (S/.)", x + Col1Width - 5, y, 6);

            canvas.Text("Descripcion", x + Col1Width + 2, y, 6);
            canvas.RightText("Monto (S/.)", x + Col1Width + Col2Width - 5, y, 6);

            canvas.Text("Descripcion", x + Col1Width + Col2Width + 2, y, 6);
            canvas.RightText("Monto (S/.)", x + w - 5, y, 6);

            y -= 4; canvas.Line(x, y, x + w, y);

            return y;
        }

        private static double DibujarCuerpo(PdfCanvas canvas, BoletaSlot slot, IReadOnlyList<MovimientoTrabajador> movimientos, double startY)
        {
            double x = slot.X;
            double w = slot.Width;
            double y = startY;

            var descuentos = movimientos.Where(m =>
                (m.NombreConcepto ?? "").IndexOf("Descuento", StringComparison.OrdinalIgnoreCase) >= 0 ||
                (m.NombreConcepto ?? "").IndexOf("DSCTO", StringComparison.OrdinalIgnoreCase) >= 0 ||
                (m.Descripcion ?? "").IndexOf("Descuento", StringComparison.OrdinalIgnoreCase) >= 0 ||
                (m.Descripcion ?? "").IndexOf("DSCTO", StringComparison.OrdinalIgnoreCase) >= 0
            ).ToList();

            var remuneraciones = movimientos.Except(descuentos).ToList();
            var aportes = new List<MovimientoTrabajador>();

            int maxRows = Math.Max(remuneraciones.Count, Math.Max(descuentos.Count, aportes.Count));

            string ObtenerDetalle(MovimientoTrabajador m)
            {
                if (m.Cantidad > 0 && m.Tarifa > 0)
                {
                    string concepto = m.NombreConcepto ?? "";
                    if (m.Cantidad == 1 && (concepto.IndexOf("Saldo", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            concepto.IndexOf("Movilidad", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            concepto.IndexOf("ONP", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        return "";
                    }
                    return $"{FormatoNumero(m.Tarifa)} x {m.Cantidad:0.##} UND";
                }
                return "";
            }

            double fontSize = 4.5;

            for (int i = 0; i < maxRows; i++)
            {
                y -= 10;

                // --- COLUMNA 1 ---
                if (i < remuneraciones.Count)
                {
                    var r = remuneraciones[i];
                    string texto = string.IsNullOrWhiteSpace(r.Descripcion) ? r.NombreConcepto : r.Descripcion;
                    decimal valor = r.Importe > 0 ? r.Importe : (r.Cantidad * r.Tarifa);
                    string detalle = ObtenerDetalle(r);

                    // Ahora trunca a 50 chars (con detalle) o 65 (sin detalle) = Mucho más espacio libre
                    int limit = string.IsNullOrEmpty(detalle) ? 65 : 50;

                    canvas.Text(LimpiarYTruncar(texto, limit), x + 2, y, fontSize);

                    if (!string.IsNullOrEmpty(detalle))
                        canvas.RightText(detalle, x + Col1Width - 35, y, fontSize); // Alineado a la derecha, cerca al importe

                    canvas.RightText(FormatoNumero(valor), x + Col1Width - 5, y, fontSize); // Importe con decimal alineado
                }

                // --- COLUMNA 2 ---
                if (i < descuentos.Count)
                {
                    var d = descuentos[i];
                    string texto = string.IsNullOrWhiteSpace(d.Descripcion) ? d.NombreConcepto : d.Descripcion;
                    decimal valor = d.Importe > 0 ? d.Importe : (d.Cantidad * d.Tarifa);
                    string detalle = ObtenerDetalle(d);

                    int limit = string.IsNullOrEmpty(detalle) ? 58 : 42;

                    canvas.Text(LimpiarYTruncar(texto, limit), x + Col1Width + 2, y, fontSize);

                    if (!string.IsNullOrEmpty(detalle))
                        canvas.RightText(detalle, x + Col1Width + Col2Width - 35, y, fontSize);

                    canvas.RightText(FormatoNumero(valor), x + Col1Width + Col2Width - 5, y, fontSize);
                }

                // --- COLUMNA 3 ---
                if (i < aportes.Count)
                {
                    var a = aportes[i];
                    string texto = string.IsNullOrWhiteSpace(a.Descripcion) ? a.NombreConcepto : a.Descripcion;
                    decimal valor = a.Importe > 0 ? a.Importe : (a.Cantidad * a.Tarifa);
                    string detalle = ObtenerDetalle(a);

                    int limit = string.IsNullOrEmpty(detalle) ? 45 : 30;

                    canvas.Text(LimpiarYTruncar(texto, limit), x + Col1Width + Col2Width + 2, y, fontSize);

                    if (!string.IsNullOrEmpty(detalle))
                        canvas.RightText(detalle, x + w - 35, y, fontSize);

                    canvas.RightText(FormatoNumero(valor), x + w - 5, y, fontSize);
                }
            }

            return y;
        }

        private static double DibujarTotalesYFirmas(PdfCanvas canvas, BoletaSlot slot, ResumenPagoTrabajador resumen)
        {
            double x = slot.X;
            double w = slot.Width;

            // Subimos la zona de totales para dar espacio amplio a las firmas
            double y = slot.Bottom + 55;

            canvas.Line(x, y, x + w, y);

            y -= 10;
            canvas.Text("TOTAL REMUNERACION", x + 2, y, 6);
            canvas.RightText(FormatoNumero(resumen.TotalIngresos), x + Col1Width - 5, y, 6);

            canvas.Text("TOTAL DESCUENTO", x + Col1Width + 2, y, 6);
            canvas.RightText(FormatoNumero(resumen.TotalDescuentos), x + Col1Width + Col2Width - 5, y, 6);

            canvas.Text("TOTAL APORTACIONES", x + Col1Width + Col2Width + 2, y, 6);
            canvas.RightText("0.00", x + w - 5, y, 6);

            y -= 4; canvas.Line(x, y, x + w, y);

            y -= 10;
            canvas.Text("TOTAL NETO EFECTIVO", x + 2, y, 7, true);
            canvas.RightText(FormatoNumero(resumen.NetoCalculado), x + Col1Width - 5, y, 8, true);

            y -= 4; // Fin exacto del recuadro
            double tableBottom = y;

            // Las firmas bajan drásticamente (22 puntos de diferencia vs el cuadro inferior)
            double yFirmas = slot.Bottom + 5;

            canvas.Line(x + 130, yFirmas + 10, x + 250, yFirmas + 10);
            canvas.Text("Firma del Empleador", x + 155, yFirmas, 6);

            canvas.Line(x + 320, yFirmas + 10, x + 440, yFirmas + 10);
            canvas.Text("Firma del Trabajador", x + 345, yFirmas, 6);

            return tableBottom;
        }

        // --- MÉTODOS DE APOYO Y CLASES ---

        private static bool EsProduccion(MovimientoTrabajador m)
        {
            return !m.EsDescuento
                && !m.TipoMovimiento.Equals("Pago", StringComparison.OrdinalIgnoreCase)
                && (m.CategoriaMovimiento.Equals("Produccion", StringComparison.OrdinalIgnoreCase)
                    || m.CategoriaMovimiento.Equals("Produccion por destajo", StringComparison.OrdinalIgnoreCase));
        }

        private static string EmpresaNombre(Empresa empresa)
        {
            return string.IsNullOrWhiteSpace(empresa.NombreComercial) ? empresa.Nombre : empresa.NombreComercial;
        }

        private static string EmpresaDireccion(Empresa empresa)
        {
            string ubigeo = string.Join(" ", new[] { empresa.Distrito, empresa.Provincia, empresa.Departamento }.Where(v => !string.IsNullOrWhiteSpace(v)));
            return string.IsNullOrWhiteSpace(ubigeo) ? empresa.Direccion : $"{empresa.Direccion} - {ubigeo}";
        }

        private static string TextoMediosPago(IReadOnlyList<PagoTrabajador> pagos, ResumenPagoTrabajador resumen)
        {
            string texto = string.Join(", ", pagos.Select(p => p.MedioPago).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct());
            return string.IsNullOrWhiteSpace(texto) ? resumen.MedioPagoPreferido : texto;
        }

        private static string TextoOperacionesPago(IReadOnlyList<PagoTrabajador> pagos)
        {
            string texto = string.Join(", ", pagos.Select(p => p.NumeroOperacion).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct());
            return string.IsNullOrWhiteSpace(texto) ? "-" : texto;
        }

        private static string TextoFechasPago(IReadOnlyList<PagoTrabajador> pagos)
        {
            if (pagos.Count == 0)
                return "-";

            DateTime min = pagos.Min(p => p.FechaPago).Date;
            DateTime max = pagos.Max(p => p.FechaPago).Date;
            return min == max ? min.ToString("dd/MM/yyyy") : $"{min:dd/MM}-{max:dd/MM}";
        }
        private static string LimpiarYTruncar(string t, int maxLength)
        {
            string limpio = QuitarTildes(t);
            if (limpio.Length > maxLength) return limpio.Substring(0, maxLength - 3) + "...";
            return limpio;
        }

        private static string QuitarTildes(string t)
        {
            if (string.IsNullOrWhiteSpace(t)) return "";
            return t.Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u")
                    .Replace("Á", "A").Replace("É", "E").Replace("Í", "I").Replace("Ó", "O").Replace("Ú", "U")
                    .Replace("ñ", "n").Replace("Ñ", "N").Replace("¿", "").Replace("¡", "");
        }

        private static string FormatoNumero(decimal v) => v == 0 ? "0.00" : v.ToString("N2");
        private static string NormalizarTexto(string t) => t ?? "";

        private sealed record BoletaSlot(double X, double Bottom, double Width, double Height) { public double Top => Bottom + Height; }

        private sealed class BoletaPaginator
        {
            private readonly SimplePdfDocument _document;
            private PdfCanvas? _currentPage;
            private int _slotIndex = Slots.Length;

            public BoletaPaginator(SimplePdfDocument document) => _document = document;

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

            // NUEVA FUNCIÓN: Alinea texto a la derecha (Ideal para columnas de dinero)
            public void RightText(string text, double rightX, double y, double size, bool bold = false)
            {
                double textWidth = ApproximateWidth(text, size, bold);
                Text(text, rightX - textWidth, y, size, bold);
            }

            // Motor matemático para aproximar el ancho exacto del texto en la fuente Helvetica del PDF
            private double ApproximateWidth(string text, double size, bool bold)
            {
                if (string.IsNullOrEmpty(text)) return 0;
                double w = 0;
                foreach (char c in text)
                {
                    if (char.IsDigit(c)) w += size * 0.556; // Números tabulares
                    else if (c == '.' || c == ',' || c == ' ') w += size * 0.278; // Signos estrechos
                    else if (c == 'i' || c == 'l' || c == 'I' || c == 't' || c == 'f') w += size * 0.278;
                    else if (char.IsUpper(c) || c == 'w' || c == 'm') w += size * 0.722;
                    else w += size * 0.556; // Letras promedio
                }
                if (bold) w *= 1.05; // La negrita ocupa un ~5% más
                return w;
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
                AppendRectangle(x, y, width, height);
                _content.Append("S\n");
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
                if (character is '(' or ')' or '\\') builder.Append('\\');
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