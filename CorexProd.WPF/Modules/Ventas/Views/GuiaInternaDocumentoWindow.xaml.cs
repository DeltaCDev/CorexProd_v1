using CorexProd.Entidad.Entidades;
using CorexProd.WPF.Helpers;
using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Printing;

namespace CorexProd.WPF.Modules.Ventas.Views
{
    public partial class GuiaInternaDocumentoWindow : Window
    {
        private const double AnchoA4 = 793.7007874015749;
        private const double AltoA4 = 1122.51968503937;
        private readonly GuiaInterna _guia;
        private readonly Empresa _empresa;
        private readonly FlowDocument _documento;

        public GuiaInternaDocumentoWindow(GuiaInterna guia, Empresa empresa)
        {
            InitializeComponent();
            _guia = guia;
            _empresa = empresa;
            _documento = CrearDocumento();
            Visor.Document = _documento;
        }

        private FlowDocument CrearDocumento(GuiaInternaImpresion? impresion = null)
        {
            FlowDocument doc = new()
            {
                PageWidth = AnchoA4,
                PageHeight = AltoA4,
                PagePadding = new Thickness(46),
                ColumnWidth = AnchoA4 - 92,
                ColumnGap = 0,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 11
            };
            Table cabecera = new() { CellSpacing = 0 };
            cabecera.Columns.Add(new TableColumn { Width = new GridLength(430) });
            cabecera.Columns.Add(new TableColumn { Width = new GridLength(250) });
            TableRow fila = new();
            StackPanel empresaPanel = new() { Orientation = Orientation.Horizontal };
            Image? logo = CrearLogo();
            if (logo != null) empresaPanel.Children.Add(logo);
            StackPanel datos = new() { Margin = new Thickness(12, 0, 0, 0) };
            datos.Children.Add(Texto(NombreEmpresa(), 16, FontWeights.Bold));
            datos.Children.Add(Texto($"RUC: {_empresa.Ruc}"));
            datos.Children.Add(Texto(_empresa.Direccion));
            datos.Children.Add(Texto(string.Join(" - ", new[] { _empresa.Telefono, _empresa.Correo }), 10));
            empresaPanel.Children.Add(datos);
            fila.Cells.Add(Celda(new BlockUIContainer(empresaPanel), new Thickness(0), Brushes.Transparent));
            StackPanel numero = new();
            numero.Children.Add(Texto("GUÍA INTERNA DE SALIDA", 15, FontWeights.Bold, TextAlignment.Center));
            numero.Children.Add(Texto(_guia.NumeroGuia, 14, FontWeights.Bold, TextAlignment.Center));
            fila.Cells.Add(Celda(new BlockUIContainer(numero), new Thickness(12), Brushes.White));
            cabecera.RowGroups.Add(new TableRowGroup()); cabecera.RowGroups[0].Rows.Add(fila); doc.Blocks.Add(cabecera);

            if (_guia.EsAnulada)
                doc.Blocks.Add(new Paragraph(new Run("GUÍA ANULADA")) { Foreground = Brushes.DarkRed, FontSize = 28, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 14, 0, 8) });

            Table info = NuevaTabla(4, 170);
            AgregarFila(info, "Fecha y hora", FechaHora(), "Almacén", _guia.NombreAlmacen);
            AgregarFila(info, "Cliente / destinatario", _guia.ClienteMostrar, "RUC / documento", Valor(_guia.RucDestino));
            AgregarFila(info, "Número de OCI", Valor(_guia.NumeroOci), "Orden compra cliente", Valor(_guia.OrdenCompraCliente));
            AgregarFila(info, "Número de proforma", Valor(_guia.NumeroProforma), "Origen", Valor(_guia.Origen));
            AgregarFila(info, "Motivo de salida", Valor(_guia.MotivoEmisionManual), "Destino", _guia.ClienteMostrar);
            AgregarFila(info, "Usuario responsable", _guia.UsuarioEmisor, "Autorizado por", _guia.UsuarioAutorizador);
            doc.Blocks.Add(info);

