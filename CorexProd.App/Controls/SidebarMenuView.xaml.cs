using CorexProd.App.Pages;
using CorexProd.App.Services;

namespace CorexProd.App.Controls;

public partial class SidebarMenuView : ContentView
{
    private readonly SessionState _session;

    public SidebarMenuView()
    {
        InitializeComponent();
        _session = ServiceHelper.GetRequiredService<SessionState>();
        CargarUsuario();
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();
        CargarUsuario();
    }

    private void CargarUsuario()
    {
        UserNameLabel.Text = string.IsNullOrWhiteSpace(_session.Usuario?.NombreCompleto)
            ? "Usuario"
            : _session.Usuario.NombreCompleto;

        UserRoleLabel.Text = string.IsNullOrWhiteSpace(_session.Usuario?.NombreRol)
            ? "Sin rol asignado"
            : _session.Usuario.NombreRol;
    }

    private void OnCloseClicked(object? sender, EventArgs e) => CerrarDrawer();

    private void OnInicioTapped(object? sender, TappedEventArgs e)
    {
        CerrarDrawer();
    }

    private void OnVentasTapped(object? sender, TappedEventArgs e)
    {
        bool abrir = !VentasChildren.IsVisible;
        CerrarGrupos();
        VentasChildren.IsVisible = abrir;
        VentasArrow.Text = abrir ? "⌄" : "›";
        VentasHeader.BackgroundColor = abrir ? Color.FromArgb("#4C2ACB") : Colors.Transparent;
    }

    private void OnProduccionTapped(object? sender, TappedEventArgs e)
    {
        bool abrir = !ProduccionChildren.IsVisible;
        CerrarGrupos();
        ProduccionChildren.IsVisible = abrir;
        ProduccionArrow.Text = abrir ? "⌄" : "›";
    }

    private void OnAlmacenTapped(object? sender, TappedEventArgs e)
    {
        bool abrir = !AlmacenChildren.IsVisible;
        CerrarGrupos();
        AlmacenChildren.IsVisible = abrir;
        AlmacenArrow.Text = abrir ? "⌄" : "›";
    }

    private void OnReportesTapped(object? sender, TappedEventArgs e)
    {
        bool abrir = !ReportesChildren.IsVisible;
        CerrarGrupos();
        ReportesChildren.IsVisible = abrir;
        ReportesArrow.Text = abrir ? "⌄" : "›";
    }

    private void CerrarGrupos()
    {
        VentasChildren.IsVisible = false;
        ProduccionChildren.IsVisible = false;
        AlmacenChildren.IsVisible = false;
        ReportesChildren.IsVisible = false;

        VentasArrow.Text = "›";
        ProduccionArrow.Text = "›";
        AlmacenArrow.Text = "›";
        ReportesArrow.Text = "›";
        VentasHeader.BackgroundColor = Colors.Transparent;
    }

    private async void OnOrdenesCompraTapped(object? sender, TappedEventArgs e) =>
        await NavegarAsync(nameof(OciPage));

    private async void OnGuiasInternasTapped(object? sender, TappedEventArgs e) =>
        await NavegarAsync(nameof(GuiaInternaPage));

    private async void OnOrdenesTrabajoClicked(object? sender, EventArgs e) =>
        await NavegarAsync(nameof(OrdenesTrabajoPage));

    private async void OnOtManualClicked(object? sender, EventArgs e) =>
        await NavegarAsync(nameof(OrdenTrabajoManualPage));

    private async void OnStockProductosClicked(object? sender, EventArgs e) =>
        await NavegarAsync(nameof(StockProductosPage));

    private async void OnStockInsumosClicked(object? sender, EventArgs e) =>
        await NavegarAsync(nameof(StockInsumosPage));

    private async void OnIngresoStockClicked(object? sender, EventArgs e) =>
        await NavegarAsync(nameof(IngresoManualStockPage));

    private async void OnKardexClicked(object? sender, EventArgs e) =>
        await NavegarAsync($"{nameof(ModuloPage)}?titulo={Uri.EscapeDataString("Kardex")}");

    private async void OnConfiguracionTapped(object? sender, TappedEventArgs e) =>
        await NavegarAsync($"{nameof(ModuloPage)}?titulo={Uri.EscapeDataString("Configuración")}");

    private async Task NavegarAsync(string ruta)
    {
        CerrarDrawer();
        await Shell.Current.GoToAsync(ruta);
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        bool confirmar = await Shell.Current.DisplayAlert(
            "Cerrar sesión",
            "¿Está seguro de que desea cerrar sesión?",
            "Sí",
            "No");

        if (!confirmar)
            return;

        CerrarDrawer();
        _session.Cerrar();
        await Shell.Current.GoToAsync("//login");
    }

    private void CerrarDrawer()
    {
        Element? elemento = this;

        while (elemento is not null)
        {
            if (elemento is VisualElement visual && visual.StyleId == "MenuDrawerOverlay")
            {
                visual.IsVisible = false;
                return;
            }

            elemento = elemento.Parent;
        }
    }
}
