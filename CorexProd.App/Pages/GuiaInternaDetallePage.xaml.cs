using System.Collections.ObjectModel;
using CorexProd.App.Models;
using CorexProd.App.Services;

namespace CorexProd.App.Pages;

[QueryProperty(nameof(IdGuia), "id")]
public partial class GuiaInternaDetallePage : ContentPage
{
    private readonly CorexProdApiClient _apiClient;
    private readonly SessionState _session;
    private readonly ObservableCollection<GuiaInternaDetalleApi> _detalles = [];
    private int _id;

    public GuiaInternaDetallePage()
    {
        InitializeComponent();
        _apiClient = ServiceHelper.GetRequiredService<CorexProdApiClient>();
        _session = ServiceHelper.GetRequiredService<SessionState>();
        DetalleView.ItemsSource = _detalles;
    }

    public string IdGuia
    {
        set => _id = int.TryParse(value, out int id) ? id : 0;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarAsync();
    }

    private async Task CargarAsync()
    {
        try
        {
            GuiaInternaDetalleResponse detalle = _session.EsDemo
                ? DemoData.GuiaInternaDetalle(_id)
                : await _apiClient.GetGuiaInternaDetalleAsync(_id);

            NumeroLabel.Text = detalle.Cabecera.NumeroGuia;
            OrigenLabel.Text = detalle.Cabecera.Origen;
            OcClienteLabel.Text = TextoVacio(detalle.Cabecera.OrdenCompraCliente);
            DestinoLabel.Text = TextoVacio(detalle.Cabecera.EmpresaDestino);
            AlmacenLabel.Text = detalle.Cabecera.NombreAlmacen;
            EstadoLabel.Text = detalle.Cabecera.Estado;

            _detalles.Clear();
            foreach (GuiaInternaDetalleApi item in detalle.Detalles)
                _detalles.Add(item);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Detalle guia", ex.Message, "OK");
        }
    }

    private static string TextoVacio(string? valor) => string.IsNullOrWhiteSpace(valor) ? "No especificado" : valor.Trim();
}
