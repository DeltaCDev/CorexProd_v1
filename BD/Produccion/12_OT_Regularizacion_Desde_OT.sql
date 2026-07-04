SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_CREAR_REGULARIZACION
    @IdOrdenCompraInterna INT,
    @IdOrdenTrabajoOrigen INT,
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

        IF NOT EXISTS
        (
            SELECT 1
            FROM dbo.OrdenTrabajo
            WHERE IdOrdenTrabajo = @IdOrdenTrabajoOrigen
              AND IdOrdenCompraInterna = @IdOrdenCompraInterna
              AND UPPER(Estado) <> 'ANULADA'
        )
            THROW 51000, 'La OT origen no existe, no pertenece a la OCI o esta anulada.', 1;

        IF EXISTS
        (
            SELECT 1
            FROM dbo.OrdenTrabajo
            WHERE IdOrdenTrabajoRelacionada = @IdOrdenTrabajoOrigen
              AND Estado IN ('PENDIENTE','EMITIDA','EN_PROCESO','PARCIAL')
        )
            THROW 51000, 'La OT origen ya tiene una regularizacion pendiente o en proceso.', 1;

        IF NOT EXISTS(SELECT 1 FROM dbo.AreaProduccion WHERE Activo = 1 AND EsInicio = 1)
            OR NOT EXISTS(SELECT 1 FROM dbo.AreaProduccion WHERE Activo = 1 AND EsTermino = 1)
            THROW 51000, 'Configure las areas activas de inicio y termino.', 1;

        DECLARE @Pendientes TABLE
        (
            IdOrdenCompraInternaDetalle INT PRIMARY KEY,
            CantidadPendiente DECIMAL(18,2) NOT NULL
        );

        INSERT @Pendientes(IdOrdenCompraInternaDetalle, CantidadPendiente)
        SELECT D.IdOrdenCompraInternaDetalle, CONVERT(DECIMAL(18,2), D.CantidadPendiente)
        FROM dbo.OrdenTrabajoDetalle D
        WHERE D.IdOrdenTrabajo = @IdOrdenTrabajoOrigen
          AND D.Estado <> 'ANULADO'
          AND D.CantidadPendiente > 0;

        IF NOT EXISTS(SELECT 1 FROM @Pendientes)
            THROW 51000, 'La OT origen no tiene productos pendientes por regularizar.', 1;

        IF (SELECT COUNT(*) FROM @Detalles) <> (SELECT COUNT(*) FROM @Pendientes)
            THROW 51000, 'La regularizacion debe incluir todos los productos pendientes de la OT origen.', 1;

        IF EXISTS
        (
            SELECT 1
            FROM @Detalles X
            LEFT JOIN @Pendientes P ON P.IdOrdenCompraInternaDetalle = X.IdOrdenCompraInternaDetalle
            WHERE P.IdOrdenCompraInternaDetalle IS NULL
               OR X.CantidadPlanificada <= 0
               OR X.CantidadPlanificada > P.CantidadPendiente
        )
            THROW 51000, 'La planificacion contiene productos sin pendiente o cantidades no validas.', 1;

        DECLARE @Correlativo INT =
            ISNULL((SELECT MAX(TRY_CONVERT(INT, RIGHT(NumeroOT, 6))) FROM dbo.OrdenTrabajo WITH(UPDLOCK, HOLDLOCK)), 0) + 1;
        SET @NumeroOT = CONCAT('OT-', RIGHT(CONCAT('000000', @Correlativo), 6));

        INSERT dbo.OrdenTrabajo
        (
            NumeroOT,
            IdOrdenCompraInterna,
            IdCliente,
            NombreCliente,
            IdUsuarioCreacion,
            Observacion,
            Estado,
            TipoOT,
            IdOrdenTrabajoRelacionada
        )
        SELECT
            @NumeroOT,
            O.IdOrdenCompraInterna,
            O.IdCliente,
            O.NombreCliente,
            @IdUsuario,
            ISNULL(@Observacion, N''),
            'PENDIENTE',
            'OT',
            @IdOrdenTrabajoOrigen
        FROM dbo.OrdenesCompraInterna O
        WHERE O.IdOrdenCompraInterna = @IdOrdenCompraInterna
          AND O.Estado <> 'Anulado';

        IF @@ROWCOUNT = 0
            THROW 51000, 'La OCI no existe o esta anulada.', 1;

        SET @IdOrdenTrabajo = CONVERT(INT, SCOPE_IDENTITY());

        INSERT dbo.OrdenTrabajoDetalle
        (
            IdOrdenTrabajo,
            IdOrdenCompraInternaDetalle,
            IdProducto,
            CodigoProducto,
            NombreProducto,
            CantidadRequerida,
            CantidadPlanificada,
            CantidadPendiente
        )
        SELECT
            @IdOrdenTrabajo,
            OTD.IdOrdenCompraInternaDetalle,
            OTD.IdProducto,
            OTD.CodigoProducto,
            OTD.NombreProducto,
            P.CantidadPendiente,
            X.CantidadPlanificada,
            X.CantidadPlanificada
        FROM @Detalles X
        JOIN dbo.OrdenTrabajoDetalle OTD ON OTD.IdOrdenCompraInternaDetalle = X.IdOrdenCompraInternaDetalle
                                       AND OTD.IdOrdenTrabajo = @IdOrdenTrabajoOrigen
        JOIN @Pendientes P ON P.IdOrdenCompraInternaDetalle = OTD.IdOrdenCompraInternaDetalle;

        INSERT dbo.OrdenTrabajoDetalleArea
        (
            IdOrdenTrabajo,
            IdDetalleOT,
            IdAreaProduccion,
            CodigoArea,
            NombreArea,
            OrdenSecuencia,
            EsInicio,
            EsTermino,
            ManejaMerma,
            ModoEnvio
        )
        SELECT
            @IdOrdenTrabajo,
            D.IdDetalleOT,
            A.IdAreaProduccion,
            A.CodigoArea,
            A.NombreArea,
            A.OrdenSecuencia,
            A.EsInicio,
            A.EsTermino,
            A.ManejaMerma,
            A.ModoEnvio
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
