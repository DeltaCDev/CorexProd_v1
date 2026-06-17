using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class StockInsumoDatos
    {
        public List<StockInsumo> Listar()
        {
            List<StockInsumo> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new(ObtenerConsulta(), conexion);
            cmd.CommandType = CommandType.Text;

            conexion.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new StockInsumo
                {
                    IdInsumo = Convert.ToInt32(dr["IdInsumo"]),
                    Codigo = dr["Codigo"]?.ToString() ?? string.Empty,
                    NombreInsumo = dr["NombreInsumo"]?.ToString() ?? string.Empty,
                    IdCategoriaInsumo = Convert.ToInt32(dr["IdCategoriaInsumo"]),
                    NombreCategoria = dr["NombreCategoria"]?.ToString() ?? string.Empty,
                    IdUnidadMedida = Convert.ToInt32(dr["IdUnidadMedida"]),
                    NombreUnidad = dr["NombreUnidad"]?.ToString() ?? string.Empty,
                    Abreviatura = dr["Abreviatura"]?.ToString() ?? string.Empty,
                    StockMinimo = Convert.ToDecimal(dr["StockMinimo"]),
                    Cantidad = Convert.ToDecimal(dr["Cantidad"])
                });
            }

            return lista;
        }

        private static string ObtenerConsulta()
        {
            return @"
DECLARE @sql NVARCHAR(MAX);

IF OBJECT_ID('dbo.StockInsumos', 'U') IS NOT NULL
BEGIN
    SET @sql = N'
        SELECT
            I.IdInsumo,
            I.Codigo,
            I.NombreInsumo,
            I.IdCategoriaInsumo,
            CI.NombreCategoria,
            I.IdUnidadMedida,
            UM.NombreUnidad,
            UM.Abreviatura,
            I.StockMinimo,
            CAST(ISNULL(SI.StockActual, 0) AS DECIMAL(18, 2)) AS Cantidad
        FROM dbo.Insumos I
        INNER JOIN dbo.CategoriasInsumo CI ON CI.IdCategoriaInsumo = I.IdCategoriaInsumo
        INNER JOIN dbo.UnidadesMedida UM ON UM.IdUnidadMedida = I.IdUnidadMedida
        LEFT JOIN dbo.StockInsumos SI ON SI.IdInsumo = I.IdInsumo
        WHERE I.Estado = 1
        ORDER BY I.NombreInsumo;';
END
ELSE
BEGIN
    SET @sql = N'
        SELECT
            I.IdInsumo,
            I.Codigo,
            I.NombreInsumo,
            I.IdCategoriaInsumo,
            CI.NombreCategoria,
            I.IdUnidadMedida,
            UM.NombreUnidad,
            UM.Abreviatura,
            I.StockMinimo,
            CAST(0 AS DECIMAL(18, 2)) AS Cantidad
        FROM dbo.Insumos I
        INNER JOIN dbo.CategoriasInsumo CI ON CI.IdCategoriaInsumo = I.IdCategoriaInsumo
        INNER JOIN dbo.UnidadesMedida UM ON UM.IdUnidadMedida = I.IdUnidadMedida
        WHERE I.Estado = 1
        ORDER BY I.NombreInsumo;';
END

EXEC sp_executesql @sql;";
        }
    }
}