            Paragraph titulo = new(new Run("DETALLE DE PRODUCTOS")) { FontWeight = FontWeights.Bold, Margin = new Thickness(0, 18, 0, 7) };
            doc.Blocks.Add(titulo);
            Table detalle = NuevaTabla(4, 0);
            detalle.Columns[0].Width = new GridLength(100);
            detalle.Columns[1].Width = new GridLength(400);
            detalle.Columns[2].Width = new GridLength(90);
            detalle.Columns[3].Width = new GridLength(110);
            AgregarEncabezado(detalle, "Código", "Producto (observación)", "Cantidad", "Estado");
            foreach (GuiaInternaDetalle d in _guia.Detalles.Where(d => d.CantidadDespachar >= 1))
                AgregarFilaSimple(detalle, d.CodigoProducto, ProductoConObservacion(d), d.CantidadDespachar.ToString("N2"), EstadoDetalle(d));
            doc.Blocks.Add(detalle);

            doc.Blocks.Add(new Paragraph(new Run("OBSERVACIONES")) { FontWeight = FontWeights.Bold, Margin = new Thickness(0, 16, 0, 4) });
            doc.Blocks.Add(new Paragraph(new Run(Valor(_guia.Observacion))) { BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(1), Padding = new Thickness(8, 8, 8, 24), Margin = new Thickness(0) });
            Table firmas = NuevaTabla(3, 0); firmas.Margin = new Thickness(0, 65, 0, 0);
            AgregarFilaSimple(firmas, "________________________\nENTREGADO POR", "________________________\nRECIBIDO POR", "________________________\nAUTORIZADO POR");
            doc.Blocks.Add(firmas);
            if (impresion != null)
            {
                string tituloAuditoria = impresion.EsReimpresion ? "REIMPRESIÓN" : "IMPRESIÓN ORIGINAL";
                string usuario = impresion.EsReimpresion ? "Reimpreso por" : "Impreso por";
                string fecha = impresion.EsReimpresion ? "Fecha y hora de reimpresión" : "Fecha y hora de impresión";
                doc.Blocks.Add(new Paragraph(new Run(tituloAuditoria))
                {
                    FontWeight = FontWeights.Bold,
                    FontSize = impresion.EsReimpresion ? 13 : 10,
                    Foreground = impresion.EsReimpresion ? Brushes.DarkRed : Brushes.Black,
                    Margin = new Thickness(0, 24, 0, 2),
                    KeepTogether = true
                });
                doc.Blocks.Add(new Paragraph(new Run($"{usuario}: {impresion.NombreUsuario}\n{fecha}: {impresion.FechaImpresion:dd/MM/yyyy HH:mm:ss}"))
                {
                    FontSize = 9,
                    Margin = new Thickness(0),
                    KeepTogether = true
                });
            }
            return doc;
        }

