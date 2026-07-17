using CorexProd.Entidad.Entidades;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Resources;

namespace CorexProd.WPF.Helpers
{
    internal static class OrdenServicioPdfExporter
    {
        private const double PageWidth = 595;
        private const double PageHeight = 842;
        private const double Margin = 85;
        private const double ContentWidth = 430;

        public static void Exportar(string ruta, Empresa empresa, OrdenServicio orden, bool incluirFotos)
        {
            ProformaPdfExporter.SimplePdfDocument document = new();
            ProformaPdfExporter.PdfCanvas canvas = document.AddPage(PageWidth, PageHeight);
            DibujarMarcaAnulada(canvas, orden);
            double y = PageHeight - 52;

            DibujarCabecera(canvas, empresa, ref y);
            DibujarTitulo(canvas, orden, ref y);
            DibujarResumen(canvas, orden, ref y);
            DibujarACuenta(canvas, orden, ref y);
            DibujarFotosEnPaginasSiguientes(document, orden, incluirFotos);

            document.Save(ruta);
        }

        private static void DibujarCabecera(ProformaPdfExporter.PdfCanvas c, Empresa empresa, ref double y)
        {
            byte[]? logo = ObtenerLogo(empresa);
            if (logo != null)
                c.Image(logo, Margin, y - 72, 180, 72);
            else
                c.Text("DELTA CONFECCIONES", Margin, y - 30, 20, true);

            y -= 78;
            c.Line(Margin, y, PageWidth - Margin, y);
            c.Line(Margin, y - 1.5, PageWidth - Margin, y - 1.5);
            y -= 28;
        }

        private static void DibujarTitulo(ProformaPdfExporter.PdfCanvas c, OrdenServicio orden, ref double y)
        {
            c.Text($"Orden de servicio {Limpiar(orden.NumeroOrden)}", Margin, y, 14, true);
            c.RightText($"Lima, {FechaLarga(orden.Fecha)}", PageWidth - Margin, y - 28, 11);
            y -= 54;

            string proveedor = string.IsNullOrWhiteSpace(orden.NombreProveedor)
                ? "Proveedor"
                : FormatoNombre(orden.NombreProveedor);
            c.Text($"Senor: {Limpiar(proveedor)}", Margin, y, 11);
            y -= 28;

            string nombrePrincipal = string.IsNullOrWhiteSpace(orden.TipoServicioNombre)
                ? "SERVICIO"
                : orden.TipoServicioNombre.ToUpperInvariant();
            c.Text(Limpiar(nombrePrincipal), Margin, y, 14, true);
            c.Line(Margin, y - 3, Margin + Math.Min(ContentWidth, c.MeasureText(Limpiar(nombrePrincipal), 14, true)), y - 3);
            y -= 28;
            y -= 6;

            decimal cantidadTotal = orden.Detalles.Sum(d => d.Cantidad);
            c.Text("CANTIDAD:", Margin, y, 11, true);
            c.Line(Margin, y - 3, Margin + 64, y - 3);
            c.Text(cantidadTotal.ToString("N0"), Margin + 70, y, 11);
            y -= 28;

            c.Text("RESUMEN:", Margin, y, 11, true);
            c.Line(Margin, y - 3, Margin + 62, y - 3);
            y -= 20;
        }

