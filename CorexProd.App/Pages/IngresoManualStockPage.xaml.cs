using System.Collections.ObjectModel;
using System.Globalization;
using CorexProd.App.Models;
using CorexProd.App.Services;

namespace CorexProd.App.Pages;

public partial class IngresoManualStockPage : ContentPage
{
    private readonly CorexProdApiClient _apiClient;
    private readonly SessionState _session;
    private readonly ObservableCollection<IngresoManualStockDetalleItem> _detalles = [];

    public IngresoManualStockPage()
    {
        InitializeComponent();
        _apiClient = ServiceHelper.GetRequiredService<CorexProdApiClient>();
        _session = ServiceHelper.GetRequiredService<SessionState>();
        DetalleView.ItemsSource = _detalles;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (ProveedorPicker.ItemsSource == null)
            await CargarCombosAsync();
    }

    private async Task CargarCombosAsync()
    {
        try
        {
            StockManualPrepararResponse data = await _apiClient.GetStockManualPrepararAsync();
            ProveedorPicker.ItemsSource = data.Proveedores.ToList();
            TipoDocumentoPicker.ItemsSource = data.TiposDocumento.ToList();
            AlmacenPicker.ItemsSource = data.Almacenes.ToList();
            ProveedorPicker.ItemDisplayBinding = new Binding(nameof(ProveedorStockApi.NombreRazonSocial));
            TipoDocumentoPicker.ItemDisplayBinding = new Binding(nameof(TipoDocumentoStockApi.NombreTipoDocumento));
            AlmacenPicker.ItemDisplayBinding = new Binding(nameof(AlmacenStockApi.NombreAlmacen));
            ProveedorPicker.SelectedIndex = data.Proveedores.Count > 0 ? 0 : -1;
            TipoDocumentoPicker.SelectedIndex = data.TiposDocumento.Count > 0 ? 0 : -1;
            AlmacenPicker.SelectedIndex = data.Almacenes.Count > 0 ? 0 : -1;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Ingreso manual", ex.Message, "OK");
        }
    }

    private async void OnProductoSearchPressed(object? sender, EventArgs e) => await BuscarProductosAsync();
    private async void OnBuscarProductoClicked(object? sender, EventArgs e) => await BuscarProductosAsync();

    private async void OnAlmacenChanged(object? sender, EventArgs e)
    {
        ProductoPicker.ItemsSource = null;
        if (!string.IsNullOrWhiteSpace(ProductoSearch.Text))
            await BuscarProductosAsync();
    }

    private async Task BuscarProductosAsync()
    {
        if (AlmacenPicker.SelectedItem is not AlmacenStockApi almacen)
        {
            await DisplayAlertAsync("Ingreso manual", "Seleccione un almacen.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(ProductoSearch.Text) || ProductoSearch.Text.Trim().Length < 2)
        {
            await DisplayAlertAsync("Ingreso manual", "Ingrese al menos 2 caracteres para buscar producto.", "OK");
            return;
        }

        ApiListResponse<ProductoStockBusquedaApi> response = await _apiClient.BuscarProductosStockManualAsync(almacen.IdAlmacen, ProductoSearch.Text);
        ProductoPicker.ItemsSource = response.Items.ToList();
        ProductoPicker.SelectedIndex = response.Items.Count > 0 ? 0 : -1;
    }

    private async void OnAgregarClicked(object? sender, EventArgs e)
    {
        if (ProductoPicker.SelectedItem is not ProductoStockBusquedaApi producto)
        {
            await DisplayAlertAsync("Ingreso manual", "Seleccione un producto.", "OK");
            return;
        }

        if (!decimal.TryParse(CantidadEntry.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal cantidad)
            && !decimal.TryParse(CantidadEntry.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out cantidad))
        {
            await DisplayAlertAsync("Ingreso manual", "Ingrese una cantidad valida.", "OK");
            return;
        }

        if (cantidad <= 0)
        {
            await DisplayAlertAsync("Ingreso manual", "La cantidad debe ser mayor a cero.", "OK");
            return;
        }

        IngresoManualStockDetalleItem? existente = _detalles.FirstOrDefault(x => x.Producto.IdProducto == producto.IdProducto);
        if (existente != null)
        {
            existente.Cantidad += cantidad;
        }
        else
        {
            _detalles.Add(new IngresoManualStockDetalleItem(producto, cantidad));
        }

        CantidadEntry.Text = string.Empty;
        ActualizarTotal();
    }

    private async void OnGuardarClicked(object? sender, EventArgs e)
    {
        if (ProveedorPicker.SelectedItem is not ProveedorStockApi proveedor
            || TipoDocumentoPicker.SelectedItem is not TipoDocumentoStockApi tipo
            || AlmacenPicker.SelectedItem is not AlmacenStockApi almacen)
        {
            await DisplayAlertAsync("Ingreso manual", "Seleccione proveedor, tipo de documento y almacen.", "OK");
            return;
        }

        if (_detalles.Count == 0)
        {
            await DisplayAlertAsync("Ingreso manual", "Agregue al menos un producto.", "OK");
            return;
        }

        try
        {
            GuardarButton.IsEnabled = false;
            IngresoManualStockRequest request = new(
                proveedor.IdProveedor,
                tipo.IdTipoDocumento,
                almacen.IdAlmacen,
                ObservacionEditor.Text ?? string.Empty,
                _session.Usuario?.NombreUsuario ?? "Android",
                _detalles.Select(x => new IngresoManualStockDetalleRequest(x.Producto.IdProducto, x.Cantidad)).ToList());

            IngresoManualStockResponse response = await _apiClient.IngresarStockManualAsync(request);
            await DisplayAlertAsync("Ingreso manual", $"{response.Mensaje}\nDocumento: {response.NumeroDocumento}", "OK");
            Limpiar();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Ingreso manual", ex.Message, "OK");
        }
        finally
        {
            GuardarButton.IsEnabled = true;
        }
    }

    private void OnLimpiarClicked(object? sender, EventArgs e) => Limpiar();

    private void Limpiar()
    {
        ProductoSearch.Text = string.Empty;
        ProductoPicker.ItemsSource = null;
        CantidadEntry.Text = string.Empty;
        ObservacionEditor.Text = string.Empty;
        _detalles.Clear();
        ActualizarTotal();
    }

    private void ActualizarTotal()
    {
        TotalLabel.Text = $"{_detalles.Count} producto(s) | {_detalles.Sum(x => x.Cantidad):N2} unidades";
    }

    private sealed class IngresoManualStockDetalleItem
    {
        public IngresoManualStockDetalleItem(ProductoStockBusquedaApi producto, decimal cantidad)
        {
            Producto = producto;
            Cantidad = cantidad;
        }

        public ProductoStockBusquedaApi Producto { get; }
        public decimal Cantidad { get; set; }
    }
}
