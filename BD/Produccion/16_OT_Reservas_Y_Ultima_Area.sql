SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
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

    SELECT
        DA.*,
        CONVERT(DECIMAL(18,2), ISNULL(R.CantidadReservada, 0)) AS CantidadReservada,
        D.CodigoProducto,
        D.NombreProducto
    FROM dbo.OrdenTrabajoDetalleArea DA
    JOIN dbo.OrdenTrabajoDetalle D ON D.IdDetalleOT = DA.IdDetalleOT
    OUTER APPLY
    (
        SELECT SUM(SPR.Cantidad - SPR.CantidadAplicada) AS CantidadReservada
        FROM dbo.StockProcesoReserva SPR
        WHERE SPR.IdDetalleArea = DA.IdDetalleArea
          AND SPR.Estado IN ('DISPONIBLE','RESERVADO')
          AND SPR.Cantidad - SPR.CantidadAplicada > 0
    ) R
    WHERE DA.IdOrdenTrabajo = @IdOrdenTrabajo
    ORDER BY DA.OrdenSecuencia, DA.IdDetalleArea;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_TRANSFERIR
    @IdOrdenTrabajo INT,@IdAreaOrigen INT,@IdUsuarioSesion INT,@IdUsuarioAutoriza INT,
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

        DECLARE @OrdenOrigen INT=(SELECT OrdenSecuencia FROM dbo.AreaProduccion WHERE IdAreaProduccion=@IdAreaOrigen);
        DECLARE @IdAreaDestino INT=(SELECT TOP(1) IdAreaProduccion FROM dbo.AreaProduccion WHERE Activo=1 AND OrdenSecuencia>@OrdenOrigen ORDER BY OrdenSecuencia);
        IF @IdAreaDestino IS NULL THROW 51000,'El area seleccionada no tiene una siguiente area.',1;

        DECLARE @Error NVARCHAR(2048);
        SELECT TOP(1) @Error=CONCAT('Producto ',ISNULL(a.CodigoProducto,CONVERT(VARCHAR(20),x.IdDetalleOT)),': ',
            CASE WHEN a.IdDetalleArea IS NULL THEN 'no pertenece a la OT o no esta en el area de origen'
                 WHEN d.Estado IN('TERMINADO','ANULADO') OR a.Estado IN('FINALIZADA','BLOQUEADA','ANULADA') THEN 'esta finalizado o bloqueado'
                 WHEN x.Cantidad<=0 THEN 'la cantidad debe ser mayor a cero'
                 WHEN x.Cantidad>a.Disponible THEN 'la cantidad supera el pendiente disponible no reservado'
                 WHEN a.ModoEnvio='UNICO' AND (a.CantidadEnviada>0 OR x.Cantidad<>a.Disponible) THEN 'el modo UNICO exige un solo envio por todo el saldo disponible'
                 WHEN dest.IdDetalleArea IS NULL THEN 'no tiene configurada el area de destino' END)
        FROM @Detalles x
        LEFT JOIN
        (
            SELECT da.*,d.CodigoProducto,
                CONVERT(DECIMAL(18,2), CASE WHEN da.CantidadPendiente-ISNULL(r.CantidadReservada,0)>0 THEN da.CantidadPendiente-ISNULL(r.CantidadReservada,0) ELSE 0 END) AS Disponible
            FROM dbo.OrdenTrabajoDetalleArea da
            JOIN dbo.OrdenTrabajoDetalle d ON d.IdDetalleOT=da.IdDetalleOT
            OUTER APPLY
            (
                SELECT SUM(spr.Cantidad-spr.CantidadAplicada) AS CantidadReservada
                FROM dbo.StockProcesoReserva spr WITH(UPDLOCK,HOLDLOCK)
                WHERE spr.IdDetalleArea=da.IdDetalleArea
                  AND spr.Estado IN('DISPONIBLE','RESERVADO')
                  AND spr.Cantidad-spr.CantidadAplicada>0
            ) r
            WHERE da.IdOrdenTrabajo=@IdOrdenTrabajo AND da.IdAreaProduccion=@IdAreaOrigen
        ) a ON a.IdDetalleOT=x.IdDetalleOT
        LEFT JOIN dbo.OrdenTrabajoDetalle d ON d.IdDetalleOT=x.IdDetalleOT
        LEFT JOIN dbo.OrdenTrabajoDetalleArea dest ON dest.IdDetalleOT=x.IdDetalleOT AND dest.IdAreaProduccion=@IdAreaDestino
        WHERE a.IdDetalleArea IS NULL OR d.Estado IN('TERMINADO','ANULADO') OR a.Estado IN('FINALIZADA','BLOQUEADA','ANULADA')
           OR x.Cantidad<=0 OR x.Cantidad>a.Disponible OR (a.ModoEnvio='UNICO' AND (a.CantidadEnviada>0 OR x.Cantidad<>a.Disponible)) OR dest.IdDetalleArea IS NULL;
        IF @Error IS NOT NULL THROW 51000,@Error,1;

        INSERT dbo.OrdenTrabajoTransferencia(IdOrdenTrabajo,IdAreaOrigen,IdAreaDestino,IdUsuarioSesion,IdUsuarioAutoriza,Observacion)
        VALUES(@IdOrdenTrabajo,@IdAreaOrigen,@IdAreaDestino,@IdUsuarioSesion,@IdUsuarioAutoriza,ISNULL(@Observacion,N''));
        SET @IdOperacion=SCOPE_IDENTITY();

        INSERT dbo.OrdenTrabajoTransferenciaDetalle(IdOperacionTransferencia,IdDetalleOT,IdDetalleAreaOrigen,IdDetalleAreaDestino,CantidadEnviada,IdUsuarioSesion,IdUsuarioAutoriza)
        SELECT @IdOperacion,x.IdDetalleOT,o.IdDetalleArea,d.IdDetalleArea,x.Cantidad,@IdUsuarioSesion,@IdUsuarioAutoriza
        FROM @Detalles x
        JOIN dbo.OrdenTrabajoDetalleArea o ON o.IdDetalleOT=x.IdDetalleOT AND o.IdAreaProduccion=@IdAreaOrigen
        JOIN dbo.OrdenTrabajoDetalleArea d ON d.IdDetalleOT=x.IdDetalleOT AND d.IdAreaProduccion=@IdAreaDestino;

        UPDATE a SET CantidadEnviada=CantidadEnviada+x.Cantidad,
            Estado=CASE WHEN CantidadRecibida-(CantidadEnviada+x.Cantidad)-CantidadMerma<=0 THEN 'FINALIZADA' ELSE 'PARCIAL' END,
            FechaFin=CASE WHEN CantidadRecibida-(CantidadEnviada+x.Cantidad)-CantidadMerma<=0 THEN SYSDATETIME() ELSE NULL END
        FROM dbo.OrdenTrabajoDetalleArea a JOIN @Detalles x ON x.IdDetalleOT=a.IdDetalleOT WHERE a.IdAreaProduccion=@IdAreaOrigen;

        UPDATE a SET CantidadRecibida=CantidadRecibida+x.Cantidad,Estado='EN_PROCESO',FechaInicio=COALESCE(FechaInicio,SYSDATETIME())
        FROM dbo.OrdenTrabajoDetalleArea a JOIN @Detalles x ON x.IdDetalleOT=a.IdDetalleOT WHERE a.IdAreaProduccion=@IdAreaDestino;

        EXEC dbo.USP_PRO_OT_RECALCULAR_ESTADO @IdOrdenTrabajo;
        COMMIT;
    END TRY
    BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH
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
                 WHEN x.Cantidad>a.Disponible THEN 'la cantidad supera el pendiente disponible no reservado'
                 WHEN a.ModoEnvio='UNICO' AND (a.CantidadEnviada>0 OR x.Cantidad<>a.Disponible) THEN 'el modo UNICO exige terminar todo el saldo disponible' END)
        FROM @Detalles x
        LEFT JOIN
        (
            SELECT da.*,d.CodigoProducto,
                CONVERT(DECIMAL(18,2), CASE WHEN da.CantidadPendiente-ISNULL(r.CantidadReservada,0)>0 THEN da.CantidadPendiente-ISNULL(r.CantidadReservada,0) ELSE 0 END) AS Disponible
            FROM dbo.OrdenTrabajoDetalleArea da
            JOIN dbo.OrdenTrabajoDetalle d ON d.IdDetalleOT=da.IdDetalleOT
            OUTER APPLY
            (
                SELECT SUM(spr.Cantidad-spr.CantidadAplicada) AS CantidadReservada
                FROM dbo.StockProcesoReserva spr WITH(UPDLOCK,HOLDLOCK)
                WHERE spr.IdDetalleArea=da.IdDetalleArea
                  AND spr.Estado IN('DISPONIBLE','RESERVADO')
                  AND spr.Cantidad-spr.CantidadAplicada>0
            ) r
            WHERE da.IdOrdenTrabajo=@IdOrdenTrabajo AND da.IdAreaProduccion=@IdAreaTermino AND da.EsTermino=1
        ) a ON a.IdDetalleOT=x.IdDetalleOT
        LEFT JOIN dbo.OrdenTrabajoDetalle d ON d.IdDetalleOT=x.IdDetalleOT
        WHERE a.IdDetalleArea IS NULL OR d.Estado IN('TERMINADO','ANULADO') OR a.Estado IN('FINALIZADA','BLOQUEADA','ANULADA')
           OR x.Cantidad<=0 OR x.Cantidad>a.Disponible OR (a.ModoEnvio='UNICO' AND (a.CantidadEnviada>0 OR x.Cantidad<>a.Disponible));
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

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_MERMA_REGISTRAR
    @IdDetalleArea BIGINT,
    @Cantidad DECIMAL(18,2),
    @Motivo NVARCHAR(200),
    @IdUsuarioSesion INT,
    @IdUsuarioAutoriza INT,
    @Observacion NVARCHAR(500)=N''
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        DECLARE @IdOT INT,@IdDet INT,@Pendiente DECIMAL(18,2),@Reservado DECIMAL(18,2),@Maneja BIT,@Estado VARCHAR(20);
        SELECT @IdOT=IdOrdenTrabajo,@IdDet=IdDetalleOT,@Pendiente=CantidadPendiente,@Maneja=ManejaMerma,@Estado=Estado
        FROM dbo.OrdenTrabajoDetalleArea WITH(UPDLOCK,HOLDLOCK) WHERE IdDetalleArea=@IdDetalleArea;

        SELECT @Reservado=ISNULL(SUM(Cantidad-CantidadAplicada),0)
        FROM dbo.StockProcesoReserva WITH(UPDLOCK,HOLDLOCK)
        WHERE IdDetalleArea=@IdDetalleArea
          AND Estado IN('DISPONIBLE','RESERVADO')
          AND Cantidad-CantidadAplicada>0;

        IF @IdOT IS NULL THROW 51000,'No se encontro el producto en el area.',1;
        IF @Maneja<>1 THROW 51000,'El area no permite registrar merma.',1;
        IF @Estado IN('FINALIZADA','BLOQUEADA','ANULADA') OR @Pendiente<=0 THROW 51000,'El producto ya no tiene saldo pendiente en el area.',1;
        IF @Cantidad<=0 THROW 51000,'La cantidad de merma debe ser mayor a cero.',1;
        IF @Cantidad>@Pendiente-ISNULL(@Reservado,0) THROW 51000,'La merma no puede superar el pendiente disponible no reservado.',1;
        IF NULLIF(LTRIM(RTRIM(@Motivo)),N'') IS NULL THROW 51000,'Ingrese el motivo de la merma.',1;
        IF NOT EXISTS(SELECT 1 FROM dbo.Usuarios WHERE IdUsuario=@IdUsuarioSesion AND Estado=1)
           OR NOT EXISTS(SELECT 1 FROM dbo.Usuarios WHERE IdUsuario=@IdUsuarioAutoriza AND Estado=1)
            THROW 51000,'El usuario de sesion o autorizador no es valido.',1;

        UPDATE dbo.OrdenTrabajoDetalleArea
        SET CantidadMerma=CantidadMerma+@Cantidad,
            Estado=CASE WHEN CantidadPendiente-@Cantidad=0 THEN 'FINALIZADA' ELSE 'PARCIAL' END,
            FechaFin=CASE WHEN CantidadPendiente-@Cantidad=0 THEN SYSDATETIME() ELSE FechaFin END
        WHERE IdDetalleArea=@IdDetalleArea;

        INSERT dbo.OrdenTrabajoMerma(IdOrdenTrabajo,IdDetalleOT,IdDetalleArea,Cantidad,Motivo,Observacion,IdUsuarioSesion,IdUsuarioAutoriza)
        VALUES(@IdOT,@IdDet,@IdDetalleArea,@Cantidad,LTRIM(RTRIM(@Motivo)),ISNULL(@Observacion,N''),@IdUsuarioSesion,@IdUsuarioAutoriza);

        COMMIT;
    END TRY
    BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH
