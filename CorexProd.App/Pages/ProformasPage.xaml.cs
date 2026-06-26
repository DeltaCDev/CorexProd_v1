using System.Collections.ObjectModel;
using CorexProd.App.Models;
using CorexProd.App.Services;

namespace CorexProd.App.Pages;

public partial class ProformasPage : ContentPage
{
    private readonly CorexProdApiClient _apiClient;
    private readonly ObservableCollection<ProformaResumen> _items = [];
    private CancellationTokenSource? _searchDelay;

    public ProformasPage()
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
    private async void OnNuevaClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(ProformaEditorPage));
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
        if (e.CurrentSelection.FirstOrDefault() is not ProformaResumen item)
            return;

        ItemsView.SelectedItem = null;
        try
        {
            string generarTexto = item.TieneOrdenCompraInterna ? "OCI ya generada" : "Generar OCI";
            string action = await DisplayActionSheetAsync(item.SerieNumero, "Cancelar", null, "Ver detalle", generarTexto, "Anular");

            if (action == "Ver detalle")
                await MostrarDetalleAsync(item.IdProforma);
            else if (action == "Generar OCI" && !item.TieneOrdenCompraInterna)
                await GenerarOciAsync(item);
            else if (action == "Anular")
                await AnularAsync(item);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Proformas", ex.Message, "OK");
        }
    }

    private async Task MostrarDetalleAsync(int idProforma)
    {
        ProformaDetalleResponse detalle = await _apiClient.GetProformaDetalleAsync(idProforma);
        string productos = string.Join(Environment.NewLine, detalle.Detalles.Take(8).Select(x => $"{x.CodigoProducto} x {x.Cantidad:N2} - {x.NombreProducto}"));
        await DisplayAlertAsync(
            detalle.Cabecera.SerieNumero,
            $"Cliente: {detalle.Cabecera.NombreCliente}\nEstado: {detalle.Cabecera.Estado}\nTotal: S/ {detalle.Cabecera.Total:N2}\n\n{productos}",
            "OK");
    }

    private async Task GenerarOciAsync(ProformaResumen item)
    {
        bool confirmar = await DisplayAlertAsync("Generar OCI", $"Generar OCI desde {item.SerieNumero}?", "Generar", "Cancelar");
        if (!confirmar)
            return;

        SessionState session = ServiceHelper.GetRequiredService<SessionState>();
        DocumentoAccionResponse response = await _apiClient.GenerarOciDesdeProformaAsync(item.IdProforma, new(session.Usuario?.NombreUsuario ?? "Android", string.Empty));
        await DisplayAlertAsync("OCI", response.Mensaje, "OK");
        await LoadAsync();
    }

    private async Task AnularAsync(ProformaResumen item)
    {
        string? motivo = await DisplayPromptAsync("Anular proforma", "Motivo de anulacion", "Anular", "Cancelar", "Motivo", maxLength: 200);
        if (string.IsNullOrWhiteSpace(motivo))
            return;

        SessionState session = ServiceHelper.GetRequiredService<SessionState>();
        DocumentoAccionResponse response = await _apiClient.AnularProformaAsync(item.IdProforma, new(session.Usuario?.NombreUsuario ?? "Android", motivo));
        await DisplayAlertAsync("Proformas", response.Mensaje, "OK");
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            Refresh.IsRefreshing = true;
            ApiListResponse<ProformaResumen> response = await _apiClient.GetProformasAsync(Search.Text ?? string.Empty);
            _items.Clear();
            foreach (ProformaResumen item in response.Items)
                _items.Add(item);
            CountLabel.Text = $"{response.Total} proforma(s)";
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Proformas", ex.Message, "OK");
        }
        finally
        {
            Refresh.IsRefreshing = false;
        }
    }
}
