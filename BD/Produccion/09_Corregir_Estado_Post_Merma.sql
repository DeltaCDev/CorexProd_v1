SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

UPDATE d
SET CantidadLanzada=ini.CantidadRecibida
FROM dbo.OrdenTrabajoDetalle d
JOIN dbo.OrdenTrabajoDetalleArea ini ON ini.IdDetalleOT=d.IdDetalleOT AND ini.EsInicio=1
WHERE d.CantidadLanzada=0 AND ini.CantidadRecibida>0;

UPDATE d
SET Estado=CASE
        WHEN ISNULL(t.CantidadTerminada,0)>=d.CantidadLanzada AND d.CantidadLanzada>0 THEN 'TERMINADO'
        WHEN d.CantidadProducida>0 THEN 'PARCIAL'
        WHEN EXISTS(SELECT 1 FROM dbo.OrdenTrabajoDetalleArea a WHERE a.IdDetalleOT=d.IdDetalleOT AND (a.CantidadRecibida>0 OR a.CantidadEnviada>0 OR a.CantidadMerma>0)) THEN 'EN_PROCESO'
        ELSE 'PENDIENTE' END,
    FechaFin=CASE WHEN ISNULL(t.CantidadTerminada,0)>=d.CantidadLanzada AND d.CantidadLanzada>0 THEN d.FechaFin ELSE NULL END
FROM dbo.OrdenTrabajoDetalle d
OUTER APPLY(SELECT SUM(td.Cantidad) CantidadTerminada FROM dbo.OrdenTrabajoTerminacionDetalle td WHERE td.IdDetalleOT=d.IdDetalleOT)t
WHERE d.Estado<>'ANULADO';

UPDATE o
SET Estado=CASE
        WHEN NOT EXISTS(SELECT 1 FROM dbo.OrdenTrabajoDetalle d WHERE d.IdOrdenTrabajo=o.IdOrdenTrabajo AND d.Estado<>'TERMINADO') THEN 'TERMINADA'
        WHEN EXISTS(SELECT 1 FROM dbo.OrdenTrabajoDetalle d WHERE d.IdOrdenTrabajo=o.IdOrdenTrabajo AND d.Estado='TERMINADO') THEN 'PARCIAL'
        WHEN EXISTS(SELECT 1 FROM dbo.OrdenTrabajoDetalle d WHERE d.IdOrdenTrabajo=o.IdOrdenTrabajo AND d.Estado IN('EN_PROCESO','PARCIAL')) THEN 'EN_PROCESO'
        ELSE 'PENDIENTE' END
FROM dbo.OrdenTrabajo o
WHERE o.Estado<>'ANULADA';
GO