END;
GO

;WITH SaldosTermino AS
(
    SELECT IdDetalleOT, SUM(CantidadPendiente) AS PendienteTermino
    FROM dbo.OrdenTrabajoDetalleArea
    WHERE EsTermino = 1
      AND Estado NOT IN ('ANULADA','BLOQUEADA')
      AND CantidadPendiente > 0
    GROUP BY IdDetalleOT
)
UPDATE D
SET
    CantidadProducida = CASE WHEN D.CantidadProducida - S.PendienteTermino > 0 THEN D.CantidadProducida - S.PendienteTermino ELSE 0 END,
    CantidadAplicada = CASE
        WHEN D.CantidadAplicada > CASE WHEN D.CantidadProducida - S.PendienteTermino > 0 THEN D.CantidadProducida - S.PendienteTermino ELSE 0 END
        THEN CASE WHEN D.CantidadProducida - S.PendienteTermino > 0 THEN D.CantidadProducida - S.PendienteTermino ELSE 0 END
        ELSE D.CantidadAplicada
    END,
    CantidadExcedente = CASE
        WHEN D.CantidadProducida - S.PendienteTermino > D.CantidadRequerida THEN D.CantidadProducida - S.PendienteTermino - D.CantidadRequerida
        ELSE 0
    END,
    CantidadPendiente = CASE
        WHEN D.CantidadRequerida - (CASE WHEN D.CantidadProducida - S.PendienteTermino > 0 THEN D.CantidadProducida - S.PendienteTermino ELSE 0 END) > 0
        THEN D.CantidadRequerida - (CASE WHEN D.CantidadProducida - S.PendienteTermino > 0 THEN D.CantidadProducida - S.PendienteTermino ELSE 0 END)
        ELSE 0
    END,
    Estado = 'PARCIAL',
    FechaFin = NULL
