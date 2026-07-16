using CorexProd.App.Models;
using CorexProd.App.Services;
using System.Globalization;

namespace CorexProd.App.Pages;

public partial class HomePage : ContentPage
{
    private readonly CorexProdApiClient _apiClient;
    private readonly SessionState _session;
    private readonly CultureInfo _culture = new("es-PE");
    private DateTime _periodoSeleccionado = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private bool _isLoading;
    private IReadOnlyList<EntregaAlertItem> _alertasEntrega = [];

    public HomePage()
    {
        InitializeComponent();
        _apiClient = ServiceHelper.GetRequiredService<CorexProdApiClient>();
        _session = ServiceHelper.GetRequiredService<SessionState>();
        Shell.SetBackButtonBehavior(this, new BackButtonBehavior
        {
            Command = new Command(async () => await ConfirmarCerrarSesionAsync())
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_session.EstaAutenticado)
        {
            await Shell.Current.GoToAsync("//login");
            return;
        }

        DrawerUserLabel.Text = $"{_session.Usuario?.NombreCompleto} | {_session.Usuario?.NombreRol}";
        ActualizarPeriodoVisual();
        ModuleMenuView.ItemsSource = CrearMenuModulos();
        await CargarInicioAsync();
    }

    private async void OnRefreshing(object? sender, EventArgs e) => await CargarInicioAsync();

    private async Task CargarInicioAsync()
    {
        if (_isLoading)
            return;

        try
        {
            _isLoading = true;
            Refresh.IsRefreshing = true;

            HealthResponse health;
            IReadOnlyList<OciResumen> ocis;
            IReadOnlyList<OrdenTrabajoResumen> ots;
            IReadOnlyList<GuiaInternaResumen> guias;
            IReadOnlyList<ProductoStock> productos;
            IReadOnlyList<InsumoStock> insumos;

            if (_session.EsDemo)
            {
                health = new HealthResponse("OK", "Demo", "Local", DateTime.Now);
                ocis = DemoData.Ocis;
                ots = DemoData.OrdenesTrabajo;
                guias = DemoData.GuiasInternas;
                productos = DemoData.Productos;
                insumos = DemoData.Insumos;
            }
            else
            {
                health = await _apiClient.GetHealthAsync();
                ocis = (await _apiClient.GetOciAsync(string.Empty)).Items;
                ots = (await _apiClient.GetOrdenesTrabajoAsync(string.Empty)).Items;
                guias = (await _apiClient.GetGuiasInternasAsync(string.Empty)).Items;
                productos = (await _apiClient.GetProductosAsync(string.Empty)).Items;
                insumos = (await _apiClient.GetInsumosAsync(string.Empty)).Items;
            }

            DashboardData data = await PrepararDashboardAsync(ocis, ots, guias, productos, insumos);
            PintarDashboard(data);

            ApiStatusLabel.Text = $"API {health.Estado} | BD {health.BaseDatos}";
            ApiStatusLabel.TextColor = Color.FromArgb("#0E9384");
            LastUpdateLabel.Text = $"Actualizado {DateTime.Now:HH:mm}";
        }
        catch (Exception ex)
        {
            ApiStatusLabel.Text = $"Sin conexion: {ex.Message}";
            ApiStatusLabel.TextColor = Color.FromArgb("#B42318");
        }
        finally
        {
            Refresh.IsRefreshing = false;
            _isLoading = false;
        }
    }

    private async Task<DashboardData> PrepararDashboardAsync(
        IReadOnlyList<OciResumen> ocis,
        IReadOnlyList<OrdenTrabajoResumen> ots,
        IReadOnlyList<GuiaInternaResumen> guias,
        IReadOnlyList<ProductoStock> productos,
        IReadOnlyList<InsumoStock> insumos)
    {
        DateTime hoy = DateTime.Today;
        DateTime inicioMes = _periodoSeleccionado;
        DateTime inicioMesSiguiente = inicioMes.AddMonths(1);
        DateTime inicioMesAnterior = inicioMes.AddMonths(-1);
        string mesAnterior = Capitalizar(inicioMesAnterior.ToString("MMMM", _culture));

        List<OciResumen> ocisMes = FiltrarMes(ocis, inicioMes, inicioMesSiguiente).ToList();
        List<OciResumen> ocisMesAnterior = FiltrarMes(ocis, inicioMesAnterior, inicioMes).ToList();
        List<OrdenTrabajoResumen> otsMes = FiltrarMes(ots, inicioMes, inicioMesSiguiente).ToList();
        List<OrdenTrabajoResumen> otsMesAnterior = FiltrarMes(ots, inicioMesAnterior, inicioMes).ToList();
        List<GuiaInternaResumen> guiasMes = FiltrarMes(guias, inicioMes, inicioMesSiguiente).ToList();
        List<GuiaInternaResumen> guiasMesAnterior = FiltrarMes(guias, inicioMesAnterior, inicioMes).ToList();

        List<EntregaAlertItem> alertas = CrearAlertasEntrega(ocis, hoy).ToList();

        int vencidas = alertas.Count(x => x.DiasRestantes < 0);
        int vencenHoy = alertas.Count(x => x.DiasRestantes == 0);
        int proximas13 = alertas.Count(x => x.DiasRestantes is >= 1 and <= 3);
        int proximas47 = alertas.Count(x => x.DiasRestantes is >= 4 and <= 7);
        int entregadasATiempo = ocisMes.Count(EsEntregadaATiempo);

        return new DashboardData(
            Kpis:
            [
                new("OC", ocisMes.Count.ToString(), "OC del mes", CrearComparacion(ocisMes.Count, ocisMesAnterior.Count, mesAnterior), "#135DFF", "#EEF2FF"),
                new("OT", otsMes.Count.ToString(), "OT del mes", CrearComparacion(otsMes.Count, otsMesAnterior.Count, mesAnterior), "#16A34A", "#EAF7EF"),
                new("GI", guiasMes.Count.ToString(), "Guias internas", CrearComparacion(guiasMes.Count, guiasMesAnterior.Count, mesAnterior), "#F97316", "#FFF1E7")
            ],
            EntregaResumen:
            [
                new(vencidas.ToString(), "Vencidas", "(> 0 dias)", "#DC2626", "#FDECEC"),
                new(vencenHoy.ToString(), "Vencen hoy", "(0 dias)", "#F97316", "#FFF1E7"),
                new(proximas13.ToString(), "Proximas", "(1 - 3 dias)", "#F59E0B", "#FFF7DF"),
                new(proximas47.ToString(), "Proximas", "(4 - 7 dias)", "#2563EB", "#EEF4FF"),
                new(entregadasATiempo.ToString(), "Entregadas", "a tiempo", "#16A34A", "#EAF7EF")
            ],
            Urgentes: alertas.Take(5).ToList(),
            Alertas: alertas,
            OcResumen:
            [
                new("Generadas", ocisMes.Count.ToString(), "#135DFF"),
                new("Pendiente / Produccion", ocisMes.Count(x => EsPendienteProduccion(x.Estado) || EsEnProceso(x.Estado) || EsParcial(x.Estado)).ToString(), "#F97316"),
                new("Entregadas", ocisMes.Count(x => EsEntregadoOTerminado(x.Estado)).ToString(), "#16A34A"),
                new("Anuladas", ocisMes.Count(x => EsAnulado(x.Estado)).ToString(), "#E11D48")
            ],
            OtResumen:
            [
                new("Generadas", otsMes.Count.ToString(), "#135DFF"),
                new("Pendientes / En proceso", otsMes.Count(x => EsOtActiva(x.Estado)).ToString(), "#F97316"),
                new("Terminadas", otsMes.Count(x => EsEntregadoOTerminado(x.Estado)).ToString(), "#16A34A"),
                new("Anuladas", otsMes.Count(x => EsAnulado(x.Estado)).ToString(), "#E11D48")
            ],
            TopProductos: await ObtenerTopProductosElaboradosAsync(otsMes));
    }

    private void PintarDashboard(DashboardData data)
    {
        _alertasEntrega = data.Alertas;

        KpiView.ItemsSource = data.Kpis;
        EntregaResumenView.ItemsSource = data.EntregaResumen;
        UrgentesView.ItemsSource = data.Urgentes;
        AlertasView.ItemsSource = data.Alertas;
        OcResumenView.ItemsSource = data.OcResumen;
        OtResumenView.ItemsSource = data.OtResumen;
        TopProductosView.ItemsSource = data.TopProductos;

        AlertCountLabel.Text = data.Alertas.Count.ToString();
        AlertBadge.IsVisible = data.Alertas.Count > 0;
    }

    private IEnumerable<EntregaAlertItem> CrearAlertasEntrega(IReadOnlyList<OciResumen> ocis, DateTime hoy)
    {
        return ocis
            .Where(x => !EsEntregadoOTerminado(x.Estado) && !EsAnulado(x.Estado))
            .Where(x => x.FechaEntrega != default)
            .Select(x => CrearAlertaEntrega(x, hoy))
            .Where(x => x.DiasRestantes <= 7)
            .OrderBy(x => x.FechaEntrega)
            .ThenBy(x => x.NumeroOci);
    }

    private EntregaAlertItem CrearAlertaEntrega(OciResumen oci, DateTime hoy)
    {
        DateTime fechaEntrega = oci.FechaEntrega.Date;
        int dias = (fechaEntrega - hoy).Days;
        string color = dias switch
        {
            < 0 => "#DC2626",
            0 => "#F97316",
            <= 3 => "#F59E0B",
            _ => "#2563EB"
        };

        string alerta = dias switch
        {
            < 0 => $"Vencida hace {Math.Abs(dias)} dia{Plural(Math.Abs(dias))}",
            0 => "Vence hoy",
            1 => "Vence manana",
            _ => $"Vence en {dias} dias"
        };

        return new EntregaAlertItem(
            oci.IdOrdenCompraInterna,
            FormatearNumeroOc(oci.NumeroOci),
            string.IsNullOrWhiteSpace(oci.OrdenCompraCliente) ? "OC Cliente: Sin dato" : $"OC Cliente: {oci.OrdenCompraCliente.Trim()}",
            TextoVacio(oci.NombreCliente),
            TextoVacio(oci.Estado),
            fechaEntrega,
            fechaEntrega.ToString("dd/MM/yyyy", _culture),
            alerta,
            dias,
            color);
    }

    private async Task<IReadOnlyList<RankingItem>> ObtenerTopProductosElaboradosAsync(IReadOnlyList<OrdenTrabajoResumen> otsMes)
    {
        Dictionary<string, decimal> acumulado = new(StringComparer.OrdinalIgnoreCase);

        foreach (OrdenTrabajoResumen ot in otsMes.Where(x => !EsAnulado(x.Estado)).Take(30))
        {
            OrdenTrabajoDetalleResponse? detalle = null;

            try
            {
                detalle = _session.EsDemo
                    ? DemoData.OrdenTrabajoDetalle(ot.IdOrdenTrabajo)
                    : await _apiClient.GetOrdenTrabajoDetalleAsync(ot.IdOrdenTrabajo);
            }
            catch
            {
                continue;
            }

            foreach (OrdenTrabajoProducto producto in detalle.Detalles)
            {
                decimal cantidad = producto.CantidadProducida > 0
                    ? producto.CantidadProducida
                    : Math.Max(producto.CantidadLanzada, producto.CantidadPlanificada);

                if (cantidad <= 0)
                    continue;

                string nombre = $"{TextoVacio(producto.CodigoProducto)} - {TextoVacio(producto.NombreProducto)}";
                acumulado[nombre] = acumulado.TryGetValue(nombre, out decimal actual)
                    ? actual + cantidad
                    : cantidad;
            }
        }

        return acumulado
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Key)
            .Take(5)
            .Select((x, index) => new RankingItem(index + 1, x.Key, FormatearCantidad(x.Value)))
            .ToList();
    }

