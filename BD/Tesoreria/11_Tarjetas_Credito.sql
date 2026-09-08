USE CorexProdDB;
GO

/*
    ETAPA 14 - Tarjetas de Credito / Obligaciones Financieras.
    Alcance: SQL inicial. No modifica WPF, C#, Excel, PDF ni Programacion General.

    Decision tecnica:
    - Se crean tablas propias para Tarjetas de Credito porque el dominio trabaja por
      periodos mensuales que pueden estar abiertos y sin monto final.
    - No se reutiliza TesObligacionFinancieraCuotas: una cuota de prestamo representa
      un cronograma fijo; un periodo de tarjeta representa un estado de cuenta mensual.
    - Se reutilizan maestros existentes: TesEntidadesFinancieras, TesBancos,
      TesCuentasBancarias y Empresas.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.TesTarjetasCredito', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TesTarjetasCredito
    (
        IdTarjetaCredito INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TesTarjetasCredito PRIMARY KEY,
        IdEntidadFinanciera INT NOT NULL,
        Titular VARCHAR(180) NOT NULL,
        IdEmpresaResponsable INT NULL,
        EmpresaResponsable VARCHAR(180) NOT NULL,
        Alias VARCHAR(120) NOT NULL,
        Ultimos4 VARCHAR(4) NULL,
        Moneda VARCHAR(10) NOT NULL CONSTRAINT DF_TesTarCred_Moneda DEFAULT('PEN'),
        DiaCorte TINYINT NULL,
        DiaPago TINYINT NULL,
        Estado VARCHAR(20) NOT NULL CONSTRAINT DF_TesTarCred_Estado DEFAULT('ACTIVA'),
        Activo BIT NOT NULL CONSTRAINT DF_TesTarCred_Activo DEFAULT(1),
        Observacion VARCHAR(1000) NOT NULL CONSTRAINT DF_TesTarCred_Observacion DEFAULT(''),
        FechaCreacion DATETIME NOT NULL CONSTRAINT DF_TesTarCred_FechaCreacion DEFAULT(GETDATE()),
        UsuarioCreacion VARCHAR(80) NOT NULL CONSTRAINT DF_TesTarCred_UsuarioCreacion DEFAULT('Sistema'),
        FechaModificacion DATETIME NULL,
        UsuarioModificacion VARCHAR(80) NULL,
        FechaAnulacion DATETIME NULL,
        UsuarioAnulacion VARCHAR(80) NULL,
        MotivoAnulacion VARCHAR(500) NULL,
        CONSTRAINT FK_TesTarCred_Entidad FOREIGN KEY (IdEntidadFinanciera) REFERENCES dbo.TesEntidadesFinancieras(IdEntidadFinanciera),
        CONSTRAINT FK_TesTarCred_EmpresaResponsable FOREIGN KEY (IdEmpresaResponsable) REFERENCES dbo.Empresas(IdEmpresa),
        CONSTRAINT CK_TesTarCred_Moneda CHECK (Moneda IN ('PEN', 'USD')),
        CONSTRAINT CK_TesTarCred_DiaCorte CHECK (DiaCorte IS NULL OR DiaCorte BETWEEN 1 AND 31),
        CONSTRAINT CK_TesTarCred_DiaPago CHECK (DiaPago IS NULL OR DiaPago BETWEEN 1 AND 31),
        CONSTRAINT CK_TesTarCred_Estado CHECK (Estado IN ('ACTIVA', 'INACTIVA', 'ANULADA')),
        CONSTRAINT CK_TesTarCred_Ultimos4 CHECK (Ultimos4 IS NULL OR (LEN(Ultimos4) = 4 AND Ultimos4 NOT LIKE '%[^0-9]%')),
        CONSTRAINT CK_TesTarCred_Activo_Estado CHECK ((Estado IN ('ACTIVA', 'INACTIVA') AND Activo IN (0, 1)) OR (Estado = 'ANULADA' AND Activo = 0))
    );
END;
GO

IF OBJECT_ID('dbo.TesTarjetaCreditoPeriodos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TesTarjetaCreditoPeriodos
    (
        IdTarjetaPeriodo INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TesTarjetaCreditoPeriodos PRIMARY KEY,
        IdTarjetaCredito INT NOT NULL,
        PeriodoAnio SMALLINT NOT NULL,
        PeriodoMes TINYINT NOT NULL,
        FechaCorte DATE NOT NULL,
        FechaVencimiento DATE NOT NULL,
        Monto DECIMAL(18,2) NULL,
        TotalPagado DECIMAL(18,2) NOT NULL CONSTRAINT DF_TesTarCredPer_TotalPagado DEFAULT(0),
        SaldoPendiente DECIMAL(18,2) NULL,
        Estado VARCHAR(30) NOT NULL CONSTRAINT DF_TesTarCredPer_Estado DEFAULT('ABIERTO'),
        Observacion VARCHAR(1000) NOT NULL CONSTRAINT DF_TesTarCredPer_Observacion DEFAULT(''),
        FechaCreacion DATETIME NOT NULL CONSTRAINT DF_TesTarCredPer_FechaCreacion DEFAULT(GETDATE()),
        UsuarioCreacion VARCHAR(80) NOT NULL CONSTRAINT DF_TesTarCredPer_UsuarioCreacion DEFAULT('Sistema'),
        FechaModificacion DATETIME NULL,
        UsuarioModificacion VARCHAR(80) NULL,
        FechaAnulacion DATETIME NULL,
        UsuarioAnulacion VARCHAR(80) NULL,
        MotivoAnulacion VARCHAR(500) NULL,
        CONSTRAINT FK_TesTarCredPer_Tarjeta FOREIGN KEY (IdTarjetaCredito) REFERENCES dbo.TesTarjetasCredito(IdTarjetaCredito),
        CONSTRAINT CK_TesTarCredPer_Periodo CHECK (PeriodoAnio BETWEEN 2000 AND 2100 AND PeriodoMes BETWEEN 1 AND 12),
        CONSTRAINT CK_TesTarCredPer_Fechas CHECK (FechaVencimiento >= FechaCorte),
        CONSTRAINT CK_TesTarCredPer_Estado CHECK (Estado IN ('ABIERTO', 'PENDIENTE', 'PARCIAL', 'CANCELADA', 'ANULADA')),
        CONSTRAINT CK_TesTarCredPer_MontoEstado CHECK
        (
            (Estado = 'ABIERTO' AND Monto IS NULL AND TotalPagado = 0 AND SaldoPendiente IS NULL)
            OR
            (Estado IN ('PENDIENTE', 'PARCIAL', 'CANCELADA') AND Monto > 0 AND TotalPagado >= 0 AND TotalPagado <= Monto AND SaldoPendiente >= 0 AND SaldoPendiente = Monto - TotalPagado)
            OR
            (Estado = 'ANULADA' AND (Monto IS NULL OR Monto > 0) AND TotalPagado >= 0 AND (SaldoPendiente IS NULL OR SaldoPendiente >= 0))
        )
    );
END;
GO

IF OBJECT_ID('dbo.TesTarjetaCreditoPagos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TesTarjetaCreditoPagos
    (
        IdTarjetaPago INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TesTarjetaCreditoPagos PRIMARY KEY,
        IdTarjetaPeriodo INT NOT NULL,
        FechaPago DATE NOT NULL,
        IdBanco INT NULL,
        IdCuentaBancaria INT NULL,
        Importe DECIMAL(18,2) NOT NULL,
        NumeroOperacion VARCHAR(80) NOT NULL CONSTRAINT DF_TesTarCredPagos_NumeroOperacion DEFAULT(''),
        Observacion VARCHAR(500) NOT NULL CONSTRAINT DF_TesTarCredPagos_Observacion DEFAULT(''),
        Estado VARCHAR(20) NOT NULL CONSTRAINT DF_TesTarCredPagos_Estado DEFAULT('ACTIVO'),
        FechaCreacion DATETIME NOT NULL CONSTRAINT DF_TesTarCredPagos_FechaCreacion DEFAULT(GETDATE()),
        UsuarioCreacion VARCHAR(80) NOT NULL CONSTRAINT DF_TesTarCredPagos_UsuarioCreacion DEFAULT('Sistema'),
        FechaAnulacion DATETIME NULL,
        UsuarioAnulacion VARCHAR(80) NULL,
        MotivoAnulacion VARCHAR(500) NULL,
        CONSTRAINT FK_TesTarCredPagos_Periodo FOREIGN KEY (IdTarjetaPeriodo) REFERENCES dbo.TesTarjetaCreditoPeriodos(IdTarjetaPeriodo),
        CONSTRAINT FK_TesTarCredPagos_Banco FOREIGN KEY (IdBanco) REFERENCES dbo.TesBancos(IdBanco),
        CONSTRAINT FK_TesTarCredPagos_CuentaBancaria FOREIGN KEY (IdCuentaBancaria) REFERENCES dbo.TesCuentasBancarias(IdCuentaBancaria),
        CONSTRAINT CK_TesTarCredPagos_Importe CHECK (Importe > 0),
        CONSTRAINT CK_TesTarCredPagos_Estado CHECK (Estado IN ('ACTIVO', 'ANULADO'))
    );
END;
GO

IF OBJECT_ID('dbo.TesTarjetaCreditoHistorial', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TesTarjetaCreditoHistorial
    (
        IdHistorialTarjetaCredito BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TesTarjetaCreditoHistorial PRIMARY KEY,
        IdTarjetaCredito INT NOT NULL,
        IdTarjetaPeriodo INT NULL,
        IdTarjetaPago INT NULL,
        Usuario VARCHAR(80) NOT NULL,
        Accion VARCHAR(120) NOT NULL,
        EstadoAnterior VARCHAR(30) NULL,
        EstadoNuevo VARCHAR(30) NULL,
        Descripcion VARCHAR(1000) NOT NULL CONSTRAINT DF_TesTarCredHist_Descripcion DEFAULT(''),
        FechaHora DATETIME NOT NULL CONSTRAINT DF_TesTarCredHist_FechaHora DEFAULT(GETDATE()),
        CONSTRAINT FK_TesTarCredHist_Tarjeta FOREIGN KEY (IdTarjetaCredito) REFERENCES dbo.TesTarjetasCredito(IdTarjetaCredito),
        CONSTRAINT FK_TesTarCredHist_Periodo FOREIGN KEY (IdTarjetaPeriodo) REFERENCES dbo.TesTarjetaCreditoPeriodos(IdTarjetaPeriodo),
        CONSTRAINT FK_TesTarCredHist_Pago FOREIGN KEY (IdTarjetaPago) REFERENCES dbo.TesTarjetaCreditoPagos(IdTarjetaPago)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesTarCred_Entidad' AND object_id = OBJECT_ID('dbo.TesTarjetasCredito'))
    CREATE INDEX IX_TesTarCred_Entidad ON dbo.TesTarjetasCredito(IdEntidadFinanciera, Estado);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesTarCred_EmpresaResponsable' AND object_id = OBJECT_ID('dbo.TesTarjetasCredito'))
    CREATE INDEX IX_TesTarCred_EmpresaResponsable ON dbo.TesTarjetasCredito(IdEmpresaResponsable, Estado);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesTarCred_Estado' AND object_id = OBJECT_ID('dbo.TesTarjetasCredito'))
    CREATE INDEX IX_TesTarCred_Estado ON dbo.TesTarjetasCredito(Estado, Activo);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesTarCredPer_Tarjeta' AND object_id = OBJECT_ID('dbo.TesTarjetaCreditoPeriodos'))
    CREATE INDEX IX_TesTarCredPer_Tarjeta ON dbo.TesTarjetaCreditoPeriodos(IdTarjetaCredito, PeriodoAnio, PeriodoMes);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_TesTarCredPer_Tarjeta_Periodo' AND object_id = OBJECT_ID('dbo.TesTarjetaCreditoPeriodos'))
    CREATE UNIQUE INDEX UX_TesTarCredPer_Tarjeta_Periodo ON dbo.TesTarjetaCreditoPeriodos(IdTarjetaCredito, PeriodoAnio, PeriodoMes);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesTarCredPer_FechaVencimiento' AND object_id = OBJECT_ID('dbo.TesTarjetaCreditoPeriodos'))
    CREATE INDEX IX_TesTarCredPer_FechaVencimiento ON dbo.TesTarjetaCreditoPeriodos(FechaVencimiento, Estado);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesTarCredPer_Estado' AND object_id = OBJECT_ID('dbo.TesTarjetaCreditoPeriodos'))
    CREATE INDEX IX_TesTarCredPer_Estado ON dbo.TesTarjetaCreditoPeriodos(Estado, PeriodoAnio, PeriodoMes);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesTarCredPagos_Periodo' AND object_id = OBJECT_ID('dbo.TesTarjetaCreditoPagos'))
    CREATE INDEX IX_TesTarCredPagos_Periodo ON dbo.TesTarjetaCreditoPagos(IdTarjetaPeriodo, Estado);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesTarCredPagos_FechaPago' AND object_id = OBJECT_ID('dbo.TesTarjetaCreditoPagos'))
    CREATE INDEX IX_TesTarCredPagos_FechaPago ON dbo.TesTarjetaCreditoPagos(FechaPago);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesTarCredPagos_Banco' AND object_id = OBJECT_ID('dbo.TesTarjetaCreditoPagos'))
    CREATE INDEX IX_TesTarCredPagos_Banco ON dbo.TesTarjetaCreditoPagos(IdBanco, Estado);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesTarCredHist_Tarjeta' AND object_id = OBJECT_ID('dbo.TesTarjetaCreditoHistorial'))
    CREATE INDEX IX_TesTarCredHist_Tarjeta ON dbo.TesTarjetaCreditoHistorial(IdTarjetaCredito, FechaHora DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesTarCredHist_Periodo' AND object_id = OBJECT_ID('dbo.TesTarjetaCreditoHistorial'))
    CREATE INDEX IX_TesTarCredHist_Periodo ON dbo.TesTarjetaCreditoHistorial(IdTarjetaPeriodo, FechaHora DESC);
GO

CREATE OR ALTER PROCEDURE dbo.USP_TES_TARJETA_LISTAR
    @Texto VARCHAR(160) = NULL,
    @Estado VARCHAR(20) = NULL,
    @IdEntidadFinanciera INT = NULL,
    @Moneda VARCHAR(10) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @Texto = NULLIF(LTRIM(RTRIM(ISNULL(@Texto, ''))), '');
    SET @Estado = NULLIF(UPPER(LTRIM(RTRIM(ISNULL(@Estado, '')))), '');
    SET @Moneda = NULLIF(UPPER(LTRIM(RTRIM(ISNULL(@Moneda, '')))), '');

    SELECT
        T.IdTarjetaCredito,
        T.IdEntidadFinanciera,
        E.Nombre AS EntidadFinanciera,
        T.Titular,
        T.IdEmpresaResponsable,
        T.EmpresaResponsable,
        T.Alias,
        T.Ultimos4,
        T.Moneda,
        T.DiaCorte,
        T.DiaPago,
        T.Estado,
        T.Activo,
        T.Observacion,
        T.FechaCreacion,
        T.UsuarioCreacion,
        T.FechaModificacion,
        T.UsuarioModificacion
    FROM dbo.TesTarjetasCredito T
    INNER JOIN dbo.TesEntidadesFinancieras E ON E.IdEntidadFinanciera = T.IdEntidadFinanciera
    WHERE (@Estado IS NULL OR T.Estado = @Estado)
      AND (@IdEntidadFinanciera IS NULL OR T.IdEntidadFinanciera = @IdEntidadFinanciera)
      AND (@Moneda IS NULL OR T.Moneda = @Moneda)
      AND
      (
          @Texto IS NULL
          OR E.Nombre LIKE '%' + @Texto + '%'
          OR T.Titular LIKE '%' + @Texto + '%'
          OR T.EmpresaResponsable LIKE '%' + @Texto + '%'
          OR T.Alias LIKE '%' + @Texto + '%'
          OR T.Ultimos4 LIKE '%' + @Texto + '%'
      )
    ORDER BY E.Nombre, T.Alias, T.Titular;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_TES_TARJETA_OBTENER
    @IdTarjetaCredito INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        T.IdTarjetaCredito,
        T.IdEntidadFinanciera,
        E.Nombre AS EntidadFinanciera,
        T.Titular,
        T.IdEmpresaResponsable,
        T.EmpresaResponsable,
        T.Alias,
        T.Ultimos4,
        T.Moneda,
        T.DiaCorte,
        T.DiaPago,
        T.Estado,
        T.Activo,
        T.Observacion,
        T.FechaCreacion,
        T.UsuarioCreacion,
        T.FechaModificacion,
        T.UsuarioModificacion,
        T.FechaAnulacion,
        T.UsuarioAnulacion,
        T.MotivoAnulacion
    FROM dbo.TesTarjetasCredito T
    INNER JOIN dbo.TesEntidadesFinancieras E ON E.IdEntidadFinanciera = T.IdEntidadFinanciera
    WHERE T.IdTarjetaCredito = @IdTarjetaCredito;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_TES_TARJETA_GUARDAR
    @IdTarjetaCredito INT OUTPUT,
    @IdEntidadFinanciera INT,
    @Titular VARCHAR(180),
    @IdEmpresaResponsable INT = NULL,
    @EmpresaResponsable VARCHAR(180),
    @Alias VARCHAR(120),
    @Ultimos4 VARCHAR(4) = NULL,
    @Moneda VARCHAR(10),
    @DiaCorte TINYINT = NULL,
    @DiaPago TINYINT = NULL,
    @Estado VARCHAR(20) = 'ACTIVA',
    @Observacion VARCHAR(1000) = '',
    @Usuario VARCHAR(80),
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @Resultado = 0;
    SET @Mensaje = '';
    SET @IdTarjetaCredito = ISNULL(@IdTarjetaCredito, 0);
    SET @Titular = LTRIM(RTRIM(ISNULL(@Titular, '')));
    SET @EmpresaResponsable = LTRIM(RTRIM(ISNULL(@EmpresaResponsable, '')));
    SET @Alias = LTRIM(RTRIM(ISNULL(@Alias, '')));
    SET @Ultimos4 = NULLIF(LTRIM(RTRIM(ISNULL(@Ultimos4, ''))), '');
    SET @Moneda = UPPER(LTRIM(RTRIM(ISNULL(@Moneda, 'PEN'))));
    SET @Estado = UPPER(LTRIM(RTRIM(ISNULL(@Estado, 'ACTIVA'))));
    SET @Observacion = LTRIM(RTRIM(ISNULL(@Observacion, '')));
    SET @Usuario = LTRIM(RTRIM(ISNULL(@Usuario, 'Sistema')));

    IF @Usuario = ''
    BEGIN
        SET @Mensaje = 'Debe indicar el usuario que registra la tarjeta.';
        RETURN;
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.TesEntidadesFinancieras WHERE IdEntidadFinanciera = @IdEntidadFinanciera AND Activo = 1)
    BEGIN
        SET @Mensaje = 'La entidad financiera no existe o esta inactiva.';
        RETURN;
    END;

    IF @Titular = ''
    BEGIN
        SET @Mensaje = 'Debe indicar el titular de la tarjeta.';
        RETURN;
    END;

    IF @IdEmpresaResponsable IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.Empresas WHERE IdEmpresa = @IdEmpresaResponsable AND Estado = 1)
    BEGIN
        SET @Mensaje = 'La empresa responsable no existe o esta inactiva.';
        RETURN;
    END;

    IF @EmpresaResponsable = '' AND @IdEmpresaResponsable IS NOT NULL
    BEGIN
        SELECT @EmpresaResponsable = Nombre
        FROM dbo.Empresas
        WHERE IdEmpresa = @IdEmpresaResponsable;
    END;

    IF @EmpresaResponsable = ''
    BEGIN
        SET @Mensaje = 'Debe indicar la empresa responsable del pago.';
        RETURN;
    END;

    IF @Alias = ''
    BEGIN
        SET @Mensaje = 'Debe indicar el alias de la tarjeta.';
        RETURN;
    END;

    IF @Ultimos4 IS NOT NULL AND (LEN(@Ultimos4) <> 4 OR @Ultimos4 LIKE '%[^0-9]%')
    BEGIN
        SET @Mensaje = 'Ultimos4 debe contener exactamente cuatro digitos.';
        RETURN;
    END;

    IF @Moneda NOT IN ('PEN', 'USD')
    BEGIN
        SET @Mensaje = 'La moneda debe ser PEN o USD.';
        RETURN;
    END;

    IF @DiaCorte IS NOT NULL AND (@DiaCorte < 1 OR @DiaCorte > 31)
    BEGIN
        SET @Mensaje = 'El dia de corte debe estar entre 1 y 31.';
        RETURN;
    END;

    IF @DiaPago IS NOT NULL AND (@DiaPago < 1 OR @DiaPago > 31)
    BEGIN
        SET @Mensaje = 'El dia de pago debe estar entre 1 y 31.';
        RETURN;
    END;

    IF @Estado NOT IN ('ACTIVA', 'INACTIVA')
    BEGIN
        SET @Mensaje = 'El estado administrativo debe ser ACTIVA o INACTIVA.';
        RETURN;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE
            @EstadoAnterior VARCHAR(20),
            @MonedaAnterior VARCHAR(10);

        IF @IdTarjetaCredito = 0
        BEGIN
            INSERT INTO dbo.TesTarjetasCredito
            (
                IdEntidadFinanciera,
                Titular,
                IdEmpresaResponsable,
                EmpresaResponsable,
                Alias,
                Ultimos4,
                Moneda,
                DiaCorte,
                DiaPago,
                Estado,
                Activo,
                Observacion,
                UsuarioCreacion
            )
            VALUES
            (
                @IdEntidadFinanciera,
                @Titular,
                @IdEmpresaResponsable,
                @EmpresaResponsable,
                @Alias,
                @Ultimos4,
                @Moneda,
                @DiaCorte,
                @DiaPago,
                @Estado,
                CASE WHEN @Estado = 'ACTIVA' THEN 1 ELSE 0 END,
                @Observacion,
                @Usuario
            );

            SET @IdTarjetaCredito = SCOPE_IDENTITY();

            INSERT INTO dbo.TesTarjetaCreditoHistorial
            (
                IdTarjetaCredito,
                Usuario,
                Accion,
                EstadoNuevo,
                Descripcion
            )
            VALUES
            (
                @IdTarjetaCredito,
                @Usuario,
                'CREACION_TARJETA',
                @Estado,
                'Tarjeta de credito creada.'
            );
        END
        ELSE
        BEGIN
            SELECT
                @EstadoAnterior = Estado,
                @MonedaAnterior = Moneda
            FROM dbo.TesTarjetasCredito WITH (UPDLOCK, HOLDLOCK)
            WHERE IdTarjetaCredito = @IdTarjetaCredito;

            IF @EstadoAnterior IS NULL
            BEGIN
                ROLLBACK TRANSACTION;
                SET @Mensaje = 'La tarjeta de credito no existe.';
                RETURN;
            END;

            IF @EstadoAnterior = 'ANULADA'
            BEGIN
                ROLLBACK TRANSACTION;
                SET @Mensaje = 'No se puede editar una tarjeta anulada.';
                RETURN;
            END;

            IF @Moneda <> @MonedaAnterior
               AND EXISTS (SELECT 1 FROM dbo.TesTarjetaCreditoPeriodos WHERE IdTarjetaCredito = @IdTarjetaCredito)
            BEGIN
                ROLLBACK TRANSACTION;
                SET @Mensaje = 'No se puede cambiar la moneda porque la tarjeta ya tiene periodos registrados.';
                RETURN;
            END;

            UPDATE dbo.TesTarjetasCredito
            SET IdEntidadFinanciera = @IdEntidadFinanciera,
                Titular = @Titular,
                IdEmpresaResponsable = @IdEmpresaResponsable,
                EmpresaResponsable = @EmpresaResponsable,
                Alias = @Alias,
                Ultimos4 = @Ultimos4,
                Moneda = @Moneda,
                DiaCorte = @DiaCorte,
                DiaPago = @DiaPago,
                Estado = @Estado,
                Activo = CASE WHEN @Estado = 'ACTIVA' THEN 1 ELSE 0 END,
                Observacion = @Observacion,
                FechaModificacion = GETDATE(),
                UsuarioModificacion = @Usuario
            WHERE IdTarjetaCredito = @IdTarjetaCredito;

            INSERT INTO dbo.TesTarjetaCreditoHistorial
            (
                IdTarjetaCredito,
                Usuario,
                Accion,
                EstadoAnterior,
                EstadoNuevo,
                Descripcion
            )
            VALUES
            (
                @IdTarjetaCredito,
                @Usuario,
                'EDICION_TARJETA',
                @EstadoAnterior,
                @Estado,
                'Datos administrativos de tarjeta actualizados.'
            );
        END;

        COMMIT TRANSACTION;

        SET @Resultado = 1;
        SET @Mensaje = 'Tarjeta de credito guardada correctamente.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH;

    SELECT @Resultado AS Resultado, @Mensaje AS Mensaje, @IdTarjetaCredito AS IdTarjetaCredito;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_TES_TARJETA_ANULAR
    @IdTarjetaCredito INT,
    @Usuario VARCHAR(80),
    @Motivo VARCHAR(500),
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @Resultado = 0;
    SET @Mensaje = '';
    SET @Usuario = LTRIM(RTRIM(ISNULL(@Usuario, 'Sistema')));
    SET @Motivo = LTRIM(RTRIM(ISNULL(@Motivo, '')));

    IF @Motivo = ''
    BEGIN
        SET @Mensaje = 'Debe ingresar el motivo de anulacion.';
        RETURN;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @EstadoAnterior VARCHAR(20);

        SELECT @EstadoAnterior = Estado
        FROM dbo.TesTarjetasCredito WITH (UPDLOCK, HOLDLOCK)
        WHERE IdTarjetaCredito = @IdTarjetaCredito;

        IF @EstadoAnterior IS NULL
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'La tarjeta de credito no existe.';
            RETURN;
        END;

        IF @EstadoAnterior = 'ANULADA'
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'La tarjeta de credito ya se encuentra anulada.';
            RETURN;
        END;

        IF EXISTS
        (
            SELECT 1
            FROM dbo.TesTarjetaCreditoPagos P
            INNER JOIN dbo.TesTarjetaCreditoPeriodos PR ON PR.IdTarjetaPeriodo = P.IdTarjetaPeriodo
            WHERE PR.IdTarjetaCredito = @IdTarjetaCredito
              AND P.Estado = 'ACTIVO'
        )
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'No se puede anular una tarjeta con pagos activos. Primero revierta los pagos.';
            RETURN;
        END;

        UPDATE dbo.TesTarjetasCredito
        SET Estado = 'ANULADA',
            Activo = 0,
            FechaAnulacion = GETDATE(),
            UsuarioAnulacion = @Usuario,
            MotivoAnulacion = @Motivo
        WHERE IdTarjetaCredito = @IdTarjetaCredito;

        UPDATE dbo.TesTarjetaCreditoPeriodos
        SET Estado = 'ANULADA',
            FechaAnulacion = GETDATE(),
            UsuarioAnulacion = @Usuario,
            MotivoAnulacion = @Motivo
        WHERE IdTarjetaCredito = @IdTarjetaCredito
          AND Estado <> 'ANULADA';

        INSERT INTO dbo.TesTarjetaCreditoHistorial
        (
            IdTarjetaCredito,
            Usuario,
            Accion,
            EstadoAnterior,
            EstadoNuevo,
            Descripcion
        )
        VALUES
        (
            @IdTarjetaCredito,
            @Usuario,
            'ANULACION_TARJETA',
            @EstadoAnterior,
            'ANULADA',
            @Motivo
        );

        COMMIT TRANSACTION;

        SET @Resultado = 1;
        SET @Mensaje = 'Tarjeta de credito anulada correctamente.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH;

    SELECT @Resultado AS Resultado, @Mensaje AS Mensaje;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_TES_TARJETA_PERIODO_LISTAR
    @IdTarjetaCredito INT = NULL,
    @Anio SMALLINT = NULL,
    @Estado VARCHAR(30) = NULL,
    @FechaDesde DATE = NULL,
    @FechaHasta DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @Estado = NULLIF(UPPER(LTRIM(RTRIM(ISNULL(@Estado, '')))), '');

    SELECT
        P.IdTarjetaPeriodo,
        P.IdTarjetaCredito,
        T.Alias,
        T.Ultimos4,
        T.Moneda,
        E.Nombre AS EntidadFinanciera,
        T.Titular,
        T.EmpresaResponsable,
        P.PeriodoAnio,
        P.PeriodoMes,
        P.FechaCorte,
        P.FechaVencimiento,
        P.Monto,
        P.TotalPagado,
        P.SaldoPendiente,
        P.Estado,
        P.Observacion,
        P.FechaCreacion,
        P.UsuarioCreacion,
        P.FechaModificacion,
        P.UsuarioModificacion
    FROM dbo.TesTarjetaCreditoPeriodos P
    INNER JOIN dbo.TesTarjetasCredito T ON T.IdTarjetaCredito = P.IdTarjetaCredito
    INNER JOIN dbo.TesEntidadesFinancieras E ON E.IdEntidadFinanciera = T.IdEntidadFinanciera
    WHERE (@IdTarjetaCredito IS NULL OR P.IdTarjetaCredito = @IdTarjetaCredito)
      AND (@Anio IS NULL OR P.PeriodoAnio = @Anio)
      AND (@Estado IS NULL OR P.Estado = @Estado)
      AND (@FechaDesde IS NULL OR P.FechaVencimiento >= @FechaDesde)
      AND (@FechaHasta IS NULL OR P.FechaVencimiento <= @FechaHasta)
    ORDER BY P.FechaVencimiento, E.Nombre, T.Alias, P.PeriodoAnio, P.PeriodoMes;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_TES_TARJETA_PERIODO_OBTENER
    @IdTarjetaPeriodo INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        P.IdTarjetaPeriodo,
        P.IdTarjetaCredito,
        T.Alias,
        T.Ultimos4,
        T.Moneda,
        E.Nombre AS EntidadFinanciera,
        T.Titular,
        T.EmpresaResponsable,
        P.PeriodoAnio,
        P.PeriodoMes,
        P.FechaCorte,
        P.FechaVencimiento,
        P.Monto,
        P.TotalPagado,
        P.SaldoPendiente,
        P.Estado,
        P.Observacion,
        P.FechaCreacion,
        P.UsuarioCreacion,
        P.FechaModificacion,
        P.UsuarioModificacion,
        P.FechaAnulacion,
        P.UsuarioAnulacion,
        P.MotivoAnulacion
    FROM dbo.TesTarjetaCreditoPeriodos P
    INNER JOIN dbo.TesTarjetasCredito T ON T.IdTarjetaCredito = P.IdTarjetaCredito
    INNER JOIN dbo.TesEntidadesFinancieras E ON E.IdEntidadFinanciera = T.IdEntidadFinanciera
    WHERE P.IdTarjetaPeriodo = @IdTarjetaPeriodo;

    SELECT
        PG.IdTarjetaPago,
        PG.IdTarjetaPeriodo,
        PG.FechaPago,
        PG.IdBanco,
        B.Nombre AS Banco,
        PG.IdCuentaBancaria,
        CB.NombreCuenta,
        PG.Importe,
        PG.NumeroOperacion,
        PG.Observacion,
        PG.Estado,
        PG.FechaCreacion,
        PG.UsuarioCreacion,
        PG.FechaAnulacion,
        PG.UsuarioAnulacion,
        PG.MotivoAnulacion
    FROM dbo.TesTarjetaCreditoPagos PG
    LEFT JOIN dbo.TesBancos B ON B.IdBanco = PG.IdBanco
    LEFT JOIN dbo.TesCuentasBancarias CB ON CB.IdCuentaBancaria = PG.IdCuentaBancaria
    WHERE PG.IdTarjetaPeriodo = @IdTarjetaPeriodo
    ORDER BY PG.FechaPago, PG.IdTarjetaPago;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_TES_TARJETA_PERIODO_GUARDAR
    @IdTarjetaPeriodo INT OUTPUT,
    @IdTarjetaCredito INT,
    @PeriodoAnio SMALLINT,
    @PeriodoMes TINYINT,
    @FechaCorte DATE,
    @FechaVencimiento DATE,
    @Monto DECIMAL(18,2) = NULL,
    @Estado VARCHAR(30) = NULL,
    @Observacion VARCHAR(1000) = '',
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

    SET @IdTarjetaPeriodo = ISNULL(@IdTarjetaPeriodo, 0);
    SET @Resultado = 0;
    SET @Mensaje = '';
    SET @TotalPagado = 0;
    SET @SaldoPendiente = 0;
    SET @EstadoPeriodo = '';
    SET @Estado = NULLIF(UPPER(LTRIM(RTRIM(ISNULL(@Estado, '')))), '');
    SET @Observacion = LTRIM(RTRIM(ISNULL(@Observacion, '')));
    SET @Usuario = LTRIM(RTRIM(ISNULL(@Usuario, 'Sistema')));

    IF @Usuario = ''
    BEGIN
        SET @Mensaje = 'Debe indicar el usuario que registra el periodo.';
        RETURN;
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.TesTarjetasCredito WHERE IdTarjetaCredito = @IdTarjetaCredito AND Estado = 'ACTIVA')
    BEGIN
        SET @Mensaje = 'La tarjeta de credito no existe o no esta activa.';
        RETURN;
    END;

    IF @PeriodoAnio NOT BETWEEN 2000 AND 2100 OR @PeriodoMes NOT BETWEEN 1 AND 12
    BEGIN
        SET @Mensaje = 'El periodo de tarjeta no es valido.';
        RETURN;
    END;

    IF @FechaCorte IS NULL OR @FechaVencimiento IS NULL OR @FechaVencimiento < @FechaCorte
    BEGIN
        SET @Mensaje = 'La fecha de vencimiento debe ser mayor o igual a la fecha de corte.';
        RETURN;
    END;

    IF @Estado IS NULL
        SET @Estado = CASE WHEN @Monto IS NULL THEN 'ABIERTO' ELSE 'PENDIENTE' END;

    IF @Estado NOT IN ('ABIERTO', 'PENDIENTE', 'PARCIAL', 'CANCELADA')
    BEGIN
        SET @Mensaje = 'El estado del periodo no es valido.';
        RETURN;
    END;

    IF @Estado = 'ABIERTO' AND @Monto IS NOT NULL
    BEGIN
        SET @Mensaje = 'Un periodo ABIERTO no debe tener monto definitivo.';
        RETURN;
    END;

    IF @Estado IN ('PENDIENTE', 'PARCIAL', 'CANCELADA') AND (ISNULL(@Monto, 0) <= 0)
    BEGIN
        SET @Mensaje = 'Un periodo exigible debe tener monto mayor a cero.';
        RETURN;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE
            @EstadoAnterior VARCHAR(30),
            @IdTarjetaAnterior INT,
            @MontoAnterior DECIMAL(18,2),
            @TienePagosActivos BIT = 0;

        IF @IdTarjetaPeriodo <> 0
        BEGIN
            SELECT
                @EstadoAnterior = Estado,
                @IdTarjetaAnterior = IdTarjetaCredito,
                @MontoAnterior = Monto
            FROM dbo.TesTarjetaCreditoPeriodos WITH (UPDLOCK, HOLDLOCK)
            WHERE IdTarjetaPeriodo = @IdTarjetaPeriodo;

            IF @EstadoAnterior IS NULL
            BEGIN
                ROLLBACK TRANSACTION;
                SET @Mensaje = 'El periodo de tarjeta no existe.';
                RETURN;
            END;

            IF @EstadoAnterior = 'ANULADA'
            BEGIN
                ROLLBACK TRANSACTION;
                SET @Mensaje = 'No se puede editar un periodo anulado.';
                RETURN;
            END;

            IF @IdTarjetaAnterior <> @IdTarjetaCredito
            BEGIN
                ROLLBACK TRANSACTION;
                SET @Mensaje = 'No se puede cambiar la tarjeta de un periodo existente.';
                RETURN;
            END;

            IF EXISTS (SELECT 1 FROM dbo.TesTarjetaCreditoPagos WHERE IdTarjetaPeriodo = @IdTarjetaPeriodo AND Estado = 'ACTIVO')
                SET @TienePagosActivos = 1;

            IF @TienePagosActivos = 1 AND ISNULL(@Monto, -1) <> ISNULL(@MontoAnterior, -1)
            BEGIN
                ROLLBACK TRANSACTION;
                SET @Mensaje = 'No se puede cambiar el monto de un periodo con pagos activos.';
                RETURN;
            END;
        END;

        IF EXISTS
        (
            SELECT 1
            FROM dbo.TesTarjetaCreditoPeriodos
            WHERE IdTarjetaCredito = @IdTarjetaCredito
              AND PeriodoAnio = @PeriodoAnio
              AND PeriodoMes = @PeriodoMes
              AND IdTarjetaPeriodo <> @IdTarjetaPeriodo
        )
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'Ya existe un periodo para la misma tarjeta, anio y mes.';
            RETURN;
        END;

        IF @IdTarjetaPeriodo = 0
        BEGIN
            SET @TotalPagado = 0;
            SET @SaldoPendiente = ISNULL(@Monto, 0);
            SET @EstadoPeriodo = CASE WHEN @Monto IS NULL THEN 'ABIERTO' ELSE 'PENDIENTE' END;

            INSERT INTO dbo.TesTarjetaCreditoPeriodos
            (
                IdTarjetaCredito,
                PeriodoAnio,
                PeriodoMes,
                FechaCorte,
                FechaVencimiento,
                Monto,
                TotalPagado,
                SaldoPendiente,
                Estado,
                Observacion,
                UsuarioCreacion
            )
            VALUES
            (
                @IdTarjetaCredito,
                @PeriodoAnio,
                @PeriodoMes,
                @FechaCorte,
                @FechaVencimiento,
                @Monto,
                0,
                CASE WHEN @Monto IS NULL THEN NULL ELSE @Monto END,
                @EstadoPeriodo,
                @Observacion,
                @Usuario
            );

            SET @IdTarjetaPeriodo = SCOPE_IDENTITY();

            INSERT INTO dbo.TesTarjetaCreditoHistorial
            (
                IdTarjetaCredito,
                IdTarjetaPeriodo,
                Usuario,
                Accion,
                EstadoNuevo,
                Descripcion
            )
            VALUES
            (
                @IdTarjetaCredito,
                @IdTarjetaPeriodo,
                @Usuario,
                'CREACION_PERIODO',
                @EstadoPeriodo,
                'Periodo de tarjeta creado.'
            );
        END
        ELSE
        BEGIN
            SELECT @TotalPagado = ISNULL(SUM(Importe), 0)
            FROM dbo.TesTarjetaCreditoPagos WITH (UPDLOCK, HOLDLOCK)
            WHERE IdTarjetaPeriodo = @IdTarjetaPeriodo
              AND Estado = 'ACTIVO';

            IF @Estado = 'ABIERTO' AND @TotalPagado > 0
            BEGIN
                ROLLBACK TRANSACTION;
                SET @Mensaje = 'Un periodo ABIERTO no puede tener pagos activos.';
                RETURN;
            END;

            IF @Monto IS NOT NULL AND @TotalPagado > @Monto
            BEGIN
                ROLLBACK TRANSACTION;
                SET @Mensaje = 'El monto del periodo no puede ser menor al total pagado.';
                RETURN;
            END;

            SET @EstadoPeriodo = CASE
                WHEN @Monto IS NULL THEN 'ABIERTO'
                WHEN @TotalPagado <= 0 THEN 'PENDIENTE'
                WHEN @TotalPagado < @Monto THEN 'PARCIAL'
                ELSE 'CANCELADA'
            END;
            SET @SaldoPendiente = CASE WHEN @Monto IS NULL THEN 0 ELSE @Monto - @TotalPagado END;

            UPDATE dbo.TesTarjetaCreditoPeriodos
            SET PeriodoAnio = @PeriodoAnio,
                PeriodoMes = @PeriodoMes,
                FechaCorte = @FechaCorte,
                FechaVencimiento = @FechaVencimiento,
                Monto = @Monto,
                TotalPagado = @TotalPagado,
                SaldoPendiente = CASE WHEN @Monto IS NULL THEN NULL ELSE @SaldoPendiente END,
                Estado = @EstadoPeriodo,
                Observacion = @Observacion,
                FechaModificacion = GETDATE(),
                UsuarioModificacion = @Usuario
            WHERE IdTarjetaPeriodo = @IdTarjetaPeriodo;

            INSERT INTO dbo.TesTarjetaCreditoHistorial
            (
                IdTarjetaCredito,
                IdTarjetaPeriodo,
                Usuario,
                Accion,
                EstadoAnterior,
                EstadoNuevo,
                Descripcion
            )
            VALUES
            (
                @IdTarjetaCredito,
                @IdTarjetaPeriodo,
                @Usuario,
                CASE WHEN @EstadoAnterior = 'ABIERTO' AND @EstadoPeriodo <> 'ABIERTO' THEN 'CIERRE_PERIODO' ELSE 'EDICION_PERIODO' END,
                @EstadoAnterior,
                @EstadoPeriodo,
                'Periodo de tarjeta actualizado.'
            );
        END;

        COMMIT TRANSACTION;

        SET @Resultado = 1;
        SET @Mensaje = 'Periodo de tarjeta guardado correctamente.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH;

    SELECT
        @Resultado AS Resultado,
        @Mensaje AS Mensaje,
        @IdTarjetaPeriodo AS IdTarjetaPeriodo,
        @TotalPagado AS TotalPagado,
        @SaldoPendiente AS SaldoPendiente,
        @EstadoPeriodo AS EstadoPeriodo;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_TES_TARJETA_PERIODO_ANULAR
    @IdTarjetaPeriodo INT,
    @Usuario VARCHAR(80),
    @Motivo VARCHAR(500),
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @Resultado = 0;
    SET @Mensaje = '';
    SET @Usuario = LTRIM(RTRIM(ISNULL(@Usuario, 'Sistema')));
    SET @Motivo = LTRIM(RTRIM(ISNULL(@Motivo, '')));

    IF @Motivo = ''
    BEGIN
        SET @Mensaje = 'Debe ingresar el motivo de anulacion del periodo.';
        RETURN;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE
            @IdTarjetaCredito INT,
            @EstadoAnterior VARCHAR(30);

        SELECT
            @IdTarjetaCredito = IdTarjetaCredito,
            @EstadoAnterior = Estado
        FROM dbo.TesTarjetaCreditoPeriodos WITH (UPDLOCK, HOLDLOCK)
        WHERE IdTarjetaPeriodo = @IdTarjetaPeriodo;

        IF @IdTarjetaCredito IS NULL
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'El periodo de tarjeta no existe.';
            RETURN;
        END;

        IF @EstadoAnterior = 'ANULADA'
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'El periodo ya se encuentra anulado.';
            RETURN;
        END;

        IF EXISTS (SELECT 1 FROM dbo.TesTarjetaCreditoPagos WHERE IdTarjetaPeriodo = @IdTarjetaPeriodo AND Estado = 'ACTIVO')
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'No se puede anular un periodo con pagos activos. Primero revierta los pagos.';
            RETURN;
        END;

        UPDATE dbo.TesTarjetaCreditoPeriodos
        SET Estado = 'ANULADA',
            FechaAnulacion = GETDATE(),
            UsuarioAnulacion = @Usuario,
            MotivoAnulacion = @Motivo
        WHERE IdTarjetaPeriodo = @IdTarjetaPeriodo;

        INSERT INTO dbo.TesTarjetaCreditoHistorial
        (
            IdTarjetaCredito,
            IdTarjetaPeriodo,
            Usuario,
            Accion,
            EstadoAnterior,
            EstadoNuevo,
            Descripcion
        )
        VALUES
        (
            @IdTarjetaCredito,
            @IdTarjetaPeriodo,
            @Usuario,
            'ANULACION_PERIODO',
            @EstadoAnterior,
            'ANULADA',
            @Motivo
        );

        COMMIT TRANSACTION;

        SET @Resultado = 1;
        SET @Mensaje = 'Periodo de tarjeta anulado correctamente.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH;

    SELECT @Resultado AS Resultado, @Mensaje AS Mensaje;
END;
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

        IF @EstadoTarjeta <> 'ACTIVA'
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'No se puede registrar pagos sobre una tarjeta no activa.';
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
            IdTarjetaPeriodo,
            FechaPago,
            IdBanco,
            IdCuentaBancaria,
            Importe,
            NumeroOperacion,
            Observacion,
            Estado,
            UsuarioCreacion
        )
        VALUES
        (
            @IdTarjetaPeriodo,
            @FechaPago,
            @IdBanco,
            @IdCuentaBancaria,
            @Importe,
            @NumeroOperacion,
            @Observacion,
            'ACTIVO',
            @Usuario
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
            IdTarjetaCredito,
            IdTarjetaPeriodo,
            IdTarjetaPago,
            Usuario,
            Accion,
            EstadoAnterior,
            EstadoNuevo,
            Descripcion
        )
        VALUES
        (
            @IdTarjetaCredito,
            @IdTarjetaPeriodo,
            @IdTarjetaPago,
            @Usuario,
            'REGISTRO_PAGO',
            @EstadoAnterior,
            @EstadoPeriodo,
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

    SELECT
        @Resultado AS Resultado,
        @Mensaje AS Mensaje,
        @IdTarjetaPago AS IdTarjetaPago,
        @TotalPagado AS TotalPagado,
        @SaldoPendiente AS SaldoPendiente,
        @EstadoPeriodo AS EstadoPeriodo;
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
            @EstadoPago VARCHAR(20),
            @EstadoAnterior VARCHAR(30),
            @Monto DECIMAL(18,2);

        SELECT
            @IdTarjetaPeriodo = P.IdTarjetaPeriodo,
            @IdTarjetaCredito = PR.IdTarjetaCredito,
            @EstadoPago = P.Estado,
            @EstadoAnterior = PR.Estado,
            @Monto = PR.Monto
        FROM dbo.TesTarjetaCreditoPagos P WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN dbo.TesTarjetaCreditoPeriodos PR WITH (UPDLOCK, HOLDLOCK) ON PR.IdTarjetaPeriodo = P.IdTarjetaPeriodo
        WHERE P.IdTarjetaPago = @IdTarjetaPago;

        IF @IdTarjetaPeriodo IS NULL
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'El pago seleccionado no existe.';
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
            IdTarjetaCredito,
            IdTarjetaPeriodo,
            IdTarjetaPago,
            Usuario,
            Accion,
            EstadoAnterior,
            EstadoNuevo,
            Descripcion
        )
        VALUES
        (
            @IdTarjetaCredito,
            @IdTarjetaPeriodo,
            @IdTarjetaPago,
            @Usuario,
            'ANULACION_PAGO',
            @EstadoAnterior,
            @EstadoPeriodo,
            @Motivo
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

    SELECT
        @Resultado AS Resultado,
        @Mensaje AS Mensaje,
        @IdTarjetaPago AS IdTarjetaPago,
        @TotalPagado AS TotalPagado,
        @SaldoPendiente AS SaldoPendiente,
        @EstadoPeriodo AS EstadoPeriodo;
END;
GO
