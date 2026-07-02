SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

CREATE OR ALTER PROCEDURE dbo.USP_VEN_OCI_LISTAR
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        O.IdOrdenCompraInterna,
        O.NumeroOci,
        O.IdProforma,
        P.SerieNumero AS NumeroProforma,
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
        CAST(CASE WHEN O.Estado <> 'Anulado' AND EXISTS
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
                SELECT SUM(DA.CantidadPendiente) AS StockProceso
                FROM dbo.OrdenTrabajoDetalle OD
                JOIN dbo.OrdenTrabajoDetalleArea DA ON DA.IdDetalleOT = OD.IdDetalleOT
                JOIN dbo.AreaProduccion A ON A.IdAreaProduccion = DA.IdAreaProduccion
                WHERE OD.IdProducto = D.IdProducto
                  AND OD.Estado NOT IN ('TERMINADO', 'ANULADO')
                  AND (A.NombreArea LIKE '%CORTE%' OR A.NombreArea LIKE '%CONFECCI%' OR A.NombreArea LIKE '%ACABADO%')
            ) AP
            WHERE D.IdOrdenCompraInterna = O.IdOrdenCompraInterna
              AND D.Cantidad - CASE WHEN D.CantidadDespachada > ISNULL(PROD.CantidadAplicada, 0) THEN D.CantidadDespachada ELSE ISNULL(PROD.CantidadAplicada, 0) END
                  > ISNULL(SP.StockActual, 0) + ISNULL(AP.StockProceso, 0)
        ) THEN 1 ELSE 0 END AS BIT) AS PuedeGenerarOt,
        CAST(CASE WHEN O.Estado <> 'Anulado' AND EXISTS
        (
            SELECT 1
            FROM dbo.OrdenCompraInternaDetalle D
            LEFT JOIN dbo.StockProductos S ON S.IdProducto = D.IdProducto
            WHERE D.IdOrdenCompraInterna = O.IdOrdenCompraInterna
              AND D.Cantidad - D.CantidadDespachada > 0
              AND ISNULL(S.StockActual, 0) > 0
        ) THEN 1 ELSE 0 END AS BIT) AS PuedeGenerarGuiaSalida
    FROM dbo.OrdenesCompraInterna O
    INNER JOIN dbo.Proformas P ON P.IdProforma = O.IdProforma
    ORDER BY O.FechaEmision DESC, O.IdOrdenCompraInterna DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_VEN_OCI_OBTENER
    @IdOrdenCompraInterna INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM
    (
        SELECT
            O.IdOrdenCompraInterna,
            O.NumeroOci,
            O.IdProforma,
            P.SerieNumero AS NumeroProforma,
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
                SELECT 1 FROM dbo.GuiasInternas G
                WHERE G.IdOrdenCompraInterna = O.IdOrdenCompraInterna
                  AND UPPER(G.Estado) <> 'ANULADO'
            ) THEN 1 ELSE 0 END AS BIT) AS TieneGuiaSalida,
            CAST(CASE WHEN EXISTS
            (
                SELECT 1 FROM dbo.OrdenTrabajo OT
                WHERE OT.IdOrdenCompraInterna = O.IdOrdenCompraInterna
                  AND UPPER(OT.Estado) <> 'ANULADA'
            ) THEN 1 ELSE 0 END AS BIT) AS TieneOrdenTrabajo,
            CAST(CASE WHEN O.Estado <> 'Anulado' AND EXISTS
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
                OUTER APPLY (SELECT SUM(SPA.StockActual) AS StockActual FROM dbo.StockProductosAlmacen SPA WHERE SPA.IdProducto = D.IdProducto) SP
                OUTER APPLY
                (
                    SELECT SUM(DA.CantidadPendiente) AS StockProceso
                    FROM dbo.OrdenTrabajoDetalle OD
                    JOIN dbo.OrdenTrabajoDetalleArea DA ON DA.IdDetalleOT = OD.IdDetalleOT
                    JOIN dbo.AreaProduccion A ON A.IdAreaProduccion = DA.IdAreaProduccion
                    WHERE OD.IdProducto = D.IdProducto
                      AND OD.Estado NOT IN ('TERMINADO', 'ANULADO')
                      AND (A.NombreArea LIKE '%CORTE%' OR A.NombreArea LIKE '%CONFECCI%' OR A.NombreArea LIKE '%ACABADO%')
                ) AP
                WHERE D.IdOrdenCompraInterna = O.IdOrdenCompraInterna
                  AND D.Cantidad - CASE WHEN D.CantidadDespachada > ISNULL(PROD.CantidadAplicada, 0) THEN D.CantidadDespachada ELSE ISNULL(PROD.CantidadAplicada, 0) END
                      > ISNULL(SP.StockActual, 0) + ISNULL(AP.StockProceso, 0)
            ) THEN 1 ELSE 0 END AS BIT) AS PuedeGenerarOt,
            CAST(CASE WHEN O.Estado <> 'Anulado' AND EXISTS
            (
                SELECT 1
                FROM dbo.OrdenCompraInternaDetalle D
                LEFT JOIN dbo.StockProductos S ON S.IdProducto = D.IdProducto
                WHERE D.IdOrdenCompraInterna = O.IdOrdenCompraInterna
                  AND D.Cantidad - D.CantidadDespachada > 0
                  AND ISNULL(S.StockActual, 0) > 0
            ) THEN 1 ELSE 0 END AS BIT) AS PuedeGenerarGuiaSalida
        FROM dbo.OrdenesCompraInterna O
        INNER JOIN dbo.Proformas P ON P.IdProforma = O.IdProforma
    ) O
    WHERE O.IdOrdenCompraInterna = @IdOrdenCompraInterna;

    SELECT
        D.IdOrdenCompraInternaDetalle,
        D.IdOrdenCompraInterna,
        D.IdProducto,
        D.CodigoProducto,
        D.NombreProducto,
        D.Cantidad,
        CAST(ISNULL(S.StockActual, 0) AS DECIMAL(18,2)) AS StockActual,
        D.CantidadDespachada,
        D.PrecioUnitario,
        D.Descuento,
        D.Importe,
        D.Observacion
    FROM dbo.OrdenCompraInternaDetalle D
    LEFT JOIN dbo.StockProductos S ON S.IdProducto = D.IdProducto
    WHERE D.IdOrdenCompraInterna = @IdOrdenCompraInterna
    ORDER BY D.IdOrdenCompraInternaDetalle;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_VALIDAR_INSUMOS @IdOrdenCompraInterna INT
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH ProductosOCI AS
    (
        SELECT
            D.IdOrdenCompraInternaDetalle,
            D.IdProducto,
            D.CodigoProducto,
            D.NombreProducto,
            D.Observacion,
            CONVERT(DECIMAL(18,3), PEND.CantidadPendiente) AS CantidadPendiente,
            CONVERT(DECIMAL(18,3), ISNULL(SP.StockActual, 0)) AS StockAlmacen,
            CONVERT(DECIMAL(18,3), ISNULL(AP.StockCorte, 0)) AS StockCorte,
            CONVERT(DECIMAL(18,3), ISNULL(AP.StockConfeccion, 0)) AS StockConfeccion,
            CONVERT(DECIMAL(18,3), ISNULL(AP.StockAcabado, 0)) AS StockAcabado,
            CONVERT(DECIMAL(18,3), ISNULL(SP.StockActual, 0) + ISNULL(AP.StockCorte, 0) + ISNULL(AP.StockConfeccion, 0) + ISNULL(AP.StockAcabado, 0)) AS StockTotal,
            CONVERT(DECIMAL(18,3),
                CASE
                    WHEN PEND.CantidadPendiente - (ISNULL(SP.StockActual, 0) + ISNULL(AP.StockCorte, 0) + ISNULL(AP.StockConfeccion, 0) + ISNULL(AP.StockAcabado, 0)) > 0
                    THEN PEND.CantidadPendiente - (ISNULL(SP.StockActual, 0) + ISNULL(AP.StockCorte, 0) + ISNULL(AP.StockConfeccion, 0) + ISNULL(AP.StockAcabado, 0))
                    ELSE 0
                END) AS Deficit
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
        CROSS APPLY
        (
            SELECT CONVERT(DECIMAL(18,3),
                CASE
                    WHEN D.Cantidad - CASE WHEN D.CantidadDespachada > ISNULL(PROD.CantidadAplicada, 0) THEN D.CantidadDespachada ELSE ISNULL(PROD.CantidadAplicada, 0) END > 0
                    THEN D.Cantidad - CASE WHEN D.CantidadDespachada > ISNULL(PROD.CantidadAplicada, 0) THEN D.CantidadDespachada ELSE ISNULL(PROD.CantidadAplicada, 0) END
                    ELSE 0
                END) AS CantidadPendiente
        ) PEND
        OUTER APPLY (SELECT SUM(S.StockActual) AS StockActual FROM dbo.StockProductosAlmacen S WHERE S.IdProducto = D.IdProducto) SP
        OUTER APPLY
        (
            SELECT
                SUM(CASE WHEN A.NombreArea LIKE '%CORTE%' THEN DA.CantidadPendiente ELSE 0 END) AS StockCorte,
                SUM(CASE WHEN A.NombreArea LIKE '%CONFECCI%' THEN DA.CantidadPendiente ELSE 0 END) AS StockConfeccion,
                SUM(CASE WHEN A.NombreArea LIKE '%ACABADO%' THEN DA.CantidadPendiente ELSE 0 END) AS StockAcabado
            FROM dbo.OrdenTrabajoDetalle OD
            JOIN dbo.OrdenTrabajoDetalleArea DA ON DA.IdDetalleOT = OD.IdDetalleOT
            JOIN dbo.AreaProduccion A ON A.IdAreaProduccion = DA.IdAreaProduccion
            WHERE OD.IdProducto = D.IdProducto
              AND OD.Estado NOT IN ('TERMINADO', 'ANULADO')
        ) AP
        WHERE D.IdOrdenCompraInterna = @IdOrdenCompraInterna
    ), Ficha AS
    (
        SELECT P.*, F.IdFichaTecnica, ROW_NUMBER() OVER(PARTITION BY P.IdProducto ORDER BY F.Version DESC, F.IdFichaTecnica DESC) AS rn
        FROM ProductosOCI P
        LEFT JOIN dbo.FichaTecnica F ON F.IdProducto = P.IdProducto AND F.Estado = 1
        WHERE P.Deficit > 0
    )
    SELECT
        F.IdOrdenCompraInternaDetalle,
        F.IdProducto,
        F.CodigoProducto,
        F.NombreProducto,
        F.Observacion,
        F.Deficit AS CantidadRequerida,
        F.IdFichaTecnica,
        F.StockAlmacen,
        F.StockCorte,
        F.StockConfeccion,
        F.StockAcabado,
        F.StockTotal,
        F.Deficit,
        CASE
            WHEN F.IdFichaTecnica IS NULL
                 OR NOT EXISTS(SELECT 1 FROM dbo.FichaTecnicaDetalle FD WHERE FD.IdFichaTecnica = F.IdFichaTecnica AND FD.Estado = 1)
                THEN 'Sin ficha tecnica'
            WHEN EXISTS
            (
                SELECT 1
                FROM dbo.FichaTecnicaDetalle FD
                LEFT JOIN dbo.StockInsumos SI ON SI.IdInsumo = FD.IdInsumo
                WHERE FD.IdFichaTecnica = F.IdFichaTecnica
                  AND FD.Estado = 1
                  AND ISNULL(SI.StockActual, 0) < FD.Cantidad * F.Deficit
            ) THEN 'Faltantes'
            ELSE 'Completo para producir'
        END AS EstadoInsumos
    FROM Ficha F
    WHERE F.rn = 1
    ORDER BY F.IdOrdenCompraInternaDetalle;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_DETALLE_INSUMOS @IdOrdenCompraInternaDetalle INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdProducto INT, @Cantidad DECIMAL(18,3), @IdFicha INT;

    ;WITH Producto AS
    (
        SELECT
            D.IdProducto,
            PEND.CantidadPendiente,
            ISNULL(SP.StockActual, 0) + ISNULL(AP.StockCorte, 0) + ISNULL(AP.StockConfeccion, 0) + ISNULL(AP.StockAcabado, 0) AS StockTotal
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
        CROSS APPLY
        (
            SELECT CONVERT(DECIMAL(18,3),
                CASE
                    WHEN D.Cantidad - CASE WHEN D.CantidadDespachada > ISNULL(PROD.CantidadAplicada, 0) THEN D.CantidadDespachada ELSE ISNULL(PROD.CantidadAplicada, 0) END > 0
                    THEN D.Cantidad - CASE WHEN D.CantidadDespachada > ISNULL(PROD.CantidadAplicada, 0) THEN D.CantidadDespachada ELSE ISNULL(PROD.CantidadAplicada, 0) END
                    ELSE 0
                END) AS CantidadPendiente
        ) PEND
        OUTER APPLY (SELECT SUM(S.StockActual) AS StockActual FROM dbo.StockProductosAlmacen S WHERE S.IdProducto = D.IdProducto) SP
        OUTER APPLY
        (
            SELECT
                SUM(CASE WHEN A.NombreArea LIKE '%CORTE%' THEN DA.CantidadPendiente ELSE 0 END) AS StockCorte,
                SUM(CASE WHEN A.NombreArea LIKE '%CONFECCI%' THEN DA.CantidadPendiente ELSE 0 END) AS StockConfeccion,
                SUM(CASE WHEN A.NombreArea LIKE '%ACABADO%' THEN DA.CantidadPendiente ELSE 0 END) AS StockAcabado
            FROM dbo.OrdenTrabajoDetalle OD
            JOIN dbo.OrdenTrabajoDetalleArea DA ON DA.IdDetalleOT = OD.IdDetalleOT
            JOIN dbo.AreaProduccion A ON A.IdAreaProduccion = DA.IdAreaProduccion
            WHERE OD.IdProducto = D.IdProducto
              AND OD.Estado NOT IN ('TERMINADO', 'ANULADO')
        ) AP
        WHERE D.IdOrdenCompraInternaDetalle = @IdOrdenCompraInternaDetalle
    )
    SELECT
        @IdProducto = IdProducto,
        @Cantidad = CONVERT(DECIMAL(18,3), CASE WHEN CantidadPendiente - StockTotal > 0 THEN CantidadPendiente - StockTotal ELSE 0 END)
    FROM Producto;

    SELECT TOP(1) @IdFicha = IdFichaTecnica
    FROM dbo.FichaTecnica
    WHERE IdProducto = @IdProducto AND Estado = 1
    ORDER BY Version DESC, IdFichaTecnica DESC;

    SELECT
        FD.IdInsumo,
        I.Codigo AS CodigoInsumo,
        I.NombreInsumo,
        UM.Abreviatura AS UnidadMedida,
        CONVERT(DECIMAL(18,3), FD.Cantidad) AS ConsumoUnitario,
        @Cantidad AS CantidadProduccion,
        CONVERT(DECIMAL(18,3), FD.Cantidad * @Cantidad) AS CantidadNecesaria,
        CONVERT(DECIMAL(18,3), ISNULL(SI.StockActual, 0)) AS StockActual,
        CONVERT(DECIMAL(18,3), ISNULL(SI.StockActual, 0) - FD.Cantidad * @Cantidad) AS StockProyectado,
        CONVERT(DECIMAL(18,3), CASE WHEN FD.Cantidad * @Cantidad - ISNULL(SI.StockActual, 0) > 0 THEN FD.Cantidad * @Cantidad - ISNULL(SI.StockActual, 0) ELSE 0 END) AS CantidadFaltante,
        CASE WHEN ISNULL(SI.StockActual, 0) >= FD.Cantidad * @Cantidad THEN 'Completo' ELSE 'Faltante' END AS Estado
    FROM dbo.FichaTecnicaDetalle FD
    JOIN dbo.Insumos I ON I.IdInsumo = FD.IdInsumo
    JOIN dbo.UnidadesMedida UM ON UM.IdUnidadMedida = FD.IdUnidadMedida
    LEFT JOIN dbo.StockInsumos SI ON SI.IdInsumo = FD.IdInsumo
    WHERE FD.IdFichaTecnica = @IdFicha AND FD.Estado = 1
    ORDER BY I.NombreInsumo;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_CREAR
    @IdOrdenCompraInterna INT,
    @IdUsuario INT,
    @Observacion NVARCHAR(500),
    @Detalles dbo.TipoOTPlanificacion READONLY,
    @IdOrdenTrabajo INT OUTPUT,
    @NumeroOT VARCHAR(30) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        IF NOT EXISTS(SELECT 1 FROM dbo.Usuarios WHERE IdUsuario = @IdUsuario AND Estado = 1)
            THROW 51000, 'El usuario de sesion no es valido.', 1;
        IF NOT EXISTS(SELECT 1 FROM @Detalles)
            THROW 51000, 'Seleccione al menos un producto.', 1;
        IF NOT EXISTS(SELECT 1 FROM dbo.AreaProduccion WHERE Activo = 1 AND EsInicio = 1)
            OR NOT EXISTS(SELECT 1 FROM dbo.AreaProduccion WHERE Activo = 1 AND EsTermino = 1)
            THROW 51000, 'Configure las areas activas de inicio y termino.', 1;

        DECLARE @Pendientes TABLE(IdOrdenCompraInternaDetalle INT PRIMARY KEY, CantidadPendiente DECIMAL(18,2) NOT NULL);

        INSERT @Pendientes(IdOrdenCompraInternaDetalle, CantidadPendiente)
        SELECT D.IdOrdenCompraInternaDetalle, CONVERT(DECIMAL(18,2), DEF.Deficit)
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
        CROSS APPLY
        (
            SELECT CONVERT(DECIMAL(18,3),
                CASE
                    WHEN D.Cantidad - CASE WHEN D.CantidadDespachada > ISNULL(PROD.CantidadAplicada, 0) THEN D.CantidadDespachada ELSE ISNULL(PROD.CantidadAplicada, 0) END > 0
                    THEN D.Cantidad - CASE WHEN D.CantidadDespachada > ISNULL(PROD.CantidadAplicada, 0) THEN D.CantidadDespachada ELSE ISNULL(PROD.CantidadAplicada, 0) END
                    ELSE 0
                END) AS CantidadPendiente
        ) PEND
        OUTER APPLY (SELECT SUM(S.StockActual) AS StockActual FROM dbo.StockProductosAlmacen S WHERE S.IdProducto = D.IdProducto) SP
        OUTER APPLY
        (
            SELECT SUM(DA.CantidadPendiente) AS StockProceso
            FROM dbo.OrdenTrabajoDetalle OD
            JOIN dbo.OrdenTrabajoDetalleArea DA ON DA.IdDetalleOT = OD.IdDetalleOT
            JOIN dbo.AreaProduccion A ON A.IdAreaProduccion = DA.IdAreaProduccion
            WHERE OD.IdProducto = D.IdProducto
              AND OD.Estado NOT IN ('TERMINADO', 'ANULADO')
              AND (A.NombreArea LIKE '%CORTE%' OR A.NombreArea LIKE '%CONFECCI%' OR A.NombreArea LIKE '%ACABADO%')
        ) AP
        CROSS APPLY
        (
            SELECT CONVERT(DECIMAL(18,3),
                CASE
                    WHEN PEND.CantidadPendiente - (ISNULL(SP.StockActual, 0) + ISNULL(AP.StockProceso, 0)) > 0
                    THEN PEND.CantidadPendiente - (ISNULL(SP.StockActual, 0) + ISNULL(AP.StockProceso, 0))
                    ELSE 0
                END) AS Deficit
        ) DEF
        WHERE D.IdOrdenCompraInterna = @IdOrdenCompraInterna
          AND DEF.Deficit > 0;

        IF EXISTS
        (
            SELECT 1
            FROM @Detalles X
            LEFT JOIN @Pendientes P ON P.IdOrdenCompraInternaDetalle = X.IdOrdenCompraInternaDetalle
            WHERE P.IdOrdenCompraInternaDetalle IS NULL
               OR X.CantidadPlanificada <= 0
               OR X.CantidadPlanificada > P.CantidadPendiente
        )
            THROW 51000, 'La planificacion contiene productos sin deficit o cantidades no validas.', 1;

        DECLARE @IdOrdenTrabajoRelacionada INT =
        (
            SELECT TOP(1) IdOrdenTrabajo
            FROM dbo.OrdenTrabajo
            WHERE IdOrdenCompraInterna = @IdOrdenCompraInterna
              AND UPPER(Estado) <> 'ANULADA'
            ORDER BY IdOrdenTrabajo DESC
        );
        DECLARE @Correlativo INT = ISNULL((SELECT MAX(TRY_CONVERT(INT, RIGHT(NumeroOT, 6))) FROM dbo.OrdenTrabajo WITH(UPDLOCK, HOLDLOCK)), 0) + 1;
        SET @NumeroOT = CONCAT('OT-', RIGHT(CONCAT('000000', @Correlativo), 6));

        INSERT dbo.OrdenTrabajo(NumeroOT, IdOrdenCompraInterna, IdCliente, NombreCliente, IdUsuarioCreacion, Observacion, Estado, TipoOT, IdOrdenTrabajoRelacionada)
        SELECT @NumeroOT, O.IdOrdenCompraInterna, O.IdCliente, O.NombreCliente, @IdUsuario, ISNULL(@Observacion, N''), 'PENDIENTE',
               CASE WHEN @IdOrdenTrabajoRelacionada IS NULL THEN 'OCI' ELSE 'OT' END, @IdOrdenTrabajoRelacionada
        FROM dbo.OrdenesCompraInterna O
        WHERE O.IdOrdenCompraInterna = @IdOrdenCompraInterna
          AND O.Estado <> 'Anulado';
        IF @@ROWCOUNT = 0
            THROW 51000, 'La OCI no existe o esta anulada.', 1;

        SET @IdOrdenTrabajo = CONVERT(INT, SCOPE_IDENTITY());

        INSERT dbo.OrdenTrabajoDetalle(IdOrdenTrabajo, IdOrdenCompraInternaDetalle, IdProducto, CodigoProducto, NombreProducto, CantidadRequerida, CantidadPlanificada, CantidadPendiente)
        SELECT @IdOrdenTrabajo, D.IdOrdenCompraInternaDetalle, D.IdProducto, D.CodigoProducto, D.NombreProducto,
               P.CantidadPendiente, X.CantidadPlanificada, X.CantidadPlanificada
        FROM @Detalles X
        JOIN dbo.OrdenCompraInternaDetalle D ON D.IdOrdenCompraInternaDetalle = X.IdOrdenCompraInternaDetalle
        JOIN @Pendientes P ON P.IdOrdenCompraInternaDetalle = D.IdOrdenCompraInternaDetalle;

        INSERT dbo.OrdenTrabajoDetalleArea(IdOrdenTrabajo, IdDetalleOT, IdAreaProduccion, CodigoArea, NombreArea, OrdenSecuencia, EsInicio, EsTermino, ManejaMerma, ModoEnvio)
        SELECT @IdOrdenTrabajo, D.IdDetalleOT, A.IdAreaProduccion, A.CodigoArea, A.NombreArea, A.OrdenSecuencia, A.EsInicio, A.EsTermino, A.ManejaMerma, A.ModoEnvio
        FROM dbo.OrdenTrabajoDetalle D
        CROSS JOIN dbo.AreaProduccion A
        WHERE D.IdOrdenTrabajo = @IdOrdenTrabajo
          AND A.Activo = 1;

        UPDATE A
        SET CantidadRecibida = D.CantidadPlanificada,
            Estado = 'PENDIENTE'
        FROM dbo.OrdenTrabajoDetalleArea A
        JOIN dbo.OrdenTrabajoDetalle D ON D.IdDetalleOT = A.IdDetalleOT
        WHERE A.IdOrdenTrabajo = @IdOrdenTrabajo
          AND A.EsInicio = 1;

        UPDATE dbo.OrdenesCompraInterna
        SET TieneOrdenTrabajo = 1,
            Estado = 'PROCESO'
        WHERE IdOrdenCompraInterna = @IdOrdenCompraInterna;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO
