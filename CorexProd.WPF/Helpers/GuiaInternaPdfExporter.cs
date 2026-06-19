using CorexProd.Entidad.Entidades;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Resources;

namespace CorexProd.WPF.Helpers
{
    internal static class GuiaInternaPdfExporter
    {
        private const double PageWidth = 595;
        private const double PageHeight = 842;
        private const double Margin = 34;

        public static void Exportar(string ruta, Empresa empresa, GuiaInterna guia)
        {
            ProformaPdfExporter.SimplePdfDocument document = new();
            ProformaPdfExporter.PdfCanvas canvas = NuevaPagina(document, empresa, guia, out double y);
            DibujarDatos(canvas, guia, ref y);
            DibujarCabeceraDetalle(canvas, ref y);

            foreach (GuiaInternaDetalle detalle in guia.Detalles)
            {
                if (y < 155)
                {
                    canvas = NuevaPagina(document, empresa, guia, out y);
                    DibujarCabeceraDetalle(canvas, ref y);
                }

                DibujarDetalle(canvas, detalle, ref y);
            }

            if (y < 145)
                canvas = NuevaPagina(document, empresa, guia, out y);

            DibujarCierre(canvas, guia, ref y);
            document.Save(ruta);
        }

        private static ProformaPdfExporter.PdfCanvas NuevaPagina(ProformaPdfExporter.SimplePdfDocument document, Empresa empresa, GuiaInterna guia, out double y)
        {
            ProformaPdfExporter.PdfCanvas canvas = document.AddPage(PageWidth, PageHeight);
            y = PageHeight - Margin;
            byte[]? logo = ObtenerLogo(empresa);
            bool tieneLogo = logo != null && canvas.Image(logo, Margin, y - 45, 88, 44);
            double infoX = tieneLogo ? Margin + 98 : Margin;
            string nombre = string.IsNullOrWhiteSpace(empresa.NombreComercial) ? empresa.Nombre : empresa.NombreComercial;
            canvas.Text(Limpiar(nombre).ToUpperInvariant(), infoX, y, 13, true);
            canvas.Text($"RUC: {empresa.Ruc}", infoX, y - 15, 9);
            canvas.Text(Limpiar(empresa.Direccion), infoX, y - 28, 8);
            canvas.Text(Limpiar(Unir(empresa.Telefono, empresa.Correo)), infoX, y - 40, 8);

            double boxX = 405, boxY = y - 55, boxW = 156, boxH = 58;
            canvas.Rectangle(boxX, boxY, boxW, boxH);
            canvas.CenterText("GUIA INTERNA DE SALIDA", boxX + boxW / 2, boxY + 37, 10, true);
            canvas.CenterText(Limpiar(guia.NumeroGuia), boxX + boxW / 2, boxY + 18, 12, true);
            y -= 66;

            if (guia.EsAnulada)
            {
                canvas.CenterText("GUIA ANULADA", PageWidth / 2, y, 24, true, 185, 28, 28);
                y -= 28;
            }
            return canvas;
        }

        private static void DibujarDatos(ProformaPdfExporter.PdfCanvas c, GuiaInterna g, ref double y)
        {
            const double h = 19;
            double x = Margin, w = PageWidth - Margin * 2, half = w / 2;
            string fechaHora = $"{g.FechaEmision:dd/MM/yyyy} {g.FechaRegistro:HH:mm}";
            FilaDato(c, x, y, half, "FECHA Y HORA", fechaHora); FilaDato(c, x + half, y, half, "ALMACEN", g.NombreAlmacen); y -= h;
            FilaDato(c, x, y, half, "CLIENTE", g.ClienteMostrar); FilaDato(c, x + half, y, half, "DOCUMENTO", Valor(g.RucDestino)); y -= h;
            FilaDato(c, x, y, half, "N. OCI", Valor(g.NumeroOci)); FilaDato(c, x + half, y, half, "N. PROFORMA", Valor(g.NumeroProforma)); y -= h;
            FilaDato(c, x, y, w, "MOTIVO SALIDA", Valor(g.MotivoEmisionManual)); y -= h;
            FilaDato(c, x, y, half, "RESPONSABLE", g.UsuarioEmisor); FilaDato(c, x + half, y, half, "AUTORIZADO", g.UsuarioAutorizador); y -= h + 14;
        }

