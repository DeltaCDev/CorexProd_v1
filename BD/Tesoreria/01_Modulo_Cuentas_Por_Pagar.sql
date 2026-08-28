USE CorexProdDB;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.TesTiposObligacion', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TesTiposObligacion
    (
        IdTipoObligacion INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TesTiposObligacion PRIMARY KEY,
        Codigo VARCHAR(40) NOT NULL,
        Nombre VARCHAR(120) NOT NULL,
        Descripcion VARCHAR(300) NOT NULL CONSTRAINT DF_TesTiposObligacion_Descripcion DEFAULT(''),
        Estado BIT NOT NULL CONSTRAINT DF_TesTiposObligacion_Estado DEFAULT(1),
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_TesTiposObligacion_FechaRegistro DEFAULT(GETDATE())
    );

    CREATE UNIQUE INDEX UX_TesTiposObligacion_Codigo ON dbo.TesTiposObligacion(Codigo);
END;
GO

MERGE dbo.TesTiposObligacion AS T
USING (VALUES
    ('COMPRA_PRODUCTOS', 'Compra de productos', 'Obligaciones originadas por ingreso o compra de productos'),
    ('COMPRA_INSUMOS', 'Compra de insumos', 'Obligaciones originadas por ingreso o compra de insumos'),
    ('SERVICIO', 'Servicio', 'Obligaciones originadas por ordenes de servicio'),
    ('LETRA', 'Letra por pagar', 'Obligaciones documentadas con letras o cuotas'),
    ('OTROS', 'Otros', 'Obligaciones administrativas no clasificadas')
) AS S(Codigo, Nombre, Descripcion)
ON T.Codigo = S.Codigo
WHEN MATCHED THEN
    UPDATE SET Nombre = S.Nombre, Descripcion = S.Descripcion, Estado = 1
WHEN NOT MATCHED THEN
    INSERT (Codigo, Nombre, Descripcion, Estado)
    VALUES (S.Codigo, S.Nombre, S.Descripcion, 1);
GO

IF OBJECT_ID('dbo.TesBancos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TesBancos
    (
        IdBanco INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TesBancos PRIMARY KEY,
        Codigo VARCHAR(30) NOT NULL,
        Nombre VARCHAR(120) NOT NULL,
        Estado BIT NOT NULL CONSTRAINT DF_TesBancos_Estado DEFAULT(1),
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_TesBancos_FechaRegistro DEFAULT(GETDATE())
    );

    CREATE UNIQUE INDEX UX_TesBancos_Codigo ON dbo.TesBancos(Codigo);
END;
GO

MERGE dbo.TesBancos AS T
USING (VALUES
    ('BCP', 'Banco de Credito del Peru'),
    ('BBVA', 'BBVA'),
    ('INTERBANK', 'Interbank'),
    ('SCOTIABANK', 'Scotiabank'),
    ('BN', 'Banco de la Nacion')
) AS S(Codigo, Nombre)
ON T.Codigo = S.Codigo
WHEN MATCHED THEN
    UPDATE SET Nombre = S.Nombre, Estado = 1
WHEN NOT MATCHED THEN
    INSERT (Codigo, Nombre, Estado)
    VALUES (S.Codigo, S.Nombre, 1);
GO

IF OBJECT_ID('dbo.TesCuentasBancarias', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TesCuentasBancarias
    (
        IdCuentaBancaria INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TesCuentasBancarias PRIMARY KEY,
        IdBanco INT NOT NULL,
        NombreCuenta VARCHAR(120) NOT NULL,
        Titular VARCHAR(180) NOT NULL CONSTRAINT DF_TesCuentasBancarias_Titular DEFAULT(''),
        Moneda VARCHAR(10) NOT NULL CONSTRAINT DF_TesCuentasBancarias_Moneda DEFAULT('PEN'),
        NumeroCuenta VARCHAR(80) NOT NULL,
        Cci VARCHAR(80) NOT NULL CONSTRAINT DF_TesCuentasBancarias_Cci DEFAULT(''),
        Estado BIT NOT NULL CONSTRAINT DF_TesCuentasBancarias_Estado DEFAULT(1),
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_TesCuentasBancarias_FechaRegistro DEFAULT(GETDATE()),
        CONSTRAINT FK_TesCuentasBancarias_Banco FOREIGN KEY (IdBanco) REFERENCES dbo.TesBancos(IdBanco),
        CONSTRAINT CK_TesCuentasBancarias_Moneda CHECK (Moneda IN ('PEN', 'USD', 'EUR'))
    );

    CREATE INDEX IX_TesCuentasBancarias_Banco ON dbo.TesCuentasBancarias(IdBanco, Estado);
    CREATE INDEX IX_TesCuentasBancarias_Numero ON dbo.TesCuentasBancarias(NumeroCuenta);
END;
GO

IF OBJECT_ID('dbo.TesCuentasPorPagar', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TesCuentasPorPagar
    (
        IdCuentaPorPagar INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TesCuentasPorPagar PRIMARY KEY,
        IdProveedor INT NOT NULL,
        IdTipoObligacion INT NOT NULL,
        FechaDocumento DATE NOT NULL,
        Moneda VARCHAR(10) NOT NULL CONSTRAINT DF_TesCuentasPorPagar_Moneda DEFAULT('PEN'),
        ImporteTotal DECIMAL(18,2) NOT NULL,
        Estado VARCHAR(30) NOT NULL CONSTRAINT DF_TesCuentasPorPagar_Estado DEFAULT('PENDIENTE'),
        OrigenTipo VARCHAR(60) NOT NULL CONSTRAINT DF_TesCuentasPorPagar_OrigenTipo DEFAULT('MANUAL'),
        OrigenId INT NULL,
        Observacion VARCHAR(1000) NOT NULL CONSTRAINT DF_TesCuentasPorPagar_Observacion DEFAULT(''),
        UsuarioRegistro VARCHAR(80) NOT NULL CONSTRAINT DF_TesCuentasPorPagar_UsuarioRegistro DEFAULT('Sistema'),
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_TesCuentasPorPagar_FechaRegistro DEFAULT(GETDATE()),
        UsuarioModificacion VARCHAR(80) NULL,
        FechaModificacion DATETIME NULL,
        UsuarioAnulacion VARCHAR(80) NULL,
        FechaAnulacion DATETIME NULL,
        MotivoAnulacion VARCHAR(500) NULL,
        CONSTRAINT FK_TesCuentasPorPagar_Proveedor FOREIGN KEY (IdProveedor) REFERENCES dbo.Proveedores(IdProveedor),
        CONSTRAINT FK_TesCuentasPorPagar_TipoObligacion FOREIGN KEY (IdTipoObligacion) REFERENCES dbo.TesTiposObligacion(IdTipoObligacion),
        CONSTRAINT CK_TesCuentasPorPagar_ImporteTotal CHECK (ImporteTotal > 0),
        CONSTRAINT CK_TesCuentasPorPagar_Moneda CHECK (Moneda IN ('PEN', 'USD', 'EUR')),
        CONSTRAINT CK_TesCuentasPorPagar_Estado CHECK (Estado IN ('PENDIENTE', 'PARCIAL', 'PAGADA', 'ANULADA'))
    );
END;
GO

IF OBJECT_ID('dbo.TesCuentaPorPagarDocumentos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TesCuentaPorPagarDocumentos
    (
        IdCuentaPorPagarDocumento INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TesCuentaPorPagarDocumentos PRIMARY KEY,
        IdCuentaPorPagar INT NOT NULL,
        IdTipoDocumento INT NOT NULL,
        Serie VARCHAR(20) NOT NULL CONSTRAINT DF_TesCxpDocumentos_Serie DEFAULT(''),
        Numero VARCHAR(30) NOT NULL CONSTRAINT DF_TesCxpDocumentos_Numero DEFAULT(''),
        NumeroDocumento VARCHAR(60) NOT NULL,
        FechaDocumento DATE NOT NULL,
        Importe DECIMAL(18,2) NOT NULL,
        Observacion VARCHAR(500) NOT NULL CONSTRAINT DF_TesCxpDocumentos_Observacion DEFAULT(''),
        Estado VARCHAR(20) NOT NULL CONSTRAINT DF_TesCxpDocumentos_Estado DEFAULT('ACTIVO'),
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_TesCxpDocumentos_FechaRegistro DEFAULT(GETDATE()),
        CONSTRAINT FK_TesCxpDocumentos_Cuenta FOREIGN KEY (IdCuentaPorPagar) REFERENCES dbo.TesCuentasPorPagar(IdCuentaPorPagar),
        CONSTRAINT FK_TesCxpDocumentos_TipoDocumento FOREIGN KEY (IdTipoDocumento) REFERENCES dbo.TiposDocumentoStock(IdTipoDocumento),
        CONSTRAINT CK_TesCxpDocumentos_Importe CHECK (Importe > 0),
        CONSTRAINT CK_TesCxpDocumentos_Estado CHECK (Estado IN ('ACTIVO', 'ANULADO'))
    );
END;
GO

IF OBJECT_ID('dbo.TesCuentaPorPagarCuotas', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TesCuentaPorPagarCuotas
    (
        IdCuota INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TesCuentaPorPagarCuotas PRIMARY KEY,
        IdCuentaPorPagar INT NOT NULL,
        NumeroCuota INT NOT NULL,
        TotalCuotas INT NOT NULL,
        NumeroLetra VARCHAR(60) NULL,
        FechaGiro DATE NOT NULL,
        FechaVencimiento DATE NOT NULL,
        Importe DECIMAL(18,2) NOT NULL,
        Estado VARCHAR(30) NOT NULL CONSTRAINT DF_TesCxpCuotas_Estado DEFAULT('PENDIENTE'),
        Observacion VARCHAR(500) NOT NULL CONSTRAINT DF_TesCxpCuotas_Observacion DEFAULT(''),
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_TesCxpCuotas_FechaRegistro DEFAULT(GETDATE()),
        CONSTRAINT FK_TesCxpCuotas_Cuenta FOREIGN KEY (IdCuentaPorPagar) REFERENCES dbo.TesCuentasPorPagar(IdCuentaPorPagar),
        CONSTRAINT CK_TesCxpCuotas_Numero CHECK (NumeroCuota > 0 AND TotalCuotas > 0 AND NumeroCuota <= TotalCuotas),
        CONSTRAINT CK_TesCxpCuotas_Fechas CHECK (FechaVencimiento >= FechaGiro),
        CONSTRAINT CK_TesCxpCuotas_Importe CHECK (Importe > 0),
        CONSTRAINT CK_TesCxpCuotas_Estado CHECK (Estado IN ('PENDIENTE', 'PARCIAL', 'PAGADA', 'ANULADA'))
    );
END;
GO

IF OBJECT_ID('dbo.TesCuentaPorPagarPagos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TesCuentaPorPagarPagos
    (
        IdCuentaPorPagarPago INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TesCuentaPorPagarPagos PRIMARY KEY,
        IdCuota INT NOT NULL,
        FechaPago DATE NOT NULL,
        Importe DECIMAL(18,2) NOT NULL,
        MedioPago VARCHAR(60) NOT NULL,
        IdCuentaBancaria INT NULL,
        NumeroOperacion VARCHAR(80) NOT NULL CONSTRAINT DF_TesCxpPagos_NumeroOperacion DEFAULT(''),
        Observacion VARCHAR(500) NOT NULL CONSTRAINT DF_TesCxpPagos_Observacion DEFAULT(''),
        Estado VARCHAR(20) NOT NULL CONSTRAINT DF_TesCxpPagos_Estado DEFAULT('ACTIVO'),
        UsuarioRegistro VARCHAR(80) NOT NULL CONSTRAINT DF_TesCxpPagos_UsuarioRegistro DEFAULT('Sistema'),
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_TesCxpPagos_FechaRegistro DEFAULT(GETDATE()),
        UsuarioAnulacion VARCHAR(80) NULL,
        FechaAnulacion DATETIME NULL,
        MotivoAnulacion VARCHAR(500) NULL,
        CONSTRAINT FK_TesCxpPagos_Cuota FOREIGN KEY (IdCuota) REFERENCES dbo.TesCuentaPorPagarCuotas(IdCuota),
        CONSTRAINT FK_TesCxpPagos_CuentaBancaria FOREIGN KEY (IdCuentaBancaria) REFERENCES dbo.TesCuentasBancarias(IdCuentaBancaria),
        CONSTRAINT CK_TesCxpPagos_Importe CHECK (Importe > 0),
        CONSTRAINT CK_TesCxpPagos_Estado CHECK (Estado IN ('ACTIVO', 'ANULADO'))
    );
END;
GO

IF OBJECT_ID('dbo.TesCuentaPorPagarHistorial', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TesCuentaPorPagarHistorial
    (
        IdCuentaPorPagarHistorial BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TesCuentaPorPagarHistorial PRIMARY KEY,
        IdCuentaPorPagar INT NOT NULL,
        IdCuota INT NULL,
        Usuario VARCHAR(80) NOT NULL,
        Accion VARCHAR(120) NOT NULL,
        EstadoAnterior VARCHAR(30) NULL,
        EstadoNuevo VARCHAR(30) NULL,
        Descripcion VARCHAR(1000) NOT NULL CONSTRAINT DF_TesCxpHistorial_Descripcion DEFAULT(''),
        FechaHora DATETIME NOT NULL CONSTRAINT DF_TesCxpHistorial_FechaHora DEFAULT(GETDATE()),
        CONSTRAINT FK_TesCxpHistorial_Cuenta FOREIGN KEY (IdCuentaPorPagar) REFERENCES dbo.TesCuentasPorPagar(IdCuentaPorPagar),
        CONSTRAINT FK_TesCxpHistorial_Cuota FOREIGN KEY (IdCuota) REFERENCES dbo.TesCuentaPorPagarCuotas(IdCuota)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesCxp_Proveedor' AND object_id = OBJECT_ID('dbo.TesCuentasPorPagar'))
    CREATE INDEX IX_TesCxp_Proveedor ON dbo.TesCuentasPorPagar(IdProveedor, Estado);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesCxp_Estado' AND object_id = OBJECT_ID('dbo.TesCuentasPorPagar'))
    CREATE INDEX IX_TesCxp_Estado ON dbo.TesCuentasPorPagar(Estado);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesCxp_FechaDocumento' AND object_id = OBJECT_ID('dbo.TesCuentasPorPagar'))
    CREATE INDEX IX_TesCxp_FechaDocumento ON dbo.TesCuentasPorPagar(FechaDocumento);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesCxp_Origen' AND object_id = OBJECT_ID('dbo.TesCuentasPorPagar'))
    CREATE INDEX IX_TesCxp_Origen ON dbo.TesCuentasPorPagar(OrigenTipo, OrigenId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesCxpDocumentos_Cuenta' AND object_id = OBJECT_ID('dbo.TesCuentaPorPagarDocumentos'))
    CREATE INDEX IX_TesCxpDocumentos_Cuenta ON dbo.TesCuentaPorPagarDocumentos(IdCuentaPorPagar);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesCxpDocumentos_NumeroDocumento' AND object_id = OBJECT_ID('dbo.TesCuentaPorPagarDocumentos'))
    CREATE INDEX IX_TesCxpDocumentos_NumeroDocumento ON dbo.TesCuentaPorPagarDocumentos(NumeroDocumento);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesCxpCuotas_Cuenta' AND object_id = OBJECT_ID('dbo.TesCuentaPorPagarCuotas'))
    CREATE INDEX IX_TesCxpCuotas_Cuenta ON dbo.TesCuentaPorPagarCuotas(IdCuentaPorPagar, NumeroCuota);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesCxpCuotas_FechaVencimiento' AND object_id = OBJECT_ID('dbo.TesCuentaPorPagarCuotas'))
    CREATE INDEX IX_TesCxpCuotas_FechaVencimiento ON dbo.TesCuentaPorPagarCuotas(FechaVencimiento, Estado);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesCxpCuotas_NumeroLetra' AND object_id = OBJECT_ID('dbo.TesCuentaPorPagarCuotas'))
    CREATE INDEX IX_TesCxpCuotas_NumeroLetra ON dbo.TesCuentaPorPagarCuotas(NumeroLetra) WHERE NumeroLetra IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_TesCxpCuotas_Cuenta_NumeroLetra' AND object_id = OBJECT_ID('dbo.TesCuentaPorPagarCuotas'))
    CREATE UNIQUE INDEX UX_TesCxpCuotas_Cuenta_NumeroLetra
    ON dbo.TesCuentaPorPagarCuotas(IdCuentaPorPagar, NumeroLetra)
    WHERE NumeroLetra IS NOT NULL AND Estado <> 'ANULADA';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesCxpPagos_Cuota' AND object_id = OBJECT_ID('dbo.TesCuentaPorPagarPagos'))
    CREATE INDEX IX_TesCxpPagos_Cuota ON dbo.TesCuentaPorPagarPagos(IdCuota, Estado);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesCxpPagos_FechaPago' AND object_id = OBJECT_ID('dbo.TesCuentaPorPagarPagos'))
    CREATE INDEX IX_TesCxpPagos_FechaPago ON dbo.TesCuentaPorPagarPagos(FechaPago);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesCxpHistorial_Cuenta' AND object_id = OBJECT_ID('dbo.TesCuentaPorPagarHistorial'))
    CREATE INDEX IX_TesCxpHistorial_Cuenta ON dbo.TesCuentaPorPagarHistorial(IdCuentaPorPagar, FechaHora DESC);
GO

IF TYPE_ID('dbo.TesCuentaPorPagarDocumentoType') IS NULL
BEGIN
    CREATE TYPE dbo.TesCuentaPorPagarDocumentoType AS TABLE
    (
        IdTipoDocumento INT NOT NULL,
        Serie VARCHAR(20) NULL,
        Numero VARCHAR(30) NULL,
        NumeroDocumento VARCHAR(60) NOT NULL,
        FechaDocumento DATE NOT NULL,
        Importe DECIMAL(18,2) NOT NULL,
        Observacion VARCHAR(500) NULL
    );
END;
GO

IF TYPE_ID('dbo.TesCuentaPorPagarCuotaType') IS NULL
BEGIN
    CREATE TYPE dbo.TesCuentaPorPagarCuotaType AS TABLE
    (
        NumeroCuota INT NOT NULL,
        TotalCuotas INT NOT NULL,
        NumeroLetra VARCHAR(60) NULL,
        FechaGiro DATE NOT NULL,
        FechaVencimiento DATE NOT NULL,
        Importe DECIMAL(18,2) NOT NULL,
        Observacion VARCHAR(500) NULL
    );
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_TES_CXP_GUARDAR
    @IdCuentaPorPagar INT OUTPUT,
    @IdProveedor INT,
    @IdTipoObligacion INT,
    @FechaDocumento DATE,
    @Moneda VARCHAR(10),
    @ImporteTotal DECIMAL(18,2),
    @OrigenTipo VARCHAR(60) = 'MANUAL',
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

    SET @Resultado = 0;
    SET @Mensaje = '';
    SET @Moneda = UPPER(LTRIM(RTRIM(ISNULL(@Moneda, 'PEN'))));
    SET @OrigenTipo = UPPER(LTRIM(RTRIM(ISNULL(@OrigenTipo, 'MANUAL'))));
    SET @Observacion = LTRIM(RTRIM(ISNULL(@Observacion, '')));
    SET @Usuario = LTRIM(RTRIM(ISNULL(@Usuario, 'Sistema')));

    IF NOT EXISTS (SELECT 1 FROM dbo.Proveedores WHERE IdProveedor = @IdProveedor)
    BEGIN
        SET @Mensaje = 'El proveedor no existe.';
        RETURN;
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.TesTiposObligacion WHERE IdTipoObligacion = @IdTipoObligacion AND Estado = 1)
    BEGIN
        SET @Mensaje = 'El tipo de obligacion no existe o esta inactivo.';
        RETURN;
    END;

    IF @Moneda NOT IN ('PEN', 'USD', 'EUR')
    BEGIN
        SET @Mensaje = 'La moneda debe ser PEN, USD o EUR.';
        RETURN;
    END;

    IF @ImporteTotal <= 0
    BEGIN
        SET @Mensaje = 'El importe total debe ser mayor a cero.';
        RETURN;
    END;

    IF NOT EXISTS (SELECT 1 FROM @Cuotas)
    BEGIN
        SET @Mensaje = 'Debe registrar al menos una cuota.';
        RETURN;
    END;

    IF EXISTS (SELECT 1 FROM @Cuotas WHERE Importe <= 0)
    BEGIN
        SET @Mensaje = 'Los importes de las cuotas deben ser mayores a cero.';
        RETURN;
    END;

    IF EXISTS (SELECT 1 FROM @Cuotas WHERE FechaVencimiento < FechaGiro)
    BEGIN
        SET @Mensaje = 'La fecha de vencimiento no puede ser anterior a la fecha de giro.';
        RETURN;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM @Cuotas
        WHERE NumeroCuota <= 0
           OR TotalCuotas <= 0
           OR NumeroCuota > TotalCuotas
    )
    BEGIN
        SET @Mensaje = 'La numeracion de cuotas no es valida.';
        RETURN;
    END;

    IF EXISTS
    (
        SELECT NumeroLetra
        FROM @Cuotas
        WHERE NULLIF(LTRIM(RTRIM(NumeroLetra)), '') IS NOT NULL
        GROUP BY NumeroLetra
        HAVING COUNT(*) > 1
    )
    BEGIN
        SET @Mensaje = 'No se puede duplicar el numero de letra dentro de la misma cuenta.';
        RETURN;
    END;

    IF ROUND((SELECT SUM(Importe) FROM @Cuotas), 2) <> ROUND(@ImporteTotal, 2)
    BEGIN
        SET @Mensaje = 'La suma de cuotas debe ser igual al importe total.';
        RETURN;
    END;

    IF EXISTS (SELECT 1 FROM @Documentos WHERE Importe <= 0)
    BEGIN
        SET @Mensaje = 'Los importes de los documentos deben ser mayores a cero.';
        RETURN;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM @Documentos D
        WHERE NOT EXISTS (SELECT 1 FROM dbo.TiposDocumentoStock TD WHERE TD.IdTipoDocumento = D.IdTipoDocumento AND TD.Estado = 1)
    )
    BEGIN
        SET @Mensaje = 'Uno de los tipos de documento no existe o esta inactivo.';
        RETURN;
    END;

    DECLARE @EstadoAnterior VARCHAR(30) = NULL;
    DECLARE @Accion VARCHAR(120) = 'CREACION';

    BEGIN TRY
        BEGIN TRANSACTION;

        IF ISNULL(@IdCuentaPorPagar, 0) = 0
        BEGIN
            INSERT INTO dbo.TesCuentasPorPagar
            (
                IdProveedor, IdTipoObligacion, FechaDocumento, Moneda, ImporteTotal,
                Estado, OrigenTipo, OrigenId, Observacion, UsuarioRegistro, FechaRegistro
            )
            VALUES
            (
                @IdProveedor, @IdTipoObligacion, @FechaDocumento, @Moneda, @ImporteTotal,
                'PENDIENTE', @OrigenTipo, @OrigenId, @Observacion, @Usuario, GETDATE()
            );

            SET @IdCuentaPorPagar = SCOPE_IDENTITY();
        END
        ELSE
        BEGIN
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
                SET @Mensaje = 'No se puede modificar una cuenta por pagar anulada.';
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
                SET @Mensaje = 'No se puede modificar una cuenta por pagar con pagos registrados.';
                RETURN;
            END;

            SET @Accion = 'MODIFICACION';

            UPDATE dbo.TesCuentasPorPagar
            SET IdProveedor = @IdProveedor,
                IdTipoObligacion = @IdTipoObligacion,
                FechaDocumento = @FechaDocumento,
                Moneda = @Moneda,
                ImporteTotal = @ImporteTotal,
                Estado = 'PENDIENTE',
                OrigenTipo = @OrigenTipo,
                OrigenId = @OrigenId,
                Observacion = @Observacion,
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
        END;

        INSERT INTO dbo.TesCuentaPorPagarDocumentos
        (
            IdCuentaPorPagar, IdTipoDocumento, Serie, Numero, NumeroDocumento,
            FechaDocumento, Importe, Observacion, Estado
        )
        SELECT
            @IdCuentaPorPagar,
            IdTipoDocumento,
            LTRIM(RTRIM(ISNULL(Serie, ''))),
            LTRIM(RTRIM(ISNULL(Numero, ''))),
            LTRIM(RTRIM(NumeroDocumento)),
            FechaDocumento,
            Importe,
            LTRIM(RTRIM(ISNULL(Observacion, ''))),
            'ACTIVO'
        FROM @Documentos;

        INSERT INTO dbo.TesCuentaPorPagarCuotas
        (
            IdCuentaPorPagar, NumeroCuota, TotalCuotas, NumeroLetra,
            FechaGiro, FechaVencimiento, Importe, Estado, Observacion
        )
        SELECT
            @IdCuentaPorPagar,
            NumeroCuota,
            TotalCuotas,
            NULLIF(LTRIM(RTRIM(NumeroLetra)), ''),
            FechaGiro,
            FechaVencimiento,
            Importe,
            'PENDIENTE',
            LTRIM(RTRIM(ISNULL(Observacion, '')))
        FROM @Cuotas;

        INSERT INTO dbo.TesCuentaPorPagarHistorial
        (
            IdCuentaPorPagar, Usuario, Accion, EstadoAnterior, EstadoNuevo, Descripcion
        )
        VALUES
        (
            @IdCuentaPorPagar, @Usuario, @Accion, @EstadoAnterior, 'PENDIENTE',
            CASE WHEN @Accion = 'CREACION'
                THEN 'Cuenta por pagar registrada.'
                ELSE 'Cuenta por pagar modificada. Documentos y cuotas anteriores quedaron anulados logicamente.'
            END
        );

        COMMIT TRANSACTION;

        SET @Resultado = 1;
        SET @Mensaje = 'Cuenta por pagar guardada correctamente.';
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
    SET @Texto = NULLIF(LTRIM(RTRIM(ISNULL(@Texto, ''))), '');

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
      AND
      (
          @Texto IS NULL
          OR P.NombreRazonSocial LIKE '%' + @Texto + '%'
          OR P.NumeroDocumento LIKE '%' + @Texto + '%'
          OR C.OrigenTipo LIKE '%' + @Texto + '%'
          OR EXISTS
          (
              SELECT 1
              FROM dbo.TesCuentaPorPagarDocumentos D
              WHERE D.IdCuentaPorPagar = C.IdCuentaPorPagar
                AND D.Estado = 'ACTIVO'
                AND D.NumeroDocumento LIKE '%' + @Texto + '%'
          )
          OR EXISTS
          (
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
        C.Estado, C.OrigenTipo, C.OrigenId, C.Observacion, C.UsuarioRegistro, C.FechaRegistro,
        PG.TotalPagado
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
    ORDER BY Q.FechaVencimiento, P.NombreRazonSocial, Q.NumeroCuota;
END;
GO
