using System.Collections.ObjectModel;
using System.Globalization;
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
    private int _loadVersion;

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

        try
        {
            if (_session.EsDemo)
            {
                await DisplayAlertAsync("OT demo", $"Se generaria una OT para {item.Item.NumeroOci} por falta de stock.", "OK");
                return;
            }

            OtValidacionResponse validacion = await ObtenerValidacionOtAsync(item.Item);
            bool continuar = await MostrarValidacionOtAsync(item.Item, validacion);
            if (!continuar || validacion.Productos.Count == 0)
                return;

            bool confirmar = await MostrarConfirmacionOtAsync(item.Item, validacion);
            if (!confirmar)
                return;

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

        try
        {
            if (_session.EsDemo)
            {
                await DisplayAlertAsync("Guia Interna demo", $"Se revisaria stock, se confirmarian cantidades y se imprimiria la guia interna de {item.Item.NumeroOci}.", "OK");
                return;
            }

            GuiaInternaPrepararResponse preparacion = await ObtenerPreparacionGuiaAsync(item.Item);
            List<GuiaInternaStockItem> stockItems = preparacion.Detalles.Select(GuiaInternaStockItem.FromApi).ToList();
            bool continuar = await MostrarEvaluacionStockAsync(preparacion, stockItems);
            if (!continuar)
                return;

            GuiaInternaConfirmacionResultado? resultado = await MostrarConfirmacionGuiaAsync(preparacion, stockItems);
            if (resultado == null)
                return;

            GenerarGuiaInternaResponse response;
            try
            {
                response = await _apiClient.EmitirGuiaInternaDesdeOciAsync(
                    item.Item.IdOrdenCompraInterna,
                    new GuiaInternaOciRequest(
                        preparacion.Cabecera.IdAlmacen,
                        DateTime.Today,
                        _session.Usuario?.NombreUsuario ?? "Android",
                        _session.Usuario?.NombreUsuario ?? "Android",
                        resultado.Observacion,
                        resultado.Detalles));
            }
            catch (InvalidOperationException ex) when (EsHttp404(ex))
            {
                response = await _apiClient.GenerarGuiaInternaDesdeOciAsync(
                    item.Item.IdOrdenCompraInterna,
                    new(_session.Usuario?.NombreUsuario ?? "Android", resultado.Observacion));
            }

            await DisplayAlertAsync("Guia Interna", response.Mensaje, "OK");
            if (response.IdGuiaInterna > 0)
                await DescargarYAbrirGuiaPdfAsync(response);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Guia Interna", ex.Message, "OK");
        }
    }

    private async Task<OtValidacionResponse> ObtenerValidacionOtAsync(OciResumen oci)
    {
        try
        {
            return await _apiClient.ValidarOtDesdeOciAsync(oci.IdOrdenCompraInterna);
        }
        catch (InvalidOperationException ex) when (EsHttp404(ex))
        {
            OciDetalleResponse detalle = await _apiClient.GetOciDetalleAsync(oci.IdOrdenCompraInterna);
            List<OtValidacionProducto> productos = detalle.Detalles
                .Select(CrearOtValidacionDesdeDetalle)
                .Where(x => x.CantidadRequerida > 0)
                .ToList();

            return new OtValidacionResponse(
                productos.Count > 0,
                productos.Count > 0
                    ? "Validacion local por stock disponible. El servidor no tiene publicada la validacion de insumos."
                    : "No hay productos faltantes para producir.",
                productos);
        }
    }

    private async Task<GuiaInternaPrepararResponse> ObtenerPreparacionGuiaAsync(OciResumen oci)
    {
        try
        {
            return await _apiClient.PrepararGuiaInternaDesdeOciAsync(oci.IdOrdenCompraInterna);
        }
        catch (InvalidOperationException ex) when (EsHttp404(ex))
        {
            OciDetalleResponse detalle = await _apiClient.GetOciDetalleAsync(oci.IdOrdenCompraInterna);
            List<GuiaInternaDetalleApi> detalles = detalle.Detalles
                .Select(CrearGuiaDetalleDesdeDocumento)
                .Where(x => x.CantidadPendiente > 0)
                .ToList();

            GuiaInternaPrepararCabecera cabecera = new(
                "OCI",
                detalle.Cabecera.IdOrdenCompraInterna,
                detalle.Cabecera.NumeroOci,
                detalle.Cabecera.NumeroProforma,
                detalle.Cabecera.OrdenCompraCliente,
                1,
                "Almacen Principal",
                string.Empty,
                string.Empty,
                string.Empty,
                detalle.Cabecera.NombreCliente,
                DateTime.Today);

            return new GuiaInternaPrepararResponse(cabecera, detalles);
        }
    }

    private static OtValidacionProducto CrearOtValidacionDesdeDetalle(DocumentoDetalle detalle)
    {
        decimal pendiente = Math.Max(0, detalle.Cantidad - (detalle.CantidadDespachada ?? 0));
        decimal stock = Math.Max(0, detalle.StockActual ?? 0);
        decimal deficit = Math.Max(0, pendiente - stock);

        return new OtValidacionProducto(
            detalle.IdOrdenCompraInternaDetalle,
            detalle.IdProducto,
            detalle.CodigoProducto,
            detalle.NombreProducto,
            detalle.Observacion,
            deficit,
            0,
            stock,
            0,
            0,
            0,
            stock,
            deficit,
            "Pendiente por validar");
    }

    private static GuiaInternaDetalleApi CrearGuiaDetalleDesdeDocumento(DocumentoDetalle detalle)
    {
        decimal pendiente = Math.Max(0, detalle.Cantidad - (detalle.CantidadDespachada ?? 0));
        decimal stock = Math.Max(0, detalle.StockActual ?? 0);
        decimal sugerida = Math.Min(pendiente, stock);

        return new GuiaInternaDetalleApi(
            detalle.IdOrdenCompraInternaDetalle,
            detalle.IdProducto,
            detalle.CodigoProducto,
            detalle.NombreProducto,
            0,
            string.Empty,
            detalle.Cantidad,
            detalle.CantidadDespachada ?? 0,
            pendiente,
            stock,
            detalle.PrecioUnitario,
            sugerida,
            detalle.Observacion);
    }

    private static bool EsHttp404(Exception ex) =>
        ex.Message.Contains("HTTP 404", StringComparison.OrdinalIgnoreCase);

    private async Task<bool> MostrarEvaluacionStockAsync(GuiaInternaPrepararResponse preparacion, IReadOnlyList<GuiaInternaStockItem> items)
    {
        TaskCompletionSource<bool> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ContentPage page = CrearEvaluacionStockPage(preparacion, items, tcs);
        await Navigation.PushModalAsync(page);
        return await tcs.Task;
    }

    private async Task<GuiaInternaConfirmacionResultado?> MostrarConfirmacionGuiaAsync(GuiaInternaPrepararResponse preparacion, IReadOnlyList<GuiaInternaStockItem> items)
    {
        TaskCompletionSource<GuiaInternaConfirmacionResultado?> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ContentPage page = CrearConfirmacionGuiaPage(preparacion, items, tcs);
        await Navigation.PushModalAsync(page);
        return await tcs.Task;
    }

    private async Task<bool> MostrarValidacionOtAsync(OciResumen oci, OtValidacionResponse validacion)
    {
        TaskCompletionSource<bool> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ContentPage page = CrearValidacionOtPage(oci, validacion, tcs);
        await Navigation.PushModalAsync(page);
        return await tcs.Task;
    }

    private async Task<bool> MostrarConfirmacionOtAsync(OciResumen oci, OtValidacionResponse validacion)
    {
        TaskCompletionSource<bool> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ContentPage page = CrearConfirmacionOtPage(oci, validacion, tcs);
        await Navigation.PushModalAsync(page);
        return await tcs.Task;
    }

    private async Task DescargarYAbrirGuiaPdfAsync(GenerarGuiaInternaResponse response)
    {
        byte[] pdf = await _apiClient.GetGuiaInternaPdfAsync(response.IdGuiaInterna);
        string fileName = string.IsNullOrWhiteSpace(response.NumeroGuia) ? $"guia-interna-{response.IdGuiaInterna}.pdf" : $"{response.NumeroGuia}.pdf";
        string path = System.IO.Path.Combine(FileSystem.CacheDirectory, fileName);
        await File.WriteAllBytesAsync(path, pdf);

        await Launcher.OpenAsync(new OpenFileRequest
        {
            Title = "Imprimir Guia Interna",
            File = new ReadOnlyFile(path, "application/pdf")
        });
    }

    private ContentPage CrearValidacionOtPage(
        OciResumen oci,
        OtValidacionResponse validacion,
        TaskCompletionSource<bool> tcs)
    {
        VerticalStackLayout contenido = new() { Padding = 14, Spacing = 12 };
        contenido.Add(new Label { Text = "Validacion de OT", FontFamily = "OpenSansSemibold", FontSize = 22, TextColor = Color.FromArgb("#101828") });
        contenido.Add(new Label { Text = $"{oci.NumeroOci} | OC Cliente: {TextoVacio(oci.OrdenCompraCliente)}", FontSize = 13, TextColor = Color.FromArgb("#667085") });
        contenido.Add(CrearEstadoGeneralOt(validacion));

        foreach (OtValidacionProducto producto in validacion.Productos)
            contenido.Add(CrearOtProductoCard(producto, true));

        Grid acciones = new()
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 10
        };
        Button cancelar = new() { Text = "Cancelar", BackgroundColor = Color.FromArgb("#667085"), TextColor = Colors.White };
        Button continuar = new() { Text = "Continuar", BackgroundColor = Color.FromArgb("#0E9384"), TextColor = Colors.White, IsEnabled = validacion.Productos.Count > 0 };
        acciones.Add(cancelar, 0, 0);
        acciones.Add(continuar, 1, 0);
        contenido.Add(acciones);

        ContentPage page = new()
        {
            Title = "Validacion OT",
            BackgroundColor = Color.FromArgb("#F4F6F8"),
            Content = new ScrollView { Content = contenido }
        };
        cancelar.Clicked += async (_, _) => await CerrarModalConResultadoAsync(page, tcs, false, cancelar, continuar);
        continuar.Clicked += async (_, _) => await CerrarModalConResultadoAsync(page, tcs, true, cancelar, continuar);
        return page;
    }

    private ContentPage CrearConfirmacionOtPage(
        OciResumen oci,
        OtValidacionResponse validacion,
        TaskCompletionSource<bool> tcs)
    {
        VerticalStackLayout contenido = new() { Padding = 14, Spacing = 12 };
        contenido.Add(new Label { Text = "Confirmar OT", FontFamily = "OpenSansSemibold", FontSize = 22, TextColor = Color.FromArgb("#101828") });
        contenido.Add(new Label { Text = "Revise la ficha tecnica e insumos antes de generar.", FontSize = 13, TextColor = Color.FromArgb("#667085") });
        contenido.Add(CrearResumenDocumento(oci.NombreCliente, oci.OrdenCompraCliente, oci.Estado, oci.Total));

        foreach (OtValidacionProducto producto in validacion.Productos)
            contenido.Add(CrearOtProductoCard(producto, false));

        Grid acciones = new()
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 10
        };
        Button volver = new() { Text = "Volver", BackgroundColor = Color.FromArgb("#667085"), TextColor = Colors.White };
        Button generar = new() { Text = "Generar OT", BackgroundColor = Color.FromArgb("#7A5AF8"), TextColor = Colors.White };
        acciones.Add(volver, 0, 0);
        acciones.Add(generar, 1, 0);
        contenido.Add(acciones);

        ContentPage page = new()
        {
            Title = "Confirmar OT",
            BackgroundColor = Color.FromArgb("#F4F6F8"),
            Content = new ScrollView { Content = contenido }
        };
        volver.Clicked += async (_, _) => await CerrarModalConResultadoAsync(page, tcs, false, volver, generar);
        generar.Clicked += async (_, _) => await CerrarModalConResultadoAsync(page, tcs, true, volver, generar);
        return page;
    }

    private Border CrearOtProductoCard(OtValidacionProducto producto, bool mostrarResumen)
    {
        Color estadoColor = ObtenerOtValidacionColor(producto);
        VerticalStackLayout stack = new() { Spacing = 8 };
        Grid header = new()
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            ColumnSpacing = 10
        };
        header.Add(new Label { Text = producto.CodigoProducto, FontFamily = "OpenSansSemibold", TextColor = Color.FromArgb("#10324A") }, 0, 0);
        header.Add(new Label { Text = ObtenerOtValidacionTexto(producto), FontFamily = "OpenSansSemibold", TextColor = estadoColor }, 1, 0);
        stack.Add(header);
        stack.Add(new Label { Text = producto.NombreProducto, TextColor = Color.FromArgb("#344054"), LineBreakMode = LineBreakMode.WordWrap });
        stack.Add(new Label { Text = $"Cantidad a producir: {producto.CantidadRequerida:N2}", FontFamily = "OpenSansSemibold", TextColor = Color.FromArgb("#101828") });
        stack.Add(new Label
        {
            Text = $"Stock prod.: {producto.StockTotal:N2} | {ObtenerOtValidacionResumen(producto)} | Insumos: {producto.EstadoInsumos}",
            FontSize = 12,
            TextColor = estadoColor,
            LineBreakMode = LineBreakMode.WordWrap
        });

        if (!string.IsNullOrWhiteSpace(producto.Observacion))
            stack.Add(new Label { Text = $"Obs.: {producto.Observacion}", FontSize = 12, TextColor = Color.FromArgb("#667085"), LineBreakMode = LineBreakMode.WordWrap });

        Button detalle = new()
        {
            Text = mostrarResumen ? "Ver detalle ficha" : "Ver insumos",
            BackgroundColor = Color.FromArgb("#2563EB"),
            TextColor = Colors.White
        };
        detalle.Clicked += async (_, _) => await MostrarDetalleInsumosOtAsync(producto);
        stack.Add(detalle);

        return new Border
        {
            Padding = 12,
            BackgroundColor = Colors.White,
            Stroke = estadoColor,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Content = stack
        };
    }

    private async Task MostrarDetalleInsumosOtAsync(OtValidacionProducto producto)
    {
        try
        {
            ApiListResponse<OtValidacionInsumo> response = await _apiClient.GetDetalleInsumosOtAsync(producto.IdOrdenCompraInternaDetalle);
            VerticalStackLayout contenido = new() { Padding = 14, Spacing = 12 };
            contenido.Add(new Label { Text = producto.CodigoProducto, FontFamily = "OpenSansSemibold", FontSize = 21, TextColor = Color.FromArgb("#101828") });
            contenido.Add(new Label { Text = $"Cantidad a producir: {producto.CantidadRequerida:N2}", FontSize = 13, TextColor = Color.FromArgb("#667085") });

            foreach (OtValidacionInsumo insumo in response.Items)
                contenido.Add(CrearInsumoCard(insumo));

            Button cerrar = new() { Text = "Cerrar", BackgroundColor = Color.FromArgb("#3F1D95"), TextColor = Colors.White };
            ContentPage page = new()
            {
                Title = "Ficha tecnica",
                BackgroundColor = Color.FromArgb("#F4F6F8"),
                Content = new ScrollView { Content = contenido }
            };
            cerrar.Clicked += async (_, _) => await page.Navigation.PopModalAsync();
            contenido.Add(cerrar);
            await Navigation.PushModalAsync(page);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Ficha tecnica", ex.Message, "OK");
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
        int loadVersion = Interlocked.Increment(ref _loadVersion);
        string busqueda = Search.Text ?? string.Empty;

        try
        {
            Refresh.IsRefreshing = true;
            IReadOnlyList<OciResumen> items = _session.EsDemo
                ? DemoData.Ocis
                : (await _apiClient.GetOciAsync(busqueda)).Items;
            string filtro = busqueda.Trim();
            if (!string.IsNullOrWhiteSpace(filtro))
                items = items.Where(x => x.NumeroOci.Contains(filtro, StringComparison.OrdinalIgnoreCase)
                    || x.NumeroProforma.Contains(filtro, StringComparison.OrdinalIgnoreCase)
                    || x.NombreCliente.Contains(filtro, StringComparison.OrdinalIgnoreCase)
                    || x.OrdenCompraCliente.Contains(filtro, StringComparison.OrdinalIgnoreCase)).ToList();

            List<OciListItem> nuevosItems = [];
            foreach (OciResumen item in items)
            {
                if (loadVersion != Volatile.Read(ref _loadVersion))
                    return;

                GuiaDisponibilidad disponibilidad = await ObtenerDisponibilidadGuiaAsync(item);
                nuevosItems.Add(new OciListItem(item, disponibilidad.TieneStockDisponible, disponibilidad.TienePendiente, disponibilidad.NecesitaOt));
            }

            if (loadVersion != Volatile.Read(ref _loadVersion))
                return;

            _items.Clear();
            foreach (OciListItem item in nuevosItems)
                _items.Add(item);

            CountLabel.Text = $"{_items.Count} OCI";
        }
        catch (Exception ex)
        {
            if (loadVersion != Volatile.Read(ref _loadVersion))
                return;

            await DisplayAlertAsync("OCI", ex.Message, "OK");
        }
        finally
        {
            if (loadVersion == Volatile.Read(ref _loadVersion))
                Refresh.IsRefreshing = false;
        }
    }

    private async Task<GuiaDisponibilidad> ObtenerDisponibilidadGuiaAsync(OciResumen item)
    {
        if (EsOciAnulada(item.Estado))
            return new(false, false, false);

        try
        {
            if (_session.EsDemo)
            {
                OciDetalleResponse demoDetalle = DemoData.OciDetalle(item.IdOrdenCompraInterna);
                return CalcularDisponibilidadDesdeDetalle(demoDetalle.Detalles);
            }

            OciDetalleResponse detalle = await _apiClient.GetOciDetalleAsync(item.IdOrdenCompraInterna);
            GuiaDisponibilidad desdeDetalle = CalcularDisponibilidadDesdeDetalle(detalle.Detalles);

            if (desdeDetalle.TieneStockDisponible || desdeDetalle.NecesitaOt)
                return desdeDetalle;

            GuiaInternaPrepararResponse preparacion = await _apiClient.PrepararGuiaInternaDesdeOciAsync(item.IdOrdenCompraInterna);
            bool tienePendiente = preparacion.Detalles.Any(x => x.CantidadPendiente > 0);
            bool tieneStock = preparacion.Detalles.Any(x => x.CantidadPendiente > 0 && x.StockActual > 0);
            bool necesitaOt = preparacion.Detalles.Any(x => x.CantidadPendiente > x.StockActual);
            return new(tieneStock, tienePendiente, necesitaOt);
        }
        catch
        {
            return new(item.PuedeGenerarGuiaSalida, item.PuedeGenerarGuiaSalida, item.PuedeGenerarOt);
        }
    }

    private static GuiaDisponibilidad CalcularDisponibilidadDesdeDetalle(IReadOnlyList<DocumentoDetalle> detalles)
    {
        bool tienePendiente = false;
        bool tieneStock = false;
        bool necesitaOt = false;

        foreach (DocumentoDetalle detalle in detalles)
        {
            decimal pendiente = Math.Max(0, detalle.Cantidad - (detalle.CantidadDespachada ?? 0));
            decimal stock = Math.Max(0, detalle.StockActual ?? 0);
            if (pendiente <= 0)
                continue;

            tienePendiente = true;
            if (stock > 0)
                tieneStock = true;
            if (pendiente > stock)
                necesitaOt = true;
        }

        return new(tieneStock, tienePendiente, necesitaOt);
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
        Label disponibilidad = CrearDisponibilidadOciLabel(detalle);
        grid.Add(new Label { Text = detalle.CodigoProducto, FontFamily = "OpenSansSemibold", TextColor = Color.FromArgb("#10324A") }, 0, 0);
        grid.Add(new Label { Text = $"x {detalle.Cantidad:N2}", FontFamily = "OpenSansSemibold", TextColor = Color.FromArgb("#067647") }, 1, 0);
        grid.Add(nombre, 0, 1);
        grid.SetColumnSpan(nombre, 2);
        grid.Add(importe, 0, 2);
        grid.SetColumnSpan(importe, 2);
        grid.Add(disponibilidad, 0, 3);
        grid.SetColumnSpan(disponibilidad, 2);

        return new Border
        {
            Padding = 12,
            BackgroundColor = Colors.White,
            Stroke = ObtenerDisponibilidadOciColor(detalle),
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Content = grid
        };
    }

    private static Label CrearDisponibilidadOciLabel(DocumentoDetalle detalle)
    {
        decimal pendiente = Math.Max(0, detalle.Cantidad - (detalle.CantidadDespachada ?? 0));
        decimal stock = detalle.StockActual ?? 0;
        string texto = pendiente <= 0
            ? "Entregado"
            : stock >= pendiente
                ? $"Stock suficiente para despachar {pendiente:N2}"
                : stock > 0
                    ? $"Stock parcial {stock:N2}; requiere producir {(pendiente - stock):N2}"
                    : $"Sin stock; requiere producir {pendiente:N2}";

        return new Label
        {
            Text = texto,
            FontFamily = "OpenSansSemibold",
            FontSize = 12,
            TextColor = ObtenerDisponibilidadOciColor(detalle),
            LineBreakMode = LineBreakMode.WordWrap
        };
    }

    private static Color ObtenerDisponibilidadOciColor(DocumentoDetalle detalle)
    {
        decimal pendiente = Math.Max(0, detalle.Cantidad - (detalle.CantidadDespachada ?? 0));
        decimal stock = detalle.StockActual ?? 0;
        if (pendiente <= 0)
            return Color.FromArgb("#667085");
        if (stock >= pendiente)
            return Color.FromArgb("#067647");
        if (stock > 0)
            return Color.FromArgb("#B54708");
        return Color.FromArgb("#B42318");
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

    private static ContentPage CrearEvaluacionStockPage(
        GuiaInternaPrepararResponse preparacion,
        IReadOnlyList<GuiaInternaStockItem> items,
        TaskCompletionSource<bool> tcs)
    {
        VerticalStackLayout contenido = new() { Padding = 14, Spacing = 12 };
        contenido.Add(new Label { Text = "Evaluacion de stock", FontFamily = "OpenSansSemibold", FontSize = 22, TextColor = Color.FromArgb("#101828") });
        contenido.Add(new Label { Text = $"{preparacion.Cabecera.NumeroOci} | {preparacion.Cabecera.NombreAlmacen}", FontSize = 13, TextColor = Color.FromArgb("#667085") });
        contenido.Add(CrearResumenStock(items));

        foreach (GuiaInternaStockItem item in items)
            contenido.Add(CrearStockCard(item, false));

        Grid acciones = new()
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 10
        };
        Button cancelar = new() { Text = "Cancelar", BackgroundColor = Color.FromArgb("#667085"), TextColor = Colors.White };
        Button confirmar = new() { Text = "Confirmar", BackgroundColor = Color.FromArgb("#0E9384"), TextColor = Colors.White, IsEnabled = items.Any(x => x.CantidadMaxima > 0) };
        acciones.Add(cancelar, 0, 0);
        acciones.Add(confirmar, 1, 0);
        contenido.Add(acciones);

        ContentPage page = new()
        {
            Title = "Stock",
            BackgroundColor = Color.FromArgb("#F4F6F8"),
            Content = new ScrollView { Content = contenido }
        };
        cancelar.Clicked += async (_, _) => await CerrarModalConResultadoAsync(page, tcs, false, cancelar, confirmar);
        confirmar.Clicked += async (_, _) => await CerrarModalConResultadoAsync(page, tcs, true, cancelar, confirmar);
        return page;
    }

    private static ContentPage CrearConfirmacionGuiaPage(
        GuiaInternaPrepararResponse preparacion,
        IReadOnlyList<GuiaInternaStockItem> stockItems,
        TaskCompletionSource<GuiaInternaConfirmacionResultado?> tcs)
    {
        List<GuiaInternaDespachoItem> items = stockItems
            .Where(x => x.CantidadMaxima > 0)
            .Select(GuiaInternaDespachoItem.FromStock)
            .ToList();

        VerticalStackLayout contenido = new() { Padding = 14, Spacing = 12 };
        contenido.Add(new Label { Text = "Confirmar despacho", FontFamily = "OpenSansSemibold", FontSize = 22, TextColor = Color.FromArgb("#101828") });
        contenido.Add(new Label { Text = "Ajusta la cantidad a despachar antes de emitir la guia.", FontSize = 13, TextColor = Color.FromArgb("#667085") });
        contenido.Add(CrearResumenDocumento(preparacion.Cabecera.EmpresaDestino, preparacion.Cabecera.OrdenCompraCliente, "Borrador", 0));

        foreach (GuiaInternaDespachoItem item in items)
            contenido.Add(CrearDespachoCard(item));

        Entry observacion = new() { Placeholder = "Observacion general", BackgroundColor = Colors.White };
        contenido.Add(observacion);

        Grid acciones = new()
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 10
        };
        Button volver = new() { Text = "Volver", BackgroundColor = Color.FromArgb("#667085"), TextColor = Colors.White };
        Button emitir = new() { Text = "Emitir e imprimir", BackgroundColor = Color.FromArgb("#0E9384"), TextColor = Colors.White };
        ContentPage page = new()
        {
            Title = "Confirmar",
            BackgroundColor = Color.FromArgb("#F4F6F8"),
            Content = new ScrollView { Content = contenido }
        };

        volver.Clicked += async (_, _) => await CerrarModalConResultadoAsync(page, tcs, null, volver, emitir);
        emitir.Clicked += async (_, _) =>
        {
            emitir.IsEnabled = false;
            volver.IsEnabled = false;
            foreach (GuiaInternaDespachoItem item in items)
            {
                if (!TryParseCantidad(item.CantidadTexto, out decimal cantidad))
                {
                    emitir.IsEnabled = true;
                    volver.IsEnabled = true;
                    await page.DisplayAlertAsync("Guia Interna", $"Cantidad invalida en {item.CodigoProducto}.", "OK");
                    return;
                }

                item.CantidadDespachar = cantidad;
                if (cantidad < 0 || cantidad > item.CantidadMaxima)
                {
                    emitir.IsEnabled = true;
                    volver.IsEnabled = true;
                    await page.DisplayAlertAsync("Guia Interna", $"La cantidad maxima para {item.CodigoProducto} es {item.CantidadMaxima:N2}.", "OK");
                    return;
                }
            }

            List<GuiaInternaOciDetalleRequest> detalles = items
                .Where(x => x.CantidadDespachar > 0)
                .Select(x => new GuiaInternaOciDetalleRequest(x.IdOrdenCompraInternaDetalle, x.CantidadDespachar, x.ObservacionTexto))
                .ToList();

            if (detalles.Count == 0)
            {
                emitir.IsEnabled = true;
                volver.IsEnabled = true;
                await page.DisplayAlertAsync("Guia Interna", "Ingrese al menos una cantidad a despachar.", "OK");
                return;
            }

            await CerrarModalConResultadoAsync(
                page,
                tcs,
                new GuiaInternaConfirmacionResultado(observacion.Text ?? string.Empty, detalles),
                volver,
                emitir);
        };
        acciones.Add(volver, 0, 0);
        acciones.Add(emitir, 1, 0);
        contenido.Add(acciones);

        return page;
    }

    private static async Task CerrarModalConResultadoAsync<T>(
        Page page,
        TaskCompletionSource<T> tcs,
        T resultado,
        params Button[] botones)
    {
        foreach (Button boton in botones)
            boton.IsEnabled = false;

        try
        {
            await page.Navigation.PopModalAsync();
            tcs.TrySetResult(resultado);
        }
        catch (Exception ex)
        {
            tcs.TrySetException(ex);
        }
    }

    private static bool TryParseCantidad(string? value, out decimal cantidad)
    {
        string texto = (value ?? string.Empty).Trim();
        return decimal.TryParse(texto, NumberStyles.Number, CultureInfo.CurrentCulture, out cantidad)
            || decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out cantidad);
    }

    private static Border CrearResumenStock(IReadOnlyList<GuiaInternaStockItem> items)
    {
        int completos = items.Count(x => x.Estado == GuiaInternaEstadoStock.Completo);
        int parciales = items.Count(x => x.Estado == GuiaInternaEstadoStock.Parcial);
        int sinStock = items.Count(x => x.Estado == GuiaInternaEstadoStock.SinStock);
        Grid grid = new()
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 8
        };
        grid.Add(CrearMiniResumen("Completos", completos.ToString(), Color.FromArgb("#067647")), 0, 0);
        grid.Add(CrearMiniResumen("Parciales", parciales.ToString(), Color.FromArgb("#B54708")), 1, 0);
        grid.Add(CrearMiniResumen("Sin stock", sinStock.ToString(), Color.FromArgb("#B42318")), 2, 0);
        return new Border { Padding = 12, BackgroundColor = Colors.White, Stroke = Color.FromArgb("#D9E0E6"), StrokeShape = new RoundRectangle { CornerRadius = 8 }, Content = grid };
    }

    private static VerticalStackLayout CrearMiniResumen(string etiqueta, string valor, Color color) => new()
    {
        Children =
        {
            new Label { Text = etiqueta, FontSize = 12, TextColor = Color.FromArgb("#667085") },
            new Label { Text = valor, FontFamily = "OpenSansSemibold", FontSize = 18, TextColor = color }
        }
    };

    private static Border CrearStockCard(GuiaInternaStockItem item, bool compacto)
    {
        Grid grid = new()
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) },
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            RowSpacing = 6,
            ColumnSpacing = 10
        };

        grid.Add(new Label { Text = item.CodigoProducto, FontFamily = "OpenSansSemibold", TextColor = Color.FromArgb("#10324A") }, 0, 0);
        grid.Add(new Label { Text = item.EstadoTexto, FontFamily = "OpenSansSemibold", TextColor = item.EstadoColor }, 1, 0);
        Label nombre = new() { Text = item.NombreProducto, LineBreakMode = LineBreakMode.WordWrap, TextColor = Color.FromArgb("#344054") };
        grid.Add(nombre, 0, 1);
        grid.SetColumnSpan(nombre, 2);
        Label cantidades = new()
        {
            Text = $"Requerido: {item.CantidadPendiente:N2} | Stock: {item.StockActual:N2} | Sugerido: {item.CantidadSugerida:N2}",
            FontSize = compacto ? 11 : 12,
            TextColor = Color.FromArgb("#667085"),
            LineBreakMode = LineBreakMode.WordWrap
        };
        grid.Add(cantidades, 0, 2);
        grid.SetColumnSpan(cantidades, 2);

        return new Border { Padding = 12, BackgroundColor = Colors.White, Stroke = item.EstadoColor, StrokeShape = new RoundRectangle { CornerRadius = 8 }, Content = grid };
    }

    private static Border CrearDespachoCard(GuiaInternaDespachoItem item)
    {
        VerticalStackLayout stack = new() { Spacing = 8 };
        stack.Add(CrearStockCard(item.Stock, true));
        Grid grid = new()
        {
            ColumnDefinitions = { new ColumnDefinition(new GridLength(120)), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 8
        };
        Entry cantidad = new()
        {
            Text = item.CantidadTexto,
            Keyboard = Keyboard.Numeric,
            HorizontalTextAlignment = TextAlignment.End,
            BackgroundColor = Color.FromArgb("#F8FAFC")
        };
        cantidad.TextChanged += (_, e) => item.CantidadTexto = e.NewTextValue ?? string.Empty;
        Entry observacion = new() { Placeholder = "Observacion", BackgroundColor = Color.FromArgb("#F8FAFC") };
        observacion.TextChanged += (_, e) => item.ObservacionTexto = e.NewTextValue ?? string.Empty;
        grid.Add(cantidad, 0, 0);
        grid.Add(observacion, 1, 0);
        stack.Add(new Label { Text = $"Maximo: {item.CantidadMaxima:N2}", FontSize = 12, TextColor = Color.FromArgb("#667085") });
        stack.Add(grid);
        return new Border { Padding = 12, BackgroundColor = Colors.White, Stroke = Color.FromArgb("#D9E0E6"), StrokeShape = new RoundRectangle { CornerRadius = 8 }, Content = stack };
    }

    private static string TextoVacio(string? valor) => string.IsNullOrWhiteSpace(valor) ? "Sin OC cliente" : valor.Trim();

    private static Border CrearEstadoGeneralOt(OtValidacionResponse validacion)
    {
        Color color = validacion.PuedeGenerar ? Color.FromArgb("#067647") : Color.FromArgb("#B42318");
        return new Border
        {
            Padding = 12,
            BackgroundColor = Colors.White,
            Stroke = color,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Content = new Label
            {
                Text = validacion.Mensaje,
                FontFamily = "OpenSansSemibold",
                TextColor = color,
                LineBreakMode = LineBreakMode.WordWrap
            }
        };
    }

    private static string ObtenerOtValidacionTexto(OtValidacionProducto producto)
    {
        return producto.Deficit > 0 ? "Deficit" : "Completo";
    }

    private static Color ObtenerOtValidacionColor(OtValidacionProducto producto)
    {
        return producto.Deficit > 0
            ? Color.FromArgb("#B42318")
            : Color.FromArgb("#067647");
    }

    private static string ObtenerOtValidacionResumen(OtValidacionProducto producto) =>
        producto.Deficit > 0
            ? $"Deficit: {producto.Deficit:N2}"
            : "Completo: stock suficiente";

    private static Border CrearInsumoCard(OtValidacionInsumo insumo)
    {
        Color color = insumo.CantidadFaltante > 0 ? Color.FromArgb("#B42318") : Color.FromArgb("#067647");
        Grid grid = new()
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) },
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            RowSpacing = 5,
            ColumnSpacing = 10
        };
        grid.Add(new Label { Text = insumo.CodigoInsumo, FontFamily = "OpenSansSemibold", TextColor = Color.FromArgb("#10324A") }, 0, 0);
        grid.Add(new Label { Text = insumo.Estado, FontFamily = "OpenSansSemibold", TextColor = color }, 1, 0);
        Label nombre = new() { Text = insumo.NombreInsumo, TextColor = Color.FromArgb("#344054"), LineBreakMode = LineBreakMode.WordWrap };
        grid.Add(nombre, 0, 1);
        grid.SetColumnSpan(nombre, 2);
        Label cantidades = new()
        {
            Text = $"Necesario {insumo.CantidadNecesaria:N3} {insumo.UnidadMedida} | Stock {insumo.StockActual:N3} | Faltante {insumo.CantidadFaltante:N3}",
            FontSize = 12,
            TextColor = Color.FromArgb("#667085"),
            LineBreakMode = LineBreakMode.WordWrap
        };
        grid.Add(cantidades, 0, 2);
        grid.SetColumnSpan(cantidades, 2);
        return new Border { Padding = 12, BackgroundColor = Colors.White, Stroke = color, StrokeShape = new RoundRectangle { CornerRadius = 8 }, Content = grid };
    }

    private sealed record GuiaInternaConfirmacionResultado(
        string Observacion,
        IReadOnlyList<GuiaInternaOciDetalleRequest> Detalles);

    private enum GuiaInternaEstadoStock
    {
        Completo,
        Parcial,
        SinStock
    }

    private sealed record GuiaInternaStockItem(
        int IdOrdenCompraInternaDetalle,
        string CodigoProducto,
        string NombreProducto,
        decimal CantidadPendiente,
        decimal StockActual,
        decimal CantidadSugerida,
        GuiaInternaEstadoStock Estado)
    {
        public decimal CantidadMaxima => Math.Max(0, Math.Min(CantidadPendiente, StockActual));
        public string EstadoTexto => Estado switch
        {
            GuiaInternaEstadoStock.Completo => "Completo",
            GuiaInternaEstadoStock.Parcial => "Despacho parcial",
            _ => "Sin stock"
        };
        public Color EstadoColor => Estado switch
        {
            GuiaInternaEstadoStock.Completo => Color.FromArgb("#067647"),
            GuiaInternaEstadoStock.Parcial => Color.FromArgb("#B54708"),
            _ => Color.FromArgb("#B42318")
        };

        public static GuiaInternaStockItem FromApi(GuiaInternaDetalleApi detalle)
        {
            decimal maximo = Math.Max(0, Math.Min(detalle.CantidadPendiente, detalle.StockActual));
            GuiaInternaEstadoStock estado = maximo <= 0
                ? GuiaInternaEstadoStock.SinStock
                : maximo >= detalle.CantidadPendiente ? GuiaInternaEstadoStock.Completo : GuiaInternaEstadoStock.Parcial;

            return new GuiaInternaStockItem(
                detalle.IdOrdenCompraInternaDetalle,
                detalle.CodigoProducto,
                detalle.NombreProducto,
                detalle.CantidadPendiente,
                detalle.StockActual,
                maximo,
                estado);
        }
    }

    private sealed class GuiaInternaDespachoItem
    {
        public required GuiaInternaStockItem Stock { get; init; }
        public int IdOrdenCompraInternaDetalle => Stock.IdOrdenCompraInternaDetalle;
        public string CodigoProducto => Stock.CodigoProducto;
        public decimal CantidadMaxima => Stock.CantidadMaxima;
        public decimal CantidadDespachar { get; set; }
        public string CantidadTexto { get; set; } = string.Empty;
        public string ObservacionTexto { get; set; } = string.Empty;

        public static GuiaInternaDespachoItem FromStock(GuiaInternaStockItem stock) => new()
        {
            Stock = stock,
            CantidadDespachar = stock.CantidadMaxima,
            CantidadTexto = stock.CantidadMaxima.ToString("N2")
        };
    }

    private static bool EsOciAnulada(string estado) =>
        estado.Equals("Anulada", StringComparison.OrdinalIgnoreCase)
        || estado.Equals("Anulado", StringComparison.OrdinalIgnoreCase);

    private sealed record GuiaDisponibilidad(bool TieneStockDisponible, bool TienePendiente, bool NecesitaOt);

    private sealed record OciListItem(OciResumen Item, bool TieneStockDisponible, bool TienePendiente, bool NecesitaOt)
    {
        public string NumeroOci => Item.NumeroOci;
        public string NumeroProforma => Item.NumeroProforma;
        public DateTime FechaEmision => Item.FechaEmision;
        public string OrdenCompraCliente => string.IsNullOrWhiteSpace(Item.OrdenCompraCliente) ? "Sin OC cliente" : Item.OrdenCompraCliente;
        public string NombreCliente => Item.NombreCliente;
        public decimal Total => Item.Total;
        public string Estado => Item.Estado;
        public bool PuedeGenerarOt => (Item.PuedeGenerarOt || NecesitaOt)
            && !EsOciAnulada(Item.Estado);
        public bool PuedeGenerarGuia => (Item.PuedeGenerarGuiaSalida || TieneStockDisponible)
            && !EsOciAnulada(Item.Estado);
        public string OtTexto => PuedeGenerarOt
            ? Item.TieneOrdenTrabajo ? "OT faltante" : "OT"
            : Item.TieneOtActiva ? "OT en proceso" : Item.TieneOrdenTrabajo ? "OT generada" : "Sin pendiente";
        public string GuiaTexto => PuedeGenerarGuia ? "Guia" : TienePendiente ? "Sin stock" : "Sin pendiente";
        public Color OtColor => PuedeGenerarOt ? Color.FromArgb("#7A5AF8") : Color.FromArgb("#667085");
        public Color GuiaColor => PuedeGenerarGuia ? Color.FromArgb("#0E9384") : Color.FromArgb("#667085");
    }
}
