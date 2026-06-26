using CorexProd.App.Models;

namespace CorexProd.App.Services;

public sealed class SessionState
{
    public LoginUser? Usuario { get; private set; }
    public IReadOnlyList<string> Menus { get; private set; } = [];
    public bool EstaAutenticado => Usuario != null;

    public void Iniciar(LoginResponse response)
    {
        Usuario = response.Usuario;
        Menus = response.Menus;
    }

    public void Cerrar()
    {
        Usuario = null;
        Menus = [];
    }
}
