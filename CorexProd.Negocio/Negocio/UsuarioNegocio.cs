using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Servicios;
using System.Collections.Generic;
using System.Linq;

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

        public string ObtenerNombrePersona(string nombreUsuario)
        {
            nombreUsuario = nombreUsuario?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(nombreUsuario))
                return string.Empty;

            Usuario? usuario = Listar()
                .FirstOrDefault(u => u.NombreUsuario.Equals(nombreUsuario, StringComparison.OrdinalIgnoreCase));

            if (usuario == null)
                return nombreUsuario;

            if (!string.IsNullOrWhiteSpace(usuario.NombreCompleto))
                return usuario.NombreCompleto.Trim();

            if (!string.IsNullOrWhiteSpace(usuario.NombreEmpleado))
                return usuario.NombreEmpleado.Trim();

            return usuario.NombreUsuario.Trim();
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

            string claveAprobacionLimpia = usuario.ClaveAprobacionOs?.Trim() ?? string.Empty;
            if (usuario.AprobacionOs)
            {
                if (usuario.IdUsuario == 0 && string.IsNullOrWhiteSpace(claveAprobacionLimpia))
                    return "Debe ingresar la clave de aprobacion OS.";

                if (!string.IsNullOrWhiteSpace(claveAprobacionLimpia) && !EsClaveAprobacionValida(claveAprobacionLimpia))
                    return "La clave de aprobacion OS debe tener 4 digitos numericos.";

                if (usuario.IdUsuario > 0 && string.IsNullOrWhiteSpace(claveAprobacionLimpia))
                {
                    Usuario? existente = _usuarioDatos.ObtenerPorId(usuario.IdUsuario);
                    if (existente == null || string.IsNullOrWhiteSpace(existente.ClaveAprobacionOs))
                        return "Debe ingresar la clave de aprobacion OS.";
                }
            }
            else
            {
                claveAprobacionLimpia = string.Empty;
            }

            if (usuario.IdUsuario == 0)
            {
                usuario.Clave = PasswordService.HashPassword(usuario.Clave);
                usuario.ClaveAprobacionOs = string.IsNullOrWhiteSpace(claveAprobacionLimpia)
                    ? string.Empty
                    : PasswordService.HashPassword(claveAprobacionLimpia);

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
            usuario.ClaveAprobacionOs = string.IsNullOrWhiteSpace(claveAprobacionLimpia)
                ? string.Empty
                : PasswordService.HashPassword(claveAprobacionLimpia);

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

        public string ValidarAprobacionOs(string nombreUsuario, string claveAprobacion)
        {
            return ResolverAprobadorOs(nombreUsuario, claveAprobacion).Mensaje;
        }

        public (string Mensaje, string UsuarioAprobador) ResolverAprobadorOs(string nombreUsuario, string claveAprobacion)
        {
            nombreUsuario = nombreUsuario?.Trim() ?? string.Empty;
            claveAprobacion = claveAprobacion?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(nombreUsuario))
                return ("Debe ingresar el usuario aprobador.", string.Empty);

            if (!EsClaveAprobacionValida(claveAprobacion))
                return ("La clave de aprobacion debe tener 4 digitos numericos.", string.Empty);

            Usuario? usuario = _usuarioDatos.ObtenerPorNombreUsuario(nombreUsuario)
                ?? Listar().FirstOrDefault(u =>
                    u.NombreUsuario.Equals(nombreUsuario, StringComparison.OrdinalIgnoreCase)
                    || u.NombreCompleto.Equals(nombreUsuario, StringComparison.OrdinalIgnoreCase)
                    || u.NombreEmpleado.Equals(nombreUsuario, StringComparison.OrdinalIgnoreCase));

            if (usuario != null
                && usuario.Estado
                && usuario.AprobacionOs
                && EsHashBcrypt(usuario.ClaveAprobacionOs)
                && PasswordService.VerifyPassword(claveAprobacion, usuario.ClaveAprobacionOs))
            {
                return ("OK", usuario.NombreUsuario);
            }

            Usuario? aprobadorPorClave = Listar()
                .Where(u => u.Estado && u.AprobacionOs && EsHashBcrypt(u.ClaveAprobacionOs))
                .FirstOrDefault(u => PasswordService.VerifyPassword(claveAprobacion, u.ClaveAprobacionOs));

            if (aprobadorPorClave != null)
                return ("OK", aprobadorPorClave.NombreUsuario);

            if (usuario == null)
                return ("No se encontro el usuario aprobador o la clave no pertenece a un aprobador OS activo.", string.Empty);

            if (!usuario.Estado)
                return ("El usuario aprobador esta inactivo.", string.Empty);

            if (!usuario.AprobacionOs)
                return ("El usuario no tiene permiso para aprobar ordenes de servicio.", string.Empty);

            if (string.IsNullOrWhiteSpace(usuario.ClaveAprobacionOs))
                return ("El usuario no tiene clave de aprobacion OS configurada.", string.Empty);

            if (!EsHashBcrypt(usuario.ClaveAprobacionOs))
                return ("La clave de aprobacion OS del usuario debe volver a configurarse.", string.Empty);

            return ("Clave de aprobacion incorrecta.", string.Empty);
        }

        public string Eliminar(int idUsuario)
        {
            if (idUsuario <= 0)
                return "Debe seleccionar un usuario válido";

            return _usuarioDatos.Eliminar(idUsuario);
        }

        public string Desactivar(int idUsuario)
        {
            if (idUsuario <= 0)
                return "Debe seleccionar un usuario válido";

            return _usuarioDatos.Desactivar(idUsuario);
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

            Usuario? usuarioDB = _usuarioDatos.ObtenerPorId(idUsuario);

            if (usuarioDB == null)
                return "No se encontró el usuario en sesión.";

            if (!usuarioDB.Estado)
                return "El usuario se encuentra inactivo.";

            if (!PasswordService.VerifyPassword(claveActual, usuarioDB.Clave))
                return "La clave actual no es correcta.";

            string claveNuevaHash = PasswordService.HashPassword(claveNueva);

            return _usuarioDatos.CambiarClave(idUsuario, usuarioDB.Clave, claveNuevaHash);
        }

        private static bool EsClaveAprobacionValida(string clave) =>
            clave.Length == 4 && clave.All(char.IsDigit);

        private static bool EsHashBcrypt(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return false;

            return valor.StartsWith("$2a$", StringComparison.Ordinal)
                || valor.StartsWith("$2b$", StringComparison.Ordinal)
                || valor.StartsWith("$2x$", StringComparison.Ordinal)
                || valor.StartsWith("$2y$", StringComparison.Ordinal);
        }
    }
}
