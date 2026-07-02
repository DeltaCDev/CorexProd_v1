using CorexProd.App.Models;

namespace CorexProd.App.Services;

public sealed class SessionState
{
    public LoginUser? Usuario { get; private set; }
    public IReadOnlyList<string> Menus { get; private set; } = [];
    public bool EsDemo { get; private set; }
    public bool EstaAutenticado => Usuario != null;

    public void Iniciar(LoginResponse response)
    {
        Usuario = response.Usuario;
        Menus = response.Menus;
        EsDemo = false;
    }

    public void IniciarDemo()
    {
        Usuario = new LoginUser(0, "Demo", "Usuario Demo", 0, "Demostracion");
        Menus =
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
        EsDemo = true;
    }

    public void Cerrar()
    {
        Usuario = null;
        Menus = [];
        EsDemo = false;
    }
}
