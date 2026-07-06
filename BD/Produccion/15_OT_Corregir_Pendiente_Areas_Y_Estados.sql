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

CREATE OR ALTER PROCEDURE dbo.USP_VEN_OCI_LISTAR
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        O.IdOrdenCompraInterna,
        O.NumeroOci,
        ISNULL(O.IdProforma, 0) AS IdProforma,
        ISNULL(P.SerieNumero, '') AS NumeroProforma,
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
        CAST(CASE WHEN O.Estado <> 'Anulado'
             AND NOT EXISTS
             (
                SELECT 1
                FROM dbo.OrdenTrabajo OT
                WHERE OT.IdOrdenCompraInterna = O.IdOrdenCompraInterna
                  AND UPPER(OT.Estado) IN ('PENDIENTE','EMITIDA','EN_PROCESO','PROCESO','PARCIAL')
             )
             AND EXISTS
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
                SELECT SUM(R.Cantidad - R.CantidadAplicada) AS StockProceso
                FROM dbo.StockProcesoReserva R
                WHERE R.IdProducto = D.IdProducto
                  AND R.Estado IN ('DISPONIBLE','RESERVADO')
                  AND R.Cantidad - R.CantidadAplicada > 0
            ) RES
            OUTER APPLY
            (
                SELECT SUM(DA.CantidadPendiente) AS StockProceso
                FROM dbo.OrdenTrabajoDetalle OD
                JOIN dbo.OrdenTrabajoDetalleArea DA ON DA.IdDetalleOT = OD.IdDetalleOT
                JOIN dbo.OrdenTrabajo OT ON OT.IdOrdenTrabajo = OD.IdOrdenTrabajo
                WHERE OD.IdProducto = D.IdProducto
                  AND UPPER(OT.Estado) <> 'ANULADA'
                  AND UPPER(OD.Estado) NOT IN ('TERMINADO','ANULADO')
                  AND DA.CantidadPendiente > 0
            ) PRC
            WHERE D.IdOrdenCompraInterna = O.IdOrdenCompraInterna
              AND D.Cantidad - CASE WHEN D.CantidadDespachada > ISNULL(PROD.CantidadAplicada, 0) THEN D.CantidadDespachada ELSE ISNULL(PROD.CantidadAplicada, 0) END
                  > ISNULL(SP.StockActual, 0) + ISNULL(RES.StockProceso, 0) + ISNULL(PRC.StockProceso, 0)
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
    LEFT JOIN dbo.Proformas P ON P.IdProforma = O.IdProforma
    ORDER BY O.FechaEmision DESC, O.IdOrdenCompraInterna DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_TERMINAR
    @IdOrdenTrabajo INT,@IdAreaTermino INT,@IdUsuarioSesion INT,@IdUsuarioAutoriza INT,
    @Observacion NVARCHAR(500),@Detalles dbo.TipoOTTransferencia READONLY,@IdOperacion BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        IF NOT EXISTS(SELECT 1 FROM @Detalles) THROW 51000,'Seleccione al menos un producto.',1;
        IF NOT EXISTS(SELECT 1 FROM dbo.Usuarios WHERE IdUsuario=@IdUsuarioSesion AND Estado=1)
           OR NOT EXISTS(SELECT 1 FROM dbo.Usuarios WHERE IdUsuario=@IdUsuarioAutoriza AND Estado=1)
            THROW 51000,'El usuario de sesion o autorizador no es valido.',1;
        IF NOT EXISTS(SELECT 1 FROM dbo.AreaProduccion WHERE IdAreaProduccion=@IdAreaTermino AND Activo=1 AND EsTermino=1)
            THROW 51000,'El area seleccionada no esta configurada como ultima area de produccion.',1;

        DECLARE @IdAlmacen INT;
        SELECT TOP(1) @IdAlmacen=IdAlmacen FROM dbo.Almacenes WHERE Estado=1
        ORDER BY CASE WHEN NombreAlmacen='Almacen Principal' THEN 0 ELSE 1 END,IdAlmacen;
        IF @IdAlmacen IS NULL THROW 51000,'No existe un almacen activo para recibir el producto terminado.',1;

        DECLARE @Error NVARCHAR(2048);
        SELECT TOP(1) @Error=CONCAT('Producto ',ISNULL(a.CodigoProducto,CONVERT(VARCHAR(20),x.IdDetalleOT)),': ',
            CASE WHEN a.IdDetalleArea IS NULL THEN 'no pertenece a la ultima area de la OT'
                 WHEN d.Estado IN('TERMINADO','ANULADO') OR a.Estado IN('FINALIZADA','BLOQUEADA','ANULADA') THEN 'esta finalizado o bloqueado'
                 WHEN x.Cantidad<=0 THEN 'la cantidad debe ser mayor a cero'
                 WHEN x.Cantidad>a.CantidadPendiente THEN 'la cantidad supera el pendiente disponible'
                 WHEN a.ModoEnvio='UNICO' AND (a.CantidadEnviada>0 OR x.Cantidad<>a.CantidadPendiente) THEN 'el modo UNICO exige terminar todo el saldo' END)
        FROM @Detalles x
        LEFT JOIN (SELECT da.*,d.CodigoProducto FROM dbo.OrdenTrabajoDetalleArea da JOIN dbo.OrdenTrabajoDetalle d ON d.IdDetalleOT=da.IdDetalleOT
                   WHERE da.IdOrdenTrabajo=@IdOrdenTrabajo AND da.IdAreaProduccion=@IdAreaTermino AND da.EsTermino=1) a ON a.IdDetalleOT=x.IdDetalleOT
        LEFT JOIN dbo.OrdenTrabajoDetalle d ON d.IdDetalleOT=x.IdDetalleOT
        WHERE a.IdDetalleArea IS NULL OR d.Estado IN('TERMINADO','ANULADO') OR a.Estado IN('FINALIZADA','BLOQUEADA','ANULADA')
           OR x.Cantidad<=0 OR x.Cantidad>a.CantidadPendiente OR (a.ModoEnvio='UNICO' AND (a.CantidadEnviada>0 OR x.Cantidad<>a.CantidadPendiente));
        IF @Error IS NOT NULL THROW 51000,@Error,1;

        INSERT dbo.OrdenTrabajoTerminacion(IdOrdenTrabajo,IdAreaTermino,IdUsuarioSesion,IdUsuarioAutoriza,Observacion)
        VALUES(@IdOrdenTrabajo,@IdAreaTermino,@IdUsuarioSesion,@IdUsuarioAutoriza,ISNULL(@Observacion,N''));
        SET @IdOperacion=SCOPE_IDENTITY();
        INSERT dbo.OrdenTrabajoTerminacionDetalle(IdOperacionTerminacion,IdDetalleOT,IdDetalleArea,Cantidad)
        SELECT @IdOperacion,x.IdDetalleOT,a.IdDetalleArea,x.Cantidad FROM @Detalles x
        JOIN dbo.OrdenTrabajoDetalleArea a ON a.IdDetalleOT=x.IdDetalleOT AND a.IdAreaProduccion=@IdAreaTermino;

        UPDATE a SET CantidadEnviada=CantidadEnviada+x.Cantidad,
            Estado=CASE WHEN CantidadRecibida-(CantidadEnviada+x.Cantidad)-CantidadMerma<=0 THEN 'FINALIZADA' ELSE 'PARCIAL' END,
            FechaFin=CASE WHEN CantidadRecibida-(CantidadEnviada+x.Cantidad)-CantidadMerma<=0 THEN SYSDATETIME() ELSE NULL END
        FROM dbo.OrdenTrabajoDetalleArea a JOIN @Detalles x ON x.IdDetalleOT=a.IdDetalleOT WHERE a.IdAreaProduccion=@IdAreaTermino;

        DECLARE @Ingresos TABLE(IdProducto INT PRIMARY KEY,Cantidad DECIMAL(18,2));
        INSERT @Ingresos SELECT d.IdProducto,SUM(x.Cantidad) FROM @Detalles x JOIN dbo.OrdenTrabajoDetalle d ON d.IdDetalleOT=x.IdDetalleOT GROUP BY d.IdProducto;
        INSERT dbo.StockProductosAlmacen(IdProducto,IdAlmacen,StockActual)
        SELECT i.IdProducto,@IdAlmacen,0 FROM @Ingresos i WHERE NOT EXISTS(SELECT 1 FROM dbo.StockProductosAlmacen WITH(UPDLOCK,HOLDLOCK) WHERE IdProducto=i.IdProducto AND IdAlmacen=@IdAlmacen);
        INSERT dbo.StockProductos(IdProducto,StockActual)
        SELECT i.IdProducto,0 FROM @Ingresos i WHERE NOT EXISTS(SELECT 1 FROM dbo.StockProductos WITH(UPDLOCK,HOLDLOCK) WHERE IdProducto=i.IdProducto);
        INSERT dbo.KardexProductos(TipoMovimiento,IdIngresoManualStock,IdProducto,IdAlmacen,StockAnterior,Cantidad,StockResultante,UsuarioResponsable,FechaMovimiento,Observacion,IdOperacionTerminacion)
        SELECT 'INGRESO PRODUCCION',NULL,i.IdProducto,@IdAlmacen,s.StockActual,i.Cantidad,s.StockActual+i.Cantidad,
               ISNULL(u.NombreUsuario,CONVERT(VARCHAR(20),@IdUsuarioSesion)),SYSDATETIME(),CONCAT('Producto terminado - OT ',ot.NumeroOT),@IdOperacion
        FROM @Ingresos i JOIN dbo.StockProductosAlmacen s ON s.IdProducto=i.IdProducto AND s.IdAlmacen=@IdAlmacen
        LEFT JOIN dbo.Usuarios u ON u.IdUsuario=@IdUsuarioSesion CROSS JOIN dbo.OrdenTrabajo ot WHERE ot.IdOrdenTrabajo=@IdOrdenTrabajo;
        UPDATE s SET StockActual=s.StockActual+i.Cantidad,FechaActualizacion=GETDATE() FROM dbo.StockProductosAlmacen s JOIN @Ingresos i ON i.IdProducto=s.IdProducto WHERE s.IdAlmacen=@IdAlmacen;
        UPDATE s SET StockActual=s.StockActual+i.Cantidad,FechaActualizacion=GETDATE() FROM dbo.StockProductos s JOIN @Ingresos i ON i.IdProducto=s.IdProducto;

        UPDATE d SET CantidadProducida=d.CantidadProducida+x.Cantidad,
            CantidadAplicada=CASE WHEN d.CantidadProducida+x.Cantidad>d.CantidadRequerida THEN d.CantidadRequerida ELSE d.CantidadProducida+x.Cantidad END,
            CantidadExcedente=CASE WHEN d.CantidadProducida+x.Cantidad>d.CantidadRequerida THEN d.CantidadProducida+x.Cantidad-d.CantidadRequerida ELSE 0 END,
            CantidadPendiente=CASE WHEN d.CantidadRequerida-d.CantidadProducida-x.Cantidad>0 THEN d.CantidadRequerida-d.CantidadProducida-x.Cantidad ELSE 0 END,
            Estado=CASE WHEN d.CantidadProducida+x.Cantidad+ISNULL(m.TotalMerma,0)>=d.CantidadLanzada THEN 'TERMINADO' ELSE 'PARCIAL' END,
            FechaFin=CASE WHEN d.CantidadProducida+x.Cantidad+ISNULL(m.TotalMerma,0)>=d.CantidadLanzada THEN SYSDATETIME() ELSE NULL END
        FROM dbo.OrdenTrabajoDetalle d JOIN @Detalles x ON x.IdDetalleOT=d.IdDetalleOT
        OUTER APPLY(SELECT SUM(CantidadMerma) TotalMerma FROM dbo.OrdenTrabajoDetalleArea WHERE IdDetalleOT=d.IdDetalleOT)m;

        EXEC dbo.USP_PRO_OT_RECALCULAR_ESTADO @IdOrdenTrabajo;
        COMMIT;
    END TRY
    BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH
