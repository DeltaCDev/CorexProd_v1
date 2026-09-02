SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.TesCuentasPorPagar', 'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_TesCuentasPorPagar_Estado' AND parent_object_id = OBJECT_ID('dbo.TesCuentasPorPagar'))
BEGIN
    ALTER TABLE dbo.TesCuentasPorPagar DROP CONSTRAINT CK_TesCuentasPorPagar_Estado;
END;
GO

IF OBJECT_ID('dbo.TesCuentasPorPagar', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_TesCuentasPorPagar_Estado' AND parent_object_id = OBJECT_ID('dbo.TesCuentasPorPagar'))
BEGIN
    ALTER TABLE dbo.TesCuentasPorPagar
    ADD CONSTRAINT CK_TesCuentasPorPagar_Estado CHECK (Estado IN ('PENDIENTE', 'PARCIAL', 'PAGADA', 'CANCELADA', 'ANULADA'));
END;
GO

IF OBJECT_ID('dbo.TesCuentaPorPagarCuotas', 'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_TesCxpCuotas_Estado' AND parent_object_id = OBJECT_ID('dbo.TesCuentaPorPagarCuotas'))
BEGIN
    ALTER TABLE dbo.TesCuentaPorPagarCuotas DROP CONSTRAINT CK_TesCxpCuotas_Estado;
END;
GO

IF OBJECT_ID('dbo.TesCuentaPorPagarCuotas', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_TesCxpCuotas_Estado' AND parent_object_id = OBJECT_ID('dbo.TesCuentaPorPagarCuotas'))
BEGIN
    ALTER TABLE dbo.TesCuentaPorPagarCuotas
    ADD CONSTRAINT CK_TesCxpCuotas_Estado CHECK (Estado IN ('PENDIENTE', 'PARCIAL', 'PAGADA', 'CANCELADA', 'ANULADA'));
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_TES_CXP_REGISTRAR_PAGO
    @IdPago INT OUTPUT,
    @IdCuota INT,
    @FechaPago DATE,
    @Importe DECIMAL(18,2),
    @IdCuentaBancaria INT = NULL,
    @NumeroOperacion VARCHAR(80) = '',
    @Observacion VARCHAR(500) = '',
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

    SET @IdPago = 0;
    SET @Resultado = 0;
    SET @Mensaje = '';
    SET @TotalPagado = 0;
    SET @SaldoPendiente = 0;
    SET @EstadoCuota = '';
    SET @EstadoCuentaPorPagar = '';
    SET @NumeroOperacion = LTRIM(RTRIM(ISNULL(@NumeroOperacion, '')));
    SET @Observacion = LTRIM(RTRIM(ISNULL(@Observacion, '')));
    SET @Usuario = LTRIM(RTRIM(ISNULL(@Usuario, '')));

    IF @Usuario = ''
    BEGIN
        SET @Mensaje = 'Debe indicar el usuario que registra el pago.';
        RETURN;
    END;

    IF @IdCuota <= 0
    BEGIN
        SET @Mensaje = 'Debe seleccionar una cuota valida.';
        RETURN;
    END;

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

    IF @IdCuentaBancaria IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.TesCuentasBancarias WHERE IdCuentaBancaria = @IdCuentaBancaria AND Estado = 1)
    BEGIN
        SET @Mensaje = 'La cuenta bancaria no existe o esta inactiva.';
        RETURN;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE
            @IdCuentaPorPagar INT,
            @ImporteCuota DECIMAL(18,2),
            @EstadoAnteriorCuota VARCHAR(30),
            @EstadoAnteriorCuenta VARCHAR(30),
            @TotalPagadoAnterior DECIMAL(18,2),
            @SaldoAnterior DECIMAL(18,2);

        SELECT
            @IdCuentaPorPagar = C.IdCuentaPorPagar,
            @ImporteCuota = Q.Importe,
            @EstadoAnteriorCuota = Q.Estado,
            @EstadoAnteriorCuenta = C.Estado
        FROM dbo.TesCuentaPorPagarCuotas Q WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN dbo.TesCuentasPorPagar C WITH (UPDLOCK, HOLDLOCK)
            ON C.IdCuentaPorPagar = Q.IdCuentaPorPagar
        WHERE Q.IdCuota = @IdCuota;

        IF @IdCuentaPorPagar IS NULL
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'La cuota seleccionada no existe.';
            RETURN;
        END;

        IF @EstadoAnteriorCuenta = 'ANULADA'
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'No se puede registrar pagos sobre una cuenta por pagar anulada.';
            RETURN;
        END;

        IF @EstadoAnteriorCuota = 'ANULADA'
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'No se puede registrar pagos sobre una cuota anulada.';
            RETURN;
        END;

        IF @EstadoAnteriorCuota = 'CANCELADA'
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'La cuota seleccionada ya se encuentra cancelada.';
            RETURN;
        END;

        SELECT @TotalPagadoAnterior = ISNULL(SUM(Importe), 0)
        FROM dbo.TesCuentaPorPagarPagos WITH (UPDLOCK, HOLDLOCK)
        WHERE IdCuota = @IdCuota
          AND Estado = 'ACTIVO';

        SET @SaldoAnterior = @ImporteCuota - @TotalPagadoAnterior;

        IF @SaldoAnterior <= 0
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'La cuota seleccionada no tiene saldo pendiente.';
            RETURN;
        END;

        IF @Importe > @SaldoAnterior
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = CONCAT('El importe del pago no puede superar el saldo pendiente de ', FORMAT(@SaldoAnterior, 'N2'), '.');
            RETURN;
        END;

        INSERT INTO dbo.TesCuentaPorPagarPagos
        (
            IdCuota,
            FechaPago,
            Importe,
            MedioPago,
            IdCuentaBancaria,
            NumeroOperacion,
            Observacion,
            Estado,
            UsuarioRegistro
        )
        VALUES
        (
            @IdCuota,
            @FechaPago,
            @Importe,
            CASE WHEN @IdCuentaBancaria IS NULL THEN 'NO ESPECIFICADO' ELSE 'TRANSFERENCIA' END,
            @IdCuentaBancaria,
            @NumeroOperacion,
            @Observacion,
            'ACTIVO',
            @Usuario
        );

        SET @IdPago = SCOPE_IDENTITY();

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
        WHERE IdCuentaPorPagar = @IdCuentaPorPagar;

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
            'REGISTRO_PAGO',
            @EstadoAnteriorCuota,
            @EstadoCuota,
            CONCAT(
                'Pago registrado. Importe: ', FORMAT(@Importe, 'N2'),
                '. Fecha: ', CONVERT(VARCHAR(10), @FechaPago, 103),
                CASE WHEN @NumeroOperacion <> '' THEN CONCAT('. Operacion: ', @NumeroOperacion) ELSE '' END,
                '. Saldo pendiente: ', FORMAT(@SaldoPendiente, 'N2'), '.'
            )
        );

        COMMIT TRANSACTION;

        SET @Resultado = 1;
        SET @Mensaje = CASE
            WHEN @EstadoCuota = 'CANCELADA' THEN 'Pago registrado correctamente. La cuota quedo cancelada.'
            ELSE 'Pago registrado correctamente.'
        END;

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
        PA.FechaRegistro
    FROM dbo.TesCuentaPorPagarPagos PA
    INNER JOIN dbo.TesCuentaPorPagarCuotas Q ON Q.IdCuota = PA.IdCuota
    LEFT JOIN dbo.TesCuentasBancarias CB ON CB.IdCuentaBancaria = PA.IdCuentaBancaria
    LEFT JOIN dbo.TesBancos B ON B.IdBanco = CB.IdBanco
    WHERE Q.IdCuentaPorPagar = @IdCuentaPorPagar
      AND PA.Estado = 'ACTIVO'
    ORDER BY PA.FechaPago, PA.IdCuentaPorPagarPago;

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

