using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class KardexProductoReporteDatos
    {
        public List<KardexProductoReporte> Listar(DateTime? desde, DateTime? hasta, int? idAlmacen, int? idProducto, string tipoMovimiento, int? limite)
        {
            List<KardexProductoReporte> lista = [];

            using SqlConnection cn = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("""
                SELECT TOP (ISNULL(@Limite, 2147483647))
                    K.FechaMovimiento,
                    K.TipoMovimiento,
                    P.Codigo AS CodigoProducto,
                    P.NombreProducto,
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
                FROM dbo.KardexProductos K
                INNER JOIN dbo.Productos P ON P.IdProducto = K.IdProducto
                LEFT JOIN dbo.Almacenes A ON A.IdAlmacen = K.IdAlmacen
                LEFT JOIN dbo.UnidadesMedida UM ON UM.IdUnidadMedida = P.IdUnidadMedida
                CROSS APPLY
                (
                    SELECT UPPER(LTRIM(RTRIM(ISNULL(P.Codigo, '')))) AS CodigoOrden
                ) CO
                CROSS APPLY
                (
                    SELECT PATINDEX('%[0-9]%', CO.CodigoOrden) AS PosNumero
                ) PN
                CROSS APPLY
                (
                    SELECT
                        CASE WHEN PN.PosNumero > 0 THEN LEFT(CO.CodigoOrden, PN.PosNumero - 1) ELSE CO.CodigoOrden END AS Cliente,
                        CASE WHEN PN.PosNumero > 0 THEN SUBSTRING(CO.CodigoOrden, PN.PosNumero, 8000) ELSE '' END AS DesdeNumero
                ) CP
                CROSS APPLY
                (
                    SELECT
                        CASE WHEN PN.PosNumero > 0 THEN PATINDEX('%[^0-9]%', CP.DesdeNumero + 'X') - 1 ELSE 0 END AS LargoNumero
                ) LN
                CROSS APPLY
                (
                    SELECT
                        CASE WHEN LN.LargoNumero > 0 THEN TRY_CONVERT(INT, LEFT(CP.DesdeNumero, LN.LargoNumero)) END AS Numero,
                        CASE WHEN LN.LargoNumero > 0 THEN SUBSTRING(CP.DesdeNumero, LN.LargoNumero + 1, 8000) ELSE '' END AS RestoCodigo
                ) NR
                OUTER APPLY
                (
                    SELECT
                        CASE
                            WHEN NR.RestoCodigo LIKE '%T[0-9]%'
                             AND TRY_CONVERT(INT, SUBSTRING(NR.RestoCodigo, LEN(NR.RestoCodigo) - PATINDEX('%[^0-9]%', REVERSE(NR.RestoCodigo) + 'X') + 2, 8000)) IS NOT NULL
                             AND SUBSTRING(NR.RestoCodigo, LEN(NR.RestoCodigo) - PATINDEX('%[^0-9]%', REVERSE(NR.RestoCodigo) + 'X') + 1, 1) = 'T'
                                THEN TRY_CONVERT(INT, SUBSTRING(NR.RestoCodigo, LEN(NR.RestoCodigo) - PATINDEX('%[^0-9]%', REVERSE(NR.RestoCodigo) + 'X') + 2, 8000))
                        END AS TallaNumero,
                        CASE
                            WHEN NR.RestoCodigo LIKE '%T[0-9]%'
                             AND TRY_CONVERT(INT, SUBSTRING(NR.RestoCodigo, LEN(NR.RestoCodigo) - PATINDEX('%[^0-9]%', REVERSE(NR.RestoCodigo) + 'X') + 2, 8000)) IS NOT NULL
                             AND SUBSTRING(NR.RestoCodigo, LEN(NR.RestoCodigo) - PATINDEX('%[^0-9]%', REVERSE(NR.RestoCodigo) + 'X') + 1, 1) = 'T'
                                THEN LEFT(NR.RestoCodigo, LEN(NR.RestoCodigo) - PATINDEX('%[^0-9]%', REVERSE(NR.RestoCodigo) + 'X'))
                        END AS VarianteNumero
                ) TN
                OUTER APPLY
                (
                    SELECT TOP (1)
                        V.Talla,
                        V.OrdenTalla
                    FROM (VALUES
                        ('XXXL', 7),
                        ('XXL', 6),
                        ('XL', 5),
                        ('XS', 1),
                        ('L', 4),
                        ('M', 3),
                        ('S', 2)
                    ) V(Talla, OrdenTalla)
                    WHERE TN.TallaNumero IS NULL
                      AND RIGHT(NR.RestoCodigo, LEN(V.Talla)) = V.Talla
                      AND LEN(NR.RestoCodigo) >= LEN(V.Talla)
                    ORDER BY LEN(V.Talla) DESC
                ) TT
                CROSS APPLY
                (
                    SELECT
                        COALESCE(TN.VarianteNumero, CASE WHEN TT.Talla IS NOT NULL THEN LEFT(NR.RestoCodigo, LEN(NR.RestoCodigo) - LEN(TT.Talla)) ELSE NR.RestoCodigo END) AS Variante,
                        CASE
                            WHEN TT.OrdenTalla IS NOT NULL THEN TT.OrdenTalla
                            WHEN TN.TallaNumero IS NOT NULL THEN 100
                            ELSE 0
                        END AS OrdenTalla
                ) OK
                WHERE (@Desde IS NULL OR K.FechaMovimiento >= @Desde)
                  AND (@Hasta IS NULL OR K.FechaMovimiento < DATEADD(DAY, 1, @Hasta))
                  AND (@IdAlmacen IS NULL OR K.IdAlmacen = @IdAlmacen)
                  AND (@IdProducto IS NULL OR K.IdProducto = @IdProducto)
                  AND (@TipoMovimiento = '' OR K.TipoMovimiento = @TipoMovimiento)
                ORDER BY
                    CP.Cliente,
                    CASE WHEN NR.Numero IS NULL THEN 1 ELSE 0 END,
                    NR.Numero,
                    OK.Variante,
                    OK.OrdenTalla,
                    TN.TallaNumero,
                    CO.CodigoOrden,
                    K.FechaMovimiento DESC,
                    K.IdKardexProducto DESC;
                """, cn);

            cmd.Parameters.Add("@Desde", SqlDbType.DateTime).Value = (object?)desde?.Date ?? DBNull.Value;
            cmd.Parameters.Add("@Hasta", SqlDbType.DateTime).Value = (object?)hasta?.Date ?? DBNull.Value;
            cmd.Parameters.Add("@IdAlmacen", SqlDbType.Int).Value = (object?)idAlmacen ?? DBNull.Value;
            cmd.Parameters.Add("@IdProducto", SqlDbType.Int).Value = (object?)idProducto ?? DBNull.Value;
            cmd.Parameters.Add("@TipoMovimiento", SqlDbType.VarChar, 50).Value = tipoMovimiento ?? string.Empty;
            cmd.Parameters.Add("@Limite", SqlDbType.Int).Value = (object?)limite ?? DBNull.Value;

            cn.Open();

            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new KardexProductoReporte
                {
                    FechaMovimiento = Convert.ToDateTime(dr["FechaMovimiento"]),
                    TipoMovimiento = dr["TipoMovimiento"]?.ToString() ?? string.Empty,
                    CodigoProducto = dr["CodigoProducto"]?.ToString() ?? string.Empty,
                    NombreProducto = dr["NombreProducto"]?.ToString() ?? string.Empty,
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
                FROM dbo.KardexProductos
                WHERE TipoMovimiento IS NOT NULL
                  AND LTRIM(RTRIM(TipoMovimiento)) <> ''
                ORDER BY TipoMovimiento;
                """, cn);

            cn.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(dr["TipoMovimiento"]?.ToString() ?? string.Empty);
            }

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
