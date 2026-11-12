using System.Collections.ObjectModel;
using CorexProd.App.Models;
using CorexProd.App.Services;

namespace CorexProd.App.Pages;

[QueryProperty(nameof(IdOrden), "id")]
public partial class OtAprobacionDetallePage : ContentPage
{
    private readonly CorexProdApiClient _apiClient;
    private readonly SessionState _session;
    private readonly ObservableCollection<OtAprobacionProductoView> _productos = [];
    private int _id;
    private OtAprobacionDetalleResponse? _detalle;
    private bool _cargando;

    public OtAprobacionDetallePage()
    {
        InitializeComponent();
        _apiClient = ServiceHelper.GetRequiredService<CorexProdApiClient>();
        _session = ServiceHelper.GetRequiredService<SessionState>();
        ProductosView.ItemsSource = _productos;
    }

    public string IdOrden
    {
        set => _id = int.TryParse(value, out int id) ? id : 0;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarAsync();
    }

    private async void OnRefreshing(object? sender, EventArgs e) => await CargarAsync();

    private async Task CargarAsync()
    {
        if (_id <= 0 || _cargando) return;
        try
        {
            _cargando = true;
            Refresh.IsRefreshing = true;
            _detalle = await _apiClient.GetOtAprobacionDetalleAsync(_id);
            TituloLabel.Text = _detalle.Cabecera.NumeroOT;
            CabeceraLabel.Text = $"Cliente: {_detalle.Cabecera.NombreCliente} | OCI: {_detalle.Cabecera.NumeroOci} | OC cliente: {_detalle.Cabecera.OrdenCompraCliente}";
            EstadoLabel.Text = _detalle.Cabecera.EstadoAprobacion.Replace('_', ' ');
            _productos.Clear();
            foreach (OtAprobacionProducto item in _detalle.Detalles)
                _productos.Add(new OtAprobacionProductoView(item));
            TotalGeneralLabel.Text = _detalle.Detalles.Sum(x => x.TotalProducir).ToString("N2");
            ObservacionGeneralLabel.Text = string.IsNullOrWhiteSpace(_detalle.Cabecera.Observacion) ? "Sin observación general." : _detalle.Cabecera.Observacion;
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

    private async void OnAprobarClicked(object? sender, EventArgs e)
    {
        if (_detalle == null) return;
        OtAprobacionUsuario? usuario = await SeleccionarUsuarioAsync();
        if (usuario == null) return;
        string? clave = await DisplayPromptAsync("Aprobar OT", $"Contraseña de {usuario.NombreUsuario}", "Continuar", "Cancelar", "Contraseña", keyboard: Keyboard.Text);
        if (string.IsNullOrWhiteSpace(clave)) return;
        bool confirmar = await DisplayAlertAsync("Confirmar aprobación", $"¿Desea aprobar la OT {_detalle.Cabecera.NumeroOT}?", "Sí", "No");
        if (!confirmar) return;
        try
        {
            DocumentoAccionResponse response = await _apiClient.AprobarOtAsync(_id, new OtAprobacionRequest(usuario.NombreUsuario, clave));
            await DisplayAlertAsync("Aprobación", response.Mensaje, "OK");
            await AbrirPdfAsync();
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex) { await DisplayAlertAsync("Aprobación", ex.Message, "OK"); }
    }

    private async Task AbrirPdfAsync()
    {
        try
        {
            await Launcher.Default.OpenAsync(_apiClient.GetOtAprobacionPdfUrl(_id));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("PDF de OT", $"La OT fue aprobada, pero no se pudo abrir el PDF: {ex.Message}", "OK");
        }
    }

    private async void OnRechazarClicked(object? sender, EventArgs e)
    {
        if (_detalle == null) return;
        OtAprobacionUsuario? usuario = await SeleccionarUsuarioAsync();
        if (usuario == null) return;
        string? motivo = await DisplayPromptAsync("Rechazar OT", "Motivo obligatorio", "Continuar", "Cancelar", "Motivo", maxLength: 500);
        if (string.IsNullOrWhiteSpace(motivo)) return;
        string? clave = await DisplayPromptAsync("Rechazar OT", $"Contraseña de {usuario.NombreUsuario}", "Continuar", "Cancelar", "Contraseña", keyboard: Keyboard.Text);
        if (string.IsNullOrWhiteSpace(clave)) return;
        bool confirmar = await DisplayAlertAsync("Confirmar rechazo", $"¿Desea rechazar la OT {_detalle.Cabecera.NumeroOT}?", "Sí", "No");
        if (!confirmar) return;
        try
        {
            DocumentoAccionResponse response = await _apiClient.RechazarOtAsync(_id, new OtAprobacionRequest(usuario.NombreUsuario, clave, motivo));
            await DisplayAlertAsync("Rechazo", response.Mensaje, "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex) { await DisplayAlertAsync("Rechazo", ex.Message, "OK"); }
    }

    private async Task<OtAprobacionUsuario?> SeleccionarUsuarioAsync()
    {
        IReadOnlyList<OtAprobacionUsuario> usuarios = (await _apiClient.GetUsuariosAprobadoresOtAsync()).Items;
        if (usuarios.Count == 0)
        {
            await DisplayAlertAsync("Aprobación", "No existen usuarios autorizados para aprobar OT.", "OK");
            return null;
        }
        string? seleccion = await DisplayActionSheetAsync("Seleccione usuario autorizador", "Cancelar", null, usuarios.Select(x => x.Texto).ToArray());
        return usuarios.FirstOrDefault(x => x.Texto == seleccion);
    }

    private sealed class OtAprobacionProductoView(OtAprobacionProducto item)
    {
        public string ProductoTitulo => $"{item.CodigoProducto} - {item.NombreProducto}";
        public decimal CantidadReqOc => item.CantidadReqOc;
        public decimal StockActual => item.StockActual;
        public decimal CantidadFaltante => item.CantidadFaltante;
        public string AdicionalTotalTexto => $"{item.CantidadAdicionalStock:N2} / {item.TotalProducir:N2}";
        public string ObservacionModeloTexto => string.IsNullOrWhiteSpace(item.ObservacionModelo) ? "Sin observación del modelo." : $"Observación: {item.ObservacionModelo}";
    }
}
