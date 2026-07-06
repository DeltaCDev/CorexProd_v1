USE CorexProdDB;
GO

/*
    MIGRACION: Orden de Compra sin IdProforma

    Nuevo criterio funcional:
    - Las Ordenes de Compra ya no nacen desde Proforma.
    - La Orden de Compra es el documento principal del flujo comercial/productivo.
    - Proformas puede seguir existiendo como modulo independiente de cotizacion, pero ya no debe amarrar OCI.

    Impacto:
    - Se elimina la dependencia de IdProforma en OrdenesCompraInterna.
    - Se recrean los SP de listado, obtencion y guardado para trabajar solo con OrdenesCompraInterna.
*/

IF EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.OrdenesCompraInterna')
      AND name = 'UX_OCI_Proforma_Activa'
)
    DROP INDEX UX_OCI_Proforma_Activa ON dbo.OrdenesCompraInterna;
GO

IF EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID('dbo.OrdenesCompraInterna')
      AND name = 'FK_OCI_Proforma'
)
    ALTER TABLE dbo.OrdenesCompraInterna DROP CONSTRAINT FK_OCI_Proforma;
GO

CREATE OR ALTER PROCEDURE dbo.USP_VEN_OCI_LISTAR
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        O.IdOrdenCompraInterna,
        O.NumeroOci,
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

CREATE OR ALTER PROCEDURE dbo.USP_VEN_ORDEN_COMPRA_GUARDAR
    @FechaEmision DATE,
    @OrdenCompraCliente VARCHAR(100),
    @IdCliente INT,
    @Subtotal DECIMAL(18,2),
    @Descuento DECIMAL(18,2),
    @Igv DECIMAL(18,2),
    @IgvPorcentaje DECIMAL(9,4),
    @CondicionTributaria VARCHAR(50),
    @Total DECIMAL(18,2),
    @DetallesXml XML,
    @UsuarioGenerador VARCHAR(80),
    @IdGenerado INT OUTPUT,
    @NumeroOrden VARCHAR(40) OUTPUT,
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @IdGenerado = 0;
    SET @NumeroOrden = '';
    SET @Resultado = 0;

    IF NOT EXISTS (SELECT 1 FROM dbo.Clientes WHERE IdCliente = @IdCliente AND Estado = 1)
    BEGIN
        SET @Mensaje = 'Debe seleccionar un cliente activo.';
        RETURN;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM @DetallesXml.nodes('/Detalles/Detalle') D(X)
        WHERE D.X.value('@IdProducto', 'INT') > 0
          AND D.X.value('@Cantidad', 'DECIMAL(18,2)') > 0
    )
    BEGIN
        SET @Mensaje = 'Debe agregar productos con cantidad mayor a cero.';
        RETURN;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO dbo.OrdenesCompraInterna
        (
            NumeroOci, FechaEmision, OrdenCompraCliente, IdCliente,
            NombreCliente, Subtotal, Descuento, Igv, IgvPorcentaje, CondicionTributaria,
            Total, Estado, UsuarioGenerador
        )
        SELECT
            '', @FechaEmision, ISNULL(@OrdenCompraCliente, ''), C.IdCliente,
            C.NombreRazonSocial, @Subtotal, @Descuento, @Igv, @IgvPorcentaje,
            ISNULL(@CondicionTributaria, ''), @Total, 'Emitida', ISNULL(NULLIF(@UsuarioGenerador, ''), 'Sistema')
        FROM dbo.Clientes C
        WHERE C.IdCliente = @IdCliente;

        SET @IdGenerado = CONVERT(INT, SCOPE_IDENTITY());
        SET @NumeroOrden = CONCAT('OC-', RIGHT(CONCAT('000000', @IdGenerado), 6));

        UPDATE dbo.OrdenesCompraInterna
        SET NumeroOci = @NumeroOrden
        WHERE IdOrdenCompraInterna = @IdGenerado;

        INSERT INTO dbo.OrdenCompraInternaDetalle
        (
            IdOrdenCompraInterna, IdProducto, CodigoProducto, NombreProducto,
            Cantidad, PrecioUnitario, Descuento, Importe, Observacion
        )
        SELECT
            @IdGenerado,
            P.IdProducto,
            P.Codigo,
            P.NombreProducto,
            X.Cantidad,
            X.PrecioUnitario,
            X.Descuento,
            X.Importe,
            X.Observacion
        FROM
        (
            SELECT
                D.X.value('@IdProducto', 'INT') AS IdProducto,
                D.X.value('@Cantidad', 'DECIMAL(18,2)') AS Cantidad,
                D.X.value('@PrecioUnitario', 'DECIMAL(18,2)') AS PrecioUnitario,
                D.X.value('@Descuento', 'DECIMAL(18,2)') AS Descuento,
                D.X.value('@Importe', 'DECIMAL(18,2)') AS Importe,
                D.X.value('@Observacion', 'VARCHAR(500)') AS Observacion
            FROM @DetallesXml.nodes('/Detalles/Detalle') D(X)
        ) X
        INNER JOIN dbo.Productos P ON P.IdProducto = X.IdProducto
        WHERE X.Cantidad > 0;

        COMMIT TRANSACTION;
        SET @Resultado = 1;
        SET @Mensaje = CONCAT('Orden de compra ', @NumeroOrden, ' registrada correctamente.');
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH;
END;
GO

IF COL_LENGTH('dbo.OrdenesCompraInterna', 'IdProforma') IS NOT NULL
BEGIN
    ALTER TABLE dbo.OrdenesCompraInterna DROP COLUMN IdProforma;
END;
GO