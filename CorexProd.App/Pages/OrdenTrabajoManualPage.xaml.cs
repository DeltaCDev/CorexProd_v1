using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using CorexProd.App.Models;
using CorexProd.App.Services;

namespace CorexProd.App.Pages;

public partial class OrdenTrabajoManualPage : ContentPage
{
    private const string MotivoAbastecimiento = "Abastecimiento de Stock";
    private const string MotivoRegularizacion = "Regularizacion de OT";

    private readonly CorexProdApiClient _apiClient;
    private readonly SessionState _session;
    private readonly ObservableCollection<DetalleManualItem> _detalles = [];
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private CancellationTokenSource? _searchDelay;

    public OrdenTrabajoManualPage()
    {
        InitializeComponent();
        _apiClient = ServiceHelper.GetRequiredService<CorexProdApiClient>();
        _session = ServiceHelper.GetRequiredService<SessionState>();
        DetallesView.ItemsSource = _detalles;
        MotivoPicker.ItemsSource = new[] { MotivoAbastecimiento, MotivoRegularizacion };
        MotivoPicker.SelectedIndex = 0;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_session.EstaAutenticado)
        {
            await Shell.Current.GoToAsync("//login");
            return;
        }

        await CargarProductosAsync();
        ActualizarResumen();
    }

    private async void OnRefreshing(object? sender, EventArgs e) => await CargarProductosAsync();
    private async void OnBuscarProducto(object? sender, EventArgs e) => await CargarProductosAsync();

    private void OnBuscarProductoChanged(object? sender, TextChangedEventArgs e)
    {
        _searchDelay?.Cancel();
        _searchDelay = new CancellationTokenSource();
        CancellationToken token = _searchDelay.Token;
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(450), async () =>
        {
            if (!token.IsCancellationRequested)
                await CargarProductosAsync(silencioso: true);
        });
    }

    private void OnMotivoChanged(object? sender, EventArgs e)
    {
        bool regularizacion = EsRegularizacion;
        BuscarProducto.IsEnabled = !regularizacion;
        ProductoPicker.IsEnabled = !regularizacion;
        CantidadEntry.IsEnabled = !regularizacion;
        ObservacionEditor.Placeholder = regularizacion
            ? "Observacion. Para regularizacion, use el boton Regularizar desde una OT Terminado Parcial."
            : "Observacion";
    }

    private async Task CargarProductosAsync(bool silencioso = false)
    {
        if (EsRegularizacion)
        {
            ProductoPicker.ItemsSource = Array.Empty<ProductoPickerItem>();
            Refresh.IsRefreshing = false;
            return;
        }

        try
        {
            Refresh.IsRefreshing = true;
            string buscar = BuscarProducto.Text?.Trim() ?? string.Empty;
            IReadOnlyList<ProductoStock> productos = _session.EsDemo
                ? DemoData.ProductosStock
                : (await _apiClient.GetProductosAsync(buscar)).Items;
            ProductoPicker.ItemsSource = productos.Take(150).Select(x => new ProductoPickerItem(x)).ToList();
            if (ProductoPicker.SelectedIndex < 0 && productos.Count > 0)
                ProductoPicker.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            if (!silencioso)
                await DisplayAlertAsync("OT Manual", ex.Message, "OK");
        }
        finally
        {
            Refresh.IsRefreshing = false;
        }
    }

    private async void OnAgregarClicked(object? sender, EventArgs e)
    {
        if (EsRegularizacion)
        {
            await DisplayAlertAsync("Regularizacion de OT", "Para regularizar, ingrese a OT, seleccione una OT en estado Terminado Parcial y pulse Regularizar. Asi se respetan los pendientes de la OT origen.", "OK");
            return;
        }

        if (ProductoPicker.SelectedItem is not ProductoPickerItem item)
        {
            await DisplayAlertAsync("OT Manual", "Seleccione un producto.", "OK");
            return;
        }

        ProductoStock producto = item.Producto;
        if (!decimal.TryParse(CantidadEntry.Text?.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out decimal cantidad) || cantidad <= 0)
        {
            await DisplayAlertAsync("OT Manual", "Ingrese una cantidad mayor que cero.", "OK");
            return;
        }

        DetalleManualItem? existente = _detalles.FirstOrDefault(x => x.IdProducto == producto.IdProducto);
        if (existente != null)
            existente.Cantidad += cantidad;
        else
            _detalles.Add(new DetalleManualItem(producto, cantidad));

        CantidadEntry.Text = string.Empty;
        ActualizarResumen();
    }

    private void OnQuitarClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is DetalleManualItem item)
            _detalles.Remove(item);
        ActualizarResumen();
    }

    private async void OnGuardarClicked(object? sender, EventArgs e)
    {
        if (EsRegularizacion)
        {
            await Shell.Current.GoToAsync(nameof(OrdenesTrabajoPage));
            return;
        }

        await GuardarAbastecimientoAsync();
    }

    private async Task GuardarAbastecimientoAsync()
    {
        try
        {
            if (_detalles.Count == 0 || _detalles.Any(x => x.Cantidad <= 0))
            {
                await DisplayAlertAsync("OT Manual", "Agregue al menos un producto con cantidad mayor que cero.", "OK");
                return;
            }

            OrdenTrabajoManualRequest request = CrearRequest();
            if (_session.EsDemo)
            {
                await DisplayAlertAsync("OT Manual demo", "Se generaria una OT Manual por Abastecimiento de Stock.", "OK");
                return;
            }

            SetBusy(true);
            OtValidacionManualResponse validacion = await PostAsync<OrdenTrabajoManualRequest, OtValidacionManualResponse>("api/ordenes-trabajo/manual/validar", request);
            string resumenValidacion = CrearResumenValidacion(validacion);
            bool continuar = await DisplayAlertAsync("Validacion de insumos", resumenValidacion, "Generar", "Cancelar");
            if (!continuar)
                return;

            GenerarOtResponse response = await PostAsync<OrdenTrabajoManualRequest, GenerarOtResponse>("api/ordenes-trabajo/manual", request);
            await DisplayAlertAsync("OT Manual", response.Mensaje, "OK");
            _detalles.Clear();
            ObservacionEditor.Text = string.Empty;
            ActualizarResumen();
            await Shell.Current.GoToAsync(nameof(OrdenesTrabajoPage));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("OT Manual", ex.Message, "OK");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private OrdenTrabajoManualRequest CrearRequest()
    {
        string usuario = _session.Usuario?.NombreUsuario ?? "Android";
        return new OrdenTrabajoManualRequest(
            _session.Usuario?.IdUsuario ?? 0,
            usuario,
            MotivoAbastecimiento,
            ObservacionEditor.Text?.Trim() ?? string.Empty,
            _detalles.Select(x => new OrdenTrabajoManualDetalleRequest(x.IdProducto, x.Cantidad)).ToList());
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(string route, TRequest request)
    {
        using HttpClient httpClient = new();
        string baseUrl = _apiClient.BaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("Ingrese manualmente la URL de la API antes de continuar.");

        using HttpResponseMessage response = await httpClient.PostAsJsonAsync($"{baseUrl}/{route}", request, _jsonOptions);
        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync();
            ApiProblem? problem = null;
            if (!string.IsNullOrWhiteSpace(body))
            {
                try { problem = JsonSerializer.Deserialize<ApiProblem>(body, _jsonOptions); }
                catch (JsonException) { }
            }

            throw new InvalidOperationException(problem?.Mensaje ?? problem?.Detail ?? problem?.Title ?? $"La API devolvio HTTP {(int)response.StatusCode}.");
        }

        TResponse? value = await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
        return value ?? throw new InvalidOperationException("La API devolvio una respuesta vacia.");
    }

    private static string CrearResumenValidacion(OtValidacionManualResponse validacion)
    {
        if (validacion.Productos.Count == 0)
            return string.IsNullOrWhiteSpace(validacion.Mensaje) ? "Validacion correcta." : validacion.Mensaje;

        IEnumerable<string> lineas = validacion.Productos.Take(6).Select(x =>
            $"{x.CodigoProducto} - {x.NombreProducto}\nCant.: {x.CantidadRequerida:N2} | Insumos: {x.EstadoInsumos}");
        string extra = validacion.Productos.Count > 6 ? $"\n... y {validacion.Productos.Count - 6} producto(s) mas." : string.Empty;
        return $"{validacion.Mensaje}\n\n{string.Join("\n\n", lineas)}{extra}";
    }

    private void ActualizarResumen()
    {
        decimal total = _detalles.Sum(x => x.Cantidad);
        ResumenLabel.Text = $"{_detalles.Count} producto(s) | Total: {total:N2}";
    }

    private void SetBusy(bool isBusy)
    {
        GuardarButton.IsEnabled = !isBusy;
        GuardarButton.Text = isBusy ? "Procesando..." : EsRegularizacion ? "Ir a Regularizar OT" : "Validar y generar OT";
    }

    private bool EsRegularizacion => MotivoPicker.SelectedItem?.ToString()?.Equals(MotivoRegularizacion, StringComparison.OrdinalIgnoreCase) == true;

    private sealed class ProductoPickerItem(ProductoStock producto)
    {
        public ProductoStock Producto { get; } = producto;
        public string Display => $"{producto.Codigo} | {producto.Producto} | Stock: {producto.StockActual:N2}";
    }

    private sealed class DetalleManualItem(ProductoStock producto, decimal cantidad)
    {
        public int IdProducto { get; } = producto.IdProducto;
        public string Codigo { get; } = producto.Codigo;
        public string NombreProducto { get; } = producto.Producto;
        public decimal Cantidad { get; set; } = cantidad;
        public string ProductoTexto => $"{Codigo} - {NombreProducto}";
        public string CantidadTexto => $"Cantidad: {Cantidad:N2}";
    }

    private sealed record OrdenTrabajoManualRequest(
        int IdUsuario,
        string Usuario,
        string Motivo,
        string Observacion,
        IReadOnlyList<OrdenTrabajoManualDetalleRequest> Detalles);

    private sealed record OrdenTrabajoManualDetalleRequest(int IdProducto, decimal CantidadPlanificada);

    private sealed record OtValidacionManualResponse(
        bool PuedeGenerar,
        string Mensaje,
        IReadOnlyList<OtValidacionProductoManual> Productos);

    private sealed record OtValidacionProductoManual(
        int IdProducto,
        string CodigoProducto,
        string NombreProducto,
        string Observacion,
        decimal CantidadRequerida,
        int? IdFichaTecnica,
        decimal StockAlmacen,
        decimal StockCorte,
        decimal StockConfeccion,
        decimal StockAcabado,
        decimal StockTotal,
        decimal Deficit,
        string EstadoInsumos);
}