FROM dbo.OrdenTrabajoDetalle D
JOIN SaldosTermino S ON S.IdDetalleOT = D.IdDetalleOT
WHERE D.Estado = 'TERMINADO';
GO

UPDATE D
SET Estado = CASE WHEN D.CantidadProducida > 0 THEN 'PARCIAL' ELSE 'EN_PROCESO' END,
    FechaFin = NULL
FROM dbo.OrdenTrabajoDetalle D
WHERE D.Estado = 'TERMINADO'
  AND EXISTS
  (
      SELECT 1
      FROM dbo.OrdenTrabajoDetalleArea A
      WHERE A.IdDetalleOT = D.IdDetalleOT
        AND A.Estado NOT IN ('ANULADA','BLOQUEADA')
        AND A.CantidadPendiente > 0
  );
GO

;WITH Terminados AS
(
    SELECT TD.IdDetalleOT, SUM(TD.Cantidad) AS CantidadTerminada
    FROM dbo.OrdenTrabajoTerminacionDetalle TD
    GROUP BY TD.IdDetalleOT
),
DetallesConProceso AS
(
    SELECT DISTINCT A.IdDetalleOT
    FROM dbo.OrdenTrabajoDetalleArea A
    WHERE A.Estado NOT IN ('ANULADA','BLOQUEADA')
      AND A.CantidadPendiente > 0
)
UPDATE D
SET
    CantidadProducida = ISNULL(T.CantidadTerminada, 0),
    CantidadAplicada = CASE
        WHEN ISNULL(T.CantidadTerminada, 0) > D.CantidadRequerida THEN D.CantidadRequerida
        ELSE ISNULL(T.CantidadTerminada, 0)
    END,
    CantidadExcedente = CASE
        WHEN ISNULL(T.CantidadTerminada, 0) > D.CantidadRequerida THEN ISNULL(T.CantidadTerminada, 0) - D.CantidadRequerida
        ELSE 0
    END,
    CantidadPendiente = CASE
        WHEN D.CantidadRequerida - ISNULL(T.CantidadTerminada, 0) > 0 THEN D.CantidadRequerida - ISNULL(T.CantidadTerminada, 0)
        ELSE 0
    END,
    Estado = CASE WHEN ISNULL(T.CantidadTerminada, 0) > 0 THEN 'PARCIAL' ELSE 'EN_PROCESO' END,
    FechaFin = NULL
