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

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        _session.Cerrar();
        await Shell.Current.GoToAsync("//login");
    }
}