CREATE OR ALTER PROCEDURE dbo.USP_TES_CXP_PROGRAMACION_RANGO
    @FechaDesde DATE,
    @FechaHasta DATE,
    @IdProveedor INT = NULL,
    @Estado VARCHAR(30) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @Estado = NULLIF(UPPER(LTRIM(RTRIM(ISNULL(@Estado, '')))), '');

    SELECT
        Q.IdCuota,
        Q.IdCuentaPorPagar,
        C.IdProveedor,
        P.NombreRazonSocial AS NombreProveedor,
        P.NumeroDocumento AS NumeroDocumentoProveedor,
        C.IdTipoObligacion,
        T.Nombre AS TipoObligacion,
        C.Moneda,
        C.FechaDocumento,
        Q.NumeroCuota,
        Q.TotalCuotas,
        Q.NumeroLetra,
        Q.FechaGiro,
        Q.FechaVencimiento,
        Q.Importe,
        ISNULL(PG.TotalPagado, 0) AS TotalPagado,
        Q.Importe - ISNULL(PG.TotalPagado, 0) AS SaldoPendiente,
        Q.Estado,
        C.OrigenTipo,
        C.OrigenId,
        C.Observacion
    FROM dbo.TesCuentaPorPagarCuotas Q
    INNER JOIN dbo.TesCuentasPorPagar C ON C.IdCuentaPorPagar = Q.IdCuentaPorPagar
    INNER JOIN dbo.Proveedores P ON P.IdProveedor = C.IdProveedor
    INNER JOIN dbo.TesTiposObligacion T ON T.IdTipoObligacion = C.IdTipoObligacion
    OUTER APPLY
    (
        SELECT SUM(PA.Importe) AS TotalPagado
        FROM dbo.TesCuentaPorPagarPagos PA
        WHERE PA.IdCuota = Q.IdCuota
          AND PA.Estado = 'ACTIVO'
    ) PG
    WHERE Q.FechaVencimiento >= @FechaDesde
      AND Q.FechaVencimiento <= @FechaHasta
      AND C.Estado <> 'ANULADA'
      AND Q.Estado <> 'ANULADA'
      AND (@IdProveedor IS NULL OR C.IdProveedor = @IdProveedor)
      AND (@Estado IS NULL OR Q.Estado = @Estado)
      AND (@Estado = 'CANCELADA' OR Q.Importe - ISNULL(PG.TotalPagado, 0) > 0)
    ORDER BY Q.FechaVencimiento, P.NombreRazonSocial, Q.NumeroCuota;
END;
GO
