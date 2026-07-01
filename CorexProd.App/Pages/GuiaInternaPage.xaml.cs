using System.Collections.ObjectModel;
using System.Globalization;
using CorexProd.App.Models;
using CorexProd.App.Services;

namespace CorexProd.App.Pages;

public partial class GuiaInternaPage : ContentPage
{
    private readonly CorexProdApiClient _apiClient;
    private readonly SessionState _session;
    private readonly ObservableCollection<GuiaInternaListItem> _guias = [];
    private readonly ObservableCollection<ProductoSeleccionItem> _productos = [];
    private readonly ObservableCollection<GuiaManualDetalleItem> _detalles = [];
    private GuiaInternaManualPrepararResponse? _preparacion;
    private ProductoStockBusquedaApi? _productoSeleccionado;
    private int _loadVersion;

    public GuiaInternaPage()
    {
        InitializeComponent();
        _apiClient = ServiceHelper.GetRequiredService<CorexProdApiClient>();
        _session = ServiceHelper.GetRequiredService<SessionState>();
        GuiasView.ItemsSource = _guias;
        ProductosView.ItemsSource = _productos;
        DetalleView.ItemsSource = _detalles;
        MotivoPicker.ItemsSource = new[]
        {
            "Entrega a cliente",
            "Consumo interno",
            "Entrega a un area",
            "Prestamo",
            "Muestra",
            "Reposicion",
            "Donacion",
            "Otro tipo de salida"
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarGuiasAsync();
    }

    private async void OnRefreshing(object? sender, EventArgs e) => await CargarGuiasAsync();
    private async void OnBuscarPressed(object? sender, EventArgs e) => await CargarGuiasAsync();

    private async Task CargarGuiasAsync()
    {
        int loadVersion = Interlocked.Increment(ref _loadVersion);
        string busqueda = BuscarSearch.Text ?? string.Empty;

        try
        {
            Refresh.IsRefreshing = true;
            IReadOnlyList<GuiaInternaResumen> guias = _session.EsDemo
                ? DemoData.GuiasInternas
                    .Where(x => string.IsNullOrWhiteSpace(busqueda)
                        || x.NumeroGuia.Contains(busqueda, StringComparison.OrdinalIgnoreCase)
                        || x.NumeroOci.Contains(busqueda, StringComparison.OrdinalIgnoreCase)
                        || x.EmpresaDestino.Contains(busqueda, StringComparison.OrdinalIgnoreCase)
                        || x.OrdenCompraCliente.Contains(busqueda, StringComparison.OrdinalIgnoreCase))
                    .ToList()
                : (await _apiClient.GetGuiasInternasAsync(busqueda)).Items;

            List<GuiaInternaListItem> nuevosItems = [];
            foreach (GuiaInternaResumen guia in guias.GroupBy(x => x.IdGuiaInterna).Select(x => x.First()))
            {
                if (loadVersion != Volatile.Read(ref _loadVersion))
                    return;

                nuevosItems.Add(await CrearGuiaListItemAsync(guia));
            }

            if (loadVersion != Volatile.Read(ref _loadVersion))
                return;

            _guias.Clear();
            foreach (GuiaInternaListItem guia in nuevosItems)
                _guias.Add(guia);

            ResumenLabel.Text = $"{_guias.Count} guia(s) interna(s)";
        }
        catch (Exception ex)
        {
            if (loadVersion != Volatile.Read(ref _loadVersion))
                return;

            await DisplayAlertAsync("Guia Interna", ex.Message, "OK");
        }
        finally
        {
            if (loadVersion == Volatile.Read(ref _loadVersion))
                Refresh.IsRefreshing = false;
        }
    }

    private async void OnNuevaClicked(object? sender, EventArgs e)
    {
        try
        {
            _preparacion = _session.EsDemo
                ? DemoData.GuiaInternaManualPreparar
                : await _apiClient.PrepararGuiaInternaManualAsync();
            AlmacenLabel.Text = $"Almacen: {_preparacion.Cabecera.NombreAlmacen}";
            MotivoPicker.SelectedIndex = -1;
            LimpiarFormulario();
            MostrarFormulario();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Guia Interna", ex.Message, "OK");
        }
    }

    private async void OnVolverClicked(object? sender, EventArgs e)
    {
        MostrarListado();
        await CargarGuiasAsync();
    }

    private async void OnVerDetalleClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not GuiaInternaListItem item)
            return;

