using CorexProd.Entidad.Entidades;
using CorexProd.Entidad.Utilidades;
using CorexProd.Negocio.Negocio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Printing;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CorexProd.WPF.Modules.Produccion.Views
{
    public partial class OrdenTrabajoImpresionWindow : Window
    {
        private const double AnchoA4 = 793.7007874015749;
        private const double AltoA4 = 1122.51968503937;
        private readonly FlowDocument _documento;
        private readonly bool _requiereMarcaAgua;

        public OrdenTrabajoImpresionWindow(
            string numeroOt,
            string numeroOci,
            string ordenCompraCliente,
            string cliente,
            string usuarioGenerador,
            string usuarioAutorizador,
            DateTime fecha,
            string observacion,
            IEnumerable<OrdenTrabajoImpresionItem> items,
            DateTime? fechaEntregaOt = null,
            DateTime? fechaPactadaOc = null,
            string estadoAprobacion = "PENDIENTE_APROBACION",
            string usuarioAprobadorGerencia = "")
        {
            InitializeComponent();
            _documento = CrearDocumento(numeroOt, numeroOci, ordenCompraCliente, cliente,
                usuarioGenerador, usuarioAutorizador, fecha, observacion, items.ToList(),
                fechaEntregaOt, fechaPactadaOc, estadoAprobacion, usuarioAprobadorGerencia);
            _requiereMarcaAgua = !string.Equals(estadoAprobacion, "APROBADA", StringComparison.OrdinalIgnoreCase);
            MarcaAguaText.Visibility = _requiereMarcaAgua ? Visibility.Visible : Visibility.Collapsed;
            Visor.Document = _documento;
        }

        private static FlowDocument CrearDocumento(string numeroOt, string numeroOci, string ordenCompraCliente,
            string cliente, string usuarioGenerador, string usuarioAutorizador, DateTime fecha, string observacion,
            IReadOnlyCollection<OrdenTrabajoImpresionItem> items,
            DateTime? fechaEntregaOt,
            DateTime? fechaPactadaOc,
            string estadoAprobacion,
            string usuarioAprobadorGerencia)
        {
            FlowDocument documento = new()
            {
                PageWidth = AnchoA4,
                PageHeight = AltoA4,
                PagePadding = new Thickness(28),
                ColumnWidth = AnchoA4 - 56,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 11
            };

            documento.Blocks.Add(CrearCabecera(numeroOt, numeroOci, ordenCompraCliente, cliente,
                usuarioGenerador, usuarioAutorizador, fecha, fechaEntregaOt, fechaPactadaOc));

            List<IGrouping<string, OrdenTrabajoImpresionItem>> grupos = items
                .GroupBy(x => string.IsNullOrWhiteSpace(x.Modelo) ? CodigoModeloProducto.Obtener(x.Codigo) : x.Modelo,
                    StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (IGrouping<string, OrdenTrabajoImpresionItem> grupo in grupos)
                documento.Blocks.Add(CrearBloqueModelo(grupo, grupos.IndexOf(grupo) + 1));

            decimal totalOc = items.Sum(x => x.CantidadOc);
            decimal totalStock = items.Sum(x => x.CantidadStock);
            documento.Blocks.Add(CrearTotalGeneral(totalOc, totalStock));
            documento.Blocks.Add(CrearObservacionesGenerales(observacion));
            documento.Blocks.Add(CrearFirmas(usuarioGenerador, usuarioAprobadorGerencia));
            return documento;
        }

        private static Section CrearCabecera(string numeroOt, string numeroOci, string ordenCompraCliente, string cliente,
            string usuarioGenerador, string usuarioAutorizador, DateTime fecha, DateTime? fechaEntregaOt, DateTime? fechaPactadaOc)
        {
            Section section = new() { Margin = new Thickness(0, 0, 0, 12) };
            Table top = Tabla(3);
            top.Columns[0].Width = new GridLength(190);
            top.Columns[1].Width = new GridLength(360);
            top.Columns[2].Width = new GridLength(190);
            TableRow row = new();
            row.Cells.Add(new TableCell(new BlockUIContainer(CrearLogo())) { Padding = new Thickness(4), RowSpan = 2 });
            TableCell centro = new() { Padding = new Thickness(4), BorderThickness = new Thickness(0), RowSpan = 2 };
            centro.Blocks.Add(new Paragraph(new Run("ORDEN DE TRABAJO (OT)")) { FontSize = 27, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 3, 0, 8) });
            centro.Blocks.Add(new Paragraph(new Run(numeroOt)) { FontSize = 21, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center, Background = new SolidColorBrush(Color.FromRgb(19, 70, 126)), Foreground = Brushes.White, Padding = new Thickness(14, 6, 14, 6), Margin = new Thickness(70, 0, 70, 0) });
            row.Cells.Add(centro);
            row.Cells.Add(new TableCell(new BlockUIContainer(CrearCodigoBarras(numeroOt))) { Padding = new Thickness(0), BorderThickness = new Thickness(0), RowSpan = 2 });
            top.RowGroups[0].Rows.Add(row);
            section.Blocks.Add(top);

            Table info = Tabla(4);
            info.Columns[0].Width = new GridLength(155); info.Columns[1].Width = new GridLength(215);
            info.Columns[2].Width = new GridLength(155); info.Columns[3].Width = new GridLength(215);
            Fila(info, "Empresa / Cliente", Valor(cliente), "Fecha y hora de emisión", fecha.ToString("dd/MM/yyyy HH:mm"));
            Fila(info, "Orden de Compra Cliente", Valor(ordenCompraCliente), "Fecha entrega OT", fechaEntregaOt?.ToString("dd/MM/yyyy") ?? "No registrada");
            Fila(info, "OCI (Referencia)", Valor(numeroOci), "Fecha pactada OC", fechaPactadaOc?.ToString("dd/MM/yyyy") ?? "No registrada");
            Fila(info, "Elaborado por", Valor(usuarioGenerador), "Autorizado por", Valor(usuarioAutorizador));
            section.Blocks.Add(info);
            return section;
        }

        private static Section CrearBloqueModelo(IGrouping<string, OrdenTrabajoImpresionItem> grupo, int numero)
        {
            OrdenTrabajoImpresionItem primero = grupo.First();
            string modelo = string.IsNullOrWhiteSpace(grupo.Key) ? primero.Codigo : grupo.Key;
            string nombre = ObtenerNombreModelo(primero.Producto);
            Section section = new() { Margin = new Thickness(0, 12, 0, 0) };
            Paragraph encabezado = new(new Run($"{numero}    Modelo: {modelo} – {nombre}")) { FontSize = 15, FontWeight = FontWeights.Bold, Background = new SolidColorBrush(Color.FromRgb(218, 234, 252)), Padding = new Thickness(10, 7, 10, 7), Margin = new Thickness(0) };
            section.Blocks.Add(encabezado);
            string observacion = grupo.Select(x => x.Observacion).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;
            section.Blocks.Add(new Paragraph(new Run("Observación del modelo:")) { FontSize = 12, FontWeight = FontWeights.Bold, Margin = new Thickness(8, 5, 8, 2) });
            TextBox observacionEditor = new()
            {
                Text = observacion,
                FontSize = 11,
                FontStyle = FontStyles.Italic,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                MinHeight = 42,
                Padding = new Thickness(8),
                BorderBrush = new SolidColorBrush(Color.FromRgb(145, 170, 205)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(4, 0, 4, 8)
            };
            section.Blocks.Add(new BlockUIContainer(observacionEditor));

            Table tabla = Tabla(6);
            tabla.Columns[0].Width = new GridLength(42); tabla.Columns[1].Width = new GridLength(105); tabla.Columns[2].Width = new GridLength(95); tabla.Columns[3].Width = new GridLength(135); tabla.Columns[4].Width = new GridLength(150); tabla.Columns[5].Width = new GridLength(145);
            Encabezado(tabla, "N.º", "Código", "Talla", "Para OC", "Adicional para Stock", "Total a Producir");
            int fila = 1;
            foreach (OrdenTrabajoImpresionItem item in grupo)
            {
                FilaSimple(tabla, fila.ToString(), item.Codigo, ObtenerTalla(item.Codigo), item.CantidadOc.ToString("N2"), item.CantidadStock.ToString("N2"), item.Total.ToString("N2"));
                fila++;
            }
            FilaTotal(tabla, $"Total del Modelo {modelo}:", grupo.Sum(x => x.CantidadOc), grupo.Sum(x => x.CantidadStock), grupo.Sum(x => x.Total));
            section.Blocks.Add(tabla);
            return section;
        }

        private static Table CrearTotalGeneral(decimal oc, decimal stock)
        {
            Table tabla = Tabla(4);
            tabla.Columns[0].Width = new GridLength(390); tabla.Columns[1].Width = new GridLength(130); tabla.Columns[2].Width = new GridLength(150); tabla.Columns[3].Width = new GridLength(145);
            FilaTotalGeneral(tabla, "TOTAL GENERAL A PRODUCIR:", oc, stock, oc + stock);
            return tabla;
        }

        private static Paragraph CrearObservacionesGenerales(string observacion)
        {
            Paragraph p = new(new Run($"OBSERVACIONES GENERALES:\n{Valor(observacion)}")) { FontSize = 11, FontWeight = FontWeights.Bold, Padding = new Thickness(10), BorderBrush = new SolidColorBrush(Color.FromRgb(145, 170, 205)), BorderThickness = new Thickness(0.8), Margin = new Thickness(0, 0, 0, 12) };
            return p;
        }

        private static Table CrearFirmas(string usuarioGenerador, string usuarioAprobadorGerencia)
        {
            List<Usuario> usuarios = new UsuarioNegocio().Listar();
            Usuario? elaborador = BuscarUsuario(usuarios, usuarioGenerador);
            Usuario? gerente = BuscarUsuario(usuarios, usuarioAprobadorGerencia);
            Table tabla = Tabla(3); tabla.Columns[0].Width = new GridLength(250); tabla.Columns[1].Width = new GridLength(250); tabla.Columns[2].Width = new GridLength(250);
            TableRow row = new();
            row.Cells.Add(CrearCeldaFirma("Elaborado por", elaborador));
            row.Cells.Add(CrearCeldaFirma("Recibido por", null));
            row.Cells.Add(CrearCeldaFirma("Autorizado por\n(Gerencia)", gerente));
            tabla.RowGroups[0].Rows.Add(row); return tabla;
        }

        private static TableCell CrearCeldaFirma(string etiqueta, Usuario? usuario)
        {
            TableCell celda = new() { Padding = new Thickness(8, 8, 8, 16), BorderBrush = new SolidColorBrush(Color.FromRgb(31, 56, 96)), BorderThickness = new Thickness(0.8) };
            if (usuario?.FirmaImagen is { Length: > 0 } bytes)
            {
                try
                {
                    using MemoryStream stream = new(bytes);
                    BitmapImage bitmap = new();
                    bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad; bitmap.StreamSource = stream; bitmap.EndInit(); bitmap.Freeze();
                    celda.Blocks.Add(new BlockUIContainer(new Image
                    {
                        Source = bitmap,
                        Width = 220,
                        Height = 96,
                        Stretch = Stretch.Uniform,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        // Baja la firma hacia la línea sin desplazar las etiquetas inferiores.
                        Margin = new Thickness(0, 14, 0, -14)
                    }) { TextAlignment = TextAlignment.Center });
                }
                catch { }
            }
            else
            {
                // Conserva la misma altura visual para "Recibido por" aunque no tenga firma cargada.
                celda.Blocks.Add(new BlockUIContainer(new Border { Height = 96, Background = Brushes.Transparent })
                {
                    TextAlignment = TextAlignment.Center
                });
            }

            // El nombre ya forma parte de la imagen de firma. No se repite debajo.
            celda.Blocks.Add(new Paragraph(new Run($"\n________________________\n{etiqueta}"))
            {
                TextAlignment = TextAlignment.Center,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0)
            });
            return celda;
        }

        private static Usuario? BuscarUsuario(IEnumerable<Usuario> usuarios, string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return null;
            return usuarios.FirstOrDefault(x => x.NombreUsuario.Equals(nombre, StringComparison.OrdinalIgnoreCase)
                || x.NombreEmpleado.Equals(nombre, StringComparison.OrdinalIgnoreCase)
                || x.NombreCompleto.Equals(nombre, StringComparison.OrdinalIgnoreCase));
        }

        private static Image CrearLogo()
        {
            byte[]? bytes = new EmpresaNegocio().ObtenerPredeterminada()?.Logo;
            BitmapImage imagen = new();
            if (bytes is { Length: > 0 })
            {
                using MemoryStream stream = new(bytes); imagen.BeginInit(); imagen.CacheOption = BitmapCacheOption.OnLoad; imagen.StreamSource = stream; imagen.EndInit();
            }
            else
            {
                imagen.BeginInit(); imagen.UriSource = new Uri("pack://application:,,,/Images/LOGO.png"); imagen.CacheOption = BitmapCacheOption.OnLoad; imagen.EndInit();
            }
            return new Image { Source = imagen, Width = 190, Height = 76, Stretch = Stretch.Uniform };
        }

        private static Image CrearCodigoBarras(string texto)
        {
            const double modulo = 2; const double alto = 48; string valor = $"*{texto.ToUpperInvariant()}*"; double ancho = 12;
            foreach (char c in valor) ancho += Codigo39.TryGetValue(c, out string? patron) ? patron.Sum(x => x == 'w' ? 5 : 2) + 2 : 0;
            DrawingVisual visual = new(); using (DrawingContext dc = visual.RenderOpen()) { dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, ancho, 70)); double x = 6; foreach (char c in valor) { if (!Codigo39.TryGetValue(c, out string? patron)) continue; for (int i = 0; i < patron.Length; i++) { double w = patron[i] == 'w' ? modulo * 3 : modulo; if (i % 2 == 0) dc.DrawRectangle(Brushes.Black, null, new Rect(x, 5, w, alto)); x += w; } x += modulo; } }
            RenderTargetBitmap bitmap = new((int)Math.Ceiling(ancho), 70, 96, 96, PixelFormats.Pbgra32); bitmap.Render(visual); return new Image { Source = bitmap, Width = Math.Min(175, ancho), Height = 55, Stretch = Stretch.Fill };
        }

        private static readonly Dictionary<char, string> Codigo39 = new()
        {
            ['0']="nnnwwnwnn",['1']="wnnwnnnnw",['2']="nnwwnnnnw",['3']="wnwwnnnnn",['4']="nnnwwnnnw",['5']="wnnwwnnnn",['6']="nnwwwnnnn",['7']="nnnwnnwnw",['8']="wnnwnnwnn",['9']="nnwwnnwnn",['O']="wnnnwnnwn",['T']="nnnnwnwwn",['-']="nwnnnnwnw",['*']="nwnnwnwnn"
        };

        private static string ObtenerNombreModelo(string producto) => Regex.Replace(Valor(producto), @"\s*[-–]?\s*(TALLA\s+[A-Z0-9]+|T\d+)\s*$", string.Empty, RegexOptions.IgnoreCase).Trim();
        private static string ObtenerTalla(string codigo) => Regex.Match(codigo ?? string.Empty, @"(?:TALLA)?\s*(XXXL|XXL|XL|XS|L|M|S|T\d+)$", RegexOptions.IgnoreCase).Value.Trim();
        private static Table Tabla(int columnas) { Table tabla = new() { CellSpacing = 0 }; for (int i = 0; i < columnas; i++) tabla.Columns.Add(new TableColumn()); tabla.RowGroups.Add(new TableRowGroup()); return tabla; }
        private static void Fila(Table tabla, string e1, string v1, string e2, string v2) => FilaSimple(tabla, new[] { e1, v1, e2, v2 }, true);
        private static void FilaSimple(Table tabla, params string[] valores) => FilaSimple(tabla, valores, false);
        private static void FilaSimple(Table tabla, string[] valores, bool etiquetas) { TableRow fila = new(); for (int i = 0; i < valores.Length; i++) fila.Cells.Add(new TableCell(new Paragraph(new Run(Valor(valores[i]))) { Margin = new Thickness(0), FontWeight = etiquetas && i % 2 == 0 ? FontWeights.Bold : FontWeights.Normal, TextAlignment = i > 0 && !etiquetas ? TextAlignment.Center : TextAlignment.Left }) { Padding = new Thickness(5), BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(0.5) }); tabla.RowGroups[0].Rows.Add(fila); }
        private static void Encabezado(Table tabla, params string[] valores) { TableRow fila = new(); foreach (string valor in valores) fila.Cells.Add(new TableCell(new Paragraph(new Run(valor)) { Margin = new Thickness(0), FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center, Foreground = Brushes.White }) { Padding = new Thickness(5), Background = new SolidColorBrush(Color.FromRgb(16, 65, 119)), BorderBrush = Brushes.White, BorderThickness = new Thickness(0.5) }); tabla.RowGroups[0].Rows.Add(fila); }
        private static void FilaTotal(Table tabla, string etiqueta, decimal oc, decimal stock, decimal total) { TableRow fila = new(); fila.Cells.Add(new TableCell(new Paragraph(new Run(etiqueta)) { FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Right }) { ColumnSpan = 3, Padding = new Thickness(5), Background = new SolidColorBrush(Color.FromRgb(218, 234, 252)) }); foreach (decimal n in new[] { oc, stock, total }) fila.Cells.Add(new TableCell(new Paragraph(new Run(n.ToString("N2"))) { FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Right }) { Padding = new Thickness(5), Background = new SolidColorBrush(Color.FromRgb(218, 234, 252)) }); tabla.RowGroups[0].Rows.Add(fila); }
        private static void FilaTotalGeneral(Table tabla, string etiqueta, decimal oc, decimal stock, decimal total) { TableRow fila = new(); fila.Cells.Add(new TableCell(new Paragraph(new Run(etiqueta)) { FontSize = 14, FontWeight = FontWeights.Bold, Foreground = Brushes.White, TextAlignment = TextAlignment.Right }) { Padding = new Thickness(6), Background = new SolidColorBrush(Color.FromRgb(12, 60, 112)) }); foreach (decimal n in new[] { oc, stock, total }) fila.Cells.Add(new TableCell(new Paragraph(new Run(n.ToString("N2"))) { FontSize = 13, FontWeight = FontWeights.Bold, Foreground = Brushes.White, TextAlignment = TextAlignment.Right }) { Padding = new Thickness(6), Background = new SolidColorBrush(Color.FromRgb(12, 60, 112)) }); tabla.RowGroups[0].Rows.Add(fila); }
        private static string Valor(string? valor) => string.IsNullOrWhiteSpace(valor) ? "-" : valor.Trim();
        private void Imprimir_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog dialogo = new();
            if (dialogo.ShowDialog() != true) return;
            _documento.PageWidth = dialogo.PrintableAreaWidth > 0 ? dialogo.PrintableAreaWidth : AnchoA4;
            _documento.PageHeight = dialogo.PrintableAreaHeight > 0 ? dialogo.PrintableAreaHeight : AltoA4;
            _documento.ColumnWidth = Math.Max(1, _documento.PageWidth - _documento.PagePadding.Left - _documento.PagePadding.Right);
            DocumentPaginator paginator = ((IDocumentPaginatorSource)_documento).DocumentPaginator;
            paginator.PageSize = new Size(_documento.PageWidth, _documento.PageHeight);
            if (_requiereMarcaAgua)
                paginator = new MarcaAguaDocumentPaginator(paginator);
            dialogo.PrintDocument(paginator, "Orden de Trabajo");
        }

        private sealed class MarcaAguaDocumentPaginator : DocumentPaginator
        {
            private readonly DocumentPaginator _origen;
            public MarcaAguaDocumentPaginator(DocumentPaginator origen) => _origen = origen;
            public override bool IsPageCountValid => _origen.IsPageCountValid;
            public override int PageCount => _origen.PageCount;
            public override Size PageSize { get => _origen.PageSize; set => _origen.PageSize = value; }
            public override IDocumentPaginatorSource Source => _origen.Source;
            public override void ComputePageCount() => _origen.ComputePageCount();
            public override DocumentPage GetPage(int pageNumber)
            {
                DocumentPage pagina = _origen.GetPage(pageNumber);
                DrawingVisual marca = new();
                using (DrawingContext dc = marca.RenderOpen())
                {
                    double centroX = pagina.Size.Width / 2;
                    double centroY = pagina.Size.Height * 0.62;
                    dc.PushTransform(new RotateTransform(35, centroX, centroY));
                    FormattedText texto = new(
                        "PENDIENTE DE APROBACIÓN",
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"),
                        58,
                        new SolidColorBrush(Color.FromArgb(88, 110, 110, 110)),
                        96);
                    dc.DrawText(texto, new Point(centroX - texto.Width / 2, centroY - texto.Height / 2));
                    dc.Pop();
                }
                ContainerVisual contenedor = new();
                contenedor.Children.Add(pagina.Visual);
                contenedor.Children.Add(marca);
                return new DocumentPage(contenedor, pagina.Size, pagina.BleedBox, pagina.ContentBox);
            }
        }
    }

    public sealed class OrdenTrabajoImpresionItem
    {
        public string Codigo { get; init; } = string.Empty;
        public string Producto { get; init; } = string.Empty;
        public string Observacion { get; init; } = string.Empty;
        public string Modelo { get; init; } = string.Empty;
        public decimal CantidadOc { get; init; }
        public decimal CantidadStock { get; init; }
        public decimal Total => CantidadOc + CantidadStock;
    }
}
