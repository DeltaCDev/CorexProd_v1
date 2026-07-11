using System.Collections.ObjectModel;
using System.Text;
using Android.Graphics;
using CorexProd.App.Models;
using CorexProd.App.Services;
using AColor = Android.Graphics.Color;
using ACanvas = Android.Graphics.Canvas;
using APaint = Android.Graphics.Paint;
using MColor = Microsoft.Maui.Graphics.Color;
using MRoundRectangle = Microsoft.Maui.Controls.Shapes.RoundRectangle;

namespace CorexProd.App.Pages;

public partial class StockProductosPage : ContentPage
{
    private readonly CorexProdApiClient _apiClient;
    private readonly SessionState _session;
    private readonly ObservableCollection<StockDisponibilidad> _productos = [];
    private CancellationTokenSource? _searchDelay;

    public StockProductosPage()
    {
        InitializeComponent();
        _apiClient = ServiceHelper.GetRequiredService<CorexProdApiClient>();
        _session = ServiceHelper.GetRequiredService<SessionState>();
        ItemsView.ItemsSource = _productos;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_productos.Count == 0)
        {
            await LoadAsync();
        }
    }

    private async void OnBuscarClicked(object? sender, EventArgs e)
    {
        await LoadAsync();
    }

    private async void OnSearchPressed(object? sender, EventArgs e)
    {
        await LoadAsync();
    }

    private async void OnRefreshing(object? sender, EventArgs e)
    {
        await LoadAsync();
    }

    private async void OnExportarClicked(object? sender, EventArgs e)
    {
        if (_productos.Count == 0)
        {
            await DisplayAlertAsync("Exportar stock", "No hay productos filtrados para exportar.", "OK");
            return;
        }

        if (_productos.Count > 15)
        {
            string texto = CrearTextoExportacion();
            await Clipboard.Default.SetTextAsync(texto);
            await DisplayAlertAsync("Exportar stock", "Hay más de 15 productos. Se copió el listado filtrado como texto.", "OK");
            return;
        }

        try
        {
            string filePath = await CrearImagenExportacionAsync();

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Stock productos",
                File = new ShareFile(filePath)
            });
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Exportar stock", ex.Message, "OK");
        }
    }

    private async void OnHistorialClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not StockDisponibilidad item)
            return;

        try
        {
            IReadOnlyList<StockReservaHistorico> historico = _session.EsDemo
                ? DemoData.StockReservaHistorico
                    .Where(x => x.IdProducto == item.IdProducto && (!x.IdAlmacen.HasValue || x.IdAlmacen == item.IdAlmacen))
                    .OrderByDescending(x => x.FechaMovimiento)
                    .ToList()
                : (await _apiClient.GetStockReservaHistoricoAsync(item.IdProducto, item.IdAlmacen, top: 100)).Items;

            await Navigation.PushModalAsync(CrearHistorialPage(item, historico));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Historial de reservas", ex.Message, "OK");
        }
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        _searchDelay?.Cancel();
        _searchDelay = new CancellationTokenSource();
        CancellationToken token = _searchDelay.Token;

        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(450), async () =>
        {
            if (!token.IsCancellationRequested)
            {
                await LoadAsync();
            }
        });
    }

    private async Task LoadAsync()
    {
        try
        {
            Refresh.IsRefreshing = true;
            IReadOnlyList<StockDisponibilidad> items = _session.EsDemo
                ? DemoData.StockDisponibilidadProductos
                : (await _apiClient.GetStockDisponibilidadAsync(Search.Text ?? string.Empty)).Items;
            List<StockDisponibilidad> productosFiltrados = FiltrarPorEtiquetaOCliente(FiltrarGeneral(items, Search.Text), EtiquetaSearch.Text).ToList();
            _productos.Clear();
            foreach (StockDisponibilidad item in productosFiltrados
                         .OrderBy(x => ProductoOrdenHelper.CrearClave(x.Codigo, x.Producto).Cliente)
                         .ThenBy(x => ProductoOrdenHelper.CrearClave(x.Codigo, x.Producto).NumeroNuloOrden)
                         .ThenBy(x => ProductoOrdenHelper.CrearClave(x.Codigo, x.Producto).Numero)
                         .ThenBy(x => ProductoOrdenHelper.CrearClave(x.Codigo, x.Producto).Variante)
                         .ThenBy(x => ProductoOrdenHelper.CrearClave(x.Codigo, x.Producto).OrdenTalla)
                         .ThenBy(x => ProductoOrdenHelper.CrearClave(x.Codigo, x.Producto).TallaNumero)
                         .ThenBy(x => ProductoOrdenHelper.CrearClave(x.Codigo, x.Producto).CodigoOrden)
                         .ThenBy(x => ProductoOrdenHelper.CrearClave(x.Codigo, x.Producto).NombreProducto)
                         .ThenBy(x => x.NombreAlmacen))
            {
                _productos.Add(item);
            }

            CountLabel.Text = $"{_productos.Count} registro(s)";
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Stock productos", ex.Message, "OK");
        }
        finally
        {
            Refresh.IsRefreshing = false;
        }
    }

    private string CrearTextoExportacion()
    {
        StringBuilder sb = new();
        foreach (StockDisponibilidad producto in _productos)
        {
            sb.Append(producto.EtiquetaCliente);
            sb.Append(" | ");
            sb.Append(producto.Codigo);
            sb.Append(" | ");
            sb.Append(producto.Producto);
            sb.Append(" | ");
            sb.Append(producto.NombreAlmacen);
            sb.Append(" | Fisico: ");
            sb.Append(producto.StockFisico.ToString("N3"));
            sb.Append(" | Reservado: ");
            sb.Append(producto.StockReservado.ToString("N3"));
            sb.Append(" | Disponible: ");
            sb.Append(producto.StockDisponible.ToString("N3"));
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    private async Task<string> CrearImagenExportacionAsync()
    {
        const int width = 1280;
        const int margin = 44;
        const int gap = 18;
        const int titleHeight = 150;
        const int headerHeight = 58;
        const int minRowHeight = 112;

        using APaint titlePaint = CrearPaint(AColor.Rgb(16, 50, 74), 38, true);
        using APaint subtitlePaint = CrearPaint(AColor.Rgb(99, 112, 131), 24, false);
        using APaint headerPaint = CrearPaint(AColor.White, 24, true);
        using APaint textPaint = CrearPaint(AColor.Rgb(52, 64, 84), 24, false);
        using APaint strongPaint = CrearPaint(AColor.Rgb(16, 50, 74), 25, true);
        using APaint qtyPaint = CrearPaint(AColor.Rgb(6, 118, 71), 25, true);
        using APaint linePaint = new() { Color = AColor.Rgb(217, 224, 230), StrokeWidth = 2 };
        using APaint headerBg = new() { Color = AColor.Rgb(16, 50, 74) };
        using APaint altBg = new() { Color = AColor.Rgb(250, 251, 252) };
        using APaint whiteBg = new() { Color = AColor.White };

        int etiquetaW = 230;
        int codigoW = 170;
        int cantidadW = 150;
        int productoW = width - (margin * 2) - etiquetaW - codigoW - cantidadW - (gap * 3);

        List<(StockDisponibilidad Producto, int Alto, List<string> NombreLineas, List<string> EtiquetaLineas)> filas = [];
        foreach (StockDisponibilidad producto in _productos)
        {
            List<string> nombreLineas = DividirLineas(producto.Producto, textPaint, productoW);
            List<string> etiquetaLineas = DividirLineas(producto.EtiquetaCliente, textPaint, etiquetaW);
            int lineas = Math.Max(nombreLineas.Count, etiquetaLineas.Count);
            int alto = Math.Max(minRowHeight, 52 + (lineas * 31));
            filas.Add((producto, alto, nombreLineas, etiquetaLineas));
        }

        int height = titleHeight + headerHeight + filas.Sum(x => x.Alto) + margin;
        using Bitmap bitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888!);
        using ACanvas canvas = new(bitmap);
        canvas.DrawColor(AColor.White);

        float y = margin;
        canvas.DrawText("Disponibilidad productos", margin, y + 38, titlePaint);
        string filtros = $"Codigo/producto: {TextoFiltro(Search.Text)}   Etiqueta: {TextoFiltro(EtiquetaSearch.Text)}";
        canvas.DrawText(filtros, margin, y + 76, subtitlePaint);
        canvas.DrawText($"{_productos.Count} producto(s) | {DateTime.Now:dd/MM/yyyy HH:mm}", margin, y + 110, subtitlePaint);
        y += titleHeight;

        canvas.DrawRect(margin, y, width - margin, y + headerHeight, headerBg);
        float xEtiqueta = margin + 14;
        float xCodigo = margin + etiquetaW + gap;
        float xProducto = xCodigo + codigoW + gap;
        float xCantidad = xProducto + productoW + gap;
        canvas.DrawText("ETIQUETA", xEtiqueta, y + 38, headerPaint);
        canvas.DrawText("CODIGO", xCodigo, y + 38, headerPaint);
        canvas.DrawText("PRODUCTO", xProducto, y + 38, headerPaint);
        canvas.DrawText("DISP.", xCantidad, y + 38, headerPaint);
        y += headerHeight;

        for (int i = 0; i < filas.Count; i++)
        {
            var fila = filas[i];
            if (i % 2 == 1)
                canvas.DrawRect(margin, y, width - margin, y + fila.Alto, altBg);
            else
                canvas.DrawRect(margin, y, width - margin, y + fila.Alto, whiteBg);

            float textY = y + 38;
            DibujarLineas(canvas, fila.EtiquetaLineas, xEtiqueta, textY, textPaint);
            canvas.DrawText(fila.Producto.Codigo, xCodigo, textY, strongPaint);
            DibujarLineas(canvas, fila.NombreLineas, xProducto, textY, textPaint);
            canvas.DrawText(fila.Producto.StockDisponible.ToString("N3"), xCantidad, textY, qtyPaint);
            canvas.DrawLine(margin, y + fila.Alto, width - margin, y + fila.Alto, linePaint);
            y += fila.Alto;
        }

        string filePath = System.IO.Path.Combine(FileSystem.CacheDirectory, $"stock_productos_{DateTime.Now:yyyyMMdd_HHmmss}.png");
        await using FileStream stream = File.Create(filePath);
        bitmap.Compress(Bitmap.CompressFormat.Png!, 100, stream);
        await stream.FlushAsync();
        return filePath;
    }

    private static APaint CrearPaint(AColor color, float size, bool bold)
    {
        APaint paint = new(PaintFlags.AntiAlias)
        {
            Color = color,
            TextSize = size
        };
        paint.SetTypeface(bold ? Typeface.Create(Typeface.Default, TypefaceStyle.Bold) : Typeface.Default);
        return paint;
    }

    private static string TextoFiltro(string? value) => string.IsNullOrWhiteSpace(value) ? "Todos" : value.Trim();

    private static IEnumerable<StockDisponibilidad> FiltrarPorEtiquetaOCliente(IEnumerable<StockDisponibilidad> productos, string? filtro)
    {
        string texto = filtro?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(texto))
            return productos;

        return productos.Where(producto =>
        {
            ProductoOrdenClave clave = ProductoOrdenHelper.CrearClave(producto.Codigo, producto.Producto);
            return Contiene(producto.EtiquetaCliente, texto)
                || Contiene(producto.Codigo, texto)
                || Contiene(producto.Producto, texto)
                || Contiene(clave.Cliente, texto);
        });
    }

    private static bool Contiene(string? valor, string filtro)
        => (valor ?? string.Empty).Contains(filtro, StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<StockDisponibilidad> FiltrarGeneral(IEnumerable<StockDisponibilidad> productos, string? filtro)
    {
        string texto = filtro?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(texto))
            return productos;

        return productos.Where(x =>
            Contiene(x.Codigo, texto)
            || Contiene(x.Producto, texto)
            || Contiene(x.EtiquetaCliente, texto)
            || Contiene(x.NombreAlmacen, texto));
    }

    private static ContentPage CrearHistorialPage(StockDisponibilidad producto, IReadOnlyList<StockReservaHistorico> items)
    {
        VerticalStackLayout contenido = new() { Padding = 14, Spacing = 12 };
        contenido.Add(new Label { Text = "Historial de reservas", FontFamily = "OpenSansSemibold", FontSize = 22, TextColor = MColor.FromArgb("#101828") });
        contenido.Add(new Label
        {
            Text = $"{producto.Codigo} | {producto.Producto}",
            TextColor = MColor.FromArgb("#344054"),
            LineBreakMode = LineBreakMode.WordWrap
        });
        contenido.Add(new Label { Text = producto.NombreAlmacen, FontSize = 12, TextColor = MColor.FromArgb("#667085") });

        if (items.Count == 0)
        {
            contenido.Add(new Label { Text = "No hay movimientos de reserva para este producto.", Padding = 12, TextColor = MColor.FromArgb("#667085") });
        }
        else
        {
            foreach (StockReservaHistorico item in items.OrderByDescending(x => x.FechaMovimiento))
                contenido.Add(CrearHistoricoCard(item));
        }

        Button cerrar = new() { Text = "Cerrar", BackgroundColor = MColor.FromArgb("#3F1D95"), TextColor = Colors.White };
        ContentPage page = new()
        {
            Title = "Historial",
            BackgroundColor = MColor.FromArgb("#F4F6F8"),
            Content = new ScrollView { Content = contenido }
        };
        cerrar.Clicked += async (_, _) => await page.Navigation.PopModalAsync();
        contenido.Add(cerrar);
        return page;
    }

    private static Border CrearHistoricoCard(StockReservaHistorico item)
    {
        VerticalStackLayout stack = new() { Spacing = 5 };
        stack.Add(new Label
        {
            Text = $"{item.TipoMovimiento} | {item.CantidadMovimiento:N2}",
            FontFamily = "OpenSansSemibold",
            TextColor = MColor.FromArgb("#10324A")
        });
        stack.Add(new Label { Text = $"{item.FechaMovimiento:dd/MM/yyyy HH:mm} | {item.UsuarioMovimiento}", FontSize = 12, TextColor = MColor.FromArgb("#667085") });
        stack.Add(new Label
        {
            Text = $"OC: {TextoHistorial(item.NumeroOci)} | OT: {TextoHistorial(item.NumeroOT)} | Doc.: {TextoHistorial(item.DocumentoReferencia)}",
            FontSize = 12,
            TextColor = MColor.FromArgb("#475467"),
            LineBreakMode = LineBreakMode.WordWrap
        });
        stack.Add(new Label
        {
            Text = $"Reserva: {item.CantidadReservada:N2} | Consumida: {item.CantidadConsumida:N2} | Liberada: {item.CantidadLiberada:N2} | Pendiente: {item.CantidadPendiente:N2}",
            FontSize = 12,
            TextColor = MColor.FromArgb("#475467"),
            LineBreakMode = LineBreakMode.WordWrap
        });

        if (!string.IsNullOrWhiteSpace(item.ObservacionMovimiento))
            stack.Add(new Label { Text = item.ObservacionMovimiento, FontSize = 12, TextColor = MColor.FromArgb("#667085"), LineBreakMode = LineBreakMode.WordWrap });

        return new Border
        {
            Padding = 12,
            BackgroundColor = Colors.White,
            Stroke = MColor.FromArgb("#D9E0E6"),
            StrokeShape = new MRoundRectangle { CornerRadius = 8 },
            Content = stack
        };
    }

    private static string TextoHistorial(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();

    private static List<string> DividirLineas(string? text, APaint paint, int maxWidth)
    {
        string value = string.IsNullOrWhiteSpace(text) ? "-" : text.Trim();
        string[] words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        List<string> lines = [];
        StringBuilder current = new();

        foreach (string word in words)
        {
            string candidate = current.Length == 0 ? word : $"{current} {word}";
            if (paint.MeasureText(candidate) <= maxWidth)
            {
                current.Clear();
                current.Append(candidate);
                continue;
            }

            if (current.Length > 0)
                lines.Add(current.ToString());

            current.Clear();
            current.Append(word);
        }

        if (current.Length > 0)
            lines.Add(current.ToString());

        return lines.Count == 0 ? ["-"] : lines;
    }

    private static void DibujarLineas(ACanvas canvas, IReadOnlyList<string> lines, float x, float y, APaint paint)
    {
        for (int i = 0; i < lines.Count; i++)
            canvas.DrawText(lines[i], x, y + (i * 31), paint);
    }
}