    private static IEnumerable<T> FiltrarMes<T>(IEnumerable<T> items, DateTime inicio, DateTime fin)
    {
        return items.Where(x =>
        {
            DateTime fecha = x switch
            {
                OciResumen oci => oci.FechaEmision,
                OrdenTrabajoResumen ot => ot.FechaEmision,
                GuiaInternaResumen guia => guia.FechaEmision,
                _ => DateTime.MinValue
            };

            return fecha >= inicio && fecha < fin;
        });
    }

    private static bool EsEntregadaATiempo(OciResumen oci)
    {
        if (!EsEntregadoOTerminado(oci.Estado))
            return false;

        DateTime fechaCierre = (oci.FechaCierre ?? oci.FechaEmision).Date;
        return oci.FechaEntrega == default || fechaCierre <= oci.FechaEntrega.Date;
    }

    private static string CrearComparacion(int actual, int anterior, string mesAnterior)
    {
        int diferencia = actual - anterior;
        string signo = diferencia >= 0 ? "+" : string.Empty;
        return $"{signo}{diferencia} vs. {mesAnterior}";
    }

    private static bool EsPendienteProduccion(string estado)
    {
        string valor = DocumentoFiltroHelper.Normalizar(estado);
        return valor is "PENDIENTE" or "EMITIDA" or "EMITIDO" or "PENDIENTE_PRODUCCION" or "PENDIENTE PRODUCCION";
    }