        await Shell.Current.GoToAsync($"{nameof(GuiaInternaDetallePage)}?id={item.Guia.IdGuiaInterna}");
    }

    private async void OnImprimirClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not GuiaInternaListItem item)
            return;

        try
        {
            if (_session.EsDemo)
            {
            await DisplayAlertAsync("Imprimir guia", "Impresion simulada en modo demo.", "OK");
                return;
            }

            await DescargarYAbrirGuiaPdfAsync(item.Guia);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Imprimir guia", ex.Message, "OK");
        }
    }

    private async void OnAnularClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not GuiaInternaListItem item)
            return;
        GuiaInternaResumen guia = item.Guia;

        if (guia.Estado.Equals("Anulada", StringComparison.OrdinalIgnoreCase))
        {
            await DisplayAlertAsync("Guia Interna", "La guia ya esta anulada.", "OK");
            return;
        }

        string motivo = await DisplayPromptAsync("Anular guia", $"Motivo de anulacion para {guia.NumeroGuia}", "Anular", "Cancelar", "Motivo", maxLength: 200) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(motivo))
            return;

        try
        {
            if (_session.EsDemo)
            {
                await DisplayAlertAsync("Guia Interna demo", "Anulacion simulada en modo demo.", "OK");
            }
            else
            {
                DocumentoAccionResponse response = await _apiClient.AnularGuiaInternaAsync(
                    guia.IdGuiaInterna,
                    new DocumentoAccionRequest(_session.Usuario?.NombreUsuario ?? "Android", motivo));
                await DisplayAlertAsync("Guia Interna", response.Mensaje, "OK");
            }

            await CargarGuiasAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Guia Interna", ex.Message, "OK");
        }
    }

    private void OnProductoSearchPressed(object? sender, EventArgs e)
    {
        if (_preparacion == null)
            return;

        string texto = ProductoSearch.Text?.Trim() ?? string.Empty;
        IReadOnlyList<ProductoStockBusquedaApi> productos = string.IsNullOrWhiteSpace(texto)
            ? _preparacion.Productos
            : _preparacion.Productos.Where(x =>
                x.Codigo.Contains(texto, StringComparison.OrdinalIgnoreCase)
                || x.NombreProducto.Contains(texto, StringComparison.OrdinalIgnoreCase)
                || x.EtiquetaCliente.Contains(texto, StringComparison.OrdinalIgnoreCase)).ToList();

        _productos.Clear();
        _productoSeleccionado = null;
        foreach (ProductoStockBusquedaApi producto in productos.Where(x => x.StockActual > 0).Take(40))
            _productos.Add(new ProductoSeleccionItem(producto, false));

        ProductosLabel.Text = _productos.Count == 0 ? "No se encontraron productos con stock." : $"{_productos.Count} producto(s) encontrado(s).";
    }

    private void OnProductoSeleccionado(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not ProductoSeleccionItem item)
            return;

        _productoSeleccionado = item.Producto;
        for (int i = 0; i < _productos.Count; i++)
        {
            ProductoSeleccionItem actual = _productos[i];
            _productos[i] = actual with { Seleccionado = actual.Producto.IdProducto == item.Producto.IdProducto };
        }
    }

    private async void OnAgregarClicked(object? sender, EventArgs e)
    {
        if (_productoSeleccionado == null)
        {
            await DisplayAlertAsync("Guia Interna", "Seleccione un producto.", "OK");
            return;
        }

        if (!decimal.TryParse(CantidadEntry.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal cantidad)
            && !decimal.TryParse(CantidadEntry.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out cantidad))
        {
            await DisplayAlertAsync("Guia Interna", "Ingrese una cantidad valida.", "OK");
            return;
        }

        if (cantidad <= 0 || cantidad > _productoSeleccionado.StockActual)
        {
            await DisplayAlertAsync("Guia Interna", "La cantidad debe ser mayor a cero y no superar el stock.", "OK");
            return;
        }

        GuiaManualDetalleItem? existente = _detalles.FirstOrDefault(x => x.Producto.IdProducto == _productoSeleccionado.IdProducto);
        if (existente == null)
            _detalles.Add(new GuiaManualDetalleItem(_productoSeleccionado, cantidad));
        else
            existente.Cantidad += cantidad;

        CantidadEntry.Text = string.Empty;
        ActualizarTotal();
    }

    private async void OnEmitirClicked(object? sender, EventArgs e)
    {
        if (_preparacion == null)
            return;

        if (MotivoPicker.SelectedItem is not string motivo)
        {
            await DisplayAlertAsync("Guia Interna", "Seleccione el motivo.", "OK");
            return;
        }

        if (_detalles.Count == 0)
        {
            await DisplayAlertAsync("Guia Interna", "Agregue productos para emitir la guia.", "OK");
            return;
        }

        try
        {
            EmitirButton.IsEnabled = false;
            string usuario = _session.Usuario?.NombreUsuario ?? "Android";
            if (_session.EsDemo)
            {
                await DisplayAlertAsync("Guia Interna demo", $"Guia manual demo emitida con {_detalles.Sum(x => x.Cantidad):N2} unidades.", "OK");
            }
            else
            {
                GuiaInternaManualRequest request = new(
                    _preparacion.Cabecera.IdAlmacen,
                    DateTime.Today,
                    null,
                    motivo,
                    string.Join(" | ", new[] { DestinoEntry.Text, ObservacionEditor.Text }.Where(x => !string.IsNullOrWhiteSpace(x))),
                    usuario,
                    usuario,
                    _detalles.Select(x => new GuiaInternaManualDetalleRequest(x.Producto.IdProducto, x.Cantidad, string.Empty)).ToList());

                GuiaInternaEmitirResponse response = await _apiClient.EmitirGuiaInternaManualAsync(request);
                await DisplayAlertAsync("Guia Interna", $"{response.Mensaje}\nGuia: {response.NumeroGuia}", "OK");
            }

            MostrarListado();
            await CargarGuiasAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Guia Interna", ex.Message, "OK");
        }
        finally
        {
            EmitirButton.IsEnabled = true;
        }
    }

    private void OnLimpiarClicked(object? sender, EventArgs e) => LimpiarFormulario();

    private void MostrarListado()
    {
        ListadoPanel.IsVisible = true;
        FormularioPanel.IsVisible = false;
    }

    private void MostrarFormulario()
    {
        ListadoPanel.IsVisible = false;
        FormularioPanel.IsVisible = true;
    }

    private void LimpiarFormulario()
    {
        ProductoSearch.Text = string.Empty;
        DestinoEntry.Text = string.Empty;
        ObservacionEditor.Text = string.Empty;
        CantidadEntry.Text = string.Empty;
        _productos.Clear();
        _detalles.Clear();
        _productoSeleccionado = null;
        ProductosLabel.Text = "Busque un producto para agregar.";
        ActualizarTotal();
    }

    private void ActualizarTotal()
    {
        TotalLabel.Text = $"{_detalles.Count} producto(s) | {_detalles.Sum(x => x.Cantidad):N2} unidades";
    }

    private async Task<GuiaInternaListItem> CrearGuiaListItemAsync(GuiaInternaResumen guia)
    {
        try
        {
            GuiaInternaDetalleResponse detalle = _session.EsDemo
                ? DemoData.GuiaInternaDetalle(guia.IdGuiaInterna)
                : await _apiClient.GetGuiaInternaDetalleAsync(guia.IdGuiaInterna);

            bool esParcial = detalle.Detalles.Any(x => x.CantidadDespachar < x.CantidadPendiente);
            return new GuiaInternaListItem(guia, esParcial);
        }
        catch
        {
            return new GuiaInternaListItem(guia, false);
        }
    }

    private async Task DescargarYAbrirGuiaPdfAsync(GuiaInternaResumen guia)
    {
        byte[] pdf = await _apiClient.GetGuiaInternaPdfAsync(guia.IdGuiaInterna);
        string nombre = string.IsNullOrWhiteSpace(guia.NumeroGuia)
            ? $"guia-interna-{guia.IdGuiaInterna}.pdf"
            : $"{LimpiarNombreArchivo(guia.NumeroGuia)}.pdf";
        string ruta = Path.Combine(FileSystem.CacheDirectory, nombre);
        await File.WriteAllBytesAsync(ruta, pdf);

        await Launcher.OpenAsync(new OpenFileRequest
        {
            Title = "Imprimir Guia Interna",
            File = new ReadOnlyFile(ruta, "application/pdf")
        });
    }

    private static string LimpiarNombreArchivo(string value)
    {
        char[] invalidos = Path.GetInvalidFileNameChars();
        return new string(value.Select(c => invalidos.Contains(c) ? '-' : c).ToArray());
    }

    private static string TextoVacio(string? valor) => string.IsNullOrWhiteSpace(valor) ? "No especificado" : valor.Trim();

    private sealed record GuiaInternaListItem(GuiaInternaResumen Guia, bool EsParcial)
    {
        public string NumeroGuia => Guia.NumeroGuia;
        public string EmpresaDestino => TextoVacio(Guia.EmpresaDestino);
        public DateTime FechaEmision => Guia.FechaEmision;
        public string Estado => Guia.Estado;
        public string NumeroOciTexto => string.IsNullOrWhiteSpace(Guia.NumeroOci) ? "OCI: Manual" : $"OCI: {Guia.NumeroOci}";
        public string OrdenCompraClienteTexto => string.IsNullOrWhiteSpace(Guia.OrdenCompraCliente) ? "OC Cliente: No especificado" : $"OC Cliente: {Guia.OrdenCompraCliente}";
        public string EmisionTexto => Guia.Estado.Equals("Anulada", StringComparison.OrdinalIgnoreCase)
            ? "Anulada"
            : EsParcial ? "Emitida parcial" : "Emitida completa";
        public Color EmisionColor => Guia.Estado.Equals("Anulada", StringComparison.OrdinalIgnoreCase)
            ? Color.FromArgb("#B42318")
            : EsParcial ? Color.FromArgb("#B54708") : Color.FromArgb("#067647");
    }

    private sealed class GuiaManualDetalleItem
    {
        public GuiaManualDetalleItem(ProductoStockBusquedaApi producto, decimal cantidad)
        {
            Producto = producto;
            Cantidad = cantidad;
        }

        public ProductoStockBusquedaApi Producto { get; }
        public decimal Cantidad { get; set; }
    }

    private sealed record ProductoSeleccionItem(ProductoStockBusquedaApi Producto, bool Seleccionado)
    {
        public Color Fondo => Seleccionado ? Color.FromArgb("#EFF6FF") : Colors.White;
        public Color Borde => Seleccionado ? Color.FromArgb("#2563EB") : Color.FromArgb("#D9E0E6");
    }
}