        private void Descargar_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new() { Title = "Guardar guía interna", FileName = $"Guia_Interna_{_guia.NumeroGuia}.pdf", Filter = "PDF|*.pdf" };
            if (dialog.ShowDialog(this) != true) return;
            try { GuiaInternaPdfExporter.Exportar(dialog.FileName, _empresa, _guia); NotificationService.Success("PDF generado correctamente."); }
            catch (Exception ex) { NotificationService.Error($"No se pudo generar el PDF: {ex.Message}"); }
        }

        private void Imprimir_Click(object sender, RoutedEventArgs e)
        {
            string? error = GuiaInternaImpresionService.Reimprimir(_guia);
            if (error == null)
                NotificationService.Success("Reimpresión enviada y registrada correctamente.");
            else
                NotificationService.Warning(error);
        }

        internal void EnviarAImpresora(PrintQueue cola, GuiaInternaImpresion impresion)
        {
            FlowDocument documento = CrearDocumento(impresion);
            PrintTicket solicitado = new()
            {
                PageMediaSize = new PageMediaSize(PageMediaSizeName.ISOA4, AnchoA4, AltoA4),
                PageOrientation = PageOrientation.Portrait
            };
            PrintTicket validado = cola.MergeAndValidatePrintTicket(cola.UserPrintTicket, solicitado).ValidatedPrintTicket;
            PrintDialog dialog = new()
            {
                PrintQueue = cola,
                PrintTicket = validado
            };

            double anchoPagina = validado.PageMediaSize?.Width ?? AnchoA4;
            double altoPagina = validado.PageMediaSize?.Height ?? AltoA4;
            documento.PageWidth = anchoPagina;
            documento.PageHeight = altoPagina;
            documento.ColumnWidth = Math.Max(1, anchoPagina - documento.PagePadding.Left - documento.PagePadding.Right);
            DocumentPaginator paginador = ((IDocumentPaginatorSource)documento).DocumentPaginator;
            paginador.PageSize = new Size(anchoPagina, altoPagina);
            dialog.PrintDocument(paginador, $"Guía interna {_guia.NumeroGuia}");
        }

        private Image? CrearLogo()
        {
            try
            {
                BitmapImage bitmap = new(); bitmap.BeginInit();
                if (_empresa.Logo is { Length: > 0 }) bitmap.StreamSource = new System.IO.MemoryStream(_empresa.Logo);
                else bitmap.UriSource = new Uri("pack://application:,,,/Images/LOGO.png", UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad; bitmap.EndInit(); bitmap.Freeze();
                return new Image { Source = bitmap, Width = 90, Height = 55, Stretch = Stretch.Uniform };
            }
            catch { return null; }
        }

        private string NombreEmpresa() => string.IsNullOrWhiteSpace(_empresa.NombreComercial) ? _empresa.Nombre : _empresa.NombreComercial;
        private string FechaHora() => $"{_guia.FechaEmision:dd/MM/yyyy} {_guia.FechaRegistro:HH:mm}";
        private static string ProductoConObservacion(GuiaInternaDetalle detalle) => string.IsNullOrWhiteSpace(detalle.Observacion)
            ? detalle.NombreProducto
            : $"{detalle.NombreProducto} ({detalle.Observacion.Trim()})";
        private static string EstadoDetalle(GuiaInternaDetalle detalle) => detalle.CantidadPendiente > 0 ? "PARCIAL" : "COMPLETO";
        private static string Valor(string? valor) => string.IsNullOrWhiteSpace(valor) ? "-" : valor;
        private static TextBlock Texto(string text, double size = 11, FontWeight? weight = null, TextAlignment alignment = TextAlignment.Left) => new() { Text = Valor(text), FontSize = size, FontWeight = weight ?? FontWeights.Normal, TextAlignment = alignment, TextWrapping = TextWrapping.Wrap };
        private static TableCell Celda(Block block, Thickness padding, Brush background) => new(block) { Padding = padding, Background = background, BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(1) };
        private static Table NuevaTabla(int columnas, double ancho) { Table t = new() { CellSpacing = 0 }; for (int i = 0; i < columnas; i++) t.Columns.Add(new TableColumn { Width = ancho > 0 ? new GridLength(ancho) : GridLength.Auto }); t.RowGroups.Add(new TableRowGroup()); return t; }
        private static void AgregarFila(Table t, string e1, string v1, string e2, string v2) => AgregarFilaSimple(t, e1, v1, e2, v2, true);
        private static void AgregarEncabezado(Table t, params string[] values) { TableRow r = new(); foreach (string v in values) r.Cells.Add(new TableCell(new Paragraph(new Run(v)) { Margin = new Thickness(0), FontWeight = FontWeights.Bold }) { Padding = new Thickness(5), Background = new SolidColorBrush(Color.FromRgb(226, 232, 240)), BorderBrush = Brushes.Gray, BorderThickness = new Thickness(0.5) }); t.RowGroups[0].Rows.Add(r); }
        private static void AgregarFilaSimple(Table t, params string[] values) => AgregarFilaSimple(t, values, false);
        private static void AgregarFilaSimple(Table t, string e1, string v1, string e2, string v2, bool etiquetas) => AgregarFilaSimple(t, new[] { e1, v1, e2, v2 }, etiquetas);
        private static void AgregarFilaSimple(Table t, string[] values, bool etiquetas) { TableRow r = new(); for (int i = 0; i < values.Length; i++) r.Cells.Add(new TableCell(new Paragraph(new Run(Valor(values[i]))) { Margin = new Thickness(0), FontWeight = etiquetas && i % 2 == 0 ? FontWeights.Bold : FontWeights.Normal }) { Padding = new Thickness(5), BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(0.5) }); t.RowGroups[0].Rows.Add(r); }
    }
}
