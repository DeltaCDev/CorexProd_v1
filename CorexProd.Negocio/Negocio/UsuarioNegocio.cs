using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Servicios;
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

            string usuarioLimpio = usuario.Trim();

            Usuario? usuarioDB = _usuarioDatos.Login(usuarioLimpio);

            if (usuarioDB == null)
            {
                AuditoriaService.Registrar(
                    usuarioLimpio,
                    "LOGIN FALLIDO",
                    "SEGURIDAD",
                    "Intento de inicio de sesión con usuario inexistente");

                return null;
            }

            bool claveCorrecta = PasswordService.VerifyPassword(clave.Trim(), usuarioDB.Clave);

            if (!claveCorrecta)
            {
                AuditoriaService.Registrar(
                    usuarioLimpio,
                    "LOGIN FALLIDO",
                    "SEGURIDAD",
                    "Contraseña incorrecta");

                return null;
            }

            if (!usuarioDB.Estado)
            {
                AuditoriaService.Registrar(
                    usuarioLimpio,
                    "LOGIN FALLIDO",
                    "SEGURIDAD",
                    "Usuario inactivo intentó iniciar sesión");

                return null;
            }

            AuditoriaService.Registrar(
                usuarioDB.NombreUsuario,
                "LOGIN",
                "SEGURIDAD",
                "Inicio de sesión correcto");

            return usuarioDB;
        }

        public List<Usuario> Listar()
        {
            return _usuarioDatos.Listar();
        }

        public string Guardar(Usuario usuario, string usuarioAuditoria)
        {
            usuario.NombreUsuario = usuario.NombreUsuario.Trim();
            usuario.Clave = usuario.Clave?.Trim() ?? string.Empty;

            if (usuario.IdEmpleado <= 0)
                return "Debe seleccionar un empleado";

            if (string.IsNullOrWhiteSpace(usuario.NombreUsuario))
                return "El nombre de usuario es obligatorio";

            if (usuario.IdUsuario == 0 && string.IsNullOrWhiteSpace(usuario.Clave))
                return "La clave es obligatoria";

            if (usuario.IdRol <= 0)
                return "Debe seleccionar un rol";

            if (usuario.IdUsuario == 0)
            {
                usuario.Clave = PasswordService.HashPassword(usuario.Clave);

                string mensajeRegistro = _usuarioDatos.Registrar(usuario);

                if (mensajeRegistro.Contains("correctamente"))
                {
                    AuditoriaService.Registrar(
                        usuarioAuditoria,
                        "CREAR",
                        "USUARIOS",
                        $"Se registró el usuario: {usuario.NombreUsuario}");
                }

                return mensajeRegistro;
            }

            if (!string.IsNullOrWhiteSpace(usuario.Clave))
            {
                usuario.Clave = PasswordService.HashPassword(usuario.Clave);
            }

            string mensajeEdicion = _usuarioDatos.Editar(usuario);

            if (mensajeEdicion.Contains("correctamente"))
            {
                AuditoriaService.Registrar(
                    usuarioAuditoria,
                    "EDITAR",
                    "USUARIOS",
                    $"Se actualizó el usuario: {usuario.NombreUsuario}");
            }

            return mensajeEdicion;
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

            string claveNuevaHash = PasswordService.HashPassword(claveNueva);

            return _usuarioDatos.CambiarClave(idUsuario, claveActual, claveNuevaHash);
        }
    }
}