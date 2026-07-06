using System.Collections.ObjectModel;
using CorexProd.App.Models;
using CorexProd.App.Services;

namespace CorexProd.App.Pages;

public partial class OrdenesTrabajoPage : ContentPage
{
    private readonly CorexProdApiClient _apiClient;
    private readonly SessionState _session;
    private readonly ObservableCollection<OrdenTrabajoListItem> _ordenes = [];
    private CancellationTokenSource? _searchDelay;
    private IDispatcherTimer? _refreshTimer;
    private bool _isRefreshing;
    private bool _filtroPredeterminado = true;
    private bool _inicializandoFiltros;

    public OrdenesTrabajoPage()
    {
        InitializeComponent();
        _apiClient = ServiceHelper.GetRequiredService<CorexProdApiClient>();
        _session = ServiceHelper.GetRequiredService<SessionState>();
        OrdenesView.ItemsSource = _ordenes;
        InicializarFiltros();
    }

    private void InicializarFiltros()
    {
        _inicializandoFiltros = true;
        FechaDesde.Date = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        FechaHasta.Date = DateTime.Today;
        EstadoPicker.ItemsSource = new[] { "Todos", "Pendiente", "En Proceso", "Terminado", "Terminado Parcial", "Anulado" };
        EstadoPicker.SelectedIndex = 0;
        _inicializandoFiltros = false;
    }

    private async void OnFilterChanged(object? sender, EventArgs e)
    {
        if (_inicializandoFiltros) return;
        _filtroPredeterminado = false;
        await LoadAsync();
    }

