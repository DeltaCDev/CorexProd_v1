SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
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
            CONVERT(DECIMAL(18,3), ISNULL(STK.StockFisico, 0)) AS StockFisico,
            CONVERT(DECIMAL(18,3), ISNULL(ROC.StockReservadoOci, 0)) AS StockReservadoOci,
            CONVERT(DECIMAL(18,3), ISNULL(ROT.StockReservadoOtros, 0)) AS StockReservadoOtros,
            CONVERT(DECIMAL(18,3),
                CASE
                    WHEN ISNULL(STK.StockFisico, 0) - ISNULL(ROC.StockReservadoOci, 0) - ISNULL(ROT.StockReservadoOtros, 0) > 0
                    THEN ISNULL(STK.StockFisico, 0) - ISNULL(ROC.StockReservadoOci, 0) - ISNULL(ROT.StockReservadoOtros, 0)
                    ELSE 0
                END) AS StockLibre,
            CONVERT(DECIMAL(18,3), ISNULL(PRC.StockCorte, 0)) AS StockCorte,
            CONVERT(DECIMAL(18,3), ISNULL(PRC.StockConfeccion, 0) + ISNULL(RES.StockConfeccion, 0)) AS StockConfeccion,
            CONVERT(DECIMAL(18,3), ISNULL(PRC.StockAcabado, 0) + ISNULL(RES.StockAcabado, 0)) AS StockAcabado,
            CONVERT(DECIMAL(18,3), ISNULL(RES.StockProceso, 0)) AS StockProceso
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
        OUTER APPLY
        (
            SELECT SUM(S.StockActual) AS StockFisico
            FROM dbo.StockProductosAlmacen S
            WHERE S.IdProducto = D.IdProducto
        ) STK
        OUTER APPLY
        (
            SELECT SUM(R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada) AS StockReservadoOci
            FROM dbo.StockReserva R
            WHERE R.IdOrdenCompraInternaDetalle = D.IdOrdenCompraInternaDetalle
              AND R.Estado IN ('ACTIVA','PARCIALMENTE_CONSUMIDA')
              AND R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada > 0
        ) ROC
        OUTER APPLY
        (
            SELECT SUM(R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada) AS StockReservadoOtros
            FROM dbo.StockReserva R
            WHERE R.IdProducto = D.IdProducto
              AND NOT
              (
                  R.IdOrdenCompraInterna = D.IdOrdenCompraInterna
                  AND R.IdOrdenCompraInternaDetalle = D.IdOrdenCompraInternaDetalle
              )
              AND R.Estado IN ('ACTIVA','PARCIALMENTE_CONSUMIDA')
              AND R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada > 0
        ) ROT
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
    ), Calculo AS
    (
        SELECT
            P.*,
            CONVERT(DECIMAL(18,3),
                CASE
                    WHEN P.CantidadPendiente - ISNULL(P.StockReservadoOci, 0) - ISNULL(P.StockLibre, 0) > 0
                    THEN P.CantidadPendiente - ISNULL(P.StockReservadoOci, 0) - ISNULL(P.StockLibre, 0)
                    ELSE 0
                END) AS CantidadObjetivo,
            CONVERT(DECIMAL(18,3), ISNULL(P.StockReservadoOci, 0) + ISNULL(P.StockLibre, 0)) AS StockDisponibleParaOci,
            CONVERT(DECIMAL(18,3), ISNULL(P.StockReservadoOci, 0) + ISNULL(P.StockLibre, 0) + ISNULL(P.StockProceso, 0)) AS StockTotalParaOci
        FROM ProductosOCI P
    ), Ficha AS
    (
        SELECT C.*, F.IdFichaTecnica, ROW_NUMBER() OVER(PARTITION BY C.IdProducto ORDER BY F.Version DESC, F.IdFichaTecnica DESC) AS rn
        FROM Calculo C
        LEFT JOIN dbo.FichaTecnica F ON F.IdProducto = C.IdProducto AND F.Estado = 1
        WHERE C.CantidadObjetivo > 0
           OR C.StockProceso > 0
    )
    SELECT
        F.IdOrdenCompraInternaDetalle,
        F.IdProducto,
        F.CodigoProducto,
        F.NombreProducto,
        F.Observacion,
        F.CantidadObjetivo AS CantidadRequerida,
        F.IdFichaTecnica,
        F.StockDisponibleParaOci AS StockAlmacen,
        F.StockCorte,
        F.StockConfeccion,
        F.StockAcabado,
        F.StockTotalParaOci AS StockTotal,
        CONVERT(DECIMAL(18,3),
            CASE WHEN F.CantidadObjetivo - ISNULL(F.StockProceso, 0) > 0 THEN F.CantidadObjetivo - ISNULL(F.StockProceso, 0) ELSE 0 END) AS Deficit,
        F.StockFisico,
        F.StockReservadoOci,
        F.StockReservadoOtros,
        F.StockLibre AS StockDisponibleReal,
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
                  AND ISNULL(SI.StockActual, 0) < FD.Cantidad *
                    CASE WHEN F.CantidadObjetivo - ISNULL(F.StockProceso, 0) > 0 THEN F.CantidadObjetivo - ISNULL(F.StockProceso, 0) ELSE 0 END
            ) THEN 'Faltantes'
            ELSE 'Completo para producir'
        END AS EstadoInsumos
    FROM Ficha F
    WHERE F.rn = 1
      AND F.CantidadObjetivo > 0
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

        DECLARE @UsuarioReserva VARCHAR(100);
        SELECT @UsuarioReserva = NombreUsuario FROM dbo.Usuarios WHERE IdUsuario = @IdUsuario AND Estado = 1;

        IF @UsuarioReserva IS NULL
            THROW 51000, 'El usuario de sesion no es valido.', 1;
        IF NOT EXISTS(SELECT 1 FROM @Detalles)
            THROW 51000, 'Seleccione al menos un producto.', 1;
        IF NOT EXISTS(SELECT 1 FROM dbo.AreaProduccion WHERE Activo = 1 AND EsInicio = 1)
            OR NOT EXISTS(SELECT 1 FROM dbo.AreaProduccion WHERE Activo = 1 AND EsTermino = 1)
            THROW 51000, 'Configure las areas activas de inicio y termino.', 1;

        DECLARE @Pendientes TABLE
        (
            IdOrdenCompraInternaDetalle INT PRIMARY KEY,
            IdProducto INT NOT NULL,
            CantidadPendiente DECIMAL(18,2) NOT NULL,
            CantidadReservadaOci DECIMAL(18,2) NOT NULL,
            CantidadReservarStock DECIMAL(18,2) NOT NULL,
            CantidadObjetivo DECIMAL(18,2) NOT NULL,
            CantidadDeficit DECIMAL(18,2) NOT NULL,
            CantidadReservaAplicar DECIMAL(18,2) NOT NULL
        );

        INSERT @Pendientes
        (
            IdOrdenCompraInternaDetalle, IdProducto, CantidadPendiente, CantidadReservadaOci,
            CantidadReservarStock, CantidadObjetivo, CantidadDeficit, CantidadReservaAplicar
        )
        SELECT
            D.IdOrdenCompraInternaDetalle,
            D.IdProducto,
            CONVERT(DECIMAL(18,2), PEND.CantidadPendiente),
            CONVERT(DECIMAL(18,2), ISNULL(ROC.StockReservadoOci, 0)),
            CONVERT(DECIMAL(18,2),
                CASE
                    WHEN PEND.CantidadPendiente - ISNULL(ROC.StockReservadoOci, 0) <= 0 THEN 0
                    WHEN ISNULL(DISP.StockLibre, 0) >= PEND.CantidadPendiente - ISNULL(ROC.StockReservadoOci, 0)
                        THEN PEND.CantidadPendiente - ISNULL(ROC.StockReservadoOci, 0)
                    ELSE ISNULL(DISP.StockLibre, 0)
                END),
            CONVERT(DECIMAL(18,2),
                CASE
                    WHEN PEND.CantidadPendiente - ISNULL(ROC.StockReservadoOci, 0) - ISNULL(DISP.StockLibre, 0) > 0
                    THEN PEND.CantidadPendiente - ISNULL(ROC.StockReservadoOci, 0) - ISNULL(DISP.StockLibre, 0)
                    ELSE 0
                END),
            CONVERT(DECIMAL(18,2),
                CASE
                    WHEN PEND.CantidadPendiente - ISNULL(ROC.StockReservadoOci, 0) - ISNULL(DISP.StockLibre, 0) - ISNULL(RES.StockProceso, 0) > 0
                    THEN PEND.CantidadPendiente - ISNULL(ROC.StockReservadoOci, 0) - ISNULL(DISP.StockLibre, 0) - ISNULL(RES.StockProceso, 0)
                    ELSE 0
                END),
            CONVERT(DECIMAL(18,2),
                CASE
                    WHEN @ProcesarTodaReserva = 1 THEN ISNULL(RES.StockProceso, 0)
                    WHEN ISNULL(RES.StockProceso, 0) >=
                        CASE
                            WHEN PEND.CantidadPendiente - ISNULL(ROC.StockReservadoOci, 0) - ISNULL(DISP.StockLibre, 0) > 0
                            THEN PEND.CantidadPendiente - ISNULL(ROC.StockReservadoOci, 0) - ISNULL(DISP.StockLibre, 0)
                            ELSE 0
                        END
                        THEN CASE
                            WHEN PEND.CantidadPendiente - ISNULL(ROC.StockReservadoOci, 0) - ISNULL(DISP.StockLibre, 0) > 0
                            THEN PEND.CantidadPendiente - ISNULL(ROC.StockReservadoOci, 0) - ISNULL(DISP.StockLibre, 0)
                            ELSE 0
                        END
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
        OUTER APPLY
        (
            SELECT SUM(R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada) AS StockReservadoOci
            FROM dbo.StockReserva R WITH (UPDLOCK, HOLDLOCK)
            WHERE R.IdOrdenCompraInternaDetalle = D.IdOrdenCompraInternaDetalle
              AND R.Estado IN ('ACTIVA','PARCIALMENTE_CONSUMIDA')
              AND R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada > 0
        ) ROC
        OUTER APPLY
        (
            SELECT SUM(R.Cantidad - R.CantidadAplicada) AS StockProceso
            FROM dbo.StockProcesoReserva R WITH (UPDLOCK, HOLDLOCK)
            WHERE R.IdProducto = D.IdProducto
              AND R.Estado IN ('DISPONIBLE','RESERVADO')
              AND R.Cantidad - R.CantidadAplicada > 0
        ) RES
        OUTER APPLY
        (
            SELECT SUM(CASE WHEN S.StockActual - ISNULL(RA.ReservadoAlmacen, 0) > 0 THEN S.StockActual - ISNULL(RA.ReservadoAlmacen, 0) ELSE 0 END) AS StockLibre
            FROM dbo.StockProductosAlmacen S WITH (UPDLOCK, HOLDLOCK)
            OUTER APPLY
            (
                SELECT SUM(R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada) AS ReservadoAlmacen
                FROM dbo.StockReserva R WITH (UPDLOCK, HOLDLOCK)
                WHERE R.IdProducto = S.IdProducto
                  AND R.IdAlmacen = S.IdAlmacen
                  AND R.Estado IN ('ACTIVA','PARCIALMENTE_CONSUMIDA')
                  AND R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada > 0
            ) RA
            WHERE S.IdProducto = D.IdProducto
        ) DISP
        WHERE D.IdOrdenCompraInterna = @IdOrdenCompraInterna
          AND PEND.CantidadPendiente > 0;

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

        DECLARE @IdDetalleOciReserva INT, @IdProductoReserva INT, @IdDetalleOTReserva INT, @CantidadReservar DECIMAL(18,2);
        DECLARE cur_stock_reserva CURSOR LOCAL FAST_FORWARD FOR
            SELECT P.IdOrdenCompraInternaDetalle, P.IdProducto, D.IdDetalleOT, P.CantidadReservarStock
            FROM @Pendientes P
            JOIN dbo.OrdenTrabajoDetalle D ON D.IdOrdenCompraInternaDetalle = P.IdOrdenCompraInternaDetalle
                                      AND D.IdOrdenTrabajo = @IdOrdenTrabajo
            WHERE P.CantidadReservarStock > 0
            ORDER BY P.IdOrdenCompraInternaDetalle;

        OPEN cur_stock_reserva;
        FETCH NEXT FROM cur_stock_reserva INTO @IdDetalleOciReserva, @IdProductoReserva, @IdDetalleOTReserva, @CantidadReservar;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            WHILE @CantidadReservar > 0
            BEGIN
                DECLARE @IdAlmacenReserva INT = NULL, @DisponibleAlmacen DECIMAL(18,2) = 0, @ReservarAlmacen DECIMAL(18,2) = 0;

                SELECT TOP(1)
                    @IdAlmacenReserva = S.IdAlmacen,
                    @DisponibleAlmacen = CASE WHEN S.StockActual - ISNULL(RA.ReservadoAlmacen, 0) > 0 THEN S.StockActual - ISNULL(RA.ReservadoAlmacen, 0) ELSE 0 END
                FROM dbo.StockProductosAlmacen S WITH (UPDLOCK, HOLDLOCK)
                OUTER APPLY
                (
                    SELECT SUM(R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada) AS ReservadoAlmacen
                    FROM dbo.StockReserva R WITH (UPDLOCK, HOLDLOCK)
                    WHERE R.IdProducto = S.IdProducto
                      AND R.IdAlmacen = S.IdAlmacen
                      AND R.Estado IN ('ACTIVA','PARCIALMENTE_CONSUMIDA')
                      AND R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada > 0
                ) RA
                WHERE S.IdProducto = @IdProductoReserva
                  AND S.StockActual - ISNULL(RA.ReservadoAlmacen, 0) > 0
                ORDER BY S.IdAlmacen;

                IF @IdAlmacenReserva IS NULL
                    THROW 51000, 'No hay stock disponible suficiente para reservar.', 1;

                SET @ReservarAlmacen = CASE WHEN @DisponibleAlmacen >= @CantidadReservar THEN @CantidadReservar ELSE @DisponibleAlmacen END;

                INSERT dbo.StockReserva
                (
                    IdOrdenCompraInterna, IdOrdenCompraInternaDetalle, IdProducto, IdAlmacen,
                    IdOrdenTrabajo, IdDetalleOT, CantidadReservada, TipoOrigen, Estado,
                    UsuarioReserva, UsuarioActualizacion, Observacion
                )
                VALUES
                (
                    @IdOrdenCompraInterna, @IdDetalleOciReserva, @IdProductoReserva, @IdAlmacenReserva,
                    @IdOrdenTrabajo, @IdDetalleOTReserva, @ReservarAlmacen, 'STOCK_FISICO', 'ACTIVA',
                    @UsuarioReserva, @UsuarioReserva, CONCAT('Reserva automatica al generar ', @NumeroOT)
                );

                DECLARE @IdStockReserva BIGINT = CONVERT(BIGINT, SCOPE_IDENTITY());

                INSERT dbo.StockReservaMovimiento
                (
                    IdStockReserva, TipoMovimiento, Cantidad, EstadoAnterior, EstadoNuevo,
                    DocumentoReferencia, UsuarioMovimiento, Observacion
                )
                VALUES
                (
                    @IdStockReserva, 'CREADA', @ReservarAlmacen, NULL, 'ACTIVA',
                    @NumeroOT, @UsuarioReserva, 'Reserva automatica de stock fisico para OC.'
                );

                SET @CantidadReservar -= @ReservarAlmacen;
            END;

            FETCH NEXT FROM cur_stock_reserva INTO @IdDetalleOciReserva, @IdProductoReserva, @IdDetalleOTReserva, @CantidadReservar;
        END;
        CLOSE cur_stock_reserva;
        DEALLOCATE cur_stock_reserva;

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
            SELECT D.IdDetalleOT, D.IdProducto, P.CantidadReservaAplicar
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
        IF CURSOR_STATUS('local','cur_stock_reserva') >= 0 CLOSE cur_stock_reserva;
        IF CURSOR_STATUS('local','cur_stock_reserva') >= -1 DEALLOCATE cur_stock_reserva;
        IF CURSOR_STATUS('local','cur_reserva') >= 0 CLOSE cur_reserva;
        IF CURSOR_STATUS('local','cur_reserva') >= -1 DEALLOCATE cur_reserva;
        IF CURSOR_STATUS('local','cur_ot_recalculo') >= 0 CLOSE cur_ot_recalculo;
        IF CURSOR_STATUS('local','cur_ot_recalculo') >= -1 DEALLOCATE cur_ot_recalculo;
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
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

        DECLARE @UsuarioReserva VARCHAR(100);
        SELECT @UsuarioReserva = NombreUsuario FROM dbo.Usuarios WHERE IdUsuario = @IdUsuarioSesion;

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

        DECLARE @ReservasProduccion TABLE
        (
            IdDetalleOT INT NOT NULL,
            IdOrdenCompraInternaDetalle INT NOT NULL,
            IdProducto INT NOT NULL,
            CantidadReservar DECIMAL(18,2) NOT NULL
        );

        DECLARE @ReservasProduccionCreadas TABLE
        (
            IdStockReserva BIGINT NOT NULL,
            CantidadReservada DECIMAL(18,2) NOT NULL,
            Estado VARCHAR(30) NOT NULL
        );

        INSERT @ReservasProduccion(IdDetalleOT, IdOrdenCompraInternaDetalle, IdProducto, CantidadReservar)
        SELECT
            D.IdDetalleOT,
            D.IdOrdenCompraInternaDetalle,
            D.IdProducto,
            CONVERT(DECIMAL(18,2),
                CASE
                    WHEN X.Cantidad <= 0 THEN 0
                    WHEN OciPend.PendienteOci - ISNULL(ResActual.ReservaActual, 0) <= 0 THEN 0
                    WHEN X.Cantidad < OciPend.PendienteOci - ISNULL(ResActual.ReservaActual, 0) THEN X.Cantidad
                    ELSE OciPend.PendienteOci - ISNULL(ResActual.ReservaActual, 0)
                END)
        FROM @Detalles X
        JOIN dbo.OrdenTrabajoDetalle D ON D.IdDetalleOT = X.IdDetalleOT
        JOIN dbo.OrdenCompraInternaDetalle OCD ON OCD.IdOrdenCompraInternaDetalle = D.IdOrdenCompraInternaDetalle
        CROSS APPLY
        (
            SELECT CASE WHEN OCD.Cantidad - OCD.CantidadDespachada > 0 THEN OCD.Cantidad - OCD.CantidadDespachada ELSE 0 END AS PendienteOci
        ) OciPend
        OUTER APPLY
        (
            SELECT SUM(R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada) AS ReservaActual
            FROM dbo.StockReserva R WITH (UPDLOCK, HOLDLOCK)
            WHERE R.IdOrdenCompraInternaDetalle = D.IdOrdenCompraInternaDetalle
              AND R.Estado IN ('ACTIVA','PARCIALMENTE_CONSUMIDA')
              AND R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada > 0
        ) ResActual
        WHERE D.IdOrdenTrabajo = @IdOrdenTrabajo
          AND D.IdOrdenCompraInternaDetalle IS NOT NULL;

        INSERT dbo.StockReserva
        (
            IdOrdenCompraInterna, IdOrdenCompraInternaDetalle, IdProducto, IdAlmacen,
            IdOrdenTrabajo, IdDetalleOT, CantidadReservada, TipoOrigen, Estado,
            UsuarioReserva, UsuarioActualizacion, Observacion
        )
        OUTPUT INSERTED.IdStockReserva, INSERTED.CantidadReservada, INSERTED.Estado
        INTO @ReservasProduccionCreadas(IdStockReserva, CantidadReservada, Estado)
        SELECT
            OT.IdOrdenCompraInterna, R.IdOrdenCompraInternaDetalle, R.IdProducto, @IdAlmacen,
            @IdOrdenTrabajo, R.IdDetalleOT, R.CantidadReservar, 'PRODUCCION_OT', 'ACTIVA',
            ISNULL(@UsuarioReserva, 'Sistema'), ISNULL(@UsuarioReserva, 'Sistema'), CONCAT('Produccion comprometida - OT ', OT.NumeroOT)
        FROM @ReservasProduccion R
        JOIN dbo.OrdenTrabajo OT ON OT.IdOrdenTrabajo = @IdOrdenTrabajo
        WHERE R.CantidadReservar > 0;

        INSERT dbo.StockReservaMovimiento
        (
            IdStockReserva, TipoMovimiento, Cantidad, EstadoAnterior, EstadoNuevo,
            DocumentoReferencia, UsuarioMovimiento, Observacion
        )
        SELECT
            SR.IdStockReserva, 'CREADA', SR.CantidadReservada, NULL, SR.Estado,
            OT.NumeroOT, ISNULL(@UsuarioReserva, 'Sistema'), 'Reserva automatica por ingreso de produccion terminada.'
        FROM @ReservasProduccionCreadas SR
        CROSS JOIN dbo.OrdenTrabajo OT
        WHERE OT.IdOrdenTrabajo = @IdOrdenTrabajo;

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

CREATE OR ALTER PROCEDURE dbo.USP_VEN_GUIA_INTERNA_PREPARAR
    @IdOrdenCompraInterna INT,
    @IdAlmacen INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @IdAlmacen IS NULL
        SELECT TOP (1) @IdAlmacen = IdAlmacen FROM dbo.Almacenes WHERE Estado = 1 ORDER BY IdAlmacen;

    SELECT
        O.IdOrdenCompraInterna,
        O.NumeroOci,
        ISNULL(OT.NumeroOT, '') AS NumeroProforma,
        ISNULL(OT.NumeroOT, '') AS NumeroOrdenTrabajo,
        O.OrdenCompraCliente,
        A.IdAlmacen,
        A.NombreAlmacen,
        ISNULL(E.Ruc, '') AS RucEmisor,
        ISNULL(E.Nombre, '') AS EmpresaEmisora,
        ISNULL(C.NumeroDocumento, '') AS RucDestino,
        O.NombreCliente AS EmpresaDestino
    FROM dbo.OrdenesCompraInterna O
    INNER JOIN dbo.Almacenes A ON A.IdAlmacen = @IdAlmacen AND A.Estado = 1
    INNER JOIN dbo.Clientes C ON C.IdCliente = O.IdCliente
    OUTER APPLY
    (
        SELECT TOP (1) Ruc, Nombre
        FROM dbo.Empresas
        WHERE Estado = 1
        ORDER BY EsPredeterminada DESC, IdEmpresa
    ) E
    OUTER APPLY
    (
        SELECT TOP (1) T.NumeroOT
        FROM dbo.OrdenTrabajo T
        WHERE T.IdOrdenCompraInterna = O.IdOrdenCompraInterna
          AND UPPER(ISNULL(T.Estado, '')) NOT IN ('ANULADA', 'ANULADO')
        ORDER BY CASE WHEN T.IdOrdenTrabajoRelacionada IS NULL THEN 0 ELSE 1 END, T.IdOrdenTrabajo DESC
    ) OT
    WHERE O.IdOrdenCompraInterna = @IdOrdenCompraInterna
      AND O.Estado <> 'Anulado';

    SELECT
        D.IdOrdenCompraInternaDetalle,
        D.IdProducto,
        D.CodigoProducto,
        D.NombreProducto,
        P.IdUnidadMedida,
        UM.NombreUnidad,
        D.Cantidad AS CantidadRequerida,
        D.CantidadDespachada AS CantidadEntregada,
        D.Cantidad - D.CantidadDespachada AS CantidadPendiente,
        CAST(CASE
            WHEN ISNULL(S.StockActual, 0) < ISNULL(RO.ReservaOci, 0) + ISNULL(DISP.StockLibre, 0)
                THEN ISNULL(S.StockActual, 0)
            ELSE ISNULL(RO.ReservaOci, 0) + ISNULL(DISP.StockLibre, 0)
        END AS DECIMAL(18,2)) AS StockActual,
        CAST(ISNULL(S.StockActual, 0) AS DECIMAL(18,2)) AS StockFisico,
        CAST(ISNULL(RO.ReservaOci, 0) AS DECIMAL(18,2)) AS StockReservadoOci,
        CAST(ISNULL(DISP.StockLibre, 0) AS DECIMAL(18,2)) AS StockDisponibleReal,
        D.PrecioUnitario,
        CAST(CASE
            WHEN D.Cantidad - D.CantidadDespachada <= 0 THEN 0
            WHEN ISNULL(S.StockActual, 0) <= 0 THEN 0
            WHEN D.Cantidad - D.CantidadDespachada <
                CASE
                    WHEN ISNULL(S.StockActual, 0) < ISNULL(RO.ReservaOci, 0) + ISNULL(DISP.StockLibre, 0)
                        THEN ISNULL(S.StockActual, 0)
                    ELSE ISNULL(RO.ReservaOci, 0) + ISNULL(DISP.StockLibre, 0)
                END
                THEN D.Cantidad - D.CantidadDespachada
            ELSE
                CASE
                    WHEN ISNULL(S.StockActual, 0) < ISNULL(RO.ReservaOci, 0) + ISNULL(DISP.StockLibre, 0)
                        THEN ISNULL(S.StockActual, 0)
                    ELSE ISNULL(RO.ReservaOci, 0) + ISNULL(DISP.StockLibre, 0)
                END
        END AS DECIMAL(18,2)) AS CantidadSugerida,
        D.Observacion
    FROM dbo.OrdenCompraInternaDetalle D
    INNER JOIN dbo.Productos P ON P.IdProducto = D.IdProducto
    INNER JOIN dbo.UnidadesMedida UM ON UM.IdUnidadMedida = P.IdUnidadMedida
    LEFT JOIN dbo.StockProductosAlmacen S ON S.IdProducto = D.IdProducto AND S.IdAlmacen = @IdAlmacen
    OUTER APPLY
    (
        SELECT SUM(R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada) AS ReservaOci
        FROM dbo.StockReserva R
        WHERE R.IdOrdenCompraInternaDetalle = D.IdOrdenCompraInternaDetalle
          AND (R.IdAlmacen = @IdAlmacen OR R.IdAlmacen IS NULL)
          AND R.Estado IN ('ACTIVA','PARCIALMENTE_CONSUMIDA')
          AND R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada > 0
    ) RO
    OUTER APPLY
    (
        SELECT CASE
            WHEN ISNULL(S.StockActual, 0) - ISNULL(RT.ReservaTotalAlmacen, 0) > 0
            THEN ISNULL(S.StockActual, 0) - ISNULL(RT.ReservaTotalAlmacen, 0)
            ELSE 0
        END AS StockLibre
        FROM
        (
            SELECT SUM(R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada) AS ReservaTotalAlmacen
            FROM dbo.StockReserva R
            WHERE R.IdProducto = D.IdProducto
              AND R.IdAlmacen = @IdAlmacen
              AND R.Estado IN ('ACTIVA','PARCIALMENTE_CONSUMIDA')
              AND R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada > 0
        ) RT
    ) DISP
    WHERE D.IdOrdenCompraInterna = @IdOrdenCompraInterna
      AND D.Cantidad > D.CantidadDespachada
    ORDER BY D.IdOrdenCompraInternaDetalle;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_VEN_GUIA_INTERNA_EMITIR
    @IdOrdenCompraInterna INT,
    @IdAlmacen INT,
    @FechaEmision DATE,
    @UsuarioEmisor VARCHAR(80),
    @UsuarioAutorizador VARCHAR(80),
    @Observacion VARCHAR(500),
    @Detalles dbo.GuiaInternaDetalleType READONLY,
    @NumeroGuia VARCHAR(30) OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @NumeroGuia = '';

    IF NOT EXISTS (SELECT 1 FROM @Detalles WHERE CantidadDespachar > 0)
    BEGIN
        SET @Mensaje = 'Debe indicar al menos un producto para despachar.';
        RETURN;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS
        (
            SELECT 1 FROM dbo.OrdenesCompraInterna WITH (UPDLOCK, HOLDLOCK)
            WHERE IdOrdenCompraInterna = @IdOrdenCompraInterna AND Estado <> 'Anulado'
        )
            THROW 51000, 'La OCI no existe o se encuentra anulada.', 1;

        IF EXISTS
        (
            SELECT 1
            FROM @Detalles T
            LEFT JOIN dbo.OrdenCompraInternaDetalle D WITH (UPDLOCK, HOLDLOCK)
                ON D.IdOrdenCompraInternaDetalle = T.IdOrdenCompraInternaDetalle
               AND D.IdOrdenCompraInterna = @IdOrdenCompraInterna
            WHERE D.IdOrdenCompraInternaDetalle IS NULL
               OR T.CantidadDespachar <= 0
        )
            THROW 51001, 'Uno o mas detalles de la guia no son validos.', 1;

        DECLARE @CodigoProductoInvalido VARCHAR(100), @CantidadMaxima DECIMAL(18,2), @MensajeValidacion VARCHAR(500);
        SELECT TOP (1)
            @CodigoProductoInvalido = D.CodigoProducto,
            @CantidadMaxima = MAXIMO.CantidadMaxima
        FROM @Detalles T
        INNER JOIN dbo.OrdenCompraInternaDetalle D WITH (UPDLOCK, HOLDLOCK)
            ON D.IdOrdenCompraInternaDetalle = T.IdOrdenCompraInternaDetalle
           AND D.IdOrdenCompraInterna = @IdOrdenCompraInterna
        LEFT JOIN dbo.StockProductosAlmacen S WITH (UPDLOCK, HOLDLOCK)
            ON S.IdProducto = D.IdProducto AND S.IdAlmacen = @IdAlmacen
        OUTER APPLY
        (
            SELECT SUM(R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada) AS ReservaOci
            FROM dbo.StockReserva R WITH (UPDLOCK, HOLDLOCK)
            WHERE R.IdOrdenCompraInternaDetalle = D.IdOrdenCompraInternaDetalle
              AND (R.IdAlmacen = @IdAlmacen OR R.IdAlmacen IS NULL)
              AND R.Estado IN ('ACTIVA','PARCIALMENTE_CONSUMIDA')
              AND R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada > 0
        ) RO
        OUTER APPLY
        (
            SELECT SUM(R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada) AS ReservaTotalAlmacen
            FROM dbo.StockReserva R WITH (UPDLOCK, HOLDLOCK)
            WHERE R.IdProducto = D.IdProducto
              AND R.IdAlmacen = @IdAlmacen
              AND R.Estado IN ('ACTIVA','PARCIALMENTE_CONSUMIDA')
              AND R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada > 0
        ) RT
        CROSS APPLY
        (
            SELECT
                CASE
                    WHEN D.Cantidad - D.CantidadDespachada <= 0 THEN 0
                    WHEN ISNULL(S.StockActual, 0) <= 0 THEN 0
                    WHEN D.Cantidad - D.CantidadDespachada <
                        CASE
                            WHEN ISNULL(S.StockActual, 0) < ISNULL(RO.ReservaOci, 0) + CASE WHEN ISNULL(S.StockActual, 0) - ISNULL(RT.ReservaTotalAlmacen, 0) > 0 THEN ISNULL(S.StockActual, 0) - ISNULL(RT.ReservaTotalAlmacen, 0) ELSE 0 END
                            THEN ISNULL(S.StockActual, 0)
                            ELSE ISNULL(RO.ReservaOci, 0) + CASE WHEN ISNULL(S.StockActual, 0) - ISNULL(RT.ReservaTotalAlmacen, 0) > 0 THEN ISNULL(S.StockActual, 0) - ISNULL(RT.ReservaTotalAlmacen, 0) ELSE 0 END
                        END
                        THEN D.Cantidad - D.CantidadDespachada
                    ELSE
                        CASE
                            WHEN ISNULL(S.StockActual, 0) < ISNULL(RO.ReservaOci, 0) + CASE WHEN ISNULL(S.StockActual, 0) - ISNULL(RT.ReservaTotalAlmacen, 0) > 0 THEN ISNULL(S.StockActual, 0) - ISNULL(RT.ReservaTotalAlmacen, 0) ELSE 0 END
                            THEN ISNULL(S.StockActual, 0)
                            ELSE ISNULL(RO.ReservaOci, 0) + CASE WHEN ISNULL(S.StockActual, 0) - ISNULL(RT.ReservaTotalAlmacen, 0) > 0 THEN ISNULL(S.StockActual, 0) - ISNULL(RT.ReservaTotalAlmacen, 0) ELSE 0 END
                        END
                END AS CantidadMaxima
        ) MAXIMO
        WHERE T.CantidadDespachar > MAXIMO.CantidadMaxima
        ORDER BY T.IdOrdenCompraInternaDetalle;

        IF @CodigoProductoInvalido IS NOT NULL
        BEGIN
            SET @MensajeValidacion = CONCAT(
                'La cantidad maxima permitida para ', @CodigoProductoInvalido,
                ' es ', CONVERT(VARCHAR(30), CAST(@CantidadMaxima AS DECIMAL(18,2))), '.');
            THROW 51001, @MensajeValidacion, 1;
        END;

        DECLARE @SerieGuia VARCHAR(20), @Correlativo BIGINT, @NumeroCorrelativo VARCHAR(30);
        EXEC dbo.USP_SEG_SERIE_TOMAR_SIGUIENTE
            @CodigoTipoDocumento='GUIA_SALIDA', @Serie=@SerieGuia OUTPUT,
            @Correlativo=@Correlativo OUTPUT, @Numero=@NumeroCorrelativo OUTPUT;
        SET @NumeroGuia = CONCAT(@SerieGuia, '-', @NumeroCorrelativo);

        INSERT INTO dbo.GuiasInternas
        (
            NumeroGuia, IdOrdenCompraInterna, IdAlmacen, FechaEmision,
            RucEmisor, EmpresaEmisora, RucDestino, EmpresaDestino,
            UsuarioEmisor, UsuarioAutorizador, Observacion, Estado
        )
        SELECT
            @NumeroGuia, O.IdOrdenCompraInterna, @IdAlmacen, @FechaEmision,
            ISNULL(E.Ruc, ''), ISNULL(E.Nombre, ''), ISNULL(C.NumeroDocumento, ''), O.NombreCliente,
            @UsuarioEmisor, @UsuarioAutorizador, ISNULL(@Observacion, ''), 'Emitida'
        FROM dbo.OrdenesCompraInterna O
        INNER JOIN dbo.Clientes C ON C.IdCliente = O.IdCliente
        OUTER APPLY
        (
            SELECT TOP (1) Ruc, Nombre FROM dbo.Empresas
            WHERE Estado = 1 ORDER BY EsPredeterminada DESC, IdEmpresa
        ) E
        WHERE O.IdOrdenCompraInterna = @IdOrdenCompraInterna;

        DECLARE @IdGuiaInterna INT = SCOPE_IDENTITY();

        INSERT INTO dbo.GuiaInternaDetalle
        (
            IdGuiaInterna, IdOrdenCompraInternaDetalle, IdProducto, CodigoProducto, NombreProducto,
            IdUnidadMedida, NombreUnidad, CantidadRequerida, CantidadDespachada,
            StockAnterior, PrecioUnitario, Observacion
        )
        SELECT
            @IdGuiaInterna, D.IdOrdenCompraInternaDetalle, D.IdProducto, D.CodigoProducto, D.NombreProducto,
            P.IdUnidadMedida, U.NombreUnidad, D.Cantidad, T.CantidadDespachar,
            S.StockActual, D.PrecioUnitario, ISNULL(T.Observacion, '')
        FROM @Detalles T
        INNER JOIN dbo.OrdenCompraInternaDetalle D ON D.IdOrdenCompraInternaDetalle = T.IdOrdenCompraInternaDetalle
        INNER JOIN dbo.Productos P ON P.IdProducto = D.IdProducto
        INNER JOIN dbo.UnidadesMedida U ON U.IdUnidadMedida = P.IdUnidadMedida
        INNER JOIN dbo.StockProductosAlmacen S ON S.IdProducto = D.IdProducto AND S.IdAlmacen = @IdAlmacen;

        DECLARE @IdDetalle INT, @IdProducto INT, @Cantidad DECIMAL(18,2), @StockAnterior DECIMAL(18,2);
        DECLARE detalle_cursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT T.IdOrdenCompraInternaDetalle, D.IdProducto, T.CantidadDespachar, S.StockActual
            FROM @Detalles T
            INNER JOIN dbo.OrdenCompraInternaDetalle D ON D.IdOrdenCompraInternaDetalle = T.IdOrdenCompraInternaDetalle
            INNER JOIN dbo.StockProductosAlmacen S ON S.IdProducto = D.IdProducto AND S.IdAlmacen = @IdAlmacen;
        OPEN detalle_cursor;
        FETCH NEXT FROM detalle_cursor INTO @IdDetalle, @IdProducto, @Cantidad, @StockAnterior;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            DECLARE @PendienteConsumoReserva DECIMAL(18,2) = @Cantidad;
            DECLARE @IdStockReserva BIGINT, @ReservaPendiente DECIMAL(18,2), @ConsumirReserva DECIMAL(18,2), @EstadoAnterior VARCHAR(30), @EstadoNuevo VARCHAR(30);

            DECLARE reserva_cursor CURSOR LOCAL FAST_FORWARD FOR
                SELECT IdStockReserva, CantidadReservada - CantidadConsumida - CantidadLiberada AS Pendiente, Estado
                FROM dbo.StockReserva WITH (UPDLOCK, HOLDLOCK)
                WHERE IdOrdenCompraInternaDetalle = @IdDetalle
                  AND (IdAlmacen = @IdAlmacen OR IdAlmacen IS NULL)
                  AND Estado IN ('ACTIVA','PARCIALMENTE_CONSUMIDA')
                  AND CantidadReservada - CantidadConsumida - CantidadLiberada > 0
                ORDER BY FechaReserva, IdStockReserva;

            OPEN reserva_cursor;
            FETCH NEXT FROM reserva_cursor INTO @IdStockReserva, @ReservaPendiente, @EstadoAnterior;
            WHILE @@FETCH_STATUS = 0 AND @PendienteConsumoReserva > 0
            BEGIN
                SET @ConsumirReserva = CASE WHEN @ReservaPendiente >= @PendienteConsumoReserva THEN @PendienteConsumoReserva ELSE @ReservaPendiente END;

                UPDATE dbo.StockReserva
                SET CantidadConsumida = CantidadConsumida + @ConsumirReserva,
                    Estado = CASE
                        WHEN CantidadReservada - (CantidadConsumida + @ConsumirReserva) - CantidadLiberada <= 0 THEN 'CONSUMIDA'
                        ELSE 'PARCIALMENTE_CONSUMIDA'
                    END,
                    FechaActualizacion = SYSDATETIME(),
                    UsuarioActualizacion = @UsuarioEmisor
                WHERE IdStockReserva = @IdStockReserva;

                SELECT @EstadoNuevo = Estado FROM dbo.StockReserva WHERE IdStockReserva = @IdStockReserva;

                INSERT dbo.StockReservaMovimiento
                (
                    IdStockReserva, TipoMovimiento, Cantidad, EstadoAnterior, EstadoNuevo,
                    DocumentoReferencia, UsuarioMovimiento, Observacion
                )
                VALUES
                (
                    @IdStockReserva, 'CONSUMIDA', @ConsumirReserva, @EstadoAnterior, @EstadoNuevo,
                    @NumeroGuia, @UsuarioEmisor, 'Consumo automatico por Guia Interna.'
                );

                SET @PendienteConsumoReserva -= @ConsumirReserva;
                FETCH NEXT FROM reserva_cursor INTO @IdStockReserva, @ReservaPendiente, @EstadoAnterior;
            END;
            CLOSE reserva_cursor;
            DEALLOCATE reserva_cursor;

            UPDATE dbo.StockProductosAlmacen
            SET StockActual = StockActual - @Cantidad, FechaActualizacion = GETDATE()
            WHERE IdProducto = @IdProducto AND IdAlmacen = @IdAlmacen;

            UPDATE dbo.StockProductos
            SET StockActual = StockActual - @Cantidad, FechaActualizacion = GETDATE()
            WHERE IdProducto = @IdProducto;

            UPDATE dbo.OrdenCompraInternaDetalle
            SET CantidadDespachada = CantidadDespachada + @Cantidad
            WHERE IdOrdenCompraInternaDetalle = @IdDetalle;

            INSERT INTO dbo.KardexProductos
            (
                TipoMovimiento, IdIngresoManualStock, IdGuiaInterna, IdProducto, IdAlmacen,
                StockAnterior, Cantidad, StockResultante, UsuarioResponsable, FechaMovimiento, Observacion
            )
            VALUES
            (
                'GUIA_INTERNA_SALIDA', NULL, @IdGuiaInterna, @IdProducto, @IdAlmacen,
                @StockAnterior, @Cantidad, @StockAnterior - @Cantidad, @UsuarioEmisor, GETDATE(),
                CONCAT('Salida por ', @NumeroGuia, ' - OCI ', @IdOrdenCompraInterna)
            );

            FETCH NEXT FROM detalle_cursor INTO @IdDetalle, @IdProducto, @Cantidad, @StockAnterior;
        END;
        CLOSE detalle_cursor;
        DEALLOCATE detalle_cursor;

        UPDATE dbo.OrdenesCompraInterna
        SET TieneGuiaSalida = 1,
            Estado = CASE WHEN EXISTS
            (
                SELECT 1 FROM dbo.OrdenCompraInternaDetalle
                WHERE IdOrdenCompraInterna = @IdOrdenCompraInterna AND CantidadDespachada < Cantidad
            ) THEN 'Parcial' ELSE 'Entregado' END
        WHERE IdOrdenCompraInterna = @IdOrdenCompraInterna;

        COMMIT TRANSACTION;
        SET @Mensaje = CONCAT('Guia interna ', @NumeroGuia, ' emitida correctamente.');
    END TRY
    BEGIN CATCH
        IF CURSOR_STATUS('local', 'reserva_cursor') >= 0 CLOSE reserva_cursor;
        IF CURSOR_STATUS('local', 'reserva_cursor') >= -1 DEALLOCATE reserva_cursor;
        IF CURSOR_STATUS('local', 'detalle_cursor') >= 0 CLOSE detalle_cursor;
        IF CURSOR_STATUS('local', 'detalle_cursor') >= -1 DEALLOCATE detalle_cursor;
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Mensaje = ERROR_MESSAGE();
        SET @NumeroGuia = '';
    END CATCH;
END;
GO

PRINT 'Flujo principal de reservas de stock configurado correctamente.';
GO
