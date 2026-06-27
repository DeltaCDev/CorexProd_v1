using System.Collections.ObjectModel;
using System.Text;
using Android.Graphics;
using CorexProd.App.Models;
using CorexProd.App.Services;
using AColor = Android.Graphics.Color;
using ACanvas = Android.Graphics.Canvas;
using APaint = Android.Graphics.Paint;

namespace CorexProd.App.Pages;

public partial class StockProductosPage : ContentPage
{
    private readonly CorexProdApiClient _apiClient;
    private readonly SessionState _session;
    private readonly ObservableCollection<ProductoStock> _productos = [];
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
            IReadOnlyList<ProductoStock> items = _session.EsDemo
                ? DemoData.Productos
                : (await _apiClient.GetProductosAsync(Search.Text ?? string.Empty)).Items;
            List<ProductoStock> productosFiltrados = FiltrarPorEtiquetaOCliente(FiltrarGeneral(items, Search.Text), EtiquetaSearch.Text).ToList();
            _productos.Clear();
            foreach (ProductoStock item in productosFiltrados
                         .OrderBy(x => ProductoOrdenHelper.CrearClave(x.Codigo, x.Producto).Cliente)
                         .ThenBy(x => ProductoOrdenHelper.CrearClave(x.Codigo, x.Producto).NumeroNuloOrden)
                         .ThenBy(x => ProductoOrdenHelper.CrearClave(x.Codigo, x.Producto).Numero)
                         .ThenBy(x => ProductoOrdenHelper.CrearClave(x.Codigo, x.Producto).Variante)
                         .ThenBy(x => ProductoOrdenHelper.CrearClave(x.Codigo, x.Producto).OrdenTalla)
                         .ThenBy(x => ProductoOrdenHelper.CrearClave(x.Codigo, x.Producto).TallaNumero)
                         .ThenBy(x => ProductoOrdenHelper.CrearClave(x.Codigo, x.Producto).CodigoOrden)
                         .ThenBy(x => ProductoOrdenHelper.CrearClave(x.Codigo, x.Producto).NombreProducto))
            {
                _productos.Add(item);
            }

            CountLabel.Text = $"{_productos.Count} producto(s)";
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
        foreach (ProductoStock producto in _productos)
        {
            sb.Append(producto.EtiquetaCliente);
            sb.Append(" | ");
            sb.Append(producto.Codigo);
            sb.Append(" | ");
            sb.Append(producto.Producto);
            sb.Append(" | ");
            sb.Append(producto.StockActual.ToString("N3"));
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

        List<(ProductoStock Producto, int Alto, List<string> NombreLineas, List<string> EtiquetaLineas)> filas = [];
        foreach (ProductoStock producto in _productos)
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
        canvas.DrawText("Stock productos filtrado", margin, y + 38, titlePaint);
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
        canvas.DrawText("CANT.", xCantidad, y + 38, headerPaint);
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
            canvas.DrawText(fila.Producto.StockActual.ToString("N3"), xCantidad, textY, qtyPaint);
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

    private static IEnumerable<ProductoStock> FiltrarPorEtiquetaOCliente(IEnumerable<ProductoStock> productos, string? filtro)
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

    private static IEnumerable<ProductoStock> FiltrarGeneral(IEnumerable<ProductoStock> productos, string? filtro)
    {
        string texto = filtro?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(texto))
            return productos;

        return productos.Where(x => Contiene(x.Codigo, texto) || Contiene(x.Producto, texto) || Contiene(x.EtiquetaCliente, texto));
    }

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
