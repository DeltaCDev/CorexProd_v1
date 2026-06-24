SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

IF COL_LENGTH('dbo.OrdenTrabajo','TipoOT') IS NULL
    ALTER TABLE dbo.OrdenTrabajo ADD TipoOT VARCHAR(20) NOT NULL CONSTRAINT DF_OrdenTrabajo_Tipo DEFAULT('OCI') WITH VALUES;
IF COL_LENGTH('dbo.OrdenTrabajo','IdOrdenTrabajoRelacionada') IS NULL
    ALTER TABLE dbo.OrdenTrabajo ADD IdOrdenTrabajoRelacionada INT NULL;
IF COL_LENGTH('dbo.OrdenTrabajo','IdUsuarioAutorizaCreacion') IS NULL
    ALTER TABLE dbo.OrdenTrabajo ADD IdUsuarioAutorizaCreacion INT NULL;
GO

IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name='FK_OrdenTrabajo_Relacionada')
    ALTER TABLE dbo.OrdenTrabajo ADD CONSTRAINT FK_OrdenTrabajo_Relacionada FOREIGN KEY(IdOrdenTrabajoRelacionada) REFERENCES dbo.OrdenTrabajo(IdOrdenTrabajo);
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name='FK_OrdenTrabajo_AutorizaCreacion')
    ALTER TABLE dbo.OrdenTrabajo ADD CONSTRAINT FK_OrdenTrabajo_AutorizaCreacion FOREIGN KEY(IdUsuarioAutorizaCreacion) REFERENCES dbo.Usuarios(IdUsuario);
GO

DECLARE @DefaultEstado SYSNAME=(SELECT dc.name FROM sys.default_constraints dc JOIN sys.columns c ON c.default_object_id=dc.object_id WHERE dc.parent_object_id=OBJECT_ID('dbo.OrdenTrabajo') AND c.name='Estado');
IF @DefaultEstado IS NOT NULL
BEGIN
    DECLARE @SqlDefault NVARCHAR(500)=N'ALTER TABLE dbo.OrdenTrabajo DROP CONSTRAINT '+QUOTENAME(@DefaultEstado);
    EXEC sp_executesql @SqlDefault;
END;
ALTER TABLE dbo.OrdenTrabajo ADD CONSTRAINT DF_OrdenTrabajo_Estado DEFAULT('PENDIENTE') FOR Estado;
IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE name='CK_OrdenTrabajo_Estado') ALTER TABLE dbo.OrdenTrabajo DROP CONSTRAINT CK_OrdenTrabajo_Estado;
ALTER TABLE dbo.OrdenTrabajo ADD CONSTRAINT CK_OrdenTrabajo_Estado CHECK(Estado IN('PENDIENTE','EMITIDA','EN_PROCESO','PARCIAL','TERMINADA','ANULADA'));
GO

UPDATE dbo.OrdenTrabajo SET IdUsuarioAutorizaCreacion=IdUsuarioCreacion WHERE IdUsuarioAutorizaCreacion IS NULL;
UPDATE dbo.OrdenTrabajo SET Estado='PENDIENTE' WHERE Estado='EMITIDA';
UPDATE dbo.OrdenesCompraInterna SET Estado='PROCESO' WHERE TieneOrdenTrabajo=1 AND Estado IN('Emitida','En proceso','PENDIENTE');
UPDATE a SET CantidadRecibida=d.CantidadPlanificada,Estado='PENDIENTE'
FROM dbo.OrdenTrabajoDetalleArea a JOIN dbo.OrdenTrabajoDetalle d ON d.IdDetalleOT=a.IdDetalleOT
WHERE a.EsInicio=1 AND a.CantidadRecibida=0 AND a.CantidadEnviada=0 AND a.CantidadMerma=0 AND d.Estado='PENDIENTE';
GO

CREATE OR ALTER TRIGGER dbo.TRG_OCI_ESTADO_ORDEN_TRABAJO
ON dbo.OrdenesCompraInterna AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(TieneOrdenTrabajo) RETURN;
    UPDATE o SET Estado='PROCESO'
    FROM dbo.OrdenesCompraInterna o JOIN inserted i ON i.IdOrdenCompraInterna=o.IdOrdenCompraInterna
    JOIN deleted d ON d.IdOrdenCompraInterna=i.IdOrdenCompraInterna
    WHERE d.TieneOrdenTrabajo=0 AND i.TieneOrdenTrabajo=1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_VALIDAR_INSUMOS @IdOrdenCompraInterna INT