    private static bool EsEnProceso(string estado)
    {
        string valor = DocumentoFiltroHelper.Normalizar(estado);
        return valor.Contains("PROCESO") || valor is "EN_PROCESO" or "EN PROCESO";
    }

    private static bool EsOtActiva(string estado)
    {
        string valor = DocumentoFiltroHelper.Normalizar(estado);
        bool esPendiente = valor is "PENDIENTE" or "PENDIENTE_PRODUCCION" or "PENDIENTE PRODUCCION";
        return esPendiente || EsEnProceso(estado);
    }

    private static bool EsParcial(string estado) => DocumentoFiltroHelper.Normalizar(estado).Contains("PARCIAL");

    private static bool EsEntregadoOTerminado(string estado)
    {
        string valor = DocumentoFiltroHelper.Normalizar(estado);
        return valor is "ENTREGADO" or "ENTREGADA" or "TERMINADO" or "TERMINADA" or "COMPLETADO" or "COMPLETADA" or "CERRADO" or "CERRADA";
    }

    private static bool EsAnulado(string estado) => DocumentoFiltroHelper.Normalizar(estado) is "ANULADO" or "ANULADA";

    private static string FormatearNumeroOc(string numero) =>
        numero.StartsWith("OCI-", StringComparison.OrdinalIgnoreCase)
            ? "OC-" + numero[4..]
            : numero;

