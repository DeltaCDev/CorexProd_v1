using System.Collections.ObjectModel;
using CorexProd.App.Models;
using CorexProd.App.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CorexProd.App.Pages;

public partial class OciPage : ContentPage
{
    private readonly CorexProdApiClient _apiClient;
    private readonly SessionState _session;
    private readonly ObservableCollection<OciListItem> _items = [];
    private CancellationTokenSource? _searchDelay;

    public OciPage()
    {
        InitializeComponent();
        _apiClient = ServiceHelper.GetRequiredService<CorexProdApiClient>();
        _session = ServiceHelper.GetRequiredService<SessionState>();
        ItemsView.ItemsSource = _items;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_items.Count == 0)
            await LoadAsync();
    }

    private async void OnBuscarClicked(object? sender, EventArgs e) => await LoadAsync();
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

    private async void OnVerDetalleClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not OciListItem item)
            return;

        try
        {
            await MostrarDetalleAsync(item.Item.IdOrdenCompraInterna);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("OCI", ex.Message, "OK");
        }
    }

    private async void OnAnularClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is OciListItem item)
            await AnularAsync(item.Item);
    }

    private async void OnGenerarOtClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not OciListItem item || !item.PuedeGenerarOt)
            return;

        bool confirmar = await DisplayAlertAsync("Generar OT", $"Generar OT para {item.Item.NumeroOci}?", "Generar", "Cancelar");
        if (!confirmar)
            return;

        try
        {
            if (_session.EsDemo)
            {
                await DisplayAlertAsync("OT demo", $"Se generaria una OT para {item.Item.NumeroOci} por falta de stock.", "OK");
                return;
            }

            GenerarOtResponse response = await _apiClient.GenerarOtDesdeOciAsync(
                item.Item.IdOrdenCompraInterna,
                new(_session.Usuario?.NombreUsuario ?? "Android", "OT generada desde Android"));
            await DisplayAlertAsync("OT", response.Mensaje, "OK");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("OT", ex.Message, "OK");
        }
    }

    private async void OnGenerarGuiaClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not OciListItem item || !item.PuedeGenerarGuia)
            return;

        bool confirmar = await DisplayAlertAsync("Guia Interna", $"Generar Guia Interna para {item.Item.NumeroOci}?", "Generar", "Cancelar");
        if (!confirmar)
            return;

        try
        {
            if (_session.EsDemo)
            {
                await DisplayAlertAsync("Guia Interna demo", $"Se generaria la guia interna de {item.Item.NumeroOci} con el stock disponible.", "OK");
                return;
            }

            GenerarGuiaInternaResponse response = await _apiClient.GenerarGuiaInternaDesdeOciAsync(
                item.Item.IdOrdenCompraInterna,
                new(_session.Usuario?.NombreUsuario ?? "Android", "Guia interna generada desde Android"));
            await DisplayAlertAsync("Guia Interna", response.Mensaje, "OK");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Guia Interna", ex.Message, "OK");
        }
    }

    private async Task MostrarDetalleAsync(int idOrdenCompraInterna)
    {
        OciDetalleResponse detalle = _session.EsDemo
            ? DemoData.OciDetalle(idOrdenCompraInterna)
            : await _apiClient.GetOciDetalleAsync(idOrdenCompraInterna);
        await Navigation.PushModalAsync(CrearDetalleDocumentoPage(
            detalle.Cabecera.NumeroOci,
            detalle.Cabecera.NumeroProforma,
            detalle.Cabecera.NombreCliente,
            detalle.Cabecera.OrdenCompraCliente,
            detalle.Cabecera.Estado,
            detalle.Cabecera.Total,
            detalle.Detalles));
    }

    private async Task AnularAsync(OciResumen item)
    {
        string? motivo = await DisplayPromptAsync("Anular OCI", "Motivo de anulacion", "Anular", "Cancelar", "Motivo", maxLength: 200);
        if (string.IsNullOrWhiteSpace(motivo))
            return;

        if (_session.EsDemo)
        {
            await DisplayAlertAsync("OCI demo", $"Se anularia {item.NumeroOci} con motivo: {motivo}", "OK");
            return;
        }

        DocumentoAccionResponse response = await _apiClient.AnularOciAsync(item.IdOrdenCompraInterna, new(_session.Usuario?.NombreUsuario ?? "Android", motivo));
        await DisplayAlertAsync("OCI", response.Mensaje, "OK");
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            Refresh.IsRefreshing = true;
            IReadOnlyList<OciResumen> items = _session.EsDemo
                ? DemoData.Ocis
                : (await _apiClient.GetOciAsync(Search.Text ?? string.Empty)).Items;
            string filtro = Search.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(filtro))
                items = items.Where(x => x.NumeroOci.Contains(filtro, StringComparison.OrdinalIgnoreCase)
                    || x.NumeroProforma.Contains(filtro, StringComparison.OrdinalIgnoreCase)
                    || x.NombreCliente.Contains(filtro, StringComparison.OrdinalIgnoreCase)
                    || x.OrdenCompraCliente.Contains(filtro, StringComparison.OrdinalIgnoreCase)).ToList();
            _items.Clear();
            foreach (OciResumen item in items)
                _items.Add(new OciListItem(item));
            CountLabel.Text = $"{_items.Count} OCI";
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("OCI", ex.Message, "OK");
        }
        finally
        {
            Refresh.IsRefreshing = false;
        }
    }

    private static ContentPage CrearDetalleDocumentoPage(
        string numero,
        string proforma,
        string cliente,
        string ordenCompraCliente,
        string estado,
        decimal total,
        IReadOnlyList<DocumentoDetalle> detalles)
    {
        VerticalStackLayout contenido = new() { Padding = new Thickness(14), Spacing = 12 };
        contenido.Add(new Label { Text = numero, FontFamily = "OpenSansSemibold", FontSize = 22, TextColor = Color.FromArgb("#101828") });
        contenido.Add(new Label { Text = $"Proforma: {proforma}", FontSize = 13, TextColor = Color.FromArgb("#667085") });
        contenido.Add(CrearResumenDocumento(cliente, ordenCompraCliente, estado, total));
        contenido.Add(new Label { Text = "Productos", FontFamily = "OpenSansSemibold", FontSize = 16, TextColor = Color.FromArgb("#101828") });

        foreach (DocumentoDetalle detalle in detalles)
            contenido.Add(CrearProductoDetalle(detalle));

        Button cerrar = new() { Text = "Cerrar", BackgroundColor = Color.FromArgb("#3F1D95"), TextColor = Colors.White };
        cerrar.Clicked += async (_, _) => await Shell.Current.Navigation.PopModalAsync();
        contenido.Add(cerrar);

        return new ContentPage
        {
            Title = numero,
            BackgroundColor = Color.FromArgb("#F4F6F8"),
            Content = new ScrollView { Content = contenido }
        };
    }

    private static Border CrearResumenDocumento(string cliente, string ordenCompraCliente, string estado, decimal total)
    {
        Grid grid = new()
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto)
            },
            RowSpacing = 6
        };

        grid.Add(CrearDato("Cliente", cliente), 0, 0);
        grid.Add(CrearDato("OC Cliente", TextoVacio(ordenCompraCliente)), 0, 1);
        grid.Add(CrearDato("Estado", estado), 0, 2);
        grid.Add(CrearDato("Total", $"S/ {total:N2}"), 0, 3);

        return new Border
        {
            Padding = 12,
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#D9E0E6"),
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Content = grid
        };
    }

    private static Border CrearProductoDetalle(DocumentoDetalle detalle)
    {
        Grid grid = new()
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto)
            },
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            RowSpacing = 4,
            ColumnSpacing = 10
        };

        Label nombre = new() { Text = detalle.NombreProducto, LineBreakMode = LineBreakMode.WordWrap, TextColor = Color.FromArgb("#344054") };
        Label importe = new() { Text = $"Importe: S/ {detalle.Importe:N2}", FontSize = 12, TextColor = Color.FromArgb("#667085") };
        grid.Add(new Label { Text = detalle.CodigoProducto, FontFamily = "OpenSansSemibold", TextColor = Color.FromArgb("#10324A") }, 0, 0);
        grid.Add(new Label { Text = $"x {detalle.Cantidad:N2}", FontFamily = "OpenSansSemibold", TextColor = Color.FromArgb("#067647") }, 1, 0);
        grid.Add(nombre, 0, 1);
        grid.SetColumnSpan(nombre, 2);
        grid.Add(importe, 0, 2);
        grid.SetColumnSpan(importe, 2);

        return new Border
        {
            Padding = 12,
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#D9E0E6"),
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Content = grid
        };
    }

    private static Grid CrearDato(string etiqueta, string valor)
    {
        Grid grid = new()
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(new GridLength(110)),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 8
        };
        grid.Add(new Label { Text = etiqueta, FontSize = 12, TextColor = Color.FromArgb("#667085") }, 0, 0);
        grid.Add(new Label { Text = valor, FontFamily = "OpenSansSemibold", TextColor = Color.FromArgb("#344054"), LineBreakMode = LineBreakMode.WordWrap }, 1, 0);
        return grid;
    }

    private static string TextoVacio(string? valor) => string.IsNullOrWhiteSpace(valor) ? "Sin OC cliente" : valor.Trim();

    private sealed record OciListItem(OciResumen Item)
    {
        public string NumeroOci => Item.NumeroOci;
        public string NumeroProforma => Item.NumeroProforma;
        public DateTime FechaEmision => Item.FechaEmision;
        public string OrdenCompraCliente => string.IsNullOrWhiteSpace(Item.OrdenCompraCliente) ? "Sin OC cliente" : Item.OrdenCompraCliente;
        public string NombreCliente => Item.NombreCliente;
        public decimal Total => Item.Total;
        public string Estado => Item.Estado;
        public bool PuedeGenerarOt => !Item.TieneOrdenTrabajo;
        public bool PuedeGenerarGuia => !Item.TieneGuiaSalida;
        public string OtTexto => Item.TieneOrdenTrabajo ? "OT generada" : "OT";
        public string GuiaTexto => Item.TieneGuiaSalida ? "Guia emitida" : "Guia";
        public Color OtColor => Item.TieneOrdenTrabajo ? Color.FromArgb("#667085") : Color.FromArgb("#7A5AF8");
        public Color GuiaColor => Item.TieneGuiaSalida ? Color.FromArgb("#667085") : Color.FromArgb("#0E9384");
    }
}
