SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

UPDATE O
SET Estado = CASE
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
    WHEN O.TieneOrdenTrabajo = 1 THEN 'PROCESO'
    ELSE 'Emitida'
END
FROM dbo.OrdenesCompraInterna O
WHERE O.Estado NOT IN ('Anulada', 'Anulado');
GO
