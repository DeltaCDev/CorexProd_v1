SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.OrdenTrabajoTerminacion','U') IS NULL
BEGIN
    CREATE TABLE dbo.OrdenTrabajoTerminacion
    (
        IdOperacionTerminacion BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrdenTrabajoTerminacion PRIMARY KEY,
        IdOrdenTrabajo INT NOT NULL,
        IdAreaTermino INT NOT NULL,
        IdUsuarioSesion INT NOT NULL,
        IdUsuarioAutoriza INT NOT NULL,
        FechaRegistro DATETIME2(0) NOT NULL CONSTRAINT DF_OrdenTrabajoTerminacion_Fecha DEFAULT(SYSDATETIME()),
        Observacion NVARCHAR(500) NOT NULL CONSTRAINT DF_OrdenTrabajoTerminacion_Observacion DEFAULT(N''),
        CONSTRAINT FK_OrdenTrabajoTerminacion_OT FOREIGN KEY(IdOrdenTrabajo) REFERENCES dbo.OrdenTrabajo(IdOrdenTrabajo),
        CONSTRAINT FK_OrdenTrabajoTerminacion_Area FOREIGN KEY(IdAreaTermino) REFERENCES dbo.AreaProduccion(IdAreaProduccion),
        CONSTRAINT FK_OrdenTrabajoTerminacion_UsuarioSesion FOREIGN KEY(IdUsuarioSesion) REFERENCES dbo.Usuarios(IdUsuario),
        CONSTRAINT FK_OrdenTrabajoTerminacion_UsuarioAutoriza FOREIGN KEY(IdUsuarioAutoriza) REFERENCES dbo.Usuarios(IdUsuario)
    );
END;
GO

IF OBJECT_ID('dbo.OrdenTrabajoTerminacionDetalle','U') IS NULL
BEGIN
    CREATE TABLE dbo.OrdenTrabajoTerminacionDetalle
    (
        IdTerminacionDetalle BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrdenTrabajoTerminacionDetalle PRIMARY KEY,
        IdOperacionTerminacion BIGINT NOT NULL,
        IdDetalleOT INT NOT NULL,
        IdDetalleArea BIGINT NOT NULL,
        Cantidad DECIMAL(18,2) NOT NULL,
        CONSTRAINT FK_OrdenTrabajoTerminacionDetalle_Operacion FOREIGN KEY(IdOperacionTerminacion) REFERENCES dbo.OrdenTrabajoTerminacion(IdOperacionTerminacion),
        CONSTRAINT FK_OrdenTrabajoTerminacionDetalle_DetalleOT FOREIGN KEY(IdDetalleOT) REFERENCES dbo.OrdenTrabajoDetalle(IdDetalleOT),
        CONSTRAINT FK_OrdenTrabajoTerminacionDetalle_Area FOREIGN KEY(IdDetalleArea) REFERENCES dbo.OrdenTrabajoDetalleArea(IdDetalleArea),
        CONSTRAINT CK_OrdenTrabajoTerminacionDetalle_Cantidad CHECK(Cantidad>0)
    );
    CREATE INDEX IX_OrdenTrabajoTerminacionDetalle_Operacion ON dbo.OrdenTrabajoTerminacionDetalle(IdOperacionTerminacion);
END;
GO

IF COL_LENGTH('dbo.KardexProductos','IdOperacionTerminacion') IS NULL
    ALTER TABLE dbo.KardexProductos ADD IdOperacionTerminacion BIGINT NULL;
GO

IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name='FK_KardexProductos_OperacionTerminacion')
    ALTER TABLE dbo.KardexProductos ADD CONSTRAINT FK_KardexProductos_OperacionTerminacion
        FOREIGN KEY(IdOperacionTerminacion) REFERENCES dbo.OrdenTrabajoTerminacion(IdOperacionTerminacion);
GO

IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.KardexProductos') AND name='IX_KardexProductos_OperacionTerminacion')
    CREATE INDEX IX_KardexProductos_OperacionTerminacion ON dbo.KardexProductos(IdOperacionTerminacion,IdProducto);
GO

DECLARE @AreaTerminada INT=(SELECT TOP(1) IdAreaProduccion FROM dbo.AreaProduccion WHERE Activo=1 AND EsTermino=1 AND UPPER(LTRIM(RTRIM(NombreArea)))='TERMINADO');
DECLARE @UltimaAreaReal INT;
IF @AreaTerminada IS NOT NULL
BEGIN
    SELECT TOP(1) @UltimaAreaReal=IdAreaProduccion FROM dbo.AreaProduccion WHERE Activo=1 AND IdAreaProduccion<>@AreaTerminada ORDER BY OrdenSecuencia DESC;
    IF EXISTS(SELECT 1 FROM dbo.OrdenTrabajoDetalleArea WHERE IdAreaProduccion=@AreaTerminada AND (CantidadRecibida<>0 OR CantidadEnviada<>0 OR CantidadMerma<>0))
        THROW 51000,'El area TERMINADO tiene movimientos y requiere migracion manual.',1;
    DELETE FROM dbo.OrdenTrabajoDetalleArea WHERE IdAreaProduccion=@AreaTerminada;
    UPDATE dbo.AreaProduccion SET EsTermino=0,Activo=0,FechaModificacion=SYSDATETIME() WHERE IdAreaProduccion=@AreaTerminada;
    UPDATE dbo.AreaProduccion SET EsTermino=1,FechaModificacion=SYSDATETIME() WHERE IdAreaProduccion=@UltimaAreaReal;
    UPDATE dbo.OrdenTrabajoDetalleArea SET EsTermino=1 WHERE IdAreaProduccion=@UltimaAreaReal;
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
            Estado=CASE WHEN CantidadRecibida-(CantidadEnviada+x.Cantidad)-CantidadMerma=0 THEN 'FINALIZADA' ELSE 'PARCIAL' END,
            FechaFin=CASE WHEN CantidadRecibida-(CantidadEnviada+x.Cantidad)-CantidadMerma=0 THEN SYSDATETIME() ELSE NULL END
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
        UPDATE o SET Estado=CASE WHEN NOT EXISTS(SELECT 1 FROM dbo.OrdenTrabajoDetalle WHERE IdOrdenTrabajo=o.IdOrdenTrabajo AND Estado<>'TERMINADO') THEN 'TERMINADA'
            WHEN EXISTS(SELECT 1 FROM dbo.OrdenTrabajoDetalle WHERE IdOrdenTrabajo=o.IdOrdenTrabajo AND Estado='TERMINADO') THEN 'PARCIAL' ELSE 'EN_PROCESO' END
        FROM dbo.OrdenTrabajo o WHERE o.IdOrdenTrabajo=@IdOrdenTrabajo;
        COMMIT;
    END TRY
    BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH
END;
GO
