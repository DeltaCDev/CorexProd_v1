SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_RECALCULAR_ESTADO
    @IdOrdenTrabajo INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE O
    SET Estado = CASE
        WHEN EXISTS
        (
            SELECT 1
            FROM dbo.OrdenTrabajoDetalle D
            WHERE D.IdOrdenTrabajo = O.IdOrdenTrabajo
              AND D.Estado IN ('EN_PROCESO', 'PARCIAL')
        ) THEN 'EN_PROCESO'
        WHEN EXISTS
        (
            SELECT 1
            FROM dbo.OrdenTrabajoDetalle D
            WHERE D.IdOrdenTrabajo = O.IdOrdenTrabajo
              AND D.Estado <> 'ANULADO'
              AND D.CantidadPendiente > 0
        )
        AND EXISTS
        (
            SELECT 1
            FROM dbo.OrdenTrabajoDetalle D
            WHERE D.IdOrdenTrabajo = O.IdOrdenTrabajo
              AND D.Estado <> 'ANULADO'
              AND D.CantidadProducida > 0
        ) THEN 'PARCIAL'
        WHEN EXISTS
        (
            SELECT 1
            FROM dbo.OrdenTrabajoDetalle D
            WHERE D.IdOrdenTrabajo = O.IdOrdenTrabajo
              AND D.Estado <> 'ANULADO'
              AND D.CantidadPendiente > 0
        ) THEN 'PENDIENTE'
        WHEN EXISTS
        (
            SELECT 1
            FROM dbo.OrdenTrabajoDetalle D
            WHERE D.IdOrdenTrabajo = O.IdOrdenTrabajo
              AND D.Estado <> 'ANULADO'
        )
        AND NOT EXISTS
        (
            SELECT 1
            FROM dbo.OrdenTrabajoDetalle D
            WHERE D.IdOrdenTrabajo = O.IdOrdenTrabajo
              AND D.Estado <> 'ANULADO'
              AND (D.Estado <> 'TERMINADO' OR D.CantidadPendiente > 0)
        ) THEN 'TERMINADA'
        ELSE 'PENDIENTE'
    END
    FROM dbo.OrdenTrabajo O
    WHERE O.IdOrdenTrabajo = @IdOrdenTrabajo
      AND O.Estado <> 'ANULADA';
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_OrdenTrabajoDetalle_RecalcularEstado
ON dbo.OrdenTrabajoDetalle
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Ordenes TABLE(IdOrdenTrabajo INT PRIMARY KEY);

    INSERT INTO @Ordenes(IdOrdenTrabajo)
    SELECT IdOrdenTrabajo FROM inserted
    UNION
    SELECT IdOrdenTrabajo FROM deleted;

    DECLARE @IdOrdenTrabajo INT;
    DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT IdOrdenTrabajo FROM @Ordenes;

    OPEN cur;
    FETCH NEXT FROM cur INTO @IdOrdenTrabajo;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        EXEC dbo.USP_PRO_OT_RECALCULAR_ESTADO @IdOrdenTrabajo;
        FETCH NEXT FROM cur INTO @IdOrdenTrabajo;
    END;

    CLOSE cur;
    DEALLOCATE cur;
END;
GO

DECLARE @IdOrdenTrabajo INT;
DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT IdOrdenTrabajo
    FROM dbo.OrdenTrabajo
    WHERE Estado <> 'ANULADA';

OPEN cur;
FETCH NEXT FROM cur INTO @IdOrdenTrabajo;
WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC dbo.USP_PRO_OT_RECALCULAR_ESTADO @IdOrdenTrabajo;
    FETCH NEXT FROM cur INTO @IdOrdenTrabajo;
END;

CLOSE cur;
DEALLOCATE cur;
GO
