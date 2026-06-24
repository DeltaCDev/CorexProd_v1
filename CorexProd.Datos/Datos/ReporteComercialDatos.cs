using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class ReporteComercialDatos
    {
        public DataTable ProductosMasDespachados(DateTime? desde, DateTime? hasta) => Consultar(@"
SELECT D.CodigoProducto AS Codigo, D.NombreProducto AS Producto,
       SUM(D.CantidadDespachada) AS [Cantidad despachada],
       MAX(ISNULL(C.NombreRazonSocial, G.EmpresaDestino)) AS Cliente,
       MAX(G.FechaEmision) AS Fecha,
       MAX(G.UsuarioEmisor) AS [Usuario responsable],
       SUM(D.CantidadDespachada * D.PrecioUnitario) AS [Importe despachado]
FROM dbo.GuiasInternas G
INNER JOIN dbo.GuiaInternaDetalle D ON D.IdGuiaInterna = G.IdGuiaInterna
LEFT JOIN dbo.Clientes C ON C.IdCliente = G.IdCliente
WHERE G.Estado <> 'Anulado'
  AND (@Desde IS NULL OR G.FechaEmision >= @Desde)
  AND (@Hasta IS NULL OR G.FechaEmision <= @Hasta)
GROUP BY D.CodigoProducto, D.NombreProducto
ORDER BY [Cantidad despachada] DESC;", desde, hasta);

        public DataTable Clientes(DateTime? desde, DateTime? hasta) => Consultar(@"
SELECT ISNULL(C.NombreRazonSocial, G.EmpresaDestino) AS Cliente,
       COUNT(DISTINCT G.IdGuiaInterna) AS [Pedidos realizados],
       SUM(D.CantidadDespachada) AS [Cantidad despachada],
       SUM(D.CantidadDespachada * D.PrecioUnitario) AS [Monto acumulado]
FROM dbo.GuiasInternas G
INNER JOIN dbo.GuiaInternaDetalle D ON D.IdGuiaInterna = G.IdGuiaInterna
LEFT JOIN dbo.Clientes C ON C.IdCliente = G.IdCliente
WHERE G.Estado <> 'Anulado'
  AND (@Desde IS NULL OR G.FechaEmision >= @Desde)
  AND (@Hasta IS NULL OR G.FechaEmision <= @Hasta)
GROUP BY ISNULL(C.NombreRazonSocial, G.EmpresaDestino)
ORDER BY [Pedidos realizados] DESC, [Cantidad despachada] DESC;", desde, hasta);

        public DataTable OciDespachadas(DateTime? desde, DateTime? hasta) => Consultar(@"
WITH GuiaResumen AS
(
    SELECT
        G.IdOrdenCompraInterna,
        MAX(G.FechaRegistro) AS FechaGeneracionGuia,
        MAX(G.UsuarioEmisor) AS UsuarioResponsable,
        SUM(D.CantidadRequerida) AS TotalSolicitado,
        SUM(D.CantidadDespachada) AS TotalDespachado
    FROM dbo.GuiasInternas G
    INNER JOIN dbo.GuiaInternaDetalle D ON D.IdGuiaInterna = G.IdGuiaInterna
    WHERE G.Estado <> 'Anulado'
      AND (@Desde IS NULL OR G.FechaEmision >= @Desde)
      AND (@Hasta IS NULL OR G.FechaEmision <= @Hasta)
    GROUP BY G.IdOrdenCompraInterna
),
OtResumen AS
(
    SELECT
        OT.IdOrdenCompraInterna,
        MAX(OT.NumeroOT) AS NumeroOT,
        MAX(OT.FechaRegistro) AS FechaGeneracionOT,
        SUM(OTD.CantidadProducida) AS TotalProducido
    FROM dbo.OrdenTrabajo OT
    INNER JOIN dbo.OrdenTrabajoDetalle OTD ON OTD.IdOrdenTrabajo = OT.IdOrdenTrabajo
    GROUP BY OT.IdOrdenCompraInterna
)
SELECT O.IdOrdenCompraInterna,
       O.NombreCliente AS Cliente,
       O.NumeroOci AS OCI,
       P.SerieNumero AS Proforma,
       ISNULL(OT.NumeroOT, '') AS OT,
       O.Estado,
       O.FechaRegistro AS [Fecha creacion OCI],
       OT.FechaGeneracionOT AS [Fecha generacion OT],
       GR.FechaGeneracionGuia AS [Fecha generacion Guia],
       GR.UsuarioResponsable AS [Usuario responsable],
       GR.TotalSolicitado AS [Total solicitado],
       ISNULL(OT.TotalProducido, 0) AS [Total producido],
       GR.TotalDespachado AS [Total despachado]
FROM GuiaResumen GR
INNER JOIN dbo.OrdenesCompraInterna O ON O.IdOrdenCompraInterna = GR.IdOrdenCompraInterna
INNER JOIN dbo.Proformas P ON P.IdProforma = O.IdProforma
LEFT JOIN OtResumen OT ON OT.IdOrdenCompraInterna = O.IdOrdenCompraInterna
ORDER BY GR.FechaGeneracionGuia DESC, O.NumeroOci;", desde, hasta);

        public DataTable OciDespachadaDetalle(int idOrdenCompraInterna) => ConsultarPorId(@"
WITH GuiaDetalle AS
(
    SELECT
        D.IdProducto,
        D.CodigoProducto,
        D.NombreProducto,
        SUM(D.CantidadRequerida) AS CantidadSolicitada,
        SUM(D.CantidadDespachada) AS CantidadDespachada
    FROM dbo.GuiasInternas G
    INNER JOIN dbo.GuiaInternaDetalle D ON D.IdGuiaInterna = G.IdGuiaInterna
    WHERE G.Estado <> 'Anulado'
      AND G.IdOrdenCompraInterna = @IdOrdenCompraInterna
    GROUP BY D.IdProducto, D.CodigoProducto, D.NombreProducto
),
OtDetalle AS
(
    SELECT
        OTD.IdProducto,
        SUM(OTD.CantidadProducida) AS CantidadProducida
    FROM dbo.OrdenTrabajo OT
    INNER JOIN dbo.OrdenTrabajoDetalle OTD ON OTD.IdOrdenTrabajo = OT.IdOrdenTrabajo
    WHERE OT.IdOrdenCompraInterna = @IdOrdenCompraInterna
    GROUP BY OTD.IdProducto
)
SELECT GD.CodigoProducto AS Codigo,
       GD.NombreProducto AS Descripcion,
       GD.CantidadSolicitada AS [Cantidad solicitada],
       ISNULL(OD.CantidadProducida, 0) AS [Cantidad producida],
       GD.CantidadDespachada AS [Cantidad despachada]
FROM GuiaDetalle GD
LEFT JOIN OtDetalle OD ON OD.IdProducto = GD.IdProducto
ORDER BY GD.CodigoProducto;", idOrdenCompraInterna);

        public DataTable UsuariosConMasProformas(DateTime? desde, DateTime? hasta) => Consultar(@"
SELECT UsuarioGenerador AS Usuario,
       COUNT(*) AS [Proformas generadas],
       SUM(Total) AS [Monto total],
       MIN(FechaEmision) AS [Fecha inicial],
       MAX(FechaEmision) AS [Fecha final]
FROM dbo.Proformas
WHERE Estado <> 'Anulado'
  AND (@Desde IS NULL OR FechaEmision >= @Desde)
  AND (@Hasta IS NULL OR FechaEmision <= @Hasta)
GROUP BY UsuarioGenerador
ORDER BY [Proformas generadas] DESC, [Monto total] DESC;", desde, hasta);

        private static DataTable Consultar(string sql, DateTime? desde, DateTime? hasta)
        {
            DataTable tabla = new();
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new(sql, conexion);
            cmd.Parameters.Add("@Desde", SqlDbType.Date).Value = (object?)desde?.Date ?? DBNull.Value;
            cmd.Parameters.Add("@Hasta", SqlDbType.Date).Value = (object?)hasta?.Date ?? DBNull.Value;
            using SqlDataAdapter adaptador = new(cmd);
            adaptador.Fill(tabla);
            return tabla;
        }

        private static DataTable ConsultarPorId(string sql, int id)
        {
            DataTable tabla = new();
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new(sql, conexion);
            cmd.Parameters.Add("@IdOrdenCompraInterna", SqlDbType.Int).Value = id;
            using SqlDataAdapter adaptador = new(cmd);
            adaptador.Fill(tabla);
            return tabla;
        }
    }
}
