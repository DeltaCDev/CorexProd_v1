USE CorexProdDB;
GO

IF COL_LENGTH('dbo.TesCuentaPorPagarDocumentos', 'FactorEfecto') IS NULL
BEGIN
    ALTER TABLE dbo.TesCuentaPorPagarDocumentos
    ADD FactorEfecto SMALLINT NOT NULL
        CONSTRAINT DF_TesCxpDocumentos_FactorEfecto DEFAULT (1);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_TesCxpDocumentos_FactorEfecto'
      AND parent_object_id = OBJECT_ID('dbo.TesCuentaPorPagarDocumentos')
)
BEGIN
    ALTER TABLE dbo.TesCuentaPorPagarDocumentos WITH CHECK
    ADD CONSTRAINT CK_TesCxpDocumentos_FactorEfecto CHECK (FactorEfecto IN (1, -1));
END;
GO

IF COL_LENGTH('dbo.TesCuentaPorPagarCuotas', 'TipoCuota') IS NULL
BEGIN
    ALTER TABLE dbo.TesCuentaPorPagarCuotas
    ADD TipoCuota VARCHAR(20) NOT NULL
        CONSTRAINT DF_TesCxpCuotas_TipoCuota DEFAULT ('LETRA');
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_TesCxpCuotas_TipoCuota'
      AND parent_object_id = OBJECT_ID('dbo.TesCuentaPorPagarCuotas')
)
BEGIN
    ALTER TABLE dbo.TesCuentaPorPagarCuotas WITH CHECK
    ADD CONSTRAINT CK_TesCxpCuotas_TipoCuota CHECK (TipoCuota IN ('LETRA', 'CUOTA_FACTURA'));
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.TiposDocumentoStock WHERE UPPER(NombreTipoDocumento) IN ('NOTA DE CREDITO', 'NOTA DE CRÉDITO'))
BEGIN
    INSERT INTO dbo.TiposDocumentoStock (NombreTipoDocumento, Estado)
    VALUES ('Nota de credito', 1);
END
ELSE
BEGIN
    UPDATE dbo.TiposDocumentoStock
    SET Estado = 1
    WHERE UPPER(NombreTipoDocumento) IN ('NOTA DE CREDITO', 'NOTA DE CRÉDITO');
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.TesTiposObligacion WHERE Codigo = 'FACTURA_CREDITO')
BEGIN
    INSERT INTO dbo.TesTiposObligacion (Codigo, Nombre, Descripcion, Estado)
    VALUES ('FACTURA_CREDITO', 'Factura a credito', 'Obligaciones pagadas al proveedor segun vencimientos de factura', 1);
END
ELSE
BEGIN
    UPDATE dbo.TesTiposObligacion
    SET Nombre = 'Factura a credito',
        Descripcion = 'Obligaciones pagadas al proveedor segun vencimientos de factura',
        Estado = 1
    WHERE Codigo = 'FACTURA_CREDITO';
END;
GO

IF OBJECT_ID('dbo.USP_TES_CXP_GUARDAR', 'P') IS NOT NULL
    DROP PROCEDURE dbo.USP_TES_CXP_GUARDAR;
GO

IF TYPE_ID('dbo.TesCuentaPorPagarDocumentoType') IS NOT NULL
    DROP TYPE dbo.TesCuentaPorPagarDocumentoType;
GO

CREATE TYPE dbo.TesCuentaPorPagarDocumentoType AS TABLE
(
    IdTipoDocumento INT NOT NULL,
    Serie VARCHAR(20) NULL,
    Numero VARCHAR(30) NULL,
    NumeroDocumento VARCHAR(60) NOT NULL,
    FechaDocumento DATE NOT NULL,
    Importe DECIMAL(18,2) NOT NULL,
    FactorEfecto SMALLINT NOT NULL,
    Observacion VARCHAR(500) NULL
);
GO

IF TYPE_ID('dbo.TesCuentaPorPagarCuotaType') IS NOT NULL
    DROP TYPE dbo.TesCuentaPorPagarCuotaType;
GO

CREATE TYPE dbo.TesCuentaPorPagarCuotaType AS TABLE
(
    NumeroCuota INT NOT NULL,
    TotalCuotas INT NOT NULL,
    NumeroLetra VARCHAR(50) NULL,
    TipoCuota VARCHAR(20) NOT NULL,
    FechaGiro DATE NULL,
    FechaVencimiento DATE NOT NULL,
    Importe DECIMAL(18,2) NOT NULL,
    Observacion VARCHAR(500) NULL
);
GO

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
