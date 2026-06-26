using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class StockProductoDatos
    {
        public List<StockProducto> Listar()
        {
            List<StockProducto> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_PRODUCTO_LISTAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@SoloActivos", true);

            conexion.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new StockProducto
                {
                    IdProducto = Convert.ToInt32(dr["IdProducto"]),
                    Codigo = dr["Codigo"]?.ToString() ?? string.Empty,
                    NombreProducto = dr["NombreProducto"]?.ToString() ?? string.Empty,
                    EtiquetaCliente = dr["EtiquetaCliente"]?.ToString() ?? string.Empty,
                    IdCategoriaProducto = Convert.ToInt32(dr["IdCategoriaProducto"]),
                    NombreCategoria = dr["NombreCategoria"]?.ToString() ?? string.Empty,
                    Cantidad = Convert.ToDecimal(dr["Cantidad"])
                });
            }

            return lista;
        }
    }
}
