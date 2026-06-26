using System.Collections.ObjectModel;
using CorexProd.App.Models;
using CorexProd.App.Services;

namespace CorexProd.App.Pages;

public partial class StockInsumosPage : ContentPage
{
    private readonly CorexProdApiClient _apiClient;
    private readonly ObservableCollection<InsumoStock> _insumos = [];
    private CancellationTokenSource? _searchDelay;

    public StockInsumosPage()
    {
        InitializeComponent();
        _apiClient = ServiceHelper.GetRequiredService<CorexProdApiClient>();
        ItemsView.ItemsSource = _insumos;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_insumos.Count == 0)
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
            var response = await _apiClient.GetInsumosAsync(Search.Text ?? string.Empty);
            _insumos.Clear();
            foreach (InsumoStock item in response.Items)
            {
                _insumos.Add(item);
            }

            CountLabel.Text = $"{response.Total} insumo(s)";
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Stock insumos", ex.Message, "OK");
        }
        finally
        {
            Refresh.IsRefreshing = false;
        }
    }
}
