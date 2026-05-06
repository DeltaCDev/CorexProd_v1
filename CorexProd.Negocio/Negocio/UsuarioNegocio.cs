using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;

namespace CorexProd.Negocio.Negocio
{
    public class UsuarioNegocio
    {
        private readonly UsuarioDatos _usuarioDatos;

        public UsuarioNegocio()
        {
            _usuarioDatos = new UsuarioDatos();
        }

        public Usuario Login(string usuario, string clave)
        {
            if (string.IsNullOrWhiteSpace(usuario))
            {
                throw new Exception("Ingrese el usuario.");
            }

            if (string.IsNullOrWhiteSpace(clave))
            {
                throw new Exception("Ingrese la contraseña.");
            }

            Usuario usuarioEncontrado =
                _usuarioDatos.Login(usuario, clave);

            if (usuarioEncontrado == null)
            {
                throw new Exception(
                    "Usuario o contraseña incorrectos.");
            }

            return usuarioEncontrado;
        }
    }
}