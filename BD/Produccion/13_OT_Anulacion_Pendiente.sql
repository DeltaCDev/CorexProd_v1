SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

IF COL_LENGTH('dbo.OrdenTrabajo', 'MotivoAnulacion') IS NULL
    ALTER TABLE dbo.OrdenTrabajo ADD MotivoAnulacion VARCHAR(500) NULL;
GO

IF COL_LENGTH('dbo.OrdenTrabajo', 'UsuarioAnulacion') IS NULL
    ALTER TABLE dbo.OrdenTrabajo ADD UsuarioAnulacion VARCHAR(80) NULL;
GO

IF COL_LENGTH('dbo.OrdenTrabajo', 'FechaAnulacion') IS NULL
    ALTER TABLE dbo.OrdenTrabajo ADD FechaAnulacion DATETIME NULL;
GO

UPDATE dbo.OrdenTrabajo
SET MotivoAnulacion = 'No registrado (anulacion anterior)'
WHERE UPPER(Estado) IN ('ANULADA', 'ANULADO')
  AND NULLIF(LTRIM(RTRIM(MotivoAnulacion)), '') IS NULL;
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_ANULAR
    @IdOrdenTrabajo INT,
    @ConvertirProcesoAMerma BIT = 0,
    @IdUsuarioSesion INT = NULL,
    @MotivoAnulacion VARCHAR(500) = '',
    @UsuarioAnulacion VARCHAR(80) = ''
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        DECLARE @IdOrdenCompraInterna INT;
        DECLARE @Estado VARCHAR(20);
        DECLARE @EsProceso BIT = 0;
        DECLARE @IdUsuarioMerma INT;

        SET @MotivoAnulacion = LTRIM(RTRIM(ISNULL(@MotivoAnulacion, '')));
        SET @UsuarioAnulacion = LTRIM(RTRIM(ISNULL(@UsuarioAnulacion, '')));

        IF @MotivoAnulacion = ''
            THROW 51000, 'Ingrese el motivo de anulacion.', 1;

        IF @UsuarioAnulacion = ''
            SET @UsuarioAnulacion = 'Sistema';

        SELECT
            @IdOrdenCompraInterna = IdOrdenCompraInterna,
            @Estado = UPPER(Estado),
            @IdUsuarioMerma = IdUsuarioCreacion
        FROM dbo.OrdenTrabajo WITH (UPDLOCK, HOLDLOCK)
        WHERE IdOrdenTrabajo = @IdOrdenTrabajo;

        IF @IdOrdenCompraInterna IS NULL
            THROW 51000, 'La OT seleccionada no existe.', 1;

        IF @Estado = 'ANULADA'
            THROW 51000, 'La OT seleccionada ya se encuentra anulada.', 1;

        SET @IdUsuarioMerma = ISNULL(NULLIF(@IdUsuarioSesion, 0), @IdUsuarioMerma);

        IF @Estado NOT IN ('PENDIENTE', 'EMITIDA', 'EN_PROCESO', 'PROCESO')
            THROW 51000, 'Solo se puede anular una OT en estado Pendiente o En Proceso sin productos terminados.', 1;

        SET @EsProceso = CASE WHEN @Estado IN ('EN_PROCESO', 'PROCESO') THEN 1 ELSE 0 END;

        IF @EsProceso = 0 AND EXISTS
        (
            SELECT 1
            FROM dbo.OrdenTrabajoDetalle
            WHERE IdOrdenTrabajo = @IdOrdenTrabajo
              AND
              (
                  CantidadLanzada > 0
                  OR CantidadProducida > 0
                  OR CantidadAplicada > 0
                  OR Estado NOT IN ('PENDIENTE', 'ANULADO')
              )
        )
            THROW 51000, 'La OT ya tiene movimiento de produccion y no puede anularse.', 1;

        IF @EsProceso = 0
           AND
           (
               EXISTS (SELECT 1 FROM dbo.OrdenTrabajoTransferencia WHERE IdOrdenTrabajo = @IdOrdenTrabajo)
               OR EXISTS (SELECT 1 FROM dbo.OrdenTrabajoTerminacion WHERE IdOrdenTrabajo = @IdOrdenTrabajo)
               OR EXISTS (SELECT 1 FROM dbo.OrdenTrabajoMerma WHERE IdOrdenTrabajo = @IdOrdenTrabajo)
               OR EXISTS (SELECT 1 FROM dbo.OrdenTrabajoConsumoInsumo WHERE IdOrdenTrabajo = @IdOrdenTrabajo)
           )
            THROW 51000, 'La OT ya tiene movimientos registrados y no puede anularse.', 1;

        IF @EsProceso = 1
        BEGIN
            IF @ConvertirProcesoAMerma = 0
                THROW 51000, 'Confirme la conversion de productos en proceso a merma para anular la OT.', 1;

            IF EXISTS
            (
                SELECT 1
                FROM dbo.OrdenTrabajoDetalle
                WHERE IdOrdenTrabajo = @IdOrdenTrabajo
                  AND (Estado = 'TERMINADO' OR CantidadProducida > 0)
            )
                THROW 51000, 'La OT tiene productos terminados y no puede anularse.', 1;

            IF EXISTS (SELECT 1 FROM dbo.OrdenTrabajoTerminacion WHERE IdOrdenTrabajo = @IdOrdenTrabajo)
                THROW 51000, 'La OT tiene productos ingresados como terminados y no puede anularse.', 1;

            INSERT dbo.OrdenTrabajoMerma
            (
                IdOrdenTrabajo,
                IdDetalleOT,
                IdDetalleArea,
                Cantidad,
                Motivo,
                Observacion,
                IdUsuarioSesion,
                IdUsuarioAutoriza
            )
            SELECT
                A.IdOrdenTrabajo,
                A.IdDetalleOT,
                A.IdDetalleArea,
                A.CantidadPendiente,
                N'ANULACION DE OT EN PROCESO',
                N'Saldo en proceso convertido a merma por anulacion de OT.',
                @IdUsuarioMerma,
                @IdUsuarioMerma
            FROM dbo.OrdenTrabajoDetalleArea A
            WHERE A.IdOrdenTrabajo = @IdOrdenTrabajo
              AND A.CantidadPendiente > 0
              AND A.Estado NOT IN ('FINALIZADA', 'BLOQUEADA', 'ANULADA');

            UPDATE dbo.OrdenTrabajoDetalleArea
            SET CantidadMerma = CantidadRecibida - CantidadEnviada,
                Estado = 'FINALIZADA',
                FechaFin = SYSDATETIME()
            WHERE IdOrdenTrabajo = @IdOrdenTrabajo
              AND CantidadPendiente > 0
              AND Estado NOT IN ('FINALIZADA', 'BLOQUEADA', 'ANULADA');
        END
        ELSE
        BEGIN
            UPDATE dbo.OrdenTrabajoDetalleArea
            SET Estado = 'ANULADA'
            WHERE IdOrdenTrabajo = @IdOrdenTrabajo;
        END

        UPDATE dbo.OrdenTrabajoDetalle
        SET Estado = 'ANULADO',
            CantidadLanzada = 0,
            CantidadPendiente = 0
        WHERE IdOrdenTrabajo = @IdOrdenTrabajo;

        UPDATE dbo.OrdenTrabajo
        SET Estado = 'ANULADA',
            MotivoAnulacion = @MotivoAnulacion,
            UsuarioAnulacion = @UsuarioAnulacion,
            FechaAnulacion = GETDATE()
        WHERE IdOrdenTrabajo = @IdOrdenTrabajo;

        UPDATE O
        SET TieneOrdenTrabajo = CASE
                WHEN EXISTS
                (
                    SELECT 1
                    FROM dbo.OrdenTrabajo OT
                    WHERE OT.IdOrdenCompraInterna = O.IdOrdenCompraInterna
                      AND UPPER(OT.Estado) <> 'ANULADA'
                ) THEN 1 ELSE 0 END,
            Estado = CASE
                WHEN O.Estado IN ('Anulada', 'Anulado') THEN 'Anulado'
                WHEN NOT EXISTS
                (
                    SELECT 1
                    FROM dbo.OrdenCompraInternaDetalle D
                    WHERE D.IdOrdenCompraInterna = O.IdOrdenCompraInterna
                      AND D.CantidadDespachada < D.Cantidad
                ) THEN 'Entregado'
                WHEN EXISTS
                (
                    SELECT 1
                    FROM dbo.OrdenCompraInternaDetalle D
                    WHERE D.IdOrdenCompraInterna = O.IdOrdenCompraInterna
                      AND D.CantidadDespachada > 0
                ) THEN 'Parcial'
                WHEN EXISTS
                (
                    SELECT 1
                    FROM dbo.OrdenTrabajo OT
                    WHERE OT.IdOrdenCompraInterna = O.IdOrdenCompraInterna
                      AND UPPER(OT.Estado) <> 'ANULADA'
                ) THEN 'PROCESO'
                ELSE 'Emitida'
            END
        FROM dbo.OrdenesCompraInterna O
        WHERE O.IdOrdenCompraInterna = @IdOrdenCompraInterna;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO
