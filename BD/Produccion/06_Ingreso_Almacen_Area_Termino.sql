SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.KardexProductos','IdOperacionTransferencia') IS NULL
    ALTER TABLE dbo.KardexProductos ADD IdOperacionTransferencia BIGINT NULL;
GO

IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name='FK_KardexProductos_OperacionTransferencia')
    ALTER TABLE dbo.KardexProductos ADD CONSTRAINT FK_KardexProductos_OperacionTransferencia
        FOREIGN KEY(IdOperacionTransferencia) REFERENCES dbo.OrdenTrabajoTransferencia(IdOperacionTransferencia);
GO

IF EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.KardexProductos') AND name='UX_KardexProductos_OperacionTransferencia_Producto')
    DROP INDEX UX_KardexProductos_OperacionTransferencia_Producto ON dbo.KardexProductos;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.KardexProductos') AND name='IX_KardexProductos_OperacionTransferencia_Producto')
    CREATE INDEX IX_KardexProductos_OperacionTransferencia_Producto
        ON dbo.KardexProductos(IdOperacionTransferencia,IdProducto);
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_TRANSFERIR
    @IdOrdenTrabajo INT,@IdAreaOrigen INT,@IdUsuarioSesion INT,@IdUsuarioAutoriza INT,@Observacion NVARCHAR(500),@Detalles dbo.TipoOTTransferencia READONLY,@IdOperacion BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRY BEGIN TRAN;
        IF NOT EXISTS(SELECT 1 FROM @Detalles) THROW 51000,'Seleccione al menos un producto.',1;
        IF NOT EXISTS(SELECT 1 FROM dbo.Usuarios WHERE IdUsuario=@IdUsuarioSesion AND Estado=1) OR NOT EXISTS(SELECT 1 FROM dbo.Usuarios WHERE IdUsuario=@IdUsuarioAutoriza AND Estado=1) THROW 51000,'El usuario de sesion o autorizador no es valido.',1;
        DECLARE @OrdenOrigen INT=(SELECT OrdenSecuencia FROM dbo.AreaProduccion WHERE IdAreaProduccion=@IdAreaOrigen);
        DECLARE @IdAreaDestino INT=(SELECT TOP(1) IdAreaProduccion FROM dbo.AreaProduccion WHERE Activo=1 AND OrdenSecuencia>@OrdenOrigen ORDER BY OrdenSecuencia);
        DECLARE @DestinoEsTermino BIT=0;
        IF @IdAreaDestino IS NULL THROW 51000,'El area seleccionada no tiene una siguiente area.',1;
        DECLARE @IdAlmacenTermino INT;
        IF @DestinoEsTermino=1
        BEGIN
            SELECT TOP(1) @IdAlmacenTermino=IdAlmacen FROM dbo.Almacenes WHERE Estado=1 ORDER BY CASE WHEN NombreAlmacen='Almacen Principal' THEN 0 ELSE 1 END,IdAlmacen;
            IF @IdAlmacenTermino IS NULL THROW 51000,'No existe un almacen activo para recibir el producto terminado.',1;
        END;
        DECLARE @Error NVARCHAR(2048);
        SELECT TOP(1) @Error=CONCAT('Producto ',ISNULL(a.CodigoProducto,CONVERT(VARCHAR(20),x.IdDetalleOT)),': ',CASE WHEN a.IdDetalleArea IS NULL THEN 'no pertenece a la OT o no esta en el area de origen' WHEN d.Estado IN('TERMINADO','ANULADO') OR a.Estado IN('FINALIZADA','BLOQUEADA','ANULADA') THEN 'esta finalizado o bloqueado' WHEN x.Cantidad<=0 THEN 'la cantidad debe ser mayor a cero' WHEN x.Cantidad>a.CantidadPendiente THEN 'la cantidad supera el pendiente disponible' WHEN a.ModoEnvio='UNICO' AND (a.CantidadEnviada>0 OR x.Cantidad<>a.CantidadPendiente) THEN 'el modo UNICO exige un solo envio por todo el saldo' WHEN dest.IdDetalleArea IS NULL THEN 'no tiene configurada el area de destino' END)
        FROM @Detalles x LEFT JOIN (SELECT da.*,d.CodigoProducto FROM dbo.OrdenTrabajoDetalleArea da JOIN dbo.OrdenTrabajoDetalle d ON d.IdDetalleOT=da.IdDetalleOT WHERE da.IdOrdenTrabajo=@IdOrdenTrabajo AND da.IdAreaProduccion=@IdAreaOrigen) a ON a.IdDetalleOT=x.IdDetalleOT
        LEFT JOIN dbo.OrdenTrabajoDetalle d ON d.IdDetalleOT=x.IdDetalleOT LEFT JOIN dbo.OrdenTrabajoDetalleArea dest ON dest.IdDetalleOT=x.IdDetalleOT AND dest.IdAreaProduccion=@IdAreaDestino
        WHERE a.IdDetalleArea IS NULL OR d.Estado IN('TERMINADO','ANULADO') OR a.Estado IN('FINALIZADA','BLOQUEADA','ANULADA') OR x.Cantidad<=0 OR x.Cantidad>a.CantidadPendiente OR (a.ModoEnvio='UNICO' AND (a.CantidadEnviada>0 OR x.Cantidad<>a.CantidadPendiente)) OR dest.IdDetalleArea IS NULL;
        IF @Error IS NOT NULL THROW 51000,@Error,1;
        INSERT dbo.OrdenTrabajoTransferencia(IdOrdenTrabajo,IdAreaOrigen,IdAreaDestino,IdUsuarioSesion,IdUsuarioAutoriza,Observacion) VALUES(@IdOrdenTrabajo,@IdAreaOrigen,@IdAreaDestino,@IdUsuarioSesion,@IdUsuarioAutoriza,ISNULL(@Observacion,N'')); SET @IdOperacion=SCOPE_IDENTITY();
        INSERT dbo.OrdenTrabajoTransferenciaDetalle(IdOperacionTransferencia,IdDetalleOT,IdDetalleAreaOrigen,IdDetalleAreaDestino,CantidadEnviada,IdUsuarioSesion,IdUsuarioAutoriza)
        SELECT @IdOperacion,x.IdDetalleOT,o.IdDetalleArea,d.IdDetalleArea,x.Cantidad,@IdUsuarioSesion,@IdUsuarioAutoriza FROM @Detalles x JOIN dbo.OrdenTrabajoDetalleArea o ON o.IdDetalleOT=x.IdDetalleOT AND o.IdAreaProduccion=@IdAreaOrigen JOIN dbo.OrdenTrabajoDetalleArea d ON d.IdDetalleOT=x.IdDetalleOT AND d.IdAreaProduccion=@IdAreaDestino;
        UPDATE a SET CantidadEnviada=CantidadEnviada+x.Cantidad,Estado=CASE WHEN CantidadRecibida-(CantidadEnviada+x.Cantidad)-CantidadMerma=0 THEN 'FINALIZADA' ELSE 'PARCIAL' END,FechaFin=CASE WHEN CantidadRecibida-(CantidadEnviada+x.Cantidad)-CantidadMerma=0 THEN SYSDATETIME() ELSE NULL END FROM dbo.OrdenTrabajoDetalleArea a JOIN @Detalles x ON x.IdDetalleOT=a.IdDetalleOT WHERE a.IdAreaProduccion=@IdAreaOrigen;
        UPDATE a SET CantidadRecibida=CantidadRecibida+x.Cantidad,Estado='EN_PROCESO',FechaInicio=COALESCE(FechaInicio,SYSDATETIME()) FROM dbo.OrdenTrabajoDetalleArea a JOIN @Detalles x ON x.IdDetalleOT=a.IdDetalleOT WHERE a.IdAreaProduccion=@IdAreaDestino;
        IF @DestinoEsTermino=1
        BEGIN
            DECLARE @IngresosTermino TABLE(IdProducto INT PRIMARY KEY,Cantidad DECIMAL(18,2));
            INSERT @IngresosTermino(IdProducto,Cantidad)
            SELECT d.IdProducto,SUM(x.Cantidad) FROM @Detalles x JOIN dbo.OrdenTrabajoDetalle d ON d.IdDetalleOT=x.IdDetalleOT GROUP BY d.IdProducto;
            INSERT dbo.StockProductosAlmacen(IdProducto,IdAlmacen,StockActual)
            SELECT i.IdProducto,@IdAlmacenTermino,0 FROM @IngresosTermino i WHERE NOT EXISTS(SELECT 1 FROM dbo.StockProductosAlmacen WITH(UPDLOCK,HOLDLOCK) WHERE IdProducto=i.IdProducto AND IdAlmacen=@IdAlmacenTermino);
            INSERT dbo.StockProductos(IdProducto,StockActual)
            SELECT i.IdProducto,0 FROM @IngresosTermino i WHERE NOT EXISTS(SELECT 1 FROM dbo.StockProductos WITH(UPDLOCK,HOLDLOCK) WHERE IdProducto=i.IdProducto);
            INSERT dbo.KardexProductos(TipoMovimiento,IdIngresoManualStock,IdProducto,IdAlmacen,StockAnterior,Cantidad,StockResultante,UsuarioResponsable,FechaMovimiento,Observacion,IdOperacionTransferencia)
            SELECT 'INGRESO PRODUCCION',NULL,i.IdProducto,@IdAlmacenTermino,s.StockActual,i.Cantidad,s.StockActual+i.Cantidad,ISNULL(u.NombreUsuario,CONVERT(VARCHAR(20),@IdUsuarioSesion)),SYSDATETIME(),CONCAT('Ingreso de producto terminado - OT ',ot.NumeroOT),@IdOperacion
            FROM @IngresosTermino i JOIN dbo.StockProductosAlmacen s ON s.IdProducto=i.IdProducto AND s.IdAlmacen=@IdAlmacenTermino LEFT JOIN dbo.Usuarios u ON u.IdUsuario=@IdUsuarioSesion CROSS JOIN dbo.OrdenTrabajo ot WHERE ot.IdOrdenTrabajo=@IdOrdenTrabajo;
            UPDATE s SET StockActual=s.StockActual+i.Cantidad,FechaActualizacion=GETDATE() FROM dbo.StockProductosAlmacen s JOIN @IngresosTermino i ON i.IdProducto=s.IdProducto WHERE s.IdAlmacen=@IdAlmacenTermino;
            UPDATE s SET StockActual=s.StockActual+i.Cantidad,FechaActualizacion=GETDATE() FROM dbo.StockProductos s JOIN @IngresosTermino i ON i.IdProducto=s.IdProducto;
            UPDATE d SET CantidadProducida=d.CantidadProducida+x.Cantidad,
                CantidadAplicada=CASE WHEN d.CantidadProducida+x.Cantidad>d.CantidadRequerida THEN d.CantidadRequerida ELSE d.CantidadProducida+x.Cantidad END,
                CantidadExcedente=CASE WHEN d.CantidadProducida+x.Cantidad>d.CantidadRequerida THEN d.CantidadProducida+x.Cantidad-d.CantidadRequerida ELSE 0 END,
                CantidadPendiente=CASE WHEN d.CantidadRequerida-d.CantidadProducida-x.Cantidad>0 THEN d.CantidadRequerida-d.CantidadProducida-x.Cantidad ELSE 0 END,
                Estado=CASE WHEN d.CantidadProducida+x.Cantidad+ISNULL(m.TotalMerma,0)>=d.CantidadLanzada THEN 'TERMINADO' ELSE 'PARCIAL' END,
                FechaFin=CASE WHEN d.CantidadProducida+x.Cantidad+ISNULL(m.TotalMerma,0)>=d.CantidadLanzada THEN SYSDATETIME() ELSE NULL END
            FROM dbo.OrdenTrabajoDetalle d JOIN @Detalles x ON x.IdDetalleOT=d.IdDetalleOT OUTER APPLY(SELECT SUM(CantidadMerma) TotalMerma FROM dbo.OrdenTrabajoDetalleArea WHERE IdDetalleOT=d.IdDetalleOT)m;
        END;
        UPDATE o SET Estado=CASE WHEN NOT EXISTS(SELECT 1 FROM dbo.OrdenTrabajoDetalle WHERE IdOrdenTrabajo=o.IdOrdenTrabajo AND Estado<>'TERMINADO') THEN 'TERMINADA' WHEN EXISTS(SELECT 1 FROM dbo.OrdenTrabajoDetalle WHERE IdOrdenTrabajo=o.IdOrdenTrabajo AND Estado='TERMINADO') THEN 'PARCIAL' ELSE 'EN_PROCESO' END FROM dbo.OrdenTrabajo o WHERE o.IdOrdenTrabajo=@IdOrdenTrabajo;
        COMMIT; END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH
END;
GO
