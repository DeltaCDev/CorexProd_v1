using System.Collections.ObjectModel;
using CorexProd.App.Models;
using CorexProd.App.Services;

namespace CorexProd.App.Pages;

[QueryProperty(nameof(IdIngreso), "id")]
public partial class IngresoManualStockDetallePage : ContentPage
{
    private readonly CorexProdApiClient _apiClient;
    private readonly SessionState _session;
    private readonly ObservableCollection<IngresoManualStockDetalleApi> _detalles = [];
    private int _id;

    public IngresoManualStockDetallePage()
    {
        InitializeComponent();
        _apiClient = ServiceHelper.GetRequiredService<CorexProdApiClient>();
        _session = ServiceHelper.GetRequiredService<SessionState>();
        DetalleView.ItemsSource = _detalles;
    }

    public string IdIngreso
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
            IngresoManualStockDetalleResponse detalle = _session.EsDemo
                ? DemoData.IngresoStockManualDetalle(_id)
                : await _apiClient.GetIngresoStockManualDetalleAsync(_id);

            DocumentoLabel.Text = detalle.Cabecera.NumeroDocumento;
            ProveedorLabel.Text = detalle.Cabecera.NombreProveedor;
            AlmacenLabel.Text = detalle.Cabecera.NombreAlmacen;
            EstadoLabel.Text = detalle.Cabecera.Estado;
            FechaLabel.Text = detalle.Cabecera.FechaEmision.ToString("dd/MM/yyyy");

            _detalles.Clear();
            foreach (IngresoManualStockDetalleApi item in detalle.Detalles)
                _detalles.Add(item);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Detalle ingreso", ex.Message, "OK");
        }
    }
}
