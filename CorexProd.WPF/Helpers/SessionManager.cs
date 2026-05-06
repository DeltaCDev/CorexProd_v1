using CorexProd.Entidad.Entidades;

namespace CorexProd.WPF.Helpers
{
    public static class SessionManager
    {
        public static Usuario UsuarioActual { get; set; }

        public static bool EstaLogueado()
        {
            return UsuarioActual != null;
        }

        public static void CerrarSesion()
        {
            UsuarioActual = null;
        }
    }
}