END;
GO

UPDATE A
SET Estado = CASE
        WHEN A.Estado IN ('ANULADA','BLOQUEADA') THEN A.Estado
        WHEN A.CantidadRecibida-A.CantidadEnviada-A.CantidadMerma <= 0
             AND (A.CantidadRecibida > 0 OR A.CantidadEnviada > 0 OR A.CantidadMerma > 0) THEN 'FINALIZADA'
        WHEN A.CantidadRecibida > 0 OR A.CantidadEnviada > 0 OR A.CantidadMerma > 0 THEN 'EN_PROCESO'
        ELSE 'PENDIENTE'
    END,
    FechaFin = CASE
        WHEN A.Estado NOT IN ('ANULADA','BLOQUEADA')
             AND A.CantidadRecibida-A.CantidadEnviada-A.CantidadMerma <= 0
             AND (A.CantidadRecibida > 0 OR A.CantidadEnviada > 0 OR A.CantidadMerma > 0)
        THEN COALESCE(A.FechaFin,SYSDATETIME())
        ELSE A.FechaFin
    END
FROM dbo.OrdenTrabajoDetalleArea A
WHERE A.Estado NOT IN ('ANULADA','BLOQUEADA');
GO

UPDATE A
SET CantidadEnviada = A.CantidadRecibida - A.CantidadMerma,
    Estado = 'FINALIZADA',
    FechaFin = COALESCE(A.FechaFin,D.FechaFin,SYSDATETIME())
FROM dbo.OrdenTrabajoDetalleArea A
JOIN dbo.OrdenTrabajoDetalle D ON D.IdDetalleOT = A.IdDetalleOT
WHERE A.EsTermino = 1
  AND D.Estado = 'TERMINADO'
  AND A.Estado NOT IN ('ANULADA','BLOQUEADA')
  AND A.CantidadRecibida - A.CantidadEnviada - A.CantidadMerma > 0;
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
