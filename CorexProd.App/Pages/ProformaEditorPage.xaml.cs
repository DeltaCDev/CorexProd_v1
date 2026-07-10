using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using CorexProd.App.Models;
using CorexProd.App.Services;

namespace CorexProd.App.Pages;

public partial class ProformaEditorPage : ContentPage, IQueryAttributable
{
    private readonly CorexProdApiClient _apiClient;
    private readonly SessionState _session;
    private readonly ObservableCollection<ProformaLineaItem> _detalles = [];
    private List<ClienteApi> _clientes = [];
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
        if (_clientes.Count == 0)
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

            _clientes = response.Clientes
                .OrderBy(x => x.NombreRazonSocial)
                .ThenBy(x => x.NumeroDocumento)
                .ToList();
            ClientePicker.ItemsSource = Array.Empty<ClienteApi>();
            ClientePicker.IsVisible = false;
            ClienteAyudaLabel.IsVisible = true;

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

        ClienteApi? cliente = clientes.FirstOrDefault(x =>
            x.NombreRazonSocial.Equals(detalle.Cabecera.NombreCliente, StringComparison.OrdinalIgnoreCase));
        if (cliente != null)
        {
            ClienteSearch.Text = cliente.Display;
            ClientePicker.ItemsSource = new List<ClienteApi> { cliente };
            ClientePicker.SelectedItem = cliente;
            ClientePicker.IsVisible = true;
            ClienteAyudaLabel.IsVisible = false;
        }

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
                item.Observacion,
                copiar));
        }
        ActualizarTotales();

        if (copiar)
            _idOrdenCompraInterna = 0;
    }

    private void OnClienteSearchChanged(object? sender, TextChangedEventArgs e)
    {
        string filtro = (e.NewTextValue ?? string.Empty).Trim();

        if (filtro.Length < 4)
        {
            ClientePicker.SelectedItem = null;
            ClientePicker.ItemsSource = Array.Empty<ClienteApi>();
            ClientePicker.IsVisible = false;
            ClienteAyudaLabel.Text = "Escriba mínimo 4 caracteres para buscar.";
            ClienteAyudaLabel.IsVisible = true;
            return;
        }

        List<ClienteApi> coincidencias = _clientes
            .Where(x =>
                x.NombreRazonSocial.Contains(filtro, StringComparison.OrdinalIgnoreCase)
                || x.NumeroDocumento.Contains(filtro, StringComparison.OrdinalIgnoreCase)
                || x.Display.Contains(filtro, StringComparison.OrdinalIgnoreCase))
            .Take(50)
            .ToList();

        ClientePicker.ItemsSource = coincidencias;
        ClientePicker.IsVisible = coincidencias.Count > 0;
        ClienteAyudaLabel.Text = coincidencias.Count == 0
            ? "No se encontraron clientes."
            : $"{coincidencias.Count} coincidencia(s). Seleccione un cliente.";
        ClienteAyudaLabel.IsVisible = true;

        if (ClientePicker.SelectedItem is ClienteApi seleccionado
            && !coincidencias.Any(x => x.IdCliente == seleccionado.IdCliente))
            ClientePicker.SelectedItem = null;
    }

    private void OnClienteSeleccionado(object? sender, EventArgs e)
    {
        if (ClientePicker.SelectedItem is ClienteApi cliente)
        {
            ClienteSearch.Text = cliente.Display;
            ClienteAyudaLabel.Text = $"Cliente seleccionado: {cliente.NombreRazonSocial}";
            ClienteAyudaLabel.IsVisible = true;
        }
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
            DetalleObservacionEntry.Text ?? string.Empty,
            false));

        CantidadEntry.Text = string.Empty;
        PrecioEntry.Text = string.Empty;
        DescuentoEntry.Text = string.Empty;
        DetalleObservacionEntry.Text = string.Empty;
        ActualizarTotales();
    }

    private void OnCantidadCopiadaChanged(object? sender, TextChangedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not ProformaLineaItem item || !item.EsCantidadEditable)
            return;

        if (LeerDecimal(e.NewTextValue, out decimal cantidad) && cantidad > 0)
        {
            item.ActualizarCantidad(cantidad);
            ActualizarTotales();
        }
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
            await DisplayAlertAsync("OC", "Busque y seleccione un cliente.", "OK");
            return;
        }

        if (_detalles.Count == 0)
        {
            await DisplayAlertAsync("OC", "Agregue al menos un producto.", "OK");
            return;
        }

        if (_detalles.Any(x => x.Cantidad <= 0))
        {
            await DisplayAlertAsync("OC", "Todas las cantidades deben ser mayores a cero.", "OK");
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

    private sealed class ProformaLineaItem : INotifyPropertyChanged
    {
        private decimal _cantidad;
        private decimal _importe;

        public ProformaLineaItem(
            int idProducto,
            string codigoProducto,
            string nombreProducto,
            decimal cantidad,
            decimal precioUnitario,
            decimal descuento,
            decimal importe,
            string observacion,
            bool esCantidadEditable)
        {
            IdProducto = idProducto;
            CodigoProducto = codigoProducto;
            NombreProducto = nombreProducto;
            _cantidad = cantidad;
            PrecioUnitario = precioUnitario;
            Descuento = descuento;
            _importe = importe;
            Observacion = observacion;
            EsCantidadEditable = esCantidadEditable;
        }

        public int IdProducto { get; }
        public string CodigoProducto { get; }
        public string NombreProducto { get; }
        public decimal Cantidad => _cantidad;
        public decimal PrecioUnitario { get; }
        public decimal Descuento { get; }
        public decimal Importe => _importe;
        public string Observacion { get; }
        public bool EsCantidadEditable { get; }
        public string CantidadTexto => Cantidad.ToString("0.##", CultureInfo.InvariantCulture);
        public string CantidadPrecio => $"{Cantidad:N2} x S/ {PrecioUnitario:N2} | Desc. S/ {Descuento:N2}";

        public event PropertyChangedEventHandler? PropertyChanged;

        public void ActualizarCantidad(decimal cantidad)
        {
            _cantidad = cantidad;
            _importe = Math.Max(0, Math.Round((_cantidad * PrecioUnitario) - Descuento, 2));
            OnPropertyChanged(nameof(Cantidad));
            OnPropertyChanged(nameof(CantidadTexto));
            OnPropertyChanged(nameof(Importe));
            OnPropertyChanged(nameof(CantidadPrecio));
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
