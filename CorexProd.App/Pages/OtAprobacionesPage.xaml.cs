using System.Collections.ObjectModel;
using CorexProd.App.Models;
using CorexProd.App.Services;

namespace CorexProd.App.Pages;

public partial class OtAprobacionesPage : ContentPage
{
    private readonly CorexProdApiClient _apiClient;
    private readonly SessionState _session;
    private readonly ObservableCollection<OtAprobacionResumen> _items = [];
    private bool _cargando;

    public OtAprobacionesPage()
    {
        InitializeComponent();
        _apiClient = ServiceHelper.GetRequiredService<CorexProdApiClient>();
        _session = ServiceHelper.GetRequiredService<SessionState>();
        OrdenesView.ItemsSource = _items;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarAsync();
    }

    private async void OnRefreshing(object? sender, EventArgs e) => await CargarAsync();

    private async void OnRevisarClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not OtAprobacionResumen item)
            return;
        await Shell.Current.GoToAsync($"{nameof(OtAprobacionDetallePage)}?id={item.IdOrdenTrabajo}");
    }

    private async Task CargarAsync()
    {
        if (_cargando) return;
        try
        {
            _cargando = true;
            Refresh.IsRefreshing = true;
            IReadOnlyList<OtAprobacionResumen> items = _session.EsDemo
                ? []
                : (await _apiClient.GetOtAprobacionesAsync()).Items;
            _items.Clear();
            foreach (OtAprobacionResumen item in items)
                _items.Add(item);
            ContadorLabel.Text = $"{_items.Count} OT pendiente(s) de aprobación";
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Aprobación de OT", ex.Message, "OK");
        }
        finally
        {
            Refresh.IsRefreshing = false;
            _cargando = false;
        }
    }
}
