using System.Collections.ObjectModel;
using CorexProd.App.Models;
using CorexProd.App.Services;

namespace CorexProd.App.Pages;

public partial class OrdenesTrabajoPage : ContentPage
{
    private readonly CorexProdApiClient _apiClient;
    private readonly SessionState _session;
    private readonly ObservableCollection<OrdenTrabajoResumen> _ordenes = [];
    private CancellationTokenSource? _searchDelay;
    private IDispatcherTimer? _refreshTimer;
    private bool _isRefreshing;

    public OrdenesTrabajoPage()
    {
        InitializeComponent();
        _apiClient = ServiceHelper.GetRequiredService<CorexProdApiClient>();
        _session = ServiceHelper.GetRequiredService<SessionState>();
        OrdenesView.ItemsSource = _ordenes;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _refreshTimer ??= CrearTimer();
        _refreshTimer.Start();
        await LoadAsync(silencioso: _ordenes.Count > 0);
    }

    protected override void OnDisappearing()
    {
        _refreshTimer?.Stop();
        base.OnDisappearing();
    }

    private async void OnBuscarClicked(object? sender, EventArgs e) => await LoadAsync();
    private async void OnSearchPressed(object? sender, EventArgs e) => await LoadAsync();
    private async void OnRefreshing(object? sender, EventArgs e) => await LoadAsync();

    private IDispatcherTimer CrearTimer()
    {
        IDispatcherTimer timer = Dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromSeconds(15);
        timer.Tick += async (_, _) => await LoadAsync(silencioso: true);
        return timer;
    }

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

    private async void OnOrdenSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not OrdenTrabajoResumen item)
            return;

        OrdenesView.SelectedItem = null;
        await Shell.Current.GoToAsync($"{nameof(OrdenTrabajoDetallePage)}?id={item.IdOrdenTrabajo}");
    }

    private async Task LoadAsync(bool silencioso = false)
    {
        if (_isRefreshing)
            return;

        try
        {
            _isRefreshing = true;
            Refresh.IsRefreshing = true;
            IReadOnlyList<OrdenTrabajoResumen> items = _session.EsDemo
                ? DemoData.OrdenesTrabajo
                : (await _apiClient.GetOrdenesTrabajoAsync(Search.Text ?? string.Empty)).Items;
            string filtro = Search.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(filtro))
                items = items.Where(x => x.NumeroOT.Contains(filtro, StringComparison.OrdinalIgnoreCase)
                    || x.NumeroOci.Contains(filtro, StringComparison.OrdinalIgnoreCase)
                    || x.NombreCliente.Contains(filtro, StringComparison.OrdinalIgnoreCase)
                    || x.Estado.Contains(filtro, StringComparison.OrdinalIgnoreCase)).ToList();
            _ordenes.Clear();
            foreach (OrdenTrabajoResumen item in items)
                _ordenes.Add(item);
            CountLabel.Text = $"{_ordenes.Count} OT | Actualizado {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            if (!silencioso)
                await DisplayAlertAsync("OT Producción", ex.Message, "OK");
        }
        finally
        {
            Refresh.IsRefreshing = false;
            _isRefreshing = false;
        }
    }
}
