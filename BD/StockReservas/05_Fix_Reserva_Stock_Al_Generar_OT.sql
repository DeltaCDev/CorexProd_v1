SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/*
    Corrige instalaciones donde un script anterior de Produccion volvio a
    publicar USP_PRO_OT_VALIDAR_INSUMOS y USP_PRO_OT_CREAR sin considerar
    las reservas activas de otras ordenes de compra.

    Ejecutar una sola vez sobre CorexProdDB. El script es idempotente.
*/

IF OBJECT_ID('dbo.StockReserva', 'U') IS NULL
    THROW 51000, 'No existe dbo.StockReserva. Ejecute primero 01_Modulo_Stock_Reservas.sql.', 1;

IF OBJECT_ID('dbo.USP_ALM_STOCK_RESERVA_CREAR', 'P') IS NULL
    THROW 51000, 'No existe USP_ALM_STOCK_RESERVA_CREAR. Ejecute primero 01_Modulo_Stock_Reservas.sql.', 1;
GO

/* 1. Corregir la validacion previa de OT si fue sobrescrita por una version antigua. */
DECLARE @DefValidacion NVARCHAR(MAX);
DECLARE @StockAntiguo NVARCHAR(MAX) =
    N'OUTER APPLY (SELECT SUM(S.StockActual) AS StockActual FROM dbo.StockProductosAlmacen S WHERE S.IdProducto = D.IdProducto) SP';
DECLARE @StockConReservas NVARCHAR(MAX) = N'OUTER APPLY
        (
            SELECT SUM(
                CASE
                    WHEN S.StockActual - ISNULL(RA.Reservado, 0) > 0
                        THEN S.StockActual - ISNULL(RA.Reservado, 0)
                    ELSE 0
                END) AS StockActual
            FROM dbo.StockProductosAlmacen S
            OUTER APPLY
            (
                SELECT SUM(R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada) AS Reservado
                FROM dbo.StockReserva R
                WHERE R.IdProducto = S.IdProducto
                  AND R.IdAlmacen = S.IdAlmacen
                  AND R.Estado IN (''ACTIVA'', ''PARCIALMENTE_CONSUMIDA'')
                  AND R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada > 0
            ) RA
            WHERE S.IdProducto = D.IdProducto
        ) SP';

SELECT @DefValidacion = M.definition
FROM sys.sql_modules M
WHERE M.object_id = OBJECT_ID('dbo.USP_PRO_OT_VALIDAR_INSUMOS');

IF @DefValidacion IS NULL
    THROW 51000, 'No existe USP_PRO_OT_VALIDAR_INSUMOS.', 1;

IF @DefValidacion NOT LIKE '%StockReserva%'
BEGIN
    IF CHARINDEX(@StockAntiguo, @DefValidacion) = 0
        THROW 51000, 'No se pudo reconocer la version actual de USP_PRO_OT_VALIDAR_INSUMOS.', 1;

    SET @DefValidacion = REPLACE(@DefValidacion, @StockAntiguo, @StockConReservas);
    EXEC sys.sp_executesql @DefValidacion;
END;
GO

/* 2. Corregir la creacion de OT y reservar el stock fisico usado por la OC. */
DECLARE @DefCrear NVARCHAR(MAX);
DECLARE @StockAntiguoCrear NVARCHAR(MAX) =
    N'OUTER APPLY (SELECT SUM(S.StockActual) AS StockActual FROM dbo.StockProductosAlmacen S WHERE S.IdProducto = D.IdProducto) SP';
DECLARE @StockConReservasCrear NVARCHAR(MAX) = N'OUTER APPLY
        (
            SELECT SUM(
                CASE
                    WHEN S.StockActual - ISNULL(RA.Reservado, 0) > 0
                        THEN S.StockActual - ISNULL(RA.Reservado, 0)
                    ELSE 0
                END) AS StockActual
            FROM dbo.StockProductosAlmacen S WITH (UPDLOCK, HOLDLOCK)
            OUTER APPLY
            (
                SELECT SUM(R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada) AS Reservado
                FROM dbo.StockReserva R WITH (UPDLOCK, HOLDLOCK)
                WHERE R.IdProducto = S.IdProducto
                  AND R.IdAlmacen = S.IdAlmacen
                  AND R.Estado IN (''ACTIVA'', ''PARCIALMENTE_CONSUMIDA'')
                  AND R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada > 0
            ) RA
            WHERE S.IdProducto = D.IdProducto
        ) SP';

DECLARE @FinalAntiguo NVARCHAR(MAX) = N'        UPDATE dbo.OrdenesCompraInterna
        SET TieneOrdenTrabajo = 1,
            Estado = ''PROCESO''
        WHERE IdOrdenCompraInterna = @IdOrdenCompraInterna;

        COMMIT;';

