/*
    Limpieza operativa CorexProd
    ---------------------------------------
    Objetivo:
    - Dejar el sistema sin operaciones ni catálogos productivos.
    - Mantener: usuarios, roles, empleados, clientes, proveedores, empresa, áreas,
      almacenes, unidades de medida, categorías y configuración base.
    - Ocultar el módulo Destajo y Pagos hasta que esté afinado.

    Borra:
    - Movimientos/Kardex de productos e insumos.
    - Proformas.
    - OCI.
    - OT y movimientos de producción.
    - Guías internas.
    - Ingresos manuales.
    - Stock.
    - Insumos.
    - Productos.
    - Fichas técnicas.

    Ejecutar en la base CorexProdDB.
*/

USE CorexProdDB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;

BEGIN TRY
    BEGIN TRAN;

    /* 1. Movimientos y Kardex */
    IF OBJECT_ID('dbo.KardexProductos', 'U') IS NOT NULL
        DELETE FROM dbo.KardexProductos;

    IF OBJECT_ID('dbo.KardexInsumos', 'U') IS NOT NULL
        DELETE FROM dbo.KardexInsumos;

    /* 2. Guías internas */
    IF OBJECT_ID('dbo.GuiaInternaImpresiones', 'U') IS NOT NULL
        DELETE FROM dbo.GuiaInternaImpresiones;

    IF OBJECT_ID('dbo.GuiaInternaDetalle', 'U') IS NOT NULL
        DELETE FROM dbo.GuiaInternaDetalle;

    IF OBJECT_ID('dbo.GuiasInternas', 'U') IS NOT NULL
        DELETE FROM dbo.GuiasInternas;

    /* 3. Ingresos manuales de productos */
    IF OBJECT_ID('dbo.IngresosManualesStockAnulaciones', 'U') IS NOT NULL
        DELETE FROM dbo.IngresosManualesStockAnulaciones;

    IF OBJECT_ID('dbo.IngresosManualesStockDetalle', 'U') IS NOT NULL
        DELETE FROM dbo.IngresosManualesStockDetalle;

    IF OBJECT_ID('dbo.IngresosManualesStock', 'U') IS NOT NULL
        DELETE FROM dbo.IngresosManualesStock;

    /* 4. Ingresos manuales de insumos */
    IF OBJECT_ID('dbo.IngresosManualesStockInsumosAnulaciones', 'U') IS NOT NULL
        DELETE FROM dbo.IngresosManualesStockInsumosAnulaciones;

    IF OBJECT_ID('dbo.IngresosManualesStockInsumosDetalle', 'U') IS NOT NULL
        DELETE FROM dbo.IngresosManualesStockInsumosDetalle;

    IF OBJECT_ID('dbo.IngresosManualesStockInsumos', 'U') IS NOT NULL
        DELETE FROM dbo.IngresosManualesStockInsumos;

    /* 5. Ordenes de trabajo y movimientos de producción */
    IF OBJECT_ID('dbo.OrdenTrabajoConsumoInsumo', 'U') IS NOT NULL
        DELETE FROM dbo.OrdenTrabajoConsumoInsumo;

    IF OBJECT_ID('dbo.OrdenTrabajoMerma', 'U') IS NOT NULL
        DELETE FROM dbo.OrdenTrabajoMerma;

    IF OBJECT_ID('dbo.OrdenTrabajoTerminacionDetalle', 'U') IS NOT NULL
        DELETE FROM dbo.OrdenTrabajoTerminacionDetalle;

    IF OBJECT_ID('dbo.OrdenTrabajoTerminacion', 'U') IS NOT NULL
        DELETE FROM dbo.OrdenTrabajoTerminacion;

    IF OBJECT_ID('dbo.OrdenTrabajoTransferenciaDetalle', 'U') IS NOT NULL
        DELETE FROM dbo.OrdenTrabajoTransferenciaDetalle;

    IF OBJECT_ID('dbo.OrdenTrabajoTransferencia', 'U') IS NOT NULL
        DELETE FROM dbo.OrdenTrabajoTransferencia;

    IF OBJECT_ID('dbo.OrdenTrabajoDetalleArea', 'U') IS NOT NULL
        DELETE FROM dbo.OrdenTrabajoDetalleArea;

    IF OBJECT_ID('dbo.OrdenTrabajoDetalle', 'U') IS NOT NULL
        DELETE FROM dbo.OrdenTrabajoDetalle;

    IF OBJECT_ID('dbo.OrdenTrabajo', 'U') IS NOT NULL
        DELETE FROM dbo.OrdenTrabajo;

    /* 6. OCI y Proformas */
    IF OBJECT_ID('dbo.OrdenCompraInternaDetalle', 'U') IS NOT NULL
        DELETE FROM dbo.OrdenCompraInternaDetalle;

    IF OBJECT_ID('dbo.OrdenesCompraInterna', 'U') IS NOT NULL
        DELETE FROM dbo.OrdenesCompraInterna;

    IF OBJECT_ID('dbo.ProformaDetalle', 'U') IS NOT NULL
        DELETE FROM dbo.ProformaDetalle;

    IF OBJECT_ID('dbo.Proformas', 'U') IS NOT NULL
        DELETE FROM dbo.Proformas;

    /* 7. Fichas técnicas */
    IF OBJECT_ID('dbo.FichaTecnicaDetalle', 'U') IS NOT NULL
        DELETE FROM dbo.FichaTecnicaDetalle;

    IF OBJECT_ID('dbo.FichaTecnica', 'U') IS NOT NULL
        DELETE FROM dbo.FichaTecnica;

    /* 8. Stock */
    IF OBJECT_ID('dbo.StockProductosAlmacen', 'U') IS NOT NULL
        DELETE FROM dbo.StockProductosAlmacen;

    IF OBJECT_ID('dbo.StockProductos', 'U') IS NOT NULL
        DELETE FROM dbo.StockProductos;

    IF OBJECT_ID('dbo.StockInsumosAlmacen', 'U') IS NOT NULL
        DELETE FROM dbo.StockInsumosAlmacen;

    IF OBJECT_ID('dbo.StockInsumos', 'U') IS NOT NULL
        DELETE FROM dbo.StockInsumos;

    /* 9. Catálogos productivos */
    IF OBJECT_ID('dbo.Productos', 'U') IS NOT NULL
        DELETE FROM dbo.Productos;

    IF OBJECT_ID('dbo.Insumos', 'U') IS NOT NULL
        DELETE FROM dbo.Insumos;

    /* 10. Ocultar Destajo y Pagos */
    DECLARE @IdDestajoPagos INT;

    SELECT @IdDestajoPagos = IdMenu
    FROM dbo.Menu
    WHERE NombreMenu = 'Destajo y Pagos'
      AND IdMenuPadre IS NULL;

    IF @IdDestajoPagos IS NOT NULL
    BEGIN
        UPDATE dbo.Menu
        SET Estado = 0
        WHERE IdMenu = @IdDestajoPagos
           OR IdMenuPadre = @IdDestajoPagos;

        UPDATE PM
        SET PuedeVer = 0
        FROM dbo.PermisosMenu PM
        INNER JOIN dbo.Menu M ON M.IdMenu = PM.IdMenu
        WHERE M.IdMenu = @IdDestajoPagos
           OR M.IdMenuPadre = @IdDestajoPagos;
    END;

    /* 11. Reiniciar correlativos de todas las series configuradas */
    IF OBJECT_ID('dbo.SeriesCorrelativosHistorial', 'U') IS NOT NULL
        DELETE FROM dbo.SeriesCorrelativosHistorial;

    IF OBJECT_ID('dbo.SeriesCorrelativos', 'U') IS NOT NULL
        UPDATE dbo.SeriesCorrelativos
        SET UltimoCorrelativo = 0,
            FechaUltimoUso = NULL;

    IF OBJECT_ID('dbo.SerieGuiaInterna', 'U') IS NOT NULL
        UPDATE dbo.SerieGuiaInterna
        SET UltimoNumero = 0;

    IF OBJECT_ID('dbo.SerieIngresoManualStock', 'U') IS NOT NULL
        UPDATE dbo.SerieIngresoManualStock
        SET UltimoNumero = 0;

    IF OBJECT_ID('dbo.SerieIngresoManualStockInsumo', 'U') IS NOT NULL
        UPDATE dbo.SerieIngresoManualStockInsumo
        SET UltimoNumero = 0;

    /* 12. Reiniciar identities de tablas limpiadas */
    DECLARE @TablasIdentity TABLE (NombreTabla SYSNAME);
    INSERT INTO @TablasIdentity (NombreTabla)
    VALUES
        ('GuiasInternas'),
        ('GuiaInternaDetalle'),
        ('GuiaInternaImpresiones'),
        ('IngresosManualesStock'),
        ('IngresosManualesStockDetalle'),
        ('IngresosManualesStockInsumos'),
        ('IngresosManualesStockInsumosDetalle'),
        ('OrdenTrabajo'),
        ('OrdenTrabajoDetalle'),
        ('OrdenTrabajoDetalleArea'),
        ('OrdenTrabajoTransferencia'),
        ('OrdenTrabajoTransferenciaDetalle'),
        ('OrdenTrabajoTerminacion'),
        ('OrdenTrabajoTerminacionDetalle'),
        ('OrdenTrabajoMerma'),
        ('OrdenCompraInternaDetalle'),
        ('OrdenesCompraInterna'),
        ('ProformaDetalle'),
        ('Proformas'),
        ('FichaTecnica'),
        ('FichaTecnicaDetalle'),
        ('Productos'),
        ('Insumos'),
        ('StockProductos'),
        ('StockProductosAlmacen'),
        ('StockInsumos'),
        ('StockInsumosAlmacen'),
        ('KardexProductos'),
        ('KardexInsumos'),
        ('SeriesCorrelativosHistorial');

    DECLARE @TablaIdentity SYSNAME;
    DECLARE @SqlReset NVARCHAR(MAX);

    DECLARE CursorIdentity CURSOR LOCAL FAST_FORWARD FOR
        SELECT T.NombreTabla
        FROM @TablasIdentity T
        WHERE OBJECT_ID('dbo.' + T.NombreTabla, 'U') IS NOT NULL
          AND EXISTS
          (
              SELECT 1
              FROM sys.identity_columns IC
              WHERE IC.object_id = OBJECT_ID('dbo.' + T.NombreTabla)
          );

    OPEN CursorIdentity;
    FETCH NEXT FROM CursorIdentity INTO @TablaIdentity;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @SqlReset = N'DBCC CHECKIDENT (''dbo.' + REPLACE(@TablaIdentity, '''', '''''') + N''', RESEED, 0) WITH NO_INFOMSGS;';
        EXEC sp_executesql @SqlReset;
        FETCH NEXT FROM CursorIdentity INTO @TablaIdentity;
    END;

    CLOSE CursorIdentity;
    DEALLOCATE CursorIdentity;

    COMMIT TRAN;

    PRINT 'OK: CorexProd quedo limpio. Se conservaron usuarios, empleados, clientes, proveedores, empresa y areas.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRAN;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
