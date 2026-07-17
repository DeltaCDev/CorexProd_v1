using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class UsuarioDatos
    {
        public Usuario? Login(string usuario)
        {
            Usuario? obj = null;

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_SEG_USUARIO_LOGIN", conexion);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Usuario", usuario);

            conexion.Open();
            AsegurarColumnasAprobacionOs(conexion);

            using SqlDataReader dr = cmd.ExecuteReader();

            if (dr.Read())
            {
                obj = new Usuario
                {
                    IdUsuario = Convert.ToInt32(dr["IdUsuario"]),
                    NombreUsuario = dr["NombreUsuario"]?.ToString() ?? string.Empty,
                    Clave = dr["Clave"]?.ToString() ?? string.Empty,
                    NombreCompleto = dr["NombreCompleto"]?.ToString() ?? string.Empty,
                    IdRol = Convert.ToInt32(dr["IdRol"]),
                    NombreRol = dr["NombreRol"]?.ToString() ?? string.Empty,
                    Estado = Convert.ToBoolean(dr["Estado"]),
                    AprobacionOs = TieneColumna(dr, "AprobacionOs") && Convert.ToBoolean(dr["AprobacionOs"]),
                    ClaveAprobacionOs = TieneColumna(dr, "ClaveAprobacionOs") ? dr["ClaveAprobacionOs"]?.ToString() ?? string.Empty : string.Empty
                };
            }

            return obj;
        }

        public List<Usuario> Listar()
        {
            List<Usuario> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_SEG_USUARIO_LISTAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            conexion.Open();
            AsegurarColumnasAprobacionOs(conexion);

            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new Usuario
                {
                    IdUsuario = Convert.ToInt32(dr["IdUsuario"]),
                    IdEmpleado = Convert.ToInt32(dr["IdEmpleado"]),
                    NombreEmpleado = dr["NombreEmpleado"]?.ToString() ?? string.Empty,
                    NombreUsuario = dr["NombreUsuario"]?.ToString() ?? string.Empty,
                    Clave = dr["Clave"]?.ToString() ?? string.Empty,
                    IdRol = Convert.ToInt32(dr["IdRol"]),
                    NombreRol = dr["NombreRol"]?.ToString() ?? string.Empty,
                    FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"]),
                    Estado = Convert.ToBoolean(dr["Estado"]),
                    AprobacionOs = TieneColumna(dr, "AprobacionOs") && Convert.ToBoolean(dr["AprobacionOs"]),
                    ClaveAprobacionOs = TieneColumna(dr, "ClaveAprobacionOs") ? dr["ClaveAprobacionOs"]?.ToString() ?? string.Empty : string.Empty
                });
            }

            dr.Close();
            CompletarAprobacionOs(conexion, lista);

            return lista;
        }

        public Usuario? ObtenerPorId(int idUsuario)
        {
            Usuario? obj = null;

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new(
                "SELECT IdUsuario, NombreUsuario, Clave, Estado, AprobacionOs, ClaveAprobacionOs FROM Usuarios WHERE IdUsuario = @IdUsuario",
                conexion);

            cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);

            conexion.Open();
            AsegurarColumnasAprobacionOs(conexion);

            using SqlDataReader dr = cmd.ExecuteReader();

            if (dr.Read())
            {
                obj = new Usuario
                {
                    IdUsuario = Convert.ToInt32(dr["IdUsuario"]),
                    NombreUsuario = dr["NombreUsuario"]?.ToString() ?? string.Empty,
                    Clave = dr["Clave"]?.ToString() ?? string.Empty,
                    Estado = Convert.ToBoolean(dr["Estado"]),
                    AprobacionOs = Convert.ToBoolean(dr["AprobacionOs"]),
                    ClaveAprobacionOs = dr["ClaveAprobacionOs"]?.ToString() ?? string.Empty
                };
            }

            return obj;
        }

        public Usuario? ObtenerPorNombreUsuario(string nombreUsuario)
        {
            Usuario? obj = null;

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new(
                "SELECT IdUsuario, NombreUsuario, Clave, Estado, AprobacionOs, ClaveAprobacionOs FROM Usuarios WHERE NombreUsuario = @NombreUsuario",
                conexion);

            cmd.Parameters.AddWithValue("@NombreUsuario", nombreUsuario);

            conexion.Open();
            AsegurarColumnasAprobacionOs(conexion);

            using SqlDataReader dr = cmd.ExecuteReader();

            if (dr.Read())
            {
                obj = new Usuario
                {
                    IdUsuario = Convert.ToInt32(dr["IdUsuario"]),
                    NombreUsuario = dr["NombreUsuario"]?.ToString() ?? string.Empty,
                    Clave = dr["Clave"]?.ToString() ?? string.Empty,
                    Estado = Convert.ToBoolean(dr["Estado"]),
                    AprobacionOs = Convert.ToBoolean(dr["AprobacionOs"]),
                    ClaveAprobacionOs = dr["ClaveAprobacionOs"]?.ToString() ?? string.Empty
                };
            }

            return obj;
        }

        public string Registrar(Usuario usuario)
        {
            string mensaje = string.Empty;

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_SEG_USUARIO_REGISTRAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@IdEmpleado", usuario.IdEmpleado);
            cmd.Parameters.AddWithValue("@NombreUsuario", usuario.NombreUsuario);
            cmd.Parameters.AddWithValue("@Clave", usuario.Clave);
            cmd.Parameters.AddWithValue("@IdRol", usuario.IdRol);

            SqlParameter resultado = new("@Resultado", SqlDbType.Bit)
            {
                Direction = ParameterDirection.Output
            };

            SqlParameter mensajeParam = new("@Mensaje", SqlDbType.VarChar, 500)
            {
                Direction = ParameterDirection.Output
            };

            cmd.Parameters.Add(resultado);
            cmd.Parameters.Add(mensajeParam);

            conexion.Open();
            AsegurarColumnasAprobacionOs(conexion);
            cmd.ExecuteNonQuery();

            mensaje = mensajeParam.Value?.ToString() ?? string.Empty;
            if (mensaje.Contains("correctamente", StringComparison.OrdinalIgnoreCase))
                GuardarAprobacionOs(conexion, usuario);

            return mensaje;
        }

        public string Editar(Usuario usuario)
        {
            string mensaje = string.Empty;

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_SEG_USUARIO_EDITAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@IdUsuario", usuario.IdUsuario);
            cmd.Parameters.AddWithValue("@IdEmpleado", usuario.IdEmpleado);
            cmd.Parameters.AddWithValue("@NombreUsuario", usuario.NombreUsuario);
            cmd.Parameters.AddWithValue("@Clave", usuario.Clave);
            cmd.Parameters.AddWithValue("@IdRol", usuario.IdRol);
            cmd.Parameters.AddWithValue("@Estado", usuario.Estado);

            SqlParameter resultado = new("@Resultado", SqlDbType.Bit)
            {
                Direction = ParameterDirection.Output
            };

            SqlParameter mensajeParam = new("@Mensaje", SqlDbType.VarChar, 500)
            {
                Direction = ParameterDirection.Output
            };

            cmd.Parameters.Add(resultado);
            cmd.Parameters.Add(mensajeParam);

            conexion.Open();
            AsegurarColumnasAprobacionOs(conexion);
            cmd.ExecuteNonQuery();

            mensaje = mensajeParam.Value?.ToString() ?? string.Empty;
            if (mensaje.Contains("correctamente", StringComparison.OrdinalIgnoreCase))
                GuardarAprobacionOs(conexion, usuario);

            return mensaje;
        }

        public string Eliminar(int idUsuario)
        {
            string mensaje = string.Empty;

            try
            {
                using SqlConnection conexion = Conexion.ObtenerConexion();
                using SqlCommand cmd = new("USP_SEG_USUARIO_ELIMINAR", conexion);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);

                SqlParameter resultado = new("@Resultado", SqlDbType.Bit)
                {
                    Direction = ParameterDirection.Output
                };

                SqlParameter mensajeParam = new("@Mensaje", SqlDbType.VarChar, 500)
                {
                    Direction = ParameterDirection.Output
                };

                cmd.Parameters.Add(resultado);
                cmd.Parameters.Add(mensajeParam);

                conexion.Open();
                cmd.ExecuteNonQuery();

                mensaje = mensajeParam.Value?.ToString() ?? string.Empty;
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                mensaje = "Usuario tiene registros, no se puede eliminar ahora.";
            }
            catch (SqlException ex)
            {
                mensaje = $"No se pudo eliminar el usuario: {ex.Message}";
            }

            return mensaje;
        }

        public string Desactivar(int idUsuario)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new(
                "UPDATE Usuarios SET Estado = 0 WHERE IdUsuario = @IdUsuario",
                conexion);

            cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);

            conexion.Open();
            int filasAfectadas = cmd.ExecuteNonQuery();

            return filasAfectadas > 0
                ? "Usuario desactivado correctamente."
                : "No se encontró el usuario seleccionado.";
        }

        public string CambiarClave(int idUsuario, string claveActual, string claveNueva)
        {
            string mensaje = string.Empty;

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_SEG_USUARIO_CAMBIAR_CLAVE", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
            cmd.Parameters.AddWithValue("@ClaveActual", claveActual);
            cmd.Parameters.AddWithValue("@ClaveNueva", claveNueva);

            SqlParameter resultado = new("@Resultado", SqlDbType.Bit)
            {
                Direction = ParameterDirection.Output
            };

            SqlParameter mensajeParam = new("@Mensaje", SqlDbType.VarChar, 500)
            {
                Direction = ParameterDirection.Output
            };

            cmd.Parameters.Add(resultado);
            cmd.Parameters.Add(mensajeParam);

            conexion.Open();
            cmd.ExecuteNonQuery();

            mensaje = mensajeParam.Value?.ToString() ?? string.Empty;

            return mensaje;
        }

        private static void AsegurarColumnasAprobacionOs(SqlConnection conexion)
        {
            using SqlCommand cmd = new("""
                IF COL_LENGTH('dbo.Usuarios', 'AprobacionOs') IS NULL
                    ALTER TABLE dbo.Usuarios ADD AprobacionOs BIT NOT NULL CONSTRAINT DF_Usuarios_AprobacionOs DEFAULT(0);
                IF COL_LENGTH('dbo.Usuarios', 'ClaveAprobacionOs') IS NULL
                    ALTER TABLE dbo.Usuarios ADD ClaveAprobacionOs VARCHAR(200) NOT NULL CONSTRAINT DF_Usuarios_ClaveAprobacionOs DEFAULT('');
                """, conexion);
            cmd.ExecuteNonQuery();
        }

        private static void GuardarAprobacionOs(SqlConnection conexion, Usuario usuario)
        {
            using SqlCommand cmd = new("""
                UPDATE dbo.Usuarios
                SET AprobacionOs = @AprobacionOs,
                    ClaveAprobacionOs = CASE
                        WHEN @AprobacionOs = 0 THEN ''
                        WHEN @ClaveAprobacionOs = '' THEN ClaveAprobacionOs
                        ELSE @ClaveAprobacionOs
                    END
                WHERE IdUsuario = @IdUsuario OR NombreUsuario = @NombreUsuario;
                """, conexion);
            cmd.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = usuario.IdUsuario;
            cmd.Parameters.Add("@NombreUsuario", SqlDbType.VarChar, 80).Value = usuario.NombreUsuario;
            cmd.Parameters.Add("@AprobacionOs", SqlDbType.Bit).Value = usuario.AprobacionOs;
            cmd.Parameters.Add("@ClaveAprobacionOs", SqlDbType.VarChar, 200).Value = usuario.ClaveAprobacionOs ?? string.Empty;
            cmd.ExecuteNonQuery();
        }

        private static void CompletarAprobacionOs(SqlConnection conexion, List<Usuario> usuarios)
        {
            foreach (Usuario usuario in usuarios)
            {
                using SqlCommand cmd = new("""
                    SELECT AprobacionOs, ClaveAprobacionOs
                    FROM dbo.Usuarios
                    WHERE IdUsuario = @IdUsuario;
                    """, conexion);
                cmd.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = usuario.IdUsuario;
                using SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    usuario.AprobacionOs = Convert.ToBoolean(dr["AprobacionOs"]);
                    usuario.ClaveAprobacionOs = dr["ClaveAprobacionOs"]?.ToString() ?? string.Empty;
                }
            }
        }

        private static bool TieneColumna(IDataRecord record, string nombre)
        {
            for (int i = 0; i < record.FieldCount; i++)
            {
                if (record.GetName(i).Equals(nombre, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
