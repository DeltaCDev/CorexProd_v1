USE CorexProdDB;
GO

/*
    Etapa 14.4.2.10-B
    @TotalPagado y @SaldoPendiente permanecen por compatibilidad y representan
    exclusivamente la moneda del pago anulado. No son una suma PEN + USD.
*/
CREATE OR ALTER PROCEDURE dbo.USP_TES_TARJETA_ANULAR_PAGO
    @IdTarjetaPago INT,
    @Usuario VARCHAR(80),
    @Motivo VARCHAR(500),
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT,
    @TotalPagado DECIMAL(18,2) OUTPUT,
    @SaldoPendiente DECIMAL(18,2) OUTPUT,
    @EstadoPeriodo VARCHAR(30) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @Resultado = 0;
    SET @Mensaje = '';
    SET @Usuario = LTRIM(RTRIM(ISNULL(@Usuario, '')));
    SET @Motivo = LTRIM(RTRIM(ISNULL(@Motivo, '')));

    IF @Motivo = ''
    BEGIN
        SET @Mensaje = 'Debe indicar el motivo de anulacion del pago.';
        RETURN;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @IdTarjetaPeriodo INT;
        DECLARE @IdTarjetaCredito INT;
        DECLARE @MonedaPago VARCHAR(10);
        DECLARE @ImportePago DECIMAL(18,2);
        DECLARE @ImportePEN DECIMAL(18,2);
        DECLARE @ImporteUSD DECIMAL(18,2);
        DECLARE @TotalPagadoPEN DECIMAL(18,2);
        DECLARE @TotalPagadoUSD DECIMAL(18,2);
        DECLARE @SaldoPEN DECIMAL(18,2);
        DECLARE @SaldoUSD DECIMAL(18,2);
        DECLARE @EstadoPEN VARCHAR(30);
        DECLARE @EstadoUSD VARCHAR(30);
        DECLARE @EstadoActualPeriodo VARCHAR(30);

        /* Se conserva moneda e importe antes de la anulación lógica. */
        SELECT
            @IdTarjetaPeriodo = PG.IdTarjetaPeriodo,
            @MonedaPago = PG.Moneda,
            @ImportePago = PG.Importe
        FROM dbo.TesTarjetaCreditoPagos AS PG WITH (UPDLOCK, HOLDLOCK)
        WHERE PG.IdTarjetaPago = @IdTarjetaPago
          AND PG.Estado = 'ACTIVO';

        IF @IdTarjetaPeriodo IS NULL
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'El pago seleccionado no existe o ya se encuentra anulado.';
            RETURN;
        END;

        SELECT
            @IdTarjetaCredito = P.IdTarjetaCredito,
            @ImportePEN = P.ImportePEN,
            @ImporteUSD = P.ImporteUSD,
            @EstadoActualPeriodo = P.Estado
        FROM dbo.TesTarjetaCreditoPeriodos AS P WITH (UPDLOCK, HOLDLOCK)
        WHERE P.IdTarjetaPeriodo = @IdTarjetaPeriodo;

        IF @EstadoActualPeriodo = 'ANULADA'
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'No se puede anular pagos de un periodo anulado.';
            RETURN;
        END;

        UPDATE dbo.TesTarjetaCreditoPagos
        SET Estado = 'ANULADO',
            FechaAnulacion = GETDATE(),
            UsuarioAnulacion = @Usuario,
            MotivoAnulacion = @Motivo
        WHERE IdTarjetaPago = @IdTarjetaPago;

        SELECT
            @TotalPagadoPEN = ISNULL(SUM(CASE WHEN Estado = 'ACTIVO' AND Moneda = 'PEN' THEN Importe END), 0),
            @TotalPagadoUSD = ISNULL(SUM(CASE WHEN Estado = 'ACTIVO' AND Moneda = 'USD' THEN Importe END), 0)
        FROM dbo.TesTarjetaCreditoPagos
        WHERE IdTarjetaPeriodo = @IdTarjetaPeriodo;

        SET @SaldoPEN = CASE WHEN @ImportePEN IS NULL THEN NULL ELSE @ImportePEN - @TotalPagadoPEN END;
        SET @SaldoUSD = CASE WHEN @ImporteUSD IS NULL THEN NULL ELSE @ImporteUSD - @TotalPagadoUSD END;

        SET @EstadoPEN = CASE
            WHEN @ImportePEN IS NULL THEN NULL
            WHEN @SaldoPEN <= 0 THEN 'CANCELADA'
            WHEN @TotalPagadoPEN > 0 THEN 'PARCIAL'
            ELSE 'PENDIENTE'
        END;
        SET @EstadoUSD = CASE
            WHEN @ImporteUSD IS NULL THEN NULL
            WHEN @SaldoUSD <= 0 THEN 'CANCELADA'
            WHEN @TotalPagadoUSD > 0 THEN 'PARCIAL'
            ELSE 'PENDIENTE'
        END;

        /* Monedas NULL no participan; PARCIAL prevalece sobre PENDIENTE. */
        SET @EstadoPeriodo = CASE
            WHEN @ImportePEN IS NULL AND @ImporteUSD IS NULL THEN 'PENDIENTE'
            WHEN @EstadoPEN = 'PARCIAL' OR @EstadoUSD = 'PARCIAL' THEN 'PARCIAL'
            WHEN @EstadoPEN = 'PENDIENTE' OR @EstadoUSD = 'PENDIENTE' THEN 'PENDIENTE'
            WHEN @EstadoPEN = 'CANCELADA' OR @EstadoUSD = 'CANCELADA' THEN 'CANCELADA'
            ELSE 'PENDIENTE'
        END;

        UPDATE dbo.TesTarjetaCreditoPeriodos
        SET TotalPagadoPEN = @TotalPagadoPEN,
            TotalPagadoUSD = @TotalPagadoUSD,
            SaldoPendientePEN = @SaldoPEN,
            SaldoPendienteUSD = @SaldoUSD,
            EstadoPEN = @EstadoPEN,
            EstadoUSD = @EstadoUSD,
            Estado = @EstadoPeriodo
        WHERE IdTarjetaPeriodo = @IdTarjetaPeriodo;

        COMMIT TRANSACTION;

        SET @Resultado = 1;
        SET @Mensaje = 'Pago de tarjeta anulado correctamente.';
        SET @TotalPagado = CASE WHEN @MonedaPago = 'PEN' THEN @TotalPagadoPEN ELSE @TotalPagadoUSD END;
        SET @SaldoPendiente = CASE WHEN @MonedaPago = 'PEN' THEN @SaldoPEN ELSE @SaldoUSD END;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;

        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH;

    SELECT
        @Resultado AS Resultado,
        @Mensaje AS Mensaje,
        @IdTarjetaPago AS IdTarjetaPago,
        @TotalPagado AS TotalPagado,
        @SaldoPendiente AS SaldoPendiente,
        @EstadoPeriodo AS EstadoPeriodo;
END;
GO
