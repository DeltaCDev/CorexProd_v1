using CorexProd.Entidad.Entidades;
using CorexProd.WPF.Helpers;
using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CorexProd.WPF.Modules.Ventas.Views
{
    public partial class GuiaInternaDocumentoWindow : Window
    {
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

        private FlowDocument CrearDocumento()
        {
            FlowDocument doc = new() { PageWidth = 793, PageHeight = 1122, PagePadding = new Thickness(46), FontFamily = new FontFamily("Segoe UI"), FontSize = 11 };
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
            AgregarFila(info, "Número de OCI", Valor(_guia.NumeroOci), "Número de proforma", Valor(_guia.NumeroProforma));
            AgregarFila(info, "Motivo de salida", Valor(_guia.MotivoEmisionManual), "Destino", _guia.ClienteMostrar);
            AgregarFila(info, "Usuario responsable", _guia.UsuarioEmisor, "Autorizado por", _guia.UsuarioAutorizador);
            doc.Blocks.Add(info);

            Paragraph titulo = new(new Run("DETALLE DE PRODUCTOS")) { FontWeight = FontWeights.Bold, Margin = new Thickness(0, 18, 0, 7) };
            doc.Blocks.Add(titulo);
            Table detalle = NuevaTabla(6, 0);
            detalle.Columns[0].Width = new GridLength(75); detalle.Columns[1].Width = new GridLength(260);
            detalle.Columns[2].Width = new GridLength(70); detalle.Columns[3].Width = new GridLength(90);
            detalle.Columns[4].Width = new GridLength(90); detalle.Columns[5].Width = new GridLength(130);
            AgregarEncabezado(detalle, "Código", "Producto", "Unidad", "Despachada", "Pendiente", "Observación");
            foreach (GuiaInternaDetalle d in _guia.Detalles)
                AgregarFilaSimple(detalle, d.CodigoProducto, d.NombreProducto, d.NombreUnidad, d.CantidadDespachar.ToString("N2"), d.CantidadPendiente.ToString("N2"), d.Observacion);
            doc.Blocks.Add(detalle);

            doc.Blocks.Add(new Paragraph(new Run("OBSERVACIONES")) { FontWeight = FontWeights.Bold, Margin = new Thickness(0, 16, 0, 4) });
            doc.Blocks.Add(new Paragraph(new Run(Valor(_guia.Observacion))) { BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(1), Padding = new Thickness(8, 8, 8, 24), Margin = new Thickness(0) });
            Table firmas = NuevaTabla(3, 0); firmas.Margin = new Thickness(0, 65, 0, 0);
            AgregarFilaSimple(firmas, "________________________\nENTREGADO POR", "________________________\nRECIBIDO POR", "________________________\nAUTORIZADO POR");
            doc.Blocks.Add(firmas);
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
            PrintDialog dialog = new();
            if (dialog.ShowDialog() == true) dialog.PrintDocument(((IDocumentPaginatorSource)_documento).DocumentPaginator, $"Guía interna {_guia.NumeroGuia}");
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
