using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
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
        private const double Margin = 28;
        private const double ContentWidth = PageWidth - (Margin * 2);
        private const byte AzulOscuroR = 0;
        private const byte AzulOscuroG = 45;
        private const byte AzulOscuroB = 111;
        private const byte AzulMedioR = 7;
        private const byte AzulMedioG = 70;
        private const byte AzulMedioB = 155;
        private const byte AzulClaroR = 232;
        private const byte AzulClaroG = 242;
        private const byte AzulClaroB = 252;
        private const byte RojoR = 220;
        private const byte RojoG = 18;
        private const byte RojoB = 24;
        private const int CaracteresNombreProductoDetalle = 15;
        private const int CaracteresDescripcionDetalle = 39;
        private const double FotoGapX = 16;
        private const double FotoGapY = 20;
        private const double FooterImageHeight = 60;
        private const double FooterReserveY = FooterImageHeight + 16;
        private const double FotoContenidoBottom = FooterReserveY;
        private const double FotoMinCellH = 120;
        private const string FooterImageFile = "footer_30_anios.png";

        public static void Exportar(string ruta, Empresa empresa, OrdenServicio orden, bool incluirFotos)
        {
            ProformaPdfExporter.SimplePdfDocument document = new();
            ProformaPdfExporter.PdfCanvas canvas = document.AddPage(PageWidth, PageHeight);
            DibujarMarcasEstado(canvas, orden);
            double y = PageHeight - 28;
            int totalPaginas = CalcularTotalPaginas(orden, incluirFotos);

            DibujarCabecera(canvas, empresa, orden, 1, totalPaginas, ref y);
            DibujarFichaGeneral(canvas, orden, ref y);
            int paginaActual = 1;
            DibujarDetalleServicio(document, ref canvas, empresa, orden, totalPaginas, ref paginaActual, ref y);

            if (y - CalcularAltoResumenObservaciones(orden) < FooterReserveY)
            {
                DibujarPie(canvas, empresa);
                paginaActual++;
                canvas = NuevaPaginaResumen(document, orden, paginaActual, totalPaginas, out y);
            }

            DibujarResumenEconomico(canvas, orden, ref y);
            DibujarObservacionesProveedor(canvas, orden, ref y);

            if (paginaActual == 1 || y - CalcularAltoAprobacionFirmas() < FooterReserveY)
            {
                DibujarPie(canvas, empresa);
                paginaActual++;
                canvas = NuevaPaginaResumen(document, orden, paginaActual, totalPaginas, out y);
            }

            DibujarAprobacion(canvas, empresa, orden, ref y);
            DibujarFirmas(canvas, orden, ref y);
            DibujarPie(canvas, empresa);
            DibujarFotosReferencia(document, ref canvas, empresa, orden, incluirFotos, totalPaginas, ref paginaActual, y);

            document.Save(ruta);
        }

        private static void DibujarCabecera(ProformaPdfExporter.PdfCanvas c, Empresa empresa, OrdenServicio orden, int paginaActual, int totalPaginas, ref double y)
        {
            string nombreEmpresa = NombreEmpresa(empresa).ToUpperInvariant();
            double leftX = Margin;
            double dividerX = 270;
            double rightLeft = 300;
            double rightCenter = (rightLeft + PageWidth - Margin) / 2;
            byte[]? logo = ObtenerLogo(empresa);
            if (logo != null)
                c.Image(logo, leftX, y - 56, 170, 56);
            else
                c.Text("DELTA", leftX, y - 28, 28, true, AzulOscuroR, AzulOscuroG, AzulOscuroB);

            double infoY = y - 74;
            c.Text(Limpiar(nombreEmpresa), leftX, infoY, 10, true);
            infoY -= 13;
            if (!string.IsNullOrWhiteSpace(empresa.Ruc))
                c.Text($"RUC: {Limpiar(empresa.Ruc)}", leftX, infoY, 9);
            infoY -= 12;
            if (!string.IsNullOrWhiteSpace(empresa.Direccion))
            {
                DibujarIconoImagen(c, "ubicacion.png", leftX, infoY - 1, 9, 9);
                List<string> direccionLineas = DividirLineas(Limpiar(empresa.Direccion), 52).Take(2).ToList();
                for (int i = 0; i < direccionLineas.Count; i++)
                    c.Text(direccionLineas[i], leftX + 14, infoY - i * 9, 8);
                infoY -= Math.Max(0, direccionLineas.Count - 1) * 9;
            }
            infoY -= 10;
            string contacto = UnirPartes(empresa.Telefono, empresa.Correo);
            if (!string.IsNullOrWhiteSpace(contacto))
            {
                DibujarIconoImagen(c, "telefono.png", leftX, infoY - 1, 9, 9);
                c.Text(Limpiar(contacto), leftX + 14, infoY, 8);
            }

            c.Line(dividerX, y - 12, dividerX, y - 128, 120, 120, 120);
            c.CenterText("ORDEN DE SERVICIO", rightCenter, y - 24, 24, true, AzulOscuroR, AzulOscuroG, AzulOscuroB);
            c.FilledRoundedRectangle(rightCenter - 105, y - 64, 210, 28, 5, AzulMedioR, AzulMedioG, AzulMedioB);
            c.CenterText($"N. OS-{NumeroPdf(orden.NumeroOrden)}", rightCenter, y - 55, 17, true, 255, 255, 255);

            double fechaX = rightLeft + 10;
            double paginaX = rightLeft + 170;
            DibujarIconoImagen(c, "calendario.png", fechaX, y - 111, 22, 22);
            c.Text("Fecha de emision:", fechaX + 30, y - 94, 10, true);
            c.Text(FechaLargaTitulo(orden.Fecha), fechaX + 30, y - 110, 10);
            DibujarIconoImagen(c, "hoja.png", paginaX, y - 111, 22, 22);
            c.Text("Pagina:", paginaX + 30, y - 94, 10, true);
            c.Text($"{paginaActual} de {totalPaginas}", paginaX + 30, y - 110, 10);

            y -= 136;
            y -= 8;
        }

        private static void DibujarFichaGeneral(ProformaPdfExporter.PdfCanvas c, OrdenServicio orden, ref double y)
        {
            double boxH = 50;
            c.RoundedRectangle(Margin, y - boxH, ContentWidth, boxH, 7, 155, 183, 220);
            double leftX = Margin + 18;
            double rightX = Margin + ContentWidth - 150;
            double centerLineX = rightX - 36;

            double labelX = leftX + 8;
            double valueX = leftX + 104;
            c.Text("PROVEEDOR", labelX, y - 18, 9, true, AzulOscuroR, AzulOscuroG, AzulOscuroB);
            c.Text(Truncar(Limpiar(FormatoNombre(orden.NombreProveedor)), 42), valueX, y - 18, 11);

            c.Text("SERVICIO", labelX, y - 36, 9, true, AzulOscuroR, AzulOscuroG, AzulOscuroB);
            c.Text(Truncar(Limpiar(orden.TipoServicioNombre), 42), valueX, y - 36, 11);

            c.Line(centerLineX, y - 8, centerLineX, y - boxH + 8, 155, 183, 220);
            DibujarIconoImagen(c, "maletin.png", rightX - 1, y - 42, 31, 31);
            c.Text("CANTIDAD TOTAL", rightX + 42, y - 18, 9, true, AzulOscuroR, AzulOscuroG, AzulOscuroB);
            c.Text(orden.Detalles.Sum(d => d.Cantidad).ToString("N0"), rightX + 42, y - 36, 18, true, RojoR, RojoG, RojoB);
            c.Text("unidades", rightX + 74, y - 36, 8);

            y -= boxH + 10;
        }

        private static void DibujarDetalleServicio(ProformaPdfExporter.SimplePdfDocument document, ref ProformaPdfExporter.PdfCanvas c, Empresa empresa, OrdenServicio orden, int totalPaginas, ref int paginaActual, ref double y)
        {
            DibujarTituloSeccion(c, "DETALLE DEL SERVICIO", ref y);

            double x = Margin;
            double productoW = 100;
            double descW = 218;
            double cantW = 65;
            double precioW = 80;
            double totalW = ContentWidth - productoW - descW - cantW - precioW;
            double tableTop = y;
            DibujarCabeceraTablaDetalle(c, x, productoW, descW, cantW, precioW, totalW, ref y);

            foreach (OrdenServicioDetalle detalle in orden.Detalles)
            {
                List<string> productoLineas = PrepararNombreProductoDetalle(detalle);
                List<string> descripcionLineas = PrepararDescripcionDetalle(detalle, CaracteresDescripcionDetalle);
                double rowH = Math.Max(42, 18 + Math.Max(productoLineas.Count, descripcionLineas.Count) * 8.5);
                if (y - rowH < 228)
                {
                    c.RoundedRectangle(x, y, ContentWidth, tableTop - y, 5, 155, 183, 220);
                    DibujarPie(c, empresa);
                    paginaActual++;
                    c = NuevaPaginaDetalle(document, orden, paginaActual, totalPaginas, out y);
                    tableTop = y;
                    DibujarCabeceraTablaDetalle(c, x, productoW, descW, cantW, precioW, totalW, ref y);
                }

                c.Line(x, y - rowH, x + ContentWidth, y - rowH, 155, 183, 220);
                DibujarColumnasAzules(c, x, y - rowH, productoW, descW, cantW, precioW, rowH);
                for (int i = 0; i < productoLineas.Count; i++)
                    c.Text(productoLineas[i], x + 10, y - 15 - i * 8.5, 7.5, i == 0);
                for (int i = 0; i < descripcionLineas.Count; i++)
                    c.Text(descripcionLineas[i], x + productoW + 10, y - 15 - i * 8.5, 7);
                c.CenterText(detalle.Cantidad.ToString("N0"), x + productoW + descW + cantW / 2, y - rowH / 2 - 3, 9);
                c.CenterText(Moneda(detalle.PrecioUnitario, espacio: true), x + productoW + descW + cantW + precioW / 2, y - rowH / 2 - 3, 9);
                c.CenterText(Moneda(detalle.Total, espacio: true), x + productoW + descW + cantW + precioW + totalW / 2, y - rowH / 2 - 3, 9);
                y -= rowH;
            }

            double totalH = 24;
            c.FilledRectangle(x, y - totalH, ContentWidth, totalH, AzulClaroR, AzulClaroG, AzulClaroB);
            c.Line(x + productoW + descW + cantW + precioW, y, x + productoW + descW + cantW + precioW, y - totalH, 155, 183, 220);
            c.RightText("Total del servicio:", x + productoW + descW + cantW + precioW - 18, y - 16, 10, true);
            c.CenterText(Moneda(orden.Total, espacio: true), x + productoW + descW + cantW + precioW + totalW / 2, y - 16, 11, true, AzulOscuroR, AzulOscuroG, AzulOscuroB);
            c.RoundedRectangle(x, y - totalH, ContentWidth, tableTop - (y - totalH), 5, 155, 183, 220);
            y -= totalH + 10;

        }

        private static void DibujarCabeceraTablaDetalle(ProformaPdfExporter.PdfCanvas c, double x, double productoW, double descW, double cantW, double precioW, double totalW, ref double y)
        {
            double headerH = 20;
            c.FilledRoundedRectangle(x, y - headerH, ContentWidth, headerH, 5, AzulMedioR, AzulMedioG, AzulMedioB);
            c.CenterText("NOMBRE PRODUCTO", x + productoW / 2, y - 14, 6.5, true, 255, 255, 255);
            c.CenterText("DESCRIPCION", x + productoW + descW / 2, y - 14, 8, true, 255, 255, 255);
            c.CenterText("CANTIDAD", x + productoW + descW + cantW / 2, y - 14, 8, true, 255, 255, 255);
            c.CenterText("P. UNITARIO", x + productoW + descW + cantW + precioW / 2, y - 14, 8, true, 255, 255, 255);
            c.CenterText("IMPORTE", x + productoW + descW + cantW + precioW + totalW / 2, y - 14, 8, true, 255, 255, 255);
            DibujarColumnasAzules(c, x, y - headerH, productoW, descW, cantW, precioW, headerH);
            y -= headerH;
        }

        private static void DibujarColumnasAzules(ProformaPdfExporter.PdfCanvas c, double x, double y, double productoW, double descW, double cantW, double precioW, double h)
        {
            c.Line(x + productoW, y + h, x + productoW, y, 155, 183, 220);
            c.Line(x + productoW + descW, y + h, x + productoW + descW, y, 155, 183, 220);
            c.Line(x + productoW + descW + cantW, y + h, x + productoW + descW + cantW, y, 155, 183, 220);
            c.Line(x + productoW + descW + cantW + precioW, y + h, x + productoW + descW + cantW + precioW, y, 155, 183, 220);
        }

        private static void DibujarResumenEconomico(ProformaPdfExporter.PdfCanvas c, OrdenServicio orden, ref double y)
        {
            double gap = 12;
            double cardW = (ContentWidth - gap) / 2;
            double cardH = 98;
            double leftX = Margin;
            double rightX = Margin + cardW + gap;

            c.RoundedRectangle(leftX, y - cardH, cardW, cardH, 7, 155, 183, 220);
            DibujarIconoImagen(c, "detalle.png", leftX + 16, y - 36, 26, 26);
            c.Text("RESUMEN ECONOMICO", leftX + 44, y - 25, 10, true, AzulOscuroR, AzulOscuroG, AzulOscuroB);
            c.Text("A CUENTA", leftX + 18, y - 48, 8);
            c.RightText(Moneda(orden.TotalPagado, espacio: true), leftX + cardW - 18, y - 48, 8);
            c.Text("SALDO PENDIENTE", leftX + 18, y - 66, 8, true, RojoR, RojoG, RojoB);
            c.RightText(Moneda(orden.SaldoPendiente, espacio: true), leftX + cardW - 18, y - 66, 8, true, RojoR, RojoG, RojoB);
            c.Line(leftX + 18, y - 76, leftX + cardW - 18, y - 76, AzulOscuroR, AzulOscuroG, AzulOscuroB);
            c.Text("TOTAL DEL SERVICIO", leftX + 18, y - 92, 9, true, AzulOscuroR, AzulOscuroG, AzulOscuroB);
            c.RightText(Moneda(orden.Total, espacio: true), leftX + cardW - 18, y - 92, 11, true);

            c.RoundedRectangle(rightX, y - cardH, cardW, cardH, 7, 155, 183, 220);
            DibujarIconoImagen(c, "detalle.png", rightX + 16, y - 36, 26, 26);
            c.Text("CONDICION DE PAGO", rightX + 44, y - 25, 10, true, AzulOscuroR, AzulOscuroG, AzulOscuroB);
            c.Text($"Condicion: {Limpiar(TextoCondicionPago(orden))}", rightX + 18, y - 49, 8, true);
            List<string> pagoLineas = DividirLineas(TextoFormaPago(orden), 44);
            for (int i = 0; i < pagoLineas.Count && i < 3; i++)
                c.Text(pagoLineas[i], rightX + 18, y - 69 - i * 11, 8);

            y -= cardH + 14;
        }

        private static void DibujarObservacionesProveedor(ProformaPdfExporter.PdfCanvas c, OrdenServicio orden, ref double y)
        {
            List<string> lineas = PrepararLineasObservacionesProveedor(orden.Observaciones).Take(5).ToList();
            double cardH = Math.Max(56, 34 + lineas.Count * 11);
            c.RoundedRectangle(Margin, y - cardH, ContentWidth, cardH, 7, 155, 183, 220);
            DibujarIconoImagen(c, "detalle.png", Margin + 16, y - 36, 26, 26);
            c.Text("OBSERVACIONES", Margin + 44, y - 23, 10, true, AzulOscuroR, AzulOscuroG, AzulOscuroB);

            for (int i = 0; i < lineas.Count; i++)
                c.Text(lineas[i], Margin + 44, y - 43 - i * 11, 8);

            y -= cardH + 16;
        }

        private static void DibujarFirmas(ProformaPdfExporter.PdfCanvas c, OrdenServicio orden, ref double y)
        {
            double centerX = PageWidth / 2;
            double lineY = y - 26;
            double lineStart = centerX - 150;
            double lineEnd = centerX + 150;

            DibujarIconoImagen(c, "conformidad.png", centerX - 18, y - 24, 36, 36);
            DibujarLineaPunteada(c, lineStart, lineEnd, lineY);
            c.CenterText("CONFORMIDAD DEL PROVEEDOR", centerX, y - 48, 10, true, AzulOscuroR, AzulOscuroG, AzulOscuroB);
            c.CenterText(Limpiar(FormatoNombre(orden.NombreProveedor)), centerX, y - 63, 9);
            c.CenterText("Fecha: _____/_____/________", centerX, y - 78, 9);

            y -= 82;
        }

        private static void DibujarAprobacion(ProformaPdfExporter.PdfCanvas c, Empresa empresa, OrdenServicio orden, ref double y)
        {
            double cardH = 52;
            c.RoundedRectangle(Margin, y - cardH, ContentWidth, cardH, 7, 155, 183, 220);
            double separatorX = Margin + ContentWidth * 0.56;
            c.Line(separatorX, y - 8, separatorX, y - cardH + 8, 155, 183, 220);

            c.Text("APROBADO POR:", Margin + 18, y - 18, 10, true, AzulOscuroR, AzulOscuroG, AzulOscuroB);

            string aprobador = ObtenerUsuarioAprobador(orden);
            if (string.IsNullOrWhiteSpace(aprobador))
            {
                c.Text("SIN AUTORIZACION", Margin + 112, y - 18, 11, true, RojoR, RojoG, RojoB);
                c.Text("Documento impreso sin aprobacion registrada.", Margin + 18, y - 36, 8, false, RojoR, RojoG, RojoB);
            }
            else
            {
                c.Text(Limpiar(aprobador), Margin + 112, y - 18, 11, true, AzulOscuroR, AzulOscuroG, AzulOscuroB);
                OrdenServicioHistorial? aprobacion = ObtenerHistorialAprobacion(orden);
                if (aprobacion != null)
                    c.Text($"Fecha autorizacion: {aprobacion.FechaHora:dd/MM/yyyy HH:mm}", Margin + 18, y - 36, 8);
            }

            double rightX = separatorX + 18;
            c.Text("ELABORADO POR", rightX, y - 18, 10, true, AzulOscuroR, AzulOscuroG, AzulOscuroB);
            c.Text(Truncar(Limpiar(NombreEmpresa(empresa)).ToUpperInvariant(), 26), rightX, y - 34, 9);
            c.Text($"Fecha: {orden.Fecha:dd/MM/yyyy}", rightX, y - 47, 8);

            y -= cardH + 14;
        }

        private static void DibujarLineaPunteada(ProformaPdfExporter.PdfCanvas c, double x1, double x2, double y)
        {
            const double dash = 2.4;
            const double gap = 2.2;
            for (double x = x1; x < x2; x += dash + gap)
                c.Line(x, y, Math.Min(x + dash, x2), y, 120, 120, 120);
        }

        private static void DibujarPie(ProformaPdfExporter.PdfCanvas c, Empresa empresa)
        {
            byte[]? footer = ObtenerRecurso($"Images/PdfOs/{FooterImageFile}");
            if (footer != null && c.Image(footer, 0, 0, PageWidth, FooterImageHeight))
                return;

            c.Line(0, FooterImageHeight + 2, PageWidth, FooterImageHeight + 2, RojoR, RojoG, RojoB);
            c.FilledRectangle(0, 0, PageWidth, FooterImageHeight, AzulMedioR, AzulMedioG, AzulMedioB);
            c.Text("Gracias por su preferencia", Margin + 20, 24, 9, false, 255, 255, 255);
            string derecha = WebEmpresa(empresa);
            c.RightText(Limpiar(derecha), PageWidth - Margin - 20, 24, 9, false, 255, 255, 255);
        }

        private static void DibujarTituloSeccion(ProformaPdfExporter.PdfCanvas c, string titulo, ref double y)
        {
            DibujarIconoImagen(c, "detalle.png", Margin, y - 24, 26, 26);
            c.Text(titulo, Margin + 34, y - 15, 12, true, AzulOscuroR, AzulOscuroG, AzulOscuroB);
            y -= 32;
        }

        private static void DibujarFotosReferencia(ProformaPdfExporter.SimplePdfDocument document, ref ProformaPdfExporter.PdfCanvas c, Empresa empresa, OrdenServicio orden, bool incluirFotos, int totalPaginas, ref int paginaActual, double yDisponible)
        {
            if (!incluirFotos)
                return;

            List<OrdenServicioFoto> fotos = ObtenerFotosOrdenadas(orden);
            if (fotos.Count == 0)
                return;

            (int columnas, int filas) = ObtenerDistribucionFotos(orden.DistribucionFotosPdf);
            int indiceFoto = 0;

            if (paginaActual >= 2)
            {
                int capacidadActual = CalcularCapacidadFotosEnEspacio(yDisponible, columnas, filas);
                if (capacidadActual > 0)
                {
                    double headerYActual = yDisponible - 12;
                    double contenidoTopActual = headerYActual - 28;
                    int fotosActuales = Math.Min(capacidadActual, fotos.Count);
                    int filasActuales = 1;

                    c.Text("FOTOS / REFERENCIAS", Margin, headerYActual, 11, true, AzulOscuroR, AzulOscuroG, AzulOscuroB);
                    c.Line(Margin, headerYActual - 6, PageWidth - Margin, headerYActual - 6, 155, 183, 220);
                    DibujarFotosEnGrilla(c, fotos, indiceFoto, fotosActuales, columnas, filasActuales, contenidoTopActual, FotoContenidoBottom);
                    indiceFoto += fotosActuales;
                }
            }

            while (indiceFoto < fotos.Count)
            {
                paginaActual++;
                double headerY = PageHeight - 60;
                c = NuevaPaginaFotos(document, orden, headerY, paginaActual, totalPaginas);
                int fotosPagina = Math.Min(columnas * filas, fotos.Count - indiceFoto);
                DibujarFotosEnGrilla(c, fotos, indiceFoto, fotosPagina, columnas, filas, headerY - 28, FotoContenidoBottom);
                DibujarPie(c, empresa);
                indiceFoto += fotosPagina;
            }
        }

        private static void DibujarFotosEnGrilla(ProformaPdfExporter.PdfCanvas c, List<OrdenServicioFoto> fotos, int indiceInicio, int cantidad, int columnas, int filas, double contenidoTop, double contenidoBottom)
        {
            if (cantidad <= 0 || filas <= 0)
                return;

            double celdaW = (ContentWidth - (columnas - 1) * FotoGapX) / columnas;
            double celdaH = (contenidoTop - contenidoBottom - (filas - 1) * FotoGapY) / filas;
            for (int i = 0; i < cantidad; i++)
            {
                int indicePagina = i;
                int fila = indicePagina / columnas;
                int columna = indicePagina % columnas;
                double x = Margin + columna * (celdaW + FotoGapX);
                double top = contenidoTop - fila * (celdaH + FotoGapY);
                DibujarFotoEnCelda(c, fotos[indiceInicio + i], x, top, celdaW, celdaH);
            }
        }

        private static ProformaPdfExporter.PdfCanvas NuevaPaginaFotos(ProformaPdfExporter.SimplePdfDocument document, OrdenServicio orden, double headerY, int paginaActual, int totalPaginas)
        {
            ProformaPdfExporter.PdfCanvas c = document.AddPage(PageWidth, PageHeight);
            DibujarMarcasEstado(c, orden);
            c.Text("FOTOS / REFERENCIAS", Margin, headerY, 11, true);
            c.RightText($"Pagina: {paginaActual} de {totalPaginas}", PageWidth - Margin, headerY, 10, true);
            c.Line(Margin, headerY - 4, PageWidth - Margin, headerY - 4);
            return c;
        }

        private static ProformaPdfExporter.PdfCanvas NuevaPaginaDetalle(ProformaPdfExporter.SimplePdfDocument document, OrdenServicio orden, int paginaActual, int totalPaginas, out double y)
        {
            ProformaPdfExporter.PdfCanvas c = document.AddPage(PageWidth, PageHeight);
            DibujarMarcasEstado(c, orden);
            double headerY = PageHeight - 48;
            c.Text("DETALLE DEL SERVICIO (CONT.)", Margin, headerY, 11, true, AzulOscuroR, AzulOscuroG, AzulOscuroB);
            c.RightText($"Pagina: {paginaActual} de {totalPaginas}", PageWidth - Margin, headerY, 10, true);
            c.Line(Margin, headerY - 6, PageWidth - Margin, headerY - 6, 155, 183, 220);
            y = headerY - 28;
            return c;
        }

        private static ProformaPdfExporter.PdfCanvas NuevaPaginaResumen(ProformaPdfExporter.SimplePdfDocument document, OrdenServicio orden, int paginaActual, int totalPaginas, out double y)
        {
            ProformaPdfExporter.PdfCanvas c = document.AddPage(PageWidth, PageHeight);
            DibujarMarcasEstado(c, orden);
            double headerY = PageHeight - 48;
            c.Text("RESUMEN Y CONFORMIDAD", Margin, headerY, 11, true, AzulOscuroR, AzulOscuroG, AzulOscuroB);
            c.RightText($"Pagina: {paginaActual} de {totalPaginas}", PageWidth - Margin, headerY, 10, true);
            c.Line(Margin, headerY - 6, PageWidth - Margin, headerY - 6, 155, 183, 220);
            y = headerY - 30;
            return c;
        }

        private static int CalcularTotalPaginas(OrdenServicio orden, bool incluirFotos)
        {
            (int paginasContenido, double yFinalContenido) = CalcularPaginasDetalleYResumen(orden);

            int cantidadFotos = ObtenerFotosOrdenadas(orden).Count;
            if (!incluirFotos || cantidadFotos == 0)
                return paginasContenido;

            (int columnas, int filas) = ObtenerDistribucionFotos(orden.DistribucionFotosPdf);
            int fotosPorPagina = Math.Max(1, columnas * filas);
            int fotosEnPaginaContenido = paginasContenido >= 2
                ? CalcularCapacidadFotosEnEspacio(yFinalContenido, columnas, filas)
                : 0;
            int fotosRestantes = Math.Max(0, cantidadFotos - fotosEnPaginaContenido);
            return paginasContenido + (int)Math.Ceiling(fotosRestantes / (double)fotosPorPagina);
        }

        private static (int Paginas, double YFinal) CalcularPaginasDetalleYResumen(OrdenServicio orden)
        {
            int paginas = 1;
            double y = PageHeight - 28;
            y -= 136;
            y -= 8;
            y -= 50 + 10;
            y -= 32;
            y -= 20;

            foreach (OrdenServicioDetalle detalle in orden.Detalles)
            {
                int lineasProducto = PrepararNombreProductoDetalle(detalle).Count;
                int lineasDescripcion = PrepararDescripcionDetalle(detalle, CaracteresDescripcionDetalle).Count;
                double rowH = Math.Max(42, 18 + Math.Max(lineasProducto, lineasDescripcion) * 8.5);
                if (y - rowH < 228)
                {
                    paginas++;
                    y = PageHeight - 48 - 28 - 20;
                }

                y -= rowH;
            }

            y -= 24 + 10;
            if (y - CalcularAltoResumenObservaciones(orden) < FooterReserveY)
            {
                paginas++;
                y = PageHeight - 48 - 30;
            }

            y -= CalcularAltoResumenObservaciones(orden);
            if (paginas == 1 || y - CalcularAltoAprobacionFirmas() < FooterReserveY)
            {
                paginas++;
                y = PageHeight - 48 - 30;
            }

            y -= CalcularAltoAprobacionFirmas();

            return (paginas, y);
        }

        private static int CalcularCapacidadFotosEnEspacio(double yDisponible, int columnas, int filas)
        {
            double headerY = yDisponible - 12;
            double contenidoTop = headerY - 28;
            double altoDisponible = contenidoTop - FotoContenidoBottom;
            if (altoDisponible < FotoMinCellH)
                return 0;

            int filasDisponibles = altoDisponible >= FotoMinCellH ? 1 : 0;
            filasDisponibles = Math.Clamp(filasDisponibles, 0, Math.Min(1, filas));
            return columnas * filasDisponibles;
        }

        private static double CalcularAltoResumenObservaciones(OrdenServicio orden)
        {
            int lineasObservaciones = PrepararLineasObservacionesProveedor(orden.Observaciones).Take(5).Count();
            double altoObservaciones = Math.Max(56, 34 + lineasObservaciones * 11);
            return 98 + 14 + altoObservaciones + 16;
        }

        private static double CalcularAltoAprobacionFirmas() => 52 + 14 + 82;

        private static List<OrdenServicioFoto> ObtenerFotosOrdenadas(OrdenServicio orden) =>
            orden.Fotos
                .OrderBy(f => f.Orden <= 0 ? int.MaxValue : f.Orden)
                .ThenBy(f => f.IdOrdenServicioFoto)
                .ToList();

        private static void DibujarFotoEnCelda(ProformaPdfExporter.PdfCanvas c, OrdenServicioFoto foto, double x, double top, double width, double height)
        {
            string titulo = string.IsNullOrWhiteSpace(foto.Titulo) ? "REFERENCIA" : Limpiar(foto.Titulo).ToUpperInvariant();
            c.Text(titulo, x, top, 9, true);

            double imageTop = top - 16;
            double imageH = height - 18;
            if (imageH < 40)
                imageH = 40;

            bool dibujo = foto.Imagen is { Length: > 0 }
                ? c.CenteredImage(foto.Imagen, x, imageTop - imageH, width, imageH)
                : c.CenteredImage(foto.RutaArchivo, x, imageTop - imageH, width, imageH);
            if (!dibujo)
                c.Text($"Archivo: {Limpiar(foto.NombreArchivo)}", x, imageTop - 12, 8);
        }

        private static (int Columnas, int Filas) ObtenerDistribucionFotos(string distribucion)
        {
            string[] partes = (distribucion ?? string.Empty)
                .Replace("*", "x", StringComparison.OrdinalIgnoreCase)
                .Split('x', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (partes.Length == 2
                && int.TryParse(partes[0], out int columnas)
                && int.TryParse(partes[1], out int filas)
                && columnas > 0
                && filas > 0)
            {
                return (Math.Min(columnas, 3), Math.Min(filas, 4));
            }

            return (1, 2);
        }

        private static List<string> PrepararLineasObservacionesProveedor(string observaciones)
        {
            observaciones = Limpiar(observaciones);
            if (string.IsNullOrWhiteSpace(observaciones))
                return ["Sin observaciones."];

            List<string> items = observaciones.Contains(',')
                ? observaciones.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
                : [observaciones.Trim()];

            List<string> lineas = [];
            foreach (string item in items.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                List<string> partes = DividirLineas(item.Trim(), 82);
                if (partes.Count == 0)
                    continue;

                lineas.Add($"- {partes[0]}");
                for (int i = 1; i < partes.Count; i++)
                    lineas.Add($"  {partes[i]}");
            }

            return lineas.Count == 0 ? ["Sin observaciones."] : lineas;
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

        private static void DibujarMarcasEstado(ProformaPdfExporter.PdfCanvas c, OrdenServicio orden)
        {
            if (!TieneAprobacion(orden))
                c.RotatedCenterText("SIN APROBACION", PageWidth / 2, PageHeight / 2, 62, 35, true, 225, 225, 225);

            if (orden.EstaAnulada)
                c.RotatedCenterText("ANULADA", PageWidth / 2, PageHeight / 2 - 70, 78, 35, true, 220, 220, 220);
        }

        private static bool TieneAprobacion(OrdenServicio orden) => ObtenerHistorialAprobacion(orden) != null;

        private static string ObtenerUsuarioAprobador(OrdenServicio orden)
        {
            string aprobador = ObtenerHistorialAprobacion(orden)?.Usuario?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(aprobador))
                return string.Empty;

            return ResolverNombreAprobador(aprobador);
        }

        private static string ResolverNombreAprobador(string aprobador)
        {
            try
            {
                Usuario? usuario = new UsuarioNegocio()
                    .Listar()
                    .FirstOrDefault(u =>
                        u.NombreUsuario.Equals(aprobador, StringComparison.OrdinalIgnoreCase)
                        || u.NombreCompleto.Equals(aprobador, StringComparison.OrdinalIgnoreCase)
                        || u.NombreEmpleado.Equals(aprobador, StringComparison.OrdinalIgnoreCase));

                if (usuario != null)
                {
                    if (!string.IsNullOrWhiteSpace(usuario.NombreCompleto))
                        return usuario.NombreCompleto.Trim();

                    if (!string.IsNullOrWhiteSpace(usuario.NombreEmpleado))
                        return usuario.NombreEmpleado.Trim();
                }
            }
            catch
            {
                return aprobador;
            }

            return aprobador;
        }

        private static OrdenServicioHistorial? ObtenerHistorialAprobacion(OrdenServicio orden) =>
            orden.Historial
                .Where(h => h.Accion.Equals("Aprobacion", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(h => h.FechaHora)
                .ThenByDescending(h => h.IdOrdenServicioHistorial)
                .FirstOrDefault();

        private static string FechaLarga(DateTime fecha)
        {
            string[] meses = ["ENERO", "FEBRERO", "MARZO", "ABRIL", "MAYO", "JUNIO", "JULIO", "AGOSTO", "SETIEMBRE", "OCTUBRE", "NOVIEMBRE", "DICIEMBRE"];
            return $"{fecha:dd} de {meses[fecha.Month - 1]} del {fecha:yyyy}";
        }

        private static string FechaLargaTitulo(DateTime fecha)
        {
            string[] meses = ["Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", "Julio", "Agosto", "Setiembre", "Octubre", "Noviembre", "Diciembre"];
            return $"{fecha:dd} de {meses[fecha.Month - 1]} del {fecha:yyyy}";
        }

        private static string Moneda(decimal value, bool espacio) => espacio ? $"S/ {value:N2}" : $"S/{value:N2}";

        private static string NombreEmpresa(Empresa empresa)
        {
            if (!string.IsNullOrWhiteSpace(empresa.NombreComercial)) return empresa.NombreComercial;
            if (!string.IsNullOrWhiteSpace(empresa.Nombre)) return empresa.Nombre;
            return "Delta Confecciones S.R.L.";
        }

        private static string NumeroPdf(string numeroOrden)
        {
            string limpio = Limpiar(numeroOrden).Trim().Replace('_', '-');
            if (limpio.StartsWith("OS-", StringComparison.OrdinalIgnoreCase))
                limpio = limpio[3..];

            string[] partes = limpio.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (partes.Length == 2 && int.TryParse(partes[1], out int correlativo))
                return $"{partes[0]}-{correlativo:000}";

            return limpio;
        }

        private static string TextoCondicionPago(OrdenServicio orden)
        {
            if (orden.FormaPago.StartsWith("Credito", StringComparison.OrdinalIgnoreCase))
                return "Credito";
            if (orden.FormaPago.StartsWith("Contado", StringComparison.OrdinalIgnoreCase))
                return "Contado";
            return string.IsNullOrWhiteSpace(orden.FormaPago) ? "Contado" : orden.FormaPago;
        }

        private static string TextoFormaPago(OrdenServicio orden)
        {
            if (orden.TotalPagado <= 0)
                return "Forma de pago: Sin adelanto registrado.";

            if (orden.SaldoPendiente <= 0)
                return $"Forma de pago: {Moneda(orden.TotalPagado, espacio: true)} cancelado.";

            return $"Forma de pago: {Moneda(orden.TotalPagado, espacio: true)} a cuenta y saldo contra entrega.";
        }

        private static string WebEmpresa(Empresa empresa)
        {
            if (!string.IsNullOrWhiteSpace(empresa.Correo) && empresa.Correo.Contains('@'))
            {
                string dominio = empresa.Correo.Split('@').Last().Trim();
                if (!string.IsNullOrWhiteSpace(dominio))
                    return $"www.{dominio}";
            }

            return "www.deltaconfecciones.com.pe";
        }

        private static string UnirPartes(params string[] partes) =>
            string.Join(" - ", partes.Where(p => !string.IsNullOrWhiteSpace(p)));

        private static string Truncar(string value, int maximo)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length <= maximo) return value;
            return value[..Math.Max(0, maximo - 3)] + "...";
        }

        private static bool DibujarIconoImagen(ProformaPdfExporter.PdfCanvas c, string archivo, double x, double y, double width, double height)
        {
            byte[]? bytes = ObtenerRecurso($"Images/PdfOs/{archivo}");
            return bytes != null && c.Image(bytes, x, y, width, height);
        }

        private static byte[]? ObtenerRecurso(string rutaRelativa)
        {
            try
            {
                StreamResourceInfo? resource = Application.GetResourceStream(new Uri($"pack://application:,,,/{rutaRelativa}", UriKind.Absolute));
                if (resource?.Stream == null) return null;
                using MemoryStream stream = new();
                resource.Stream.CopyTo(stream);
                return stream.ToArray();
            }
            catch
            {
                return null;
            }
        }

        private static void DibujarIconoCalendario(ProformaPdfExporter.PdfCanvas c, double x, double y, double size, byte r, byte g, byte b)
        {
            c.Rectangle(x, y - size, size, size, r, g, b);
            c.Line(x, y - 5, x + size, y - 5, r, g, b);
            c.Line(x + 4, y + 3, x + 4, y - 3, r, g, b);
            c.Line(x + size - 4, y + 3, x + size - 4, y - 3, r, g, b);
            for (int row = 0; row < 2; row++)
                for (int col = 0; col < 3; col++)
                    c.FilledRectangle(x + 4 + col * 5, y - 10 - row * 5, 2, 2, r, g, b);
        }

        private static void DibujarIconoHoja(ProformaPdfExporter.PdfCanvas c, double x, double y, double size, byte r, byte g, byte b)
        {
            c.Rectangle(x + 3, y - size, size - 5, size, r, g, b);
            c.Line(x + size - 5, y, x + size - 1, y - 4, r, g, b);
            c.Line(x + size - 1, y - 4, x + size - 5, y - 4, r, g, b);
            c.Line(x + 7, y - 8, x + size - 4, y - 8, r, g, b);
            c.Line(x + 7, y - 13, x + size - 4, y - 13, r, g, b);
        }

        private static void DibujarIconoUbicacion(ProformaPdfExporter.PdfCanvas c, double x, double y, double size, byte r, byte g, byte b)
        {
            c.FilledCircle(x, y, size * 0.55, r, g, b);
            c.Line(x, y - size * 1.6, x - size * 0.6, y - size * 0.1, r, g, b);
            c.Line(x, y - size * 1.6, x + size * 0.6, y - size * 0.1, r, g, b);
        }

        private static void DibujarIconoTelefono(ProformaPdfExporter.PdfCanvas c, double x, double y, double size, byte r, byte g, byte b)
        {
            c.Line(x - size * 0.8, y + size * 0.7, x - size * 0.2, y + size * 0.1, r, g, b);
            c.Line(x - size * 0.2, y + size * 0.1, x + size * 0.6, y - size * 0.7, r, g, b);
            c.Line(x - size * 0.8, y + size * 0.7, x - size * 0.4, y + size, r, g, b);
            c.Line(x + size * 0.6, y - size * 0.7, x + size, y - size * 0.3, r, g, b);
        }

        private static void DibujarIconoPersona(ProformaPdfExporter.PdfCanvas c, double x, double y, double size, byte r, byte g, byte b)
        {
            c.FilledCircle(x, y + size * 0.25, size * 0.28, r, g, b);
            c.FilledRectangle(x - size * 0.45, y - size * 0.65, size * 0.9, size * 0.45, r, g, b);
        }

        private static void DibujarIconoNota(ProformaPdfExporter.PdfCanvas c, double x, double y, double size, byte r, byte g, byte b)
        {
            c.Rectangle(x - size * 0.45, y - size * 0.6, size * 0.9, size * 1.2, r, g, b);
            c.Line(x - size * 0.2, y + size * 0.8, x - size * 0.2, y + size * 0.45, r, g, b);
            c.Line(x + size * 0.2, y + size * 0.8, x + size * 0.2, y + size * 0.45, r, g, b);
            c.Line(x - size * 0.22, y + size * 0.1, x + size * 0.22, y + size * 0.1, r, g, b);
            c.Line(x - size * 0.22, y - size * 0.2, x + size * 0.22, y - size * 0.2, r, g, b);
        }

        private static void DibujarIconoMaletin(ProformaPdfExporter.PdfCanvas c, double x, double y, double size, byte r, byte g, byte b)
        {
            c.Rectangle(x - size * 0.55, y - size * 0.35, size * 1.1, size * 0.75, r, g, b);
            c.Rectangle(x - size * 0.25, y + size * 0.4, size * 0.5, size * 0.22, r, g, b);
            c.Line(x - size * 0.55, y + size * 0.1, x + size * 0.55, y + size * 0.1, r, g, b);
        }

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

        private static List<string> PrepararNombreProductoDetalle(OrdenServicioDetalle detalle)
        {
            string producto = Limpiar(detalle.Producto);
            if (string.IsNullOrWhiteSpace(producto))
                return ["-"];

            return DividirLineas(producto.ToUpperInvariant(), CaracteresNombreProductoDetalle);
        }

        private static List<string> PrepararDescripcionDetalle(OrdenServicioDetalle detalle, int maximo = 34)
        {
            string descripcion = Limpiar(detalle.Descripcion);
            if (string.IsNullOrWhiteSpace(descripcion) || descripcion.Equals(detalle.Producto, StringComparison.OrdinalIgnoreCase))
                return ["-"];

            List<string> lineas = [];
            foreach (string parte in descripcion.Split(['|', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                foreach (string linea in DividirLineas(parte, maximo))
                    lineas.Add(linea);
            }
            return lineas.Count == 0 ? ["-"] : lineas;
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
