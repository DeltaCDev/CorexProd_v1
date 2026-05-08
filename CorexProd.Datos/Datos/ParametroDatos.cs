using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class ParametroDatos
    {
        public List<Parametro> Listar()
        {
            List<Parametro> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_SEG_PARAMETRO_LISTAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            conexion.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new Parametro
                {
                    IdParametro = Convert.ToInt32(dr["IdParametro"]),
                    CodigoParametro = dr["CodigoParametro"]?.ToString() ?? string.Empty,
                    NombreParametro = dr["NombreParametro"]?.ToString() ?? string.Empty,
                    ValorParametro = dr["ValorParametro"]?.ToString() ?? string.Empty,
                    Descripcion = dr["Descripcion"]?.ToString() ?? string.Empty,
                    Estado = Convert.ToBoolean(dr["Estado"]),
                    FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
                });
            }

            return lista;
        }

        public Parametro? ObtenerPorCodigo(string codigoParametro)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_SEG_PARAMETRO_OBTENER_POR_CODIGO", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@CodigoParametro", codigoParametro);

            conexion.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            if (dr.Read())
            {
                return new Parametro
                {
                    IdParametro = Convert.ToInt32(dr["IdParametro"]),
                    CodigoParametro = dr["CodigoParametro"]?.ToString() ?? string.Empty,
                    NombreParametro = dr["NombreParametro"]?.ToString() ?? string.Empty,
                    ValorParametro = dr["ValorParametro"]?.ToString() ?? string.Empty,
                    Descripcion = dr["Descripcion"]?.ToString() ?? string.Empty,
                    Estado = Convert.ToBoolean(dr["Estado"]),
                    FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
                };
            }

            return null;
        }

        public string Registrar(Parametro parametro)
        {
            string mensaje = string.Empty;

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_SEG_PARAMETRO_REGISTRAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@CodigoParametro", parametro.CodigoParametro);
            cmd.Parameters.AddWithValue("@NombreParametro", parametro.NombreParametro);
            cmd.Parameters.AddWithValue("@ValorParametro", parametro.ValorParametro);
            cmd.Parameters.AddWithValue("@Descripcion", parametro.Descripcion);

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

        public string Editar(Parametro parametro)
        {
            string mensaje = string.Empty;

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_SEG_PARAMETRO_EDITAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@IdParametro", parametro.IdParametro);
            cmd.Parameters.AddWithValue("@CodigoParametro", parametro.CodigoParametro);
            cmd.Parameters.AddWithValue("@NombreParametro", parametro.NombreParametro);
            cmd.Parameters.AddWithValue("@ValorParametro", parametro.ValorParametro);
            cmd.Parameters.AddWithValue("@Descripcion", parametro.Descripcion);
            cmd.Parameters.AddWithValue("@Estado", parametro.Estado);

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

        public string Eliminar(int idParametro)
        {
            string mensaje = string.Empty;

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_SEG_PARAMETRO_ELIMINAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@IdParametro", idParametro);

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