    private void OnFilterTextChanged(object? sender, TextChangedEventArgs e) => OnSearchTextChanged(sender, e);

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _refreshTimer ??= CrearTimer();
        _refreshTimer.Start();
        await LoadAsync(silencioso: _ordenes.Count > 0);
    }

    protected override void OnDisappearing()
    {
        _refreshTimer?.Stop();
        base.OnDisappearing();
    }

    private async void OnBuscarClicked(object? sender, EventArgs e) => await LoadAsync();
    private async void OnSearchPressed(object? sender, EventArgs e) => await LoadAsync();
    private async void OnRefreshing(object? sender, EventArgs e) => await LoadAsync();

    private IDispatcherTimer CrearTimer()
    {
        IDispatcherTimer timer = Dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromSeconds(15);
        timer.Tick += async (_, _) => await LoadAsync(silencioso: true);
        return timer;
    }

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

    private async void OnVerClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not OrdenTrabajoListItem item)
            return;

        await Shell.Current.GoToAsync($"{nameof(OrdenTrabajoDetallePage)}?id={item.Item.IdOrdenTrabajo}");
    }

    private async void OnAnularClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not OrdenTrabajoListItem item)
            return;

        await AnularOrdenTrabajoAsync(item.Item);
    }

    private async void OnRegularizarClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not OrdenTrabajoListItem item)
            return;

        await GenerarRegularizacionAsync(item.Item);
    }

    private async void OnAnulacionInfoClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not OrdenTrabajoListItem item)
            return;

        await DisplayAlertAsync("Anulacion", item.DetalleAnulacion, "OK");
    }

    private async Task LoadAsync(bool silencioso = false)
    {
        if (_isRefreshing)
            return;

        try
        {
            _isRefreshing = true;
            Refresh.IsRefreshing = true;
            IReadOnlyList<OrdenTrabajoResumen> items = _session.EsDemo
                ? DemoData.OrdenesTrabajo
                : (await _apiClient.GetOrdenesTrabajoAsync(Search.Text ?? string.Empty)).Items;
            HashSet<int> ordenesRegularizadas = items
                .Where(x => x.IdOrdenTrabajoRelacionada.HasValue
                    && DocumentoFiltroHelper.Normalizar(x.Estado) is not ("ANULADO" or "ANULADA"))
                .Select(x => x.IdOrdenTrabajoRelacionada!.Value)
                .ToHashSet();
            string filtro = Search.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(filtro))
                items = items.Where(x => x.NumeroOT.Contains(filtro, StringComparison.OrdinalIgnoreCase)
                    || x.NumeroOci.Contains(filtro, StringComparison.OrdinalIgnoreCase)
                    || x.NombreCliente.Contains(filtro, StringComparison.OrdinalIgnoreCase)
                    || x.Estado.Contains(filtro, StringComparison.OrdinalIgnoreCase)).ToList();
            string estadoFiltro = DocumentoFiltroHelper.Normalizar(EstadoPicker.SelectedItem?.ToString());
            items = items.Where(x =>
                DocumentoFiltroHelper.CoincideTexto(x.NombreCliente, ClienteFilter.Text)
                && CoincideEstadoOt(x.Estado, estadoFiltro)
                && DocumentoFiltroHelper.CoincideFecha(x.FechaEmision, x.FechaCierre, FechaDesde.Date, FechaHasta.Date, _filtroPredeterminado, EsOtActiva(x.Estado))).ToList();
            _ordenes.Clear();
            foreach (OrdenTrabajoResumen item in items)
                _ordenes.Add(new OrdenTrabajoListItem(item, ordenesRegularizadas.Contains(item.IdOrdenTrabajo)));
            CountLabel.Text = $"{_ordenes.Count} OT | Actualizado {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            if (!silencioso)
                await DisplayAlertAsync("OT Producción", ex.Message, "OK");
        }
        finally
        {
            Refresh.IsRefreshing = false;
            _isRefreshing = false;
        }
    }

    private async Task AnularOrdenTrabajoAsync(OrdenTrabajoResumen ot)
    {
        try
        {
            if (!PuedeAnular(ot.Estado))
            {
                await DisplayAlertAsync("OT Produccion", "Solo se puede anular una OT en estado Pendiente o En Proceso.", "OK");
                return;
            }

            OrdenTrabajoDetalleResponse detalle = _session.EsDemo
                ? DemoData.OrdenTrabajoDetalle(ot.IdOrdenTrabajo)
                : await _apiClient.GetOrdenTrabajoDetalleAsync(ot.IdOrdenTrabajo);

            bool enProceso = EsEnProceso(detalle.Cabecera.Estado) || EsEnProceso(ot.Estado);
            bool tieneTerminados = detalle.Detalles.Any(x => x.CantidadProducida > 0 || EsTerminado(x.Estado));
            if (enProceso && tieneTerminados)
            {
                await DisplayAlertAsync("OT Produccion", "La OT tiene productos terminados y no puede anularse.", "OK");
                return;
            }

            bool convertirProcesoAMerma = enProceso && detalle.Areas.Any(x => x.CantidadPendiente > 0 && x.Estado is not ("FINALIZADA" or "BLOQUEADA" or "ANULADA"));
            if (convertirProcesoAMerma)
            {
                bool confirmarMerma = await DisplayAlertAsync(
                    "Confirmar anulacion",
                    $"Esta OT tiene productos en proceso en {ResumenAreasProceso(detalle)}. Al confirmar, esos productos pasaran a merma.",
                    "Continuar",
                    "Cancelar");
                if (!confirmarMerma)
                    return;
            }

            string? motivo = await DisplayPromptAsync("Anular OT", $"Motivo de anulacion para {ot.NumeroOT}", "Anular", "Cancelar", "Motivo", maxLength: 200);
            if (string.IsNullOrWhiteSpace(motivo))
                return;

            if (_session.EsDemo)
            {
                await DisplayAlertAsync("OT demo", $"Se anularia {ot.NumeroOT} con motivo: {motivo}", "OK");
                return;
            }

            DocumentoAccionResponse response = await _apiClient.AnularOrdenTrabajoAsync(
                ot.IdOrdenTrabajo,
                new OrdenTrabajoAnularRequest(
                    convertirProcesoAMerma,
                    _session.Usuario?.IdUsuario ?? 0,
                    motivo,
                    _session.Usuario?.NombreUsuario ?? "Android"));
            await DisplayAlertAsync("OT Produccion", response.Mensaje, "OK");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("OT Produccion", ex.Message, "OK");
        }
    }

    private async Task GenerarRegularizacionAsync(OrdenTrabajoResumen ot)
    {
        try
        {
            OtValidacionResponse validacion = _session.EsDemo
                ? new OtValidacionResponse(true, "Regularizacion demo lista.", [])
                : await _apiClient.ValidarRegularizacionOrdenTrabajoAsync(ot.IdOrdenTrabajo);

            if (!validacion.PuedeGenerar)
            {
                await DisplayAlertAsync("Regularizacion", validacion.Mensaje, "OK");
                return;
            }

            bool confirmar = await DisplayAlertAsync(
                "Generar regularizacion",
                $"Generar una nueva OT de regularizacion desde {ot.NumeroOT}?",
                "Generar",
                "Cancelar");
            if (!confirmar)
                return;

            if (_session.EsDemo)
            {
                await DisplayAlertAsync("Regularizacion demo", $"Se generaria una OT de regularizacion desde {ot.NumeroOT}.", "OK");
                return;
            }

            GenerarOtResponse response = await _apiClient.GenerarRegularizacionOrdenTrabajoAsync(
                ot.IdOrdenTrabajo,
                new DocumentoAccionRequest(_session.Usuario?.NombreUsuario ?? "Android", $"Regularizacion de OT {ot.NumeroOT}"));
            await DisplayAlertAsync("Regularizacion", response.Mensaje, "OK");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Regularizacion", ex.Message, "OK");
        }
    }

    private static bool EsOtActiva(string estado) => DocumentoFiltroHelper.Normalizar(estado) is not ("TERMINADO" or "TERMINADA" or "TERMINADO_PARCIAL" or "ANULADA" or "ANULADO");

    private static bool CoincideEstadoOt(string estado, string filtro)
    {
        string valor = DocumentoFiltroHelper.Normalizar(estado);
        return filtro is "" or "TODOS"
            || filtro == "EN_PROCESO" && valor is "EN_PROCESO" or "PARCIAL" or "EMITIDA"
            || filtro == "PENDIENTE" && valor == "PENDIENTE"
            || filtro == "TERMINADO" && valor is "TERMINADO" or "TERMINADA"
            || filtro == "TERMINADO_PARCIAL" && valor == "TERMINADO_PARCIAL"
            || filtro == "ANULADA" && valor is "ANULADA" or "ANULADO";
    }

    private static bool PuedeAnular(string estado) => DocumentoFiltroHelper.Normalizar(estado) is "PENDIENTE" or "EN_PROCESO";
    private static bool EsEnProceso(string estado) => DocumentoFiltroHelper.Normalizar(estado) is "EN_PROCESO" or "PROCESO";
    private static bool EsTerminado(string estado) => DocumentoFiltroHelper.Normalizar(estado) is "TERMINADO" or "TERMINADA";

    private static string ResumenAreasProceso(OrdenTrabajoDetalleResponse detalle)
    {
        List<string> areas = detalle.Areas
            .Where(x => x.CantidadPendiente > 0 && x.Estado is not ("FINALIZADA" or "BLOQUEADA" or "ANULADA"))
            .GroupBy(x => x.NombreArea)
            .Select(x => $"{x.Key} ({x.Sum(y => y.CantidadPendiente):N2})")
            .ToList();

        return areas.Count == 0 ? "las areas de produccion" : string.Join(", ", areas);
    }

    private sealed record OrdenTrabajoListItem(OrdenTrabajoResumen Item, bool TieneRegularizacionRelacionada)
    {
        public string NumeroOT => Item.NumeroOT;
        public string FechaHoraTexto => Item.FechaEmision.ToString("dd/MM/yyyy HH:mm");
        public string OrdenCompraClienteTexto => string.IsNullOrWhiteSpace(Item.OrdenCompraCliente) ? "Sin OC cliente" : Item.OrdenCompraCliente;
        public string Cliente => Item.NombreCliente;
        public string Estado => Item.Estado;
        public Color EstadoBackgroundColor => ObtenerEstadoBackgroundColor(Item.Estado);
        public Color EstadoStrokeColor => ObtenerEstadoStrokeColor(Item.Estado);
        public Color EstadoTextColor => ObtenerEstadoTextColor(Item.Estado);
        public string NumeroOciTexto => string.IsNullOrWhiteSpace(Item.NumeroOci) ? "Sin OCI" : Item.NumeroOci;
        public string TipoOT => string.IsNullOrWhiteSpace(Item.TipoOT) ? "OCI" : Item.TipoOT;
        public string OtRelacionadaTexto => string.IsNullOrWhiteSpace(Item.NumeroOTRelacionada) ? "Sin relacion" : Item.NumeroOTRelacionada;
        public string Usuario => string.IsNullOrWhiteSpace(Item.UsuarioCreacion) ? "No registrado" : Item.UsuarioCreacion;
        public bool MostrarAnulacion => DocumentoFiltroHelper.Normalizar(Item.Estado) is "ANULADO" or "ANULADA";
        public bool PuedeAnular => OrdenesTrabajoPage.PuedeAnular(Item.Estado);
        public bool PuedeRegularizar => DocumentoFiltroHelper.Normalizar(Item.Estado) == "TERMINADO_PARCIAL"
            && !Item.TieneRegularizacion
            && !TieneRegularizacionRelacionada
            && Item.TotalPendiente > 0;
        public string DetalleAnulacion =>
            $"Motivo: {TextoOmitido(Item.MotivoAnulacion)}\nFecha y hora: {TextoFecha(Item.FechaAnulacion)}\nUsuario: {TextoOmitido(Item.UsuarioAnulacion)}";
    }

    private static string TextoOmitido(string? valor) => string.IsNullOrWhiteSpace(valor) ? "No registrado" : valor.Trim();
    private static string TextoFecha(DateTime? valor) => valor.HasValue ? valor.Value.ToString("dd/MM/yyyy HH:mm") : "No registrada";

    private static Color ObtenerEstadoBackgroundColor(string estado)
    {
        string valor = DocumentoFiltroHelper.Normalizar(estado);
        return valor switch
        {
            "ENTREGADO" or "ENTREGADA" or "TERMINADO" or "TERMINADA" => Color.FromArgb("#DCFCE7"),
            "ANULADO" or "ANULADA" => Color.FromArgb("#FEE2E2"),
            "EN_PROCESO" or "PROCESO" => Color.FromArgb("#FEF3C7"),
            "TERMINADO_PARCIAL" or "PARCIAL" => Color.FromArgb("#FFEDD5"),
            _ => Color.FromArgb("#E0F2FE")
        };
    }

    private static Color ObtenerEstadoStrokeColor(string estado)
    {
        string valor = DocumentoFiltroHelper.Normalizar(estado);
        return valor switch
        {
            "ENTREGADO" or "ENTREGADA" or "TERMINADO" or "TERMINADA" => Color.FromArgb("#22C55E"),
            "ANULADO" or "ANULADA" => Color.FromArgb("#EF4444"),
            "EN_PROCESO" or "PROCESO" => Color.FromArgb("#F59E0B"),
            "TERMINADO_PARCIAL" or "PARCIAL" => Color.FromArgb("#F97316"),
            _ => Color.FromArgb("#38BDF8")
        };
    }

    private static Color ObtenerEstadoTextColor(string estado)
    {
        string valor = DocumentoFiltroHelper.Normalizar(estado);
        return valor switch
        {
            "ENTREGADO" or "ENTREGADA" or "TERMINADO" or "TERMINADA" => Color.FromArgb("#166534"),
            "ANULADO" or "ANULADA" => Color.FromArgb("#991B1B"),
            "EN_PROCESO" or "PROCESO" => Color.FromArgb("#92400E"),
            "TERMINADO_PARCIAL" or "PARCIAL" => Color.FromArgb("#9A3412"),
            _ => Color.FromArgb("#075985")
        };
    }
}
