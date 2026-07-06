USE CorexProdDB;
GO

/*
    FIX: Ordenes de Compra sin Proforma

    Contexto:
    - El flujo actual indica que las Ordenes de Compra ya no nacen desde Proforma.
    - Las nuevas OC se registran directamente en OrdenesCompraInterna.
    - Ejemplos detectados: OC-000017 y OC-000018 tienen IdProforma = NULL.

    Problema:
    - USP_VEN_OCI_LISTAR y USP_VEN_OCI_OBTENER hacian INNER JOIN con Proformas.
    - Al no existir Proforma relacionada, la OC directa quedaba fuera de la consulta de detalle.
    - Esto bloqueaba Ver, Generar OT y Generar Guia.

    Solucion:
    - Permitir IdProforma NULL.
    - Cambiar el INNER JOIN con Proformas por LEFT JOIN.
    - Devolver NumeroProforma vacio cuando la OC no tenga Proforma.
*/

IF COL_LENGTH('dbo.OrdenesCompraInterna', 'IdProforma') IS NOT NULL
BEGIN
    ALTER TABLE dbo.OrdenesCompraInterna ALTER COLUMN IdProforma INT NULL;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_VEN_OCI_LISTAR
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        O.IdOrdenCompraInterna,
        O.NumeroOci,
        ISNULL(O.IdProforma, 0) AS IdProforma,
        ISNULL(P.SerieNumero, '') AS NumeroProforma,
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
        CAST(CASE
            WHEN EXISTS
            (
                SELECT 1
                FROM dbo.GuiasInternas G
                WHERE G.IdOrdenCompraInterna = O.IdOrdenCompraInterna
                  AND G.Estado <> 'Anulado'
            ) THEN 1 ELSE 0
        END AS BIT) AS TieneGuiaSalida,
        CAST(CASE
            WHEN EXISTS
            (
                SELECT 1
                FROM dbo.OrdenTrabajo OT
                WHERE OT.IdOrdenCompraInterna = O.IdOrdenCompraInterna
                  AND UPPER(OT.Estado) <> 'ANULADA'
            ) THEN 1 ELSE 0
        END AS BIT) AS TieneOrdenTrabajo,
        CAST(CASE WHEN O.Estado <> 'Anulado'
            AND NOT EXISTS
            (
                SELECT 1
                FROM dbo.OrdenTrabajo OT
                WHERE OT.IdOrdenCompraInterna = O.IdOrdenCompraInterna
                  AND OT.Estado IN ('PENDIENTE','EMITIDA','EN_PROCESO','PARCIAL')
            )
            AND EXISTS
            (
                SELECT 1
                FROM
                (
                    SELECT
                        D.IdProducto,
                        SUM(CASE
                            WHEN D.Cantidad - CASE WHEN D.CantidadDespachada > ISNULL(PROD.CantidadAplicada, 0) THEN D.CantidadDespachada ELSE ISNULL(PROD.CantidadAplicada, 0) END > 0
                                THEN D.Cantidad - CASE WHEN D.CantidadDespachada > ISNULL(PROD.CantidadAplicada, 0) THEN D.CantidadDespachada ELSE ISNULL(PROD.CantidadAplicada, 0) END
                            ELSE 0
                        END) AS CantidadPendiente
                    FROM dbo.OrdenCompraInternaDetalle D
                    OUTER APPLY
                    (
                        SELECT SUM(OD.CantidadAplicada) AS CantidadAplicada
                        FROM dbo.OrdenTrabajoDetalle OD
                        JOIN dbo.OrdenTrabajo OT ON OT.IdOrdenTrabajo = OD.IdOrdenTrabajo
                        WHERE OD.IdOrdenCompraInternaDetalle = D.IdOrdenCompraInternaDetalle
                          AND OT.Estado <> 'ANULADA'
                          AND OD.Estado <> 'ANULADO'
                    ) PROD
                    WHERE D.IdOrdenCompraInterna = O.IdOrdenCompraInterna
                    GROUP BY D.IdProducto
                ) PEND
                LEFT JOIN dbo.StockProductos S ON S.IdProducto = PEND.IdProducto
                WHERE PEND.CantidadPendiente > ISNULL(S.StockActual, 0)
            ) THEN 1 ELSE 0 END AS BIT) AS PuedeGenerarOt,
        CAST(CASE WHEN O.Estado <> 'Anulado'
            AND EXISTS
            (
                SELECT 1
                FROM dbo.OrdenCompraInternaDetalle D
                LEFT JOIN dbo.StockProductos S ON S.IdProducto = D.IdProducto
                WHERE D.IdOrdenCompraInterna = O.IdOrdenCompraInterna
                  AND D.Cantidad - D.CantidadDespachada > 0
                  AND ISNULL(S.StockActual, 0) > 0
            ) THEN 1 ELSE 0 END AS BIT) AS PuedeGenerarGuiaSalida
    FROM dbo.OrdenesCompraInterna O
    LEFT JOIN dbo.Proformas P ON P.IdProforma = O.IdProforma
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
        ISNULL(O.IdProforma, 0) AS IdProforma,
        ISNULL(P.SerieNumero, '') AS NumeroProforma,
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
        CAST(CASE
            WHEN EXISTS
            (
                SELECT 1
                FROM dbo.GuiasInternas G
                WHERE G.IdOrdenCompraInterna = O.IdOrdenCompraInterna
                  AND G.Estado <> 'Anulado'
            ) THEN 1 ELSE 0
        END AS BIT) AS TieneGuiaSalida,
        CAST(CASE
            WHEN EXISTS
            (
                SELECT 1
                FROM dbo.OrdenTrabajo OT
                WHERE OT.IdOrdenCompraInterna = O.IdOrdenCompraInterna
                  AND UPPER(OT.Estado) <> 'ANULADA'
            ) THEN 1 ELSE 0
        END AS BIT) AS TieneOrdenTrabajo,
        CAST(CASE WHEN O.Estado <> 'Anulado'
            AND NOT EXISTS
            (
                SELECT 1
                FROM dbo.OrdenTrabajo OT
                WHERE OT.IdOrdenCompraInterna = O.IdOrdenCompraInterna
                  AND OT.Estado IN ('PENDIENTE','EMITIDA','EN_PROCESO','PARCIAL')
            )
            AND EXISTS
            (
                SELECT 1
                FROM
                (
                    SELECT
                        D.IdProducto,
                        SUM(CASE
                            WHEN D.Cantidad - CASE WHEN D.CantidadDespachada > ISNULL(PROD.CantidadAplicada, 0) THEN D.CantidadDespachada ELSE ISNULL(PROD.CantidadAplicada, 0) END > 0
                                THEN D.Cantidad - CASE WHEN D.CantidadDespachada > ISNULL(PROD.CantidadAplicada, 0) THEN D.CantidadDespachada ELSE ISNULL(PROD.CantidadAplicada, 0) END
                            ELSE 0
                        END) AS CantidadPendiente
                    FROM dbo.OrdenCompraInternaDetalle D
                    OUTER APPLY
                    (
                        SELECT SUM(OD.CantidadAplicada) AS CantidadAplicada
                        FROM dbo.OrdenTrabajoDetalle OD
                        JOIN dbo.OrdenTrabajo OT ON OT.IdOrdenTrabajo = OD.IdOrdenTrabajo
                        WHERE OD.IdOrdenCompraInternaDetalle = D.IdOrdenCompraInternaDetalle
                          AND OT.Estado <> 'ANULADA'
                          AND OD.Estado <> 'ANULADO'
                    ) PROD
                    WHERE D.IdOrdenCompraInterna = O.IdOrdenCompraInterna
                    GROUP BY D.IdProducto
                ) PEND
                LEFT JOIN dbo.StockProductos S ON S.IdProducto = PEND.IdProducto
                WHERE PEND.CantidadPendiente > ISNULL(S.StockActual, 0)
            ) THEN 1 ELSE 0 END AS BIT) AS PuedeGenerarOt,
        CAST(CASE WHEN O.Estado <> 'Anulado'
            AND EXISTS
            (
                SELECT 1
                FROM dbo.OrdenCompraInternaDetalle D
                LEFT JOIN dbo.StockProductos S ON S.IdProducto = D.IdProducto
                WHERE D.IdOrdenCompraInterna = O.IdOrdenCompraInterna
                  AND D.Cantidad - D.CantidadDespachada > 0
                  AND ISNULL(S.StockActual, 0) > 0
            ) THEN 1 ELSE 0 END AS BIT) AS PuedeGenerarGuiaSalida
    FROM dbo.OrdenesCompraInterna O
    LEFT JOIN dbo.Proformas P ON P.IdProforma = O.IdProforma
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