        private static void FilaDato(ProformaPdfExporter.PdfCanvas c, double x, double y, double width, string label, string value)
        {
            c.Rectangle(x, y - 19, width, 19);
            c.Text(label, x + 5, y - 13, 7, true);
            c.Text(Truncar(Limpiar(Valor(value)), 33), x + 78, y - 13, 8);
        }

        private static void DibujarCabeceraDetalle(ProformaPdfExporter.PdfCanvas c, ref double y)
        {
            double x = Margin, w = PageWidth - Margin * 2;
            c.Rectangle(x, y - 21, w, 21);
            c.Text("CODIGO", x + 5, y - 14, 7, true);
            c.Text("PRODUCTO", x + 70, y - 14, 7, true);
            c.Text("UNIDAD", x + 292, y - 14, 7, true);
            c.RightText("DESPACHADA", x + 414, y - 14, 7, true);
            c.RightText("PENDIENTE", x + 486, y - 14, 7, true);
            c.Text("OBS.", x + 492, y - 14, 7, true);
            y -= 21;
        }

        private static void DibujarDetalle(ProformaPdfExporter.PdfCanvas c, GuiaInternaDetalle d, ref double y)
        {
            const double h = 23; double x = Margin, w = PageWidth - Margin * 2;
            c.Rectangle(x, y - h, w, h);
            c.Text(Truncar(Limpiar(d.CodigoProducto), 12), x + 5, y - 15, 7.5);
            c.Text(Truncar(Limpiar(d.NombreProducto), 41), x + 70, y - 15, 7.5);
            c.Text(Truncar(Limpiar(d.NombreUnidad), 12), x + 292, y - 15, 7.5);
            c.RightText(d.CantidadDespachar.ToString("N2"), x + 414, y - 15, 7.5);
            c.RightText(d.CantidadPendiente.ToString("N2"), x + 486, y - 15, 7.5);
            c.Text(Truncar(Limpiar(d.Observacion), 14), x + 492, y - 15, 7);
            y -= h;
        }

        private static void DibujarCierre(ProformaPdfExporter.PdfCanvas c, GuiaInterna g, ref double y)
        {
            y -= 14; c.Text("OBSERVACIONES", Margin, y, 8, true); y -= 13;
            c.Rectangle(Margin, y - 34, PageWidth - Margin * 2, 38);
            c.Text(Truncar(Limpiar(Valor(g.Observacion)), 105), Margin + 7, y - 15, 8);
            y -= 82;
            double ancho = (PageWidth - Margin * 2) / 3;
            c.Line(Margin + 18, y, Margin + ancho - 18, y);
            c.Line(Margin + ancho + 18, y, Margin + ancho * 2 - 18, y);
            c.Line(Margin + ancho * 2 + 18, y, PageWidth - Margin - 18, y);
            c.CenterText("ENTREGADO POR", Margin + ancho / 2, y - 14, 8, true);
            c.CenterText("RECIBIDO POR", Margin + ancho * 1.5, y - 14, 8, true);
            c.CenterText("AUTORIZADO POR", Margin + ancho * 2.5, y - 14, 8, true);
        }

        private static byte[]? ObtenerLogo(Empresa empresa)
        {
            if (empresa.Logo is { Length: > 0 }) return empresa.Logo;
            try
            {
                StreamResourceInfo? resource = Application.GetResourceStream(new Uri("pack://application:,,,/Images/LOGO.png", UriKind.Absolute));
                if (resource?.Stream == null) return null;
                using MemoryStream stream = new(); resource.Stream.CopyTo(stream); return stream.ToArray();
            }
            catch { return null; }
        }

        private static string Valor(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value;
        private static string Unir(params string[] values) => string.Join(" - ", values.Where(v => !string.IsNullOrWhiteSpace(v)));
        private static string Truncar(string value, int max) => value.Length <= max ? value : value[..Math.Max(0, max - 3)] + "...";
        private static string Limpiar(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            Dictionary<char, char> reemplazos = new() { ['á']='a',['é']='e',['í']='i',['ó']='o',['ú']='u',['Á']='A',['É']='E',['Í']='I',['Ó']='O',['Ú']='U',['ñ']='n',['Ñ']='N' };
            return new string(value.Select(c => reemplazos.TryGetValue(c, out char r) ? r : c <= 127 ? c : ' ').ToArray());
        }
    }
}
