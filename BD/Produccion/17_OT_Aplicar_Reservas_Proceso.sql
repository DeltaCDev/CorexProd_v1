SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_OTDetalle_Cantidades'
      AND parent_object_id = OBJECT_ID('dbo.OrdenTrabajoDetalle')
)
    ALTER TABLE dbo.OrdenTrabajoDetalle DROP CONSTRAINT CK_OTDetalle_Cantidades;
GO

ALTER TABLE dbo.OrdenTrabajoDetalle WITH CHECK ADD CONSTRAINT CK_OTDetalle_Cantidades
CHECK
(
    CantidadRequerida > 0
    AND CantidadPlanificada >= 0
    AND CantidadLanzada >= 0
    AND CantidadProducida >= 0
    AND CantidadAplicada >= 0
    AND CantidadExcedente >= 0
    AND CantidadPendiente >= 0
);
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_VALIDAR_INSUMOS @IdOrdenCompraInterna INT
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH ProductosOCI AS
    (
        SELECT
            D.IdOrdenCompraInternaDetalle,
            D.IdProducto,
            D.CodigoProducto,
            D.NombreProducto,
            D.Observacion,
            CONVERT(DECIMAL(18,3), PEND.CantidadPendiente) AS CantidadPendiente,
            CONVERT(DECIMAL(18,3), ISNULL(SP.StockActual, 0)) AS StockAlmacen,
            CONVERT(DECIMAL(18,3), ISNULL(PRC.StockCorte, 0)) AS StockCorte,
            CONVERT(DECIMAL(18,3), ISNULL(PRC.StockConfeccion, 0) + ISNULL(RES.StockConfeccion, 0)) AS StockConfeccion,
            CONVERT(DECIMAL(18,3), ISNULL(PRC.StockAcabado, 0) + ISNULL(RES.StockAcabado, 0)) AS StockAcabado,
            CONVERT(DECIMAL(18,3), ISNULL(SP.StockActual, 0) + ISNULL(RES.StockProceso, 0)) AS StockTotal,
            CONVERT(DECIMAL(18,3),
                CASE
                    WHEN PEND.CantidadPendiente - ISNULL(SP.StockActual, 0) > 0
                    THEN PEND.CantidadPendiente - ISNULL(SP.StockActual, 0)
                    ELSE 0
                END) AS CantidadObjetivo,
            CONVERT(DECIMAL(18,3),
                CASE
                    WHEN PEND.CantidadPendiente - (ISNULL(SP.StockActual, 0) + ISNULL(RES.StockProceso, 0)) > 0
                    THEN PEND.CantidadPendiente - (ISNULL(SP.StockActual, 0) + ISNULL(RES.StockProceso, 0))
                    ELSE 0
                END) AS Deficit
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
        CROSS APPLY
        (
            SELECT CONVERT(DECIMAL(18,3),
                CASE
                    WHEN D.Cantidad - CASE WHEN D.CantidadDespachada > ISNULL(PROD.CantidadAplicada, 0) THEN D.CantidadDespachada ELSE ISNULL(PROD.CantidadAplicada, 0) END > 0
                    THEN D.Cantidad - CASE WHEN D.CantidadDespachada > ISNULL(PROD.CantidadAplicada, 0) THEN D.CantidadDespachada ELSE ISNULL(PROD.CantidadAplicada, 0) END
                    ELSE 0
                END) AS CantidadPendiente
        ) PEND
        OUTER APPLY (SELECT SUM(S.StockActual) AS StockActual FROM dbo.StockProductosAlmacen S WHERE S.IdProducto = D.IdProducto) SP
        OUTER APPLY
        (
            SELECT
                SUM(R.Cantidad - R.CantidadAplicada) AS StockProceso,
                SUM(CASE WHEN A.NombreArea LIKE '%CORTE%' THEN R.Cantidad - R.CantidadAplicada ELSE 0 END) AS StockCorte,
                SUM(CASE WHEN A.NombreArea LIKE '%CONFECCI%' THEN R.Cantidad - R.CantidadAplicada ELSE 0 END) AS StockConfeccion,
                SUM(CASE WHEN A.NombreArea LIKE '%ACABADO%' THEN R.Cantidad - R.CantidadAplicada ELSE 0 END) AS StockAcabado
            FROM dbo.StockProcesoReserva R
            JOIN dbo.AreaProduccion A ON A.IdAreaProduccion = R.IdAreaProduccion
            WHERE R.IdProducto = D.IdProducto
              AND R.Estado IN ('DISPONIBLE','RESERVADO')
              AND R.Cantidad - R.CantidadAplicada > 0
        ) RES
        OUTER APPLY
        (
            SELECT
                SUM(CASE WHEN A.NombreArea LIKE '%CORTE%' THEN DA.CantidadPendiente ELSE 0 END) AS StockCorte,
                SUM(CASE WHEN A.NombreArea LIKE '%CONFECCI%' THEN DA.CantidadPendiente ELSE 0 END) AS StockConfeccion,
                SUM(CASE WHEN A.NombreArea LIKE '%ACABADO%' THEN DA.CantidadPendiente ELSE 0 END) AS StockAcabado
            FROM dbo.OrdenTrabajoDetalle OD
            JOIN dbo.OrdenTrabajoDetalleArea DA ON DA.IdDetalleOT = OD.IdDetalleOT
            JOIN dbo.AreaProduccion A ON A.IdAreaProduccion = DA.IdAreaProduccion
            WHERE OD.IdProducto = D.IdProducto
              AND OD.Estado NOT IN ('TERMINADO', 'ANULADO')
              AND DA.CantidadPendiente > 0
              AND NOT EXISTS
              (
                  SELECT 1
                  FROM dbo.StockProcesoReserva R
                  WHERE R.IdDetalleArea = DA.IdDetalleArea
                    AND R.Estado IN ('DISPONIBLE','RESERVADO')
                    AND R.Cantidad - R.CantidadAplicada > 0
              )
        ) PRC
        WHERE D.IdOrdenCompraInterna = @IdOrdenCompraInterna
    ), Ficha AS
    (
        SELECT P.*, F.IdFichaTecnica, ROW_NUMBER() OVER(PARTITION BY P.IdProducto ORDER BY F.Version DESC, F.IdFichaTecnica DESC) AS rn
        FROM ProductosOCI P
        LEFT JOIN dbo.FichaTecnica F ON F.IdProducto = P.IdProducto AND F.Estado = 1
        WHERE P.CantidadObjetivo > 0
          AND (P.Deficit > 0 OR P.StockTotal > P.StockAlmacen)
    )
    SELECT
        F.IdOrdenCompraInternaDetalle,
        F.IdProducto,
        F.CodigoProducto,
        F.NombreProducto,
        F.Observacion,
        F.CantidadObjetivo AS CantidadRequerida,
        F.IdFichaTecnica,
        F.StockAlmacen,
        F.StockCorte,
        F.StockConfeccion,
        F.StockAcabado,
        F.StockTotal,
        F.Deficit,
        CASE
            WHEN F.IdFichaTecnica IS NULL
                 OR NOT EXISTS(SELECT 1 FROM dbo.FichaTecnicaDetalle FD WHERE FD.IdFichaTecnica = F.IdFichaTecnica AND FD.Estado = 1)
                THEN 'Sin ficha tecnica'
            WHEN EXISTS
            (
                SELECT 1
                FROM dbo.FichaTecnicaDetalle FD
                LEFT JOIN dbo.StockInsumos SI ON SI.IdInsumo = FD.IdInsumo
                WHERE FD.IdFichaTecnica = F.IdFichaTecnica
                  AND FD.Estado = 1
                  AND ISNULL(SI.StockActual, 0) < FD.Cantidad * F.Deficit
            ) THEN 'Faltantes'
            ELSE 'Completo para producir'
        END AS EstadoInsumos
    FROM Ficha F
    WHERE F.rn = 1
    ORDER BY F.IdOrdenCompraInternaDetalle;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_CREAR
    @IdOrdenCompraInterna INT,
    @IdUsuario INT,
    @Observacion NVARCHAR(500),
    @Detalles dbo.TipoOTPlanificacion READONLY,
    @IdOrdenTrabajo INT OUTPUT,
    @NumeroOT VARCHAR(30) OUTPUT,
    @ProcesarTodaReserva BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        IF NOT EXISTS(SELECT 1 FROM dbo.Usuarios WHERE IdUsuario = @IdUsuario AND Estado = 1)
            THROW 51000, 'El usuario de sesion no es valido.', 1;
        IF NOT EXISTS(SELECT 1 FROM @Detalles)
            THROW 51000, 'Seleccione al menos un producto.', 1;
        IF NOT EXISTS(SELECT 1 FROM dbo.AreaProduccion WHERE Activo = 1 AND EsInicio = 1)
            OR NOT EXISTS(SELECT 1 FROM dbo.AreaProduccion WHERE Activo = 1 AND EsTermino = 1)
            THROW 51000, 'Configure las areas activas de inicio y termino.', 1;

        DECLARE @Pendientes TABLE
        (
            IdOrdenCompraInternaDetalle INT PRIMARY KEY,
            CantidadObjetivo DECIMAL(18,2) NOT NULL,
            CantidadDeficit DECIMAL(18,2) NOT NULL,
            CantidadReservaAplicar DECIMAL(18,2) NOT NULL
        );

        INSERT @Pendientes(IdOrdenCompraInternaDetalle, CantidadObjetivo, CantidadDeficit, CantidadReservaAplicar)
        SELECT
            D.IdOrdenCompraInternaDetalle,
            CONVERT(DECIMAL(18,2), OBJ.CantidadObjetivo),
            CONVERT(DECIMAL(18,2), DEF.Deficit),
            CONVERT(DECIMAL(18,2),
                CASE
                    WHEN @ProcesarTodaReserva = 1 THEN ISNULL(RES.StockProceso, 0)
                    WHEN ISNULL(RES.StockProceso, 0) >= OBJ.CantidadObjetivo THEN OBJ.CantidadObjetivo
                    ELSE ISNULL(RES.StockProceso, 0)
                END)
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
        CROSS APPLY
        (
            SELECT CONVERT(DECIMAL(18,3),
                CASE
                    WHEN D.Cantidad - CASE WHEN D.CantidadDespachada > ISNULL(PROD.CantidadAplicada, 0) THEN D.CantidadDespachada ELSE ISNULL(PROD.CantidadAplicada, 0) END > 0
                    THEN D.Cantidad - CASE WHEN D.CantidadDespachada > ISNULL(PROD.CantidadAplicada, 0) THEN D.CantidadDespachada ELSE ISNULL(PROD.CantidadAplicada, 0) END
                    ELSE 0
                END) AS CantidadPendiente
        ) PEND
        OUTER APPLY (SELECT SUM(S.StockActual) AS StockActual FROM dbo.StockProductosAlmacen S WHERE S.IdProducto = D.IdProducto) SP
        OUTER APPLY
        (
            SELECT SUM(R.Cantidad - R.CantidadAplicada) AS StockProceso
            FROM dbo.StockProcesoReserva R
            WHERE R.IdProducto = D.IdProducto
              AND R.Estado IN ('DISPONIBLE','RESERVADO')
              AND R.Cantidad - R.CantidadAplicada > 0
        ) RES
        CROSS APPLY
        (
            SELECT CONVERT(DECIMAL(18,3),
                CASE
                    WHEN PEND.CantidadPendiente - ISNULL(SP.StockActual, 0) > 0
                    THEN PEND.CantidadPendiente - ISNULL(SP.StockActual, 0)
                    ELSE 0
                END) AS CantidadObjetivo
        ) OBJ
        CROSS APPLY
        (
            SELECT CONVERT(DECIMAL(18,3),
                CASE
                    WHEN OBJ.CantidadObjetivo - ISNULL(RES.StockProceso, 0) > 0
                    THEN OBJ.CantidadObjetivo - ISNULL(RES.StockProceso, 0)
                    ELSE 0
                END) AS Deficit
        ) DEF
        WHERE D.IdOrdenCompraInterna = @IdOrdenCompraInterna
          AND OBJ.CantidadObjetivo > 0
          AND (DEF.Deficit > 0 OR ISNULL(RES.StockProceso, 0) > 0);

        IF EXISTS
        (
            SELECT 1
            FROM @Detalles X
            LEFT JOIN @Pendientes P ON P.IdOrdenCompraInternaDetalle = X.IdOrdenCompraInternaDetalle
            WHERE P.IdOrdenCompraInternaDetalle IS NULL
               OR X.CantidadPlanificada < 0
               OR X.CantidadPlanificada > P.CantidadDeficit
               OR (P.CantidadDeficit > 0 AND X.CantidadPlanificada <= 0)
        )
            THROW 51000, 'La planificacion contiene productos sin deficit o cantidades no validas.', 1;

        DECLARE @IdOrdenTrabajoRelacionada INT =
        (
            SELECT TOP(1) IdOrdenTrabajo
            FROM dbo.OrdenTrabajo
            WHERE IdOrdenCompraInterna = @IdOrdenCompraInterna
              AND UPPER(Estado) <> 'ANULADA'
            ORDER BY IdOrdenTrabajo DESC
        );
        DECLARE @Correlativo INT = ISNULL((SELECT MAX(TRY_CONVERT(INT, RIGHT(NumeroOT, 6))) FROM dbo.OrdenTrabajo WITH(UPDLOCK, HOLDLOCK)), 0) + 1;
        SET @NumeroOT = CONCAT('OT-', RIGHT(CONCAT('000000', @Correlativo), 6));

        INSERT dbo.OrdenTrabajo(NumeroOT, IdOrdenCompraInterna, IdCliente, NombreCliente, IdUsuarioCreacion, Observacion, Estado, TipoOT, IdOrdenTrabajoRelacionada)
        SELECT @NumeroOT, O.IdOrdenCompraInterna, O.IdCliente, O.NombreCliente, @IdUsuario, ISNULL(@Observacion, N''), 'PENDIENTE',
               CASE WHEN @IdOrdenTrabajoRelacionada IS NULL THEN 'OCI' ELSE 'OT' END, @IdOrdenTrabajoRelacionada
        FROM dbo.OrdenesCompraInterna O
        WHERE O.IdOrdenCompraInterna = @IdOrdenCompraInterna
          AND O.Estado <> 'Anulado';
        IF @@ROWCOUNT = 0
            THROW 51000, 'La OCI no existe o esta anulada.', 1;

        SET @IdOrdenTrabajo = CONVERT(INT, SCOPE_IDENTITY());

        INSERT dbo.OrdenTrabajoDetalle(IdOrdenTrabajo, IdOrdenCompraInternaDetalle, IdProducto, CodigoProducto, NombreProducto, CantidadRequerida, CantidadPlanificada, CantidadPendiente)
        SELECT @IdOrdenTrabajo, D.IdOrdenCompraInternaDetalle, D.IdProducto, D.CodigoProducto, D.NombreProducto,
               P.CantidadObjetivo, X.CantidadPlanificada, P.CantidadObjetivo
        FROM @Detalles X
        JOIN dbo.OrdenCompraInternaDetalle D ON D.IdOrdenCompraInternaDetalle = X.IdOrdenCompraInternaDetalle
        JOIN @Pendientes P ON P.IdOrdenCompraInternaDetalle = D.IdOrdenCompraInternaDetalle;

        INSERT dbo.OrdenTrabajoDetalleArea(IdOrdenTrabajo, IdDetalleOT, IdAreaProduccion, CodigoArea, NombreArea, OrdenSecuencia, EsInicio, EsTermino, ManejaMerma, PermiteReservarStockProceso, ModoEnvio)
        SELECT @IdOrdenTrabajo, D.IdDetalleOT, A.IdAreaProduccion, A.CodigoArea, A.NombreArea, A.OrdenSecuencia, A.EsInicio, A.EsTermino, A.ManejaMerma, A.PermiteReservarStockProceso, A.ModoEnvio
        FROM dbo.OrdenTrabajoDetalle D
        CROSS JOIN dbo.AreaProduccion A
        WHERE D.IdOrdenTrabajo = @IdOrdenTrabajo
          AND A.Activo = 1;

        UPDATE A
        SET CantidadRecibida = D.CantidadPlanificada,
            Estado = 'PENDIENTE'
        FROM dbo.OrdenTrabajoDetalleArea A
        JOIN dbo.OrdenTrabajoDetalle D ON D.IdDetalleOT = A.IdDetalleOT
        WHERE A.IdOrdenTrabajo = @IdOrdenTrabajo
          AND A.EsInicio = 1;

        DECLARE @ReservasAplicadas TABLE
        (
            IdStockProcesoReserva BIGINT NOT NULL,
            IdOrdenTrabajoOrigen INT NOT NULL,
            IdDetalleAreaOrigen BIGINT NOT NULL,
            IdDetalleOTNuevo INT NOT NULL,
            IdAreaProduccion INT NOT NULL,
            CantidadAplicar DECIMAL(18,2) NOT NULL
        );

        DECLARE @IdDetalleOTNuevo INT, @IdProducto INT, @NecesidadReserva DECIMAL(18,2);
        DECLARE cur_reserva CURSOR LOCAL FAST_FORWARD FOR
            SELECT
                D.IdDetalleOT,
                D.IdProducto,
                P.CantidadReservaAplicar
            FROM @Detalles X
            JOIN @Pendientes P ON P.IdOrdenCompraInternaDetalle = X.IdOrdenCompraInternaDetalle
            JOIN dbo.OrdenTrabajoDetalle D ON D.IdOrdenCompraInternaDetalle = X.IdOrdenCompraInternaDetalle
                                      AND D.IdOrdenTrabajo = @IdOrdenTrabajo
            ORDER BY D.IdDetalleOT;

        OPEN cur_reserva;
        FETCH NEXT FROM cur_reserva INTO @IdDetalleOTNuevo, @IdProducto, @NecesidadReserva;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            WHILE @NecesidadReserva > 0
            BEGIN
                DECLARE @IdReserva BIGINT = NULL,
                        @IdOtOrigen INT = NULL,
                        @IdDetalleAreaOrigen BIGINT = NULL,
                        @IdAreaProduccion INT = NULL,
                        @DisponibleReserva DECIMAL(18,2) = 0,
                        @Aplicar DECIMAL(18,2) = 0;

                SELECT TOP(1)
                    @IdReserva = R.IdStockProcesoReserva,
                    @IdOtOrigen = R.IdOrdenTrabajo,
                    @IdDetalleAreaOrigen = R.IdDetalleArea,
                    @IdAreaProduccion = R.IdAreaProduccion,
                    @DisponibleReserva = R.Cantidad - R.CantidadAplicada
                FROM dbo.StockProcesoReserva R WITH(UPDLOCK, HOLDLOCK)
                WHERE R.IdProducto = @IdProducto
                  AND R.Estado IN ('DISPONIBLE','RESERVADO')
                  AND R.Cantidad - R.CantidadAplicada > 0
                ORDER BY R.FechaRegistro, R.IdStockProcesoReserva;

                IF @IdReserva IS NULL BREAK;

                SET @Aplicar = CASE WHEN @DisponibleReserva >= @NecesidadReserva THEN @NecesidadReserva ELSE @DisponibleReserva END;

                INSERT @ReservasAplicadas(IdStockProcesoReserva, IdOrdenTrabajoOrigen, IdDetalleAreaOrigen, IdDetalleOTNuevo, IdAreaProduccion, CantidadAplicar)
                VALUES(@IdReserva, @IdOtOrigen, @IdDetalleAreaOrigen, @IdDetalleOTNuevo, @IdAreaProduccion, @Aplicar);

                UPDATE dbo.StockProcesoReserva
                SET CantidadAplicada = CantidadAplicada + @Aplicar,
                    Estado = CASE WHEN CantidadAplicada + @Aplicar >= Cantidad THEN 'APLICADO' ELSE Estado END,
                    Observacion = LEFT(CONCAT(Observacion, CASE WHEN NULLIF(Observacion,N'') IS NULL THEN N'' ELSE N' | ' END, N'Aplicado a ', @NumeroOT), 500)
                WHERE IdStockProcesoReserva = @IdReserva;

                UPDATE dbo.OrdenTrabajoDetalleArea
                SET CantidadEnviada = CantidadEnviada + @Aplicar,
                    Estado = CASE WHEN CantidadRecibida - (CantidadEnviada + @Aplicar) - CantidadMerma <= 0 THEN 'FINALIZADA' ELSE 'PARCIAL' END,
                    FechaFin = CASE WHEN CantidadRecibida - (CantidadEnviada + @Aplicar) - CantidadMerma <= 0 THEN SYSDATETIME() ELSE FechaFin END
                WHERE IdDetalleArea = @IdDetalleAreaOrigen;

                SET @NecesidadReserva -= @Aplicar;
            END;

            FETCH NEXT FROM cur_reserva INTO @IdDetalleOTNuevo, @IdProducto, @NecesidadReserva;
        END;

        CLOSE cur_reserva;
        DEALLOCATE cur_reserva;

        UPDATE A
        SET CantidadRecibida = A.CantidadRecibida + R.CantidadAplicada,
            Estado = 'EN_PROCESO',
            FechaInicio = COALESCE(A.FechaInicio, SYSDATETIME())
        FROM dbo.OrdenTrabajoDetalleArea A
        JOIN
        (
            SELECT IdDetalleOTNuevo, IdAreaProduccion, SUM(CantidadAplicar) AS CantidadAplicada
            FROM @ReservasAplicadas
            GROUP BY IdDetalleOTNuevo, IdAreaProduccion
        ) R ON R.IdDetalleOTNuevo = A.IdDetalleOT AND R.IdAreaProduccion = A.IdAreaProduccion;

        UPDATE D
        SET CantidadLanzada = D.CantidadPlanificada + R.CantidadAplicada,
            Estado = 'EN_PROCESO',
            FechaInicio = COALESCE(D.FechaInicio, SYSDATETIME())
        FROM dbo.OrdenTrabajoDetalle D
        JOIN
        (
            SELECT IdDetalleOTNuevo, SUM(CantidadAplicar) AS CantidadAplicada
            FROM @ReservasAplicadas
            GROUP BY IdDetalleOTNuevo
        ) R ON R.IdDetalleOTNuevo = D.IdDetalleOT
        WHERE D.IdOrdenTrabajo = @IdOrdenTrabajo
          AND D.CantidadPlanificada + R.CantidadAplicada > 0;

        UPDATE A
        SET Estado = 'EN_PROCESO',
            FechaInicio = COALESCE(A.FechaInicio, SYSDATETIME())
        FROM dbo.OrdenTrabajoDetalleArea A
        WHERE A.IdOrdenTrabajo = @IdOrdenTrabajo
          AND A.EsInicio = 1
          AND A.CantidadRecibida > 0
          AND EXISTS
          (
              SELECT 1
              FROM @ReservasAplicadas R
              WHERE R.IdDetalleOTNuevo = A.IdDetalleOT
          );

        DECLARE @IdOtRecalculo INT;
        DECLARE cur_ot_recalculo CURSOR LOCAL FAST_FORWARD FOR
            SELECT DISTINCT IdOrdenTrabajoOrigen FROM @ReservasAplicadas;
        OPEN cur_ot_recalculo;
        FETCH NEXT FROM cur_ot_recalculo INTO @IdOtRecalculo;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            EXEC dbo.USP_PRO_OT_RECALCULAR_ESTADO @IdOtRecalculo;
            FETCH NEXT FROM cur_ot_recalculo INTO @IdOtRecalculo;
        END;
        CLOSE cur_ot_recalculo;
        DEALLOCATE cur_ot_recalculo;

        UPDATE dbo.OrdenesCompraInterna
        SET TieneOrdenTrabajo = 1,
            Estado = 'PROCESO'
        WHERE IdOrdenCompraInterna = @IdOrdenCompraInterna;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF CURSOR_STATUS('local','cur_reserva') >= 0
        BEGIN
            CLOSE cur_reserva;
        END;
        IF CURSOR_STATUS('local','cur_reserva') >= -1
        BEGIN
            DEALLOCATE cur_reserva;
        END;
        IF CURSOR_STATUS('local','cur_ot_recalculo') >= 0
        BEGIN
            CLOSE cur_ot_recalculo;
        END;
        IF CURSOR_STATUS('local','cur_ot_recalculo') >= -1
        BEGIN
            DEALLOCATE cur_ot_recalculo;
        END;
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO
