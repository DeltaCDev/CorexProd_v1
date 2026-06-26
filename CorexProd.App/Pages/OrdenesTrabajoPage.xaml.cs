using System.Collections.ObjectModel;
using CorexProd.App.Models;
using CorexProd.App.Services;

namespace CorexProd.App.Pages;

public partial class OrdenesTrabajoPage : ContentPage
{
    private readonly CorexProdApiClient _apiClient;
    private readonly ObservableCollection<OrdenTrabajoResumen> _ordenes = [];
    private CancellationTokenSource? _searchDelay;
    private IDispatcherTimer? _refreshTimer;
    private bool _isRefreshing;

    public OrdenesTrabajoPage()
    {
        InitializeComponent();
        _apiClient = ServiceHelper.GetRequiredService<CorexProdApiClient>();
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
            ApiListResponse<OrdenTrabajoResumen> response = await _apiClient.GetOrdenesTrabajoAsync(Search.Text ?? string.Empty);
            _ordenes.Clear();
            foreach (OrdenTrabajoResumen item in response.Items)
                _ordenes.Add(item);
            CountLabel.Text = $"{response.Total} OT | Actualizado {DateTime.Now:HH:mm:ss}";
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
