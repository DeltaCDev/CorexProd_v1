using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class SuperCategoriaProductoDatos
    {
        public List<SuperCategoriaProducto> Listar()
        {
            List<SuperCategoriaProducto> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_SUPER_CATEGORIA_PRODUCTO_LISTAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            conexion.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new SuperCategoriaProducto
                {
                    IdSuperCategoriaProducto = Convert.ToInt32(dr["IdSuperCategoriaProducto"]),
                    NombreSuperCategoria = dr["NombreSuperCategoria"]?.ToString() ?? string.Empty,
                    Descripcion = dr["Descripcion"]?.ToString() ?? string.Empty,
                    Estado = Convert.ToBoolean(dr["Estado"]),
                    FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
                });
            }

            return lista;
        }

        public string Registrar(SuperCategoriaProducto superCategoria)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_SUPER_CATEGORIA_PRODUCTO_REGISTRAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@NombreSuperCategoria", superCategoria.NombreSuperCategoria);
            cmd.Parameters.AddWithValue("@Descripcion", superCategoria.Descripcion);

            SqlParameter resultado = new("@Resultado", SqlDbType.Bit) { Direction = ParameterDirection.Output };
            SqlParameter mensajeParam = new("@Mensaje", SqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };

            cmd.Parameters.Add(resultado);
            cmd.Parameters.Add(mensajeParam);

            conexion.Open();
            cmd.ExecuteNonQuery();

            return mensajeParam.Value?.ToString() ?? string.Empty;
        }

        public string Editar(SuperCategoriaProducto superCategoria)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_SUPER_CATEGORIA_PRODUCTO_EDITAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@IdSuperCategoriaProducto", superCategoria.IdSuperCategoriaProducto);
            cmd.Parameters.AddWithValue("@NombreSuperCategoria", superCategoria.NombreSuperCategoria);
            cmd.Parameters.AddWithValue("@Descripcion", superCategoria.Descripcion);
            cmd.Parameters.AddWithValue("@Estado", superCategoria.Estado);

            SqlParameter resultado = new("@Resultado", SqlDbType.Bit) { Direction = ParameterDirection.Output };
            SqlParameter mensajeParam = new("@Mensaje", SqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };

            cmd.Parameters.Add(resultado);
            cmd.Parameters.Add(mensajeParam);

            conexion.Open();
            cmd.ExecuteNonQuery();

            return mensajeParam.Value?.ToString() ?? string.Empty;
        }

        public string Eliminar(int idSuperCategoriaProducto)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_SUPER_CATEGORIA_PRODUCTO_ELIMINAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@IdSuperCategoriaProducto", idSuperCategoriaProducto);

            SqlParameter resultado = new("@Resultado", SqlDbType.Bit) { Direction = ParameterDirection.Output };
            SqlParameter mensajeParam = new("@Mensaje", SqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };

            cmd.Parameters.Add(resultado);
            cmd.Parameters.Add(mensajeParam);

            conexion.Open();
            cmd.ExecuteNonQuery();

            return mensajeParam.Value?.ToString() ?? string.Empty;
        }
    }
}