AS
BEGIN
    SET NOCOUNT ON;
    ;WITH ProductosOCI AS
    (
        SELECT d.IdOrdenCompraInternaDetalle,d.IdProducto,d.CodigoProducto,d.NombreProducto,d.Observacion,
               CONVERT(DECIMAL(18,3),d.Cantidad-d.CantidadDespachada) CantidadRequerida
        FROM dbo.OrdenCompraInternaDetalle d WHERE d.IdOrdenCompraInterna=@IdOrdenCompraInterna AND d.Cantidad>d.CantidadDespachada
    ), Ficha AS
    (
        SELECT p.*,f.IdFichaTecnica,ROW_NUMBER()OVER(PARTITION BY p.IdProducto ORDER BY f.Version DESC,f.IdFichaTecnica DESC) rn
        FROM ProductosOCI p LEFT JOIN dbo.FichaTecnica f ON f.IdProducto=p.IdProducto AND f.Estado=1
    )
    SELECT f.IdOrdenCompraInternaDetalle,f.IdProducto,f.CodigoProducto,f.NombreProducto,f.Observacion,f.CantidadRequerida,f.IdFichaTecnica,
           CONVERT(DECIMAL(18,3),ISNULL(sp.StockActual,0)) StockAlmacen,
           CONVERT(DECIMAL(18,3),ISNULL(ap.StockCorte,0)) StockCorte,
           CONVERT(DECIMAL(18,3),ISNULL(ap.StockConfeccion,0)) StockConfeccion,
           CONVERT(DECIMAL(18,3),ISNULL(ap.StockAcabado,0)) StockAcabado,
           CONVERT(DECIMAL(18,3),ISNULL(sp.StockActual,0)+ISNULL(ap.StockCorte,0)+ISNULL(ap.StockConfeccion,0)+ISNULL(ap.StockAcabado,0)) StockTotal,
           CONVERT(DECIMAL(18,3),CASE WHEN f.CantidadRequerida-(ISNULL(sp.StockActual,0)+ISNULL(ap.StockCorte,0)+ISNULL(ap.StockConfeccion,0)+ISNULL(ap.StockAcabado,0))>0 THEN f.CantidadRequerida-(ISNULL(sp.StockActual,0)+ISNULL(ap.StockCorte,0)+ISNULL(ap.StockConfeccion,0)+ISNULL(ap.StockAcabado,0)) ELSE 0 END) Deficit,
           CASE
                WHEN f.IdFichaTecnica IS NULL
                     OR NOT EXISTS(SELECT 1 FROM dbo.FichaTecnicaDetalle fd WHERE fd.IdFichaTecnica=f.IdFichaTecnica AND fd.Estado=1)
                    THEN 'Sin ficha tecnica'
                WHEN EXISTS
                (
                    SELECT 1
                    FROM dbo.FichaTecnicaDetalle fd
                    LEFT JOIN dbo.StockInsumos si ON si.IdInsumo=fd.IdInsumo
                    WHERE fd.IdFichaTecnica=f.IdFichaTecnica
                      AND fd.Estado=1
                      AND ISNULL(si.StockActual,0)<fd.Cantidad*f.CantidadRequerida
                ) THEN 'Faltantes'
                ELSE 'Completo para producir'
           END EstadoInsumos
    FROM Ficha f
    OUTER APPLY(SELECT SUM(s.StockActual) StockActual FROM dbo.StockProductosAlmacen s WHERE s.IdProducto=f.IdProducto)sp
    OUTER APPLY(SELECT SUM(CASE WHEN a.NombreArea LIKE '%CORTE%' THEN da.CantidadPendiente ELSE 0 END) StockCorte,SUM(CASE WHEN a.NombreArea LIKE '%CONFECCI%' THEN da.CantidadPendiente ELSE 0 END) StockConfeccion,SUM(CASE WHEN a.NombreArea LIKE '%ACABADO%' THEN da.CantidadPendiente ELSE 0 END) StockAcabado FROM dbo.OrdenTrabajoDetalle od JOIN dbo.OrdenTrabajoDetalleArea da ON da.IdDetalleOT=od.IdDetalleOT JOIN dbo.AreaProduccion a ON a.IdAreaProduccion=da.IdAreaProduccion WHERE od.IdProducto=f.IdProducto AND od.Estado NOT IN('TERMINADO','ANULADO'))ap
    WHERE f.rn=1 ORDER BY f.IdOrdenCompraInternaDetalle;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_DETALLE_INSUMOS @IdOrdenCompraInternaDetalle INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @IdProducto INT,@Cantidad DECIMAL(18,3),@IdFicha INT;
    SELECT @IdProducto=IdProducto,@Cantidad=Cantidad-CantidadDespachada FROM dbo.OrdenCompraInternaDetalle WHERE IdOrdenCompraInternaDetalle=@IdOrdenCompraInternaDetalle;
    SELECT TOP(1)@IdFicha=IdFichaTecnica FROM dbo.FichaTecnica WHERE IdProducto=@IdProducto AND Estado=1 ORDER BY Version DESC,IdFichaTecnica DESC;
    SELECT fd.IdInsumo,i.Codigo CodigoInsumo,i.NombreInsumo,um.Abreviatura UnidadMedida,CONVERT(DECIMAL(18,3),fd.Cantidad) ConsumoUnitario,@Cantidad CantidadProduccion,
           CONVERT(DECIMAL(18,3),fd.Cantidad*@Cantidad) CantidadNecesaria,CONVERT(DECIMAL(18,3),ISNULL(si.StockActual,0)) StockActual,
           CONVERT(DECIMAL(18,3),ISNULL(si.StockActual,0)-fd.Cantidad*@Cantidad) StockProyectado,
           CONVERT(DECIMAL(18,3),CASE WHEN fd.Cantidad*@Cantidad-ISNULL(si.StockActual,0)>0 THEN fd.Cantidad*@Cantidad-ISNULL(si.StockActual,0) ELSE 0 END) CantidadFaltante,
           CASE
                WHEN ISNULL(si.StockActual,0)>=fd.Cantidad*@Cantidad THEN 'Completo'
                ELSE 'Faltante'
           END Estado
    FROM dbo.FichaTecnicaDetalle fd JOIN dbo.Insumos i ON i.IdInsumo=fd.IdInsumo JOIN dbo.UnidadesMedida um ON um.IdUnidadMedida=fd.IdUnidadMedida LEFT JOIN dbo.StockInsumos si ON si.IdInsumo=fd.IdInsumo
    WHERE fd.IdFichaTecnica=@IdFicha AND fd.Estado=1 ORDER BY i.NombreInsumo;