FROM dbo.OrdenTrabajoDetalle D
JOIN DetallesConProceso P ON P.IdDetalleOT = D.IdDetalleOT
LEFT JOIN Terminados T ON T.IdDetalleOT = D.IdDetalleOT
WHERE D.Estado <> 'ANULADO'
  AND D.CantidadProducida > ISNULL(T.CantidadTerminada, 0);
GO

DECLARE @IdOrdenTrabajoRecalculo INT;
DECLARE cur_recalculo CURSOR LOCAL FAST_FORWARD FOR
    SELECT DISTINCT D.IdOrdenTrabajo
    FROM dbo.OrdenTrabajoDetalle D
    WHERE EXISTS
    (
        SELECT 1
        FROM dbo.OrdenTrabajoDetalleArea A
        WHERE A.IdDetalleOT = D.IdDetalleOT
          AND A.Estado NOT IN ('ANULADA','BLOQUEADA')
          AND A.CantidadPendiente > 0
    );

OPEN cur_recalculo;
FETCH NEXT FROM cur_recalculo INTO @IdOrdenTrabajoRecalculo;
WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC dbo.USP_PRO_OT_RECALCULAR_ESTADO @IdOrdenTrabajoRecalculo;
    FETCH NEXT FROM cur_recalculo INTO @IdOrdenTrabajoRecalculo;
END;

CLOSE cur_recalculo;
DEALLOCATE cur_recalculo;
GO
