USE CorexProdDB;
GO

/*
    Etapa 14.4.2.5-C
    Los parámetros legacy @TotalPagado y @SaldoPendiente se conservan por
    compatibilidad. En una operación multimoneda devuelven exclusivamente el
    total y saldo de @Moneda solicitada; nunca son suma de PEN y USD.
    La fuente de verdad financiera son las columnas *PEN y *USD del período.
*/
CREATE OR ALTER PROCEDURE dbo.USP_TES_TARJETA_REGISTRAR_PAGO
    @IdTarjetaPago INT OUTPUT,
    @IdTarjetaPeriodo INT,
    @FechaPago DATE,
    @IdBanco INT = NULL,
    @IdCuentaBancaria INT = NULL,
    @Importe DECIMAL(18,2),
    @Moneda VARCHAR(10),
    @NumeroOperacion VARCHAR(80) = '',
    @Observacion VARCHAR(500) = '',
    @Usuario VARCHAR(80),
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
    SET @Moneda = UPPER(LTRIM(RTRIM(ISNULL(@Moneda, ''))));

    IF @Moneda NOT IN ('PEN', 'USD') OR @Importe <= 0 OR @FechaPago IS NULL
    BEGIN
        SET @Mensaje = 'Datos de pago no validos.';
        RETURN;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @ImportePEN DECIMAL(18,2);
        DECLARE @ImporteUSD DECIMAL(18,2);
        DECLARE @TotalPagadoPEN DECIMAL(18,2);
        DECLARE @TotalPagadoUSD DECIMAL(18,2);
        DECLARE @SaldoPEN DECIMAL(18,2);
        DECLARE @SaldoUSD DECIMAL(18,2);
        DECLARE @EstadoPEN VARCHAR(30);
        DECLARE @EstadoUSD VARCHAR(30);
        DECLARE @IdTarjetaCredito INT;
        DECLARE @EstadoActual VARCHAR(30);

        SELECT
            @IdTarjetaCredito = P.IdTarjetaCredito,
            @EstadoActual = P.Estado,
            @ImportePEN = P.ImportePEN,
            @ImporteUSD = P.ImporteUSD,
            @TotalPagadoPEN = P.TotalPagadoPEN,
            @TotalPagadoUSD = P.TotalPagadoUSD
        FROM dbo.TesTarjetaCreditoPeriodos AS P WITH (UPDLOCK, HOLDLOCK)
        WHERE P.IdTarjetaPeriodo = @IdTarjetaPeriodo;

        IF @IdTarjetaCredito IS NULL
           OR @EstadoActual IN ('ABIERTO', 'ANULADA')
           OR (@Moneda = 'PEN' AND @ImportePEN IS NULL)
           OR (@Moneda = 'USD' AND @ImporteUSD IS NULL)
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'El periodo no permite pagos para la moneda indicada.';
            RETURN;
        END;

        IF @Importe > CASE WHEN @Moneda = 'PEN'
                            THEN @ImportePEN - @TotalPagadoPEN
                            ELSE @ImporteUSD - @TotalPagadoUSD END
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'El importe supera el saldo pendiente de la moneda.';
            RETURN;
        END;

        INSERT INTO dbo.TesTarjetaCreditoPagos
        (
            IdTarjetaPeriodo, FechaPago, IdBanco, IdCuentaBancaria, Importe,
            Moneda, NumeroOperacion, Observacion, Estado, UsuarioCreacion
        )
        VALUES
        (
            @IdTarjetaPeriodo, @FechaPago, @IdBanco, @IdCuentaBancaria, @Importe,
            @Moneda, @NumeroOperacion, @Observacion, 'ACTIVO', @Usuario
        );

        SET @IdTarjetaPago = SCOPE_IDENTITY();

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

        /* Las monedas NULL no participan. PARCIAL prevalece sobre PENDIENTE. */
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
            Estado = @EstadoPeriodo,
            FechaModificacion = GETDATE(),
            UsuarioModificacion = @Usuario
        WHERE IdTarjetaPeriodo = @IdTarjetaPeriodo;

        COMMIT TRANSACTION;

        SET @Resultado = 1;
        SET @Mensaje = 'Pago de tarjeta registrado correctamente.';
        SET @TotalPagado = CASE WHEN @Moneda = 'PEN' THEN @TotalPagadoPEN ELSE @TotalPagadoUSD END;
        SET @SaldoPendiente = CASE WHEN @Moneda = 'PEN' THEN @SaldoPEN ELSE @SaldoUSD END;
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
