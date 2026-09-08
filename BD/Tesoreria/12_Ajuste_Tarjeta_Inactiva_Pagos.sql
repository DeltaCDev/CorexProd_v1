USE CorexProdDB;
GO

/*
    ETAPA 14.1.1 - Tarjetas de credito inactivas.
    Permite registrar y anular logicamente pagos de periodos existentes
    cuando la tarjeta esta INACTIVA. Las tarjetas ANULADAS permanecen bloqueadas.
    No modifica tablas, columnas ni otros objetos.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.USP_TES_TARJETA_REGISTRAR_PAGO
    @IdTarjetaPago INT OUTPUT,
    @IdTarjetaPeriodo INT,
    @FechaPago DATE,
    @IdBanco INT = NULL,
    @IdCuentaBancaria INT = NULL,
    @Importe DECIMAL(18,2),
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

    SET @IdTarjetaPago = 0;
    SET @Resultado = 0;
    SET @Mensaje = '';
    SET @TotalPagado = 0;
    SET @SaldoPendiente = 0;
    SET @EstadoPeriodo = '';
    SET @NumeroOperacion = LTRIM(RTRIM(ISNULL(@NumeroOperacion, '')));
    SET @Observacion = LTRIM(RTRIM(ISNULL(@Observacion, '')));
    SET @Usuario = LTRIM(RTRIM(ISNULL(@Usuario, 'Sistema')));

    IF @FechaPago IS NULL
    BEGIN
        SET @Mensaje = 'Debe ingresar la fecha de pago.';
        RETURN;
    END;

    IF @Importe <= 0
    BEGIN
        SET @Mensaje = 'El importe del pago debe ser mayor a cero.';
        RETURN;
    END;

    IF @Usuario = ''
    BEGIN
        SET @Mensaje = 'Debe indicar el usuario que registra el pago.';
        RETURN;
    END;

    IF @IdBanco IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.TesBancos WHERE IdBanco = @IdBanco AND Estado = 1)
    BEGIN
        SET @Mensaje = 'El banco seleccionado no existe o esta inactivo.';
        RETURN;
    END;

    DECLARE @IdBancoCuenta INT;

    IF @IdCuentaBancaria IS NOT NULL
    BEGIN
        SELECT @IdBancoCuenta = IdBanco
        FROM dbo.TesCuentasBancarias
        WHERE IdCuentaBancaria = @IdCuentaBancaria
          AND Estado = 1;

        IF @IdBancoCuenta IS NULL
        BEGIN
            SET @Mensaje = 'La cuenta bancaria no existe o esta inactiva.';
            RETURN;
        END;

        IF @IdBanco IS NULL
            SET @IdBanco = @IdBancoCuenta;

        IF @IdBanco <> @IdBancoCuenta
        BEGIN
            SET @Mensaje = 'La cuenta bancaria no pertenece al banco seleccionado.';
            RETURN;
        END;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE
            @IdTarjetaCredito INT,
            @EstadoTarjeta VARCHAR(20),
            @EstadoAnterior VARCHAR(30),
            @Monto DECIMAL(18,2),
            @TotalPagadoAnterior DECIMAL(18,2),
            @SaldoAnterior DECIMAL(18,2);

        SELECT
            @IdTarjetaCredito = P.IdTarjetaCredito,
            @EstadoTarjeta = T.Estado,
            @EstadoAnterior = P.Estado,
            @Monto = P.Monto
        FROM dbo.TesTarjetaCreditoPeriodos P WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN dbo.TesTarjetasCredito T WITH (UPDLOCK, HOLDLOCK) ON T.IdTarjetaCredito = P.IdTarjetaCredito
        WHERE P.IdTarjetaPeriodo = @IdTarjetaPeriodo;

        IF @IdTarjetaCredito IS NULL
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'El periodo de tarjeta no existe.';
            RETURN;
        END;

        IF @EstadoTarjeta NOT IN ('ACTIVA', 'INACTIVA')
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'No se puede registrar pagos sobre una tarjeta anulada.';
            RETURN;
        END;

        IF @EstadoAnterior IN ('ABIERTO', 'ANULADA')
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'No se puede registrar pagos sobre un periodo abierto o anulado.';
            RETURN;
        END;

        IF @Monto IS NULL OR @Monto <= 0
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'El periodo no tiene monto exigible.';
            RETURN;
        END;

        SELECT @TotalPagadoAnterior = ISNULL(SUM(Importe), 0)
        FROM dbo.TesTarjetaCreditoPagos WITH (UPDLOCK, HOLDLOCK)
        WHERE IdTarjetaPeriodo = @IdTarjetaPeriodo
          AND Estado = 'ACTIVO';

        SET @SaldoAnterior = @Monto - @TotalPagadoAnterior;

        IF @SaldoAnterior <= 0
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'El periodo seleccionado no tiene saldo pendiente.';
            RETURN;
        END;

        IF @Importe > @SaldoAnterior
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = CONCAT('El importe del pago no puede superar el saldo pendiente de ', FORMAT(@SaldoAnterior, 'N2'), '.');
            RETURN;
        END;

        INSERT INTO dbo.TesTarjetaCreditoPagos
        (
            IdTarjetaPeriodo, FechaPago, IdBanco, IdCuentaBancaria, Importe,
            NumeroOperacion, Observacion, Estado, UsuarioCreacion
        )
        VALUES
        (
            @IdTarjetaPeriodo, @FechaPago, @IdBanco, @IdCuentaBancaria, @Importe,
            @NumeroOperacion, @Observacion, 'ACTIVO', @Usuario
        );

        SET @IdTarjetaPago = SCOPE_IDENTITY();

        SELECT @TotalPagado = ISNULL(SUM(Importe), 0)
        FROM dbo.TesTarjetaCreditoPagos
        WHERE IdTarjetaPeriodo = @IdTarjetaPeriodo
          AND Estado = 'ACTIVO';

        SET @SaldoPendiente = @Monto - @TotalPagado;
        SET @EstadoPeriodo = CASE
            WHEN @TotalPagado <= 0 THEN 'PENDIENTE'
            WHEN @SaldoPendiente <= 0 THEN 'CANCELADA'
            ELSE 'PARCIAL'
        END;

        UPDATE dbo.TesTarjetaCreditoPeriodos
        SET TotalPagado = @TotalPagado,
            SaldoPendiente = @SaldoPendiente,
            Estado = @EstadoPeriodo,
            FechaModificacion = GETDATE(),
            UsuarioModificacion = @Usuario
        WHERE IdTarjetaPeriodo = @IdTarjetaPeriodo;

        INSERT INTO dbo.TesTarjetaCreditoHistorial
        (
            IdTarjetaCredito, IdTarjetaPeriodo, IdTarjetaPago, Usuario, Accion,
            EstadoAnterior, EstadoNuevo, Descripcion
        )
        VALUES
        (
            @IdTarjetaCredito, @IdTarjetaPeriodo, @IdTarjetaPago, @Usuario, 'REGISTRO_PAGO',
            @EstadoAnterior, @EstadoPeriodo,
            CONCAT('Pago registrado. Importe: ', FORMAT(@Importe, 'N2'), '. Saldo pendiente: ', FORMAT(@SaldoPendiente, 'N2'), '.')
        );

        COMMIT TRANSACTION;
        SET @Resultado = 1;
        SET @Mensaje = 'Pago de tarjeta registrado correctamente.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH;

    SELECT @Resultado AS Resultado, @Mensaje AS Mensaje, @IdTarjetaPago AS IdTarjetaPago,
        @TotalPagado AS TotalPagado, @SaldoPendiente AS SaldoPendiente, @EstadoPeriodo AS EstadoPeriodo;
END;
GO

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
    SET @TotalPagado = 0;
    SET @SaldoPendiente = 0;
    SET @EstadoPeriodo = '';
    SET @Usuario = LTRIM(RTRIM(ISNULL(@Usuario, 'Sistema')));
    SET @Motivo = LTRIM(RTRIM(ISNULL(@Motivo, '')));

    IF @Motivo = ''
    BEGIN
        SET @Mensaje = 'Debe ingresar el motivo de anulacion del pago.';
        RETURN;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE
            @IdTarjetaPeriodo INT,
            @IdTarjetaCredito INT,
            @EstadoTarjeta VARCHAR(20),
            @EstadoPago VARCHAR(20),
            @EstadoAnterior VARCHAR(30),
            @Monto DECIMAL(18,2);

        SELECT
            @IdTarjetaPeriodo = P.IdTarjetaPeriodo,
            @IdTarjetaCredito = PR.IdTarjetaCredito,
            @EstadoTarjeta = T.Estado,
            @EstadoPago = P.Estado,
            @EstadoAnterior = PR.Estado,
            @Monto = PR.Monto
        FROM dbo.TesTarjetaCreditoPagos P WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN dbo.TesTarjetaCreditoPeriodos PR WITH (UPDLOCK, HOLDLOCK) ON PR.IdTarjetaPeriodo = P.IdTarjetaPeriodo
        INNER JOIN dbo.TesTarjetasCredito T WITH (UPDLOCK, HOLDLOCK) ON T.IdTarjetaCredito = PR.IdTarjetaCredito
        WHERE P.IdTarjetaPago = @IdTarjetaPago;

        IF @IdTarjetaPeriodo IS NULL
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'El pago seleccionado no existe.';
            RETURN;
        END;

        IF @EstadoTarjeta NOT IN ('ACTIVA', 'INACTIVA')
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'No se puede anular pagos de una tarjeta anulada.';
            RETURN;
        END;

        IF @EstadoPago = 'ANULADO'
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'El pago seleccionado ya se encuentra anulado.';
            RETURN;
        END;

        IF @EstadoAnterior = 'ANULADA'
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

        SELECT @TotalPagado = ISNULL(SUM(Importe), 0)
        FROM dbo.TesTarjetaCreditoPagos
        WHERE IdTarjetaPeriodo = @IdTarjetaPeriodo
          AND Estado = 'ACTIVO';

        SET @SaldoPendiente = @Monto - @TotalPagado;
        SET @EstadoPeriodo = CASE
            WHEN @Monto IS NULL THEN 'ABIERTO'
            WHEN @TotalPagado <= 0 THEN 'PENDIENTE'
            WHEN @SaldoPendiente <= 0 THEN 'CANCELADA'
            ELSE 'PARCIAL'
        END;

        UPDATE dbo.TesTarjetaCreditoPeriodos
        SET TotalPagado = @TotalPagado,
            SaldoPendiente = CASE WHEN @Monto IS NULL THEN NULL ELSE @SaldoPendiente END,
            Estado = @EstadoPeriodo,
            FechaModificacion = GETDATE(),
            UsuarioModificacion = @Usuario
        WHERE IdTarjetaPeriodo = @IdTarjetaPeriodo;

        INSERT INTO dbo.TesTarjetaCreditoHistorial
        (
            IdTarjetaCredito, IdTarjetaPeriodo, IdTarjetaPago, Usuario, Accion,
            EstadoAnterior, EstadoNuevo, Descripcion
        )
        VALUES
        (
            @IdTarjetaCredito, @IdTarjetaPeriodo, @IdTarjetaPago, @Usuario, 'ANULACION_PAGO',
            @EstadoAnterior, @EstadoPeriodo, @Motivo
        );

        COMMIT TRANSACTION;
        SET @Resultado = 1;
        SET @Mensaje = 'Pago de tarjeta anulado correctamente.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH;

    SELECT @Resultado AS Resultado, @Mensaje AS Mensaje, @IdTarjetaPago AS IdTarjetaPago,
        @TotalPagado AS TotalPagado, @SaldoPendiente AS SaldoPendiente, @EstadoPeriodo AS EstadoPeriodo;
END;
GO
