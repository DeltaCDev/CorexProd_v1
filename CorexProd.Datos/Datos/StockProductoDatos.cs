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
            using SqlCommand cmd = new(ObtenerConsulta(), conexion);
            cmd.CommandType = CommandType.Text;

            conexion.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new StockProducto
                {
                    IdProducto = Convert.ToInt32(dr["IdProducto"]),
                    Codigo = dr["Codigo"]?.ToString() ?? string.Empty,
                    NombreProducto = dr["NombreProducto"]?.ToString() ?? string.Empty,
                    IdCategoriaProducto = Convert.ToInt32(dr["IdCategoriaProducto"]),
                    NombreCategoria = dr["NombreCategoria"]?.ToString() ?? string.Empty,
                    Cantidad = Convert.ToDecimal(dr["Cantidad"])
                });
            }

            return lista;
        }

        private static string ObtenerConsulta()
        {
            return @"
DECLARE @sql NVARCHAR(MAX);

IF OBJECT_ID('dbo.StockProductos', 'U') IS NOT NULL
BEGIN
    SET @sql = N'
        SELECT
            P.IdProducto,
            P.Codigo,
            P.NombreProducto,
            P.IdCategoriaProducto,
            CP.NombreCategoria,
            CAST(ISNULL(SP.StockActual, 0) AS DECIMAL(18, 2)) AS Cantidad
        FROM dbo.Productos P
        INNER JOIN dbo.CategoriasProducto CP ON CP.IdCategoriaProducto = P.IdCategoriaProducto
        LEFT JOIN dbo.StockProductos SP ON SP.IdProducto = P.IdProducto
        WHERE P.Estado = 1
        ORDER BY P.NombreProducto;';
END
ELSE
BEGIN
    SET @sql = N'
        SELECT
            P.IdProducto,
            P.Codigo,
            P.NombreProducto,
            P.IdCategoriaProducto,
            CP.NombreCategoria,
            CAST(0 AS DECIMAL(18, 2)) AS Cantidad
        FROM dbo.Productos P
        INNER JOIN dbo.CategoriasProducto CP ON CP.IdCategoriaProducto = P.IdCategoriaProducto
        WHERE P.Estado = 1
        ORDER BY P.NombreProducto;';
END

EXEC sp_executesql @sql;";
        }
    }
}
