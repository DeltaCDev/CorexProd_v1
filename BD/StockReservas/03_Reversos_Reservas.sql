SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_STOCK_RESERVA_ANULAR_SCOPE
    @IdOrdenCompraInterna INT = NULL,
    @IdOrdenTrabajo INT = NULL,
    @DocumentoReferencia VARCHAR(100) = NULL,
    @Usuario VARCHAR(100),
    @Observacion VARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @Usuario = NULLIF(LTRIM(RTRIM(ISNULL(@Usuario, ''))), '');
    SET @DocumentoReferencia = NULLIF(LTRIM(RTRIM(ISNULL(@DocumentoReferencia, ''))), '');
    SET @Observacion = NULLIF(LTRIM(RTRIM(ISNULL(@Observacion, ''))), '');

    IF @Usuario IS NULL
        SET @Usuario = 'Sistema';

    IF @IdOrdenCompraInterna IS NULL AND @IdOrdenTrabajo IS NULL
        THROW 51000, 'Debe indicar la OCI o la OT para anular reservas.', 1;

    DECLARE @ReservasAnuladas TABLE
    (
        IdStockReserva BIGINT NOT NULL,
        CantidadAnulada DECIMAL(18,2) NOT NULL,
        EstadoAnterior VARCHAR(30) NOT NULL,
        EstadoNuevo VARCHAR(30) NOT NULL
    );

    UPDATE R
    SET CantidadLiberada = R.CantidadLiberada + (R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada),
        Estado = 'ANULADA',
        FechaActualizacion = SYSDATETIME(),
        UsuarioActualizacion = @Usuario
    OUTPUT
        INSERTED.IdStockReserva,
        DELETED.CantidadReservada - DELETED.CantidadConsumida - DELETED.CantidadLiberada,
        DELETED.Estado,
        INSERTED.Estado
    INTO @ReservasAnuladas(IdStockReserva, CantidadAnulada, EstadoAnterior, EstadoNuevo)
    FROM dbo.StockReserva R WITH (UPDLOCK, HOLDLOCK)
    WHERE R.Estado IN ('ACTIVA','PARCIALMENTE_CONSUMIDA')
      AND R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada > 0
      AND (@IdOrdenCompraInterna IS NULL OR R.IdOrdenCompraInterna = @IdOrdenCompraInterna)
      AND (@IdOrdenTrabajo IS NULL OR R.IdOrdenTrabajo = @IdOrdenTrabajo);

    INSERT dbo.StockReservaMovimiento
    (
        IdStockReserva, TipoMovimiento, Cantidad, EstadoAnterior, EstadoNuevo,
        DocumentoReferencia, UsuarioMovimiento, Observacion
    )
    SELECT
        IdStockReserva, 'ANULADA', CantidadAnulada, EstadoAnterior, EstadoNuevo,
        @DocumentoReferencia, @Usuario, ISNULL(@Observacion, 'Reserva anulada por reverso del documento.')
    FROM @ReservasAnuladas
    WHERE CantidadAnulada > 0;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_STOCK_RESERVA_REVERTIR_CONSUMO_GUIA
    @NumeroGuia VARCHAR(30),
    @Usuario VARCHAR(100),
    @Motivo VARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @NumeroGuia = NULLIF(LTRIM(RTRIM(ISNULL(@NumeroGuia, ''))), '');
    SET @Usuario = NULLIF(LTRIM(RTRIM(ISNULL(@Usuario, ''))), '');
    SET @Motivo = NULLIF(LTRIM(RTRIM(ISNULL(@Motivo, ''))), '');

    IF @NumeroGuia IS NULL
        THROW 51000, 'Debe indicar el numero de guia para revertir consumo de reservas.', 1;
    IF @Usuario IS NULL
        SET @Usuario = 'Sistema';

    DECLARE @Consumos TABLE
    (
        IdStockReserva BIGINT NOT NULL PRIMARY KEY,
        CantidadRevertir DECIMAL(18,2) NOT NULL,
        EstadoAnterior VARCHAR(30) NULL,
        EstadoNuevo VARCHAR(30) NULL
    );

    INSERT @Consumos(IdStockReserva, CantidadRevertir, EstadoAnterior)
    SELECT M.IdStockReserva, SUM(M.Cantidad), R.Estado
    FROM dbo.StockReservaMovimiento M
    INNER JOIN dbo.StockReserva R WITH (UPDLOCK, HOLDLOCK)
        ON R.IdStockReserva = M.IdStockReserva
    WHERE M.TipoMovimiento = 'CONSUMIDA'
      AND M.DocumentoReferencia = @NumeroGuia
    GROUP BY M.IdStockReserva, R.Estado;

    IF EXISTS
    (
        SELECT 1
        FROM @Consumos C
        INNER JOIN dbo.StockReserva R ON R.IdStockReserva = C.IdStockReserva
        WHERE C.CantidadRevertir > R.CantidadConsumida
    )
        THROW 51000, 'El consumo de reservas de la guia no puede revertirse porque excede lo consumido.', 1;

    UPDATE R
    SET CantidadConsumida = R.CantidadConsumida - C.CantidadRevertir,
        Estado = CASE
            WHEN R.CantidadConsumida - C.CantidadRevertir <= 0 THEN 'ACTIVA'
            WHEN R.CantidadReservada - (R.CantidadConsumida - C.CantidadRevertir) - R.CantidadLiberada > 0 THEN 'PARCIALMENTE_CONSUMIDA'
            WHEN R.CantidadLiberada >= R.CantidadReservada THEN 'LIBERADA'
            ELSE 'CONSUMIDA'
        END,
        FechaActualizacion = SYSDATETIME(),
        UsuarioActualizacion = @Usuario
    FROM dbo.StockReserva R
    INNER JOIN @Consumos C ON C.IdStockReserva = R.IdStockReserva;

    UPDATE C
    SET EstadoNuevo = R.Estado
    FROM @Consumos C
    INNER JOIN dbo.StockReserva R ON R.IdStockReserva = C.IdStockReserva;

    INSERT dbo.StockReservaMovimiento
    (
        IdStockReserva, TipoMovimiento, Cantidad, EstadoAnterior, EstadoNuevo,
        DocumentoReferencia, UsuarioMovimiento, Observacion
    )
    SELECT
        IdStockReserva, 'ANULADA', CantidadRevertir, EstadoAnterior, EstadoNuevo,
        @NumeroGuia, @Usuario, CONCAT('Reversion de consumo por anulacion de guia. ', ISNULL(@Motivo, ''))
    FROM @Consumos
    WHERE CantidadRevertir > 0;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_STOCK_RESERVA_LIBERAR_EXCEDENTE_OCI
    @IdOrdenCompraInterna INT,
    @Usuario VARCHAR(100),
    @DocumentoReferencia VARCHAR(100) = NULL,
    @Observacion VARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @Usuario = NULLIF(LTRIM(RTRIM(ISNULL(@Usuario, ''))), '');
    SET @DocumentoReferencia = NULLIF(LTRIM(RTRIM(ISNULL(@DocumentoReferencia, ''))), '');
    SET @Observacion = NULLIF(LTRIM(RTRIM(ISNULL(@Observacion, ''))), '');

    IF @Usuario IS NULL
        SET @Usuario = 'Sistema';

    DECLARE @Liberaciones TABLE
    (
        IdStockReserva BIGINT NOT NULL,
        CantidadLiberada DECIMAL(18,2) NOT NULL,
        EstadoAnterior VARCHAR(30) NOT NULL,
        EstadoNuevo VARCHAR(30) NOT NULL
    );

    ;WITH ReservaActiva AS
    (
        SELECT
            R.IdStockReserva,
            R.IdOrdenCompraInternaDetalle,
            R.FechaReserva,
            R.CantidadReservada,
            R.CantidadConsumida,
            R.CantidadLiberada,
            R.Estado,
            R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada AS PendienteReserva,
            CASE WHEN D.Cantidad - D.CantidadDespachada > 0 THEN D.Cantidad - D.CantidadDespachada ELSE 0 END AS PendienteOci
        FROM dbo.StockReserva R WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN dbo.OrdenCompraInternaDetalle D WITH (UPDLOCK, HOLDLOCK)
            ON D.IdOrdenCompraInternaDetalle = R.IdOrdenCompraInternaDetalle
        WHERE R.IdOrdenCompraInterna = @IdOrdenCompraInterna
          AND R.Estado IN ('ACTIVA','PARCIALMENTE_CONSUMIDA')
          AND R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada > 0
    ),
    ReservaAcumulada AS
    (
        SELECT
            *,
            SUM(PendienteReserva) OVER
            (
                PARTITION BY IdOrdenCompraInternaDetalle
                ORDER BY FechaReserva DESC, IdStockReserva DESC
                ROWS UNBOUNDED PRECEDING
            ) AS AcumuladoDesc
        FROM ReservaActiva
    ),
    Calculo AS
    (
        SELECT
            IdStockReserva,
            CASE
                WHEN AcumuladoDesc <=
                    CASE
                        WHEN SUM(PendienteReserva) OVER (PARTITION BY IdOrdenCompraInternaDetalle) - PendienteOci > 0
                        THEN SUM(PendienteReserva) OVER (PARTITION BY IdOrdenCompraInternaDetalle) - PendienteOci
                        ELSE 0
                    END
                    THEN PendienteReserva
                WHEN AcumuladoDesc - PendienteReserva <
                    CASE
                        WHEN SUM(PendienteReserva) OVER (PARTITION BY IdOrdenCompraInternaDetalle) - PendienteOci > 0
                        THEN SUM(PendienteReserva) OVER (PARTITION BY IdOrdenCompraInternaDetalle) - PendienteOci
                        ELSE 0
                    END
                    THEN
                        CASE
                            WHEN SUM(PendienteReserva) OVER (PARTITION BY IdOrdenCompraInternaDetalle) - PendienteOci > 0
                            THEN SUM(PendienteReserva) OVER (PARTITION BY IdOrdenCompraInternaDetalle) - PendienteOci
                            ELSE 0
                        END - (AcumuladoDesc - PendienteReserva)
                ELSE 0
            END AS CantidadLiberar
        FROM ReservaAcumulada
    )
    UPDATE R
    SET CantidadLiberada = R.CantidadLiberada + C.CantidadLiberar,
        Estado = CASE
            WHEN R.CantidadReservada - R.CantidadConsumida - (R.CantidadLiberada + C.CantidadLiberar) <= 0 THEN 'LIBERADA'
            WHEN R.CantidadConsumida > 0 THEN 'PARCIALMENTE_CONSUMIDA'
            ELSE 'ACTIVA'
        END,
        FechaActualizacion = SYSDATETIME(),
        UsuarioActualizacion = @Usuario
    OUTPUT
        INSERTED.IdStockReserva,
        INSERTED.CantidadLiberada - DELETED.CantidadLiberada,
        DELETED.Estado,
        INSERTED.Estado
    INTO @Liberaciones(IdStockReserva, CantidadLiberada, EstadoAnterior, EstadoNuevo)
    FROM dbo.StockReserva R
    INNER JOIN Calculo C ON C.IdStockReserva = R.IdStockReserva
    WHERE C.CantidadLiberar > 0;

    INSERT dbo.StockReservaMovimiento
    (
        IdStockReserva, TipoMovimiento, Cantidad, EstadoAnterior, EstadoNuevo,
        DocumentoReferencia, UsuarioMovimiento, Observacion
    )
    SELECT
        IdStockReserva, 'LIBERADA', CantidadLiberada, EstadoAnterior, EstadoNuevo,
        @DocumentoReferencia, @Usuario, ISNULL(@Observacion, 'Liberacion automatica de excedente de OCI.')
    FROM @Liberaciones
    WHERE CantidadLiberada > 0;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_VEN_GUIA_INTERNA_ANULAR
    @IdGuiaInterna INT,@Usuario VARCHAR(80),@Motivo VARCHAR(500),@Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON; SET @Motivo=LTRIM(RTRIM(ISNULL(@Motivo,'')));
    IF @Motivo='' BEGIN SET @Mensaje='Debe ingresar el motivo de anulacion.'; RETURN; END;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @Estado VARCHAR(20),@IdAlmacen INT,@IdOci INT,@NumeroGuia VARCHAR(30);
        SELECT @Estado=Estado,@IdAlmacen=IdAlmacen,@IdOci=IdOrdenCompraInterna,@NumeroGuia=NumeroGuia
        FROM dbo.GuiasInternas WITH(UPDLOCK,HOLDLOCK) WHERE IdGuiaInterna=@IdGuiaInterna;
        IF @Estado IS NULL THROW 51200,'No se encontro la guia interna.',1;
        IF @Estado='Anulada' THROW 51201,'La guia interna ya se encuentra anulada.',1;

        DECLARE @IdProducto INT,@Cantidad DECIMAL(18,2),@IdDetalleOci INT,@Anterior DECIMAL(18,2);
        DECLARE c CURSOR LOCAL FAST_FORWARD FOR
            SELECT D.IdProducto,D.CantidadDespachada,D.IdOrdenCompraInternaDetalle,S.StockActual
            FROM dbo.GuiaInternaDetalle D
            INNER JOIN dbo.StockProductosAlmacen S WITH(UPDLOCK,HOLDLOCK) ON S.IdProducto=D.IdProducto AND S.IdAlmacen=@IdAlmacen
            WHERE D.IdGuiaInterna=@IdGuiaInterna;
        OPEN c; FETCH NEXT FROM c INTO @IdProducto,@Cantidad,@IdDetalleOci,@Anterior;
        WHILE @@FETCH_STATUS=0
        BEGIN
            UPDATE dbo.StockProductosAlmacen SET StockActual=StockActual+@Cantidad,FechaActualizacion=GETDATE() WHERE IdProducto=@IdProducto AND IdAlmacen=@IdAlmacen;
            UPDATE dbo.StockProductos SET StockActual=StockActual+@Cantidad,FechaActualizacion=GETDATE() WHERE IdProducto=@IdProducto;
            IF @IdDetalleOci IS NOT NULL UPDATE dbo.OrdenCompraInternaDetalle SET CantidadDespachada=CantidadDespachada-@Cantidad WHERE IdOrdenCompraInternaDetalle=@IdDetalleOci;
            INSERT dbo.KardexProductos(TipoMovimiento,IdIngresoManualStock,IdGuiaInterna,IdProducto,IdAlmacen,StockAnterior,Cantidad,StockResultante,UsuarioResponsable,FechaMovimiento,Observacion)
            VALUES('ANULACION_GUIA_INTERNA',NULL,@IdGuiaInterna,@IdProducto,@IdAlmacen,@Anterior,@Cantidad,@Anterior+@Cantidad,@Usuario,GETDATE(),CONCAT('Anulacion de ',@NumeroGuia,': ',@Motivo));
            FETCH NEXT FROM c INTO @IdProducto,@Cantidad,@IdDetalleOci,@Anterior;
        END
        CLOSE c; DEALLOCATE c;

        EXEC dbo.USP_ALM_STOCK_RESERVA_REVERTIR_CONSUMO_GUIA
            @NumeroGuia=@NumeroGuia,
            @Usuario=@Usuario,
            @Motivo=@Motivo;

        UPDATE dbo.GuiasInternas SET Estado='Anulada',UsuarioAnulacion=@Usuario,FechaAnulacion=GETDATE(),MotivoAnulacion=@Motivo WHERE IdGuiaInterna=@IdGuiaInterna;
        IF @IdOci IS NOT NULL
            UPDATE dbo.OrdenesCompraInterna SET
                TieneGuiaSalida=CASE WHEN EXISTS(SELECT 1 FROM dbo.GuiasInternas WHERE IdOrdenCompraInterna=@IdOci AND Estado='Emitida') THEN 1 ELSE 0 END,
                Estado=CASE
                    WHEN Estado='Anulado' THEN 'Anulado'
                    WHEN NOT EXISTS(SELECT 1 FROM dbo.OrdenCompraInternaDetalle WHERE IdOrdenCompraInterna=@IdOci AND CantidadDespachada<Cantidad) THEN 'Entregado'
                    WHEN EXISTS(SELECT 1 FROM dbo.OrdenCompraInternaDetalle WHERE IdOrdenCompraInterna=@IdOci AND CantidadDespachada>0) THEN 'Parcial'
                    WHEN TieneOrdenTrabajo=1 THEN 'En proceso'
                    ELSE 'Emitida'
                    END
            WHERE IdOrdenCompraInterna=@IdOci;
        COMMIT; SET @Mensaje='Guia interna anulada correctamente. El stock y las reservas fueron restituidos.';
    END TRY
    BEGIN CATCH
        IF CURSOR_STATUS('local','c')>=0 CLOSE c; IF CURSOR_STATUS('local','c')>=-1 DEALLOCATE c;
        IF @@TRANCOUNT>0 ROLLBACK; SET @Mensaje=ERROR_MESSAGE();
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_VEN_OCI_ANULAR
    @IdOrdenCompraInterna INT,
    @MotivoAnulacion VARCHAR(500),
    @UsuarioAnulacion VARCHAR(80),
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @MotivoAnulacion = LTRIM(RTRIM(ISNULL(@MotivoAnulacion, '')));
    SET @UsuarioAnulacion = LTRIM(RTRIM(ISNULL(@UsuarioAnulacion, '')));

    IF @MotivoAnulacion = ''
    BEGIN
        SET @Mensaje = 'Debe ingresar el motivo de anulacion.';
        RETURN;
    END;

    IF @UsuarioAnulacion = ''
    BEGIN
        SET @Mensaje = 'No se pudo identificar al usuario que anula la OCI.';
        RETURN;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE dbo.OrdenesCompraInterna WITH (UPDLOCK)
        SET Estado = 'Anulado',
            MotivoAnulacion = @MotivoAnulacion,
            UsuarioAnulacion = @UsuarioAnulacion,
            FechaAnulacion = GETDATE()
        WHERE IdOrdenCompraInterna = @IdOrdenCompraInterna
          AND Estado <> 'Anulado'
          AND NOT EXISTS
          (
              SELECT 1
              FROM dbo.OrdenTrabajo OT
              WHERE OT.IdOrdenCompraInterna = @IdOrdenCompraInterna
                AND UPPER(OT.Estado) <> 'ANULADA'
          )
          AND NOT EXISTS
          (
              SELECT 1
              FROM dbo.GuiasInternas G
              WHERE G.IdOrdenCompraInterna = @IdOrdenCompraInterna
                AND G.Estado <> 'Anulado'
          );

        IF @@ROWCOUNT = 1
        BEGIN
            EXEC dbo.USP_ALM_STOCK_RESERVA_ANULAR_SCOPE
                @IdOrdenCompraInterna=@IdOrdenCompraInterna,
                @IdOrdenTrabajo=NULL,
                @DocumentoReferencia=NULL,
                @Usuario=@UsuarioAnulacion,
                @Observacion=@MotivoAnulacion;

            UPDATE P
            SET TieneOrdenCompraInterna = CAST(CASE WHEN EXISTS
                (
                    SELECT 1
                    FROM dbo.OrdenesCompraInterna O
                    WHERE O.IdProforma = P.IdProforma
                      AND UPPER(O.Estado) <> 'ANULADO'
                ) THEN 1 ELSE 0 END AS BIT),
                Estado = CASE
                    WHEN P.Estado = 'Anulado' THEN 'Anulado'
                    WHEN EXISTS
                    (
                        SELECT 1
                        FROM dbo.OrdenesCompraInterna O
                        WHERE O.IdProforma = P.IdProforma
                          AND UPPER(O.Estado) <> 'ANULADO'
                    ) THEN 'Registrado'
                    ELSE 'Emitido'
                END
            FROM dbo.Proformas P
            WHERE P.IdProforma =
            (
                SELECT O.IdProforma
                FROM dbo.OrdenesCompraInterna O
                WHERE O.IdOrdenCompraInterna = @IdOrdenCompraInterna
            );

            COMMIT;
            SET @Mensaje = 'OCI anulada correctamente. Las reservas activas fueron anuladas.';
            RETURN;
        END;

        ROLLBACK;

        IF NOT EXISTS (SELECT 1 FROM dbo.OrdenesCompraInterna WHERE IdOrdenCompraInterna = @IdOrdenCompraInterna)
            SET @Mensaje = 'No se encontro la OCI seleccionada.';
        ELSE IF EXISTS
        (
            SELECT 1 FROM dbo.OrdenesCompraInterna
            WHERE IdOrdenCompraInterna = @IdOrdenCompraInterna AND Estado = 'Anulado'
        )
            SET @Mensaje = 'La OCI ya se encuentra anulada.';
        ELSE IF EXISTS
        (
            SELECT 1
            FROM dbo.GuiasInternas
            WHERE IdOrdenCompraInterna = @IdOrdenCompraInterna
              AND Estado <> 'Anulado'
        )
            SET @Mensaje = 'No se puede anular la OCI porque tiene una Guia Interna emitida. Primero debe anular la guia.';
        ELSE IF EXISTS
        (
            SELECT 1
            FROM dbo.OrdenTrabajo
            WHERE IdOrdenCompraInterna = @IdOrdenCompraInterna
              AND UPPER(Estado) <> 'ANULADA'
        )
            SET @Mensaje = 'No se puede anular la OCI porque tiene una Orden de Trabajo emitida.';
        ELSE
            SET @Mensaje = 'No se puede anular la OCI porque tiene una Orden de Trabajo emitida.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_ANULAR
    @IdOrdenTrabajo INT,
    @ConvertirProcesoAMerma BIT = 0,
    @IdUsuarioSesion INT = NULL,
    @MotivoAnulacion VARCHAR(500) = '',
    @UsuarioAnulacion VARCHAR(80) = ''
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        DECLARE @IdOrdenCompraInterna INT;
        DECLARE @Estado VARCHAR(20);
        DECLARE @NumeroOT VARCHAR(40);
        DECLARE @EsProceso BIT = 0;
        DECLARE @IdUsuarioMerma INT;

        SET @MotivoAnulacion = LTRIM(RTRIM(ISNULL(@MotivoAnulacion, '')));
        SET @UsuarioAnulacion = LTRIM(RTRIM(ISNULL(@UsuarioAnulacion, '')));

        IF @MotivoAnulacion = ''
            THROW 51000, 'Ingrese el motivo de anulacion.', 1;

        IF @UsuarioAnulacion = ''
            SET @UsuarioAnulacion = 'Sistema';

        SELECT
            @IdOrdenCompraInterna = IdOrdenCompraInterna,
            @Estado = UPPER(Estado),
            @NumeroOT = NumeroOT,
            @IdUsuarioMerma = IdUsuarioCreacion
        FROM dbo.OrdenTrabajo WITH (UPDLOCK, HOLDLOCK)
        WHERE IdOrdenTrabajo = @IdOrdenTrabajo;

        IF @IdOrdenCompraInterna IS NULL
            THROW 51000, 'La OT seleccionada no existe.', 1;

        IF @Estado = 'ANULADA'
            THROW 51000, 'La OT seleccionada ya se encuentra anulada.', 1;

        SET @IdUsuarioMerma = ISNULL(NULLIF(@IdUsuarioSesion, 0), @IdUsuarioMerma);

        IF @Estado NOT IN ('PENDIENTE', 'EMITIDA', 'EN_PROCESO', 'PROCESO')
            THROW 51000, 'Solo se puede anular una OT en estado Pendiente o En Proceso sin productos terminados.', 1;

        SET @EsProceso = CASE WHEN @Estado IN ('EN_PROCESO', 'PROCESO') THEN 1 ELSE 0 END;

        IF @EsProceso = 0 AND EXISTS
        (
            SELECT 1
            FROM dbo.OrdenTrabajoDetalle
            WHERE IdOrdenTrabajo = @IdOrdenTrabajo
              AND
              (
                  CantidadLanzada > 0
                  OR CantidadProducida > 0
                  OR CantidadAplicada > 0
                  OR Estado NOT IN ('PENDIENTE', 'ANULADO')
              )
        )
            THROW 51000, 'La OT ya tiene movimiento de produccion y no puede anularse.', 1;

        IF @EsProceso = 0
           AND
           (
               EXISTS (SELECT 1 FROM dbo.OrdenTrabajoTransferencia WHERE IdOrdenTrabajo = @IdOrdenTrabajo)
               OR EXISTS (SELECT 1 FROM dbo.OrdenTrabajoTerminacion WHERE IdOrdenTrabajo = @IdOrdenTrabajo)
               OR EXISTS (SELECT 1 FROM dbo.OrdenTrabajoMerma WHERE IdOrdenTrabajo = @IdOrdenTrabajo)
               OR EXISTS (SELECT 1 FROM dbo.OrdenTrabajoConsumoInsumo WHERE IdOrdenTrabajo = @IdOrdenTrabajo)
           )
            THROW 51000, 'La OT ya tiene movimientos registrados y no puede anularse.', 1;

        IF @EsProceso = 1
        BEGIN
            IF @ConvertirProcesoAMerma = 0
                THROW 51000, 'Confirme la conversion de productos en proceso a merma para anular la OT.', 1;

            IF EXISTS
            (
                SELECT 1
                FROM dbo.OrdenTrabajoDetalle
                WHERE IdOrdenTrabajo = @IdOrdenTrabajo
                  AND (Estado = 'TERMINADO' OR CantidadProducida > 0)
            )
                THROW 51000, 'La OT tiene productos terminados y no puede anularse.', 1;

            IF EXISTS (SELECT 1 FROM dbo.OrdenTrabajoTerminacion WHERE IdOrdenTrabajo = @IdOrdenTrabajo)
                THROW 51000, 'La OT tiene productos ingresados como terminados y no puede anularse.', 1;

            INSERT dbo.OrdenTrabajoMerma
            (
                IdOrdenTrabajo,
                IdDetalleOT,
                IdDetalleArea,
                Cantidad,
                Motivo,
                Observacion,
                IdUsuarioSesion,
                IdUsuarioAutoriza
            )
            SELECT
                A.IdOrdenTrabajo,
                A.IdDetalleOT,
                A.IdDetalleArea,
                A.CantidadPendiente,
                N'ANULACION DE OT EN PROCESO',
                N'Saldo en proceso convertido a merma por anulacion de OT.',
                @IdUsuarioMerma,
                @IdUsuarioMerma
            FROM dbo.OrdenTrabajoDetalleArea A
            WHERE A.IdOrdenTrabajo = @IdOrdenTrabajo
              AND A.CantidadPendiente > 0
              AND A.Estado NOT IN ('FINALIZADA', 'BLOQUEADA', 'ANULADA');

            UPDATE dbo.OrdenTrabajoDetalleArea
            SET CantidadMerma = CantidadRecibida - CantidadEnviada,
                Estado = 'FINALIZADA',
                FechaFin = SYSDATETIME()
            WHERE IdOrdenTrabajo = @IdOrdenTrabajo
              AND CantidadPendiente > 0
              AND Estado NOT IN ('FINALIZADA', 'BLOQUEADA', 'ANULADA');
        END
        ELSE
        BEGIN
            UPDATE dbo.OrdenTrabajoDetalleArea
            SET Estado = 'ANULADA'
            WHERE IdOrdenTrabajo = @IdOrdenTrabajo;
        END

        EXEC dbo.USP_ALM_STOCK_RESERVA_ANULAR_SCOPE
            @IdOrdenCompraInterna=NULL,
            @IdOrdenTrabajo=@IdOrdenTrabajo,
            @DocumentoReferencia=@NumeroOT,
            @Usuario=@UsuarioAnulacion,
            @Observacion=@MotivoAnulacion;

        UPDATE dbo.OrdenTrabajoDetalle
        SET Estado = 'ANULADO',
            CantidadLanzada = 0,
            CantidadPendiente = 0
        WHERE IdOrdenTrabajo = @IdOrdenTrabajo;

        UPDATE dbo.OrdenTrabajo
        SET Estado = 'ANULADA',
            MotivoAnulacion = @MotivoAnulacion,
            UsuarioAnulacion = @UsuarioAnulacion,
            FechaAnulacion = GETDATE()
        WHERE IdOrdenTrabajo = @IdOrdenTrabajo;

        UPDATE O
        SET TieneOrdenTrabajo = CASE
                WHEN EXISTS
                (
                    SELECT 1
                    FROM dbo.OrdenTrabajo OT
                    WHERE OT.IdOrdenCompraInterna = O.IdOrdenCompraInterna
                      AND UPPER(OT.Estado) <> 'ANULADA'
                ) THEN 1 ELSE 0 END,
            Estado = CASE
                WHEN O.Estado IN ('Anulada', 'Anulado') THEN 'Anulado'
                WHEN NOT EXISTS
                (
                    SELECT 1
                    FROM dbo.OrdenCompraInternaDetalle D
                    WHERE D.IdOrdenCompraInterna = O.IdOrdenCompraInterna
                      AND D.CantidadDespachada < D.Cantidad
                ) THEN 'Entregado'
                WHEN EXISTS
                (
                    SELECT 1
                    FROM dbo.OrdenCompraInternaDetalle D
                    WHERE D.IdOrdenCompraInterna = O.IdOrdenCompraInterna
                      AND D.CantidadDespachada > 0
                ) THEN 'Parcial'
                WHEN EXISTS
                (
                    SELECT 1
                    FROM dbo.OrdenTrabajo OT
                    WHERE OT.IdOrdenCompraInterna = O.IdOrdenCompraInterna
                      AND UPPER(OT.Estado) <> 'ANULADA'
                ) THEN 'PROCESO'
                ELSE 'Emitida'
            END
        FROM dbo.OrdenesCompraInterna O
        WHERE O.IdOrdenCompraInterna = @IdOrdenCompraInterna;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

PRINT 'Reversos de reservas de stock configurados correctamente.';
GO
