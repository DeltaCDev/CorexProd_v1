using CorexProd.App.Models;
using CorexProd.App.Services;
using System.Collections.ObjectModel;

namespace CorexProd.App.Pages;

[QueryProperty(nameof(Modulo), "modulo")]
public partial class SeguridadPage : ContentPage
{
    private readonly CorexProdApiClient _api;
    private readonly ObservableCollection<SeguridadItem> _items = [];
    private readonly string[] _modulos = ["Parámetros", "Roles", "Usuarios", "Empleados"];

    public SeguridadPage(CorexProdApiClient api)
    {
        InitializeComponent();
        _api = api;
        Lista.ItemsSource = _items;
        ModuloPicker.ItemsSource = _modulos;
        ModuloPicker.SelectedIndex = 0;
    }

    public string Modulo
    {
        set
        {
            string modulo = Uri.UnescapeDataString(value ?? string.Empty);
            int indice = Array.FindIndex(_modulos, x => x.Equals(modulo, StringComparison.OrdinalIgnoreCase));
            if (indice >= 0 && ModuloPicker is not null)
                ModuloPicker.SelectedIndex = indice;
        }
    }

    private async void OnModuloChanged(object? sender, EventArgs e) => await CargarAsync();
    private async void OnRefresh(object? sender, EventArgs e) => await CargarAsync();
    private async void OnBuscarChanged(object? sender, TextChangedEventArgs e) => Filtrar();

    private async Task CargarAsync()
    {
        if (ModuloPicker.SelectedIndex < 0) return;
        Cargando.IsVisible = true;
        Cargando.IsRunning = true;
        Refrescar.IsRefreshing = false;
        try
        {
            _items.Clear();
            switch (ModuloPicker.SelectedIndex)
            {
                case 0:
                    foreach (ParametroMovil x in await _api.GetParametrosAsync())
                        _items.Add(new(x.NombreParametro, $"Código: {x.CodigoParametro} | Valor: {x.ValorParametro}", x.Estado ? "Activo" : "Inactivo"));
                    break;
                case 1:
                    foreach (RolMovil x in await _api.GetRolesAsync())
                        _items.Add(new(x.NombreRol, $"Registrado: {x.FechaRegistro:dd/MM/yyyy}", x.Estado ? "Activo" : "Inactivo"));
                    break;
                case 2:
                    foreach (UsuarioMovil x in await _api.GetUsuariosAsync())
                        _items.Add(new(x.NombreUsuario, $"{x.NombreCompleto} | Rol: {x.NombreRol}", x.Estado ? "Activo" : "Inactivo"));
                    break;
                case 3:
                    foreach (EmpleadoMovil x in await _api.GetEmpleadosAsync())
                        _items.Add(new(x.NombreCompleto, $"{x.TipoDocumento}: {x.NumeroDocumento} | Cargo: {x.NombreCargo}", x.Estado ? "Activo" : "Inactivo"));
                    break;
            }
            Filtrar();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Seguridad", ex.Message, "OK");
        }
        finally
        {
            Cargando.IsRunning = false;
            Cargando.IsVisible = false;
            Refrescar.IsRefreshing = false;
        }
    }

    private void Filtrar()
    {
        string texto = BuscarEntry.Text?.Trim() ?? string.Empty;
        Lista.ItemsSource = string.IsNullOrWhiteSpace(texto)
            ? _items
            : _items.Where(x => $"{x.Titulo} {x.Detalle} {x.Estado}".Contains(texto, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private sealed record SeguridadItem(string Titulo, string Detalle, string Estado);
}