    private static string FormatearCantidad(decimal cantidad) =>
        cantidad % 1 == 0 ? cantidad.ToString("N0", CultureInfo.InvariantCulture) : cantidad.ToString("N2", CultureInfo.InvariantCulture);

    private static string Capitalizar(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : char.ToUpper(value[0], new CultureInfo("es-PE")) + value[1..];

    private static string Plural(int cantidad) => cantidad == 1 ? string.Empty : "s";

    private static string TextoVacio(string? value) => string.IsNullOrWhiteSpace(value) ? "Sin dato" : value.Trim();

    private void ActualizarPeriodoVisual()
    {
        string texto = Capitalizar(_periodoSeleccionado.ToString("MMMM yyyy", _culture));
        PeriodoSeleccionadoLabel.Text = texto;
        PeriodoLabel.Text = texto;
        PeriodoPicker.Date = _periodoSeleccionado;
    }

    private async void OnPeriodoAnteriorClicked(object? sender, EventArgs e)
    {
        _periodoSeleccionado = _periodoSeleccionado.AddMonths(-1);
        ActualizarPeriodoVisual();
        await CargarInicioAsync();
    }

    private async void OnPeriodoSiguienteClicked(object? sender, EventArgs e)
    {
        _periodoSeleccionado = _periodoSeleccionado.AddMonths(1);
        ActualizarPeriodoVisual();
        await CargarInicioAsync();
    }

    private void OnSeleccionarPeriodoClicked(object? sender, EventArgs e)
    {
        PeriodoPicker.Focus();
    }

    private async void OnPeriodoDateSelected(object? sender, DateChangedEventArgs e)
    {
        DateTime fecha = e.NewDate ?? DateTime.Today;
        DateTime seleccionado = new(fecha.Year, fecha.Month, 1);
        if (seleccionado == _periodoSeleccionado)
            return;

        _periodoSeleccionado = seleccionado;
        ActualizarPeriodoVisual();
        await CargarInicioAsync();
    }

    private void OnToggleMenuClicked(object? sender, EventArgs e)
    {
        MenuDrawerOverlay.IsVisible = true;
    }

    private void OnCloseMenuClicked(object? sender, EventArgs e)
    {
        MenuDrawerOverlay.IsVisible = false;
        ModuleMenuView.SelectedItem = null;
    }

    private void OnBellClicked(object? sender, EventArgs e)
    {
        AlertOverlay.IsVisible = true;
    }

    private void OnCloseAlertsClicked(object? sender, EventArgs e)
    {
        AlertOverlay.IsVisible = false;
    }

    private async void OnVerAlertaClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not EntregaAlertItem item)
            return;

