using CorexProd.App.Pages;
using CorexProd.App.Services;
using CorexProd.App.Models;
using Microsoft.Maui.Controls.Shapes;

namespace CorexProd.App.Controls;

public partial class SidebarMenuView : ContentView
{
    private readonly SessionState _session;

    public SidebarMenuView()
    {
        InitializeComponent();
        _session = ServiceHelper.GetRequiredService<SessionState>();
        CargarUsuario();
        CargarMenu();
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();
        CargarUsuario();
        CargarMenu();
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

    private void CargarMenu()
    {
        if (MenuContainer is null)
            return;

        MenuContainer.Children.Clear();
        AgregarInicio();

        IReadOnlyList<MenuCatalogItem> catalogo = _session.CatalogoMenus;
        if (catalogo.Count == 0)
        {
            // Compatibilidad con sesiones/API anteriores al catálogo jerárquico.
            AgregarMensaje("Actualice la aplicación para sincronizar el menú.", "#FFD166");
            return;
        }

        Dictionary<int, MenuCatalogItem> padres = catalogo
            .Where(x => x.IdMenuPadre is null && x.TienePermiso)
            .OrderBy(x => x.Orden)
            .ToDictionary(x => x.IdMenu);

        foreach (MenuCatalogItem padre in padres.Values)
        {
            List<MenuCatalogItem> hijos = catalogo
                .Where(x => x.IdMenuPadre == padre.IdMenu && x.TienePermiso)
                .OrderBy(x => x.Orden)
                .ToList();

            if (padre.NombreMenu.Equals("Inicio", StringComparison.OrdinalIgnoreCase))
                continue;

            AgregarGrupo(padre, hijos);
        }
    }

    private void AgregarInicio()
    {
        Border item = CrearItem("⌂", "Inicio", Color.FromArgb("#A97BFF"), 16);
        item.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(CerrarDrawer) });
        MenuContainer.Children.Add(item);
    }

    private void AgregarGrupo(MenuCatalogItem padre, List<MenuCatalogItem> hijos)
    {
        Border cabecera = CrearItem(IconoGrupo(padre.NombreMenu), padre.NombreMenu, ColorGrupo(padre.NombreMenu), 17);
        VerticalStackLayout lista = new()
        {
            Spacing = 2,
            Margin = new Thickness(20, -4, 0, 4),
            IsVisible = false
        };
        cabecera.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() => lista.IsVisible = !lista.IsVisible)
        });
        MenuContainer.Children.Add(cabecera);

        if (hijos.Count == 0)
        {
            lista.Children.Add(CrearSubItem(padre.NombreMenu, padre.NombreMenu));
        }
        else
        {
            foreach (MenuCatalogItem hijo in hijos)
                lista.Children.Add(CrearSubItem(hijo.NombreMenu, hijo.NombreMenu));
        }

        MenuContainer.Children.Add(lista);
    }

    private Border CrearItem(string icono, string texto, Color color, double fontSize)
    {
        Border border = new()
        {
            BackgroundColor = Colors.Transparent,
            StrokeThickness = 0,
            Padding = new Thickness(15, 10)
        };
        border.StrokeShape = new RoundRectangle { CornerRadius = 12 };
        Grid grid = new() { ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition { Width = 38 }, new ColumnDefinition { Width = GridLength.Star } }, ColumnSpacing = 12 };
        grid.Children.Add(new Label { Text = icono, FontSize = 22, TextColor = color, VerticalTextAlignment = TextAlignment.Center });
        grid.Children.Add(new Label { Text = texto, FontSize = fontSize, FontFamily = "OpenSansSemibold", TextColor = Colors.White, VerticalTextAlignment = TextAlignment.Center });
        grid.SetColumn(grid.Children[1], 1);
        border.Content = grid;
        return border;
    }

    private View CrearSubItem(string nombre, string titulo)
    {
        Button boton = new()
        {
            Text = $"  {nombre}",
            HorizontalOptions = LayoutOptions.Fill,
            TextColor = Colors.White,
            BackgroundColor = Colors.Transparent,
            FontSize = 14,
            Padding = new Thickness(10, 8)
        };
        boton.Clicked += async (_, _) => await AbrirModuloAsync(titulo);
        return boton;
    }

    private async Task AbrirModuloAsync(string nombre)
    {
        CerrarDrawer();
        string ruta = nombre switch
        {
            "Orden de Compra" => nameof(OciPage),
            "Guía de Salida" => nameof(GuiaInternaPage),
            "Orden de Trabajo" => nameof(OrdenesTrabajoPage),
            "Stock Productos" => nameof(StockProductosPage),
            "Stock Insumos" => nameof(StockInsumosPage),
            "Entrada Manual de Productos" => nameof(IngresoManualStockPage),
            "Parámetros" => $"{nameof(SeguridadPage)}?modulo={Uri.EscapeDataString("Parámetros")}",
            "Roles" => $"{nameof(SeguridadPage)}?modulo={Uri.EscapeDataString("Roles")}",
            "Usuarios" => $"{nameof(SeguridadPage)}?modulo={Uri.EscapeDataString("Usuarios")}",
            "Empleados" => $"{nameof(SeguridadPage)}?modulo={Uri.EscapeDataString("Empleados")}",
            _ => $"{nameof(ModuloPage)}?titulo={Uri.EscapeDataString(nombre + " (En desarrollo)")}"
        };
        await Shell.Current.GoToAsync(ruta);
    }

    private void AgregarMensaje(string texto, string color) =>
        MenuContainer.Children.Add(new Label { Text = texto, TextColor = Color.FromArgb(color), FontSize = 13, Margin = new Thickness(15, 10) });

    private static string IconoGrupo(string nombre) => nombre switch
    {
        "Ventas" => "🛒",
        "Producción" => "▥",
        "Almacén" => "□",
        "Reportes" => "▥",
        "Productos" => "▦",
        "Seguridad" => "♙",
        _ => "▣"
    };

    private static Color ColorGrupo(string nombre) => nombre switch
    {
        "Producción" => Color.FromArgb("#55B6FF"),
        "Almacén" => Color.FromArgb("#F6B943"),
        "Reportes" => Color.FromArgb("#52D68B"),
        _ => Color.FromArgb("#A97BFF")
    };

    private async Task NavegarAsync(string ruta)
    {
        CerrarDrawer();
        await Shell.Current.GoToAsync(ruta);
    }

    private async void OnLogoutTapped(object? sender, TappedEventArgs e)
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
