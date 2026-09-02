SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.USP_TES_CXP_ANULAR_PAGO
    @IdPago INT,
    @Motivo VARCHAR(500),
    @Usuario VARCHAR(80),
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT,
    @TotalPagado DECIMAL(18,2) OUTPUT,
    @SaldoPendiente DECIMAL(18,2) OUTPUT,
    @EstadoCuota VARCHAR(30) OUTPUT,
    @EstadoCuentaPorPagar VARCHAR(30) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @Resultado = 0;
    SET @Mensaje = '';
    SET @TotalPagado = 0;
    SET @SaldoPendiente = 0;
    SET @EstadoCuota = '';
    SET @EstadoCuentaPorPagar = '';
    SET @Motivo = LTRIM(RTRIM(ISNULL(@Motivo, '')));
    SET @Usuario = LTRIM(RTRIM(ISNULL(@Usuario, '')));

    IF @IdPago <= 0
    BEGIN
        SET @Mensaje = 'Debe seleccionar un pago valido.';
        RETURN;
    END;

    IF @Motivo = ''
    BEGIN
        SET @Mensaje = 'Debe ingresar el motivo de anulacion del pago.';
        RETURN;
    END;

    IF @Usuario = ''
    BEGIN
        SET @Mensaje = 'Debe indicar el usuario que anula el pago.';
        RETURN;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE
            @IdCuota INT,
            @IdCuentaPorPagar INT,
            @ImportePago DECIMAL(18,2),
            @ImporteCuota DECIMAL(18,2),
            @NumeroCuota INT,
            @NumeroLetra VARCHAR(60),
            @EstadoPago VARCHAR(20),
            @EstadoAnteriorCuota VARCHAR(30),
            @EstadoAnteriorCuenta VARCHAR(30);

        SELECT
            @IdCuota = P.IdCuota,
            @ImportePago = P.Importe,
            @EstadoPago = P.Estado
        FROM dbo.TesCuentaPorPagarPagos P WITH (UPDLOCK, HOLDLOCK)
        WHERE P.IdCuentaPorPagarPago = @IdPago;

        IF @IdCuota IS NULL
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'El pago seleccionado no existe.';
            RETURN;
        END;

        IF @EstadoPago <> 'ACTIVO'
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'El pago seleccionado ya se encuentra anulado.';
            RETURN;
        END;

        SELECT
            @IdCuentaPorPagar = C.IdCuentaPorPagar,
            @ImporteCuota = Q.Importe,
            @NumeroCuota = Q.NumeroCuota,
            @NumeroLetra = Q.NumeroLetra,
            @EstadoAnteriorCuota = Q.Estado,
            @EstadoAnteriorCuenta = C.Estado
        FROM dbo.TesCuentaPorPagarCuotas Q WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN dbo.TesCuentasPorPagar C WITH (UPDLOCK, HOLDLOCK)
            ON C.IdCuentaPorPagar = Q.IdCuentaPorPagar
        WHERE Q.IdCuota = @IdCuota;

        IF @IdCuentaPorPagar IS NULL
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'La cuota asociada al pago no existe.';
            RETURN;
        END;

        IF @EstadoAnteriorCuenta = 'ANULADA'
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'No se puede anular pagos de una cuenta por pagar anulada.';
            RETURN;
        END;

        UPDATE dbo.TesCuentaPorPagarPagos
        SET Estado = 'ANULADO',
            UsuarioAnulacion = @Usuario,
            FechaAnulacion = GETDATE(),
            MotivoAnulacion = @Motivo
        WHERE IdCuentaPorPagarPago = @IdPago
          AND Estado = 'ACTIVO';

        IF @@ROWCOUNT = 0
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'El pago seleccionado ya se encuentra anulado.';
            RETURN;
        END;

        SELECT @TotalPagado = ISNULL(SUM(Importe), 0)
        FROM dbo.TesCuentaPorPagarPagos
        WHERE IdCuota = @IdCuota
          AND Estado = 'ACTIVO';

        SET @SaldoPendiente = @ImporteCuota - @TotalPagado;
        SET @EstadoCuota = CASE
            WHEN @TotalPagado <= 0 THEN 'PENDIENTE'
            WHEN @SaldoPendiente <= 0 THEN 'CANCELADA'
            ELSE 'PARCIAL'
        END;

        UPDATE dbo.TesCuentaPorPagarCuotas
        SET Estado = @EstadoCuota
        WHERE IdCuota = @IdCuota;

        DECLARE
            @CuotasActivas INT,
            @CuotasConPago INT,
            @CuotasConSaldo INT;

        SELECT
            @CuotasActivas = COUNT(1),
            @CuotasConPago = SUM(CASE WHEN ISNULL(PG.TotalPagado, 0) > 0 THEN 1 ELSE 0 END),
            @CuotasConSaldo = SUM(CASE WHEN Q.Importe - ISNULL(PG.TotalPagado, 0) > 0 THEN 1 ELSE 0 END)
        FROM dbo.TesCuentaPorPagarCuotas Q WITH (UPDLOCK, HOLDLOCK)
        OUTER APPLY
        (
            SELECT SUM(PA.Importe) AS TotalPagado
            FROM dbo.TesCuentaPorPagarPagos PA
            WHERE PA.IdCuota = Q.IdCuota
              AND PA.Estado = 'ACTIVO'
        ) PG
        WHERE Q.IdCuentaPorPagar = @IdCuentaPorPagar
          AND Q.Estado <> 'ANULADA';

        SET @EstadoCuentaPorPagar = CASE
            WHEN ISNULL(@CuotasActivas, 0) = 0 THEN @EstadoAnteriorCuenta
            WHEN ISNULL(@CuotasConSaldo, 0) = 0 THEN 'CANCELADA'
            WHEN ISNULL(@CuotasConPago, 0) > 0 THEN 'PARCIAL'
            ELSE 'PENDIENTE'
        END;

        UPDATE dbo.TesCuentasPorPagar
        SET Estado = @EstadoCuentaPorPagar,
            UsuarioModificacion = @Usuario,
            FechaModificacion = GETDATE()
        WHERE IdCuentaPorPagar = @IdCuentaPorPagar
          AND Estado <> 'ANULADA';

        INSERT INTO dbo.TesCuentaPorPagarHistorial
        (
            IdCuentaPorPagar,
            IdCuota,
            Usuario,
            Accion,
            EstadoAnterior,
            EstadoNuevo,
            Descripcion
        )
        VALUES
        (
            @IdCuentaPorPagar,
            @IdCuota,
            @Usuario,
            'ANULACION_PAGO',
            @EstadoAnteriorCuota,
            @EstadoCuota,
            CONCAT(
                'Pago anulado. IdPago: ', @IdPago,
                '. Cuota: ', @NumeroCuota,
                CASE WHEN ISNULL(@NumeroLetra, '') <> '' THEN CONCAT('. Letra: ', @NumeroLetra) ELSE '' END,
                '. Importe: ', FORMAT(@ImportePago, 'N2'),
                '. Motivo: ', @Motivo,
                '. Saldo pendiente: ', FORMAT(@SaldoPendiente, 'N2'), '.'
            )
        );

        COMMIT TRANSACTION;

        SET @Resultado = 1;
        SET @Mensaje = 'Pago anulado correctamente.';

        SELECT
            @Resultado AS Resultado,
            @Mensaje AS Mensaje,
            @IdPago AS IdPago,
            @TotalPagado AS TotalPagado,
            @SaldoPendiente AS SaldoPendiente,
            @EstadoCuota AS EstadoCuota,
            @EstadoCuentaPorPagar AS EstadoCuentaPorPagar;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();

        SELECT
            @Resultado AS Resultado,
            @Mensaje AS Mensaje,
            @IdPago AS IdPago,
            @TotalPagado AS TotalPagado,
            @SaldoPendiente AS SaldoPendiente,
            @EstadoCuota AS EstadoCuota,
            @EstadoCuentaPorPagar AS EstadoCuentaPorPagar;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_TES_CXP_OBTENER
    @IdCuentaPorPagar INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        C.IdCuentaPorPagar,
        C.IdProveedor,
        P.TipoDocumento AS TipoDocumentoProveedor,
        P.NumeroDocumento AS NumeroDocumentoProveedor,
        P.NombreRazonSocial AS NombreProveedor,
        C.IdTipoObligacion,
        T.Codigo AS CodigoTipoObligacion,
        T.Nombre AS TipoObligacion,
        C.FechaDocumento,
        C.Moneda,
        C.ImporteTotal,
        ISNULL(PG.TotalPagado, 0) AS TotalPagado,
        C.ImporteTotal - ISNULL(PG.TotalPagado, 0) AS SaldoPendiente,
        C.Estado,
        C.OrigenTipo,
        C.OrigenId,
        C.Observacion,
        C.UsuarioRegistro,
        C.FechaRegistro,
        C.UsuarioModificacion,
        C.FechaModificacion,
        C.UsuarioAnulacion,
        C.FechaAnulacion,
        C.MotivoAnulacion
    FROM dbo.TesCuentasPorPagar C
    INNER JOIN dbo.Proveedores P ON P.IdProveedor = C.IdProveedor
    INNER JOIN dbo.TesTiposObligacion T ON T.IdTipoObligacion = C.IdTipoObligacion
    OUTER APPLY
    (
        SELECT SUM(PA.Importe) AS TotalPagado
        FROM dbo.TesCuentaPorPagarCuotas CX
        INNER JOIN dbo.TesCuentaPorPagarPagos PA ON PA.IdCuota = CX.IdCuota
        WHERE CX.IdCuentaPorPagar = C.IdCuentaPorPagar
          AND PA.Estado = 'ACTIVO'
    ) PG
    WHERE C.IdCuentaPorPagar = @IdCuentaPorPagar;

    SELECT
        D.IdCuentaPorPagarDocumento,
        D.IdCuentaPorPagar,
        D.IdTipoDocumento,
        TD.NombreTipoDocumento,
        D.Serie,
        D.Numero,
        D.NumeroDocumento,
        D.FechaDocumento,
        D.Importe,
        D.Observacion,
        D.Estado
    FROM dbo.TesCuentaPorPagarDocumentos D
    INNER JOIN dbo.TiposDocumentoStock TD ON TD.IdTipoDocumento = D.IdTipoDocumento
    WHERE D.IdCuentaPorPagar = @IdCuentaPorPagar
      AND D.Estado = 'ACTIVO'
    ORDER BY D.FechaDocumento, D.IdCuentaPorPagarDocumento;

    SELECT
        Q.IdCuota,
        Q.IdCuentaPorPagar,
        Q.NumeroCuota,
        Q.TotalCuotas,
        Q.NumeroLetra,
        Q.FechaGiro,
        Q.FechaVencimiento,
        Q.Importe,
        ISNULL(PG.TotalPagado, 0) AS TotalPagado,
        Q.Importe - ISNULL(PG.TotalPagado, 0) AS SaldoPendiente,
        Q.Estado,
        Q.Observacion
    FROM dbo.TesCuentaPorPagarCuotas Q
    OUTER APPLY
    (
        SELECT SUM(PA.Importe) AS TotalPagado
        FROM dbo.TesCuentaPorPagarPagos PA
        WHERE PA.IdCuota = Q.IdCuota
          AND PA.Estado = 'ACTIVO'
    ) PG
    WHERE Q.IdCuentaPorPagar = @IdCuentaPorPagar
      AND Q.Estado <> 'ANULADA'
    ORDER BY Q.NumeroCuota, Q.IdCuota;

    SELECT
        PA.IdCuentaPorPagarPago,
        PA.IdCuota,
        Q.IdCuentaPorPagar,
        Q.NumeroCuota,
        Q.NumeroLetra,
        PA.FechaPago,
        PA.Importe,
        PA.MedioPago,
        PA.IdCuentaBancaria,
        B.Nombre AS Banco,
        CB.NumeroCuenta,
        PA.NumeroOperacion,
        PA.Observacion,
        PA.Estado,
        PA.UsuarioRegistro,
        PA.FechaRegistro,
        PA.UsuarioAnulacion,
        PA.FechaAnulacion,
        PA.MotivoAnulacion
    FROM dbo.TesCuentaPorPagarPagos PA
    INNER JOIN dbo.TesCuentaPorPagarCuotas Q ON Q.IdCuota = PA.IdCuota
    LEFT JOIN dbo.TesCuentasBancarias CB ON CB.IdCuentaBancaria = PA.IdCuentaBancaria
    LEFT JOIN dbo.TesBancos B ON B.IdBanco = CB.IdBanco
    WHERE Q.IdCuentaPorPagar = @IdCuentaPorPagar
    ORDER BY PA.FechaRegistro DESC, PA.IdCuentaPorPagarPago DESC;

    SELECT
        H.IdCuentaPorPagarHistorial,
        H.IdCuentaPorPagar,
        H.IdCuota,
        H.Usuario,
        H.Accion,
        H.EstadoAnterior,
        H.EstadoNuevo,
        H.Descripcion,
        H.FechaHora
    FROM dbo.TesCuentaPorPagarHistorial H
    WHERE H.IdCuentaPorPagar = @IdCuentaPorPagar
    ORDER BY H.FechaHora DESC, H.IdCuentaPorPagarHistorial DESC;
END;
GO
