using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class KardexInsumoReporteDatos
    {
        public List<KardexInsumoReporte> Listar(DateTime? desde, DateTime? hasta, int? idAlmacen, int? idInsumo, string tipoMovimiento, int? limite)
        {
            List<KardexInsumoReporte> lista = [];

            using SqlConnection cn = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("""
                SELECT TOP (ISNULL(@Limite, 2147483647))
                    K.FechaMovimiento,
                    K.TipoMovimiento,
                    I.Codigo AS CodigoInsumo,
                    I.NombreInsumo,
                    A.NombreAlmacen,
                    ISNULL(UM.Abreviatura, UM.NombreUnidad) AS UnidadMedida,
                    CASE
                        WHEN K.TipoMovimiento LIKE '%DEVOL%' THEN 0
                        WHEN K.TipoMovimiento LIKE '%SALIDA%' THEN 0
                        WHEN K.TipoMovimiento LIKE '%ANUL%' THEN 0
                        WHEN K.Cantidad > 0 THEN K.Cantidad
                        ELSE 0
                    END AS Entrada,
                    CASE
                        WHEN K.TipoMovimiento LIKE '%SALIDA%' THEN ABS(K.Cantidad)
                        WHEN K.Cantidad < 0 THEN ABS(K.Cantidad)
                        ELSE 0
                    END AS Salida,
                    CASE
                        WHEN K.TipoMovimiento LIKE '%DEVOL%' OR K.TipoMovimiento LIKE '%ANUL%' THEN ABS(K.Cantidad)
                        ELSE 0
                    END AS Devolucion,
                    K.StockResultante AS Stock,
                    CAST(0 AS DECIMAL(18,2)) AS CostoUnitario,
                    ISNULL(K.UsuarioResponsable, '') AS Usuario,
                    ISNULL(K.Observacion, '') AS Observacion
                FROM dbo.KardexInsumos K
                INNER JOIN dbo.Insumos I ON I.IdInsumo = K.IdInsumo
                LEFT JOIN dbo.Almacenes A ON A.IdAlmacen = K.IdAlmacen
                LEFT JOIN dbo.UnidadesMedida UM ON UM.IdUnidadMedida = I.IdUnidadMedida
                WHERE (@Desde IS NULL OR K.FechaMovimiento >= @Desde)
                  AND (@Hasta IS NULL OR K.FechaMovimiento < DATEADD(DAY, 1, @Hasta))
                  AND (@IdAlmacen IS NULL OR K.IdAlmacen = @IdAlmacen)
                  AND (@IdInsumo IS NULL OR K.IdInsumo = @IdInsumo)
                  AND (@TipoMovimiento = '' OR K.TipoMovimiento = @TipoMovimiento)
                ORDER BY K.FechaMovimiento DESC, K.IdKardexInsumo DESC;
                """, cn);

            cmd.Parameters.Add("@Desde", SqlDbType.DateTime).Value = (object?)desde?.Date ?? DBNull.Value;
            cmd.Parameters.Add("@Hasta", SqlDbType.DateTime).Value = (object?)hasta?.Date ?? DBNull.Value;
            cmd.Parameters.Add("@IdAlmacen", SqlDbType.Int).Value = (object?)idAlmacen ?? DBNull.Value;
            cmd.Parameters.Add("@IdInsumo", SqlDbType.Int).Value = (object?)idInsumo ?? DBNull.Value;
            cmd.Parameters.Add("@TipoMovimiento", SqlDbType.VarChar, 50).Value = tipoMovimiento ?? string.Empty;
            cmd.Parameters.Add("@Limite", SqlDbType.Int).Value = (object?)limite ?? DBNull.Value;

            cn.Open();

            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new KardexInsumoReporte
                {
                    FechaMovimiento = Convert.ToDateTime(dr["FechaMovimiento"]),
                    TipoMovimiento = dr["TipoMovimiento"]?.ToString() ?? string.Empty,
                    CodigoInsumo = dr["CodigoInsumo"]?.ToString() ?? string.Empty,
                    NombreInsumo = dr["NombreInsumo"]?.ToString() ?? string.Empty,
                    Almacen = dr["NombreAlmacen"]?.ToString() ?? string.Empty,
                    UnidadMedida = dr["UnidadMedida"]?.ToString() ?? string.Empty,
                    Entrada = Convert.ToDecimal(dr["Entrada"]),
                    Salida = Convert.ToDecimal(dr["Salida"]),
                    Devolucion = Convert.ToDecimal(dr["Devolucion"]),
                    Stock = Convert.ToDecimal(dr["Stock"]),
                    CostoUnitario = Convert.ToDecimal(dr["CostoUnitario"]),
                    Usuario = dr["Usuario"]?.ToString() ?? string.Empty,
                    Observacion = dr["Observacion"]?.ToString() ?? string.Empty
                });
            }

            return lista;
        }

        public List<string> ListarTiposMovimiento()
        {
            List<string> lista = [];

            using SqlConnection cn = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("""
                SELECT DISTINCT TipoMovimiento
                FROM dbo.KardexInsumos
                WHERE TipoMovimiento IS NOT NULL
                  AND LTRIM(RTRIM(TipoMovimiento)) <> ''
                ORDER BY TipoMovimiento;
                """, cn);

            cn.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
                lista.Add(dr["TipoMovimiento"]?.ToString() ?? string.Empty);

            return lista;
        }

        public List<AlmacenStock> ListarAlmacenes()
        {
            List<AlmacenStock> lista = [];

            using SqlConnection cn = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("SELECT IdAlmacen, NombreAlmacen FROM dbo.Almacenes WHERE Estado = 1 ORDER BY NombreAlmacen;", cn);

            cn.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new AlmacenStock
                {
                    IdAlmacen = Convert.ToInt32(dr["IdAlmacen"]),
                    NombreAlmacen = dr["NombreAlmacen"]?.ToString() ?? string.Empty
                });
            }

            return lista;
        }
    }
}
