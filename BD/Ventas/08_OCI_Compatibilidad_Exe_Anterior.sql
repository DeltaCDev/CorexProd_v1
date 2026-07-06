USE CorexProdDB;
GO

/*
    COMPATIBILIDAD TEMPORAL

    Usar este script si ya se elimino la columna IdProforma de OrdenesCompraInterna,
    pero todavia se esta ejecutando una version anterior del EXE que intenta leer:

        dr["IdProforma"]
        dr["NumeroProforma"]

    Este script NO vuelve a crear la columna IdProforma en la tabla.
    Solo devuelve columnas calculadas de compatibilidad en los SP:

        IdProforma = 0
        NumeroProforma = ''

    Cuando el EXE ya este actualizado con el nuevo codigo, estas columnas se podran retirar.
*/

CREATE OR ALTER PROCEDURE dbo.USP_VEN_OCI_LISTAR
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        O.IdOrdenCompraInterna,
        O.NumeroOci,
        CAST(0 AS INT) AS IdProforma,
        CAST('' AS VARCHAR(40)) AS NumeroProforma,
        O.FechaEmision,
        O.OrdenCompraCliente,
        O.IdCliente,
        O.NombreCliente,
        O.Subtotal,
        O.Descuento,
        O.Igv,
        O.IgvPorcentaje,
        O.CondicionTributaria,
        O.Total,
        O.Estado,
        O.UsuarioGenerador,
        O.FechaRegistro,
        O.MotivoAnulacion,
        O.UsuarioAnulacion,
        O.FechaAnulacion,
        CAST(CASE WHEN EXISTS
        (
            SELECT 1
            FROM dbo.GuiasInternas G
            WHERE G.IdOrdenCompraInterna = O.IdOrdenCompraInterna
              AND UPPER(G.Estado) <> 'ANULADO'
        ) THEN 1 ELSE 0 END AS BIT) AS TieneGuiaSalida,
        CAST(CASE WHEN EXISTS
        (
            SELECT 1
            FROM dbo.OrdenTrabajo OT
            WHERE OT.IdOrdenCompraInterna = O.IdOrdenCompraInterna
              AND UPPER(OT.Estado) <> 'ANULADA'
        ) THEN 1 ELSE 0 END AS BIT) AS TieneOrdenTrabajo,
        CAST(CASE WHEN UPPER(O.Estado) <> 'ANULADO' AND EXISTS
        (
            SELECT 1
            FROM dbo.OrdenCompraInternaDetalle D
            OUTER APPLY
            (
                SELECT SUM(OD.CantidadAplicada) AS CantidadAplicada
                FROM dbo.OrdenTrabajoDetalle OD
                JOIN dbo.OrdenTrabajo OT ON OT.IdOrdenTrabajo = OD.IdOrdenTrabajo
                WHERE OD.IdOrdenCompraInternaDetalle = D.IdOrdenCompraInternaDetalle
                  AND UPPER(OT.Estado) <> 'ANULADA'
                  AND UPPER(OD.Estado) <> 'ANULADO'
            ) PROD
            OUTER APPLY
            (
                SELECT SUM(SPA.StockActual) AS StockActual
                FROM dbo.StockProductosAlmacen SPA
                WHERE SPA.IdProducto = D.IdProducto
            ) SP
            OUTER APPLY
            (
                SELECT SUM(R.Cantidad - R.CantidadAplicada) AS StockProceso
                FROM dbo.StockProcesoReserva R
                WHERE R.IdProducto = D.IdProducto
                  AND R.Estado IN ('DISPONIBLE','RESERVADO')
                  AND R.Cantidad - R.CantidadAplicada > 0
            ) AP
            WHERE D.IdOrdenCompraInterna = O.IdOrdenCompraInterna
              AND D.Cantidad - CASE
                    WHEN D.CantidadDespachada > ISNULL(PROD.CantidadAplicada, 0)
                    THEN D.CantidadDespachada
                    ELSE ISNULL(PROD.CantidadAplicada, 0)
                  END > ISNULL(SP.StockActual, 0) + ISNULL(AP.StockProceso, 0)
        ) THEN 1 ELSE 0 END AS BIT) AS PuedeGenerarOt,
        CAST(CASE WHEN UPPER(O.Estado) <> 'ANULADO' AND EXISTS
        (
            SELECT 1
            FROM dbo.OrdenCompraInternaDetalle D
            LEFT JOIN dbo.StockProductos S ON S.IdProducto = D.IdProducto
            WHERE D.IdOrdenCompraInterna = O.IdOrdenCompraInterna
              AND D.Cantidad - D.CantidadDespachada > 0
              AND ISNULL(S.StockActual, 0) > 0
        ) THEN 1 ELSE 0 END AS BIT) AS PuedeGenerarGuiaSalida
    FROM dbo.OrdenesCompraInterna O
    ORDER BY O.FechaEmision DESC, O.IdOrdenCompraInterna DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_VEN_OCI_OBTENER
    @IdOrdenCompraInterna INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        O.IdOrdenCompraInterna,
        O.NumeroOci,
        CAST(0 AS INT) AS IdProforma,
        CAST('' AS VARCHAR(40)) AS NumeroProforma,
        O.FechaEmision,
        O.OrdenCompraCliente,
        O.IdCliente,
        O.NombreCliente,
        O.Subtotal,
        O.Descuento,
        O.Igv,
        O.IgvPorcentaje,
        O.CondicionTributaria,
        O.Total,
        O.Estado,
        O.UsuarioGenerador,
        O.FechaRegistro,
        O.MotivoAnulacion,
        O.UsuarioAnulacion,
        O.FechaAnulacion,
        CAST(CASE WHEN EXISTS
        (
            SELECT 1
            FROM dbo.GuiasInternas G
            WHERE G.IdOrdenCompraInterna = O.IdOrdenCompraInterna
              AND UPPER(G.Estado) <> 'ANULADO'
        ) THEN 1 ELSE 0 END AS BIT) AS TieneGuiaSalida,
        CAST(CASE WHEN EXISTS
        (
            SELECT 1
            FROM dbo.OrdenTrabajo OT
            WHERE OT.IdOrdenCompraInterna = O.IdOrdenCompraInterna
              AND UPPER(OT.Estado) <> 'ANULADA'
        ) THEN 1 ELSE 0 END AS BIT) AS TieneOrdenTrabajo,
        CAST(CASE WHEN UPPER(O.Estado) <> 'ANULADO' AND EXISTS
        (
            SELECT 1
            FROM dbo.OrdenCompraInternaDetalle D
            OUTER APPLY
            (
                SELECT SUM(OD.CantidadAplicada) AS CantidadAplicada
                FROM dbo.OrdenTrabajoDetalle OD
                JOIN dbo.OrdenTrabajo OT ON OT.IdOrdenTrabajo = OD.IdOrdenTrabajo
                WHERE OD.IdOrdenCompraInternaDetalle = D.IdOrdenCompraInternaDetalle
                  AND UPPER(OT.Estado) <> 'ANULADA'
                  AND UPPER(OD.Estado) <> 'ANULADO'
            ) PROD
            OUTER APPLY
            (
                SELECT SUM(SPA.StockActual) AS StockActual
                FROM dbo.StockProductosAlmacen SPA
                WHERE SPA.IdProducto = D.IdProducto
            ) SP
            OUTER APPLY
            (
                SELECT SUM(R.Cantidad - R.CantidadAplicada) AS StockProceso
                FROM dbo.StockProcesoReserva R
                WHERE R.IdProducto = D.IdProducto
                  AND R.Estado IN ('DISPONIBLE','RESERVADO')
                  AND R.Cantidad - R.CantidadAplicada > 0
            ) AP
            WHERE D.IdOrdenCompraInterna = O.IdOrdenCompraInterna
              AND D.Cantidad - CASE
                    WHEN D.CantidadDespachada > ISNULL(PROD.CantidadAplicada, 0)
                    THEN D.CantidadDespachada
                    ELSE ISNULL(PROD.CantidadAplicada, 0)
                  END > ISNULL(SP.StockActual, 0) + ISNULL(AP.StockProceso, 0)
        ) THEN 1 ELSE 0 END AS BIT) AS PuedeGenerarOt,
        CAST(CASE WHEN UPPER(O.Estado) <> 'ANULADO' AND EXISTS
        (
            SELECT 1
            FROM dbo.OrdenCompraInternaDetalle D
            LEFT JOIN dbo.StockProductos S ON S.IdProducto = D.IdProducto
            WHERE D.IdOrdenCompraInterna = O.IdOrdenCompraInterna
              AND D.Cantidad - D.CantidadDespachada > 0
              AND ISNULL(S.StockActual, 0) > 0
        ) THEN 1 ELSE 0 END AS BIT) AS PuedeGenerarGuiaSalida
    FROM dbo.OrdenesCompraInterna O
    WHERE O.IdOrdenCompraInterna = @IdOrdenCompraInterna;

    SELECT
        D.IdOrdenCompraInternaDetalle,
        D.IdOrdenCompraInterna,
        D.IdProducto,
        D.CodigoProducto,
        D.NombreProducto,
        D.Cantidad,
        CAST(ISNULL(S.StockActual, 0) AS DECIMAL(18,2)) AS StockActual,
        CAST(ISNULL(AP.StockProcesoReservado, 0) AS DECIMAL(18,2)) AS StockProcesoReservado,
        ISNULL(AP.StockProcesoReservadoDetalle, '') AS StockProcesoReservadoDetalle,
        D.CantidadDespachada,
        D.PrecioUnitario,
        D.Descuento,
        D.Importe,
        D.Observacion
    FROM dbo.OrdenCompraInternaDetalle D
    LEFT JOIN dbo.StockProductos S ON S.IdProducto = D.IdProducto
    OUTER APPLY
    (
        SELECT
            SUM(X.Cantidad) AS StockProcesoReservado,
            STUFF((
                SELECT '; ' + Y.NombreArea + ': ' + CONVERT(VARCHAR(30), CONVERT(DECIMAL(18,2), Y.Cantidad))
                FROM
                (
                    SELECT A.NombreArea, SUM(R.Cantidad - R.CantidadAplicada) AS Cantidad
                    FROM dbo.StockProcesoReserva R
                    JOIN dbo.AreaProduccion A ON A.IdAreaProduccion = R.IdAreaProduccion
                    WHERE R.IdProducto = D.IdProducto
                      AND R.Estado IN ('DISPONIBLE','RESERVADO')
                      AND R.Cantidad - R.CantidadAplicada > 0
                    GROUP BY A.NombreArea
                ) Y
                ORDER BY Y.NombreArea
                FOR XML PATH(''), TYPE
            ).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS StockProcesoReservadoDetalle
        FROM
        (
            SELECT A.NombreArea, SUM(R.Cantidad - R.CantidadAplicada) AS Cantidad
            FROM dbo.StockProcesoReserva R
            JOIN dbo.AreaProduccion A ON A.IdAreaProduccion = R.IdAreaProduccion
            WHERE R.IdProducto = D.IdProducto
              AND R.Estado IN ('DISPONIBLE','RESERVADO')
              AND R.Cantidad - R.CantidadAplicada > 0
            GROUP BY A.NombreArea
        ) X
    ) AP
    WHERE D.IdOrdenCompraInterna = @IdOrdenCompraInterna
    ORDER BY D.IdOrdenCompraInternaDetalle;
END;
GO