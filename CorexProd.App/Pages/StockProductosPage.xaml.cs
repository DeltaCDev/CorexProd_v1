using System.Collections.ObjectModel;
using CorexProd.App.Models;
using CorexProd.App.Services;

namespace CorexProd.App.Pages;

public partial class StockProductosPage : ContentPage
{
    private readonly CorexProdApiClient _apiClient;
    private readonly ObservableCollection<ProductoStock> _productos = [];
    private CancellationTokenSource? _searchDelay;

    public StockProductosPage()
    {
        InitializeComponent();
        _apiClient = ServiceHelper.GetRequiredService<CorexProdApiClient>();
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
            var response = await _apiClient.GetProductosAsync(Search.Text ?? string.Empty);
            _productos.Clear();
            foreach (ProductoStock item in response.Items)
            {
                _productos.Add(item);
            }

            CountLabel.Text = $"{response.Total} producto(s)";
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
}
