/*
   Corrige OT que quedaron como TERMINADAS sin terminacion ni Kardex,
   mientras el area final conserva cantidades pendientes.
*/
SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

BEGIN TRAN;

DECLARE @Afectadas TABLE(IdOrdenTrabajo INT PRIMARY KEY);

INSERT @Afectadas(IdOrdenTrabajo)
SELECT O.IdOrdenTrabajo
FROM dbo.OrdenTrabajo O
WHERE UPPER(O.Estado) IN ('TERMINADA','TERMINADO')
  AND EXISTS
  (
      SELECT 1 FROM dbo.OrdenTrabajoDetalleArea A
      WHERE A.IdOrdenTrabajo=O.IdOrdenTrabajo
        AND A.EsTermino=1
        AND A.CantidadPendiente>0
  )
  AND NOT EXISTS
  (
      SELECT 1 FROM dbo.OrdenTrabajoTerminacion T
      WHERE T.IdOrdenTrabajo=O.IdOrdenTrabajo
  )
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.KardexProductos K
      WHERE K.IdOperacionTerminacion IN
      (
          SELECT T2.IdOperacionTerminacion
          FROM dbo.OrdenTrabajoTerminacion T2
          WHERE T2.IdOrdenTrabajo=O.IdOrdenTrabajo
      )
  );

UPDATE D
SET CantidadProducida=0,
    CantidadAplicada=0,
    CantidadExcedente=0,
    CantidadPendiente=CantidadLanzada,
    Estado=CASE WHEN CantidadLanzada>0 THEN 'EN_PROCESO' ELSE 'PENDIENTE' END,
    FechaFin=NULL
FROM dbo.OrdenTrabajoDetalle D
JOIN @Afectadas A ON A.IdOrdenTrabajo=D.IdOrdenTrabajo;

UPDATE O
SET Estado='EN_PROCESO'
FROM dbo.OrdenTrabajo O
JOIN @Afectadas A ON A.IdOrdenTrabajo=O.IdOrdenTrabajo
WHERE UPPER(O.Estado) NOT IN ('ANULADA','ANULADO');

SELECT IdOrdenTrabajo FROM @Afectadas;
COMMIT;
GO

/* Evita que el recálculo marque terminada una OT si el área final aún tiene saldo. */
CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_RECALCULAR_ESTADO
    @IdOrdenTrabajo INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE O
    SET Estado = CASE
        WHEN EXISTS
        (
            SELECT 1 FROM dbo.OrdenTrabajoDetalleArea A
            WHERE A.IdOrdenTrabajo=O.IdOrdenTrabajo AND A.EsTermino=1 AND A.CantidadPendiente>0
        ) THEN 'EN_PROCESO'
        WHEN EXISTS
        (
            SELECT 1 FROM dbo.OrdenTrabajoDetalle D
            WHERE D.IdOrdenTrabajo=O.IdOrdenTrabajo AND D.Estado IN ('EN_PROCESO','PARCIAL')
        ) THEN 'EN_PROCESO'
        WHEN EXISTS
        (
            SELECT 1 FROM dbo.OrdenTrabajoDetalle D
            WHERE D.IdOrdenTrabajo=O.IdOrdenTrabajo AND D.Estado<>'ANULADO' AND D.CantidadPendiente>0
        )
        AND EXISTS
        (
            SELECT 1 FROM dbo.OrdenTrabajoDetalle D
            WHERE D.IdOrdenTrabajo=O.IdOrdenTrabajo AND D.Estado<>'ANULADO' AND D.CantidadProducida>0
        ) THEN 'PARCIAL'
        WHEN EXISTS
        (
            SELECT 1 FROM dbo.OrdenTrabajoDetalle D
            WHERE D.IdOrdenTrabajo=O.IdOrdenTrabajo AND D.Estado<>'ANULADO' AND D.CantidadPendiente>0
        ) THEN 'PENDIENTE'
        WHEN EXISTS
        (
            SELECT 1 FROM dbo.OrdenTrabajoDetalle D
            WHERE D.IdOrdenTrabajo=O.IdOrdenTrabajo AND D.Estado<>'ANULADO'
        ) THEN 'TERMINADA'
        ELSE 'PENDIENTE'
    END
    FROM dbo.OrdenTrabajo O
    WHERE O.IdOrdenTrabajo=@IdOrdenTrabajo
      AND UPPER(O.Estado) NOT IN ('ANULADA','ANULADO');
END;
GO
