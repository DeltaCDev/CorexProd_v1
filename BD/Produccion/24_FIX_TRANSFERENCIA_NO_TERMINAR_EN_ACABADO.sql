/*
   Corrección del flujo de producción:
   transferir desde Confección hacia Acabado NO termina el producto.
   La terminación debe ocurrir únicamente al registrar el ingreso a
   productos terminados mediante USP_PRO_OT_TERMINAR.
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_TRANSFERIR
    @IdOrdenTrabajo INT,
    @IdAreaOrigen INT,
    @IdUsuarioSesion INT,
    @IdUsuarioAutoriza INT,
    @Observacion NVARCHAR(500),
    @Detalles dbo.TipoOTTransferencia READONLY,
    @IdOperacion BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        IF NOT EXISTS (SELECT 1 FROM @Detalles)
            THROW 51000, 'Seleccione al menos un producto.', 1;

        IF EXISTS
        (
            SELECT 1
            FROM dbo.OrdenTrabajo WITH (UPDLOCK, HOLDLOCK)
            WHERE IdOrdenTrabajo = @IdOrdenTrabajo
              AND UPPER(LTRIM(RTRIM(Estado))) IN ('ANULADA', 'ANULADO')
        )
            THROW 51000, 'La OT está anulada. No se pueden transferir productos entre áreas.', 1;

        IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE IdUsuario = @IdUsuarioSesion AND Estado = 1)
           OR NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE IdUsuario = @IdUsuarioAutoriza AND Estado = 1)
            THROW 51000, 'El usuario de sesión o autorizador no es válido.', 1;

        DECLARE @OrdenOrigen INT =
        (
            SELECT OrdenSecuencia
            FROM dbo.AreaProduccion
            WHERE IdAreaProduccion = @IdAreaOrigen
        );

        DECLARE @IdAreaDestino INT =
        (
            SELECT TOP (1) IdAreaProduccion
            FROM dbo.AreaProduccion
            WHERE Activo = 1
              AND OrdenSecuencia > @OrdenOrigen
            ORDER BY OrdenSecuencia
        );

        IF @IdAreaDestino IS NULL
            THROW 51000, 'El área seleccionada no tiene una siguiente área.', 1;

        DECLARE @Error NVARCHAR(2048);

        SELECT TOP (1)
            @Error = CONCAT(
                'Producto ', ISNULL(A.CodigoProducto, CONVERT(VARCHAR(20), X.IdDetalleOT)), ': ',
                CASE
                    WHEN A.IdDetalleArea IS NULL THEN 'no pertenece a la OT o no está en el área de origen'
                    WHEN D.Estado IN ('TERMINADO', 'ANULADO')
                      OR A.Estado IN ('FINALIZADA', 'BLOQUEADA', 'ANULADA') THEN 'está finalizado o bloqueado'
                    WHEN X.Cantidad <= 0 THEN 'la cantidad debe ser mayor a cero'
                    WHEN X.Cantidad > A.CantidadPendiente THEN 'la cantidad supera el pendiente disponible'
                    WHEN A.ModoEnvio = 'UNICO'
                     AND (A.CantidadEnviada > 0 OR X.Cantidad <> A.CantidadPendiente)
                        THEN 'el modo ÚNICO exige un solo envío por todo el saldo'
                    WHEN Dest.IdDetalleArea IS NULL THEN 'no tiene configurada el área de destino'
                END)
        FROM @Detalles X
        LEFT JOIN
        (
            SELECT DA.*, D0.CodigoProducto
            FROM dbo.OrdenTrabajoDetalleArea DA
            INNER JOIN dbo.OrdenTrabajoDetalle D0 ON D0.IdDetalleOT = DA.IdDetalleOT
            WHERE DA.IdOrdenTrabajo = @IdOrdenTrabajo
              AND DA.IdAreaProduccion = @IdAreaOrigen
        ) A ON A.IdDetalleOT = X.IdDetalleOT
        LEFT JOIN dbo.OrdenTrabajoDetalle D ON D.IdDetalleOT = X.IdDetalleOT
        LEFT JOIN dbo.OrdenTrabajoDetalleArea Dest
               ON Dest.IdDetalleOT = X.IdDetalleOT
              AND Dest.IdAreaProduccion = @IdAreaDestino
        WHERE A.IdDetalleArea IS NULL
           OR D.Estado IN ('TERMINADO', 'ANULADO')
           OR A.Estado IN ('FINALIZADA', 'BLOQUEADA', 'ANULADA')
           OR X.Cantidad <= 0
           OR X.Cantidad > A.CantidadPendiente
           OR (A.ModoEnvio = 'UNICO' AND (A.CantidadEnviada > 0 OR X.Cantidad <> A.CantidadPendiente))
           OR Dest.IdDetalleArea IS NULL;

        IF @Error IS NOT NULL
            THROW 51000, @Error, 1;

        INSERT dbo.OrdenTrabajoTransferencia
        (
            IdOrdenTrabajo, IdAreaOrigen, IdAreaDestino,
            IdUsuarioSesion, IdUsuarioAutoriza, Observacion
        )
        VALUES
        (
            @IdOrdenTrabajo, @IdAreaOrigen, @IdAreaDestino,
            @IdUsuarioSesion, @IdUsuarioAutoriza, ISNULL(@Observacion, N'')
        );

        SET @IdOperacion = SCOPE_IDENTITY();

        INSERT dbo.OrdenTrabajoTransferenciaDetalle
        (
            IdOperacionTransferencia, IdDetalleOT, IdDetalleAreaOrigen,
            IdDetalleAreaDestino, CantidadEnviada, IdUsuarioSesion, IdUsuarioAutoriza
        )
        SELECT
            @IdOperacion, X.IdDetalleOT, O.IdDetalleArea,
            Dest.IdDetalleArea, X.Cantidad, @IdUsuarioSesion, @IdUsuarioAutoriza
        FROM @Detalles X
        INNER JOIN dbo.OrdenTrabajoDetalleArea O
                ON O.IdDetalleOT = X.IdDetalleOT
               AND O.IdAreaProduccion = @IdAreaOrigen
        INNER JOIN dbo.OrdenTrabajoDetalleArea Dest
                ON Dest.IdDetalleOT = X.IdDetalleOT
               AND Dest.IdAreaProduccion = @IdAreaDestino;

        UPDATE A
        SET CantidadEnviada = CantidadEnviada + X.Cantidad,
            Estado = CASE
                WHEN CantidadRecibida - (CantidadEnviada + X.Cantidad) - CantidadMerma <= 0
                    THEN 'FINALIZADA'
                ELSE 'PARCIAL'
            END,
            FechaFin = CASE
                WHEN CantidadRecibida - (CantidadEnviada + X.Cantidad) - CantidadMerma <= 0
                    THEN SYSDATETIME()
                ELSE NULL
            END
        FROM dbo.OrdenTrabajoDetalleArea A
        INNER JOIN @Detalles X ON X.IdDetalleOT = A.IdDetalleOT
        WHERE A.IdAreaProduccion = @IdAreaOrigen;

        UPDATE A
        SET CantidadRecibida = CantidadRecibida + X.Cantidad,
            Estado = 'EN_PROCESO',
            FechaInicio = COALESCE(FechaInicio, SYSDATETIME())
        FROM dbo.OrdenTrabajoDetalleArea A
        INNER JOIN @Detalles X ON X.IdDetalleOT = A.IdDetalleOT
        WHERE A.IdAreaProduccion = @IdAreaDestino;

        /* La transferencia no modifica CantidadProducida ni Estado del detalle. */
        UPDATE O
        SET Estado = CASE
            WHEN NOT EXISTS
            (
                SELECT 1
                FROM dbo.OrdenTrabajoDetalle
                WHERE IdOrdenTrabajo = O.IdOrdenTrabajo
                  AND Estado <> 'TERMINADO'
            ) THEN 'TERMINADA'
            WHEN EXISTS
            (
                SELECT 1
                FROM dbo.OrdenTrabajoDetalle
                WHERE IdOrdenTrabajo = O.IdOrdenTrabajo
                  AND Estado = 'TERMINADO'
            ) THEN 'PARCIAL'
            ELSE 'EN_PROCESO'
        END
        FROM dbo.OrdenTrabajo O
        WHERE O.IdOrdenTrabajo = @IdOrdenTrabajo;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO
