using System.Collections.ObjectModel;
using System.Globalization;
using CorexProd.App.Models;
using CorexProd.App.Services;

namespace CorexProd.App.Pages;

public partial class ProformaEditorPage : ContentPage
{
    private readonly CorexProdApiClient _apiClient;
    private readonly SessionState _session;
    private readonly ObservableCollection<ProformaLineaItem> _detalles = [];
    private List<ProductoProformaApi> _productos = [];
    private bool _guardando;

    public ProformaEditorPage()
    {
        InitializeComponent();
        _apiClient = ServiceHelper.GetRequiredService<CorexProdApiClient>();
        _session = ServiceHelper.GetRequiredService<SessionState>();
        DetallesView.ItemsSource = _detalles;
        VencimientoPicker.Date = DateTime.Today.AddDays(7);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (ClientePicker.ItemsSource == null)
            await CargarPreparacionAsync();
    }

    private async Task CargarPreparacionAsync()
    {
        try
        {
            ProformaPrepararResponse response = await _apiClient.GetProformaPrepararAsync();
            NumeroLabel.Text = $"Proforma {response.SiguienteNumero}";
            ClientePicker.ItemsSource = response.Clientes.ToList();
            _productos = response.Productos
                .OrderBy(x => ProductoOrdenHelper.CrearClave(x.Codigo, x.NombreProducto).Cliente)
                .ThenBy(x => ProductoOrdenHelper.CrearClave(x.Codigo, x.NombreProducto).NumeroNuloOrden)
                .ThenBy(x => ProductoOrdenHelper.CrearClave(x.Codigo, x.NombreProducto).Numero)
                .ThenBy(x => ProductoOrdenHelper.CrearClave(x.Codigo, x.NombreProducto).Variante)
                .ThenBy(x => ProductoOrdenHelper.CrearClave(x.Codigo, x.NombreProducto).OrdenTalla)
                .ThenBy(x => ProductoOrdenHelper.CrearClave(x.Codigo, x.NombreProducto).TallaNumero)
                .ThenBy(x => ProductoOrdenHelper.CrearClave(x.Codigo, x.NombreProducto).CodigoOrden)
                .ThenBy(x => ProductoOrdenHelper.CrearClave(x.Codigo, x.NombreProducto).NombreProducto)
                .ToList();
            FiltrarProductos();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Proformas", ex.Message, "OK");
        }
    }

    private void OnProductoSearchChanged(object? sender, TextChangedEventArgs e) => FiltrarProductos();

    private void FiltrarProductos()
    {
        string filtro = (ProductoSearch.Text ?? string.Empty).Trim();
        IEnumerable<ProductoProformaApi> productos = _productos;
        if (!string.IsNullOrWhiteSpace(filtro))
        {
            productos = productos.Where(x =>
                x.Codigo.Contains(filtro, StringComparison.OrdinalIgnoreCase)
                || x.NombreProducto.Contains(filtro, StringComparison.OrdinalIgnoreCase)
                || x.EtiquetaCliente.Contains(filtro, StringComparison.OrdinalIgnoreCase));
        }

        ProductoPicker.ItemsSource = productos.Take(80).ToList();
    }

    private async void OnAgregarProductoClicked(object? sender, EventArgs e)
    {
        if (ProductoPicker.SelectedItem is not ProductoProformaApi producto)
        {
            await DisplayAlertAsync("Proformas", "Seleccione un producto.", "OK");
            return;
        }

        if (!LeerDecimal(CantidadEntry.Text, out decimal cantidad) || cantidad <= 0)
        {
            await DisplayAlertAsync("Proformas", "Ingrese una cantidad mayor a cero.", "OK");
            return;
        }

        LeerDecimal(PrecioEntry.Text, out decimal precio);
        LeerDecimal(DescuentoEntry.Text, out decimal descuento);
        decimal importe = Math.Max(0, Math.Round((cantidad * precio) - descuento, 2));

        _detalles.Add(new ProformaLineaItem(
            producto.IdProducto,
            producto.Codigo,
            producto.NombreProducto,
            cantidad,
            precio,
            descuento,
            importe,
            DetalleObservacionEntry.Text ?? string.Empty));

        CantidadEntry.Text = string.Empty;
        PrecioEntry.Text = string.Empty;
        DescuentoEntry.Text = string.Empty;
        DetalleObservacionEntry.Text = string.Empty;
        ActualizarTotales();
    }

    private void OnQuitarProductoClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is ProformaLineaItem item)
        {
            _detalles.Remove(item);
            ActualizarTotales();
        }
    }

    private async void OnGuardarClicked(object? sender, EventArgs e)
    {
        if (_guardando)
            return;

        if (ClientePicker.SelectedItem is not ClienteApi cliente)
        {
            await DisplayAlertAsync("Proformas", "Seleccione un cliente.", "OK");
            return;
        }

        if (_detalles.Count == 0)
        {
            await DisplayAlertAsync("Proformas", "Agregue al menos un producto.", "OK");
            return;
        }

        try
        {
            _guardando = true;
            ProformaGuardarRequest request = new(
                cliente.IdCliente,
                VencimientoPicker.Date ?? DateTime.Today.AddDays(7),
                OrdenCompraEntry.Text ?? string.Empty,
                ObservacionEditor.Text ?? string.Empty,
                18,
                "GRAVADO",
                _session.Usuario?.NombreUsuario ?? "Android",
                _detalles.Select(x => new ProformaGuardarDetalleRequest(x.IdProducto, x.Cantidad, x.PrecioUnitario, x.Descuento, x.Observacion)).ToList());

            ProformaGuardarResponse response = await _apiClient.GuardarProformaAsync(request);
            await DisplayAlertAsync("Proforma guardada", $"{response.Mensaje}\n{response.SerieNumero}\nTotal: S/ {response.Total:N2}", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Proformas", ex.Message, "OK");
        }
        finally
        {
            _guardando = false;
        }
    }

    private void ActualizarTotales()
    {
        decimal subtotal = _detalles.Sum(x => x.Importe);
        decimal igv = Math.Round(subtotal * 0.18m, 2);
        decimal total = subtotal + igv;
        SubtotalLabel.Text = $"Subtotal: S/ {subtotal:N2} | IGV: S/ {igv:N2}";
        TotalLabel.Text = $"Total: S/ {total:N2}";
    }

    private static bool LeerDecimal(string? texto, out decimal valor)
    {
        texto = (texto ?? string.Empty).Trim().Replace(',', '.');
        return decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out valor);
    }

    private sealed record ProformaLineaItem(
        int IdProducto,
        string CodigoProducto,
        string NombreProducto,
        decimal Cantidad,
        decimal PrecioUnitario,
        decimal Descuento,
        decimal Importe,
        string Observacion)
    {
        public string CantidadPrecio => $"{Cantidad:N2} x S/ {PrecioUnitario:N2} | Desc. S/ {Descuento:N2}";
    }
}