END;
GO

IF OBJECT_ID('dbo.OrdenTrabajoConsumoInsumo','U') IS NULL
BEGIN
    CREATE TABLE dbo.OrdenTrabajoConsumoInsumo
    (
        IdConsumo BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OTConsumo PRIMARY KEY,
        IdOrdenTrabajo INT NOT NULL,IdDetalleOT INT NOT NULL,IdInsumo INT NOT NULL,IdAlmacen INT NOT NULL,
        CantidadProduccion DECIMAL(18,3) NOT NULL,ConsumoUnitario DECIMAL(18,3) NOT NULL,CantidadConsumida DECIMAL(18,3) NOT NULL,
        StockAnterior DECIMAL(18,3) NOT NULL,StockResultante DECIMAL(18,3) NOT NULL,IdUsuario INT NOT NULL,FechaRegistro DATETIME2(0) NOT NULL CONSTRAINT DF_OTConsumo_Fecha DEFAULT(SYSDATETIME()),
        CONSTRAINT FK_OTConsumo_OT FOREIGN KEY(IdOrdenTrabajo) REFERENCES dbo.OrdenTrabajo(IdOrdenTrabajo),CONSTRAINT FK_OTConsumo_Detalle FOREIGN KEY(IdDetalleOT) REFERENCES dbo.OrdenTrabajoDetalle(IdDetalleOT),
        CONSTRAINT FK_OTConsumo_Insumo FOREIGN KEY(IdInsumo) REFERENCES dbo.Insumos(IdInsumo),CONSTRAINT FK_OTConsumo_Almacen FOREIGN KEY(IdAlmacen) REFERENCES dbo.Almacenes(IdAlmacen),CONSTRAINT FK_OTConsumo_Usuario FOREIGN KEY(IdUsuario) REFERENCES dbo.Usuarios(IdUsuario)
    );
    CREATE UNIQUE INDEX UX_OTConsumo_DetalleInsumo ON dbo.OrdenTrabajoConsumoInsumo(IdDetalleOT,IdInsumo);
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_CONSUMO_CONFIRMAR @IdDetalleOT INT,@IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;SET XACT_ABORT ON;
    BEGIN TRY BEGIN TRAN;
        DECLARE @IdOT INT,@IdProducto INT,@Cantidad DECIMAL(18,3),@IdFicha INT,@IdAlmacen INT=(SELECT TOP(1)IdAlmacen FROM dbo.Almacenes WHERE Estado=1 ORDER BY IdAlmacen);
        SELECT @IdOT=IdOrdenTrabajo,@IdProducto=IdProducto,@Cantidad=CASE WHEN CantidadLanzada>0 THEN CantidadLanzada ELSE CantidadPlanificada END FROM dbo.OrdenTrabajoDetalle WITH(UPDLOCK,HOLDLOCK) WHERE IdDetalleOT=@IdDetalleOT;
        IF @IdOT IS NULL THROW 51000,'El producto no pertenece a una OT válida.',1;
        IF EXISTS(SELECT 1 FROM dbo.OrdenTrabajoConsumoInsumo WHERE IdDetalleOT=@IdDetalleOT) THROW 51000,'El consumo de insumos de este producto ya fue confirmado.',1;
        SELECT TOP(1)@IdFicha=IdFichaTecnica FROM dbo.FichaTecnica WHERE IdProducto=@IdProducto AND Estado=1 ORDER BY Version DESC,IdFichaTecnica DESC;
        IF @IdFicha IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.FichaTecnicaDetalle WHERE IdFichaTecnica=@IdFicha AND Estado=1) THROW 51000,'El producto no tiene una ficha técnica válida para confirmar consumo.',1;
        IF @IdAlmacen IS NULL THROW 51000,'No existe un almacén activo.',1;

        DECLARE @IdInsumo INT,@Unitario DECIMAL(18,3),@Consumir DECIMAL(18,3),@Anterior DECIMAL(18,3);
        DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT IdInsumo,Cantidad,Cantidad*@Cantidad FROM dbo.FichaTecnicaDetalle WHERE IdFichaTecnica=@IdFicha AND Estado=1;
        OPEN c;FETCH NEXT FROM c INTO @IdInsumo,@Unitario,@Consumir;
        WHILE @@FETCH_STATUS=0
        BEGIN
            IF NOT EXISTS(SELECT 1 FROM dbo.StockInsumos WHERE IdInsumo=@IdInsumo) INSERT dbo.StockInsumos(IdInsumo,StockActual)VALUES(@IdInsumo,0);
            SELECT @Anterior=StockActual FROM dbo.StockInsumos WITH(UPDLOCK,HOLDLOCK) WHERE IdInsumo=@IdInsumo;
            UPDATE dbo.StockInsumos SET StockActual=StockActual-@Consumir,FechaActualizacion=GETDATE() WHERE IdInsumo=@IdInsumo;
            IF NOT EXISTS(SELECT 1 FROM dbo.StockInsumosAlmacen WHERE IdInsumo=@IdInsumo AND IdAlmacen=@IdAlmacen) INSERT dbo.StockInsumosAlmacen(IdInsumo,IdAlmacen,StockActual)VALUES(@IdInsumo,@IdAlmacen,0);
            UPDATE dbo.StockInsumosAlmacen SET StockActual=StockActual-@Consumir,FechaActualizacion=GETDATE() WHERE IdInsumo=@IdInsumo AND IdAlmacen=@IdAlmacen;
            INSERT dbo.OrdenTrabajoConsumoInsumo(IdOrdenTrabajo,IdDetalleOT,IdInsumo,IdAlmacen,CantidadProduccion,ConsumoUnitario,CantidadConsumida,StockAnterior,StockResultante,IdUsuario)VALUES(@IdOT,@IdDetalleOT,@IdInsumo,@IdAlmacen,@Cantidad,@Unitario,@Consumir,@Anterior,@Anterior-@Consumir,@IdUsuario);
            INSERT dbo.KardexInsumos(TipoMovimiento,IdIngresoManualStockInsumo,IdInsumo,IdAlmacen,StockAnterior,Cantidad,StockResultante,UsuarioResponsable,FechaMovimiento,Observacion) SELECT 'SALIDA PRODUCCION',NULL,@IdInsumo,@IdAlmacen,@Anterior,@Consumir,@Anterior-@Consumir,u.NombreUsuario,GETDATE(),CONCAT('Consumo confirmado OT ',o.NumeroOT,' / Detalle ',@IdDetalleOT) FROM dbo.Usuarios u CROSS JOIN dbo.OrdenTrabajo o WHERE u.IdUsuario=@IdUsuario AND o.IdOrdenTrabajo=@IdOT;
            FETCH NEXT FROM c INTO @IdInsumo,@Unitario,@Consumir;
        END;CLOSE c;DEALLOCATE c;
        COMMIT;
    END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK;THROW;END CATCH
END;
GO
