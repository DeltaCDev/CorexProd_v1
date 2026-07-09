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

            Title = string.IsNullOrWhiteSpace(cabecera.OrdenCompraCliente) ? _numeroOci : ocCliente;
            OcClienteDestacadoLabel.Text = ocCliente;
            NumeroLabel.Text = $"OC interna: {_numeroOci}";
            FechaCreacionLabel.Text = $"Creada el {cabecera.FechaEmision:dd/MM/yyyy}";
            ClienteLabel.Text = TextoVacio(cabecera.NombreCliente);
            FechaEmisionLabel.Text = cabecera.FechaEmision.ToString("dd/MM/yyyy");
            SubtotalLabel.Text = Moneda(cabecera.Subtotal);
            IgvLabel.Text = Moneda(cabecera.Igv);
            DescuentoLabel.Text = Moneda(cabecera.Descuento);
            TotalLabel.Text = Moneda(cabecera.Total);
            ObservacionGeneralLabel.Text = _observacionGeneral;
            ProductosTituloLabel.Text = $"Productos ({detalle.Detalles.Count})";

            AplicarEstado(cabecera.Estado);

            _productos.Clear();
            foreach (DocumentoDetalle item in detalle.Detalles)
                _productos.Add(OciDetalleProductoItem.FromDetalle(item));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Orden de Compra", ex.Message, "OK");
        }
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
            string fileName = string.IsNullOrWhiteSpace(_numeroOci) ? $"orden-compra-{_idOrdenCompra}.pdf" : $"{_numeroOci}.pdf";
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

    private static string FormatearNumeroOc(string numero) =>
        numero.StartsWith("OCI-", StringComparison.OrdinalIgnoreCase)
            ? "OC-" + numero[4..]
            : numero;

    private static string TextoVacio(string? valor) => string.IsNullOrWhiteSpace(valor) ? "No especificado" : valor.Trim();
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
        public string TotalTexto { get; init; } = string.Empty;
        public string ObservacionTexto { get; init; } = string.Empty;

        public static OciDetalleProductoItem FromDetalle(DocumentoDetalle detalle) => new()
        {
            CodigoProducto = TextoVacio(detalle.CodigoProducto),
            NombreProducto = TextoVacio(detalle.NombreProducto).ToUpperInvariant(),
            CantidadTexto = $"{FormatearCantidad(detalle.Cantidad)} Unidades",
            TotalTexto = Moneda(detalle.Importe),
            ObservacionTexto = string.IsNullOrWhiteSpace(detalle.Observacion) ? "Sin observaciones." : detalle.Observacion.Trim()
        };

        private static string FormatearCantidad(decimal cantidad) =>
            cantidad == decimal.Truncate(cantidad)
                ? cantidad.ToString("N0")
                : cantidad.ToString("N2");
    }
}
