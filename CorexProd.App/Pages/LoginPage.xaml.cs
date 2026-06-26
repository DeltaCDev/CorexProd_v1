using CorexProd.App.Services;

namespace CorexProd.App.Pages;

public partial class LoginPage : ContentPage
{
    private readonly CorexProdApiClient _apiClient;
    private readonly SessionState _session;
    private int _apiLogoTapCount;
    private DateTime _lastApiLogoTap;

    public LoginPage()
    {
        InitializeComponent();
        _apiClient = ServiceHelper.GetRequiredService<CorexProdApiClient>();
        _session = ServiceHelper.GetRequiredService<SessionState>();
        ServerEntry.Text = _apiClient.BaseUrl;
    }

    private async void OnTestConnectionClicked(object? sender, EventArgs e)
    {
        await RunBusyAsync(async () =>
        {
            SaveServerUrl();
            var health = await _apiClient.GetHealthAsync();
            ServerStatusLabel.Text = $"Conectado a {health.BaseDatos} en {health.Servidor}.";
            ServerStatusLabel.TextColor = Color.FromArgb("#067647");
            MessageLabel.Text = string.Empty;
        });
    }

    private void OnApiIconTapped(object? sender, TappedEventArgs e)
    {
        DateTime now = DateTime.Now;
        _apiLogoTapCount = (now - _lastApiLogoTap).TotalSeconds > 3 ? 1 : _apiLogoTapCount + 1;
        _lastApiLogoTap = now;

        if (_apiLogoTapCount < 4)
            return;

        _apiLogoTapCount = 0;
        ApiPanel.IsVisible = !ApiPanel.IsVisible;
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        await LoginAsync();
    }

    private async void OnPasswordCompleted(object? sender, EventArgs e)
    {
        await LoginAsync();
    }

    private async Task LoginAsync()
    {
        await RunBusyAsync(async () =>
        {
            SaveServerUrl();
            string usuario = UserEntry.Text?.Trim() ?? string.Empty;
            string clave = PasswordEntry.Text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(clave))
            {
                MessageLabel.Text = "Ingrese usuario y contraseña.";
                return;
            }

            var response = await _apiClient.LoginAsync(usuario, clave);
            _session.Iniciar(response);
            MessageLabel.Text = string.Empty;
            PasswordEntry.Text = string.Empty;
            await Shell.Current.GoToAsync(nameof(HomePage));
        });
    }

    private async Task RunBusyAsync(Func<Task> action)
    {
        try
        {
            SetBusy(true);
            await action();
        }
        catch (Exception ex)
        {
            MessageLabel.Text = ex.Message;
            ServerStatusLabel.TextColor = Color.FromArgb("#B42318");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SaveServerUrl()
    {
        _apiClient.BaseUrl = ServerEntry.Text ?? string.Empty;
        ServerEntry.Text = _apiClient.BaseUrl;
    }

    private void SetBusy(bool isBusy)
    {
        BusyIndicator.IsVisible = isBusy;
        BusyIndicator.IsRunning = isBusy;
        LoginButton.IsEnabled = !isBusy;
    }
}
