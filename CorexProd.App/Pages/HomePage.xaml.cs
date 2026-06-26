using CorexProd.App.Services;

namespace CorexProd.App.Pages;

public partial class HomePage : ContentPage
{
    private readonly SessionState _session;

    public HomePage()
    {
        InitializeComponent();
        _session = ServiceHelper.GetRequiredService<SessionState>();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (!_session.EstaAutenticado)
        {
            Shell.Current.GoToAsync("//login");
            return;
        }

        UserLabel.Text = _session.Usuario?.NombreCompleto;
        RoleLabel.Text = $"{_session.Usuario?.NombreUsuario} · {_session.Usuario?.NombreRol}";
        MenusView.ItemsSource = _session.Menus;
    }

    private async void OnProductosClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(StockProductosPage));
    }

    private async void OnInsumosClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(StockInsumosPage));
    }

    private async void OnProformasClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ProformasPage));
    }

    private async void OnOciClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(OciPage));
    }

    private async void OnIngresoStockClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(IngresoManualStockPage));
    }

    private async void OnOrdenesTrabajoClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(OrdenesTrabajoPage));
    }

    private async void OnKardexClicked(object? sender, EventArgs e)
    {
        await AbrirModuloAsync("Kardex");
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        _session.Cerrar();
        await Shell.Current.GoToAsync("//login");
    }

    protected override bool OnBackButtonPressed()
    {
        Dispatcher.Dispatch(async () =>
        {
            string action = await DisplayActionSheetAsync("¿Qué deseas hacer?", "Cancelar", null, "Cerrar sesión", "Salir de la app");
            if (action == "Cerrar sesión")
            {
                _session.Cerrar();
                await Shell.Current.GoToAsync("//login");
            }
            else if (action == "Salir de la app")
            {
                Application.Current?.Quit();
            }
        });

        return true;
    }

    private static Task AbrirModuloAsync(string titulo)
    {
        return Shell.Current.GoToAsync($"{nameof(ModuloPage)}?titulo={Uri.EscapeDataString(titulo)}");
    }
}
