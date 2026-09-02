using CorexProd.Entidad.Entidades;
using CorexProd.WPF.Modules.Tesoreria.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace CorexProd.WPF.Helpers
{
    internal static class ProgramacionPagosPdfExporter
    {
        private const double PageWidth = 842;
        private const double PageHeight = 595;
        private const double Margin = 28;
        private const double RowHeight = 14;
        private const double DayHeaderHeight = 14;
        private const double TableHeaderHeight = 14;
        private const double BlockHeaderHeight = 18;
        private const double FirstContentStartY = 424;
        private const double FirstContentStartWithPreviousY = 404;
        private const double ContinuationContentStartY = 522;
        private const double FooterLimitY = 34;
        private const double TableLeft = Margin;
        private const double TableWidth = PageWidth - (Margin * 2);
        private static readonly CultureInfo Cultura = new("es-PE");

        public static void Exportar(
            string ruta,
            Empresa? empresa,
            DateTime fechaDesde,
            DateTime fechaHasta,
            string proveedor,
            string estado,
            bool incluyePendientesAnteriores,
            IEnumerable<CuentaPorPagarProgramacionItem> pagos)
        {
            List<CuentaPorPagarProgramacionItem> filas = pagos
                .OrderBy(p => p.FechaVencimiento)
                .ThenBy(p => p.NombreProveedor)
                .ThenBy(p => p.NumeroCuota)
                .ToList();

            List<CuentaPorPagarProgramacionItem> pendientesAnteriores = incluyePendientesAnteriores
                ? filas.Where(p => EsPendienteConSaldo(p) && p.FechaVencimiento.Date < fechaDesde.Date).ToList()
                : [];
            List<CuentaPorPagarProgramacionItem> semana = incluyePendientesAnteriores
                ? filas.Where(p => p.FechaVencimiento.Date >= fechaDesde.Date && p.FechaVencimiento.Date <= fechaHasta.Date).ToList()
                : filas;

            ProformaPdfExporter.SimplePdfDocument document = new();
            ProformaPdfExporter.PdfCanvas canvas = NuevaPagina(document, empresa, fechaDesde, fechaHasta, proveedor, estado, incluyePendientesAnteriores, filas.Count, pendientesAnteriores, semana, 1, true);

            double y = incluyePendientesAnteriores ? FirstContentStartWithPreviousY : FirstContentStartY;

            int pagina = 1;
            if (incluyePendientesAnteriores)
            {
                DibujarBloque(document, ref canvas, empresa, fechaDesde, fechaHasta, proveedor, estado, incluyePendientesAnteriores, filas.Count, pendientesAnteriores, semana, ref pagina, ref y, "PENDIENTES ANTERIORES", "Cuotas vencidas antes del inicio de la semana seleccionada.", pendientesAnteriores);
                y -= 6;
                DibujarBloque(document, ref canvas, empresa, fechaDesde, fechaHasta, proveedor, estado, incluyePendientesAnteriores, filas.Count, pendientesAnteriores, semana, ref pagina, ref y, $"PROGRAMACION DE LA SEMANA {fechaDesde:dd/MM/yyyy} AL {fechaHasta:dd/MM/yyyy}", "Cuotas con vencimiento dentro de la semana seleccionada.", semana);
            }
            else
            {
                DibujarBloque(document, ref canvas, empresa, fechaDesde, fechaHasta, proveedor, estado, incluyePendientesAnteriores, filas.Count, pendientesAnteriores, semana, ref pagina, ref y, $"PROGRAMACION DE LA SEMANA {fechaDesde:dd/MM/yyyy} AL {fechaHasta:dd/MM/yyyy}", "Cuotas de la semana visible en pantalla.", semana);
            }

            if (filas.Count == 0)
                canvas.CenterText("No existen pagos programados para el rango seleccionado.", PageWidth / 2, 300, 12, true, 100, 116, 139);

            document.Save(ruta);
        }

        private static ProformaPdfExporter.PdfCanvas NuevaPagina(
            ProformaPdfExporter.SimplePdfDocument document,
            Empresa? empresa,
            DateTime fechaDesde,
            DateTime fechaHasta,
            string proveedor,
            string estado,
            bool incluyePendientesAnteriores,
            int cantidad,
            List<CuentaPorPagarProgramacionItem> pendientesAnteriores,
            List<CuentaPorPagarProgramacionItem> semana,
            int pagina,
            bool cabeceraCompleta)
        {
            ProformaPdfExporter.PdfCanvas canvas = document.AddPage(PageWidth, PageHeight);
            if (cabeceraCompleta)
                DibujarCabecera(canvas, empresa, fechaDesde, fechaHasta, proveedor, estado, incluyePendientesAnteriores, cantidad, pendientesAnteriores, semana);
            else
                DibujarCabeceraCompacta(canvas, empresa, fechaDesde, fechaHasta, proveedor, estado, incluyePendientesAnteriores);

            canvas.Text($"Pagina {pagina}", PageWidth - Margin - 55, 22, 8, false, 100, 116, 139);
            return canvas;
        }

        private static void DibujarCabecera(
            ProformaPdfExporter.PdfCanvas canvas,
            Empresa? empresa,
            DateTime fechaDesde,
            DateTime fechaHasta,
            string proveedor,
            string estado,
            bool incluyePendientesAnteriores,
            int cantidad,
            List<CuentaPorPagarProgramacionItem> pendientesAnteriores,
            List<CuentaPorPagarProgramacionItem> semana)
        {
            string nombreEmpresa = empresa == null
                ? "COREXPROD"
                : string.IsNullOrWhiteSpace(empresa.NombreComercial) ? empresa.Nombre : empresa.NombreComercial;

            canvas.Text(Limpiar(nombreEmpresa).ToUpperInvariant(), Margin, 560, 13, true, 15, 23, 42);
            canvas.Text($"Emitido: {DateTime.Now:dd/MM/yyyy HH:mm}", PageWidth - Margin - 145, 560, 8, false, 71, 85, 105);
            canvas.Text("REPORTE SEMANAL DE PAGOS", Margin, 535, 18, true, 37, 99, 235);
            canvas.Text($"Periodo: {fechaDesde:dd/MM/yyyy} al {fechaHasta:dd/MM/yyyy}", Margin, 514, 10, true, 15, 23, 42);
            canvas.Text($"Proveedor: {Limpiar(proveedor)}", Margin, 498, 9, false, 51, 65, 85);
            canvas.Text($"Estado: {Limpiar(estado)}", 360, 498, 9, false, 51, 65, 85);
            canvas.Text($"Cuotas: {cantidad}", 540, 498, 9, false, 51, 65, 85);
            canvas.Text(incluyePendientesAnteriores ? "Incluye pendientes anteriores: SI" : "Incluye pendientes anteriores: NO", 635, 498, 9, false, 51, 65, 85);

            double y = 472;
            DibujarResumenFinanciero(canvas, "SEMANA", semana, y, 15, 23, 42);
            y -= 13;
            if (incluyePendientesAnteriores)
            {
                DibujarResumenFinanciero(canvas, "PENDIENTES ANTERIORES", pendientesAnteriores, y, 180, 83, 9);
                y -= 13;
                DibujarResumenFinanciero(canvas, "TOTAL GENERAL", pendientesAnteriores.Concat(semana), y, 37, 99, 235);
            }
        }

        private static void DibujarCabeceraCompacta(
            ProformaPdfExporter.PdfCanvas canvas,
            Empresa? empresa,
            DateTime fechaDesde,
            DateTime fechaHasta,
            string proveedor,
            string estado,
            bool incluyePendientesAnteriores)
        {
            string nombreEmpresa = empresa == null
                ? "COREXPROD"
                : string.IsNullOrWhiteSpace(empresa.NombreComercial) ? empresa.Nombre : empresa.NombreComercial;

            canvas.Text(Limpiar(nombreEmpresa).ToUpperInvariant(), Margin, 560, 11, true, 15, 23, 42);
            canvas.Text("REPORTE SEMANAL DE PAGOS", Margin, 540, 14, true, 37, 99, 235);
            canvas.Text($"Periodo: {fechaDesde:dd/MM/yyyy} al {fechaHasta:dd/MM/yyyy}", Margin, 521, 8, true, 15, 23, 42);
            canvas.Text($"Proveedor: {Limpiar(proveedor)}", 205, 521, 8, false, 51, 65, 85);
            canvas.Text($"Estado: {Limpiar(estado)}", 455, 521, 8, false, 51, 65, 85);
            canvas.Text(incluyePendientesAnteriores ? "Incluye anteriores: SI" : "Incluye anteriores: NO", 625, 521, 8, false, 51, 65, 85);
            canvas.Text($"Emitido: {DateTime.Now:dd/MM/yyyy HH:mm}", PageWidth - Margin - 140, 560, 8, false, 71, 85, 105);
        }

        private static void DibujarBloque(
            ProformaPdfExporter.SimplePdfDocument document,
            ref ProformaPdfExporter.PdfCanvas canvas,
            Empresa? empresa,
            DateTime fechaDesde,
            DateTime fechaHasta,
            string proveedor,
            string estado,
            bool incluyePendientesAnteriores,
            int cantidad,
            List<CuentaPorPagarProgramacionItem> pendientesAnteriores,
            List<CuentaPorPagarProgramacionItem> semana,
            ref int pagina,
            ref double y,
            string titulo,
            string subtitulo,
            List<CuentaPorPagarProgramacionItem> filas)
        {
            NuevaPaginaSiNecesario(document, ref canvas, empresa, fechaDesde, fechaHasta, proveedor, estado, incluyePendientesAnteriores, cantidad, pendientesAnteriores, semana, ref pagina, ref y, BlockHeaderHeight + TableHeaderHeight + RowHeight);
            canvas.FilledRectangle(TableLeft, y - 3, TableWidth, BlockHeaderHeight, 226, 232, 240);
            canvas.Text(Limpiar(titulo), TableLeft + 8, y + 3, 8, true, 15, 23, 42);
            canvas.Text(Limpiar(subtitulo), TableLeft + 365, y + 3, 7, false, 71, 85, 105);
            y -= BlockHeaderHeight;
            DibujarEncabezadoTabla(canvas, y);
            y -= TableHeaderHeight;

            if (filas.Count == 0)
            {
                canvas.Text("Sin cuotas para este bloque.", TableLeft + 8, y + 2, 7, false, 100, 116, 139);
                y -= RowHeight;
                return;
            }

            DateTime? fechaActual = null;
            foreach (CuentaPorPagarProgramacionItem pago in filas)
            {
                if (fechaActual != pago.FechaVencimiento.Date)
                {
                    if (NuevaPaginaSiNecesario(document, ref canvas, empresa, fechaDesde, fechaHasta, proveedor, estado, incluyePendientesAnteriores, cantidad, pendientesAnteriores, semana, ref pagina, ref y, DayHeaderHeight + RowHeight))
                    {
                        DibujarEncabezadoTabla(canvas, y);
                        y -= TableHeaderHeight;
                    }

                    fechaActual = pago.FechaVencimiento.Date;
                    canvas.FilledRectangle(TableLeft, y - 2, TableWidth, DayHeaderHeight, 239, 246, 255);
                    canvas.Text(Limpiar($"{NombreDia(fechaActual.Value)} {fechaActual.Value:dd/MM/yyyy}").ToUpperInvariant(), TableLeft + 6, y + 2, 7, true, 30, 64, 175);
                    y -= DayHeaderHeight;
                }

                if (NuevaPaginaSiNecesario(document, ref canvas, empresa, fechaDesde, fechaHasta, proveedor, estado, incluyePendientesAnteriores, cantidad, pendientesAnteriores, semana, ref pagina, ref y, RowHeight))
                {
                    DibujarEncabezadoTabla(canvas, y);
                    y -= TableHeaderHeight;
                }

                DibujarFila(canvas, pago, y);
                y -= RowHeight;
            }
        }

        private static bool NuevaPaginaSiNecesario(
            ProformaPdfExporter.SimplePdfDocument document,
            ref ProformaPdfExporter.PdfCanvas canvas,
            Empresa? empresa,
            DateTime fechaDesde,
            DateTime fechaHasta,
            string proveedor,
            string estado,
            bool incluyePendientesAnteriores,
            int cantidad,
            List<CuentaPorPagarProgramacionItem> pendientesAnteriores,
            List<CuentaPorPagarProgramacionItem> semana,
            ref int pagina,
            ref double y,
            double requerido)
        {
            if (y >= FooterLimitY + requerido)
                return false;

            pagina++;
            canvas = NuevaPagina(document, empresa, fechaDesde, fechaHasta, proveedor, estado, incluyePendientesAnteriores, cantidad, pendientesAnteriores, semana, pagina, false);
            y = ContinuationContentStartY;
            return true;
        }

        private static bool EsPendienteConSaldo(CuentaPorPagarProgramacionItem pago)
        {
            return pago.SaldoPendiente > 0
                && !pago.Estado.Equals("CANCELADA", StringComparison.OrdinalIgnoreCase)
                && !pago.Estado.Equals("ANULADA", StringComparison.OrdinalIgnoreCase);
        }

        private static void DibujarResumenFinanciero(
            ProformaPdfExporter.PdfCanvas canvas,
            string titulo,
            IEnumerable<CuentaPorPagarProgramacionItem> pagos,
            double y,
            byte red,
            byte green,
            byte blue)
        {
            List<ResumenMoneda> resumen = CrearResumenMoneda(pagos);

            if (resumen.Count == 0)
            {
                canvas.Text($"{titulo}: sin saldos", Margin, y, 7, true, red, green, blue);
                return;
            }

            canvas.Text($"{titulo}:", Margin, y, 7, true, red, green, blue);
            canvas.Text($"TOTAL CUOTAS: {ValoresTexto(resumen, r => r.Importe)}", 150, y, 6.5, true, 15, 23, 42);
            canvas.Text($"TOTAL CANCELADO: {ValoresTexto(resumen, r => r.TotalPagado)}", 375, y, 6.5, true, 15, 23, 42);
            canvas.Text($"TOTAL PENDIENTE: {ValoresTexto(resumen, r => r.SaldoPendiente)}", 605, y, 6.5, true, red, green, blue);
        }

        private static string ValoresTexto(IEnumerable<ResumenMoneda> resumen, Func<ResumenMoneda, decimal> selector)
        {
            return string.Join(", ", resumen.Select(r => $"{r.Moneda} {r.Simbolo} {selector(r).ToString("N2", Cultura)}"));
        }

        private static List<ResumenMoneda> CrearResumenMoneda(IEnumerable<CuentaPorPagarProgramacionItem> pagos)
        {
            return pagos
                .GroupBy(p => p.Moneda)
                .OrderBy(g => g.Key)
                .Select(g => new ResumenMoneda(
                    g.Key,
                    CuentaPorPagarProgramacionItem.ObtenerSimbolo(g.Key),
                    g.Sum(p => p.Importe),
                    g.Sum(p => p.TotalPagado),
                    g.Sum(p => p.SaldoPendiente)))
                .ToList();
        }

        private sealed record ResumenMoneda(string Moneda, string Simbolo, decimal Importe, decimal TotalPagado, decimal SaldoPendiente);

        private static void DibujarEncabezadoTabla(ProformaPdfExporter.PdfCanvas canvas, double y)
        {
            canvas.FilledRectangle(TableLeft, y - 3, TableWidth, TableHeaderHeight, 30, 41, 59);
            string[] headers = ["Venc.", "Proveedor", "Estado", "Tipo", "Letra", "Cuota", "Mon.", "Importe", "Pagado", "Saldo"];
            double[] xs = [32, 72, 214, 286, 397, 492, 535, 590, 660, 730];
            for (int i = 0; i < headers.Length; i++)
                canvas.Text(headers[i], xs[i], y + 1, 6, true, 255, 255, 255);
        }

        private static void DibujarFila(ProformaPdfExporter.PdfCanvas canvas, CuentaPorPagarProgramacionItem pago, double y)
        {
            canvas.Line(TableLeft, y - 3, TableLeft + TableWidth, y - 3, 226, 232, 240);
            canvas.Text(pago.FechaVencimiento.ToString("dd/MM"), 32, y + 1, 6);
            canvas.Text(Truncar(Limpiar(pago.NombreProveedor), 30), 72, y + 1, 6);
            DibujarEstado(canvas, pago, 214, y);
            canvas.Text(Truncar(Limpiar(pago.TipoObligacion), 22), 286, y + 1, 6);
            canvas.Text(Truncar(Limpiar(pago.ReferenciaObligacion), 18), 397, y + 1, 6);
            canvas.Text(pago.CuotaTexto, 492, y + 1, 6);
            canvas.Text(Limpiar(pago.Moneda), 535, y + 1, 6);
            canvas.RightText(Limpiar(pago.ImporteTexto), 640, y + 1, 6);
            canvas.RightText(Limpiar(pago.TotalPagadoTexto), 710, y + 1, 6);
            canvas.RightText(Limpiar(pago.SaldoPendienteTexto), 780, y + 1, 6);
        }

        private static void DibujarEstado(ProformaPdfExporter.PdfCanvas canvas, CuentaPorPagarProgramacionItem pago, double x, double y)
        {
            (byte bgR, byte bgG, byte bgB, byte borderR, byte borderG, byte borderB, byte textR, byte textG, byte textB) = EstadoColor(pago.Estado);
            canvas.FilledRoundedRectangle(x, y - 1, 60, 10, 4, bgR, bgG, bgB);
            canvas.RoundedRectangle(x, y - 1, 60, 10, 4, borderR, borderG, borderB);
            canvas.CenterText(Truncar(Limpiar(pago.Estado), 10), x + 30, y + 1, 5.5, true, textR, textG, textB);
        }

        private static (byte bgR, byte bgG, byte bgB, byte borderR, byte borderG, byte borderB, byte textR, byte textG, byte textB) EstadoColor(string estado)
        {
            return estado?.Trim().ToUpperInvariant() switch
            {
                "CANCELADA" => (220, 252, 231, 34, 197, 94, 22, 101, 52),
                "PARCIAL" => (254, 243, 199, 245, 158, 11, 146, 64, 14),
                "PENDIENTE" => (254, 226, 226, 239, 68, 68, 185, 28, 28),
                _ => (226, 232, 240, 100, 116, 139, 51, 65, 85)
            };
        }

        private static string NombreDia(DateTime fecha) => Cultura.DateTimeFormat.GetDayName(fecha.DayOfWeek);

        private static string Truncar(string texto, int max)
        {
            if (string.IsNullOrEmpty(texto) || texto.Length <= max)
                return texto;

            return texto[..Math.Max(0, max - 3)] + "...";
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
                    '°' => '.',
                    _ => c <= 127 ? c : ' '
                });
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
