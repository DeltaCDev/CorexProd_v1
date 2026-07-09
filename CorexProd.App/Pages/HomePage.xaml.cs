using CorexProd.App.Models;
using CorexProd.App.Services;
using System.Globalization;

namespace CorexProd.App.Pages;

public partial class HomePage : ContentPage
{
    private readonly CorexProdApiClient _apiClient;
    private readonly SessionState _session;
    private bool _isLoading;

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

        UserLabel.Text = _session.Usuario?.NombreCompleto;
        DrawerUserLabel.Text = _session.Usuario?.NombreCompleto;
        RoleLabel.Text = $"{_session.Usuario?.NombreUsuario} - {_session.Usuario?.NombreRol}";
        PeriodoLabel.Text = DateTime.Today.ToString("MMMM yyyy", new CultureInfo("es-PE"));
        MenusView.ItemsSource = OrdenarMenus(_session.Menus);
        ModuleMenuView.ItemsSource = CrearMenuModulos();
        await CargarInicioAsync();
    }

    private async void OnRefreshing(object? sender, EventArgs e) => await CargarInicioAsync();

    private static IReadOnlyList<string> OrdenarMenus(IReadOnlyList<string> menus)
    {
        string[] orden =
        [
            "Ventas",
            "OC",
            "Guia Interna",
            "Produccion",
            "OT",
            "OT Manual",
            "Reportes",
            "Kardex",
            "Almacen",
            "Stock productos",
            "Stock insumos",
            "Ingreso stock"
        ];

        Dictionary<string, int> posiciones = orden
            .Select((menu, index) => new { menu, index })
            .ToDictionary(x => x.menu, x => x.index, StringComparer.OrdinalIgnoreCase);

        return menus
            .Select(NormalizarMenuVisible)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => posiciones.TryGetValue(x, out int posicion) ? posicion : int.MaxValue)
            .ThenBy(x => x)
            .ToList();
    }

    private static string NormalizarMenuVisible(string menu) => menu.Trim() switch
    {
        "OCI" => "OC",
        "OT Produccion" => "OT",
        _ => menu
    };

    private static IReadOnlyList<ModuleMenuItem> CrearMenuModulos()
    {
        return
        [
            new("Inicio", "Panel principal", "🏠", "Inicio"),
            new("OC", "Ventas", "🛒", nameof(OciPage)),
            new("Guía Interna", "Ventas", "🚚", nameof(GuiaInternaPage)),
            new("OT", "Producción", "🏭", nameof(OrdenesTrabajoPage)),
            new("OT Manual", "Producción", "📋", nameof(OrdenTrabajoManualPage)),
            new("Kardex", "Reportes", "📈", "Kardex"),
            new("Stock productos", "Almacén", "📦", nameof(StockProductosPage)),
            new("Stock insumos", "Almacén", "🧱", nameof(StockInsumosPage)),
            new("Ingreso stock", "Almacén", "⬇️", nameof(IngresoManualStockPage))
        ];
    }

    private async Task CargarInicioAsync()
    {
        if (_isLoading)
            return;

        try
        {
            _isLoading = true;
            Refresh.IsRefreshing = true;
            await CargarEmpresaAsync();

            HealthResponse? health;
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

            ActualizarResumen(ocis, ots, guias, productos, insumos);
            ActualizarRankings(ocis, ots, productos);
            ActualizarGraficos(ocis, ots, guias);
            ActualizarActividad(ocis, ots, guias);
            ApiStatusLabel.Text = $"API {health.Estado} | BD {health.BaseDatos}";
            ApiStatusLabel.TextColor = Color.FromArgb("#0E9384");
            LastUpdateLabel.Text = DateTime.Now.ToString("HH:mm");
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

    private async Task CargarEmpresaAsync()
    {
        try
        {
            EmpresaInfo empresa = _session.EsDemo ? DemoData.Empresa : await _apiClient.GetEmpresaAsync();
            string nombre = string.IsNullOrWhiteSpace(empresa.Nombre) ? "CorexProd" : empresa.Nombre.Trim();
            CompanyNameLabel.Text = nombre;

            if (!string.IsNullOrWhiteSpace(empresa.LogoBase64))
            {
                byte[] bytes = Convert.FromBase64String(empresa.LogoBase64);
                CompanyLogo.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
            }
        }
        catch
        {
            CompanyNameLabel.Text = "CorexProd";
        }
    }

    private void ActualizarResumen(
        IReadOnlyList<OciResumen> ocis,
        IReadOnlyList<OrdenTrabajoResumen> ots,
        IReadOnlyList<GuiaInternaResumen> guias,
        IReadOnlyList<ProductoStock> productos,
        IReadOnlyList<InsumoStock> insumos)
    {
        DateTime inicioMes = new(DateTime.Today.Year, DateTime.Today.Month, 1);
        IReadOnlyList<OciResumen> ocisMes = ocis.Where(x => x.FechaEmision >= inicioMes).ToList();
        IReadOnlyList<OrdenTrabajoResumen> otsMes = ots.Where(x => x.FechaEmision >= inicioMes).ToList();
        IReadOnlyList<GuiaInternaResumen> guiasMesLista = guias.Where(x => x.FechaEmision >= inicioMes).ToList();

        int ociPendientes = ocisMes.Count(x => EsPendienteProduccion(x.Estado));
        int ociParciales = ocisMes.Count(x => EsParcial(x.Estado));
        int ociDespachadas = ocisMes.Count(x => EsDespachado(x.Estado));
        int ociEntregadas = ocisMes.Count(x => EsEntregadoOTerminado(x.Estado));
        int ociAnuladas = ocisMes.Count(x => EsAnulado(x.Estado));

        int otProceso = otsMes.Count(x => EsEnProceso(x.Estado));
        int otCompletadas = otsMes.Count(x => EsEntregadoOTerminado(x.Estado));
        int otRegularizacion = otsMes.Count(x => DocumentoFiltroHelper.Normalizar(x.TipoOT).Contains("REGULAR"));
        int otAbastecimiento = otsMes.Count(x => DocumentoFiltroHelper.Normalizar(x.TipoOT).Contains("ABAST"));
        int otAnuladas = otsMes.Count(x => EsAnulado(x.Estado));

        int guiasGeneradas = guiasMesLista.Count;
        int guiasPendientes = guiasMesLista.Count(x => EsPendienteProduccion(x.Estado));
        int guiasAtendidas = guiasMesLista.Count(x => EsEntregadoOTerminado(x.Estado) || EsDespachado(x.Estado));
        int guiasAnuladas = guiasMesLista.Count(x => EsAnulado(x.Estado));

        int productosBajos = productos.Count(x => x.StockActual <= 0);
        int insumosBajos = insumos.Count(x => x.StockActual <= 0);

        OciActivasLabel.Text = ocisMes.Count.ToString();
        OciPendientesLabel.Text = ociPendientes.ToString();
        OciParcialesLabel.Text = ociParciales.ToString();
        OciDespachadasLabel.Text = ociDespachadas.ToString();
        OciEntregadasLabel.Text = ociEntregadas.ToString();
        OciAnuladasLabel.Text = ociAnuladas.ToString();

        OtActivasLabel.Text = otsMes.Count.ToString();
        OtProcesoLabel.Text = otProceso.ToString();
        OtCompletadasLabel.Text = otCompletadas.ToString();
        OtRegularizacionLabel.Text = otRegularizacion.ToString();
        OtAbastecimientoLabel.Text = otAbastecimiento.ToString();
        OtAnuladasLabel.Text = otAnuladas.ToString();

        GuiasMesLabel.Text = guiasGeneradas.ToString();
        GuiasGeneradasLabel.Text = guiasGeneradas.ToString();
        GuiasPendientesLabel.Text = guiasPendientes.ToString();
        GuiasAtendidasLabel.Text = guiasAtendidas.ToString();
        GuiasAnuladasLabel.Text = guiasAnuladas.ToString();

        StockAlertasLabel.Text = (productosBajos + insumosBajos).ToString();
        StockResumenLabel.Text = $"{productosBajos} prod. | {insumosBajos} ins.";
    }

    private void ActualizarRankings(
        IReadOnlyList<OciResumen> ocis,
        IReadOnlyList<OrdenTrabajoResumen> ots,
        IReadOnlyList<ProductoStock> productos)
    {
        DateTime inicioMes = new(DateTime.Today.Year, DateTime.Today.Month, 1);

        TopClientesView.ItemsSource = ocis
            .Where(x => x.FechaEmision >= inicioMes)
            .GroupBy(x => TextoVacio(x.NombreCliente))
            .Select(x => new RankingItem(0, x.Key, x.Count()))
            .OrderByDescending(x => x.Cantidad)
            .ThenBy(x => x.Nombre)
            .Take(5)
            .Select((x, index) => x with { Posicion = index + 1 })
            .ToList();

        TopProductosView.ItemsSource = productos
            .OrderByDescending(x => x.StockActual)
            .Take(5)
            .Select((x, index) => new RankingItem(index + 1, TextoVacio(x.Producto), Convert.ToInt32(Math.Round(x.StockActual))))
            .ToList();

        var usuarioOc = ocis
            .Where(x => x.FechaEmision >= inicioMes)
            .GroupBy(_ => _session.Usuario?.NombreCompleto ?? "Usuario")
            .Select(x => new { Nombre = x.Key, Cantidad = x.Count() })
            .OrderByDescending(x => x.Cantidad)
            .FirstOrDefault();

        var usuarioOt = ots
            .Where(x => x.FechaEmision >= inicioMes)
            .GroupBy(x => TextoVacio(x.UsuarioCreacion))
            .Select(x => new { Nombre = x.Key, Cantidad = x.Count() })
            .OrderByDescending(x => x.Cantidad)
            .FirstOrDefault();

        UsuarioMasOcLabel.Text = usuarioOc?.Nombre ?? "Sin datos";
        UsuarioMasOcCantidadLabel.Text = $"{usuarioOc?.Cantidad ?? 0} OC";
        UsuarioMasOtLabel.Text = usuarioOt?.Nombre ?? "Sin datos";
        UsuarioMasOtCantidadLabel.Text = $"{usuarioOt?.Cantidad ?? 0} OT";
    }

    private void ActualizarGraficos(
        IReadOnlyList<OciResumen> ocis,
        IReadOnlyList<OrdenTrabajoResumen> ots,
        IReadOnlyList<GuiaInternaResumen> guias)
    {
        ChartOciView.ItemsSource = CrearGrafico6Meses(ocis.Select(x => x.FechaEmision));
        ChartOtView.ItemsSource = CrearGrafico6Meses(ots.Select(x => x.FechaEmision));
        ChartGuiasView.ItemsSource = CrearGrafico6Meses(guias.Select(x => x.FechaEmision));
    }

    private static List<ChartItem> CrearGrafico6Meses(IEnumerable<DateTime> fechas)
    {
        DateTime mesActual = new(DateTime.Today.Year, DateTime.Today.Month, 1);
        var meses = Enumerable.Range(0, 6)
            .Select(i => mesActual.AddMonths(i - 5))
            .ToList();

        List<int> totales = meses
            .Select(mes => fechas.Count(fecha => fecha.Year == mes.Year && fecha.Month == mes.Month))
            .ToList();

        int maximo = Math.Max(1, totales.Max());

        return meses
            .Select((mes, index) => new ChartItem(
                mes.ToString("MMM", new CultureInfo("es-PE")),
                totales[index],
                Math.Max(4, 120d * totales[index] / maximo)))
            .ToList();
    }

    private void ActualizarActividad(
        IReadOnlyList<OciResumen> ocis,
        IReadOnlyList<OrdenTrabajoResumen> ots,
        IReadOnlyList<GuiaInternaResumen> guias)
    {
        List<HomeActivityItem> items = [];
        items.AddRange(ocis
            .OrderByDescending(x => x.FechaEmision)
            .Take(3)
            .Select(x => new HomeActivityItem(
                "OC",
                FormatearNumeroOc(x.NumeroOci),
                $"{TextoVacio(x.NombreCliente)} | {TextoVacio(x.Estado)}",
                Color.FromArgb("#0E9384"),
                x.FechaEmision)));
        items.AddRange(ots
            .OrderByDescending(x => x.FechaEmision)
            .Take(3)
            .Select(x => new HomeActivityItem(
                "OT",
                x.NumeroOT,
                $"{TextoVacio(x.NombreCliente)} | {TextoVacio(x.Estado)}",
                Color.FromArgb("#7A5AF8"),
                x.FechaEmision)));
        items.AddRange(guias
            .OrderByDescending(x => x.FechaEmision)
            .Take(3)
            .Select(x => new HomeActivityItem(
                "Guia",
                x.NumeroGuia,
                $"{TextoVacio(x.EmpresaDestino)} | {TextoVacio(x.Estado)}",
                Color.FromArgb("#2563EB"),
                x.FechaEmision)));

        RecentActivityView.ItemsSource = items
            .OrderByDescending(x => x.Fecha)
            .Take(6)
            .ToList();
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

    private static bool EsParcial(string estado) => DocumentoFiltroHelper.Normalizar(estado).Contains("PARCIAL");

    private static bool EsDespachado(string estado)
    {
        string valor = DocumentoFiltroHelper.Normalizar(estado);
        return valor.Contains("DESPACH") || valor.Contains("ATENDID");
    }

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

    private static string TextoVacio(string? value) => string.IsNullOrWhiteSpace(value) ? "Sin dato" : value.Trim();

    private void OnToggleMenuClicked(object? sender, EventArgs e)
    {
        MenuDrawerOverlay.IsVisible = true;
    }

    private void OnCloseMenuClicked(object? sender, EventArgs e)
    {
        MenuDrawerOverlay.IsVisible = false;
        ModuleMenuView.SelectedItem = null;
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

    private async void OnProductosClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(StockProductosPage));

    private async void OnInsumosClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(StockInsumosPage));

    private async void OnOciClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(OciPage));

    private async void OnIngresoStockClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(IngresoManualStockPage));

    private async void OnOrdenesTrabajoClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(OrdenesTrabajoPage));

    private async void OnOtManualClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(OrdenTrabajoManualPage));

    private async void OnKardexClicked(object? sender, EventArgs e) => await AbrirModuloAsync("Kardex");

    private async void OnGuiaInternaClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(GuiaInternaPage));

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        MenuDrawerOverlay.IsVisible = false;
        await ConfirmarCerrarSesionAsync();
    }

    protected override bool OnBackButtonPressed()
    {
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

    private sealed record ModuleMenuItem(string Titulo, string Grupo, string Icono, string Ruta);

    private sealed record RankingItem(int Posicion, string Nombre, int Cantidad);

    private sealed record ChartItem(string Mes, int Total, double Ancho);

    private sealed record HomeActivityItem(
        string Tipo,
        string Titulo,
        string Detalle,
        Color Color,
        DateTime Fecha);
}
