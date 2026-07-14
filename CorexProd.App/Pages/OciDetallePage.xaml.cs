using System.Collections.ObjectModel;
using CorexProd.App.Models;
using CorexProd.App.Services;

namespace CorexProd.App.Pages;

[QueryProperty(nameof(IdOrdenCompra), "id")]
public partial class OciDetallePage : ContentPage
{
    private readonly CorexProdApiClient _apiClient;
    private readonly SessionState _session;
    private readonly ObservableCollection<OciDetalleProductoItem> _productos = [];
    private readonly ObservableCollection<OtRelacionadaItem> _otsRelacionadas = [];
    private int _idOrdenCompra;
    private string _numeroOci = string.Empty;
    private string _observacionGeneral = string.Empty;

    public OciDetallePage()
    {
        InitializeComponent();
        _apiClient = ServiceHelper.GetRequiredService<CorexProdApiClient>();
        _session = ServiceHelper.GetRequiredService<SessionState>();
        BindingContext = this;
    }

    public ObservableCollection<OciDetalleProductoItem> Productos => _productos;
    public ObservableCollection<OtRelacionadaItem> OtsRelacionadas => _otsRelacionadas;

    public string IdOrdenCompra
    {
        set => _idOrdenCompra = int.TryParse(value, out int id) ? id : 0;
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
            if (_idOrdenCompra <= 0)
            {
                await DisplayAlertAsync("Orden de Compra", "No se pudo identificar la OC.", "OK");
                return;
            }

            OciDetalleResponse detalle = _session.EsDemo
                ? DemoData.OciDetalle(_idOrdenCompra)
                : await _apiClient.GetOciDetalleAsync(_idOrdenCompra);

            OciCabecera cabecera = detalle.Cabecera;
            _numeroOci = FormatearNumeroOc(cabecera.NumeroOci);
            string ocCliente = TextoVacio(cabecera.OrdenCompraCliente);
            _observacionGeneral = "Sin observaciones generales.";

            Title = _numeroOci;
            OcClienteDestacadoLabel.Text = ocCliente;
            NumeroLabel.Text = _numeroOci;
            ClienteLabel.Text = TextoVacio(cabecera.NombreCliente);
            FechaEmisionLabel.Text = cabecera.FechaEmision.ToString("dd/MM/yyyy");
            FechaEntregaLabel.Text = cabecera.FechaEntrega.ToString("dd/MM/yyyy");
            AplicarTiempoRestante(cabecera.FechaEntrega, cabecera.Estado);
            SubtotalLabel.Text = Moneda(cabecera.Subtotal);
            IgvLabel.Text = Moneda(cabecera.Igv);
            DescuentoLabel.Text = Moneda(cabecera.Descuento);
            TotalLabel.Text = Moneda(cabecera.Total);
            ObservacionGeneralLabel.Text = _observacionGeneral;
            ProductosTituloLabel.Text = $"Productos ({detalle.Detalles.Count})";

            AplicarEstado(cabecera.Estado);

            bool mostrarDisponibilidad = EstadoPermiteDisponibilidad(cabecera.Estado);
            _productos.Clear();
            foreach (DocumentoDetalle item in detalle.Detalles)
                _productos.Add(OciDetalleProductoItem.FromDetalle(item, mostrarDisponibilidad));

            await CargarOtsRelacionadasAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Orden de Compra", ex.Message, "OK");
        }
    }

    private async Task CargarOtsRelacionadasAsync()
    {
        IReadOnlyList<OrdenTrabajoResumen> ordenes = _session.EsDemo
            ? DemoData.OrdenesTrabajo
            : (await _apiClient.GetOrdenesTrabajoAsync(_numeroOci)).Items;

        string numeroNormalizado = NormalizarNumeroDocumento(_numeroOci);
        List<OrdenTrabajoResumen> relacionadas = ordenes
            .Where(x => NormalizarNumeroDocumento(x.NumeroOci) == numeroNormalizado)
            .OrderByDescending(x => x.FechaEmision)
            .ThenByDescending(x => x.IdOrdenTrabajo)
            .ToList();

        _otsRelacionadas.Clear();
        foreach (OrdenTrabajoResumen item in relacionadas)
            _otsRelacionadas.Add(OtRelacionadaItem.FromResumen(item));

        OtsTituloLabel.Text = $"OT relacionadas ({_otsRelacionadas.Count})";
        OtsVacioLabel.IsVisible = _otsRelacionadas.Count == 0;
    }

    private void OnProductosTabClicked(object? sender, EventArgs e) => MostrarTab(mostrarOts: false);

    private void OnOtsTabClicked(object? sender, EventArgs e) => MostrarTab(mostrarOts: true);

    private void MostrarTab(bool mostrarOts)
    {
        ProductosSection.IsVisible = !mostrarOts;
        OtsSection.IsVisible = mostrarOts;

        ProductosTabButton.BackgroundColor = mostrarOts ? Colors.White : Color.FromArgb("#3F1D95");
        ProductosTabButton.BorderColor = mostrarOts ? Color.FromArgb("#D9E0E6") : Color.FromArgb("#3F1D95");
        ProductosTabButton.TextColor = mostrarOts ? Color.FromArgb("#344054") : Colors.White;

        OtsTabButton.BackgroundColor = mostrarOts ? Color.FromArgb("#3F1D95") : Colors.White;
        OtsTabButton.BorderColor = mostrarOts ? Color.FromArgb("#3F1D95") : Color.FromArgb("#D9E0E6");
        OtsTabButton.TextColor = mostrarOts ? Colors.White : Color.FromArgb("#344054");
    }

    private async void OnVerOtClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not OtRelacionadaItem item)
            return;

        await Shell.Current.GoToAsync($"{nameof(OrdenTrabajoDetallePage)}?id={item.IdOrdenTrabajo}");
    }

    private async void OnDescargarPdfClicked(object? sender, EventArgs e)
    {
        await AbrirPdfAsync(compartir: false);
    }

    private async void OnCompartirClicked(object? sender, EventArgs e)
    {
        await AbrirPdfAsync(compartir: true);
    }

    private async Task AbrirPdfAsync(bool compartir)
    {
        try
        {
            if (_session.EsDemo)
            {
                await DisplayAlertAsync("PDF demo", "En modo demo se mostraria el PDF de la OC.", "OK");
                return;
            }

            byte[] pdf = await _apiClient.GetOciPdfAsync(_idOrdenCompra);
            string fileName = string.IsNullOrWhiteSpace(_numeroOci)
                ? $"orden-compra-{_idOrdenCompra}.pdf"
                : $"{_numeroOci}.pdf";
            string path = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllBytesAsync(path, pdf);

            if (compartir)
            {
                await Share.RequestAsync(new ShareFileRequest
                {
                    Title = "Compartir Orden de Compra",
                    File = new ShareFile(path, "application/pdf")
                });
                return;
            }

            await Launcher.OpenAsync(new OpenFileRequest
            {
                Title = "Orden de Compra",
                File = new ReadOnlyFile(path, "application/pdf")
            });
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Orden de Compra", ex.Message, "OK");
        }
    }

    private void AplicarEstado(string estado)
    {
        EstadoLabel.Text = TextoVacio(estado);
        EstadoLabel.TextColor = ObtenerEstadoBadgeColor(estado, EstadoBadgePart.Text);
        EstadoBadge.BackgroundColor = ObtenerEstadoBadgeColor(estado, EstadoBadgePart.Background);
        EstadoBadge.Stroke = ObtenerEstadoBadgeColor(estado, EstadoBadgePart.Stroke);
    }

    private void AplicarTiempoRestante(DateTime fechaEntrega, string estado)
    {
        string normalizado = DocumentoFiltroHelper.Normalizar(estado);
        int dias = (fechaEntrega.Date - DateTime.Today).Days;

        if (normalizado is "ENTREGADO" or "ENTREGADA")
        {
            TiempoRestanteLabel.Text = "Entregada";
            AplicarTiempoRestanteColor("#DCFCE7", "#16A34A", "#166534");
            return;
        }

        if (normalizado is "ANULADO" or "ANULADA")
        {
            TiempoRestanteLabel.Text = "Anulada";
            AplicarTiempoRestanteColor("#FEE2E2", "#DC2626", "#991B1B");
            return;
        }

        if (dias < 0)
        {
            int vencidos = Math.Abs(dias);
            TiempoRestanteLabel.Text = vencidos == 1 ? "Vencida hace 1 dia" : $"Vencida hace {vencidos} dias";
            AplicarTiempoRestanteColor("#FEE2E2", "#DC2626", "#991B1B");
            return;
        }

        if (dias == 0)
        {
            TiempoRestanteLabel.Text = "Entrega hoy";
            AplicarTiempoRestanteColor("#FEF3C7", "#F59E0B", "#92400E");
            return;
        }

        TiempoRestanteLabel.Text = dias == 1 ? "1 dia restante" : $"{dias} dias restantes";
        AplicarTiempoRestanteColor("#DCFCE7", "#16A34A", "#166534");
    }

    private void AplicarTiempoRestanteColor(string background, string stroke, string text)
    {
        TiempoRestanteBadge.BackgroundColor = Color.FromArgb(background);
        TiempoRestanteBadge.Stroke = Color.FromArgb(stroke);
        TiempoRestanteLabel.TextColor = Color.FromArgb(text);
    }

    private static bool EstadoPermiteDisponibilidad(string estado)
    {
        string normalizado = DocumentoFiltroHelper.Normalizar(estado);
        return normalizado is "PENDIENTE" or "EMITIDA" or "EMITIDO" or "EN PROCESO" or "PROCESO" or "PARCIAL";
    }

    private static string FormatearNumeroOc(string numero) =>
        numero.StartsWith("OCI-", StringComparison.OrdinalIgnoreCase)
            ? "OC-" + numero[4..]
            : numero;

    private static string NormalizarNumeroDocumento(string? numero)
    {
        string valor = (numero ?? string.Empty).Trim().ToUpperInvariant();
        return valor.StartsWith("OCI-", StringComparison.Ordinal)
            ? "OC-" + valor[4..]
            : valor;
    }

    private static string TextoVacio(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? "No especificado" : valor.Trim();

    private static string Moneda(decimal valor) => $"S/ {valor:N2}";

    private enum EstadoBadgePart
    {
        Background,
        Stroke,
        Text
    }

    private static Color ObtenerEstadoBadgeColor(string estado, EstadoBadgePart part)
    {
        string normalizado = DocumentoFiltroHelper.Normalizar(estado);
        return normalizado switch
        {
            "PENDIENTE" or "EMITIDA" or "EMITIDO" => part switch
            {
                EstadoBadgePart.Background => Color.FromArgb("#FEF3C7"),
                EstadoBadgePart.Stroke => Color.FromArgb("#F59E0B"),
                _ => Color.FromArgb("#92400E")
            },
            "EN PROCESO" or "PROCESO" => part switch
            {
                EstadoBadgePart.Background => Color.FromArgb("#DBEAFE"),
                EstadoBadgePart.Stroke => Color.FromArgb("#2563EB"),
                _ => Color.FromArgb("#1E3A8A")
            },
            "PARCIAL" => part switch
            {
                EstadoBadgePart.Background => Color.FromArgb("#EDE9FE"),
                EstadoBadgePart.Stroke => Color.FromArgb("#7C3AED"),
                _ => Color.FromArgb("#4C1D95")
            },
            "ENTREGADO" or "ENTREGADA" => part switch
            {
                EstadoBadgePart.Background => Color.FromArgb("#DCFCE7"),
                EstadoBadgePart.Stroke => Color.FromArgb("#16A34A"),
                _ => Color.FromArgb("#166534")
            },
            "ANULADO" or "ANULADA" => part switch
            {
                EstadoBadgePart.Background => Color.FromArgb("#FEE2E2"),
                EstadoBadgePart.Stroke => Color.FromArgb("#DC2626"),
                _ => Color.FromArgb("#991B1B")
            },
            _ => part switch
            {
                EstadoBadgePart.Background => Color.FromArgb("#F1F5F9"),
                EstadoBadgePart.Stroke => Color.FromArgb("#94A3B8"),
                _ => Color.FromArgb("#334155")
            }
        };
    }

    public sealed class OciDetalleProductoItem
    {
        public string CodigoProducto { get; init; } = string.Empty;
        public string NombreProducto { get; init; } = string.Empty;
        public string CantidadTexto { get; init; } = string.Empty;
        public string ObservacionTexto { get; init; } = string.Empty;
        public bool MostrarDisponibilidad { get; init; }
        public bool TieneFaltante { get; init; }
        public double ProgresoDisponibilidad { get; init; }
        public string DisponibilidadResumenTexto { get; init; } = string.Empty;
        public string StockDisponibleTexto { get; init; } = string.Empty;
        public string FaltanteTexto { get; init; } = string.Empty;
        public Color StockIndicadorColor { get; init; } = Color.FromArgb("#22A51B");
        public Color StockTextoColor { get; init; } = Color.FromArgb("#15803D");

        public static OciDetalleProductoItem FromDetalle(DocumentoDetalle detalle, bool estadoPermiteDisponibilidad)
        {
            decimal cantidadDespachada = Math.Max(0, detalle.CantidadDespachada ?? 0);
            decimal cantidadPendiente = Math.Max(0, detalle.Cantidad - cantidadDespachada);
            decimal stockActual = Math.Max(0, detalle.StockActual ?? 0);
            decimal stockDisponible = Math.Min(stockActual, cantidadPendiente);
            decimal faltante = Math.Max(0, cantidadPendiente - stockDisponible);
            bool tieneStock = stockDisponible > 0;
            bool mostrarDisponibilidad = estadoPermiteDisponibilidad && cantidadPendiente > 0;
            double progreso = cantidadPendiente <= 0
                ? 1
                : Math.Clamp((double)(stockDisponible / cantidadPendiente), 0, 1);

            return new OciDetalleProductoItem
            {
                CodigoProducto = TextoVacio(detalle.CodigoProducto),
                NombreProducto = TextoVacio(detalle.NombreProducto),
                CantidadTexto = $"{FormatearCantidad(detalle.Cantidad)} Und",
                ObservacionTexto = string.IsNullOrWhiteSpace(detalle.Observacion)
                    ? "Sin observaciones."
                    : detalle.Observacion.Trim(),
                MostrarDisponibilidad = mostrarDisponibilidad,
                TieneFaltante = faltante > 0,
                ProgresoDisponibilidad = progreso,
                DisponibilidadResumenTexto = $"{FormatearCantidad(stockDisponible)} / {FormatearCantidad(cantidadPendiente)} disponibles",
                StockDisponibleTexto = tieneStock
                    ? $"Stock disponible: {FormatearCantidad(stockDisponible)} Und"
                    : "Sin stock disponible",
                FaltanteTexto = $"Faltan producir/despachar: {FormatearCantidad(faltante)} Und",
                StockIndicadorColor = tieneStock
                    ? Color.FromArgb("#22A51B")
                    : Color.FromArgb("#E11D48"),
                StockTextoColor = tieneStock
                    ? Color.FromArgb("#15803D")
                    : Color.FromArgb("#E11D48")
            };
        }

        private static string FormatearCantidad(decimal cantidad) =>
            cantidad == decimal.Truncate(cantidad)
                ? cantidad.ToString("N0")
                : cantidad.ToString("N2");
    }

    public sealed class OtRelacionadaItem
    {
        public int IdOrdenTrabajo { get; init; }
        public string NumeroOT { get; init; } = string.Empty;
        public string TipoOT { get; init; } = string.Empty;
        public string Estado { get; init; } = string.Empty;
        public string AvanceTexto { get; init; } = string.Empty;
        public string ProductosTexto { get; init; } = string.Empty;
        public Color EstadoBackgroundColor { get; init; } = Color.FromArgb("#F1F5F9");
        public Color EstadoStrokeColor { get; init; } = Color.FromArgb("#94A3B8");
        public Color EstadoTextColor { get; init; } = Color.FromArgb("#334155");

        public static OtRelacionadaItem FromResumen(OrdenTrabajoResumen resumen)
        {
            string estado = TextoVacio(resumen.Estado);
            return new OtRelacionadaItem
            {
                IdOrdenTrabajo = resumen.IdOrdenTrabajo,
                NumeroOT = TextoVacio(resumen.NumeroOT),
                TipoOT = TextoVacio(resumen.TipoOT),
                Estado = estado,
                AvanceTexto = $"{resumen.Avance:N0}% | {resumen.TotalProducido:N2} prod.",
                ProductosTexto = $"{resumen.CantidadProductos} item(s)",
                EstadoBackgroundColor = ObtenerEstadoBadgeColor(estado, EstadoBadgePart.Background),
                EstadoStrokeColor = ObtenerEstadoBadgeColor(estado, EstadoBadgePart.Stroke),
                EstadoTextColor = ObtenerEstadoBadgeColor(estado, EstadoBadgePart.Text)
            };
        }
    }
}