DECLARE @FinalCorregido NVARCHAR(MAX) = N'        UPDATE dbo.OrdenesCompraInterna
        SET TieneOrdenTrabajo = 1,
            Estado = ''PROCESO''
        WHERE IdOrdenCompraInterna = @IdOrdenCompraInterna;

        DECLARE
            @RsvFixDetalleOci INT,
            @RsvFixDetalleOt INT,
            @RsvFixProducto INT,
            @RsvFixPendiente DECIMAL(18,2),
            @RsvFixReservado DECIMAL(18,2),
            @RsvFixNecesidad DECIMAL(18,2),
            @RsvFixAlmacen INT,
            @RsvFixDisponible DECIMAL(18,2),
            @RsvFixCantidad DECIMAL(18,2),
            @RsvFixId BIGINT,
            @RsvFixUsuario VARCHAR(100);

        SELECT @RsvFixUsuario = COALESCE(NULLIF(LTRIM(RTRIM(NombreUsuario)), ''''), ''Sistema'')
        FROM dbo.Usuarios
        WHERE IdUsuario = @IdUsuario;

        DECLARE cur_fix_reserva_stock CURSOR LOCAL FAST_FORWARD FOR
            SELECT
                D.IdOrdenCompraInternaDetalle,
                D.IdDetalleOT,
                D.IdProducto,
                CONVERT(DECIMAL(18,2),
                    CASE
                        WHEN OCD.Cantidad - ISNULL(OCD.CantidadDespachada, 0) > 0
                            THEN OCD.Cantidad - ISNULL(OCD.CantidadDespachada, 0)
                        ELSE 0
                    END)
            FROM dbo.OrdenTrabajoDetalle D
            INNER JOIN dbo.OrdenCompraInternaDetalle OCD
                ON OCD.IdOrdenCompraInternaDetalle = D.IdOrdenCompraInternaDetalle
            WHERE D.IdOrdenTrabajo = @IdOrdenTrabajo
              AND D.IdOrdenCompraInternaDetalle IS NOT NULL;

        OPEN cur_fix_reserva_stock;
        FETCH NEXT FROM cur_fix_reserva_stock INTO
            @RsvFixDetalleOci, @RsvFixDetalleOt, @RsvFixProducto, @RsvFixPendiente;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            SELECT @RsvFixReservado = ISNULL(SUM(
                R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada), 0)
            FROM dbo.StockReserva R WITH (UPDLOCK, HOLDLOCK)
            WHERE R.IdOrdenCompraInternaDetalle = @RsvFixDetalleOci
              AND R.Estado IN (''ACTIVA'', ''PARCIALMENTE_CONSUMIDA'')
              AND R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada > 0;

            SET @RsvFixNecesidad = CASE
                WHEN @RsvFixPendiente - @RsvFixReservado > 0
                    THEN @RsvFixPendiente - @RsvFixReservado
                ELSE 0
            END;

            WHILE @RsvFixNecesidad > 0
            BEGIN
                SET @RsvFixAlmacen = NULL;
                SET @RsvFixDisponible = 0;

                SELECT TOP (1)
                    @RsvFixAlmacen = S.IdAlmacen,
                    @RsvFixDisponible = CONVERT(DECIMAL(18,2),
                        CASE
                            WHEN S.StockActual - ISNULL(RA.Reservado, 0) > 0
                                THEN S.StockActual - ISNULL(RA.Reservado, 0)
                            ELSE 0
                        END)
                FROM dbo.StockProductosAlmacen S WITH (UPDLOCK, HOLDLOCK)
                OUTER APPLY
                (
                    SELECT SUM(R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada) AS Reservado
                    FROM dbo.StockReserva R WITH (UPDLOCK, HOLDLOCK)
                    WHERE R.IdProducto = S.IdProducto
                      AND R.IdAlmacen = S.IdAlmacen
                      AND R.Estado IN (''ACTIVA'', ''PARCIALMENTE_CONSUMIDA'')
                      AND R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada > 0
                ) RA
                LEFT JOIN dbo.Almacenes A ON A.IdAlmacen = S.IdAlmacen
                WHERE S.IdProducto = @RsvFixProducto
                  AND S.StockActual - ISNULL(RA.Reservado, 0) > 0
                ORDER BY CASE WHEN A.NombreAlmacen = ''Almacen Principal'' THEN 0 ELSE 1 END, S.IdAlmacen;

                IF @RsvFixAlmacen IS NULL OR @RsvFixDisponible <= 0
                    BREAK;

                SET @RsvFixCantidad = CASE
                    WHEN @RsvFixDisponible >= @RsvFixNecesidad THEN @RsvFixNecesidad
                    ELSE @RsvFixDisponible
                END;

                EXEC dbo.USP_ALM_STOCK_RESERVA_CREAR
                    @IdOrdenCompraInterna = @IdOrdenCompraInterna,
                    @IdOrdenCompraInternaDetalle = @RsvFixDetalleOci,
                    @IdProducto = @RsvFixProducto,
                    @IdAlmacen = @RsvFixAlmacen,
                    @IdOrdenTrabajo = @IdOrdenTrabajo,
                    @IdDetalleOT = @RsvFixDetalleOt,
                    @Cantidad = @RsvFixCantidad,
                    @TipoOrigen = ''STOCK_FISICO'',
                    @Usuario = @RsvFixUsuario,
                    @Observacion = ''Reserva automatica al generar OT'',
                    @IdStockReserva = @RsvFixId OUTPUT;

                SET @RsvFixNecesidad -= @RsvFixCantidad;
            END;

            FETCH NEXT FROM cur_fix_reserva_stock INTO
                @RsvFixDetalleOci, @RsvFixDetalleOt, @RsvFixProducto, @RsvFixPendiente;
        END;

        CLOSE cur_fix_reserva_stock;
        DEALLOCATE cur_fix_reserva_stock;

        COMMIT;';

SELECT @DefCrear = M.definition
FROM sys.sql_modules M
WHERE M.object_id = OBJECT_ID('dbo.USP_PRO_OT_CREAR');

IF @DefCrear IS NULL
    THROW 51000, 'No existe USP_PRO_OT_CREAR.', 1;

IF @DefCrear NOT LIKE '%cur_fix_reserva_stock%'
BEGIN
    IF @DefCrear NOT LIKE '%StockReserva%'
    BEGIN
        IF CHARINDEX(@StockAntiguoCrear, @DefCrear) = 0
            THROW 51000, 'No se pudo reconocer el calculo de stock de USP_PRO_OT_CREAR.', 1;
        SET @DefCrear = REPLACE(@DefCrear, @StockAntiguoCrear, @StockConReservasCrear);
    END;

    IF CHARINDEX(@FinalAntiguo, @DefCrear) = 0
        THROW 51000, 'No se pudo reconocer el cierre de USP_PRO_OT_CREAR.', 1;

    SET @DefCrear = REPLACE(@DefCrear, @FinalAntiguo, @FinalCorregido);
    EXEC sys.sp_executesql @DefCrear;
END;
GO

/* 3. Reconstruir reservas faltantes para las OT activas ya creadas, en orden cronologico. */
DECLARE @FixIdOt INT;
DECLARE @FixUsuario VARCHAR(100);

DECLARE cur_fix_ot_existente CURSOR LOCAL FAST_FORWARD FOR
    SELECT OT.IdOrdenTrabajo, COALESCE(NULLIF(LTRIM(RTRIM(U.NombreUsuario)), ''), 'Sistema')
    FROM dbo.OrdenTrabajo OT
    LEFT JOIN dbo.Usuarios U ON U.IdUsuario = OT.IdUsuarioCreacion
    WHERE OT.IdOrdenCompraInterna IS NOT NULL
      AND UPPER(REPLACE(LTRIM(RTRIM(ISNULL(OT.Estado, ''))), ' ', '_'))
          IN ('PENDIENTE', 'EMITIDA', 'EN_PROCESO', 'PROCESO', 'PARCIAL')
    ORDER BY OT.FechaRegistro, OT.IdOrdenTrabajo;

OPEN cur_fix_ot_existente;
FETCH NEXT FROM cur_fix_ot_existente INTO @FixIdOt, @FixUsuario;

WHILE @@FETCH_STATUS = 0
BEGIN
    DECLARE
        @FixOci INT,
        @FixDetalleOci INT,
        @FixDetalleOt INT,
        @FixProducto INT,
        @FixPendiente DECIMAL(18,2),
        @FixReservado DECIMAL(18,2),
        @FixNecesidad DECIMAL(18,2),
        @FixAlmacen INT,
        @FixDisponible DECIMAL(18,2),
        @FixCantidad DECIMAL(18,2),
        @FixReservaId BIGINT,
        @FixNumeroOt VARCHAR(30);

    DECLARE cur_fix_detalle_existente CURSOR LOCAL FAST_FORWARD FOR
        SELECT
            OT.IdOrdenCompraInterna,
            D.IdOrdenCompraInternaDetalle,
            D.IdDetalleOT,
            D.IdProducto,
            CONVERT(DECIMAL(18,2),
                CASE WHEN OCD.Cantidad - ISNULL(OCD.CantidadDespachada, 0) > 0
                    THEN OCD.Cantidad - ISNULL(OCD.CantidadDespachada, 0) ELSE 0 END),
            OT.NumeroOT
        FROM dbo.OrdenTrabajo OT
        INNER JOIN dbo.OrdenTrabajoDetalle D ON D.IdOrdenTrabajo = OT.IdOrdenTrabajo
        INNER JOIN dbo.OrdenCompraInternaDetalle OCD
            ON OCD.IdOrdenCompraInternaDetalle = D.IdOrdenCompraInternaDetalle
        WHERE OT.IdOrdenTrabajo = @FixIdOt
          AND D.IdOrdenCompraInternaDetalle IS NOT NULL
          AND UPPER(REPLACE(LTRIM(RTRIM(ISNULL(D.Estado, ''))), ' ', '_')) NOT IN ('ANULADO', 'ANULADA');

    OPEN cur_fix_detalle_existente;
    FETCH NEXT FROM cur_fix_detalle_existente INTO
        @FixOci, @FixDetalleOci, @FixDetalleOt, @FixProducto, @FixPendiente, @FixNumeroOt;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT @FixReservado = ISNULL(SUM(
            R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada), 0)
        FROM dbo.StockReserva R
        WHERE R.IdOrdenCompraInternaDetalle = @FixDetalleOci
          AND R.Estado IN ('ACTIVA', 'PARCIALMENTE_CONSUMIDA')
          AND R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada > 0;

        SET @FixNecesidad = CASE WHEN @FixPendiente - @FixReservado > 0
            THEN @FixPendiente - @FixReservado ELSE 0 END;

        WHILE @FixNecesidad > 0
        BEGIN
            SET @FixAlmacen = NULL;
            SET @FixDisponible = 0;

            SELECT TOP (1)
                @FixAlmacen = S.IdAlmacen,
                @FixDisponible = CONVERT(DECIMAL(18,2),
                    CASE WHEN S.StockActual - ISNULL(RA.Reservado, 0) > 0
                        THEN S.StockActual - ISNULL(RA.Reservado, 0) ELSE 0 END)
            FROM dbo.StockProductosAlmacen S
            OUTER APPLY
            (
                SELECT SUM(R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada) AS Reservado
                FROM dbo.StockReserva R
                WHERE R.IdProducto = S.IdProducto
                  AND R.IdAlmacen = S.IdAlmacen
                  AND R.Estado IN ('ACTIVA', 'PARCIALMENTE_CONSUMIDA')
                  AND R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada > 0
            ) RA
            LEFT JOIN dbo.Almacenes A ON A.IdAlmacen = S.IdAlmacen
            WHERE S.IdProducto = @FixProducto
              AND S.StockActual - ISNULL(RA.Reservado, 0) > 0
            ORDER BY CASE WHEN A.NombreAlmacen = 'Almacen Principal' THEN 0 ELSE 1 END, S.IdAlmacen;

            IF @FixAlmacen IS NULL OR @FixDisponible <= 0
                BREAK;

            SET @FixCantidad = CASE WHEN @FixDisponible >= @FixNecesidad
                THEN @FixNecesidad ELSE @FixDisponible END;

            EXEC dbo.USP_ALM_STOCK_RESERVA_CREAR
                @IdOrdenCompraInterna = @FixOci,
                @IdOrdenCompraInternaDetalle = @FixDetalleOci,
                @IdProducto = @FixProducto,
                @IdAlmacen = @FixAlmacen,
                @IdOrdenTrabajo = @FixIdOt,
                @IdDetalleOT = @FixDetalleOt,
                @Cantidad = @FixCantidad,
                @TipoOrigen = 'STOCK_FISICO',
                @Usuario = @FixUsuario,
                @Observacion = 'Reconstruccion de reserva para OT existente',
                @IdStockReserva = @FixReservaId OUTPUT;

            SET @FixNecesidad -= @FixCantidad;
        END;

        FETCH NEXT FROM cur_fix_detalle_existente INTO
            @FixOci, @FixDetalleOci, @FixDetalleOt, @FixProducto, @FixPendiente, @FixNumeroOt;
    END;

    CLOSE cur_fix_detalle_existente;
    DEALLOCATE cur_fix_detalle_existente;

    FETCH NEXT FROM cur_fix_ot_existente INTO @FixIdOt, @FixUsuario;
END;

CLOSE cur_fix_ot_existente;
DEALLOCATE cur_fix_ot_existente;
GO

PRINT 'Reserva de stock por OC corregida y reservas activas reconstruidas.';
GO
