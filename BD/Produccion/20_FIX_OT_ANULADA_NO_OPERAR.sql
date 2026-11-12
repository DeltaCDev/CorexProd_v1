/* Bloquea transferencias de una OT anulada incluso si se invoca el procedimiento directamente. */
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

        IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE IdUsuario=@IdUsuarioSesion AND Estado=1)
           OR NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE IdUsuario=@IdUsuarioAutoriza AND Estado=1)
            THROW 51000, 'El usuario de sesión o autorizador no es válido.', 1;

        DECLARE @OrdenOrigen INT=(SELECT OrdenSecuencia FROM dbo.AreaProduccion WHERE IdAreaProduccion=@IdAreaOrigen);
        DECLARE @IdAreaDestino INT=(SELECT TOP(1) IdAreaProduccion FROM dbo.AreaProduccion WHERE Activo=1 AND OrdenSecuencia>@OrdenOrigen ORDER BY OrdenSecuencia);
        DECLARE @DestinoEsTermino BIT=(SELECT EsTermino FROM dbo.AreaProduccion WHERE IdAreaProduccion=@IdAreaDestino);
        IF @IdAreaDestino IS NULL THROW 51000,'El área seleccionada no tiene una siguiente área.',1;

        DECLARE @Error NVARCHAR(2048);
        SELECT TOP(1) @Error=CONCAT('Producto ',ISNULL(a.CodigoProducto,CONVERT(VARCHAR(20),x.IdDetalleOT)),': ',CASE WHEN a.IdDetalleArea IS NULL THEN 'no pertenece a la OT o no está en el área de origen' WHEN d.Estado IN('TERMINADO','ANULADO') OR a.Estado IN('FINALIZADA','BLOQUEADA','ANULADA') THEN 'está finalizado o bloqueado' WHEN x.Cantidad<=0 THEN 'la cantidad debe ser mayor a cero' WHEN x.Cantidad>a.CantidadPendiente THEN 'la cantidad supera el pendiente disponible' WHEN a.ModoEnvio='UNICO' AND (a.CantidadEnviada>0 OR x.Cantidad<>a.CantidadPendiente) THEN 'el modo ÚNICO exige un solo envío por todo el saldo' WHEN dest.IdDetalleArea IS NULL THEN 'no tiene configurada el área de destino' END)
        FROM @Detalles x
        LEFT JOIN (SELECT da.*,d.CodigoProducto FROM dbo.OrdenTrabajoDetalleArea da JOIN dbo.OrdenTrabajoDetalle d ON d.IdDetalleOT=da.IdDetalleOT WHERE da.IdOrdenTrabajo=@IdOrdenTrabajo AND da.IdAreaProduccion=@IdAreaOrigen) a ON a.IdDetalleOT=x.IdDetalleOT
        LEFT JOIN dbo.OrdenTrabajoDetalle d ON d.IdDetalleOT=x.IdDetalleOT
        LEFT JOIN dbo.OrdenTrabajoDetalleArea dest ON dest.IdDetalleOT=x.IdDetalleOT AND dest.IdAreaProduccion=@IdAreaDestino
        WHERE a.IdDetalleArea IS NULL OR d.Estado IN('TERMINADO','ANULADO') OR a.Estado IN('FINALIZADA','BLOQUEADA','ANULADA') OR x.Cantidad<=0 OR x.Cantidad>a.CantidadPendiente OR (a.ModoEnvio='UNICO' AND (a.CantidadEnviada>0 OR x.Cantidad<>a.CantidadPendiente)) OR dest.IdDetalleArea IS NULL;
        IF @Error IS NOT NULL THROW 51000,@Error,1;

        INSERT dbo.OrdenTrabajoTransferencia(IdOrdenTrabajo,IdAreaOrigen,IdAreaDestino,IdUsuarioSesion,IdUsuarioAutoriza,Observacion)
        VALUES(@IdOrdenTrabajo,@IdAreaOrigen,@IdAreaDestino,@IdUsuarioSesion,@IdUsuarioAutoriza,ISNULL(@Observacion,N''));
        SET @IdOperacion=SCOPE_IDENTITY();

        INSERT dbo.OrdenTrabajoTransferenciaDetalle(IdOperacionTransferencia,IdDetalleOT,IdDetalleAreaOrigen,IdDetalleAreaDestino,CantidadEnviada,IdUsuarioSesion,IdUsuarioAutoriza)
        SELECT @IdOperacion,x.IdDetalleOT,o.IdDetalleArea,d.IdDetalleArea,x.Cantidad,@IdUsuarioSesion,@IdUsuarioAutoriza
        FROM @Detalles x JOIN dbo.OrdenTrabajoDetalleArea o ON o.IdDetalleOT=x.IdDetalleOT AND o.IdAreaProduccion=@IdAreaOrigen JOIN dbo.OrdenTrabajoDetalleArea d ON d.IdDetalleOT=x.IdDetalleOT AND d.IdAreaProduccion=@IdAreaDestino;

        UPDATE a SET CantidadEnviada=CantidadEnviada+x.Cantidad,Estado=CASE WHEN CantidadRecibida-(CantidadEnviada+x.Cantidad)-CantidadMerma<=0 THEN 'FINALIZADA' ELSE 'PARCIAL' END,FechaFin=CASE WHEN CantidadRecibida-(CantidadEnviada+x.Cantidad)-CantidadMerma<=0 THEN SYSDATETIME() ELSE NULL END
        FROM dbo.OrdenTrabajoDetalleArea a JOIN @Detalles x ON x.IdDetalleOT=a.IdDetalleOT WHERE a.IdAreaProduccion=@IdAreaOrigen;
        UPDATE a SET CantidadRecibida=CantidadRecibida+x.Cantidad,Estado='EN_PROCESO',FechaInicio=COALESCE(FechaInicio,SYSDATETIME())
        FROM dbo.OrdenTrabajoDetalleArea a JOIN @Detalles x ON x.IdDetalleOT=a.IdDetalleOT WHERE a.IdAreaProduccion=@IdAreaDestino;

        IF @DestinoEsTermino=1
        BEGIN
            UPDATE d SET CantidadProducida=d.CantidadProducida+x.Cantidad,
                CantidadAplicada=CASE WHEN d.CantidadProducida+x.Cantidad>d.CantidadRequerida THEN d.CantidadRequerida ELSE d.CantidadProducida+x.Cantidad END,
                CantidadExcedente=CASE WHEN d.CantidadProducida+x.Cantidad>d.CantidadRequerida THEN d.CantidadProducida+x.Cantidad-d.CantidadRequerida ELSE 0 END,
                CantidadPendiente=CASE WHEN d.CantidadRequerida-d.CantidadProducida-x.Cantidad>0 THEN d.CantidadRequerida-d.CantidadProducida-x.Cantidad ELSE 0 END,
                Estado=CASE WHEN d.CantidadProducida+x.Cantidad+ISNULL(m.TotalMerma,0)>=d.CantidadLanzada THEN 'TERMINADO' ELSE 'PARCIAL' END,
                FechaFin=CASE WHEN d.CantidadProducida+x.Cantidad+ISNULL(m.TotalMerma,0)>=d.CantidadLanzada THEN SYSDATETIME() ELSE NULL END
            FROM dbo.OrdenTrabajoDetalle d JOIN @Detalles x ON x.IdDetalleOT=d.IdDetalleOT
            OUTER APPLY(SELECT SUM(CantidadMerma) TotalMerma FROM dbo.OrdenTrabajoDetalleArea WHERE IdDetalleOT=d.IdDetalleOT)m;
        END;

        UPDATE o SET Estado=CASE WHEN NOT EXISTS(SELECT 1 FROM dbo.OrdenTrabajoDetalle WHERE IdOrdenTrabajo=o.IdOrdenTrabajo AND Estado<>'TERMINADO') THEN 'TERMINADA' WHEN EXISTS(SELECT 1 FROM dbo.OrdenTrabajoDetalle WHERE IdOrdenTrabajo=o.IdOrdenTrabajo AND Estado='TERMINADO') THEN 'PARCIAL' ELSE 'EN_PROCESO' END
        FROM dbo.OrdenTrabajo o WHERE o.IdOrdenTrabajo=@IdOrdenTrabajo;
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK;
        THROW;
    END CATCH
END;
GO
