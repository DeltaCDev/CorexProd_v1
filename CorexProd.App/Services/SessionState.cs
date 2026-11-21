using CorexProd.App.Models;

namespace CorexProd.App.Services;

public sealed class SessionState
{
    public LoginUser? Usuario { get; private set; }
    public IReadOnlyList<string> Menus { get; private set; } = [];
    public IReadOnlyList<MenuCatalogItem> CatalogoMenus { get; private set; } = [];
    public bool EsDemo { get; private set; }
    public bool EstaAutenticado => Usuario != null;

    public void Iniciar(LoginResponse response)
    {
        Usuario = response.Usuario;
        Menus = response.Menus;
        CatalogoMenus = response.CatalogoMenus ?? [];
        EsDemo = false;
    }

    public void IniciarDemo()
    {
        Usuario = new LoginUser(0, "Demo", "Usuario Demo", 0, "Demostracion");
        Menus =
        [
            "Ventas",
            "OC",
            "Guia Interna",
            "Produccion",
            "OT",
            "Reportes",
            "Kardex",
            "Almacen",
            "Stock productos",
            "Stock insumos",
            "Ingreso stock"
        ];
        CatalogoMenus = [];
        EsDemo = true;
    }

    public void Cerrar()
    {
        Usuario = null;
        Menus = [];
        CatalogoMenus = [];
        EsDemo = false;
    }
}
