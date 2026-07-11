USE [CorexProdDB];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/*
    RECONSTRUCCION SEGURA DE RESERVAS PARA OT EXISTENTES

    Orden obligatorio de ejecucion:
      1) 01_Modulo_Stock_Reservas.sql
      2) 02_Flujo_Principal_Reservas.sql
      3) Este archivo

    Este archivo NO modifica procedimientos almacenados.
    Solo reconstruye reservas faltantes para las OT ya creadas.
    Es idempotente: al volver a ejecutarlo solo reserva el saldo faltante.
*/

IF OBJECT_ID('dbo.StockReserva', 'U') IS NULL
    THROW 51000, 'Falta dbo.StockReserva. Ejecute primero 01_Modulo_Stock_Reservas.sql.', 1;

IF OBJECT_ID('dbo.StockReservaMovimiento', 'U') IS NULL
    THROW 51000, 'Falta dbo.StockReservaMovimiento. Ejecute primero 01_Modulo_Stock_Reservas.sql.', 1;

IF OBJECT_ID('dbo.USP_PRO_OT_VALIDAR_INSUMOS', 'P') IS NULL
    THROW 51000, 'No existe USP_PRO_OT_VALIDAR_INSUMOS.', 1;

IF OBJECT_ID('dbo.USP_PRO_OT_CREAR', 'P') IS NULL
    THROW 51000, 'No existe USP_PRO_OT_CREAR.', 1;

IF OBJECT_DEFINITION(OBJECT_ID('dbo.USP_PRO_OT_VALIDAR_INSUMOS')) NOT LIKE '%StockReserva%'
    THROW 51000, 'USP_PRO_OT_VALIDAR_INSUMOS aun no considera reservas. Ejecute 02_Flujo_Principal_Reservas.sql.', 1;

IF OBJECT_DEFINITION(OBJECT_ID('dbo.USP_PRO_OT_CREAR')) NOT LIKE '%CantidadReservarStock%'
    THROW 51000, 'USP_PRO_OT_CREAR aun no crea reservas. Ejecute 02_Flujo_Principal_Reservas.sql.', 1;
GO