        private static void DibujarResumen(ProformaPdfExporter.PdfCanvas c, OrdenServicio orden, ref double y)
        {
            double x = Margin;
            double descW = 246;
            double cantW = 44;
            double precioW = 64;
            double totalW = ContentWidth - descW - cantW - precioW;
            double headerH = 32;

            c.FilledRectangle(x, y - headerH, ContentWidth, headerH, 160, 197, 228);
            c.CenterText("DESCRIPCION", x + descW / 2, y - 21, 12, true);
            c.CenterText("CANT", x + descW + cantW / 2, y - 21, 12, true);
            c.CenterText("PRECIO", x + descW + cantW + precioW / 2, y - 14, 11, true);
            c.CenterText("UNIT", x + descW + cantW + precioW / 2, y - 27, 11, true);
            c.CenterText("TOTAL", x + descW + cantW + precioW + totalW / 2, y - 21, 12, true);
            DibujarColumnas(c, x, y - headerH, descW, cantW, precioW, totalW, headerH);
            y -= headerH;

            foreach (OrdenServicioDetalle detalle in orden.Detalles)
            {
                List<string> descripcionLineas = PrepararDescripcionDetalle(detalle);
                double rowH = Math.Max(36, 22 + descripcionLineas.Count * 11);
                c.Rectangle(x, y - rowH, ContentWidth, rowH);
                DibujarColumnas(c, x, y - rowH, descW, cantW, precioW, totalW, rowH);
                c.Text(Limpiar(detalle.Producto).ToUpperInvariant(), x + 6, y - 13, 9, true);
                for (int i = 0; i < descripcionLineas.Count; i++)
                    c.Text(descripcionLineas[i], x + 18, y - 25 - i * 11, 8);
                c.CenterText(detalle.Cantidad.ToString("N0"), x + descW + cantW / 2, y - rowH / 2 - 3, 11);
                c.CenterText(Moneda(detalle.PrecioUnitario, espacio: true), x + descW + cantW + precioW / 2, y - rowH / 2 - 3, 11);
                c.CenterText(Moneda(detalle.Total, espacio: false), x + descW + cantW + precioW + totalW / 2, y - rowH / 2 - 3, 11);
                y -= rowH;
            }

            double totalH = 18;
            c.Rectangle(x, y - totalH, ContentWidth, totalH);
            c.Line(x + descW + cantW + precioW, y, x + descW + cantW + precioW, y - totalH);
            c.RightText("TOTAL", x + descW + cantW + precioW - 6, y - 13, 11);
            c.CenterText(Moneda(orden.Total, espacio: false), x + descW + cantW + precioW + totalW / 2, y - 13, 12, true);
            y -= totalH + 28;
        }

        private static void DibujarColumnas(ProformaPdfExporter.PdfCanvas c, double x, double y, double descW, double cantW, double precioW, double totalW, double h)
        {
            c.Line(x + descW, y + h, x + descW, y);
            c.Line(x + descW + cantW, y + h, x + descW + cantW, y);
            c.Line(x + descW + cantW + precioW, y + h, x + descW + cantW + precioW, y);
        }

        private static void DibujarACuenta(ProformaPdfExporter.PdfCanvas c, OrdenServicio orden, ref double y)
        {
            c.Text($"A CUENTA: {Moneda(orden.TotalPagado, espacio: true)}", Margin, y, 14, true);
            c.Line(Margin, y - 3, Margin + 165, y - 3);
            y -= 28;
            if (orden.SaldoPendiente > 0)
            {
                c.Text($"SALDO: {Moneda(orden.SaldoPendiente, espacio: true)}", Margin, y, 12, true);
                y -= 22;
            }
        }

        private static void DibujarFotosEnPaginasSiguientes(ProformaPdfExporter.SimplePdfDocument document, OrdenServicio orden, bool incluirFotos)
        {
            if (!incluirFotos)
                return;

            List<OrdenServicioFoto> fotos = orden.Fotos
                .OrderBy(f => OrdenUbicacion(f.UbicacionPdf))
                .ThenBy(f => f.IdOrdenServicioFoto)
                .ToList();
            if (fotos.Count == 0)
                return;

            ProformaPdfExporter.PdfCanvas c = document.AddPage(PageWidth, PageHeight);
            DibujarMarcaAnulada(c, orden);
            double y = PageHeight - 60;
            c.Text("FOTOS / REFERENCIAS", Margin, y, 11, true);
            c.Line(Margin, y - 4, PageWidth - Margin, y - 4);
            y -= 26;

            foreach (OrdenServicioFoto foto in fotos)
            {
                double bloqueAlto = 245;
                if (y < Margin + bloqueAlto)
                {
                    c = document.AddPage(PageWidth, PageHeight);
                    DibujarMarcaAnulada(c, orden);
                    y = PageHeight - 60;
                    c.Text("FOTOS / REFERENCIAS", Margin, y, 11, true);
                    c.Line(Margin, y - 4, PageWidth - Margin, y - 4);
                    y -= 26;
                }

                string titulo = string.IsNullOrWhiteSpace(foto.Titulo) ? "Referencia" : foto.Titulo;
                c.Text(Limpiar(titulo).ToUpperInvariant(), Margin, y, 10, true);
                y -= 14;

                double imageW = 300;
                double imageH = 190;
                bool dibujo = c.Image(foto.RutaArchivo, Margin, y - imageH, imageW, imageH);
                if (dibujo)
                {
                    if (!string.IsNullOrWhiteSpace(foto.Descripcion))
                    {
                        double descY = y - 14;
                        foreach (string linea in DividirLineas(Limpiar(foto.Descripcion), 32).Take(8))
                        {
                            c.Text(linea, Margin + imageW + 16, descY, 8);
                            descY -= 11;
                        }
                    }
                    y -= imageH + 28;
                }
                else
                {
                    c.Text($"Archivo: {Limpiar(foto.NombreArchivo)}", Margin + 10, y - 12, 8);
                    y -= 26;
                }
            }
        }

        private static byte[]? ObtenerLogo(Empresa empresa)
        {
            if (empresa.Logo is { Length: > 0 }) return empresa.Logo;
            try
            {
                StreamResourceInfo? resource = Application.GetResourceStream(new Uri("pack://application:,,,/Images/LOGO.png", UriKind.Absolute));
                if (resource?.Stream == null) return null;
                using MemoryStream stream = new();
                resource.Stream.CopyTo(stream);
                return stream.ToArray();
            }
            catch { return null; }
        }

        private static void DibujarMarcaAnulada(ProformaPdfExporter.PdfCanvas c, OrdenServicio orden)
        {
            if (!orden.EstaAnulada)
                return;

            c.RotatedCenterText("ANULADA", PageWidth / 2, PageHeight / 2, 78, 35, true, 220, 220, 220);
        }

        private static bool EsUbicacion(OrdenServicioFoto foto, string ubicacion) =>
            string.Equals(foto.UbicacionPdf, ubicacion, StringComparison.OrdinalIgnoreCase);

        private static int OrdenUbicacion(string ubicacion)
        {
            if (ubicacion.Equals("Antes del resumen", StringComparison.OrdinalIgnoreCase)) return 1;
            if (ubicacion.Equals("Abajo", StringComparison.OrdinalIgnoreCase)) return 2;
            if (ubicacion.Equals("Pagina final", StringComparison.OrdinalIgnoreCase)) return 3;
            return 4;
        }

        private static string FechaLarga(DateTime fecha)
        {
            string[] meses = ["ENERO", "FEBRERO", "MARZO", "ABRIL", "MAYO", "JUNIO", "JULIO", "AGOSTO", "SETIEMBRE", "OCTUBRE", "NOVIEMBRE", "DICIEMBRE"];
            return $"{fecha:dd} de {meses[fecha.Month - 1]} del {fecha:yyyy}";
        }

        private static string Moneda(decimal value, bool espacio) => espacio ? $"S/ {value:N2}" : $"S/{value:N2}";

        private static List<string> DividirLineas(string texto, int maximo)
        {
            List<string> lineas = [];
            string pendiente = texto.Trim();
            while (pendiente.Length > maximo)
            {
                int corte = pendiente.LastIndexOf(' ', maximo);
                if (corte <= 0) corte = maximo;
                lineas.Add(pendiente[..corte].Trim());
                pendiente = pendiente[corte..].TrimStart();
            }
            lineas.Add(string.IsNullOrWhiteSpace(pendiente) ? "-" : pendiente);
            return lineas;
        }

        private static List<string> PrepararDescripcionDetalle(OrdenServicioDetalle detalle)
        {
            string descripcion = Limpiar(detalle.Descripcion);
            if (string.IsNullOrWhiteSpace(descripcion) || descripcion.Equals(detalle.Producto, StringComparison.OrdinalIgnoreCase))
                return [];

            List<string> lineas = [];
            foreach (string parte in descripcion.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                foreach (string linea in DividirLineas(parte, 34))
                    lineas.Add($"- {linea}");
            }
            return lineas;
        }

        private static string Limpiar(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return value
                .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u")
                .Replace("Á", "A").Replace("É", "E").Replace("Í", "I").Replace("Ó", "O").Replace("Ú", "U")
                .Replace("ñ", "n").Replace("Ñ", "N");
        }

        private static string FormatoNombre(string value)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.Trim().ToLowerInvariant());
        }
    }
}
