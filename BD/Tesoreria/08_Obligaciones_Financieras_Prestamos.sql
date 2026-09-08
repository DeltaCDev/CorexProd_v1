USE CorexProdDB;
GO

/*
    ETAPA 9 - Modelo SQL para Obligaciones Financieras / Prestamos.
    Alcance: tablas y procedimientos base. No modifica CxP, WPF, C#, API, menus ni permisos.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.TesTiposObligacionFinanciera', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TesTiposObligacionFinanciera
    (
        IdTipoObligacionFinanciera INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TesTiposObligacionFinanciera PRIMARY KEY,
        Codigo VARCHAR(40) NOT NULL,
        Nombre VARCHAR(120) NOT NULL,
        Descripcion VARCHAR(300) NOT NULL CONSTRAINT DF_TesTiposObFin_Descripcion DEFAULT(''),
        Activo BIT NOT NULL CONSTRAINT DF_TesTiposObFin_Activo DEFAULT(1),
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_TesTiposObFin_FechaRegistro DEFAULT(GETDATE())
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_TesTiposObFin_Codigo' AND object_id = OBJECT_ID('dbo.TesTiposObligacionFinanciera'))
    CREATE UNIQUE INDEX UX_TesTiposObFin_Codigo ON dbo.TesTiposObligacionFinanciera(Codigo);
GO

MERGE dbo.TesTiposObligacionFinanciera AS T
USING (VALUES
    ('PRESTAMO', 'Prestamo', 'Obligaciones financieras originadas por prestamos bancarios, financieros o personales')
) AS S(Codigo, Nombre, Descripcion)
ON T.Codigo = S.Codigo
WHEN MATCHED THEN
    UPDATE SET Nombre = S.Nombre, Descripcion = S.Descripcion, Activo = 1
WHEN NOT MATCHED THEN
    INSERT (Codigo, Nombre, Descripcion, Activo)
    VALUES (S.Codigo, S.Nombre, S.Descripcion, 1);
GO

IF OBJECT_ID('dbo.TesEntidadesFinancieras', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TesEntidadesFinancieras
    (
        IdEntidadFinanciera INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TesEntidadesFinancieras PRIMARY KEY,
        TipoEntidad VARCHAR(30) NOT NULL,
        Nombre VARCHAR(160) NOT NULL,
        Ruc VARCHAR(20) NOT NULL CONSTRAINT DF_TesEntFin_Ruc DEFAULT(''),
        Activo BIT NOT NULL CONSTRAINT DF_TesEntFin_Activo DEFAULT(1),
        Observacion VARCHAR(300) NOT NULL CONSTRAINT DF_TesEntFin_Observacion DEFAULT(''),
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_TesEntFin_FechaRegistro DEFAULT(GETDATE()),
        CONSTRAINT CK_TesEntFin_TipoEntidad CHECK (TipoEntidad IN ('BANCO', 'FINANCIERA', 'TARJETA_RETAIL', 'ASEGURADORA', 'PERSONA', 'OTRO'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_TesEntFin_Tipo_Nombre' AND object_id = OBJECT_ID('dbo.TesEntidadesFinancieras'))
    CREATE UNIQUE INDEX UX_TesEntFin_Tipo_Nombre ON dbo.TesEntidadesFinancieras(TipoEntidad, Nombre);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesEntFin_Activo' AND object_id = OBJECT_ID('dbo.TesEntidadesFinancieras'))
    CREATE INDEX IX_TesEntFin_Activo ON dbo.TesEntidadesFinancieras(Activo, TipoEntidad, Nombre);
GO

MERGE dbo.TesEntidadesFinancieras AS T
USING (VALUES
    ('BANCO', 'Banco de Credito del Peru'),
    ('BANCO', 'BBVA'),
    ('BANCO', 'Interbank'),
    ('BANCO', 'Scotiabank'),
    ('TARJETA_RETAIL', 'Banco Falabella'),
    ('TARJETA_RETAIL', 'Banco Ripley')
) AS S(TipoEntidad, Nombre)
ON T.TipoEntidad = S.TipoEntidad AND T.Nombre = S.Nombre
WHEN MATCHED THEN
    UPDATE SET Activo = 1
WHEN NOT MATCHED THEN
    INSERT (TipoEntidad, Nombre, Activo)
    VALUES (S.TipoEntidad, S.Nombre, 1);
GO

IF OBJECT_ID('dbo.TesObligacionesFinancieras', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TesObligacionesFinancieras
    (
        IdObligacionFinanciera INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TesObligacionesFinancieras PRIMARY KEY,
        IdTipoObligacionFinanciera INT NOT NULL,
        IdEntidadFinanciera INT NOT NULL,
        TipoTitular VARCHAR(20) NOT NULL,
        IdTitular INT NULL,
        NombreTitular VARCHAR(180) NOT NULL,
        IdEmpresaResponsable INT NULL,
        CodigoReferencia VARCHAR(80) NOT NULL CONSTRAINT DF_TesObFin_CodigoReferencia DEFAULT(''),
        NumeroContrato VARCHAR(80) NOT NULL CONSTRAINT DF_TesObFin_NumeroContrato DEFAULT(''),
        NumeroOperacion VARCHAR(80) NOT NULL CONSTRAINT DF_TesObFin_NumeroOperacion DEFAULT(''),
        Concepto VARCHAR(250) NOT NULL,
        Moneda VARCHAR(10) NOT NULL CONSTRAINT DF_TesObFin_Moneda DEFAULT('PEN'),
        MontoOriginal DECIMAL(18,2) NULL,
        FechaInicio DATE NOT NULL,
        TotalCuotas INT NOT NULL,
        Estado VARCHAR(30) NOT NULL CONSTRAINT DF_TesObFin_Estado DEFAULT('PENDIENTE'),
        OrigenTipo VARCHAR(60) NOT NULL CONSTRAINT DF_TesObFin_OrigenTipo DEFAULT('MANUAL'),
        OrigenId INT NULL,
        Observacion VARCHAR(1000) NOT NULL CONSTRAINT DF_TesObFin_Observacion DEFAULT(''),
        UsuarioRegistro VARCHAR(80) NOT NULL CONSTRAINT DF_TesObFin_UsuarioRegistro DEFAULT('Sistema'),
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_TesObFin_FechaRegistro DEFAULT(GETDATE()),
        UsuarioModificacion VARCHAR(80) NULL,
        FechaModificacion DATETIME NULL,
        UsuarioAnulacion VARCHAR(80) NULL,
        FechaAnulacion DATETIME NULL,
        MotivoAnulacion VARCHAR(500) NULL,
        CONSTRAINT FK_TesObFin_Tipo FOREIGN KEY (IdTipoObligacionFinanciera) REFERENCES dbo.TesTiposObligacionFinanciera(IdTipoObligacionFinanciera),
        CONSTRAINT FK_TesObFin_Entidad FOREIGN KEY (IdEntidadFinanciera) REFERENCES dbo.TesEntidadesFinancieras(IdEntidadFinanciera),
        CONSTRAINT FK_TesObFin_EmpresaResponsable FOREIGN KEY (IdEmpresaResponsable) REFERENCES dbo.Empresas(IdEmpresa),
        CONSTRAINT CK_TesObFin_TipoTitular CHECK (TipoTitular IN ('EMPRESA', 'PERSONA', 'OTRO')),
        CONSTRAINT CK_TesObFin_Moneda CHECK (Moneda IN ('PEN', 'USD')),
        CONSTRAINT CK_TesObFin_MontoOriginal CHECK (MontoOriginal IS NULL OR MontoOriginal > 0),
        CONSTRAINT CK_TesObFin_TotalCuotas CHECK (TotalCuotas > 0),
        CONSTRAINT CK_TesObFin_Estado CHECK (Estado IN ('PENDIENTE', 'PARCIAL', 'CANCELADA', 'ANULADA'))
    );
END;
GO

IF OBJECT_ID('dbo.TesObligacionFinancieraCuotas', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TesObligacionFinancieraCuotas
    (
        IdCuotaObligacionFinanciera INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TesObFinCuotas PRIMARY KEY,
        IdObligacionFinanciera INT NOT NULL,
        NumeroCuota INT NOT NULL,
        TotalCuotas INT NOT NULL,
        FechaVencimiento DATE NOT NULL,
        Importe DECIMAL(18,2) NOT NULL,
        TotalPagado DECIMAL(18,2) NOT NULL CONSTRAINT DF_TesObFinCuotas_TotalPagado DEFAULT(0),
        SaldoPendiente DECIMAL(18,2) NOT NULL,
        Estado VARCHAR(30) NOT NULL CONSTRAINT DF_TesObFinCuotas_Estado DEFAULT('PENDIENTE'),
        Observacion VARCHAR(500) NOT NULL CONSTRAINT DF_TesObFinCuotas_Observacion DEFAULT(''),
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_TesObFinCuotas_FechaRegistro DEFAULT(GETDATE()),
        CONSTRAINT FK_TesObFinCuotas_Obligacion FOREIGN KEY (IdObligacionFinanciera) REFERENCES dbo.TesObligacionesFinancieras(IdObligacionFinanciera),
        CONSTRAINT CK_TesObFinCuotas_Numero CHECK (NumeroCuota > 0 AND TotalCuotas > 0 AND NumeroCuota <= TotalCuotas),
        CONSTRAINT CK_TesObFinCuotas_Importe CHECK (Importe > 0),
        CONSTRAINT CK_TesObFinCuotas_Totales CHECK (TotalPagado >= 0 AND SaldoPendiente >= 0 AND SaldoPendiente <= Importe),
        CONSTRAINT CK_TesObFinCuotas_Estado CHECK (Estado IN ('PENDIENTE', 'PARCIAL', 'CANCELADA', 'ANULADA'))
    );
END;
GO

IF OBJECT_ID('dbo.TesObligacionFinancieraPagos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TesObligacionFinancieraPagos
    (
        IdPagoObligacionFinanciera INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TesObFinPagos PRIMARY KEY,
        IdCuotaObligacionFinanciera INT NOT NULL,
        FechaPago DATE NOT NULL,
        Monto DECIMAL(18,2) NOT NULL,
        IdBanco INT NULL,
        IdCuentaBancaria INT NULL,
        NumeroOperacion VARCHAR(80) NOT NULL CONSTRAINT DF_TesObFinPagos_NumeroOperacion DEFAULT(''),
        Observacion VARCHAR(500) NOT NULL CONSTRAINT DF_TesObFinPagos_Observacion DEFAULT(''),
        Estado VARCHAR(20) NOT NULL CONSTRAINT DF_TesObFinPagos_Estado DEFAULT('ACTIVO'),
        UsuarioRegistro VARCHAR(80) NOT NULL CONSTRAINT DF_TesObFinPagos_UsuarioRegistro DEFAULT('Sistema'),
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_TesObFinPagos_FechaRegistro DEFAULT(GETDATE()),
        UsuarioAnulacion VARCHAR(80) NULL,
        FechaAnulacion DATETIME NULL,
        MotivoAnulacion VARCHAR(500) NULL,
        CONSTRAINT FK_TesObFinPagos_Cuota FOREIGN KEY (IdCuotaObligacionFinanciera) REFERENCES dbo.TesObligacionFinancieraCuotas(IdCuotaObligacionFinanciera),
        CONSTRAINT FK_TesObFinPagos_Banco FOREIGN KEY (IdBanco) REFERENCES dbo.TesBancos(IdBanco),
        CONSTRAINT FK_TesObFinPagos_CuentaBancaria FOREIGN KEY (IdCuentaBancaria) REFERENCES dbo.TesCuentasBancarias(IdCuentaBancaria),
        CONSTRAINT CK_TesObFinPagos_Monto CHECK (Monto > 0),
        CONSTRAINT CK_TesObFinPagos_Estado CHECK (Estado IN ('ACTIVO', 'ANULADO'))
    );
END;
GO

IF OBJECT_ID('dbo.TesObligacionFinancieraHistorial', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TesObligacionFinancieraHistorial
    (
        IdHistorialObligacionFinanciera BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TesObFinHistorial PRIMARY KEY,
        IdObligacionFinanciera INT NOT NULL,
        IdCuotaObligacionFinanciera INT NULL,
        IdPagoObligacionFinanciera INT NULL,
        Usuario VARCHAR(80) NOT NULL,
        Accion VARCHAR(120) NOT NULL,
        EstadoAnterior VARCHAR(30) NULL,
        EstadoNuevo VARCHAR(30) NULL,
        Descripcion VARCHAR(1000) NOT NULL CONSTRAINT DF_TesObFinHistorial_Descripcion DEFAULT(''),
        FechaHora DATETIME NOT NULL CONSTRAINT DF_TesObFinHistorial_FechaHora DEFAULT(GETDATE()),
        CONSTRAINT FK_TesObFinHistorial_Obligacion FOREIGN KEY (IdObligacionFinanciera) REFERENCES dbo.TesObligacionesFinancieras(IdObligacionFinanciera),
        CONSTRAINT FK_TesObFinHistorial_Cuota FOREIGN KEY (IdCuotaObligacionFinanciera) REFERENCES dbo.TesObligacionFinancieraCuotas(IdCuotaObligacionFinanciera),
        CONSTRAINT FK_TesObFinHistorial_Pago FOREIGN KEY (IdPagoObligacionFinanciera) REFERENCES dbo.TesObligacionFinancieraPagos(IdPagoObligacionFinanciera)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesObFin_Tipo' AND object_id = OBJECT_ID('dbo.TesObligacionesFinancieras'))
    CREATE INDEX IX_TesObFin_Tipo ON dbo.TesObligacionesFinancieras(IdTipoObligacionFinanciera, Estado);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesObFin_Entidad' AND object_id = OBJECT_ID('dbo.TesObligacionesFinancieras'))
    CREATE INDEX IX_TesObFin_Entidad ON dbo.TesObligacionesFinancieras(IdEntidadFinanciera, Estado);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesObFin_Titular' AND object_id = OBJECT_ID('dbo.TesObligacionesFinancieras'))
    CREATE INDEX IX_TesObFin_Titular ON dbo.TesObligacionesFinancieras(TipoTitular, IdTitular, NombreTitular);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesObFin_EmpresaResponsable' AND object_id = OBJECT_ID('dbo.TesObligacionesFinancieras'))
    CREATE INDEX IX_TesObFin_EmpresaResponsable ON dbo.TesObligacionesFinancieras(IdEmpresaResponsable, Estado);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesObFin_Estado' AND object_id = OBJECT_ID('dbo.TesObligacionesFinancieras'))
    CREATE INDEX IX_TesObFin_Estado ON dbo.TesObligacionesFinancieras(Estado);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesObFin_FechaInicio' AND object_id = OBJECT_ID('dbo.TesObligacionesFinancieras'))
    CREATE INDEX IX_TesObFin_FechaInicio ON dbo.TesObligacionesFinancieras(FechaInicio);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesObFin_Origen' AND object_id = OBJECT_ID('dbo.TesObligacionesFinancieras'))
    CREATE INDEX IX_TesObFin_Origen ON dbo.TesObligacionesFinancieras(OrigenTipo, OrigenId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesObFinCuotas_Obligacion' AND object_id = OBJECT_ID('dbo.TesObligacionFinancieraCuotas'))
    CREATE INDEX IX_TesObFinCuotas_Obligacion ON dbo.TesObligacionFinancieraCuotas(IdObligacionFinanciera, NumeroCuota);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_TesObFinCuotas_Obligacion_Numero' AND object_id = OBJECT_ID('dbo.TesObligacionFinancieraCuotas'))
    CREATE UNIQUE INDEX UX_TesObFinCuotas_Obligacion_Numero ON dbo.TesObligacionFinancieraCuotas(IdObligacionFinanciera, NumeroCuota);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesObFinCuotas_FechaVencimiento' AND object_id = OBJECT_ID('dbo.TesObligacionFinancieraCuotas'))
    CREATE INDEX IX_TesObFinCuotas_FechaVencimiento ON dbo.TesObligacionFinancieraCuotas(FechaVencimiento, Estado);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesObFinPagos_Cuota' AND object_id = OBJECT_ID('dbo.TesObligacionFinancieraPagos'))
    CREATE INDEX IX_TesObFinPagos_Cuota ON dbo.TesObligacionFinancieraPagos(IdCuotaObligacionFinanciera, Estado);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesObFinPagos_FechaPago' AND object_id = OBJECT_ID('dbo.TesObligacionFinancieraPagos'))
    CREATE INDEX IX_TesObFinPagos_FechaPago ON dbo.TesObligacionFinancieraPagos(FechaPago);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesObFinPagos_Banco' AND object_id = OBJECT_ID('dbo.TesObligacionFinancieraPagos'))
    CREATE INDEX IX_TesObFinPagos_Banco ON dbo.TesObligacionFinancieraPagos(IdBanco, Estado);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TesObFinHistorial_Obligacion' AND object_id = OBJECT_ID('dbo.TesObligacionFinancieraHistorial'))
    CREATE INDEX IX_TesObFinHistorial_Obligacion ON dbo.TesObligacionFinancieraHistorial(IdObligacionFinanciera, FechaHora DESC);
GO

IF TYPE_ID('dbo.TesObligacionFinancieraCuotaType') IS NULL
BEGIN
    CREATE TYPE dbo.TesObligacionFinancieraCuotaType AS TABLE
    (
        NumeroCuota INT NOT NULL,
        TotalCuotas INT NOT NULL,
        FechaVencimiento DATE NOT NULL,
        Importe DECIMAL(18,2) NOT NULL,
        Observacion VARCHAR(500) NULL
    );
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_TES_OBFIN_LISTAR_TIPOS
AS
BEGIN
    SET NOCOUNT ON;

    SELECT IdTipoObligacionFinanciera, Codigo, Nombre, Descripcion, Activo
    FROM dbo.TesTiposObligacionFinanciera
    WHERE Activo = 1
    ORDER BY Nombre;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_TES_OBFIN_LISTAR_ENTIDADES
    @TipoEntidad VARCHAR(30) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @TipoEntidad = NULLIF(UPPER(LTRIM(RTRIM(ISNULL(@TipoEntidad, '')))), '');

    SELECT IdEntidadFinanciera, TipoEntidad, Nombre, Ruc, Activo, Observacion
    FROM dbo.TesEntidadesFinancieras
    WHERE Activo = 1
      AND (@TipoEntidad IS NULL OR TipoEntidad = @TipoEntidad)
    ORDER BY TipoEntidad, Nombre;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_TES_OBFIN_GUARDAR_PRESTAMO
    @IdObligacionFinanciera INT OUTPUT,
    @IdEntidadFinanciera INT,
    @TipoTitular VARCHAR(20),
    @IdTitular INT = NULL,
    @NombreTitular VARCHAR(180),
    @IdEmpresaResponsable INT = NULL,
    @CodigoReferencia VARCHAR(80) = '',
    @NumeroContrato VARCHAR(80) = '',
    @NumeroOperacion VARCHAR(80) = '',
    @Concepto VARCHAR(250),
    @Moneda VARCHAR(10),
    @MontoOriginal DECIMAL(18,2) = NULL,
    @FechaInicio DATE,
    @TotalCuotas INT,
    @Observacion VARCHAR(1000) = '',
    @Usuario VARCHAR(80),
    @Cuotas dbo.TesObligacionFinancieraCuotaType READONLY,
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @Resultado = 0;
    SET @Mensaje = '';
    SET @IdObligacionFinanciera = ISNULL(@IdObligacionFinanciera, 0);
    SET @TipoTitular = UPPER(LTRIM(RTRIM(ISNULL(@TipoTitular, ''))));
    SET @NombreTitular = LTRIM(RTRIM(ISNULL(@NombreTitular, '')));
    SET @CodigoReferencia = LTRIM(RTRIM(ISNULL(@CodigoReferencia, '')));
    SET @NumeroContrato = LTRIM(RTRIM(ISNULL(@NumeroContrato, '')));
    SET @NumeroOperacion = LTRIM(RTRIM(ISNULL(@NumeroOperacion, '')));
    SET @Concepto = LTRIM(RTRIM(ISNULL(@Concepto, '')));
    SET @Moneda = UPPER(LTRIM(RTRIM(ISNULL(@Moneda, 'PEN'))));
    SET @Observacion = LTRIM(RTRIM(ISNULL(@Observacion, '')));
    SET @Usuario = LTRIM(RTRIM(ISNULL(@Usuario, 'Sistema')));

    DECLARE @IdTipoPrestamo INT;

    SELECT @IdTipoPrestamo = IdTipoObligacionFinanciera
    FROM dbo.TesTiposObligacionFinanciera
    WHERE Codigo = 'PRESTAMO'
      AND Activo = 1;

    IF @IdTipoPrestamo IS NULL
    BEGIN
        SET @Mensaje = 'No existe el tipo de obligacion financiera PRESTAMO.';
        RETURN;
    END;

    IF @IdObligacionFinanciera <> 0
    BEGIN
        SET @Mensaje = 'La etapa actual solo permite registrar nuevos prestamos. La edicion se definira en la capa funcional correspondiente.';
        RETURN;
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.TesEntidadesFinancieras WHERE IdEntidadFinanciera = @IdEntidadFinanciera AND Activo = 1)
    BEGIN
        SET @Mensaje = 'La entidad financiera no existe o esta inactiva.';
        RETURN;
    END;

    IF @TipoTitular NOT IN ('EMPRESA', 'PERSONA', 'OTRO')
    BEGIN
        SET @Mensaje = 'El tipo de titular debe ser EMPRESA, PERSONA u OTRO.';
        RETURN;
    END;

    IF @TipoTitular = 'EMPRESA' AND @IdTitular IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.Empresas WHERE IdEmpresa = @IdTitular AND Estado = 1)
    BEGIN
        SET @Mensaje = 'La empresa titular no existe o esta inactiva.';
        RETURN;
    END;

    IF @IdEmpresaResponsable IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.Empresas WHERE IdEmpresa = @IdEmpresaResponsable AND Estado = 1)
    BEGIN
        SET @Mensaje = 'La empresa responsable no existe o esta inactiva.';
        RETURN;
    END;

    IF @NombreTitular = '' AND @TipoTitular = 'EMPRESA' AND @IdTitular IS NOT NULL
    BEGIN
        SELECT @NombreTitular = Nombre
        FROM dbo.Empresas
        WHERE IdEmpresa = @IdTitular;
    END;

    IF @NombreTitular = ''
    BEGIN
        SET @Mensaje = 'Debe indicar el titular de la obligacion.';
        RETURN;
    END;

    IF @Concepto = ''
    BEGIN
        SET @Mensaje = 'Debe ingresar el concepto del prestamo.';
        RETURN;
    END;

    IF @Moneda NOT IN ('PEN', 'USD')
    BEGIN
        SET @Mensaje = 'La moneda debe ser PEN o USD.';
        RETURN;
    END;

    IF @MontoOriginal IS NOT NULL AND @MontoOriginal <= 0
    BEGIN
        SET @Mensaje = 'El monto original debe ser mayor a cero.';
        RETURN;
    END;

    IF @FechaInicio IS NULL
    BEGIN
        SET @Mensaje = 'Debe ingresar la fecha de inicio.';
        RETURN;
    END;

    IF @TotalCuotas <= 0
    BEGIN
        SET @Mensaje = 'El total de cuotas debe ser mayor a cero.';
        RETURN;
    END;

    IF NOT EXISTS (SELECT 1 FROM @Cuotas)
    BEGIN
        SET @Mensaje = 'Debe registrar el cronograma de cuotas.';
        RETURN;
    END;

    IF (SELECT COUNT(1) FROM @Cuotas) <> @TotalCuotas
    BEGIN
        SET @Mensaje = 'La cantidad de cuotas del cronograma debe coincidir con el total de cuotas.';
        RETURN;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM @Cuotas
        WHERE NumeroCuota <= 0
           OR TotalCuotas <= 0
           OR NumeroCuota > TotalCuotas
           OR TotalCuotas <> @TotalCuotas
    )
    BEGIN
        SET @Mensaje = 'La numeracion del cronograma no es valida.';
        RETURN;
    END;

    IF EXISTS
    (
        SELECT NumeroCuota
        FROM @Cuotas
        GROUP BY NumeroCuota
        HAVING COUNT(1) > 1
    )
    BEGIN
        SET @Mensaje = 'No se puede duplicar el numero de cuota dentro del prestamo.';
        RETURN;
    END;

    IF EXISTS (SELECT 1 FROM @Cuotas WHERE Importe <= 0)
    BEGIN
        SET @Mensaje = 'Los importes de las cuotas deben ser mayores a cero.';
        RETURN;
    END;

    IF EXISTS (SELECT 1 FROM @Cuotas WHERE FechaVencimiento < @FechaInicio)
    BEGIN
        SET @Mensaje = 'La fecha de vencimiento no puede ser anterior a la fecha de inicio.';
        RETURN;
    END;

    DECLARE @TotalCronograma DECIMAL(18,2);
    SELECT @TotalCronograma = ROUND(SUM(Importe), 2) FROM @Cuotas;

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO dbo.TesObligacionesFinancieras
        (
            IdTipoObligacionFinanciera,
            IdEntidadFinanciera,
            TipoTitular,
            IdTitular,
            NombreTitular,
            IdEmpresaResponsable,
            CodigoReferencia,
            NumeroContrato,
            NumeroOperacion,
            Concepto,
            Moneda,
            MontoOriginal,
            FechaInicio,
            TotalCuotas,
            Estado,
            OrigenTipo,
            Observacion,
            UsuarioRegistro
        )
        VALUES
        (
            @IdTipoPrestamo,
            @IdEntidadFinanciera,
            @TipoTitular,
            @IdTitular,
            @NombreTitular,
            @IdEmpresaResponsable,
            @CodigoReferencia,
            @NumeroContrato,
            @NumeroOperacion,
            @Concepto,
            @Moneda,
            @MontoOriginal,
            @FechaInicio,
            @TotalCuotas,
            'PENDIENTE',
            'MANUAL',
            @Observacion,
            @Usuario
        );

        SET @IdObligacionFinanciera = SCOPE_IDENTITY();

        INSERT INTO dbo.TesObligacionFinancieraCuotas
        (
            IdObligacionFinanciera,
            NumeroCuota,
            TotalCuotas,
            FechaVencimiento,
            Importe,
            TotalPagado,
            SaldoPendiente,
            Estado,
            Observacion
        )
        SELECT
            @IdObligacionFinanciera,
            NumeroCuota,
            TotalCuotas,
            FechaVencimiento,
            Importe,
            0,
            Importe,
            'PENDIENTE',
            LTRIM(RTRIM(ISNULL(Observacion, '')))
        FROM @Cuotas;

        INSERT INTO dbo.TesObligacionFinancieraHistorial
        (
            IdObligacionFinanciera,
            Usuario,
            Accion,
            EstadoAnterior,
            EstadoNuevo,
            Descripcion
        )
        VALUES
        (
            @IdObligacionFinanciera,
            @Usuario,
            'CREACION',
            NULL,
            'PENDIENTE',
            CONCAT('Prestamo registrado. Total cronograma: ', FORMAT(@TotalCronograma, 'N2'), '.')
        );

        COMMIT TRANSACTION;

        SET @Resultado = 1;
        SET @Mensaje = 'Prestamo registrado correctamente.';

        SELECT
            @Resultado AS Resultado,
            @Mensaje AS Mensaje,
            @IdObligacionFinanciera AS IdObligacionFinanciera,
            @TotalCronograma AS TotalCronograma,
            CAST(0 AS DECIMAL(18,2)) AS TotalPagado,
            @TotalCronograma AS SaldoPendiente,
            'PENDIENTE' AS EstadoObligacionFinanciera;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;

        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();

        SELECT
            @Resultado AS Resultado,
            @Mensaje AS Mensaje,
            @IdObligacionFinanciera AS IdObligacionFinanciera,
            CAST(0 AS DECIMAL(18,2)) AS TotalCronograma,
            CAST(0 AS DECIMAL(18,2)) AS TotalPagado,
            CAST(0 AS DECIMAL(18,2)) AS SaldoPendiente,
            '' AS EstadoObligacionFinanciera;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_TES_OBFIN_LISTAR
    @Texto VARCHAR(160) = NULL,
    @Estado VARCHAR(30) = NULL,
    @IdTipoObligacionFinanciera INT = NULL,
    @IdEntidadFinanciera INT = NULL,
    @TipoTitular VARCHAR(20) = NULL,
    @IdEmpresaResponsable INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @Texto = NULLIF(LTRIM(RTRIM(ISNULL(@Texto, ''))), '');
    SET @Estado = NULLIF(UPPER(LTRIM(RTRIM(ISNULL(@Estado, '')))), '');
    SET @TipoTitular = NULLIF(UPPER(LTRIM(RTRIM(ISNULL(@TipoTitular, '')))), '');

    SELECT
        O.IdObligacionFinanciera,
        O.IdTipoObligacionFinanciera,
        T.Codigo AS CodigoTipoObligacionFinanciera,
        T.Nombre AS TipoObligacionFinanciera,
        O.IdEntidadFinanciera,
        E.TipoEntidad,
        E.Nombre AS EntidadFinanciera,
        O.TipoTitular,
        O.IdTitular,
        O.NombreTitular,
        O.IdEmpresaResponsable,
        ER.Nombre AS EmpresaResponsable,
        O.CodigoReferencia,
        O.NumeroContrato,
        O.NumeroOperacion,
        O.Concepto,
        O.Moneda,
        O.MontoOriginal,
        O.FechaInicio,
        O.TotalCuotas,
        ISNULL(TOT.TotalCronograma, 0) AS TotalCronograma,
        ISNULL(TOT.TotalPagado, 0) AS TotalPagado,
        ISNULL(TOT.SaldoPendiente, 0) AS SaldoPendiente,
        NX.NumeroCuota AS ProximaCuota,
        NX.FechaVencimiento AS ProximoVencimiento,
        NX.SaldoPendiente AS ImportePendienteProximaCuota,
        O.Estado,
        O.Observacion,
        O.UsuarioRegistro,
        O.FechaRegistro
    FROM dbo.TesObligacionesFinancieras O
    INNER JOIN dbo.TesTiposObligacionFinanciera T ON T.IdTipoObligacionFinanciera = O.IdTipoObligacionFinanciera
    INNER JOIN dbo.TesEntidadesFinancieras E ON E.IdEntidadFinanciera = O.IdEntidadFinanciera
    LEFT JOIN dbo.Empresas ER ON ER.IdEmpresa = O.IdEmpresaResponsable
    OUTER APPLY
    (
        SELECT
            SUM(Q.Importe) AS TotalCronograma,
            SUM(ISNULL(PG.TotalPagado, 0)) AS TotalPagado,
            SUM(Q.Importe - ISNULL(PG.TotalPagado, 0)) AS SaldoPendiente
        FROM dbo.TesObligacionFinancieraCuotas Q
        OUTER APPLY
        (
            SELECT SUM(P.Monto) AS TotalPagado
            FROM dbo.TesObligacionFinancieraPagos P
            WHERE P.IdCuotaObligacionFinanciera = Q.IdCuotaObligacionFinanciera
              AND P.Estado = 'ACTIVO'
        ) PG
        WHERE Q.IdObligacionFinanciera = O.IdObligacionFinanciera
          AND Q.Estado <> 'ANULADA'
    ) TOT
    OUTER APPLY
    (
        SELECT TOP (1)
            Q.NumeroCuota,
            Q.FechaVencimiento,
            Q.Importe - ISNULL(PG.TotalPagado, 0) AS SaldoPendiente
        FROM dbo.TesObligacionFinancieraCuotas Q
        OUTER APPLY
        (
            SELECT SUM(P.Monto) AS TotalPagado
            FROM dbo.TesObligacionFinancieraPagos P
            WHERE P.IdCuotaObligacionFinanciera = Q.IdCuotaObligacionFinanciera
              AND P.Estado = 'ACTIVO'
        ) PG
        WHERE Q.IdObligacionFinanciera = O.IdObligacionFinanciera
          AND Q.Estado <> 'ANULADA'
          AND Q.Importe - ISNULL(PG.TotalPagado, 0) > 0
        ORDER BY Q.FechaVencimiento, Q.NumeroCuota
    ) NX
    WHERE (@Estado IS NULL OR @Estado = 'TODOS' OR O.Estado = @Estado)
      AND (@IdTipoObligacionFinanciera IS NULL OR O.IdTipoObligacionFinanciera = @IdTipoObligacionFinanciera)
      AND (@IdEntidadFinanciera IS NULL OR O.IdEntidadFinanciera = @IdEntidadFinanciera)
      AND (@TipoTitular IS NULL OR O.TipoTitular = @TipoTitular)
      AND (@IdEmpresaResponsable IS NULL OR O.IdEmpresaResponsable = @IdEmpresaResponsable)
      AND
      (
          @Texto IS NULL
          OR E.Nombre LIKE '%' + @Texto + '%'
          OR O.NombreTitular LIKE '%' + @Texto + '%'
          OR O.Concepto LIKE '%' + @Texto + '%'
          OR O.CodigoReferencia LIKE '%' + @Texto + '%'
          OR O.NumeroContrato LIKE '%' + @Texto + '%'
          OR O.NumeroOperacion LIKE '%' + @Texto + '%'
      )
    ORDER BY O.FechaInicio DESC, O.IdObligacionFinanciera DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_TES_OBFIN_OBTENER
    @IdObligacionFinanciera INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        O.IdObligacionFinanciera,
        O.IdTipoObligacionFinanciera,
        T.Codigo AS CodigoTipoObligacionFinanciera,
        T.Nombre AS TipoObligacionFinanciera,
        O.IdEntidadFinanciera,
        E.TipoEntidad,
        E.Nombre AS EntidadFinanciera,
        O.TipoTitular,
        O.IdTitular,
        O.NombreTitular,
        O.IdEmpresaResponsable,
        ER.Nombre AS EmpresaResponsable,
        O.CodigoReferencia,
        O.NumeroContrato,
        O.NumeroOperacion,
        O.Concepto,
        O.Moneda,
        O.MontoOriginal,
        O.FechaInicio,
        O.TotalCuotas,
        ISNULL(TOT.TotalCronograma, 0) AS TotalCronograma,
        ISNULL(TOT.TotalPagado, 0) AS TotalPagado,
        ISNULL(TOT.SaldoPendiente, 0) AS SaldoPendiente,
        O.Estado,
        O.OrigenTipo,
        O.OrigenId,
        O.Observacion,
        O.UsuarioRegistro,
        O.FechaRegistro,
        O.UsuarioModificacion,
        O.FechaModificacion,
        O.UsuarioAnulacion,
        O.FechaAnulacion,
        O.MotivoAnulacion
    FROM dbo.TesObligacionesFinancieras O
    INNER JOIN dbo.TesTiposObligacionFinanciera T ON T.IdTipoObligacionFinanciera = O.IdTipoObligacionFinanciera
    INNER JOIN dbo.TesEntidadesFinancieras E ON E.IdEntidadFinanciera = O.IdEntidadFinanciera
    LEFT JOIN dbo.Empresas ER ON ER.IdEmpresa = O.IdEmpresaResponsable
    OUTER APPLY
    (
        SELECT
            SUM(Q.Importe) AS TotalCronograma,
            SUM(ISNULL(PG.TotalPagado, 0)) AS TotalPagado,
            SUM(Q.Importe - ISNULL(PG.TotalPagado, 0)) AS SaldoPendiente
        FROM dbo.TesObligacionFinancieraCuotas Q
        OUTER APPLY
        (
            SELECT SUM(P.Monto) AS TotalPagado
            FROM dbo.TesObligacionFinancieraPagos P
            WHERE P.IdCuotaObligacionFinanciera = Q.IdCuotaObligacionFinanciera
              AND P.Estado = 'ACTIVO'
        ) PG
        WHERE Q.IdObligacionFinanciera = O.IdObligacionFinanciera
          AND Q.Estado <> 'ANULADA'
    ) TOT
    WHERE O.IdObligacionFinanciera = @IdObligacionFinanciera;

    SELECT
        Q.IdCuotaObligacionFinanciera,
        Q.IdObligacionFinanciera,
        Q.NumeroCuota,
        Q.TotalCuotas,
        Q.FechaVencimiento,
        Q.Importe,
        ISNULL(PG.TotalPagado, 0) AS TotalPagado,
        Q.Importe - ISNULL(PG.TotalPagado, 0) AS SaldoPendiente,
        Q.Estado,
        Q.Observacion,
        Q.FechaRegistro
    FROM dbo.TesObligacionFinancieraCuotas Q
    OUTER APPLY
    (
        SELECT SUM(P.Monto) AS TotalPagado
        FROM dbo.TesObligacionFinancieraPagos P
        WHERE P.IdCuotaObligacionFinanciera = Q.IdCuotaObligacionFinanciera
          AND P.Estado = 'ACTIVO'
    ) PG
    WHERE Q.IdObligacionFinanciera = @IdObligacionFinanciera
      AND Q.Estado <> 'ANULADA'
    ORDER BY Q.NumeroCuota, Q.IdCuotaObligacionFinanciera;

    SELECT
        P.IdPagoObligacionFinanciera,
        P.IdCuotaObligacionFinanciera,
        Q.IdObligacionFinanciera,
        Q.NumeroCuota,
        P.FechaPago,
        P.Monto,
        P.IdBanco,
        P.IdCuentaBancaria,
        ISNULL(COALESCE(BD.Nombre, BC.Nombre), '') AS Banco,
        ISNULL(CB.NumeroCuenta, '') AS NumeroCuenta,
        P.NumeroOperacion,
        P.Observacion,
        P.Estado,
        P.UsuarioRegistro,
        P.FechaRegistro,
        P.UsuarioAnulacion,
        P.FechaAnulacion,
        P.MotivoAnulacion
    FROM dbo.TesObligacionFinancieraPagos P
    INNER JOIN dbo.TesObligacionFinancieraCuotas Q ON Q.IdCuotaObligacionFinanciera = P.IdCuotaObligacionFinanciera
    LEFT JOIN dbo.TesCuentasBancarias CB ON CB.IdCuentaBancaria = P.IdCuentaBancaria
    LEFT JOIN dbo.TesBancos BC ON BC.IdBanco = CB.IdBanco
    LEFT JOIN dbo.TesBancos BD ON BD.IdBanco = P.IdBanco
    WHERE Q.IdObligacionFinanciera = @IdObligacionFinanciera
    ORDER BY P.FechaPago, P.IdPagoObligacionFinanciera;

    SELECT
        H.IdHistorialObligacionFinanciera,
        H.IdObligacionFinanciera,
        H.IdCuotaObligacionFinanciera,
        H.IdPagoObligacionFinanciera,
        H.Usuario,
        H.Accion,
        H.EstadoAnterior,
        H.EstadoNuevo,
        H.Descripcion,
        H.FechaHora
    FROM dbo.TesObligacionFinancieraHistorial H
    WHERE H.IdObligacionFinanciera = @IdObligacionFinanciera
    ORDER BY H.FechaHora DESC, H.IdHistorialObligacionFinanciera DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_TES_OBFIN_ANULAR
    @IdObligacionFinanciera INT,
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

    DECLARE @EstadoAnterior VARCHAR(30);

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @EstadoAnterior = Estado
        FROM dbo.TesObligacionesFinancieras WITH (UPDLOCK, HOLDLOCK)
        WHERE IdObligacionFinanciera = @IdObligacionFinanciera;

        IF @EstadoAnterior IS NULL
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'La obligacion financiera no existe.';
            RETURN;
        END;

        IF @EstadoAnterior = 'ANULADA'
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'La obligacion financiera ya se encuentra anulada.';
            RETURN;
        END;

        IF EXISTS
        (
            SELECT 1
            FROM dbo.TesObligacionFinancieraPagos P
            INNER JOIN dbo.TesObligacionFinancieraCuotas Q ON Q.IdCuotaObligacionFinanciera = P.IdCuotaObligacionFinanciera
            WHERE Q.IdObligacionFinanciera = @IdObligacionFinanciera
              AND P.Estado = 'ACTIVO'
        )
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'No se puede anular una obligacion financiera con pagos activos.';
            RETURN;
        END;

        UPDATE dbo.TesObligacionesFinancieras
        SET Estado = 'ANULADA',
            UsuarioAnulacion = @Usuario,
            FechaAnulacion = GETDATE(),
            MotivoAnulacion = @Motivo
        WHERE IdObligacionFinanciera = @IdObligacionFinanciera;

        UPDATE dbo.TesObligacionFinancieraCuotas
        SET Estado = 'ANULADA',
            SaldoPendiente = Importe - TotalPagado
        WHERE IdObligacionFinanciera = @IdObligacionFinanciera
          AND Estado <> 'ANULADA';

        INSERT INTO dbo.TesObligacionFinancieraHistorial
        (
            IdObligacionFinanciera,
            Usuario,
            Accion,
            EstadoAnterior,
            EstadoNuevo,
            Descripcion
        )
        VALUES
        (
            @IdObligacionFinanciera,
            @Usuario,
            'ANULACION',
            @EstadoAnterior,
            'ANULADA',
            @Motivo
        );

        COMMIT TRANSACTION;

        SET @Resultado = 1;
        SET @Mensaje = 'Obligacion financiera anulada correctamente.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_TES_OBFIN_REGISTRAR_PAGO
    @IdPagoObligacionFinanciera INT OUTPUT,
    @IdCuotaObligacionFinanciera INT,
    @FechaPago DATE,
    @Monto DECIMAL(18,2),
    @IdBanco INT = NULL,
    @IdCuentaBancaria INT = NULL,
    @NumeroOperacion VARCHAR(80) = '',
    @Observacion VARCHAR(500) = '',
    @Usuario VARCHAR(80),
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT,
    @TotalPagado DECIMAL(18,2) OUTPUT,
    @SaldoPendiente DECIMAL(18,2) OUTPUT,
    @EstadoCuota VARCHAR(30) OUTPUT,
    @EstadoObligacionFinanciera VARCHAR(30) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @IdPagoObligacionFinanciera = 0;
    SET @Resultado = 0;
    SET @Mensaje = '';
    SET @TotalPagado = 0;
    SET @SaldoPendiente = 0;
    SET @EstadoCuota = '';
    SET @EstadoObligacionFinanciera = '';
    SET @NumeroOperacion = LTRIM(RTRIM(ISNULL(@NumeroOperacion, '')));
    SET @Observacion = LTRIM(RTRIM(ISNULL(@Observacion, '')));
    SET @Usuario = LTRIM(RTRIM(ISNULL(@Usuario, 'Sistema')));

    IF @IdCuotaObligacionFinanciera <= 0
    BEGIN
        SET @Mensaje = 'Debe seleccionar una cuota valida.';
        RETURN;
    END;

    IF @FechaPago IS NULL
    BEGIN
        SET @Mensaje = 'Debe ingresar la fecha de pago.';
        RETURN;
    END;

    IF @Monto <= 0
    BEGIN
        SET @Mensaje = 'El monto del pago debe ser mayor a cero.';
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
            @IdObligacionFinanciera INT,
            @ImporteCuota DECIMAL(18,2),
            @EstadoAnteriorCuota VARCHAR(30),
            @EstadoAnteriorObligacion VARCHAR(30),
            @TotalPagadoAnterior DECIMAL(18,2),
            @SaldoAnterior DECIMAL(18,2),
            @NombreBanco VARCHAR(120);

        SELECT
            @IdObligacionFinanciera = O.IdObligacionFinanciera,
            @ImporteCuota = Q.Importe,
            @EstadoAnteriorCuota = Q.Estado,
            @EstadoAnteriorObligacion = O.Estado
        FROM dbo.TesObligacionFinancieraCuotas Q WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN dbo.TesObligacionesFinancieras O WITH (UPDLOCK, HOLDLOCK)
            ON O.IdObligacionFinanciera = Q.IdObligacionFinanciera
        WHERE Q.IdCuotaObligacionFinanciera = @IdCuotaObligacionFinanciera;

        IF @IdObligacionFinanciera IS NULL
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'La cuota seleccionada no existe.';
            RETURN;
        END;

        IF @EstadoAnteriorObligacion = 'ANULADA'
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'No se puede registrar pagos sobre una obligacion anulada.';
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

        SELECT @TotalPagadoAnterior = ISNULL(SUM(Monto), 0)
        FROM dbo.TesObligacionFinancieraPagos WITH (UPDLOCK, HOLDLOCK)
        WHERE IdCuotaObligacionFinanciera = @IdCuotaObligacionFinanciera
          AND Estado = 'ACTIVO';

        SET @SaldoAnterior = @ImporteCuota - @TotalPagadoAnterior;

        IF @SaldoAnterior <= 0
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'La cuota seleccionada no tiene saldo pendiente.';
            RETURN;
        END;

        IF @Monto > @SaldoAnterior
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = CONCAT('El monto del pago no puede superar el saldo pendiente de ', FORMAT(@SaldoAnterior, 'N2'), '.');
            RETURN;
        END;

        INSERT INTO dbo.TesObligacionFinancieraPagos
        (
            IdCuotaObligacionFinanciera,
            FechaPago,
            Monto,
            IdBanco,
            IdCuentaBancaria,
            NumeroOperacion,
            Observacion,
            Estado,
            UsuarioRegistro
        )
        VALUES
        (
            @IdCuotaObligacionFinanciera,
            @FechaPago,
            @Monto,
            @IdBanco,
            @IdCuentaBancaria,
            @NumeroOperacion,
            @Observacion,
            'ACTIVO',
            @Usuario
        );

        SET @IdPagoObligacionFinanciera = SCOPE_IDENTITY();

        SELECT @TotalPagado = ISNULL(SUM(Monto), 0)
        FROM dbo.TesObligacionFinancieraPagos
        WHERE IdCuotaObligacionFinanciera = @IdCuotaObligacionFinanciera
          AND Estado = 'ACTIVO';

        SET @SaldoPendiente = @ImporteCuota - @TotalPagado;
        SET @EstadoCuota = CASE
            WHEN @TotalPagado <= 0 THEN 'PENDIENTE'
            WHEN @SaldoPendiente <= 0 THEN 'CANCELADA'
            ELSE 'PARCIAL'
        END;

        UPDATE dbo.TesObligacionFinancieraCuotas
        SET TotalPagado = @TotalPagado,
            SaldoPendiente = @SaldoPendiente,
            Estado = @EstadoCuota
        WHERE IdCuotaObligacionFinanciera = @IdCuotaObligacionFinanciera;

        DECLARE
            @CuotasActivas INT,
            @CuotasConPago INT,
            @CuotasConSaldo INT;

        SELECT
            @CuotasActivas = COUNT(1),
            @CuotasConPago = SUM(CASE WHEN ISNULL(PG.TotalPagado, 0) > 0 THEN 1 ELSE 0 END),
            @CuotasConSaldo = SUM(CASE WHEN Q.Importe - ISNULL(PG.TotalPagado, 0) > 0 THEN 1 ELSE 0 END)
        FROM dbo.TesObligacionFinancieraCuotas Q WITH (UPDLOCK, HOLDLOCK)
        OUTER APPLY
        (
            SELECT SUM(P.Monto) AS TotalPagado
            FROM dbo.TesObligacionFinancieraPagos P
            WHERE P.IdCuotaObligacionFinanciera = Q.IdCuotaObligacionFinanciera
              AND P.Estado = 'ACTIVO'
        ) PG
        WHERE Q.IdObligacionFinanciera = @IdObligacionFinanciera
          AND Q.Estado <> 'ANULADA';

        SET @EstadoObligacionFinanciera = CASE
            WHEN ISNULL(@CuotasActivas, 0) = 0 THEN @EstadoAnteriorObligacion
            WHEN ISNULL(@CuotasConSaldo, 0) = 0 THEN 'CANCELADA'
            WHEN ISNULL(@CuotasConPago, 0) > 0 THEN 'PARCIAL'
            ELSE 'PENDIENTE'
        END;

        UPDATE dbo.TesObligacionesFinancieras
        SET Estado = @EstadoObligacionFinanciera,
            UsuarioModificacion = @Usuario,
            FechaModificacion = GETDATE()
        WHERE IdObligacionFinanciera = @IdObligacionFinanciera;

        SELECT @NombreBanco = Nombre
        FROM dbo.TesBancos
        WHERE IdBanco = @IdBanco;

        INSERT INTO dbo.TesObligacionFinancieraHistorial
        (
            IdObligacionFinanciera,
            IdCuotaObligacionFinanciera,
            IdPagoObligacionFinanciera,
            Usuario,
            Accion,
            EstadoAnterior,
            EstadoNuevo,
            Descripcion
        )
        VALUES
        (
            @IdObligacionFinanciera,
            @IdCuotaObligacionFinanciera,
            @IdPagoObligacionFinanciera,
            @Usuario,
            'REGISTRO_PAGO',
            @EstadoAnteriorCuota,
            @EstadoCuota,
            CONCAT(
                'Pago registrado. Monto: ', FORMAT(@Monto, 'N2'),
                '. Fecha: ', CONVERT(VARCHAR(10), @FechaPago, 103),
                CASE WHEN @NombreBanco IS NOT NULL THEN CONCAT('. Banco: ', @NombreBanco) ELSE '' END,
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
            @IdPagoObligacionFinanciera AS IdPagoObligacionFinanciera,
            @TotalPagado AS TotalPagado,
            @SaldoPendiente AS SaldoPendiente,
            @EstadoCuota AS EstadoCuota,
            @EstadoObligacionFinanciera AS EstadoObligacionFinanciera;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;

        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();

        SELECT
            @Resultado AS Resultado,
            @Mensaje AS Mensaje,
            @IdPagoObligacionFinanciera AS IdPagoObligacionFinanciera,
            @TotalPagado AS TotalPagado,
            @SaldoPendiente AS SaldoPendiente,
            @EstadoCuota AS EstadoCuota,
            @EstadoObligacionFinanciera AS EstadoObligacionFinanciera;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_TES_OBFIN_ANULAR_PAGO
    @IdPagoObligacionFinanciera INT,
    @Usuario VARCHAR(80),
    @Motivo VARCHAR(500),
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT,
    @TotalPagado DECIMAL(18,2) OUTPUT,
    @SaldoPendiente DECIMAL(18,2) OUTPUT,
    @EstadoCuota VARCHAR(30) OUTPUT,
    @EstadoObligacionFinanciera VARCHAR(30) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @Resultado = 0;
    SET @Mensaje = '';
    SET @TotalPagado = 0;
    SET @SaldoPendiente = 0;
    SET @EstadoCuota = '';
    SET @EstadoObligacionFinanciera = '';
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
            @IdObligacionFinanciera INT,
            @IdCuotaObligacionFinanciera INT,
            @ImporteCuota DECIMAL(18,2),
            @EstadoAnteriorCuota VARCHAR(30),
            @EstadoAnteriorObligacion VARCHAR(30),
            @EstadoAnteriorPago VARCHAR(20);

        SELECT
            @IdCuotaObligacionFinanciera = Q.IdCuotaObligacionFinanciera,
            @IdObligacionFinanciera = O.IdObligacionFinanciera,
            @ImporteCuota = Q.Importe,
            @EstadoAnteriorCuota = Q.Estado,
            @EstadoAnteriorObligacion = O.Estado,
            @EstadoAnteriorPago = P.Estado
        FROM dbo.TesObligacionFinancieraPagos P WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN dbo.TesObligacionFinancieraCuotas Q WITH (UPDLOCK, HOLDLOCK)
            ON Q.IdCuotaObligacionFinanciera = P.IdCuotaObligacionFinanciera
        INNER JOIN dbo.TesObligacionesFinancieras O WITH (UPDLOCK, HOLDLOCK)
            ON O.IdObligacionFinanciera = Q.IdObligacionFinanciera
        WHERE P.IdPagoObligacionFinanciera = @IdPagoObligacionFinanciera;

        IF @IdObligacionFinanciera IS NULL
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'El pago seleccionado no existe.';
            RETURN;
        END;

        IF @EstadoAnteriorPago = 'ANULADO'
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'El pago seleccionado ya se encuentra anulado.';
            RETURN;
        END;

        IF @EstadoAnteriorObligacion = 'ANULADA'
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'No se puede anular pagos de una obligacion anulada.';
            RETURN;
        END;

        UPDATE dbo.TesObligacionFinancieraPagos
        SET Estado = 'ANULADO',
            UsuarioAnulacion = @Usuario,
            FechaAnulacion = GETDATE(),
            MotivoAnulacion = @Motivo
        WHERE IdPagoObligacionFinanciera = @IdPagoObligacionFinanciera;

        SELECT @TotalPagado = ISNULL(SUM(Monto), 0)
        FROM dbo.TesObligacionFinancieraPagos
        WHERE IdCuotaObligacionFinanciera = @IdCuotaObligacionFinanciera
          AND Estado = 'ACTIVO';

        SET @SaldoPendiente = @ImporteCuota - @TotalPagado;
        SET @EstadoCuota = CASE
            WHEN @TotalPagado <= 0 THEN 'PENDIENTE'
            WHEN @SaldoPendiente <= 0 THEN 'CANCELADA'
            ELSE 'PARCIAL'
        END;

        UPDATE dbo.TesObligacionFinancieraCuotas
        SET TotalPagado = @TotalPagado,
            SaldoPendiente = @SaldoPendiente,
            Estado = @EstadoCuota
        WHERE IdCuotaObligacionFinanciera = @IdCuotaObligacionFinanciera;

        DECLARE
            @CuotasActivas INT,
            @CuotasConPago INT,
            @CuotasConSaldo INT;

        SELECT
            @CuotasActivas = COUNT(1),
            @CuotasConPago = SUM(CASE WHEN ISNULL(PG.TotalPagado, 0) > 0 THEN 1 ELSE 0 END),
            @CuotasConSaldo = SUM(CASE WHEN Q.Importe - ISNULL(PG.TotalPagado, 0) > 0 THEN 1 ELSE 0 END)
        FROM dbo.TesObligacionFinancieraCuotas Q WITH (UPDLOCK, HOLDLOCK)
        OUTER APPLY
        (
            SELECT SUM(P.Monto) AS TotalPagado
            FROM dbo.TesObligacionFinancieraPagos P
            WHERE P.IdCuotaObligacionFinanciera = Q.IdCuotaObligacionFinanciera
              AND P.Estado = 'ACTIVO'
        ) PG
        WHERE Q.IdObligacionFinanciera = @IdObligacionFinanciera
          AND Q.Estado <> 'ANULADA';

        SET @EstadoObligacionFinanciera = CASE
            WHEN ISNULL(@CuotasActivas, 0) = 0 THEN @EstadoAnteriorObligacion
            WHEN ISNULL(@CuotasConSaldo, 0) = 0 THEN 'CANCELADA'
            WHEN ISNULL(@CuotasConPago, 0) > 0 THEN 'PARCIAL'
            ELSE 'PENDIENTE'
        END;

        UPDATE dbo.TesObligacionesFinancieras
        SET Estado = @EstadoObligacionFinanciera,
            UsuarioModificacion = @Usuario,
            FechaModificacion = GETDATE()
        WHERE IdObligacionFinanciera = @IdObligacionFinanciera;

        INSERT INTO dbo.TesObligacionFinancieraHistorial
        (
            IdObligacionFinanciera,
            IdCuotaObligacionFinanciera,
            IdPagoObligacionFinanciera,
            Usuario,
            Accion,
            EstadoAnterior,
            EstadoNuevo,
            Descripcion
        )
        VALUES
        (
            @IdObligacionFinanciera,
            @IdCuotaObligacionFinanciera,
            @IdPagoObligacionFinanciera,
            @Usuario,
            'ANULACION_PAGO',
            @EstadoAnteriorCuota,
            @EstadoCuota,
            @Motivo
        );

        COMMIT TRANSACTION;

        SET @Resultado = 1;
        SET @Mensaje = 'Pago anulado correctamente.';

        SELECT
            @Resultado AS Resultado,
            @Mensaje AS Mensaje,
            @IdPagoObligacionFinanciera AS IdPagoObligacionFinanciera,
            @TotalPagado AS TotalPagado,
            @SaldoPendiente AS SaldoPendiente,
            @EstadoCuota AS EstadoCuota,
            @EstadoObligacionFinanciera AS EstadoObligacionFinanciera;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;

        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();

        SELECT
            @Resultado AS Resultado,
            @Mensaje AS Mensaje,
            @IdPagoObligacionFinanciera AS IdPagoObligacionFinanciera,
            @TotalPagado AS TotalPagado,
            @SaldoPendiente AS SaldoPendiente,
            @EstadoCuota AS EstadoCuota,
            @EstadoObligacionFinanciera AS EstadoObligacionFinanciera;
    END CATCH;
END;
GO
