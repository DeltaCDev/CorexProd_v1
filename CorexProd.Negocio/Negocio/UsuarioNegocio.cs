using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System.Collections.Generic;

namespace CorexProd.Negocio.Negocio
{
    public class UsuarioNegocio
    {
        private readonly UsuarioDatos _usuarioDatos = new();

        public Usuario? Login(string usuario, string clave)
        {
            if (string.IsNullOrWhiteSpace(usuario))
                return null;

            if (string.IsNullOrWhiteSpace(clave))
                return null;

            return _usuarioDatos.Login(usuario.Trim(), clave.Trim());
        }

        public List<Usuario> Listar()
        {
            return _usuarioDatos.Listar();
        }

        public string Guardar(Usuario usuario)
        {
            usuario.NombreUsuario = usuario.NombreUsuario.Trim();
            usuario.Clave = usuario.Clave.Trim();

            if (usuario.IdEmpleado <= 0)
                return "Debe seleccionar un empleado";

            if (string.IsNullOrWhiteSpace(usuario.NombreUsuario))
                return "El nombre de usuario es obligatorio";

            if (string.IsNullOrWhiteSpace(usuario.Clave))
                return "La clave es obligatoria";

            if (usuario.IdRol <= 0)
                return "Debe seleccionar un rol";

            if (usuario.IdUsuario == 0)
                return _usuarioDatos.Registrar(usuario);

            return _usuarioDatos.Editar(usuario);
        }

        public string Eliminar(int idUsuario)
        {
            if (idUsuario <= 0)
                return "Debe seleccionar un usuario válido";

            return _usuarioDatos.Eliminar(idUsuario);
        }
        public string CambiarClave(int idUsuario, string claveActual, string claveNueva, string confirmarClave)
        {
            claveActual = claveActual.Trim();
            claveNueva = claveNueva.Trim();
            confirmarClave = confirmarClave.Trim();

            if (idUsuario <= 0)
                return "No hay usuario en sesión.";

            if (string.IsNullOrWhiteSpace(claveActual))
                return "Ingrese la clave actual.";

            if (string.IsNullOrWhiteSpace(claveNueva))
                return "Ingrese la nueva clave.";

            if (string.IsNullOrWhiteSpace(confirmarClave))
                return "Confirme la nueva clave.";

            if (claveNueva != confirmarClave)
                return "La nueva clave y la confirmación no coinciden.";

            if (claveNueva.Length < 4)
                return "La nueva clave debe tener al menos 4 caracteres.";

            if (claveActual == claveNueva)
                return "La nueva clave debe ser diferente a la clave actual.";

            return _usuarioDatos.CambiarClave(idUsuario, claveActual, claveNueva);
        }
    }
}