        AlertOverlay.IsVisible = false;
        await Shell.Current.GoToAsync($"{nameof(OciDetallePage)}?id={item.IdOrdenCompraInterna}");
    }

    private async void OnModuleMenuSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not ModuleMenuItem item)
            return;

        ModuleMenuView.SelectedItem = null;
        MenuDrawerOverlay.IsVisible = false;

        switch (item.Ruta)
        {
            case "Inicio":
                await CargarInicioAsync();
                break;
            case "Kardex":
                await AbrirModuloAsync("Kardex");
                break;
            default:
                await Shell.Current.GoToAsync(item.Ruta);
                break;
        }
    }

    private async void OnOciClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(OciPage));

    private async void OnNuevaOcClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(ProformaEditorPage));

    private async void OnOrdenesTrabajoClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(OrdenesTrabajoPage));

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        MenuDrawerOverlay.IsVisible = false;
        await ConfirmarCerrarSesionAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        if (AlertOverlay.IsVisible)
        {
            AlertOverlay.IsVisible = false;
            return true;
        }

        if (MenuDrawerOverlay.IsVisible)
        {
            OnCloseMenuClicked(null, EventArgs.Empty);
            return true;
        }

        Dispatcher.Dispatch(async () => await ConfirmarSalirAsync());
        return true;
    }

    private async Task ConfirmarCerrarSesionAsync()
    {
        bool confirmar = await DisplayAlertAsync(
            "Cerrar sesion",
            "Esta seguro de que desea cerrar sesion?",
            "Si",
            "No");

        if (!confirmar)
            return;

        _session.Cerrar();
        await Shell.Current.GoToAsync("//login");
    }

    private async Task ConfirmarSalirAsync()
    {
        bool confirmar = await DisplayAlertAsync(
            "Salir",
            "Desea salir de la aplicacion?",
            "Si",
            "No");

        if (confirmar)
            Application.Current?.Quit();
    }

    private static Task AbrirModuloAsync(string titulo)
    {
        return Shell.Current.GoToAsync($"{nameof(ModuloPage)}?titulo={Uri.EscapeDataString(titulo)}");
    }

    private static IReadOnlyList<ModuleMenuItem> CrearMenuModulos()
    {
        return
        [
            new("Inicio", "Panel principal", "IN", "Inicio"),
            new("OC", "Ventas", "OC", nameof(OciPage)),
            new("Guia Interna", "Ventas", "GI", nameof(GuiaInternaPage)),
            new("OT", "Produccion", "OT", nameof(OrdenesTrabajoPage)),
            new("OT Manual", "Produccion", "OM", nameof(OrdenTrabajoManualPage)),
            new("Kardex", "Reportes", "KX", "Kardex"),
            new("Stock productos", "Almacen", "SP", nameof(StockProductosPage)),
            new("Stock insumos", "Almacen", "SI", nameof(StockInsumosPage)),
            new("Ingreso stock", "Almacen", "IS", nameof(IngresoManualStockPage))
        ];
    }

    private sealed record DashboardData(
        IReadOnlyList<DashboardKpi> Kpis,
        IReadOnlyList<DeliveryMetric> EntregaResumen,
        IReadOnlyList<EntregaAlertItem> Urgentes,
        IReadOnlyList<EntregaAlertItem> Alertas,
        IReadOnlyList<SummaryMetric> OcResumen,
        IReadOnlyList<SummaryMetric> OtResumen,
        IReadOnlyList<RankingItem> TopProductos);

    private sealed record DashboardKpi(string Icono, string Valor, string Titulo, string Comparacion, string ColorHex, string SoftColorHex)
    {
        public Color Color => Color.FromArgb(ColorHex);
        public Color SoftColor => Color.FromArgb(SoftColorHex);
    }

    private sealed record DeliveryMetric(string Valor, string Titulo, string Subtitulo, string ColorHex, string BackgroundHex)
    {
        public Color Color => Color.FromArgb(ColorHex);
        public Color Background => Color.FromArgb(BackgroundHex);
    }

    private sealed record EntregaAlertItem(
        int IdOrdenCompraInterna,
        string NumeroOci,
        string OcClienteTexto,
        string Cliente,
        string EstadoTexto,
        DateTime FechaEntrega,
        string FechaEntregaTexto,
        string AlertaTexto,
        int DiasRestantes,
        string ColorHex)
    {
        public Color Color => Color.FromArgb(ColorHex);
        public Brush Brush => new SolidColorBrush(Color);
    }

    private sealed record SummaryMetric(string Titulo, string Valor, string ColorHex)
    {
        public Color Color => Color.FromArgb(ColorHex);
    }

    private sealed record RankingItem(int Posicion, string Nombre, string Cantidad);

    private sealed record ModuleMenuItem(string Titulo, string Grupo, string Icono, string Ruta);
}