GO

/* Verificacion resumida */
SELECT 'Proformas' AS Tabla, COUNT(*) AS Registros FROM dbo.Proformas
UNION ALL SELECT 'OCI', COUNT(*) FROM dbo.OrdenesCompraInterna
UNION ALL SELECT 'OT', COUNT(*) FROM dbo.OrdenTrabajo
UNION ALL SELECT 'GuiasInternas', COUNT(*) FROM dbo.GuiasInternas
UNION ALL SELECT 'Productos', COUNT(*) FROM dbo.Productos
UNION ALL SELECT 'Insumos', COUNT(*) FROM dbo.Insumos
UNION ALL SELECT 'FichaTecnica', COUNT(*) FROM dbo.FichaTecnica
UNION ALL SELECT 'KardexProductos', COUNT(*) FROM dbo.KardexProductos
UNION ALL SELECT 'KardexInsumos', COUNT(*) FROM dbo.KardexInsumos;

SELECT IdMenu, NombreMenu, IdMenuPadre, Orden, Estado
FROM dbo.Menu
WHERE NombreMenu = 'Destajo y Pagos'
   OR IdMenuPadre = (SELECT TOP 1 IdMenu FROM dbo.Menu WHERE NombreMenu = 'Destajo y Pagos' AND IdMenuPadre IS NULL)
ORDER BY IdMenuPadre, Orden;
