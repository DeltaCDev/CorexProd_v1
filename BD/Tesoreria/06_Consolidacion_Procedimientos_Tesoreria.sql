USE CorexProdDB;
GO

/*
    Consolidacion final de procedimientos de Tesoreria / Cuentas por Pagar.
    Este script no modifica tablas, tipos ni datos de negocio.
    Debe ejecutarse al final de la cadena 01 -> 02 -> 03 -> 04 -> 05 -> 06.
*/

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO


-- Version final: dbo.USP_TES_CXP_GUARDAR
CREATE OR ALTER PROCEDURE dbo.USP_TES_CXP_GUARDAR
    @IdCuentaPorPagar INT OUTPUT,
    @IdProveedor INT,
    @IdTipoObligacion INT,
    @FechaDocumento DATE,
    @Moneda VARCHAR(10),
    @ImporteTotal DECIMAL(18,2),
    @OrigenTipo VARCHAR(60),
    @OrigenId INT = NULL,
    @Observacion VARCHAR(1000) = '',
    @Usuario VARCHAR(80),
    @Documentos dbo.TesCuentaPorPagarDocumentoType READONLY,
    @Cuotas dbo.TesCuentaPorPagarCuotaType READONLY,
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @EstadoAnterior VARCHAR(30) = NULL;
    DECLARE @CodigoTipoObligacion VARCHAR(40);
    DECLARE @TipoCuotaEsperado VARCHAR(20);
    DECLARE @TotalDocumentosPositivos DECIMAL(18,2);
    DECLARE @TotalNotasCredito DECIMAL(18,2);
    DECLARE @TotalNeto DECIMAL(18,2);

    SET @Resultado = 0;
    SET @Mensaje = '';
    SET @Moneda = UPPER(LTRIM(RTRIM(ISNULL(@Moneda, ''))));
    SET @OrigenTipo = UPPER(LTRIM(RTRIM(ISNULL(@OrigenTipo, 'MANUAL'))));
    SET @Usuario = LTRIM(RTRIM(ISNULL(@Usuario, 'Sistema')));

    SELECT @CodigoTipoObligacion = Codigo
    FROM dbo.TesTiposObligacion
    WHERE IdTipoObligacion = @IdTipoObligacion
      AND Estado = 1;

    SET @TipoCuotaEsperado = CASE WHEN @CodigoTipoObligacion = 'FACTURA_CREDITO' THEN 'CUOTA_FACTURA' ELSE 'LETRA' END;

    SELECT
        @TotalDocumentosPositivos = ROUND(ISNULL(SUM(CASE WHEN FactorEfecto = 1 THEN Importe ELSE 0 END), 0), 2),
        @TotalNotasCredito = ROUND(ISNULL(SUM(CASE WHEN FactorEfecto = -1 THEN Importe ELSE 0 END), 0), 2)
    FROM @Documentos;

    SET @TotalNeto = ROUND(@TotalDocumentosPositivos - @TotalNotasCredito, 2);

    IF NOT EXISTS (SELECT 1 FROM dbo.Proveedores WHERE IdProveedor = @IdProveedor)
    BEGIN SET @Mensaje = 'El proveedor seleccionado no existe.'; RETURN; END;

    IF @CodigoTipoObligacion IS NULL
    BEGIN SET @Mensaje = 'El tipo de obligacion seleccionado no existe o no esta activo.'; RETURN; END;

    IF @Moneda NOT IN ('PEN', 'USD', 'EUR')
    BEGIN SET @Mensaje = 'La moneda no es valida.'; RETURN; END;

    IF NOT EXISTS (SELECT 1 FROM @Documentos)
    BEGIN SET @Mensaje = 'Debe registrar al menos un documento.'; RETURN; END;

    IF EXISTS (SELECT 1 FROM @Documentos WHERE Importe <= 0)
    BEGIN SET @Mensaje = 'Los importes de los documentos deben ser mayores a cero.'; RETURN; END;

    IF EXISTS (SELECT 1 FROM @Documentos WHERE FactorEfecto NOT IN (1, -1))
    BEGIN SET @Mensaje = 'El efecto de los documentos solo puede ser positivo o nota de credito.'; RETURN; END;

    IF NOT EXISTS (SELECT 1 FROM @Documentos WHERE FactorEfecto = 1)
    BEGIN SET @Mensaje = 'Debe registrar al menos una factura o documento positivo.'; RETURN; END;

    IF @TotalNotasCredito > @TotalDocumentosPositivos
    BEGIN SET @Mensaje = 'El total de notas de credito no puede ser mayor al total de facturas.'; RETURN; END;

    IF @TotalNeto <= 0
    BEGIN SET @Mensaje = 'El total neto por pagar debe ser mayor a cero.'; RETURN; END;

    IF ABS(ROUND(@ImporteTotal, 2) - @TotalNeto) > 0.01
    BEGIN SET @Mensaje = 'El importe total debe ser igual al total neto documental.'; RETURN; END;

    IF EXISTS (
        SELECT 1
        FROM @Documentos D
        WHERE NOT EXISTS (
            SELECT 1
            FROM dbo.TiposDocumentoStock TD
            WHERE TD.IdTipoDocumento = D.IdTipoDocumento
              AND TD.Estado = 1
        )
    )
    BEGIN SET @Mensaje = 'Todos los documentos deben tener un tipo de documento activo.'; RETURN; END;

    IF EXISTS (
        SELECT 1
        FROM @Documentos D
        INNER JOIN dbo.TiposDocumentoStock TD ON TD.IdTipoDocumento = D.IdTipoDocumento
        WHERE (UPPER(TD.NombreTipoDocumento) LIKE 'NOTA DE CR%' AND D.FactorEfecto <> -1)
           OR (UPPER(TD.NombreTipoDocumento) NOT LIKE 'NOTA DE CR%' AND D.FactorEfecto <> 1)
    )
    BEGIN SET @Mensaje = 'El efecto del documento no corresponde al tipo de documento seleccionado.'; RETURN; END;

    IF NOT EXISTS (SELECT 1 FROM @Cuotas)
    BEGIN SET @Mensaje = 'Debe registrar al menos una cuota.'; RETURN; END;

    IF EXISTS (SELECT 1 FROM @Cuotas WHERE Importe <= 0)
    BEGIN SET @Mensaje = 'Los importes de las cuotas deben ser mayores a cero.'; RETURN; END;

    IF EXISTS (
        SELECT 1
        FROM @Cuotas
        WHERE NumeroCuota <= 0
           OR TotalCuotas <= 0
           OR NumeroCuota > TotalCuotas
    )
    BEGIN SET @Mensaje = 'La numeracion de cuotas no es valida.'; RETURN; END;

    IF EXISTS (SELECT 1 FROM @Cuotas WHERE ISNULL(TipoCuota, '') <> @TipoCuotaEsperado)
    BEGIN SET @Mensaje = 'El tipo de cuota no corresponde al tipo de obligacion seleccionado.'; RETURN; END;

    IF @TipoCuotaEsperado = 'LETRA'
       AND EXISTS (SELECT 1 FROM @Cuotas WHERE LTRIM(RTRIM(ISNULL(NumeroLetra, ''))) = '')
    BEGIN SET @Mensaje = 'El numero de letra es obligatorio para Letras por Pagar.'; RETURN; END;

    IF @TipoCuotaEsperado = 'LETRA'
       AND EXISTS (SELECT 1 FROM @Cuotas WHERE FechaGiro IS NULL)
    BEGIN SET @Mensaje = 'La fecha de giro es obligatoria para Letras por Pagar.'; RETURN; END;

    IF EXISTS (SELECT 1 FROM @Cuotas WHERE FechaVencimiento IS NULL)
    BEGIN SET @Mensaje = 'Todas las cuotas deben tener fecha de vencimiento.'; RETURN; END;

    IF EXISTS (SELECT 1 FROM @Cuotas WHERE FechaVencimiento < ISNULL(FechaGiro, FechaVencimiento))
    BEGIN SET @Mensaje = 'La fecha de vencimiento no puede ser anterior a la fecha de giro.'; RETURN; END;

    IF @TipoCuotaEsperado = 'LETRA'
       AND EXISTS (
            SELECT 1
            FROM @Cuotas
            WHERE LTRIM(RTRIM(ISNULL(NumeroLetra, ''))) <> ''
            GROUP BY LTRIM(RTRIM(NumeroLetra))
            HAVING COUNT(1) > 1
       )
    BEGIN SET @Mensaje = 'No se puede duplicar el numero de letra dentro de la misma cuenta.'; RETURN; END;

    IF ABS(ROUND((SELECT SUM(Importe) FROM @Cuotas), 2) - @TotalNeto) > 0.01
    BEGIN SET @Mensaje = 'La suma de cuotas debe ser igual al total neto por pagar.'; RETURN; END;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF ISNULL(@IdCuentaPorPagar, 0) = 0
        BEGIN
            INSERT INTO dbo.TesCuentasPorPagar
            (
                IdProveedor, IdTipoObligacion, FechaDocumento, Moneda, ImporteTotal,
                Estado, OrigenTipo, OrigenId, Observacion, UsuarioRegistro
            )
            VALUES
            (
                @IdProveedor, @IdTipoObligacion, @FechaDocumento, @Moneda, @TotalNeto,
                'PENDIENTE', @OrigenTipo, @OrigenId, ISNULL(@Observacion, ''), @Usuario
            );

            SET @IdCuentaPorPagar = SCOPE_IDENTITY();
            SET @Mensaje = 'Cuenta por pagar registrada correctamente.';
        END
        ELSE
        BEGIN
            SELECT @EstadoAnterior = Estado
            FROM dbo.TesCuentasPorPagar
            WHERE IdCuentaPorPagar = @IdCuentaPorPagar;

            IF @EstadoAnterior IS NULL
            BEGIN
                ROLLBACK TRANSACTION;
                SET @Mensaje = 'La cuenta por pagar no existe.';
                RETURN;
            END;

            IF @EstadoAnterior = 'ANULADA'
            BEGIN
                ROLLBACK TRANSACTION;
                SET @Mensaje = 'No se puede modificar una cuenta por pagar anulada.';
                RETURN;
            END;

            IF EXISTS (
                SELECT 1
                FROM dbo.TesCuentaPorPagarPagos P
                INNER JOIN dbo.TesCuentaPorPagarCuotas C ON C.IdCuota = P.IdCuota
                WHERE C.IdCuentaPorPagar = @IdCuentaPorPagar
                  AND P.Estado = 'ACTIVO'
            )
            BEGIN
                ROLLBACK TRANSACTION;
                SET @Mensaje = 'No se puede modificar una cuenta por pagar que ya tiene pagos registrados.';
                RETURN;
            END;

            UPDATE dbo.TesCuentasPorPagar
            SET IdProveedor = @IdProveedor,
                IdTipoObligacion = @IdTipoObligacion,
                FechaDocumento = @FechaDocumento,
                Moneda = @Moneda,
                ImporteTotal = @TotalNeto,
                OrigenTipo = @OrigenTipo,
                OrigenId = @OrigenId,
                Observacion = ISNULL(@Observacion, ''),
                UsuarioModificacion = @Usuario,
                FechaModificacion = GETDATE()
            WHERE IdCuentaPorPagar = @IdCuentaPorPagar;

            UPDATE dbo.TesCuentaPorPagarDocumentos
            SET Estado = 'ANULADO'
            WHERE IdCuentaPorPagar = @IdCuentaPorPagar
              AND Estado <> 'ANULADO';

            UPDATE dbo.TesCuentaPorPagarCuotas
            SET Estado = 'ANULADA'
            WHERE IdCuentaPorPagar = @IdCuentaPorPagar
              AND Estado <> 'ANULADA';

            SET @Mensaje = 'Cuenta por pagar modificada. Documentos y cuotas anteriores quedaron anulados logicamente.';
        END;

        INSERT INTO dbo.TesCuentaPorPagarDocumentos
        (
            IdCuentaPorPagar, IdTipoDocumento, Serie, Numero, NumeroDocumento,
            FechaDocumento, Importe, FactorEfecto, Observacion, Estado
        )
        SELECT
            @IdCuentaPorPagar,
            IdTipoDocumento,
            ISNULL(LTRIM(RTRIM(Serie)), ''),
            ISNULL(LTRIM(RTRIM(Numero)), ''),
            LTRIM(RTRIM(NumeroDocumento)),
            FechaDocumento,
            Importe,
            FactorEfecto,
            ISNULL(Observacion, ''),
            'ACTIVO'
        FROM @Documentos;

        INSERT INTO dbo.TesCuentaPorPagarCuotas
        (
            IdCuentaPorPagar, NumeroCuota, TotalCuotas, NumeroLetra,
            TipoCuota, FechaGiro, FechaVencimiento, Importe, Estado, Observacion
        )
        SELECT
            @IdCuentaPorPagar,
            NumeroCuota,
            TotalCuotas,
            NULLIF(LTRIM(RTRIM(ISNULL(NumeroLetra, ''))), ''),
            @TipoCuotaEsperado,
            ISNULL(FechaGiro, FechaVencimiento),
            FechaVencimiento,
            Importe,
            'PENDIENTE',
            ISNULL(Observacion, '')
        FROM @Cuotas;

        INSERT INTO dbo.TesCuentaPorPagarHistorial
        (
            IdCuentaPorPagar, Usuario, Accion, EstadoAnterior, EstadoNuevo, Descripcion
        )
        VALUES
        (
            @IdCuentaPorPagar,
            @Usuario,
            CASE WHEN @EstadoAnterior IS NULL THEN 'REGISTRO' ELSE 'MODIFICACION' END,
            ISNULL(@EstadoAnterior, ''),
            'PENDIENTE',
            CONCAT(
                CASE WHEN @EstadoAnterior IS NULL THEN 'Cuenta por pagar registrada.' ELSE 'Cuenta por pagar modificada.' END,
                ' Total documentos: ', FORMAT(@TotalDocumentosPositivos, 'N2'),
                '. Notas de credito: ', FORMAT(@TotalNotasCredito, 'N2'),
                '. Neto: ', FORMAT(@TotalNeto, 'N2'), '.'
            )
        );

        COMMIT TRANSACTION;
        SET @Resultado = 1;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH;
