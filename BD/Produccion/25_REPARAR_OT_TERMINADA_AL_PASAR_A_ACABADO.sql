/*
   Repara una OT afectada por la versión anterior de USP_PRO_OT_TRANSFERIR.
   Caso: el producto llegó a Acabado, pero fue marcado como TERMINADO sin
   registrar el ingreso real a productos terminados.

   Por seguridad, cambiar @NumeroOT únicamente por la OT que se verificó.
*/
SET XACT_ABORT ON;
GO

DECLARE @NumeroOT VARCHAR(30) = 'OT-000005';
DECLARE @IdOrdenTrabajo INT =
(
    SELECT IdOrdenTrabajo
    FROM dbo.OrdenTrabajo
    WHERE NumeroOT = @NumeroOT
);

IF @IdOrdenTrabajo IS NULL
    THROW 51000, 'No se encontró la OT indicada.', 1;

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.OrdenTrabajoDetalle D
    INNER JOIN dbo.OrdenTrabajoDetalleArea A ON A.IdDetalleOT = D.IdDetalleOT
    WHERE D.IdOrdenTrabajo = @IdOrdenTrabajo
      AND A.EsTermino = 1
      AND A.CantidadPendiente > 0
      AND D.Estado IN ('TERMINADO', 'TERMINADA')
      AND D.CantidadProducida > 0
)
    THROW 51000, 'La OT no coincide con el caso de terminación incorrecta; no se modificó ningún registro.', 1;

BEGIN TRAN;

/* El saldo que todavía está en Acabado no debe figurar como producido. */
UPDATE D
SET CantidadProducida = CASE
        WHEN D.CantidadProducida - A.CantidadPendiente > 0
            THEN D.CantidadProducida - A.CantidadPendiente
        ELSE 0
    END,
    CantidadAplicada = CASE
        WHEN D.CantidadProducida - A.CantidadPendiente > 0
            THEN CASE
                WHEN D.CantidadProducida - A.CantidadPendiente > D.CantidadRequerida
                    THEN D.CantidadRequerida
                ELSE D.CantidadProducida - A.CantidadPendiente
            END
        ELSE 0
    END,
    CantidadExcedente = 0,
    CantidadPendiente = CASE
        WHEN D.CantidadRequerida -
             CASE WHEN D.CantidadProducida - A.CantidadPendiente > 0
                  THEN D.CantidadProducida - A.CantidadPendiente
                  ELSE 0 END > 0
            THEN D.CantidadRequerida -
                 CASE WHEN D.CantidadProducida - A.CantidadPendiente > 0
                      THEN D.CantidadProducida - A.CantidadPendiente
                      ELSE 0 END
        ELSE 0
    END,
    Estado = CASE
        WHEN D.CantidadLanzada > 0 THEN 'EN_PROCESO'
        ELSE 'PENDIENTE'
    END,
    FechaFin = NULL
FROM dbo.OrdenTrabajoDetalle D
INNER JOIN dbo.OrdenTrabajoDetalleArea A ON A.IdDetalleOT = D.IdDetalleOT
WHERE D.IdOrdenTrabajo = @IdOrdenTrabajo
  AND A.EsTermino = 1
  AND A.CantidadPendiente > 0
  AND D.Estado IN ('TERMINADO', 'TERMINADA');

UPDATE A
SET Estado = 'EN_PROCESO',
    FechaFin = NULL
FROM dbo.OrdenTrabajoDetalleArea A
WHERE A.IdOrdenTrabajo = @IdOrdenTrabajo
  AND A.EsTermino = 1
  AND A.CantidadPendiente > 0;

UPDATE O
SET Estado = 'EN_PROCESO'
FROM dbo.OrdenTrabajo O
WHERE O.IdOrdenTrabajo = @IdOrdenTrabajo
  AND UPPER(LTRIM(RTRIM(O.Estado))) IN ('TERMINADO', 'TERMINADA', 'PARCIAL');

COMMIT;

SELECT
    O.NumeroOT,
    O.Estado AS EstadoOT,
    D.CodigoProducto,
    D.CantidadProducida,
    D.CantidadPendiente,
    D.Estado AS EstadoDetalle,
    A.NombreArea,
    A.CantidadPendiente AS PendienteEnAreaFinal,
    A.Estado AS EstadoAreaFinal
FROM dbo.OrdenTrabajo O
INNER JOIN dbo.OrdenTrabajoDetalle D ON D.IdOrdenTrabajo = O.IdOrdenTrabajo
INNER JOIN dbo.OrdenTrabajoDetalleArea A ON A.IdDetalleOT = D.IdDetalleOT
WHERE O.IdOrdenTrabajo = @IdOrdenTrabajo
  AND A.EsTermino = 1;
GO