BEGIN TRY
    BEGIN TRAN;

    DECLARE
        @IdOrdenTrabajo INT,
        @NumeroOT VARCHAR(30),
        @IdOrdenCompraInterna INT,
        @Usuario VARCHAR(100);

    DECLARE cur_ot CURSOR LOCAL FAST_FORWARD FOR
        SELECT
            OT.IdOrdenTrabajo,
            OT.NumeroOT,
            OT.IdOrdenCompraInterna,
            COALESCE(NULLIF(LTRIM(RTRIM(U.NombreUsuario)), ''), 'Sistema')
        FROM dbo.OrdenTrabajo OT
        LEFT JOIN dbo.Usuarios U ON U.IdUsuario = OT.IdUsuarioCreacion
        INNER JOIN dbo.OrdenesCompraInterna OCI
            ON OCI.IdOrdenCompraInterna = OT.IdOrdenCompraInterna
        WHERE OT.IdOrdenCompraInterna IS NOT NULL
          AND UPPER(REPLACE(LTRIM(RTRIM(ISNULL(OT.Estado, ''))), ' ', '_'))
              NOT IN ('ANULADA', 'ANULADO')
          AND UPPER(REPLACE(LTRIM(RTRIM(ISNULL(OCI.Estado, ''))), ' ', '_'))
              NOT IN ('ANULADO', 'ANULADA', 'ENTREGADO', 'ENTREGADA')
        ORDER BY OT.FechaRegistro, OT.IdOrdenTrabajo;

    OPEN cur_ot;
    FETCH NEXT FROM cur_ot
        INTO @IdOrdenTrabajo, @NumeroOT, @IdOrdenCompraInterna, @Usuario;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        DECLARE
            @IdDetalleOT INT,
            @IdOrdenCompraInternaDetalle INT,
            @IdProducto INT,
            @CantidadPendienteOci DECIMAL(18,2),
            @CantidadReservadaOci DECIMAL(18,2),
            @CantidadPorReservar DECIMAL(18,2);

        DECLARE cur_detalle CURSOR LOCAL FAST_FORWARD FOR
            SELECT
                OTD.IdDetalleOT,
                OTD.IdOrdenCompraInternaDetalle,
                OTD.IdProducto,
                CONVERT(DECIMAL(18,2),
                    CASE
                        WHEN OCD.Cantidad - ISNULL(OCD.CantidadDespachada, 0) > 0
                            THEN OCD.Cantidad - ISNULL(OCD.CantidadDespachada, 0)
                        ELSE 0
                    END)
            FROM dbo.OrdenTrabajoDetalle OTD
            INNER JOIN dbo.OrdenCompraInternaDetalle OCD
                ON OCD.IdOrdenCompraInternaDetalle = OTD.IdOrdenCompraInternaDetalle
               AND OCD.IdOrdenCompraInterna = @IdOrdenCompraInterna
            WHERE OTD.IdOrdenTrabajo = @IdOrdenTrabajo
              AND OTD.IdOrdenCompraInternaDetalle IS NOT NULL
              AND UPPER(REPLACE(LTRIM(RTRIM(ISNULL(OTD.Estado, ''))), ' ', '_'))
                  NOT IN ('ANULADO', 'ANULADA')
            ORDER BY OTD.IdDetalleOT;

        OPEN cur_detalle;
        FETCH NEXT FROM cur_detalle
            INTO @IdDetalleOT, @IdOrdenCompraInternaDetalle, @IdProducto, @CantidadPendienteOci;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            SELECT @CantidadReservadaOci =
                ISNULL(SUM(R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada), 0)
            FROM dbo.StockReserva R WITH (UPDLOCK, HOLDLOCK)
            WHERE R.IdOrdenCompraInternaDetalle = @IdOrdenCompraInternaDetalle
              AND R.IdProducto = @IdProducto
              AND R.Estado IN ('ACTIVA', 'PARCIALMENTE_CONSUMIDA')
              AND R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada > 0;

            SET @CantidadPorReservar =
                CASE
                    WHEN @CantidadPendienteOci - ISNULL(@CantidadReservadaOci, 0) > 0
                        THEN @CantidadPendienteOci - ISNULL(@CantidadReservadaOci, 0)
                    ELSE 0
                END;

            WHILE @CantidadPorReservar > 0
            BEGIN
                DECLARE
                    @IdAlmacen INT = NULL,
                    @StockLibreAlmacen DECIMAL(18,2) = 0,
                    @CantidadReserva DECIMAL(18,2) = 0,
                    @IdStockReserva BIGINT;

                SELECT TOP (1)
                    @IdAlmacen = SPA.IdAlmacen,
                    @StockLibreAlmacen = CONVERT(DECIMAL(18,2),
                        CASE
                            WHEN SPA.StockActual - ISNULL(RA.Reservado, 0) > 0
                                THEN SPA.StockActual - ISNULL(RA.Reservado, 0)
                            ELSE 0
                        END)
                FROM dbo.StockProductosAlmacen SPA WITH (UPDLOCK, HOLDLOCK)
                LEFT JOIN dbo.Almacenes A ON A.IdAlmacen = SPA.IdAlmacen
                OUTER APPLY
                (
                    SELECT SUM(
                        R.CantidadReservada
                        - R.CantidadConsumida
                        - R.CantidadLiberada) AS Reservado
                    FROM dbo.StockReserva R WITH (UPDLOCK, HOLDLOCK)
                    WHERE R.IdProducto = SPA.IdProducto
                      AND R.IdAlmacen = SPA.IdAlmacen
                      AND R.Estado IN ('ACTIVA', 'PARCIALMENTE_CONSUMIDA')
                      AND R.CantidadReservada
                          - R.CantidadConsumida
                          - R.CantidadLiberada > 0
                ) RA
                WHERE SPA.IdProducto = @IdProducto
                  AND SPA.StockActual - ISNULL(RA.Reservado, 0) > 0
                ORDER BY
                    CASE WHEN UPPER(LTRIM(RTRIM(ISNULL(A.NombreAlmacen, ''))))
                              = 'ALMACEN PRINCIPAL' THEN 0 ELSE 1 END,
                    SPA.IdAlmacen;

                IF @IdAlmacen IS NULL OR @StockLibreAlmacen <= 0
                    BREAK;

                SET @CantidadReserva =
                    CASE
                        WHEN @StockLibreAlmacen >= @CantidadPorReservar
                            THEN @CantidadPorReservar
                        ELSE @StockLibreAlmacen
                    END;

                INSERT dbo.StockReserva
                (
                    IdOrdenCompraInterna,
                    IdOrdenCompraInternaDetalle,
                    IdProducto,
                    IdAlmacen,
                    IdOrdenTrabajo,
                    IdDetalleOT,
                    CantidadReservada,
                    CantidadConsumida,
                    CantidadLiberada,
                    TipoOrigen,
                    Estado,
                    UsuarioReserva,
                    UsuarioActualizacion,
                    Observacion
                )
                VALUES
                (
                    @IdOrdenCompraInterna,
                    @IdOrdenCompraInternaDetalle,
                    @IdProducto,
                    @IdAlmacen,
                    @IdOrdenTrabajo,
                    @IdDetalleOT,
                    @CantidadReserva,
                    0,
                    0,
                    'AJUSTE',
                    'ACTIVA',
                    @Usuario,
                    @Usuario,
                    CONCAT('Reserva reconstruida para ', @NumeroOT)
                );

                SET @IdStockReserva = CONVERT(BIGINT, SCOPE_IDENTITY());

                INSERT dbo.StockReservaMovimiento
                (
                    IdStockReserva,
                    TipoMovimiento,
                    Cantidad,
                    EstadoAnterior,
                    EstadoNuevo,
                    DocumentoReferencia,
                    UsuarioMovimiento,
                    Observacion
                )
                VALUES
                (
                    @IdStockReserva,
                    'CREADA',
                    @CantidadReserva,
                    NULL,
                    'ACTIVA',
                    @NumeroOT,
                    @Usuario,
                    'Reconstruccion de reserva para una OT creada antes de instalar el modulo.'
                );

                SET @CantidadPorReservar -= @CantidadReserva;
            END;

            FETCH NEXT FROM cur_detalle
                INTO @IdDetalleOT, @IdOrdenCompraInternaDetalle, @IdProducto, @CantidadPendienteOci;
        END;

        CLOSE cur_detalle;
        DEALLOCATE cur_detalle;

        FETCH NEXT FROM cur_ot
            INTO @IdOrdenTrabajo, @NumeroOT, @IdOrdenCompraInterna, @Usuario;
    END;

    CLOSE cur_ot;
    DEALLOCATE cur_ot;

    COMMIT;
END TRY
BEGIN CATCH
    IF CURSOR_STATUS('local', 'cur_detalle') >= 0
        CLOSE cur_detalle;
    IF CURSOR_STATUS('local', 'cur_detalle') >= -1
        DEALLOCATE cur_detalle;
    IF CURSOR_STATUS('local', 'cur_ot') >= 0
        CLOSE cur_ot;
    IF CURSOR_STATUS('local', 'cur_ot') >= -1
        DEALLOCATE cur_ot;

    IF @@TRANCOUNT > 0
        ROLLBACK;

    THROW;
END CATCH;
GO

SELECT
    OCI.NumeroOci,
    P.Codigo,
    P.NombreProducto,
    CAST(SUM(R.CantidadReservada) AS DECIMAL(18,2)) AS CantidadReservada,
    CAST(SUM(R.CantidadConsumida) AS DECIMAL(18,2)) AS CantidadConsumida,
    CAST(SUM(R.CantidadLiberada) AS DECIMAL(18,2)) AS CantidadLiberada,
    CAST(SUM(
        R.CantidadReservada
        - R.CantidadConsumida
        - R.CantidadLiberada) AS DECIMAL(18,2)) AS ReservaPendiente
FROM dbo.StockReserva R
INNER JOIN dbo.OrdenesCompraInterna OCI
    ON OCI.IdOrdenCompraInterna = R.IdOrdenCompraInterna
INNER JOIN dbo.Productos P
    ON P.IdProducto = R.IdProducto
WHERE R.Estado IN ('ACTIVA', 'PARCIALMENTE_CONSUMIDA')
GROUP BY OCI.NumeroOci, P.Codigo, P.NombreProducto
HAVING SUM(
    R.CantidadReservada
    - R.CantidadConsumida
    - R.CantidadLiberada) > 0
ORDER BY OCI.NumeroOci, P.Codigo;
GO

PRINT 'Reservas de stock existentes reconstruidas correctamente.';
GO
