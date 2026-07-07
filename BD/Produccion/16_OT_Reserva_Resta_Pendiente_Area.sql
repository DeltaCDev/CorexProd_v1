SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

/*
    FIX: Reserva en proceso descuenta del pendiente operativo del area

    Criterio funcional:
    - Si un area tiene 80 pendientes y se reservan 15 como stock en proceso,
      la reserva debe quedarse en la misma area.
    - No debe transferirse automaticamente a la siguiente area.
    - El saldo disponible para transferir manualmente debe ser 65.
    - La merma se sigue registrando manualmente sobre el saldo que quede en el area.

    Nota tecnica:
    - La columna calculada OrdenTrabajoDetalleArea.CantidadPendiente conserva el pendiente fisico:
      Recibido - Enviado - Merma.
    - Para la operacion se devuelve y valida el pendiente disponible:
      Recibido - Enviado - Merma - Reservas activas.
*/

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_OBTENER @IdOrdenTrabajo INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        O.*,
        OCI.NumeroOci,
        OCI.OrdenCompraCliente,
        U.NombreUsuario,
        ISNULL(UA.NombreUsuario, U.NombreUsuario) AS UsuarioAutoriza
    FROM dbo.OrdenTrabajo O
    JOIN dbo.OrdenesCompraInterna OCI ON OCI.IdOrdenCompraInterna = O.IdOrdenCompraInterna
    JOIN dbo.Usuarios U ON U.IdUsuario = O.IdUsuarioCreacion
    LEFT JOIN dbo.Usuarios UA ON UA.IdUsuario = O.IdUsuarioAutorizaCreacion
    WHERE O.IdOrdenTrabajo = @IdOrdenTrabajo;

    SELECT *
    FROM dbo.OrdenTrabajoDetalle
    WHERE IdOrdenTrabajo = @IdOrdenTrabajo
    ORDER BY IdDetalleOT;

    SELECT
        A.IdDetalleArea,
        A.IdOrdenTrabajo,
        A.IdDetalleOT,
        A.IdAreaProduccion,
        A.CodigoArea,
        A.NombreArea,
        A.OrdenSecuencia,
        A.EsInicio,
        A.EsTermino,
        A.ManejaMerma,
        A.PermiteReservarStockProceso,
        A.ModoEnvio,
        A.CantidadRecibida,
        A.CantidadEnviada,
        A.CantidadMerma,
        CONVERT(DECIMAL(18,2),
            CASE
                WHEN A.CantidadRecibida - A.CantidadEnviada - A.CantidadMerma - ISNULL(R.CantidadReservada, 0) > 0
                    THEN A.CantidadRecibida - A.CantidadEnviada - A.CantidadMerma - ISNULL(R.CantidadReservada, 0)
                ELSE 0
            END) AS CantidadPendiente,
        A.Estado,
        A.FechaInicio,
        A.FechaFin,
        D.CodigoProducto,
        D.NombreProducto
    FROM dbo.OrdenTrabajoDetalleArea A
    JOIN dbo.OrdenTrabajoDetalle D ON D.IdDetalleOT = A.IdDetalleOT
    OUTER APPLY
    (
        SELECT SUM(SPR.Cantidad - SPR.CantidadAplicada) AS CantidadReservada
        FROM dbo.StockProcesoReserva SPR
        WHERE SPR.IdDetalleArea = A.IdDetalleArea
          AND SPR.Estado IN ('DISPONIBLE','RESERVADO')
          AND SPR.Cantidad - SPR.CantidadAplicada > 0
    ) R
    WHERE A.IdOrdenTrabajo = @IdOrdenTrabajo
    ORDER BY A.OrdenSecuencia, D.NombreProducto;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_TRANSFERIR
    @IdOrdenTrabajo INT,
    @IdAreaOrigen INT,
    @IdUsuarioSesion INT,
    @IdUsuarioAutoriza INT,
    @Observacion NVARCHAR(500),
    @Detalles dbo.TipoOTTransferencia READONLY,
    @IdOperacion BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        IF NOT EXISTS(SELECT 1 FROM @Detalles)
            THROW 51000,'Seleccione al menos un producto.',1;

        IF NOT EXISTS(SELECT 1 FROM dbo.Usuarios WHERE IdUsuario=@IdUsuarioSesion AND Estado=1)
           OR NOT EXISTS(SELECT 1 FROM dbo.Usuarios WHERE IdUsuario=@IdUsuarioAutoriza AND Estado=1)
            THROW 51000,'El usuario de sesion o autorizador no es valido.',1;

        DECLARE @OrdenOrigen INT = (SELECT OrdenSecuencia FROM dbo.AreaProduccion WHERE IdAreaProduccion=@IdAreaOrigen);
        DECLARE @IdAreaDestino INT = (SELECT TOP(1) IdAreaProduccion FROM dbo.AreaProduccion WHERE Activo=1 AND OrdenSecuencia>@OrdenOrigen ORDER BY OrdenSecuencia);

        IF @IdAreaDestino IS NULL
            THROW 51000,'El area seleccionada no tiene una siguiente area.',1;

        DECLARE @Error NVARCHAR(2048);

        ;WITH AreaOrigen AS
        (
            SELECT
                A.*,
                D.CodigoProducto,
                D.Estado AS EstadoDetalle,
                CONVERT(DECIMAL(18,2),
                    CASE
                        WHEN A.CantidadRecibida - A.CantidadEnviada - A.CantidadMerma - ISNULL(R.CantidadReservada, 0) > 0
                            THEN A.CantidadRecibida - A.CantidadEnviada - A.CantidadMerma - ISNULL(R.CantidadReservada, 0)
                        ELSE 0
                    END) AS CantidadDisponible
            FROM dbo.OrdenTrabajoDetalleArea A WITH(UPDLOCK,HOLDLOCK)
            JOIN dbo.OrdenTrabajoDetalle D ON D.IdDetalleOT = A.IdDetalleOT
            OUTER APPLY
            (
                SELECT SUM(SPR.Cantidad - SPR.CantidadAplicada) AS CantidadReservada
                FROM dbo.StockProcesoReserva SPR WITH(UPDLOCK,HOLDLOCK)
                WHERE SPR.IdDetalleArea = A.IdDetalleArea
                  AND SPR.Estado IN ('DISPONIBLE','RESERVADO')
                  AND SPR.Cantidad - SPR.CantidadAplicada > 0
            ) R
            WHERE A.IdOrdenTrabajo=@IdOrdenTrabajo
              AND A.IdAreaProduccion=@IdAreaOrigen
        )
        SELECT TOP(1) @Error = CONCAT('Producto ', ISNULL(A.CodigoProducto, CONVERT(VARCHAR(20), X.IdDetalleOT)), ': ',
            CASE
                WHEN A.IdDetalleArea IS NULL THEN 'no pertenece a la OT o no esta en el area de origen'
                WHEN A.EstadoDetalle IN ('TERMINADO','ANULADO') OR A.Estado IN ('FINALIZADA','BLOQUEADA','ANULADA') THEN 'esta finalizado o bloqueado'
                WHEN X.Cantidad <= 0 THEN 'la cantidad debe ser mayor a cero'
                WHEN X.Cantidad > A.CantidadDisponible THEN 'la cantidad supera el pendiente disponible descontando reservas'
                WHEN A.ModoEnvio='UNICO' AND (A.CantidadEnviada>0 OR X.Cantidad<>A.CantidadDisponible) THEN 'el modo UNICO exige un solo envio por todo el saldo disponible'
                WHEN Dest.IdDetalleArea IS NULL THEN 'no tiene configurada el area de destino'
            END)
        FROM @Detalles X
        LEFT JOIN AreaOrigen A ON A.IdDetalleOT = X.IdDetalleOT
        LEFT JOIN dbo.OrdenTrabajoDetalleArea Dest ON Dest.IdDetalleOT = X.IdDetalleOT AND Dest.IdAreaProduccion = @IdAreaDestino
        WHERE A.IdDetalleArea IS NULL
           OR A.EstadoDetalle IN ('TERMINADO','ANULADO')
           OR A.Estado IN ('FINALIZADA','BLOQUEADA','ANULADA')
           OR X.Cantidad <= 0
           OR X.Cantidad > A.CantidadDisponible
           OR (A.ModoEnvio='UNICO' AND (A.CantidadEnviada>0 OR X.Cantidad<>A.CantidadDisponible))
           OR Dest.IdDetalleArea IS NULL;

        IF @Error IS NOT NULL
            THROW 51000,@Error,1;

        INSERT dbo.OrdenTrabajoTransferencia(IdOrdenTrabajo,IdAreaOrigen,IdAreaDestino,IdUsuarioSesion,IdUsuarioAutoriza,Observacion)
        VALUES(@IdOrdenTrabajo,@IdAreaOrigen,@IdAreaDestino,@IdUsuarioSesion,@IdUsuarioAutoriza,ISNULL(@Observacion,N''));

        SET @IdOperacion = SCOPE_IDENTITY();

        INSERT dbo.OrdenTrabajoTransferenciaDetalle(IdOperacionTransferencia,IdDetalleOT,IdDetalleAreaOrigen,IdDetalleAreaDestino,CantidadEnviada,IdUsuarioSesion,IdUsuarioAutoriza)
        SELECT @IdOperacion, X.IdDetalleOT, O.IdDetalleArea, D.IdDetalleArea, X.Cantidad, @IdUsuarioSesion, @IdUsuarioAutoriza
        FROM @Detalles X
        JOIN dbo.OrdenTrabajoDetalleArea O ON O.IdDetalleOT = X.IdDetalleOT AND O.IdAreaProduccion = @IdAreaOrigen
        JOIN dbo.OrdenTrabajoDetalleArea D ON D.IdDetalleOT = X.IdDetalleOT AND D.IdAreaProduccion = @IdAreaDestino;

        UPDATE A
        SET CantidadEnviada = CantidadEnviada + X.Cantidad,
            Estado = CASE WHEN CantidadRecibida - (CantidadEnviada + X.Cantidad) - CantidadMerma <= 0 THEN 'FINALIZADA' ELSE 'PARCIAL' END,
            FechaFin = CASE WHEN CantidadRecibida - (CantidadEnviada + X.Cantidad) - CantidadMerma <= 0 THEN SYSDATETIME() ELSE NULL END
        FROM dbo.OrdenTrabajoDetalleArea A
        JOIN @Detalles X ON X.IdDetalleOT = A.IdDetalleOT
        WHERE A.IdAreaProduccion = @IdAreaOrigen;

        UPDATE A
        SET CantidadRecibida = CantidadRecibida + X.Cantidad,
            Estado = 'EN_PROCESO',
            FechaInicio = COALESCE(FechaInicio, SYSDATETIME())
        FROM dbo.OrdenTrabajoDetalleArea A
        JOIN @Detalles X ON X.IdDetalleOT = A.IdDetalleOT
        WHERE A.IdAreaProduccion = @IdAreaDestino;

        UPDATE O
        SET Estado = CASE
            WHEN NOT EXISTS(SELECT 1 FROM dbo.OrdenTrabajoDetalle WHERE IdOrdenTrabajo=O.IdOrdenTrabajo AND Estado <> 'TERMINADO') THEN 'TERMINADA'
            WHEN EXISTS(SELECT 1 FROM dbo.OrdenTrabajoDetalle WHERE IdOrdenTrabajo=O.IdOrdenTrabajo AND Estado = 'TERMINADO') THEN 'PARCIAL'
            ELSE 'EN_PROCESO'
        END
        FROM dbo.OrdenTrabajo O
        WHERE O.IdOrdenTrabajo = @IdOrdenTrabajo;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO