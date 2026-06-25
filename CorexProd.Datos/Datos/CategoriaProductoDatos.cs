using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class CategoriaProductoDatos
    {
        public List<CategoriaProducto> Listar()
        {
            List<CategoriaProducto> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_CATEGORIA_PRODUCTO_LISTAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            conexion.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new CategoriaProducto
                {
                    IdCategoriaProducto = Convert.ToInt32(dr["IdCategoriaProducto"]),
                    IdSuperCategoriaProducto = Convert.ToInt32(dr["IdSuperCategoriaProducto"]),
                    NombreSuperCategoria = dr["NombreSuperCategoria"]?.ToString() ?? string.Empty,
                    NombreCategoria = dr["NombreCategoria"]?.ToString() ?? string.Empty,
                    Descripcion = dr["Descripcion"]?.ToString() ?? string.Empty,
                    Estado = Convert.ToBoolean(dr["Estado"]),
                    FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
                });
            }

            return lista;
        }

        public string Registrar(CategoriaProducto categoria)
        {
            string mensaje = string.Empty;

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_CATEGORIA_PRODUCTO_REGISTRAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@NombreCategoria", categoria.NombreCategoria);
            cmd.Parameters.AddWithValue("@Descripcion", categoria.Descripcion);
            cmd.Parameters.AddWithValue("@IdSuperCategoriaProducto", categoria.IdSuperCategoriaProducto);

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

        public string Editar(CategoriaProducto categoria)
        {
            string mensaje = string.Empty;

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_CATEGORIA_PRODUCTO_EDITAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@IdCategoriaProducto", categoria.IdCategoriaProducto);
            cmd.Parameters.AddWithValue("@NombreCategoria", categoria.NombreCategoria);
            cmd.Parameters.AddWithValue("@Descripcion", categoria.Descripcion);
            cmd.Parameters.AddWithValue("@IdSuperCategoriaProducto", categoria.IdSuperCategoriaProducto);
            cmd.Parameters.AddWithValue("@Estado", categoria.Estado);

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

        public string Eliminar(int idCategoriaProducto)
        {
            string mensaje = string.Empty;

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_CATEGORIA_PRODUCTO_ELIMINAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@IdCategoriaProducto", idCategoriaProducto);

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
