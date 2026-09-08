using ClosedXML.Excel;
using CorexProd.Entidad.Entidades;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CorexProd.WPF.Helpers
{
    public static class ProgramacionGeneralExcelExporter
    {
        private static readonly CultureInfo Cultura = new("es-PE");
        private static readonly XLColor ColorCancelada = XLColor.FromHtml("#C4D79B");
        private static readonly Regex EspaciosMultiplesRegex = new(@"\s+", RegexOptions.Compiled);
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

            using XLWorkbook workbook = new();
            IXLWorksheet hoja = workbook.Worksheets.Add("DETALLE");
            ConfigurarHoja(hoja);

            int fila = 1;
            List<ProgramacionPagoGeneralItem> facturasCredito = lista
                .Where(p => EsFacturaCredito(p.Categoria))
                .ToList();
            List<ProgramacionPagoGeneralItem> letras = lista
                .Where(p => EsLetra(p.Categoria))
                .ToList();
            List<ProgramacionPagoGeneralItem> otrasObligaciones = lista
                .Where(p => EsOtraObligacion(p.Categoria))
                .ToList();

            fila = EscribirTituloPrincipal(hoja, fila, fechaDesde, fechaHasta);
            fila += 2;
            fila = EscribirBloqueCxp(hoja, fila, "FACTURAS A CREDITO", facturasCredito, "SIN FACTURAS A CREDITO PARA EL RANGO");
            fila += 2;
            fila = EscribirBloqueCxp(hoja, fila, "LETRAS POR PAGAR", letras, "SIN LETRAS POR PAGAR PARA EL RANGO");
            fila += 2;
            fila = EscribirBloquesOtrasObligaciones(hoja, fila, otrasObligaciones, fechaDesde, incluyePendientesAnteriores);
            fila += 1;
            fila = EscribirResumenGeneral(hoja, fila, lista);

            hoja.Range(1, 1, Math.Max(fila, 1), 8).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            hoja.PageSetup.PrintAreas.Clear();
            hoja.PageSetup.PrintAreas.Add($"A1:H{Math.Max(fila - 1, 1)}");
            hoja.SheetView.FreezeRows(3);

            Directory.CreateDirectory(Path.GetDirectoryName(ruta) ?? ".");
            workbook.SaveAs(ruta);
        }

        private static int EscribirTituloPrincipal(
            IXLWorksheet hoja,
            int fila,
            DateTime fechaDesde,
            DateTime fechaHasta)
        {
            string anio = fechaDesde.Year == fechaHasta.Year ? fechaDesde.Year.ToString() : $"{fechaDesde.Year}-{fechaHasta.Year}";

            hoja.Range(fila, 1, fila, 8).Merge().Value = $"PROGRAMACION GENERAL DE PAGOS  {anio}";
            hoja.Range(fila, 1, fila, 8).Style.Font.Bold = true;
            hoja.Range(fila, 1, fila, 8).Style.Font.FontSize = 16;
            hoja.Range(fila, 1, fila, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            fila++;

            hoja.Range(fila, 1, fila, 4).Merge().Value = $"Periodo: {FormatearFecha(fechaDesde)} al {FormatearFecha(fechaHasta)}";
            hoja.Range(fila, 5, fila, 8).Merge().Value = $"Emitido: {DateTime.Now:dd/MM/yyyy HH:mm}";
            hoja.Range(fila, 1, fila, 8).Style.Font.Bold = true;
            hoja.Range(fila, 1, fila, 8).Style.Font.FontSize = 11;
            hoja.Range(fila, 5, fila, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            return fila;
        }

        private static int EscribirBloqueCxp(
            IXLWorksheet hoja,
            int fila,
            string titulo,
            List<ProgramacionPagoGeneralItem> pagos,
            string mensajeSinDatos)
        {
            EscribirTituloBloque(hoja, fila, titulo);
            fila++;

            EscribirEncabezadoSuperior(hoja, fila);
            fila++;

            int inicioDatos = fila;
            foreach (ProgramacionPagoGeneralItem pago in pagos)
            {
                hoja.Cell(fila, 1).Value = pago.EntidadProveedor;
                hoja.Cell(fila, 2).Value = ObtenerMonedaTexto(pago.Moneda, pago.Categoria);
                hoja.Cell(fila, 3).Value = pago.Importe;
                hoja.Cell(fila, 4).Value = ObtenerReferenciaSuperior(pago);
                hoja.Cell(fila, 5).Value = pago.FechaDocumento.HasValue ? FormatearFecha(pago.FechaDocumento.Value) : string.Empty;
                hoja.Cell(fila, 6).Value = pago.Concepto;
                hoja.Cell(fila, 7).Value = ObtenerCuotaTexto(pago);
                hoja.Cell(fila, 8).Value = FormatearFecha(pago.FechaVencimiento);
                FormatearFilaSuperior(hoja, fila, pago);
                fila++;
            }

            if (pagos.Count == 0)
            {
                hoja.Range(fila, 1, fila, 8).Merge().Value = mensajeSinDatos;
                hoja.Range(fila, 1, fila, 8).Style.Font.Italic = true;
                hoja.Range(fila, 1, fila, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                AplicarBordes(hoja.Range(fila, 1, fila, 8));
                fila++;
            }

            fila = EscribirTotalesMoneda(hoja, fila, inicioDatos, pagos);
            return fila;
        }

        private static int EscribirBloquesOtrasObligaciones(
            IXLWorksheet hoja,
            int fila,
            List<ProgramacionPagoGeneralItem> pagos,
            DateTime fechaDesde,
            bool incluyePendientesAnteriores)
        {
            EscribirTituloBloque(hoja, fila, "OTRAS OBLIGACIONES");
            fila++;

            if (pagos.Count == 0)
            {
                EscribirEncabezadoInferior(hoja, fila);
                fila++;
                hoja.Range(fila, 1, fila, 8).Merge().Value = "SIN OTRAS OBLIGACIONES PARA EL RANGO";
                hoja.Range(fila, 1, fila, 8).Style.Font.Italic = true;
                hoja.Range(fila, 1, fila, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                AplicarBordes(hoja.Range(fila, 1, fila, 8));
                return fila + 1;
            }

            foreach (IGrouping<string, ProgramacionPagoGeneralItem> grupoMes in pagos
                .GroupBy(p => $"{p.FechaVencimiento:yyyy-MM}")
                .OrderBy(g => g.Min(p => p.FechaVencimiento)))
            {
                DateTime fechaMes = grupoMes.Min(p => p.FechaVencimiento);
                EscribirTituloVencimiento(hoja, fila, $"(VENCIMIENTO: {Meses[fechaMes.Month - 1]} {fechaMes:yyyy})");
                fila++;

                foreach (IGrouping<DateTime, ProgramacionPagoGeneralItem> grupoSemana in grupoMes
                    .GroupBy(p => ObtenerInicioSemana(p.FechaVencimiento))
                    .OrderBy(g => g.Key))
                {
                    DateTime inicioSemana = grupoSemana.Key;
                    DateTime finSemana = inicioSemana.AddDays(6);
                    string tituloSemana = incluyePendientesAnteriores && grupoSemana.Any(p => p.FechaVencimiento < fechaDesde)
                        ? "PENDIENTES ANTERIORES"
                        : $"SEMANA {inicioSemana:dd/MM/yyyy} AL {finSemana:dd/MM/yyyy}";

                    hoja.Range(fila, 1, fila, 8).Merge().Value = tituloSemana;
                    hoja.Range(fila, 1, fila, 8).Style.Font.Bold = true;
                    hoja.Range(fila, 1, fila, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#E2F0D9");
                    hoja.Range(fila, 1, fila, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    AplicarBordes(hoja.Range(fila, 1, fila, 8));
                    fila++;

                    EscribirEncabezadoInferior(hoja, fila);
                    fila++;

                    int inicioDatos = fila;
                    foreach (ProgramacionPagoGeneralItem pago in grupoSemana.OrderBy(p => p.FechaVencimiento).ThenBy(p => p.EntidadProveedor))
                    {
                        hoja.Range(fila, 1, fila, 2).Merge().Value = ObtenerEntidadOtraObligacion(pago);
                        hoja.Cell(fila, 3).Value = pago.Importe;
                        hoja.Cell(fila, 4).Value = string.Empty;
                        hoja.Cell(fila, 5).Value = ObtenerCuotaTexto(pago);
                        hoja.Cell(fila, 6).Value = Cultura.TextInfo.ToTitleCase(Cultura.DateTimeFormat.GetDayName(pago.FechaVencimiento.DayOfWeek));
                        hoja.Cell(fila, 7).Value = FormatearFecha(pago.FechaVencimiento);
                        hoja.Cell(fila, 8).Value = string.Empty;
                        FormatearFilaInferior(hoja, fila, pago);
                        fila++;
                    }

                    fila = EscribirTotalesMoneda(hoja, fila, inicioDatos, grupoSemana.ToList());
                    fila++;
                }
            }

            return fila;
        }

        private static int EscribirResumenGeneral(IXLWorksheet hoja, int fila, List<ProgramacionPagoGeneralItem> pagos)
        {
            if (pagos.Count == 0)
                return fila;

            EscribirTituloBloque(hoja, fila, "RESUMEN GENERAL");
            fila++;

            foreach (IGrouping<string, ProgramacionPagoGeneralItem> totalMoneda in pagos.GroupBy(p => NormalizarMoneda(p.Moneda)).OrderBy(g => g.Key))
            {
                hoja.Range(fila, 1, fila, 2).Merge().Value = $"TOTAL GENERAL {totalMoneda.Key}";
                hoja.Cell(fila, 3).Value = totalMoneda.Sum(p => p.Importe);
                hoja.Range(fila, 1, fila, 3).Style.Font.Bold = true;
                hoja.Cell(fila, 3).Style.NumberFormat.Format = "#,##0.00";
                AplicarBordes(hoja.Range(fila, 1, fila, 8));
                fila++;
            }

            return fila;
        }

        private static void ConfigurarHoja(IXLWorksheet hoja)
        {
            hoja.Style.Font.FontName = "Calibri";
            hoja.Column(1).Width = 20;
            hoja.Column(2).Width = 7.43;
            hoja.Column(3).Width = 12.71;
            hoja.Column(4).Width = 11;
            hoja.Column(5).Width = 11.43;
            hoja.Column(6).Width = 11.29;
            hoja.Column(7).Width = 9.43;
            hoja.Column(8).Width = 10.29;

            hoja.PageSetup.PageOrientation = XLPageOrientation.Portrait;
            hoja.PageSetup.PaperSize = XLPaperSize.A4Paper;
            hoja.PageSetup.PagesWide = 1;
            hoja.PageSetup.PagesTall = 0;
            hoja.PageSetup.Margins.Left = 0.25;
            hoja.PageSetup.Margins.Right = 0.25;
            hoja.PageSetup.Margins.Top = 0.32;
            hoja.PageSetup.Margins.Bottom = 0.25;
            hoja.PageSetup.CenterHorizontally = true;
        }

        private static void EscribirEncabezadoSuperior(IXLWorksheet hoja, int fila)
        {
            hoja.Cell(fila, 1).Value = "PROVEEDOR";
            hoja.Cell(fila, 2).Value = "MON.";
            hoja.Cell(fila, 3).Value = "IMPORTE";
            hoja.Cell(fila, 4).Value = "REFERENCIA";
            hoja.Cell(fila, 5).Value = "FECHA DOC.";
            hoja.Cell(fila, 6).Value = "CONCEPTO";
            hoja.Cell(fila, 7).Value = "CUOTA";
            hoja.Cell(fila, 8).Value = "Vencimiento";
            FormatearEncabezado(hoja.Range(fila, 1, fila, 8));
        }

        private static void EscribirEncabezadoInferior(IXLWorksheet hoja, int fila)
        {
            hoja.Range(fila, 1, fila, 2).Merge().Value = "ENTIDAD / CONCEPTO";
            hoja.Cell(fila, 3).Value = "IMPORTE";
            hoja.Cell(fila, 4).Value = "BANCO";
            hoja.Cell(fila, 5).Value = "CUOTA";
            hoja.Cell(fila, 6).Value = "DIA";
            hoja.Cell(fila, 7).Value = "VENCIMIENTO";
            hoja.Cell(fila, 8).Value = "N° UNICO";
            FormatearEncabezado(hoja.Range(fila, 1, fila, 8));
        }

        private static void EscribirTituloVencimiento(IXLWorksheet hoja, int fila, string titulo)
        {
            hoja.Range(fila, 1, fila, 8).Merge().Value = titulo;
            hoja.Range(fila, 1, fila, 8).Style.Font.Bold = true;
            hoja.Range(fila, 1, fila, 8).Style.Font.FontSize = 14;
            hoja.Range(fila, 1, fila, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#B6D7A8");
            hoja.Range(fila, 1, fila, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            AplicarBordes(hoja.Range(fila, 1, fila, 8));
        }

        private static void EscribirTituloBloque(IXLWorksheet hoja, int fila, string titulo)
        {
            hoja.Range(fila, 1, fila, 8).Merge().Value = titulo;
            hoja.Range(fila, 1, fila, 8).Style.Font.Bold = true;
            hoja.Range(fila, 1, fila, 8).Style.Font.FontSize = 14;
            hoja.Range(fila, 1, fila, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#B6D7A8");
            hoja.Range(fila, 1, fila, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            AplicarBordes(hoja.Range(fila, 1, fila, 8));
        }

        private static int EscribirTotalesMoneda(IXLWorksheet hoja, int fila, int inicioDatos, IReadOnlyCollection<ProgramacionPagoGeneralItem> pagos)
        {
            if (pagos.Count == 0)
                return fila;

            foreach (IGrouping<string, ProgramacionPagoGeneralItem> totalMoneda in pagos.GroupBy(p => NormalizarMoneda(p.Moneda)).OrderBy(g => g.Key))
            {
                hoja.Range(fila, 1, fila, 2).Merge().Value = $"TOTAL {totalMoneda.Key}";
                hoja.Range(fila, 1, fila, 2).Style.Font.Bold = true;
                hoja.Cell(fila, 3).Value = totalMoneda.Sum(p => p.Importe);
                hoja.Cell(fila, 3).Style.NumberFormat.Format = "#,##0.00";
                hoja.Cell(fila, 3).Style.Font.Bold = true;
                hoja.Range(fila, 1, fila, 8).Style.Border.TopBorder = XLBorderStyleValues.Medium;
                hoja.Range(fila, 1, fila, 8).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                fila++;
            }

            return fila;
        }

        private static void FormatearEncabezado(IXLRange rango)
        {
            rango.Style.Font.Bold = true;
            rango.Style.Font.FontSize = 11;
            rango.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E2F3");
            rango.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            rango.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            AplicarBordes(rango);
        }

        private static void FormatearFilaSuperior(IXLWorksheet hoja, int fila, ProgramacionPagoGeneralItem pago)
        {
            hoja.Row(fila).Height = 21;
            hoja.Range(fila, 1, fila, 8).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            hoja.Range(fila, 1, fila, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            hoja.Cell(fila, 1).Style.Font.Bold = true;
            hoja.Cell(fila, 1).Style.Alignment.WrapText = true;
            hoja.Range(fila, 6, fila, 7).Style.Alignment.WrapText = true;
            hoja.Cell(fila, 3).Style.NumberFormat.Format = "#,##0.00";
            if ((hoja.Cell(fila, 1).GetString().Length + hoja.Cell(fila, 6).GetString().Length) > 70)
                hoja.Row(fila).Height = 32;
            AplicarColorEstado(hoja.Range(fila, 1, fila, 8), pago);
            AplicarBordes(hoja.Range(fila, 1, fila, 8));
        }

        private static void FormatearFilaInferior(IXLWorksheet hoja, int fila, ProgramacionPagoGeneralItem pago)
        {
            hoja.Row(fila).Height = 22;
            hoja.Range(fila, 1, fila, 8).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            hoja.Range(fila, 1, fila, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            hoja.Range(fila, 1, fila, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            hoja.Range(fila, 1, fila, 2).Style.Alignment.WrapText = true;
            hoja.Cell(fila, 3).Style.NumberFormat.Format = "#,##0.00";
            if (hoja.Cell(fila, 1).GetString().Length > 40)
                hoja.Row(fila).Height = 32;
            AplicarColorEstado(hoja.Range(fila, 1, fila, 8), pago);
            AplicarBordes(hoja.Range(fila, 1, fila, 8));
        }

        private static void AplicarColorEstado(IXLRange rango, ProgramacionPagoGeneralItem pago)
        {
            if (pago.Estado.Equals("CANCELADA", StringComparison.OrdinalIgnoreCase))
                rango.Style.Fill.BackgroundColor = ColorCancelada;
        }

        private static void AplicarBordes(IXLRange rango)
        {
            rango.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            rango.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        }

        private static string ObtenerMonedaTexto(string moneda, string categoria)
        {
            return NormalizarMoneda(moneda) switch
            {
                "PEN" => "Soles",
                "USD" => "US$",
                _ => NormalizarMoneda(moneda)
            };
        }

        private static string ObtenerReferenciaSuperior(ProgramacionPagoGeneralItem pago) => pago.Referencia;

        internal static string ObtenerEntidadOtraObligacion(ProgramacionPagoGeneralItem pago)
        {
            string entidad = pago.EntidadProveedor?.Trim() ?? string.Empty;
            string concepto = pago.Concepto?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(concepto))
                return entidad;

            if (string.IsNullOrWhiteSpace(entidad))
                return concepto;

            string entidadNormalizada = NormalizarTextoComparacion(entidad);
            string conceptoNormalizado = NormalizarTextoComparacion(concepto);

            if (entidadNormalizada.Contains(conceptoNormalizado, StringComparison.OrdinalIgnoreCase)
                || conceptoNormalizado.Contains(entidadNormalizada, StringComparison.OrdinalIgnoreCase))
                return entidad;

            string conceptoSinAcronimo = QuitarAcronimoEntidadAlInicio(concepto, entidad);
            if (string.IsNullOrWhiteSpace(conceptoSinAcronimo))
                return entidad;

            return $"{entidad} ({conceptoSinAcronimo})";
        }

        private static string QuitarAcronimoEntidadAlInicio(string concepto, string entidad)
        {
            string acronimo = ObtenerAcronimo(entidad);
            if (acronimo.Length < 2)
                return concepto;

            Match match = Regex.Match(
                concepto.Trim(),
                $"^{Regex.Escape(acronimo)}(?:\\s*[-:/]?\\s*|\\s*)(?<resto>.*)$",
                RegexOptions.IgnoreCase);

            if (!match.Success)
                return concepto;

            string resto = match.Groups["resto"].Value.Trim();
            if (resto.StartsWith("(") && resto.EndsWith(")") && resto.Length > 1)
                resto = resto[1..^1].Trim();

            return resto;
        }

        private static string ObtenerAcronimo(string texto)
        {
            string[] conectores = ["DE", "DEL", "LA", "LAS", "LOS", "EL", "Y", "E"];
            string[] palabras = Regex.Split(NormalizarTextoComparacion(texto), @"\s+")
                .Where(p => p.Length > 0 && !conectores.Contains(p))
                .ToArray();

            return new string(palabras.Select(p => p[0]).ToArray());
        }

        private static string NormalizarTextoComparacion(string texto)
        {
            string sinAcentos = new(
                texto.Normalize(NormalizationForm.FormD)
                    .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    .ToArray());

            string soloTexto = Regex.Replace(sinAcentos.ToUpperInvariant(), @"[^A-Z0-9]+", " ");
            return EspaciosMultiplesRegex.Replace(soloTexto, " ").Trim();
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
    }
}