END;
GO

-- Version final: dbo.USP_TES_CXP_LISTAR
CREATE OR ALTER PROCEDURE dbo.USP_TES_CXP_LISTAR
    @FechaDesde DATE = NULL,
    @FechaHasta DATE = NULL,
    @IdProveedor INT = NULL,
    @Estado VARCHAR(30) = NULL,
    @Texto VARCHAR(120) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET @Estado = NULLIF(UPPER(LTRIM(RTRIM(ISNULL(@Estado, '')))), '');

    SELECT
        C.IdCuentaPorPagar,
        C.IdProveedor,
        P.NombreRazonSocial AS NombreProveedor,
        P.NumeroDocumento AS NumeroDocumentoProveedor,
        C.IdTipoObligacion,
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
        MIN(CU.FechaVencimiento) AS ProximoVencimiento,
        C.UsuarioRegistro,
        C.FechaRegistro
    FROM dbo.TesCuentasPorPagar C
    INNER JOIN dbo.Proveedores P ON P.IdProveedor = C.IdProveedor
    INNER JOIN dbo.TesTiposObligacion T ON T.IdTipoObligacion = C.IdTipoObligacion
    LEFT JOIN dbo.TesCuentaPorPagarCuotas CU
           ON CU.IdCuentaPorPagar = C.IdCuentaPorPagar
          AND CU.Estado <> 'ANULADA'
          AND CU.Estado <> 'CANCELADA'
    OUTER APPLY
    (
        SELECT SUM(PA.Importe) AS TotalPagado
        FROM dbo.TesCuentaPorPagarCuotas CX
        INNER JOIN dbo.TesCuentaPorPagarPagos PA ON PA.IdCuota = CX.IdCuota
        WHERE CX.IdCuentaPorPagar = C.IdCuentaPorPagar
          AND PA.Estado = 'ACTIVO'
    ) PG
    WHERE (@FechaDesde IS NULL OR C.FechaDocumento >= @FechaDesde)
      AND (@FechaHasta IS NULL OR C.FechaDocumento <= @FechaHasta)
      AND (@IdProveedor IS NULL OR C.IdProveedor = @IdProveedor)
      AND (@Estado IS NULL OR C.Estado = @Estado)
      AND (
            @Texto IS NULL
            OR P.NombreRazonSocial LIKE '%' + @Texto + '%'
            OR P.NumeroDocumento LIKE '%' + @Texto + '%'
            OR EXISTS (
                SELECT 1
                FROM dbo.TesCuentaPorPagarDocumentos D
                WHERE D.IdCuentaPorPagar = C.IdCuentaPorPagar
                  AND D.Estado = 'ACTIVO'
                  AND D.NumeroDocumento LIKE '%' + @Texto + '%'
            )
            OR EXISTS (
                SELECT 1
                FROM dbo.TesCuentaPorPagarCuotas Q
                WHERE Q.IdCuentaPorPagar = C.IdCuentaPorPagar
                  AND Q.Estado <> 'ANULADA'
                  AND Q.NumeroLetra LIKE '%' + @Texto + '%'
            )
          )
    GROUP BY
        C.IdCuentaPorPagar, C.IdProveedor, P.NombreRazonSocial, P.NumeroDocumento,
        C.IdTipoObligacion, T.Nombre, C.FechaDocumento, C.Moneda, C.ImporteTotal,
        C.Estado, C.OrigenTipo, C.OrigenId, C.Observacion, C.UsuarioRegistro,
        C.FechaRegistro, PG.TotalPagado
    ORDER BY C.FechaDocumento DESC, C.IdCuentaPorPagar DESC;
END;
GO

-- Version final: dbo.USP_TES_CXP_OBTENER
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
        D.FactorEfecto,
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
        Q.TipoCuota,
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
        ISNULL(B.Nombre, '') AS Banco,
        ISNULL(CB.NumeroCuenta, '') AS NumeroCuenta,
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

-- Version final: dbo.USP_TES_CXP_ANULAR
CREATE OR ALTER PROCEDURE dbo.USP_TES_CXP_ANULAR
    @IdCuentaPorPagar INT,
    @Usuario VARCHAR(80),
    @Motivo VARCHAR(500),
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @Resultado = 0;
    SET @Usuario = LTRIM(RTRIM(ISNULL(@Usuario, 'Sistema')));
    SET @Motivo = LTRIM(RTRIM(ISNULL(@Motivo, '')));

    IF @Motivo = ''
    BEGIN
        SET @Mensaje = 'Debe ingresar el motivo de anulacion.';
        RETURN;
    END;

    DECLARE @EstadoAnterior VARCHAR(30);

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @EstadoAnterior = Estado
        FROM dbo.TesCuentasPorPagar WITH (UPDLOCK, HOLDLOCK)
        WHERE IdCuentaPorPagar = @IdCuentaPorPagar;

        IF @EstadoAnterior IS NULL
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'La cuenta por pagar no existe.';
            RETURN;
        END;

        IF @EstadoAnterior = 'ANULADA'
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'La cuenta por pagar ya se encuentra anulada.';
            RETURN;
        END;

        IF EXISTS
        (
            SELECT 1
            FROM dbo.TesCuentaPorPagarPagos P
            INNER JOIN dbo.TesCuentaPorPagarCuotas C ON C.IdCuota = P.IdCuota
            WHERE C.IdCuentaPorPagar = @IdCuentaPorPagar
              AND P.Estado = 'ACTIVO'
        )
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'No se puede anular una cuenta por pagar con pagos activos.';
            RETURN;
        END;

        UPDATE dbo.TesCuentasPorPagar
        SET Estado = 'ANULADA',
            UsuarioAnulacion = @Usuario,
            FechaAnulacion = GETDATE(),
            MotivoAnulacion = @Motivo
        WHERE IdCuentaPorPagar = @IdCuentaPorPagar;

        UPDATE dbo.TesCuentaPorPagarDocumentos
        SET Estado = 'ANULADO'
        WHERE IdCuentaPorPagar = @IdCuentaPorPagar
          AND Estado <> 'ANULADO';

        UPDATE dbo.TesCuentaPorPagarCuotas
        SET Estado = 'ANULADA'
        WHERE IdCuentaPorPagar = @IdCuentaPorPagar
          AND Estado <> 'ANULADA';

        INSERT INTO dbo.TesCuentaPorPagarHistorial
        (
            IdCuentaPorPagar, Usuario, Accion, EstadoAnterior, EstadoNuevo, Descripcion
        )
        VALUES
        (
            @IdCuentaPorPagar, @Usuario, 'ANULACION', @EstadoAnterior, 'ANULADA', @Motivo
        );

        COMMIT TRANSACTION;

        SET @Resultado = 1;
        SET @Mensaje = 'Cuenta por pagar anulada correctamente.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH;
END;
GO

-- Version final: dbo.USP_TES_CXP_PROGRAMACION_RANGO
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
        Q.TipoCuota,
        DP.NumeroDocumento AS DocumentoPrincipal,
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
    OUTER APPLY
    (
        SELECT TOP (1) D.NumeroDocumento
        FROM dbo.TesCuentaPorPagarDocumentos D
        WHERE D.IdCuentaPorPagar = C.IdCuentaPorPagar
          AND D.Estado = 'ACTIVO'
          AND D.FactorEfecto = 1
        ORDER BY D.FechaDocumento, D.IdCuentaPorPagarDocumento
    ) DP
    WHERE Q.FechaVencimiento >= @FechaDesde
      AND Q.FechaVencimiento <= @FechaHasta
      AND C.Estado <> 'ANULADA'
      AND Q.Estado <> 'ANULADA'
      AND (@IdProveedor IS NULL OR C.IdProveedor = @IdProveedor)
      AND (
            (@Estado IS NULL AND Q.Estado IN ('PENDIENTE', 'PARCIAL', 'CANCELADA'))
            OR (@Estado IS NOT NULL AND Q.Estado = @Estado)
          )
    ORDER BY Q.FechaVencimiento, P.NombreRazonSocial, Q.NumeroCuota;
END;
GO

-- Version final: dbo.USP_TES_CXP_REGISTRAR_PAGO
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

-- Version final: dbo.USP_TES_CXP_ANULAR_PAGO
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
