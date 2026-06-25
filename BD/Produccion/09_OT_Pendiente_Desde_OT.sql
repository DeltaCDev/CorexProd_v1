SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

IF EXISTS(SELECT 1 FROM sys.key_constraints WHERE name='UQ_OrdenTrabajo_OCI' AND parent_object_id=OBJECT_ID('dbo.OrdenTrabajo'))
    ALTER TABLE dbo.OrdenTrabajo DROP CONSTRAINT UQ_OrdenTrabajo_OCI;
GO

IF EXISTS(SELECT 1 FROM sys.key_constraints WHERE name='UQ_OTDetalle_OCI' AND parent_object_id=OBJECT_ID('dbo.OrdenTrabajoDetalle'))
    ALTER TABLE dbo.OrdenTrabajoDetalle DROP CONSTRAINT UQ_OTDetalle_OCI;
GO

CREATE OR ALTER PROCEDURE dbo.USP_VEN_OCI_LISTAR
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        O.IdOrdenCompraInterna,
        O.NumeroOci,
        O.IdProforma,
        P.SerieNumero AS NumeroProforma,
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
        O.TieneGuiaSalida,
        O.TieneOrdenTrabajo,
        CAST(CASE WHEN O.Estado <> 'Anulado'
             AND NOT EXISTS
             (
                 SELECT 1
                 FROM dbo.OrdenTrabajo OT
                 WHERE OT.IdOrdenCompraInterna = O.IdOrdenCompraInterna
                   AND OT.Estado IN ('PENDIENTE','EMITIDA','EN_PROCESO','PARCIAL')
             )
             AND EXISTS
             (
                 SELECT 1
                 FROM
                 (
                     SELECT
                         D.IdProducto,
                         SUM(CASE
                             WHEN D.Cantidad - CASE WHEN D.CantidadDespachada > ISNULL(PROD.CantidadAplicada, 0) THEN D.CantidadDespachada ELSE ISNULL(PROD.CantidadAplicada, 0) END > 0
                                 THEN D.Cantidad - CASE WHEN D.CantidadDespachada > ISNULL(PROD.CantidadAplicada, 0) THEN D.CantidadDespachada ELSE ISNULL(PROD.CantidadAplicada, 0) END
                             ELSE 0
                         END) AS CantidadPendiente
                     FROM dbo.OrdenCompraInternaDetalle D
                     OUTER APPLY
                     (
                         SELECT SUM(OD.CantidadAplicada) CantidadAplicada
                         FROM dbo.OrdenTrabajoDetalle OD
                         JOIN dbo.OrdenTrabajo OT ON OT.IdOrdenTrabajo = OD.IdOrdenTrabajo
                         WHERE OD.IdOrdenCompraInternaDetalle = D.IdOrdenCompraInternaDetalle
                           AND OT.Estado <> 'ANULADA'
                           AND OD.Estado <> 'ANULADO'
                     ) PROD
                     WHERE D.IdOrdenCompraInterna = O.IdOrdenCompraInterna
                     GROUP BY D.IdProducto
                 ) PEND
                 LEFT JOIN dbo.StockProductos S ON S.IdProducto = PEND.IdProducto
                 WHERE PEND.CantidadPendiente > ISNULL(S.StockActual, 0)
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
    INNER JOIN dbo.Proformas P ON P.IdProforma = O.IdProforma
    ORDER BY O.FechaEmision DESC, O.IdOrdenCompraInterna DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_VEN_OCI_OBTENER
    @IdOrdenCompraInterna INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        O.IdOrdenCompraInterna,
        O.NumeroOci,
        O.IdProforma,
        P.SerieNumero AS NumeroProforma,
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
        O.TieneGuiaSalida,
        O.TieneOrdenTrabajo,
        CAST(CASE WHEN O.Estado <> 'Anulado'
             AND NOT EXISTS
             (
                 SELECT 1
                 FROM dbo.OrdenTrabajo OT
                 WHERE OT.IdOrdenCompraInterna = O.IdOrdenCompraInterna
                   AND OT.Estado IN ('PENDIENTE','EMITIDA','EN_PROCESO','PARCIAL')
             )
             AND EXISTS
             (
                 SELECT 1
                 FROM
                 (
                     SELECT
                         D.IdProducto,
                         SUM(CASE
                             WHEN D.Cantidad - CASE WHEN D.CantidadDespachada > ISNULL(PROD.CantidadAplicada, 0) THEN D.CantidadDespachada ELSE ISNULL(PROD.CantidadAplicada, 0) END > 0
                                 THEN D.Cantidad - CASE WHEN D.CantidadDespachada > ISNULL(PROD.CantidadAplicada, 0) THEN D.CantidadDespachada ELSE ISNULL(PROD.CantidadAplicada, 0) END
                             ELSE 0
                         END) AS CantidadPendiente
                     FROM dbo.OrdenCompraInternaDetalle D
                     OUTER APPLY
                     (
                         SELECT SUM(OD.CantidadAplicada) CantidadAplicada
                         FROM dbo.OrdenTrabajoDetalle OD
                         JOIN dbo.OrdenTrabajo OT ON OT.IdOrdenTrabajo = OD.IdOrdenTrabajo
                         WHERE OD.IdOrdenCompraInternaDetalle = D.IdOrdenCompraInternaDetalle
                           AND OT.Estado <> 'ANULADA'
                           AND OD.Estado <> 'ANULADO'
                     ) PROD
                     WHERE D.IdOrdenCompraInterna = O.IdOrdenCompraInterna
                     GROUP BY D.IdProducto
                 ) PEND
                 LEFT JOIN dbo.StockProductos S ON S.IdProducto = PEND.IdProducto
                 WHERE PEND.CantidadPendiente > ISNULL(S.StockActual, 0)
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
    INNER JOIN dbo.Proformas P ON P.IdProforma = O.IdProforma
    WHERE O.IdOrdenCompraInterna = @IdOrdenCompraInterna;

    SELECT
        D.IdOrdenCompraInternaDetalle,
        D.IdOrdenCompraInterna,
        D.IdProducto,
        D.CodigoProducto,
        D.NombreProducto,
        D.Cantidad,
        CAST(ISNULL(S.StockActual, 0) AS DECIMAL(18,2)) AS StockActual,
        D.CantidadDespachada,
        D.PrecioUnitario,
        D.Descuento,
        D.Importe,
        D.Observacion
    FROM dbo.OrdenCompraInternaDetalle D
    LEFT JOIN dbo.StockProductos S ON S.IdProducto = D.IdProducto
    WHERE D.IdOrdenCompraInterna = @IdOrdenCompraInterna
    ORDER BY D.IdOrdenCompraInternaDetalle;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_VALIDAR_INSUMOS @IdOrdenCompraInterna INT
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH ProductosOCI AS
    (
        SELECT d.IdOrdenCompraInternaDetalle,d.IdProducto,d.CodigoProducto,d.NombreProducto,d.Observacion,
               CONVERT(DECIMAL(18,3),d.Cantidad-CASE WHEN d.CantidadDespachada>ISNULL(prod.CantidadAplicada,0) THEN d.CantidadDespachada ELSE ISNULL(prod.CantidadAplicada,0) END) CantidadRequerida
        FROM dbo.OrdenCompraInternaDetalle d
        OUTER APPLY
        (
            SELECT SUM(od.CantidadAplicada) CantidadAplicada
            FROM dbo.OrdenTrabajoDetalle od
            JOIN dbo.OrdenTrabajo ot ON ot.IdOrdenTrabajo=od.IdOrdenTrabajo
            WHERE od.IdOrdenCompraInternaDetalle=d.IdOrdenCompraInternaDetalle
              AND ot.Estado<>'ANULADA'
              AND od.Estado<>'ANULADO'
        )prod
        WHERE d.IdOrdenCompraInterna=@IdOrdenCompraInterna
          AND d.Cantidad-CASE WHEN d.CantidadDespachada>ISNULL(prod.CantidadAplicada,0) THEN d.CantidadDespachada ELSE ISNULL(prod.CantidadAplicada,0) END>0
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

    SELECT @IdProducto=d.IdProducto,@Cantidad=d.Cantidad-CASE WHEN d.CantidadDespachada>ISNULL(prod.CantidadAplicada,0) THEN d.CantidadDespachada ELSE ISNULL(prod.CantidadAplicada,0) END
    FROM dbo.OrdenCompraInternaDetalle d
    OUTER APPLY
    (
        SELECT SUM(od.CantidadAplicada) CantidadAplicada
        FROM dbo.OrdenTrabajoDetalle od
        JOIN dbo.OrdenTrabajo ot ON ot.IdOrdenTrabajo=od.IdOrdenTrabajo
        WHERE od.IdOrdenCompraInternaDetalle=d.IdOrdenCompraInternaDetalle
          AND ot.Estado<>'ANULADA'
          AND od.Estado<>'ANULADO'
    )prod
    WHERE d.IdOrdenCompraInternaDetalle=@IdOrdenCompraInternaDetalle;

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

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_CREAR
    @IdOrdenCompraInterna INT, @IdUsuario INT, @Observacion NVARCHAR(500),
    @Detalles dbo.TipoOTPlanificacion READONLY, @IdOrdenTrabajo INT OUTPUT, @NumeroOT VARCHAR(30) OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        IF NOT EXISTS(SELECT 1 FROM dbo.Usuarios WHERE IdUsuario=@IdUsuario AND Estado=1) THROW 51000, 'El usuario de sesion no es valido.', 1;
        IF EXISTS(SELECT 1 FROM dbo.OrdenTrabajo WHERE IdOrdenCompraInterna=@IdOrdenCompraInterna AND Estado IN('PENDIENTE','EMITIDA','EN_PROCESO','PARCIAL')) THROW 51000, 'La OCI tiene una Orden de Trabajo pendiente o en proceso.', 1;
        IF NOT EXISTS(SELECT 1 FROM @Detalles) THROW 51000, 'Seleccione al menos un producto.', 1;
        IF NOT EXISTS(SELECT 1 FROM dbo.AreaProduccion WHERE Activo=1 AND EsInicio=1) OR NOT EXISTS(SELECT 1 FROM dbo.AreaProduccion WHERE Activo=1 AND EsTermino=1) THROW 51000, 'Configure las areas activas de inicio y termino.', 1;

        DECLARE @Pendientes TABLE(IdOrdenCompraInternaDetalle INT PRIMARY KEY,CantidadPendiente DECIMAL(18,2) NOT NULL);
        INSERT @Pendientes(IdOrdenCompraInternaDetalle,CantidadPendiente)
        SELECT d.IdOrdenCompraInternaDetalle,CONVERT(DECIMAL(18,2),d.Cantidad-CASE WHEN d.CantidadDespachada>ISNULL(prod.CantidadAplicada,0) THEN d.CantidadDespachada ELSE ISNULL(prod.CantidadAplicada,0) END)
        FROM dbo.OrdenCompraInternaDetalle d
        OUTER APPLY
        (
            SELECT SUM(od.CantidadAplicada) CantidadAplicada
            FROM dbo.OrdenTrabajoDetalle od
            JOIN dbo.OrdenTrabajo ot ON ot.IdOrdenTrabajo=od.IdOrdenTrabajo
            WHERE od.IdOrdenCompraInternaDetalle=d.IdOrdenCompraInternaDetalle
              AND ot.Estado<>'ANULADA'
              AND od.Estado<>'ANULADO'
        )prod
        WHERE d.IdOrdenCompraInterna=@IdOrdenCompraInterna
          AND d.Cantidad-CASE WHEN d.CantidadDespachada>ISNULL(prod.CantidadAplicada,0) THEN d.CantidadDespachada ELSE ISNULL(prod.CantidadAplicada,0) END>0;

        IF (SELECT COUNT(*) FROM @Detalles)<>(SELECT COUNT(*) FROM @Pendientes) THROW 51000, 'La OT debe incluir todos los productos pendientes de produccion.', 1;
        IF EXISTS(SELECT 1 FROM @Detalles x LEFT JOIN @Pendientes p ON p.IdOrdenCompraInternaDetalle=x.IdOrdenCompraInternaDetalle WHERE p.IdOrdenCompraInternaDetalle IS NULL OR x.CantidadPlanificada<=0 OR x.CantidadPlanificada>p.CantidadPendiente) THROW 51000, 'La planificacion contiene productos sin pendiente o cantidades no validas.', 1;

        DECLARE @IdOrdenTrabajoRelacionada INT=(SELECT TOP(1) IdOrdenTrabajo FROM dbo.OrdenTrabajo WHERE IdOrdenCompraInterna=@IdOrdenCompraInterna AND Estado<>'ANULADA' ORDER BY IdOrdenTrabajo DESC);
        DECLARE @Correlativo INT = ISNULL((SELECT MAX(TRY_CONVERT(INT,RIGHT(NumeroOT,6))) FROM dbo.OrdenTrabajo WITH(UPDLOCK,HOLDLOCK)),0)+1;
        SET @NumeroOT=CONCAT('OT-',RIGHT(CONCAT('000000',@Correlativo),6));

        INSERT dbo.OrdenTrabajo(NumeroOT,IdOrdenCompraInterna,IdCliente,NombreCliente,IdUsuarioCreacion,Observacion,Estado,TipoOT,IdOrdenTrabajoRelacionada)
        SELECT @NumeroOT,o.IdOrdenCompraInterna,o.IdCliente,o.NombreCliente,@IdUsuario,ISNULL(@Observacion,N''),'PENDIENTE',
               CASE WHEN @IdOrdenTrabajoRelacionada IS NULL THEN 'OCI' ELSE 'OT' END,@IdOrdenTrabajoRelacionada
        FROM dbo.OrdenesCompraInterna o WHERE o.IdOrdenCompraInterna=@IdOrdenCompraInterna AND o.Estado<>'Anulado';
        IF @@ROWCOUNT=0 THROW 51000, 'La OCI no existe o esta anulada.', 1;
        SET @IdOrdenTrabajo=CONVERT(INT,SCOPE_IDENTITY());

        INSERT dbo.OrdenTrabajoDetalle(IdOrdenTrabajo,IdOrdenCompraInternaDetalle,IdProducto,CodigoProducto,NombreProducto,CantidadRequerida,CantidadPlanificada,CantidadPendiente)
        SELECT @IdOrdenTrabajo,d.IdOrdenCompraInternaDetalle,d.IdProducto,d.CodigoProducto,d.NombreProducto,
               p.CantidadPendiente,x.CantidadPlanificada,x.CantidadPlanificada
        FROM @Detalles x JOIN dbo.OrdenCompraInternaDetalle d ON d.IdOrdenCompraInternaDetalle=x.IdOrdenCompraInternaDetalle
        JOIN @Pendientes p ON p.IdOrdenCompraInternaDetalle=d.IdOrdenCompraInternaDetalle;

        INSERT dbo.OrdenTrabajoDetalleArea(IdOrdenTrabajo,IdDetalleOT,IdAreaProduccion,CodigoArea,NombreArea,OrdenSecuencia,EsInicio,EsTermino,ManejaMerma,ModoEnvio)
        SELECT @IdOrdenTrabajo,d.IdDetalleOT,a.IdAreaProduccion,a.CodigoArea,a.NombreArea,a.OrdenSecuencia,a.EsInicio,a.EsTermino,a.ManejaMerma,a.ModoEnvio
        FROM dbo.OrdenTrabajoDetalle d CROSS JOIN dbo.AreaProduccion a WHERE d.IdOrdenTrabajo=@IdOrdenTrabajo AND a.Activo=1;
        UPDATE a SET CantidadRecibida=d.CantidadPlanificada,Estado='PENDIENTE'
        FROM dbo.OrdenTrabajoDetalleArea a JOIN dbo.OrdenTrabajoDetalle d ON d.IdDetalleOT=a.IdDetalleOT
        WHERE a.IdOrdenTrabajo=@IdOrdenTrabajo AND a.EsInicio=1;
        UPDATE dbo.OrdenesCompraInterna SET TieneOrdenTrabajo=1,Estado='PROCESO' WHERE IdOrdenCompraInterna=@IdOrdenCompraInterna;
        COMMIT;
    END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH
END;
GO
