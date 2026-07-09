using CorexProd.App.Models;
using CorexProd.App.Services;

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
        RoleLabel.Text = $"{_session.Usuario?.NombreUsuario} - {_session.Usuario?.NombreRol}";
        MenusView.ItemsSource = OrdenarMenus(_session.Menus);
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

    private async Task CargarInicioAsync()
    {
        if (_isLoading)
            return;

        try
        {
            _isLoading = true;
            Refresh.IsRefreshing = true;
            await CargarEmpresaAsync();

            HealthResponse? health = null;
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
        int ociActivas = ocis.Count(x => !EsCerradoOAnulado(x.Estado));
        int ociPendientes = ocis.Count(x => DocumentoFiltroHelper.Normalizar(x.Estado) is "PENDIENTE" or "EMITIDA" or "EMITIDO");
        int otActivas = ots.Count(x => !EsCerradoOAnulado(x.Estado));
        int otParciales = ots.Count(x => DocumentoFiltroHelper.Normalizar(x.Estado) is "TERMINADO_PARCIAL" or "PARCIAL");
        DateTime inicioMes = new(DateTime.Today.Year, DateTime.Today.Month, 1);
        int guiasMes = guias.Count(x => x.FechaEmision >= inicioMes && !EsAnulado(x.Estado));
        int guiasAnuladas = guias.Count(x => EsAnulado(x.Estado));
        int productosBajos = productos.Count(x => x.StockActual <= 0);
        int insumosBajos = insumos.Count(x => x.StockActual <= 0);

        OciActivasLabel.Text = ociActivas.ToString();
        OciPendientesLabel.Text = $"{ociPendientes} pendientes";
        OtActivasLabel.Text = otActivas.ToString();
        OtParcialesLabel.Text = $"{otParciales} parciales";
        GuiasMesLabel.Text = guiasMes.ToString();
        GuiasAnuladasLabel.Text = $"{guiasAnuladas} anuladas";
        StockAlertasLabel.Text = (productosBajos + insumosBajos).ToString();
        StockResumenLabel.Text = $"{productosBajos} prod. | {insumosBajos} ins.";
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

    private static bool EsCerradoOAnulado(string estado)
    {
        string valor = DocumentoFiltroHelper.Normalizar(estado);
        return valor is "ENTREGADO" or "ENTREGADA" or "TERMINADO" or "TERMINADA" or "ANULADO" or "ANULADA";
    }

    private static bool EsAnulado(string estado) => DocumentoFiltroHelper.Normalizar(estado) is "ANULADO" or "ANULADA";

    private static string FormatearNumeroOc(string numero) =>
        numero.StartsWith("OCI-", StringComparison.OrdinalIgnoreCase)
            ? "OC-" + numero[4..]
            : numero;

    private static string TextoVacio(string? value) => string.IsNullOrWhiteSpace(value) ? "Sin dato" : value.Trim();

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
        await ConfirmarCerrarSesionAsync();
    }

    protected override bool OnBackButtonPressed()
    {
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

    private sealed record HomeActivityItem(
        string Tipo,
        string Titulo,
        string Detalle,
        Color Color,
        DateTime Fecha);
}
