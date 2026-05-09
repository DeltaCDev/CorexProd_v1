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
                    Estado = Convert.ToBoolean(dr["Estado"])
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
                    Estado = Convert.ToBoolean(dr["Estado"])
                });
            }

            return lista;
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
            cmd.ExecuteNonQuery();

            mensaje = mensajeParam.Value?.ToString() ?? string.Empty;

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
            cmd.ExecuteNonQuery();

            mensaje = mensajeParam.Value?.ToString() ?? string.Empty;

            return mensaje;
        }

        public string Eliminar(int idUsuario)
        {
            string mensaje = string.Empty;

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

            return mensaje;
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
    }
}