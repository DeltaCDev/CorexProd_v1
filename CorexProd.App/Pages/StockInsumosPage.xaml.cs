using System.Collections.ObjectModel;
using CorexProd.App.Models;
using CorexProd.App.Services;

namespace CorexProd.App.Pages;

public partial class StockInsumosPage : ContentPage
{
    private readonly CorexProdApiClient _apiClient;
    private readonly SessionState _session;
    private readonly ObservableCollection<InsumoStock> _insumos = [];
    private CancellationTokenSource? _searchDelay;

    public StockInsumosPage()
    {
        InitializeComponent();
        _apiClient = ServiceHelper.GetRequiredService<CorexProdApiClient>();
        _session = ServiceHelper.GetRequiredService<SessionState>();
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
            IReadOnlyList<InsumoStock> items = _session.EsDemo
                ? DemoData.Insumos
                : (await _apiClient.GetInsumosAsync(Search.Text ?? string.Empty)).Items;
            string filtro = Search.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(filtro))
                items = items.Where(x => x.Codigo.Contains(filtro, StringComparison.OrdinalIgnoreCase)
                    || x.Insumo.Contains(filtro, StringComparison.OrdinalIgnoreCase)
                    || x.Categoria.Contains(filtro, StringComparison.OrdinalIgnoreCase)).ToList();
            _insumos.Clear();
            foreach (InsumoStock item in items)
            {
                _insumos.Add(item);
            }

            CountLabel.Text = $"{_insumos.Count} insumo(s)";
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
