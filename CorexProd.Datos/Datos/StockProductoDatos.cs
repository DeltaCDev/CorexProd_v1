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

        public List<StockProcesoReservaReporte> ListarReservasProceso()
        {
            const string sql = @"
SELECT
    R.IdProducto,
    R.CodigoProducto,
    R.NombreProducto,
    R.IdAreaProduccion,
    A.NombreArea,
    SUM(R.Cantidad) AS CantidadReservada,
    SUM(R.CantidadAplicada) AS CantidadAplicada,
    R.Estado,
    OT.NumeroOT,
    MIN(R.FechaRegistro) AS FechaRegistro
FROM dbo.StockProcesoReserva R
JOIN dbo.AreaProduccion A ON A.IdAreaProduccion = R.IdAreaProduccion
JOIN dbo.OrdenTrabajo OT ON OT.IdOrdenTrabajo = R.IdOrdenTrabajo
WHERE R.Estado IN ('DISPONIBLE','RESERVADO')
  AND R.Cantidad - R.CantidadAplicada > 0
GROUP BY
    R.IdProducto,
    R.CodigoProducto,
    R.NombreProducto,
    R.IdAreaProduccion,
    A.NombreArea,
    R.Estado,
    OT.NumeroOT
ORDER BY R.NombreProducto, A.NombreArea, OT.NumeroOT;";

            List<StockProcesoReservaReporte> lista = [];
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new(sql, conexion);
            conexion.Open();

            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new StockProcesoReservaReporte
                {
                    IdProducto = Convert.ToInt32(dr["IdProducto"]),
                    CodigoProducto = dr["CodigoProducto"]?.ToString() ?? string.Empty,
                    NombreProducto = dr["NombreProducto"]?.ToString() ?? string.Empty,
                    IdAreaProduccion = Convert.ToInt32(dr["IdAreaProduccion"]),
                    NombreArea = dr["NombreArea"]?.ToString() ?? string.Empty,
                    CantidadReservada = Convert.ToDecimal(dr["CantidadReservada"]),
                    CantidadAplicada = Convert.ToDecimal(dr["CantidadAplicada"]),
                    Estado = dr["Estado"]?.ToString() ?? string.Empty,
                    NumeroOT = dr["NumeroOT"]?.ToString() ?? string.Empty,
                    FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
                });
            }

            return lista;
        }
    }
}
