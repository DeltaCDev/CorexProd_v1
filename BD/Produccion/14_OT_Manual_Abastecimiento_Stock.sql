SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OrdenTrabajo_OCI')
    ALTER TABLE dbo.OrdenTrabajo DROP CONSTRAINT FK_OrdenTrabajo_OCI;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OTDetalle_OCIDet')
    ALTER TABLE dbo.OrdenTrabajoDetalle DROP CONSTRAINT FK_OTDetalle_OCIDet;
GO

IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_OrdenTrabajo_OCI')
    ALTER TABLE dbo.OrdenTrabajo DROP CONSTRAINT UQ_OrdenTrabajo_OCI;
GO

IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_OTDetalle_OCI')
    ALTER TABLE dbo.OrdenTrabajoDetalle DROP CONSTRAINT UQ_OTDetalle_OCI;
GO

IF COL_LENGTH('dbo.OrdenTrabajo', 'IdOrdenCompraInterna') IS NOT NULL
   AND COLUMNPROPERTY(OBJECT_ID('dbo.OrdenTrabajo'), 'IdOrdenCompraInterna', 'AllowsNull') = 0
    ALTER TABLE dbo.OrdenTrabajo ALTER COLUMN IdOrdenCompraInterna INT NULL;
GO

IF COL_LENGTH('dbo.OrdenTrabajoDetalle', 'IdOrdenCompraInternaDetalle') IS NOT NULL
   AND COLUMNPROPERTY(OBJECT_ID('dbo.OrdenTrabajoDetalle'), 'IdOrdenCompraInternaDetalle', 'AllowsNull') = 0
    ALTER TABLE dbo.OrdenTrabajoDetalle ALTER COLUMN IdOrdenCompraInternaDetalle INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OrdenTrabajo_OCI')
    ALTER TABLE dbo.OrdenTrabajo WITH CHECK
    ADD CONSTRAINT FK_OrdenTrabajo_OCI FOREIGN KEY(IdOrdenCompraInterna)
    REFERENCES dbo.OrdenesCompraInterna(IdOrdenCompraInterna);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OTDetalle_OCIDet')
    ALTER TABLE dbo.OrdenTrabajoDetalle WITH CHECK
    ADD CONSTRAINT FK_OTDetalle_OCIDet FOREIGN KEY(IdOrdenCompraInternaDetalle)
    REFERENCES dbo.OrdenCompraInternaDetalle(IdOrdenCompraInternaDetalle);
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.OrdenTrabajo')
      AND name = 'UX_OrdenTrabajo_OCI_Activa'
)
    CREATE UNIQUE INDEX UX_OrdenTrabajo_OCI_Activa
    ON dbo.OrdenTrabajo(IdOrdenCompraInterna)
    WHERE IdOrdenCompraInterna IS NOT NULL AND Estado <> 'ANULADA' AND IdOrdenTrabajoRelacionada IS NULL;
GO

IF EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.OrdenTrabajoDetalle')
      AND name = 'UX_OTDetalle_OCIDet_Activa'
)
    DROP INDEX UX_OTDetalle_OCIDet_Activa ON dbo.OrdenTrabajoDetalle;
GO

IF NOT EXISTS (SELECT 1 FROM sys.types WHERE is_table_type = 1 AND name = 'TipoOTManualPlanificacion')
    EXEC('CREATE TYPE dbo.TipoOTManualPlanificacion AS TABLE(IdProducto INT PRIMARY KEY, CantidadPlanificada DECIMAL(18,2) NOT NULL)');
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_MANUAL_VALIDAR_INSUMOS
    @Detalles dbo.TipoOTManualPlanificacion READONLY
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        P.IdProducto,
        P.Codigo AS CodigoProducto,
        P.NombreProducto,
        CAST('' AS VARCHAR(500)) AS Observacion,
        D.CantidadPlanificada AS CantidadRequerida,
        F.IdFichaTecnica,
        CONVERT(DECIMAL(18,3), ISNULL(SP.StockActual, 0)) AS StockAlmacen,
        CONVERT(DECIMAL(18,3), ISNULL(AP.StockCorte, 0)) AS StockCorte,
        CONVERT(DECIMAL(18,3), ISNULL(AP.StockConfeccion, 0)) AS StockConfeccion,
        CONVERT(DECIMAL(18,3), ISNULL(AP.StockAcabado, 0)) AS StockAcabado,
        CONVERT(DECIMAL(18,3), ISNULL(SP.StockActual, 0) + ISNULL(AP.StockCorte, 0) + ISNULL(AP.StockConfeccion, 0) + ISNULL(AP.StockAcabado, 0)) AS StockTotal,
        CONVERT(DECIMAL(18,3), 0) AS Deficit,
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
                  AND ISNULL(SI.StockActual, 0) < FD.Cantidad * D.CantidadPlanificada
            ) THEN 'Faltantes'
            ELSE 'Completo para producir'
        END AS EstadoInsumos
    FROM @Detalles D
    JOIN dbo.Productos P ON P.IdProducto = D.IdProducto
    OUTER APPLY
    (
        SELECT TOP(1) FT.IdFichaTecnica
        FROM dbo.FichaTecnica FT
        WHERE FT.IdProducto = P.IdProducto AND FT.Estado = 1
        ORDER BY FT.Version DESC, FT.IdFichaTecnica DESC
    ) F
    OUTER APPLY (SELECT SUM(S.StockActual) AS StockActual FROM dbo.StockProductosAlmacen S WHERE S.IdProducto = P.IdProducto) SP
    OUTER APPLY
    (
        SELECT
            SUM(CASE WHEN A.NombreArea LIKE '%CORTE%' THEN DA.CantidadPendiente ELSE 0 END) AS StockCorte,
            SUM(CASE WHEN A.NombreArea LIKE '%CONFECCI%' THEN DA.CantidadPendiente ELSE 0 END) AS StockConfeccion,
            SUM(CASE WHEN A.NombreArea LIKE '%ACABADO%' THEN DA.CantidadPendiente ELSE 0 END) AS StockAcabado
        FROM dbo.OrdenTrabajoDetalle OD
        JOIN dbo.OrdenTrabajoDetalleArea DA ON DA.IdDetalleOT = OD.IdDetalleOT
        JOIN dbo.AreaProduccion A ON A.IdAreaProduccion = DA.IdAreaProduccion
        WHERE OD.IdProducto = P.IdProducto
          AND OD.Estado NOT IN ('TERMINADO', 'ANULADO')
    ) AP
    WHERE D.CantidadPlanificada > 0
    ORDER BY P.Codigo, P.NombreProducto;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_MANUAL_DETALLE_INSUMOS
    @IdProducto INT,
    @Cantidad DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdFichaTecnica INT;
    SELECT TOP(1) @IdFichaTecnica = IdFichaTecnica
    FROM dbo.FichaTecnica
    WHERE IdProducto = @IdProducto AND Estado = 1
    ORDER BY Version DESC, IdFichaTecnica DESC;

    SELECT
        FD.IdInsumo,
        I.Codigo AS CodigoInsumo,
        I.NombreInsumo,
        UM.Abreviatura AS UnidadMedida,
        CONVERT(DECIMAL(18,3), FD.Cantidad) AS ConsumoUnitario,
        CONVERT(DECIMAL(18,3), @Cantidad) AS CantidadProduccion,
        CONVERT(DECIMAL(18,3), FD.Cantidad * @Cantidad) AS CantidadNecesaria,
        CONVERT(DECIMAL(18,3), ISNULL(SI.StockActual, 0)) AS StockActual,
        CONVERT(DECIMAL(18,3), ISNULL(SI.StockActual, 0) - FD.Cantidad * @Cantidad) AS StockProyectado,
        CONVERT(DECIMAL(18,3), CASE WHEN FD.Cantidad * @Cantidad - ISNULL(SI.StockActual, 0) > 0 THEN FD.Cantidad * @Cantidad - ISNULL(SI.StockActual, 0) ELSE 0 END) AS CantidadFaltante,
        CASE WHEN ISNULL(SI.StockActual, 0) >= FD.Cantidad * @Cantidad THEN 'Completo' ELSE 'Faltante' END AS Estado
    FROM dbo.FichaTecnicaDetalle FD
    JOIN dbo.Insumos I ON I.IdInsumo = FD.IdInsumo
    JOIN dbo.UnidadesMedida UM ON UM.IdUnidadMedida = FD.IdUnidadMedida
    LEFT JOIN dbo.StockInsumos SI ON SI.IdInsumo = FD.IdInsumo
    WHERE FD.IdFichaTecnica = @IdFichaTecnica
      AND FD.Estado = 1
    ORDER BY I.Codigo, I.NombreInsumo;
END;
GO

DECLARE @IdClienteDelta INT;
DECLARE @NombreClienteDelta NVARCHAR(250);

SELECT TOP (1)
    @IdClienteDelta = IdCliente,
    @NombreClienteDelta = NombreRazonSocial
FROM dbo.Clientes
WHERE NumeroDocumento = '20373078078'
  AND Estado = 1
ORDER BY IdCliente;

IF @IdClienteDelta IS NOT NULL
BEGIN
    UPDATE dbo.OrdenTrabajo
    SET IdCliente = @IdClienteDelta,
        NombreCliente = @NombreClienteDelta
    WHERE TipoOT = 'MANUAL'
      AND IdCliente = 0
      AND NombreCliente = 'ABASTECIMIENTO DE STOCK';
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_MANUAL_CREAR
    @IdUsuario INT,
    @Observacion NVARCHAR(500),
    @Detalles dbo.TipoOTManualPlanificacion READONLY,
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
        IF EXISTS(SELECT 1 FROM @Detalles WHERE CantidadPlanificada <= 0)
            THROW 51000, 'Todas las cantidades deben ser mayores que cero.', 1;
        IF EXISTS(SELECT IdProducto FROM @Detalles GROUP BY IdProducto HAVING COUNT(*) > 1)
            THROW 51000, 'No se puede repetir el producto en una OT manual.', 1;
        IF EXISTS(SELECT 1 FROM @Detalles D LEFT JOIN dbo.Productos P ON P.IdProducto = D.IdProducto WHERE P.IdProducto IS NULL OR P.Estado = 0)
            THROW 51000, 'La planificacion contiene productos inexistentes o inactivos.', 1;
        IF NOT EXISTS(SELECT 1 FROM dbo.AreaProduccion WHERE Activo = 1 AND EsInicio = 1)
            OR NOT EXISTS(SELECT 1 FROM dbo.AreaProduccion WHERE Activo = 1 AND EsTermino = 1)
            THROW 51000, 'Configure las areas activas de inicio y termino.', 1;

        DECLARE @IdClienteAbastecimiento INT;
        DECLARE @NombreClienteAbastecimiento NVARCHAR(250);

        SELECT TOP (1)
            @IdClienteAbastecimiento = IdCliente,
            @NombreClienteAbastecimiento = NombreRazonSocial
        FROM dbo.Clientes
        WHERE NumeroDocumento = '20373078078'
          AND Estado = 1
        ORDER BY IdCliente;

        IF @IdClienteAbastecimiento IS NULL
            THROW 51000, 'No se encontro el cliente interno DELTA CONFECCIONES SRLTDA para generar la OT manual de abastecimiento.', 1;

        DECLARE @Correlativo INT = ISNULL((SELECT MAX(TRY_CONVERT(INT, RIGHT(NumeroOT, 6))) FROM dbo.OrdenTrabajo WITH(UPDLOCK, HOLDLOCK)), 0) + 1;
        SET @NumeroOT = CONCAT('OT-', RIGHT(CONCAT('000000', @Correlativo), 6));

        INSERT dbo.OrdenTrabajo
        (
            NumeroOT, IdOrdenCompraInterna, IdCliente, NombreCliente, IdUsuarioCreacion,
            Observacion, Estado, TipoOT, IdOrdenTrabajoRelacionada
        )
        VALUES
        (
            @NumeroOT, NULL, @IdClienteAbastecimiento, @NombreClienteAbastecimiento, @IdUsuario,
            ISNULL(@Observacion, N''), 'PENDIENTE', 'MANUAL', NULL
        );

        SET @IdOrdenTrabajo = CONVERT(INT, SCOPE_IDENTITY());

        INSERT dbo.OrdenTrabajoDetalle
        (
            IdOrdenTrabajo, IdOrdenCompraInternaDetalle, IdProducto, CodigoProducto, NombreProducto,
            CantidadRequerida, CantidadPlanificada, CantidadPendiente
        )
        SELECT
            @IdOrdenTrabajo, NULL, P.IdProducto, P.Codigo, P.NombreProducto,
            D.CantidadPlanificada, D.CantidadPlanificada, D.CantidadPlanificada
        FROM @Detalles D
        JOIN dbo.Productos P ON P.IdProducto = D.IdProducto;

        INSERT dbo.OrdenTrabajoDetalleArea
        (
            IdOrdenTrabajo, IdDetalleOT, IdAreaProduccion, CodigoArea, NombreArea,
            OrdenSecuencia, EsInicio, EsTermino, ManejaMerma, ModoEnvio
        )
        SELECT @IdOrdenTrabajo, D.IdDetalleOT, A.IdAreaProduccion, A.CodigoArea, A.NombreArea,
               A.OrdenSecuencia, A.EsInicio, A.EsTermino, A.ManejaMerma, A.ModoEnvio
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

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_OBTENER @IdOrdenTrabajo INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        O.*,
        ISNULL(OCI.NumeroOci, '') AS NumeroOci,
        ISNULL(OCI.OrdenCompraCliente, '') AS OrdenCompraCliente,
        U.NombreUsuario,
        ISNULL(UA.NombreUsuario, U.NombreUsuario) AS UsuarioAutoriza
    FROM dbo.OrdenTrabajo O
    LEFT JOIN dbo.OrdenesCompraInterna OCI ON OCI.IdOrdenCompraInterna = O.IdOrdenCompraInterna
    JOIN dbo.Usuarios U ON U.IdUsuario = O.IdUsuarioCreacion
    LEFT JOIN dbo.Usuarios UA ON UA.IdUsuario = O.IdUsuarioAutorizaCreacion
    WHERE O.IdOrdenTrabajo = @IdOrdenTrabajo;

    SELECT * FROM dbo.OrdenTrabajoDetalle WHERE IdOrdenTrabajo = @IdOrdenTrabajo ORDER BY IdDetalleOT;
    SELECT * FROM dbo.OrdenTrabajoDetalleArea WHERE IdOrdenTrabajo = @IdOrdenTrabajo ORDER BY IdDetalleArea;
END;
GO
