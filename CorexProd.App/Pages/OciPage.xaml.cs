using System.Collections.ObjectModel;
using CorexProd.App.Models;
using CorexProd.App.Services;

namespace CorexProd.App.Pages;

public partial class OciPage : ContentPage
{
    private readonly CorexProdApiClient _apiClient;
    private readonly ObservableCollection<OciResumen> _items = [];
    private CancellationTokenSource? _searchDelay;

    public OciPage()
    {
        InitializeComponent();
        _apiClient = ServiceHelper.GetRequiredService<CorexProdApiClient>();
        ItemsView.ItemsSource = _items;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_items.Count == 0)
            await LoadAsync();
    }

    private async void OnBuscarClicked(object? sender, EventArgs e) => await LoadAsync();
    private async void OnSearchPressed(object? sender, EventArgs e) => await LoadAsync();
    private async void OnRefreshing(object? sender, EventArgs e) => await LoadAsync();

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        _searchDelay?.Cancel();
        _searchDelay = new CancellationTokenSource();
        CancellationToken token = _searchDelay.Token;
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(450), async () =>
        {
            if (!token.IsCancellationRequested)
                await LoadAsync();
        });
    }

    private async void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not OciResumen item)
            return;

        ItemsView.SelectedItem = null;
        try
        {
            OciDetalleResponse detalle = await _apiClient.GetOciDetalleAsync(item.IdOrdenCompraInterna);
            string productos = string.Join(Environment.NewLine, detalle.Detalles.Take(8).Select(x => $"{x.CodigoProducto} x {x.Cantidad:N2} - {x.NombreProducto}"));
            await DisplayAlertAsync(
                detalle.Cabecera.NumeroOci,
                $"Cliente: {detalle.Cabecera.NombreCliente}\nEstado: {detalle.Cabecera.Estado}\nTotal: S/ {detalle.Cabecera.Total:N2}\n\n{productos}",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("OCI", ex.Message, "OK");
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            Refresh.IsRefreshing = true;
            ApiListResponse<OciResumen> response = await _apiClient.GetOciAsync(Search.Text ?? string.Empty);
            _items.Clear();
            foreach (OciResumen item in response.Items)
                _items.Add(item);
            CountLabel.Text = $"{response.Total} OCI";
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("OCI", ex.Message, "OK");
        }
        finally
        {
            Refresh.IsRefreshing = false;
        }
    }
}
