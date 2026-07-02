using CorexProd.App.Models;
using CorexProd.App.Services;

namespace CorexProd.App.Pages;

public partial class HomePage : ContentPage
{
    private readonly CorexProdApiClient _apiClient;
    private readonly SessionState _session;

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
        await CargarEmpresaAsync();
    }

    private static IReadOnlyList<string> OrdenarMenus(IReadOnlyList<string> menus)
    {
        string[] orden =
        [
            "Ventas",
            "Proformas",
            "OCI",
            "Guia Interna",
            "Produccion",
            "OT Produccion",
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
            .OrderBy(x => posiciones.TryGetValue(x, out int posicion) ? posicion : int.MaxValue)
            .ThenBy(x => x)
            .ToList();
    }

    private async Task CargarEmpresaAsync()
    {
        try
        {
            EmpresaInfo empresa = _session.EsDemo ? DemoData.Empresa : await _apiClient.GetEmpresaAsync();
            string nombre = string.IsNullOrWhiteSpace(empresa.Nombre) ? "CorexProd" : empresa.Nombre.Trim();
            WelcomeLabel.Text = $"Bienvenido a {nombre}";

            if (!string.IsNullOrWhiteSpace(empresa.LogoBase64))
            {
                byte[] bytes = Convert.FromBase64String(empresa.LogoBase64);
                CompanyLogo.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
            }
        }
        catch
        {
            WelcomeLabel.Text = "Bienvenido a CorexProd";
        }
    }

    private async void OnProductosClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(StockProductosPage));

    private async void OnInsumosClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(StockInsumosPage));

    private async void OnProformasClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(ProformasPage));

    private async void OnOciClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(OciPage));

    private async void OnIngresoStockClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(IngresoManualStockPage));

    private async void OnOrdenesTrabajoClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(OrdenesTrabajoPage));

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
}
