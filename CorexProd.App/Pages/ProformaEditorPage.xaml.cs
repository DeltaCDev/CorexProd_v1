using System.Collections.ObjectModel;
using System.Globalization;
using CorexProd.App.Models;
using CorexProd.App.Services;

namespace CorexProd.App.Pages;

public partial class ProformaEditorPage : ContentPage, IQueryAttributable
{
    private readonly CorexProdApiClient _apiClient;
    private readonly SessionState _session;
    private readonly ObservableCollection<ProformaLineaItem> _detalles = [];
    private List<ProductoProformaApi> _productos = [];
    private bool _guardando;
    private int _idOrdenCompraInterna;
    private string _modo = "nuevo";
    private OciDetalleResponse? _detallePendiente;

    public ProformaEditorPage()
    {
        InitializeComponent();
        _apiClient = ServiceHelper.GetRequiredService<CorexProdApiClient>();
        _session = ServiceHelper.GetRequiredService<SessionState>();
        DetallesView.ItemsSource = _detalles;
        FechaEntregaPicker.Date = DateTime.Today.AddDays(1);
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out object? idValue)
            && int.TryParse(Uri.UnescapeDataString(idValue?.ToString() ?? string.Empty), out int id))
            _idOrdenCompraInterna = id;

        if (query.TryGetValue("modo", out object? modoValue))
            _modo = Uri.UnescapeDataString(modoValue?.ToString() ?? "nuevo").Trim().ToLowerInvariant();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (ClientePicker.ItemsSource == null)
            await CargarPreparacionAsync();
    }

    private async Task CargarPreparacionAsync()
    {
        try
        {
            ProformaPrepararResponse response = await _apiClient.GetOciPrepararAsync();
            NumeroLabel.Text = string.IsNullOrWhiteSpace(response.SiguienteNumero)
                ? "Nueva OC"
                : $"Nueva OC {response.SiguienteNumero}";
            ClientePicker.ItemsSource = response.Clientes.ToList();
            _productos = response.Productos
                .OrderBy(x => ProductoOrdenHelper.CrearClave(x.Codigo, x.NombreProducto).Cliente)
                .ThenBy(x => ProductoOrdenHelper.CrearClave(x.Codigo, x.NombreProducto).NumeroNuloOrden)
                .ThenBy(x => ProductoOrdenHelper.CrearClave(x.Codigo, x.NombreProducto).Numero)
                .ThenBy(x => ProductoOrdenHelper.CrearClave(x.Codigo, x.NombreProducto).Variante)
                .ThenBy(x => ProductoOrdenHelper.CrearClave(x.Codigo, x.NombreProducto).OrdenTalla)
                .ThenBy(x => ProductoOrdenHelper.CrearClave(x.Codigo, x.NombreProducto).TallaNumero)
                .ThenBy(x => ProductoOrdenHelper.CrearClave(x.Codigo, x.NombreProducto).CodigoOrden)
                .ThenBy(x => ProductoOrdenHelper.CrearClave(x.Codigo, x.NombreProducto).NombreProducto)
                .ToList();
            FiltrarProductos();

            if (_idOrdenCompraInterna > 0)
                await CargarOrdenExistenteAsync(response.Clientes, response.SiguienteNumero);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("OC", ex.Message, "OK");
        }
    }

    private async Task CargarOrdenExistenteAsync(IReadOnlyList<ClienteApi> clientes, string siguienteNumero)
    {
        OciDetalleResponse detalle = _detallePendiente ?? await _apiClient.GetOciDetalleAsync(_idOrdenCompraInterna);
        _detallePendiente = detalle;
        bool copiar = _modo == "copiar";

        if (!copiar && !PuedeEditar(detalle))
        {
            await DisplayAlertAsync("OC", "Solo se puede editar una OC pendiente sin OT, guias, anulacion ni acciones realizadas.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        Title = copiar ? "Copiar OC" : "Editar OC";
        NumeroLabel.Text = copiar
            ? $"Copiar como {siguienteNumero}"
            : $"Editar {detalle.Cabecera.NumeroOci}";
        OrdenCompraEntry.Text = detalle.Cabecera.OrdenCompraCliente;
        FechaEntregaPicker.Date = detalle.Cabecera.FechaEntrega == default
            ? detalle.Cabecera.FechaEmision.Date.AddDays(1)
            : detalle.Cabecera.FechaEntrega.Date;

        ClienteApi? cliente = clientes.FirstOrDefault(x => x.NombreRazonSocial.Equals(detalle.Cabecera.NombreCliente, StringComparison.OrdinalIgnoreCase));
        if (cliente != null)
            ClientePicker.SelectedItem = cliente;

        _detalles.Clear();
        foreach (DocumentoDetalle item in detalle.Detalles)
        {
            _detalles.Add(new ProformaLineaItem(
                item.IdProducto,
                item.CodigoProducto,
                item.NombreProducto,
                item.Cantidad,
                item.PrecioUnitario,
                item.Descuento,
                item.Importe,
                item.Observacion));
        }
        ActualizarTotales();

        if (copiar)
            _idOrdenCompraInterna = 0;
    }

    private void OnProductoSearchChanged(object? sender, TextChangedEventArgs e) => FiltrarProductos();

    private void FiltrarProductos()
    {
        string filtro = (ProductoSearch.Text ?? string.Empty).Trim();
        IEnumerable<ProductoProformaApi> productos = _productos;
        if (!string.IsNullOrWhiteSpace(filtro))
        {
            productos = productos.Where(x =>
                x.Codigo.Contains(filtro, StringComparison.OrdinalIgnoreCase)
                || x.NombreProducto.Contains(filtro, StringComparison.OrdinalIgnoreCase)
                || x.EtiquetaCliente.Contains(filtro, StringComparison.OrdinalIgnoreCase));
        }

        ProductoPicker.ItemsSource = productos.Take(80).ToList();
    }

    private async void OnAgregarProductoClicked(object? sender, EventArgs e)
    {
        if (ProductoPicker.SelectedItem is not ProductoProformaApi producto)
        {
            await DisplayAlertAsync("OC", "Seleccione un producto.", "OK");
            return;
        }

        if (!LeerDecimal(CantidadEntry.Text, out decimal cantidad) || cantidad <= 0)
        {
            await DisplayAlertAsync("OC", "Ingrese una cantidad mayor a cero.", "OK");
            return;
        }

        LeerDecimal(PrecioEntry.Text, out decimal precio);
        LeerDecimal(DescuentoEntry.Text, out decimal descuento);
        decimal importe = Math.Max(0, Math.Round((cantidad * precio) - descuento, 2));

        _detalles.Add(new ProformaLineaItem(
            producto.IdProducto,
            producto.Codigo,
            producto.NombreProducto,
            cantidad,
            precio,
            descuento,
            importe,
            DetalleObservacionEntry.Text ?? string.Empty));

        CantidadEntry.Text = string.Empty;
        PrecioEntry.Text = string.Empty;
        DescuentoEntry.Text = string.Empty;
        DetalleObservacionEntry.Text = string.Empty;
        ActualizarTotales();
    }

    private void OnQuitarProductoClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is ProformaLineaItem item)
        {
            _detalles.Remove(item);
            ActualizarTotales();
        }
    }

    private async void OnGuardarClicked(object? sender, EventArgs e)
    {
        if (_guardando)
            return;

        if (ClientePicker.SelectedItem is not ClienteApi cliente)
        {
            await DisplayAlertAsync("OC", "Seleccione un cliente.", "OK");
            return;
        }

        if (_detalles.Count == 0)
        {
            await DisplayAlertAsync("OC", "Agregue al menos un producto.", "OK");
            return;
        }

        DateTime fechaEmision = DateTime.Today;
        DateTime fechaEntrega = (FechaEntregaPicker.Date ?? DateTime.Today.AddDays(1)).Date;
        if (fechaEntrega <= fechaEmision)
        {
            await DisplayAlertAsync("OC", "La fecha de entrega debe ser diferente y posterior a la fecha de emisión.", "OK");
            return;
        }

        try
        {
            _guardando = true;
            ProformaGuardarRequest request = new(
                cliente.IdCliente,
                fechaEntrega,
                fechaEntrega,
                OrdenCompraEntry.Text ?? string.Empty,
                ObservacionEditor.Text ?? string.Empty,
                18,
                "GRAVADO",
                _session.Usuario?.NombreUsuario ?? "Android",
                _detalles.Select(x => new ProformaGuardarDetalleRequest(x.IdProducto, x.Cantidad, x.PrecioUnitario, x.Descuento, x.Observacion)).ToList());

            OciGuardarResponse response = _idOrdenCompraInterna > 0
                ? await _apiClient.ActualizarOciAsync(_idOrdenCompraInterna, request)
                : await _apiClient.GuardarOciAsync(request);
            await DisplayAlertAsync("OC guardada", $"{response.Mensaje}\n{response.NumeroOrden}\nTotal: S/ {response.Total:N2}", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("OC", ex.Message, "OK");
        }
        finally
        {
            _guardando = false;
        }
    }

    private void ActualizarTotales()
    {
        decimal subtotal = _detalles.Sum(x => x.Importe);
        decimal igv = Math.Round(subtotal * 0.18m, 2);
        decimal total = subtotal + igv;
        SubtotalLabel.Text = $"Subtotal: S/ {subtotal:N2} | IGV: S/ {igv:N2}";
        TotalLabel.Text = $"Total: S/ {total:N2}";
    }

    private static bool LeerDecimal(string? texto, out decimal valor)
    {
        texto = (texto ?? string.Empty).Trim().Replace(',', '.');
        return decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out valor);
    }

    private static bool PuedeEditar(OciDetalleResponse detalle)
    {
        string estado = DocumentoFiltroHelper.Normalizar(detalle.Cabecera.Estado);
        return estado is "PENDIENTE" or "EMITIDA" or "EMITIDO"
            && !detalle.Cabecera.TieneGuiaSalida
            && !detalle.Cabecera.TieneOrdenTrabajo
            && string.IsNullOrWhiteSpace(detalle.Cabecera.MotivoAnulacion)
            && !detalle.Cabecera.FechaAnulacion.HasValue
            && detalle.Detalles.All(x => (x.CantidadDespachada ?? 0) <= 0);
    }

    private sealed record ProformaLineaItem(
        int IdProducto,
        string CodigoProducto,
        string NombreProducto,
        decimal Cantidad,
        decimal PrecioUnitario,
        decimal Descuento,
        decimal Importe,
        string Observacion)
    {
        public string CantidadPrecio => $"{Cantidad:N2} x S/ {PrecioUnitario:N2} | Desc. S/ {Descuento:N2}";
    }
}
