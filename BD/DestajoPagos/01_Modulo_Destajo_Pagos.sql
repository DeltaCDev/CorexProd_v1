USE [CorexProdDB]
GO

IF OBJECT_ID('dbo.AreaOperativa', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AreaOperativa
    (
        IdAreaOperativa INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        NombreArea VARCHAR(100) NOT NULL,
        Descripcion VARCHAR(250) NULL,
        Estado BIT NOT NULL CONSTRAINT DF_AreaOperativa_Estado DEFAULT (1),
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_AreaOperativa_FechaRegistro DEFAULT (GETDATE())
    );

    CREATE UNIQUE INDEX UQ_AreaOperativa_NombreArea
        ON dbo.AreaOperativa(NombreArea);
END
GO

IF OBJECT_ID('dbo.ConceptoMovimiento', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConceptoMovimiento
    (
        IdConceptoMovimiento INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CodigoConcepto VARCHAR(40) NOT NULL,
        NombreConcepto VARCHAR(150) NOT NULL,
        TipoMovimiento VARCHAR(30) NOT NULL,
        CategoriaMovimiento VARCHAR(40) NOT NULL,
        TipoCalculo VARCHAR(40) NOT NULL,
        EsDescuento BIT NOT NULL CONSTRAINT DF_ConceptoMovimiento_EsDescuento DEFAULT (0),
        Estado BIT NOT NULL CONSTRAINT DF_ConceptoMovimiento_Estado DEFAULT (1),
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_ConceptoMovimiento_FechaRegistro DEFAULT (GETDATE())
    );

    CREATE UNIQUE INDEX UQ_ConceptoMovimiento_Codigo
        ON dbo.ConceptoMovimiento(CodigoConcepto);
END
GO

IF OBJECT_ID('dbo.TrabajadorOperativo', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TrabajadorOperativo
    (
        IdTrabajadorOperativo INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdEmpleado INT NOT NULL,
        TipoTrabajador VARCHAR(50) NOT NULL,
        MedioPagoPreferido VARCHAR(40) NOT NULL,
        NumeroCuenta VARCHAR(80) NULL,
        TelefonoPago VARCHAR(30) NULL,
        Observacion VARCHAR(250) NULL,
        Estado BIT NOT NULL CONSTRAINT DF_TrabajadorOperativo_Estado DEFAULT (1),
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_TrabajadorOperativo_FechaRegistro DEFAULT (GETDATE()),
        CONSTRAINT FK_TrabajadorOperativo_Empleados
            FOREIGN KEY (IdEmpleado) REFERENCES dbo.Empleados(IdEmpleado)
    );

    CREATE UNIQUE INDEX UQ_TrabajadorOperativo_IdEmpleado
        ON dbo.TrabajadorOperativo(IdEmpleado);
END
GO

IF OBJECT_ID('dbo.TrabajadorAreaOperativa', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TrabajadorAreaOperativa
    (
        IdTrabajadorAreaOperativa INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdTrabajadorOperativo INT NOT NULL,
        IdAreaOperativa INT NOT NULL,
        Estado BIT NOT NULL CONSTRAINT DF_TrabajadorAreaOperativa_Estado DEFAULT (1),
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_TrabajadorAreaOperativa_FechaRegistro DEFAULT (GETDATE()),
        CONSTRAINT FK_TrabajadorAreaOperativa_Trabajador
            FOREIGN KEY (IdTrabajadorOperativo) REFERENCES dbo.TrabajadorOperativo(IdTrabajadorOperativo),
        CONSTRAINT FK_TrabajadorAreaOperativa_Area
            FOREIGN KEY (IdAreaOperativa) REFERENCES dbo.AreaOperativa(IdAreaOperativa)
    );

    CREATE UNIQUE INDEX UQ_TrabajadorAreaOperativa
        ON dbo.TrabajadorAreaOperativa(IdTrabajadorOperativo, IdAreaOperativa);
END
GO

IF OBJECT_ID('dbo.OperacionTextil', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.OperacionTextil
    (
        IdOperacionTextil INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CodigoOperacion VARCHAR(40) NOT NULL,
        NombreOperacion VARCHAR(150) NOT NULL,
        IdAreaOperativa INT NULL,
        TipoOperacion VARCHAR(50) NOT NULL,
        UnidadMedida VARCHAR(40) NOT NULL,
        TarifaBase DECIMAL(18,2) NOT NULL CONSTRAINT DF_OperacionTextil_TarifaBase DEFAULT (0),
        Estado BIT NOT NULL CONSTRAINT DF_OperacionTextil_Estado DEFAULT (1),
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_OperacionTextil_FechaRegistro DEFAULT (GETDATE()),
        CONSTRAINT FK_OperacionTextil_AreaOperativa
            FOREIGN KEY (IdAreaOperativa) REFERENCES dbo.AreaOperativa(IdAreaOperativa)
    );

    CREATE UNIQUE INDEX UQ_OperacionTextil_Codigo
        ON dbo.OperacionTextil(CodigoOperacion);
END
GO

IF OBJECT_ID('dbo.PeriodoPago', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PeriodoPago
    (
        IdPeriodoPago INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CodigoPeriodo VARCHAR(40) NOT NULL,
        FechaInicio DATE NOT NULL,
        FechaFin DATE NOT NULL,
        Estado VARCHAR(40) NOT NULL CONSTRAINT DF_PeriodoPago_Estado DEFAULT ('Borrador'),
        Observacion VARCHAR(300) NULL,
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_PeriodoPago_FechaRegistro DEFAULT (GETDATE())
    );

    CREATE UNIQUE INDEX UQ_PeriodoPago_Codigo
        ON dbo.PeriodoPago(CodigoPeriodo);
END
GO

IF OBJECT_ID('dbo.MovimientoTrabajador', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MovimientoTrabajador
    (
        IdMovimientoTrabajador INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdPeriodoPago INT NOT NULL,
        IdTrabajadorOperativo INT NOT NULL,
        Fecha DATE NOT NULL,
        TipoMovimiento VARCHAR(30) NOT NULL,
        CategoriaMovimiento VARCHAR(40) NOT NULL,
        IdConceptoMovimiento INT NOT NULL,
        Descripcion VARCHAR(250) NULL,
        IdAreaOperativa INT NULL,
        IdOperacionTextil INT NULL,
        Cantidad DECIMAL(18,3) NOT NULL CONSTRAINT DF_MovimientoTrabajador_Cantidad DEFAULT (0),
        UnidadMedida VARCHAR(40) NULL,
        Tarifa DECIMAL(18,4) NOT NULL CONSTRAINT DF_MovimientoTrabajador_Tarifa DEFAULT (0),
        Importe DECIMAL(18,2) NOT NULL,
        EsDescuento BIT NOT NULL CONSTRAINT DF_MovimientoTrabajador_EsDescuento DEFAULT (0),
        EsAutomatico BIT NOT NULL CONSTRAINT DF_MovimientoTrabajador_EsAutomatico DEFAULT (0),
        OrigenMovimiento VARCHAR(40) NOT NULL CONSTRAINT DF_MovimientoTrabajador_Origen DEFAULT ('Manual'),
        ReferenciaId INT NULL,
        Estado VARCHAR(30) NOT NULL CONSTRAINT DF_MovimientoTrabajador_Estado DEFAULT ('Borrador'),
        Observacion VARCHAR(300) NULL,
        Eliminado BIT NOT NULL CONSTRAINT DF_MovimientoTrabajador_Eliminado DEFAULT (0),
        CreadoPor VARCHAR(80) NULL,
        FechaCreacion DATETIME NOT NULL CONSTRAINT DF_MovimientoTrabajador_FechaCreacion DEFAULT (GETDATE()),
        ModificadoPor VARCHAR(80) NULL,
        FechaModificacion DATETIME NULL,
        CONSTRAINT FK_MovimientoTrabajador_Periodo
            FOREIGN KEY (IdPeriodoPago) REFERENCES dbo.PeriodoPago(IdPeriodoPago),
        CONSTRAINT FK_MovimientoTrabajador_Trabajador
            FOREIGN KEY (IdTrabajadorOperativo) REFERENCES dbo.TrabajadorOperativo(IdTrabajadorOperativo),
        CONSTRAINT FK_MovimientoTrabajador_Concepto
            FOREIGN KEY (IdConceptoMovimiento) REFERENCES dbo.ConceptoMovimiento(IdConceptoMovimiento),
        CONSTRAINT FK_MovimientoTrabajador_Area
            FOREIGN KEY (IdAreaOperativa) REFERENCES dbo.AreaOperativa(IdAreaOperativa),
        CONSTRAINT FK_MovimientoTrabajador_Operacion
            FOREIGN KEY (IdOperacionTextil) REFERENCES dbo.OperacionTextil(IdOperacionTextil)
    );

    CREATE INDEX IX_MovimientoTrabajador_Periodo
        ON dbo.MovimientoTrabajador(IdPeriodoPago, IdTrabajadorOperativo, Eliminado);
END
GO

IF OBJECT_ID('dbo.PrestamoTrabajador', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PrestamoTrabajador
    (
        IdPrestamoTrabajador INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdTrabajadorOperativo INT NOT NULL,
        FechaPrestamo DATE NOT NULL,
        MontoTotal DECIMAL(18,2) NOT NULL,
        NumeroCuotas INT NOT NULL,
        MontoCuota DECIMAL(18,2) NOT NULL,
        SaldoPendiente DECIMAL(18,2) NOT NULL,
        Estado VARCHAR(30) NOT NULL CONSTRAINT DF_PrestamoTrabajador_Estado DEFAULT ('Vigente'),
        Observacion VARCHAR(300) NULL,
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_PrestamoTrabajador_FechaRegistro DEFAULT (GETDATE()),
        CONSTRAINT FK_PrestamoTrabajador_Trabajador
            FOREIGN KEY (IdTrabajadorOperativo) REFERENCES dbo.TrabajadorOperativo(IdTrabajadorOperativo)
    );
END
GO

IF OBJECT_ID('dbo.CuotaProgramadaTrabajador', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CuotaProgramadaTrabajador
    (
        IdCuotaProgramada INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        TipoOrigen VARCHAR(40) NOT NULL,
        ReferenciaId INT NOT NULL,
        IdTrabajadorOperativo INT NOT NULL,
        IdConceptoMovimiento INT NOT NULL,
        NumeroCuota INT NOT NULL,
        TotalCuotas INT NOT NULL,
        MontoCuota DECIMAL(18,2) NOT NULL,
        FechaProgramada DATE NOT NULL,
        IdPeriodoAplicado INT NULL,
        Estado VARCHAR(30) NOT NULL CONSTRAINT DF_CuotaProgramadaTrabajador_Estado DEFAULT ('Pendiente'),
        Observacion VARCHAR(300) NULL,
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_CuotaProgramadaTrabajador_FechaRegistro DEFAULT (GETDATE()),
        CONSTRAINT FK_CuotaProgramadaTrabajador_Trabajador
            FOREIGN KEY (IdTrabajadorOperativo) REFERENCES dbo.TrabajadorOperativo(IdTrabajadorOperativo),
        CONSTRAINT FK_CuotaProgramadaTrabajador_Concepto
            FOREIGN KEY (IdConceptoMovimiento) REFERENCES dbo.ConceptoMovimiento(IdConceptoMovimiento),
        CONSTRAINT FK_CuotaProgramadaTrabajador_Periodo
            FOREIGN KEY (IdPeriodoAplicado) REFERENCES dbo.PeriodoPago(IdPeriodoPago)
    );
END
GO

IF OBJECT_ID('dbo.LotePago', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LotePago
    (
        IdLotePago INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdPeriodoPago INT NOT NULL,
        MedioPago VARCHAR(40) NOT NULL,
        FechaGeneracion DATETIME NOT NULL CONSTRAINT DF_LotePago_FechaGeneracion DEFAULT (GETDATE()),
        UsuarioGenerador VARCHAR(80) NULL,
        Estado VARCHAR(30) NOT NULL CONSTRAINT DF_LotePago_Estado DEFAULT ('Generado'),
        TotalLote DECIMAL(18,2) NOT NULL CONSTRAINT DF_LotePago_Total DEFAULT (0),
        Observacion VARCHAR(300) NULL,
        CONSTRAINT FK_LotePago_Periodo
            FOREIGN KEY (IdPeriodoPago) REFERENCES dbo.PeriodoPago(IdPeriodoPago)
    );
END
GO

IF OBJECT_ID('dbo.LotePagoDetalle', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LotePagoDetalle
    (
        IdLotePagoDetalle INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdLotePago INT NOT NULL,
        IdTrabajadorOperativo INT NOT NULL,
        MontoPago DECIMAL(18,2) NOT NULL,
        MedioPago VARCHAR(40) NOT NULL,
        Estado VARCHAR(30) NOT NULL CONSTRAINT DF_LotePagoDetalle_Estado DEFAULT ('Pendiente'),
        CONSTRAINT FK_LotePagoDetalle_Lote
            FOREIGN KEY (IdLotePago) REFERENCES dbo.LotePago(IdLotePago),
        CONSTRAINT FK_LotePagoDetalle_Trabajador
            FOREIGN KEY (IdTrabajadorOperativo) REFERENCES dbo.TrabajadorOperativo(IdTrabajadorOperativo)
    );
END
GO

IF OBJECT_ID('dbo.PagoTrabajador', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PagoTrabajador
    (
        IdPagoTrabajador INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdPeriodoPago INT NOT NULL,
        IdTrabajadorOperativo INT NOT NULL,
        IdLotePagoDetalle INT NULL,
        FechaPago DATETIME NOT NULL CONSTRAINT DF_PagoTrabajador_FechaPago DEFAULT (GETDATE()),
        MedioPago VARCHAR(40) NOT NULL,
        MontoPagado DECIMAL(18,2) NOT NULL,
        Observacion VARCHAR(300) NULL,
        UsuarioRegistro VARCHAR(80) NULL,
        CONSTRAINT FK_PagoTrabajador_Periodo
            FOREIGN KEY (IdPeriodoPago) REFERENCES dbo.PeriodoPago(IdPeriodoPago),
        CONSTRAINT FK_PagoTrabajador_Trabajador
            FOREIGN KEY (IdTrabajadorOperativo) REFERENCES dbo.TrabajadorOperativo(IdTrabajadorOperativo),
        CONSTRAINT FK_PagoTrabajador_LoteDetalle
            FOREIGN KEY (IdLotePagoDetalle) REFERENCES dbo.LotePagoDetalle(IdLotePagoDetalle)
    );
END
GO

MERGE dbo.ConceptoMovimiento AS destino
USING
(
    VALUES
        ('DEST_PROD', 'Produccion a destajo', 'Ingreso', 'Produccion', 'Cantidad x tarifa', 0),
        ('BASICO', 'Basico operativo', 'Ingreso', 'Basico', 'Monto fijo', 0),
        ('HORA_EXTRA', 'Hora extra', 'Ingreso', 'Horas', 'Horas x tarifa', 0),
        ('DOMINGO', 'Domingo trabajado', 'Ingreso', 'Horas', 'Monto fijo', 0),
        ('FERIADO', 'Feriado trabajado', 'Ingreso', 'Horas', 'Monto fijo', 0),
        ('MOVILIDAD', 'Movilidad', 'Ingreso', 'Movilidad', 'Monto fijo', 0),
        ('SALDO_ANT', 'Saldo anterior', 'Ingreso', 'Saldo', 'Ajuste manual', 0),
        ('DESC_AFP', 'Descuento AFP', 'Descuento', 'Legal', 'Monto fijo', 1),
        ('DESC_ONP', 'Descuento ONP', 'Descuento', 'Legal', 'Monto fijo', 1),
        ('CUOTA_PRESTAMO', 'Cuota de prestamo', 'Descuento', 'Financiero', 'Cuota', 1),
        ('ADELANTO', 'Adelanto', 'Descuento', 'Financiero', 'Monto fijo', 1),
        ('DESC_CALIDAD', 'Descuento por calidad', 'Descuento', 'Calidad', 'Cantidad x tarifa', 1),
        ('DESC_DANO', 'Descuento por dano', 'Descuento', 'Calidad', 'Cantidad x tarifa', 1),
        ('AJUSTE_MANUAL', 'Ajuste manual', 'Ajuste', 'Ajuste', 'Ajuste manual', 0),
        ('PAGO_DIRECTO', 'Pago directo', 'Pago', 'Pago', 'Pago directo', 0)
) AS origen(CodigoConcepto, NombreConcepto, TipoMovimiento, CategoriaMovimiento, TipoCalculo, EsDescuento)
ON destino.CodigoConcepto = origen.CodigoConcepto
WHEN MATCHED THEN
    UPDATE SET
        NombreConcepto = origen.NombreConcepto,
        TipoMovimiento = origen.TipoMovimiento,
        CategoriaMovimiento = origen.CategoriaMovimiento,
        TipoCalculo = origen.TipoCalculo,
        EsDescuento = origen.EsDescuento,
        Estado = 1
WHEN NOT MATCHED THEN
    INSERT (CodigoConcepto, NombreConcepto, TipoMovimiento, CategoriaMovimiento, TipoCalculo, EsDescuento, Estado)
    VALUES (origen.CodigoConcepto, origen.NombreConcepto, origen.TipoMovimiento, origen.CategoriaMovimiento, origen.TipoCalculo, origen.EsDescuento, 1);
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_AREA_LISTAR
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        IdAreaOperativa,
        NombreArea,
        ISNULL(Descripcion, '') AS Descripcion,
        Estado,
        FechaRegistro
    FROM dbo.AreaOperativa
    ORDER BY Estado DESC, NombreArea;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_AREA_GUARDAR
(
    @IdAreaOperativa INT,
    @NombreArea VARCHAR(100),
    @Descripcion VARCHAR(250),
    @Estado BIT,
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.AreaOperativa
        WHERE NombreArea = @NombreArea
        AND IdAreaOperativa <> @IdAreaOperativa
    )
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'Ya existe un area con ese nombre.';
        RETURN;
    END;

    IF @IdAreaOperativa = 0
    BEGIN
        INSERT INTO dbo.AreaOperativa(NombreArea, Descripcion, Estado)
        VALUES(@NombreArea, @Descripcion, @Estado);

        SET @Mensaje = 'Area registrada correctamente.';
    END
    ELSE
    BEGIN
        UPDATE dbo.AreaOperativa
        SET NombreArea = @NombreArea,
            Descripcion = @Descripcion,
            Estado = @Estado
        WHERE IdAreaOperativa = @IdAreaOperativa;

        SET @Mensaje = 'Area actualizada correctamente.';
    END;

    SET @Resultado = 1;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_AREA_ELIMINAR_LOGICO
(
    @IdAreaOperativa INT,
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.AreaOperativa
    SET Estado = 0
    WHERE IdAreaOperativa = @IdAreaOperativa;

    SET @Resultado = 1;
    SET @Mensaje = 'Area desactivada correctamente.';
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_CONCEPTO_LISTAR
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        IdConceptoMovimiento,
        CodigoConcepto,
        NombreConcepto,
        TipoMovimiento,
        CategoriaMovimiento,
        TipoCalculo,
        EsDescuento,
        Estado,
        FechaRegistro
    FROM dbo.ConceptoMovimiento
    ORDER BY Estado DESC, TipoMovimiento, NombreConcepto;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_CONCEPTO_GUARDAR
(
    @IdConceptoMovimiento INT,
    @CodigoConcepto VARCHAR(40),
    @NombreConcepto VARCHAR(150),
    @TipoMovimiento VARCHAR(30),
    @CategoriaMovimiento VARCHAR(40),
    @TipoCalculo VARCHAR(40),
    @EsDescuento BIT,
    @Estado BIT,
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.ConceptoMovimiento
        WHERE CodigoConcepto = @CodigoConcepto
        AND IdConceptoMovimiento <> @IdConceptoMovimiento
    )
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'Ya existe un concepto con ese codigo.';
        RETURN;
    END;

    IF @IdConceptoMovimiento = 0
    BEGIN
        INSERT INTO dbo.ConceptoMovimiento
        (
            CodigoConcepto,
            NombreConcepto,
            TipoMovimiento,
            CategoriaMovimiento,
            TipoCalculo,
            EsDescuento,
            Estado
        )
        VALUES
        (
            @CodigoConcepto,
            @NombreConcepto,
            @TipoMovimiento,
            @CategoriaMovimiento,
            @TipoCalculo,
            @EsDescuento,
            @Estado
        );

        SET @Mensaje = 'Concepto registrado correctamente.';
    END
    ELSE
    BEGIN
        UPDATE dbo.ConceptoMovimiento
        SET CodigoConcepto = @CodigoConcepto,
            NombreConcepto = @NombreConcepto,
            TipoMovimiento = @TipoMovimiento,
            CategoriaMovimiento = @CategoriaMovimiento,
            TipoCalculo = @TipoCalculo,
            EsDescuento = @EsDescuento,
            Estado = @Estado
        WHERE IdConceptoMovimiento = @IdConceptoMovimiento;

        SET @Mensaje = 'Concepto actualizado correctamente.';
    END;

    SET @Resultado = 1;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_CONCEPTO_ELIMINAR_LOGICO
(
    @IdConceptoMovimiento INT,
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.ConceptoMovimiento
    SET Estado = 0
    WHERE IdConceptoMovimiento = @IdConceptoMovimiento;

    SET @Resultado = 1;
    SET @Mensaje = 'Concepto desactivado correctamente.';
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_OPERACION_LISTAR
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        O.IdOperacionTextil,
        O.CodigoOperacion,
        O.NombreOperacion,
        O.IdAreaOperativa,
        ISNULL(A.NombreArea, '') AS NombreArea,
        O.TipoOperacion,
        O.UnidadMedida,
        O.TarifaBase,
        O.Estado,
        O.FechaRegistro
    FROM dbo.OperacionTextil O
    LEFT JOIN dbo.AreaOperativa A ON A.IdAreaOperativa = O.IdAreaOperativa
    ORDER BY O.Estado DESC, O.NombreOperacion;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_OPERACION_GUARDAR
(
    @IdOperacionTextil INT,
    @CodigoOperacion VARCHAR(40),
    @NombreOperacion VARCHAR(150),
    @IdAreaOperativa INT = NULL,
    @TipoOperacion VARCHAR(50),
    @UnidadMedida VARCHAR(40),
    @TarifaBase DECIMAL(18,2),
    @Estado BIT,
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.OperacionTextil
        WHERE CodigoOperacion = @CodigoOperacion
        AND IdOperacionTextil <> @IdOperacionTextil
    )
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'Ya existe una operacion con ese codigo.';
        RETURN;
    END;

    IF @IdOperacionTextil = 0
    BEGIN
        INSERT INTO dbo.OperacionTextil
        (
            CodigoOperacion,
            NombreOperacion,
            IdAreaOperativa,
            TipoOperacion,
            UnidadMedida,
            TarifaBase,
            Estado
        )
        VALUES
        (
            @CodigoOperacion,
            @NombreOperacion,
            @IdAreaOperativa,
            @TipoOperacion,
            @UnidadMedida,
            @TarifaBase,
            @Estado
        );

        SET @Mensaje = 'Operacion registrada correctamente.';
    END
    ELSE
    BEGIN
        UPDATE dbo.OperacionTextil
        SET CodigoOperacion = @CodigoOperacion,
            NombreOperacion = @NombreOperacion,
            IdAreaOperativa = @IdAreaOperativa,
            TipoOperacion = @TipoOperacion,
            UnidadMedida = @UnidadMedida,
            TarifaBase = @TarifaBase,
            Estado = @Estado
        WHERE IdOperacionTextil = @IdOperacionTextil;

        SET @Mensaje = 'Operacion actualizada correctamente.';
    END;

    SET @Resultado = 1;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_OPERACION_ELIMINAR_LOGICO
(
    @IdOperacionTextil INT,
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.OperacionTextil
    SET Estado = 0
    WHERE IdOperacionTextil = @IdOperacionTextil;

    SET @Resultado = 1;
    SET @Mensaje = 'Operacion desactivada correctamente.';
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_TRABAJADOR_LISTAR
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        T.IdTrabajadorOperativo,
        T.IdEmpleado,
        CONCAT(E.Nombre, ' ', E.Apellido) AS NombreTrabajador,
        E.NumeroDocumento AS Documento,
        T.TipoTrabajador,
        T.MedioPagoPreferido,
        ISNULL(T.NumeroCuenta, '') AS NumeroCuenta,
        ISNULL(T.TelefonoPago, '') AS TelefonoPago,
        ISNULL(T.Observacion, '') AS Observacion,
        T.Estado,
        T.FechaRegistro
    FROM dbo.TrabajadorOperativo T
    INNER JOIN dbo.Empleados E ON E.IdEmpleado = T.IdEmpleado
    ORDER BY T.Estado DESC, E.Apellido, E.Nombre;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_TRABAJADOR_GUARDAR
(
    @IdTrabajadorOperativo INT,
    @IdEmpleado INT,
    @TipoTrabajador VARCHAR(50),
    @MedioPagoPreferido VARCHAR(40),
    @NumeroCuenta VARCHAR(80),
    @TelefonoPago VARCHAR(30),
    @Observacion VARCHAR(250),
    @Estado BIT,
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.TrabajadorOperativo
        WHERE IdEmpleado = @IdEmpleado
        AND IdTrabajadorOperativo <> @IdTrabajadorOperativo
    )
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'El empleado ya esta registrado como trabajador operativo.';
        RETURN;
    END;

    IF @IdTrabajadorOperativo = 0
    BEGIN
        INSERT INTO dbo.TrabajadorOperativo
        (
            IdEmpleado,
            TipoTrabajador,
            MedioPagoPreferido,
            NumeroCuenta,
            TelefonoPago,
            Observacion,
            Estado
        )
        VALUES
        (
            @IdEmpleado,
            @TipoTrabajador,
            @MedioPagoPreferido,
            @NumeroCuenta,
            @TelefonoPago,
            @Observacion,
            @Estado
        );

        SET @Mensaje = 'Trabajador registrado correctamente.';
    END
    ELSE
    BEGIN
        UPDATE dbo.TrabajadorOperativo
        SET IdEmpleado = @IdEmpleado,
            TipoTrabajador = @TipoTrabajador,
            MedioPagoPreferido = @MedioPagoPreferido,
            NumeroCuenta = @NumeroCuenta,
            TelefonoPago = @TelefonoPago,
            Observacion = @Observacion,
            Estado = @Estado
        WHERE IdTrabajadorOperativo = @IdTrabajadorOperativo;

        SET @Mensaje = 'Trabajador actualizado correctamente.';
    END;

    SET @Resultado = 1;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_TRABAJADOR_ELIMINAR_LOGICO
(
    @IdTrabajadorOperativo INT,
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.TrabajadorOperativo
    SET Estado = 0
    WHERE IdTrabajadorOperativo = @IdTrabajadorOperativo;

    SET @Resultado = 1;
    SET @Mensaje = 'Trabajador desactivado correctamente.';
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_PERIODO_LISTAR
AS
BEGIN
    SET NOCOUNT ON;

    WITH Totales AS
    (
        SELECT
            IdPeriodoPago,
            SUM(CASE WHEN TipoMovimiento <> 'Pago' AND EsDescuento = 0 THEN Importe ELSE 0 END) AS TotalIngresos,
            SUM(CASE WHEN EsDescuento = 1 OR TipoMovimiento = 'Descuento' THEN Importe ELSE 0 END) AS TotalDescuentos,
            SUM(CASE WHEN TipoMovimiento = 'Pago' THEN Importe ELSE 0 END) AS TotalPagado
        FROM dbo.MovimientoTrabajador
        WHERE Eliminado = 0
        GROUP BY IdPeriodoPago
    )
    SELECT
        P.IdPeriodoPago,
        P.CodigoPeriodo,
        P.FechaInicio,
        P.FechaFin,
        P.Estado,
        ISNULL(P.Observacion, '') AS Observacion,
        ISNULL(T.TotalIngresos, 0) AS TotalIngresos,
        ISNULL(T.TotalDescuentos, 0) AS TotalDescuentos,
        ISNULL(T.TotalIngresos, 0) - ISNULL(T.TotalDescuentos, 0) AS NetoCalculado,
        ISNULL(T.TotalPagado, 0) AS TotalPagado,
        ISNULL(T.TotalIngresos, 0) - ISNULL(T.TotalDescuentos, 0) - ISNULL(T.TotalPagado, 0) AS SaldoPendiente,
        P.FechaRegistro
    FROM dbo.PeriodoPago P
    LEFT JOIN Totales T ON T.IdPeriodoPago = P.IdPeriodoPago
    ORDER BY P.FechaInicio DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_PERIODO_GUARDAR
(
    @IdPeriodoPago INT,
    @CodigoPeriodo VARCHAR(40),
    @FechaInicio DATE,
    @FechaFin DATE,
    @Estado VARCHAR(40),
    @Observacion VARCHAR(300),
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.PeriodoPago
        WHERE CodigoPeriodo = @CodigoPeriodo
        AND IdPeriodoPago <> @IdPeriodoPago
    )
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'Ya existe un periodo con ese codigo.';
        RETURN;
    END;

    IF @IdPeriodoPago = 0
    BEGIN
        INSERT INTO dbo.PeriodoPago(CodigoPeriodo, FechaInicio, FechaFin, Estado, Observacion)
        VALUES(@CodigoPeriodo, @FechaInicio, @FechaFin, @Estado, @Observacion);

        SET @Mensaje = 'Periodo registrado correctamente.';
    END
    ELSE
    BEGIN
        IF EXISTS (SELECT 1 FROM dbo.PeriodoPago WHERE IdPeriodoPago = @IdPeriodoPago AND Estado = 'Cerrado')
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'No se puede modificar un periodo cerrado.';
            RETURN;
        END;

        UPDATE dbo.PeriodoPago
        SET CodigoPeriodo = @CodigoPeriodo,
            FechaInicio = @FechaInicio,
            FechaFin = @FechaFin,
            Estado = @Estado,
            Observacion = @Observacion
        WHERE IdPeriodoPago = @IdPeriodoPago;

        SET @Mensaje = 'Periodo actualizado correctamente.';
    END;

    SET @Resultado = 1;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_PERIODO_CAMBIAR_ESTADO
(
    @IdPeriodoPago INT,
    @Estado VARCHAR(40),
    @Usuario VARCHAR(80),
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.PeriodoPago
    SET Estado = @Estado
    WHERE IdPeriodoPago = @IdPeriodoPago;

    INSERT INTO dbo.Auditoria(Usuario, Accion, Modulo, Descripcion, Equipo)
    VALUES(@Usuario, 'CAMBIAR ESTADO', 'DESTAJO Y PAGOS', CONCAT('Periodo ', @IdPeriodoPago, ' cambio a ', @Estado), HOST_NAME());

    SET @Resultado = 1;
    SET @Mensaje = 'Estado actualizado correctamente.';
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_MOVIMIENTO_LISTAR
(
    @IdPeriodoPago INT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        M.IdMovimientoTrabajador,
        M.IdPeriodoPago,
        P.CodigoPeriodo,
        M.IdTrabajadorOperativo,
        CONCAT(E.Nombre, ' ', E.Apellido) AS NombreTrabajador,
        M.Fecha,
        M.TipoMovimiento,
        M.CategoriaMovimiento,
        M.IdConceptoMovimiento,
        C.NombreConcepto,
        ISNULL(M.Descripcion, '') AS Descripcion,
        M.IdAreaOperativa,
        ISNULL(A.NombreArea, '') AS NombreArea,
        M.IdOperacionTextil,
        ISNULL(O.NombreOperacion, '') AS NombreOperacion,
        M.Cantidad,
        ISNULL(M.UnidadMedida, '') AS UnidadMedida,
        M.Tarifa,
        M.Importe,
        M.EsDescuento,
        M.EsAutomatico,
        M.OrigenMovimiento,
        M.ReferenciaId,
        M.Estado,
        ISNULL(M.Observacion, '') AS Observacion,
        ISNULL(M.CreadoPor, '') AS CreadoPor,
        M.FechaCreacion,
        ISNULL(M.ModificadoPor, '') AS ModificadoPor,
        M.FechaModificacion
    FROM dbo.MovimientoTrabajador M
    INNER JOIN dbo.PeriodoPago P ON P.IdPeriodoPago = M.IdPeriodoPago
    INNER JOIN dbo.TrabajadorOperativo T ON T.IdTrabajadorOperativo = M.IdTrabajadorOperativo
    INNER JOIN dbo.Empleados E ON E.IdEmpleado = T.IdEmpleado
    INNER JOIN dbo.ConceptoMovimiento C ON C.IdConceptoMovimiento = M.IdConceptoMovimiento
    LEFT JOIN dbo.AreaOperativa A ON A.IdAreaOperativa = M.IdAreaOperativa
    LEFT JOIN dbo.OperacionTextil O ON O.IdOperacionTextil = M.IdOperacionTextil
    WHERE M.Eliminado = 0
    AND (@IdPeriodoPago IS NULL OR M.IdPeriodoPago = @IdPeriodoPago)
    ORDER BY M.Fecha DESC, M.IdMovimientoTrabajador DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_MOVIMIENTO_GUARDAR
(
    @IdMovimientoTrabajador INT,
    @IdPeriodoPago INT,
    @IdTrabajadorOperativo INT,
    @Fecha DATE,
    @TipoMovimiento VARCHAR(30),
    @CategoriaMovimiento VARCHAR(40),
    @IdConceptoMovimiento INT,
    @Descripcion VARCHAR(250),
    @IdAreaOperativa INT = NULL,
    @IdOperacionTextil INT = NULL,
    @Cantidad DECIMAL(18,3),
    @UnidadMedida VARCHAR(40),
    @Tarifa DECIMAL(18,4),
    @Importe DECIMAL(18,2),
    @EsDescuento BIT,
    @EsAutomatico BIT,
    @OrigenMovimiento VARCHAR(40),
    @ReferenciaId INT = NULL,
    @Estado VARCHAR(30),
    @Observacion VARCHAR(300),
    @Usuario VARCHAR(80),
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.PeriodoPago WHERE IdPeriodoPago = @IdPeriodoPago AND Estado IN ('Cerrado', 'Anulado'))
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'No se puede modificar un periodo cerrado o anulado.';
        RETURN;
    END;

    IF @IdMovimientoTrabajador = 0
    BEGIN
        INSERT INTO dbo.MovimientoTrabajador
        (
            IdPeriodoPago,
            IdTrabajadorOperativo,
            Fecha,
            TipoMovimiento,
            CategoriaMovimiento,
            IdConceptoMovimiento,
            Descripcion,
            IdAreaOperativa,
            IdOperacionTextil,
            Cantidad,
            UnidadMedida,
            Tarifa,
            Importe,
            EsDescuento,
            EsAutomatico,
            OrigenMovimiento,
            ReferenciaId,
            Estado,
            Observacion,
            CreadoPor
        )
        VALUES
        (
            @IdPeriodoPago,
            @IdTrabajadorOperativo,
            @Fecha,
            @TipoMovimiento,
            @CategoriaMovimiento,
            @IdConceptoMovimiento,
            @Descripcion,
            @IdAreaOperativa,
            @IdOperacionTextil,
            @Cantidad,
            @UnidadMedida,
            @Tarifa,
            @Importe,
            @EsDescuento,
            @EsAutomatico,
            @OrigenMovimiento,
            @ReferenciaId,
            @Estado,
            @Observacion,
            @Usuario
        );

        SET @Mensaje = 'Movimiento registrado correctamente.';
    END
    ELSE
    BEGIN
        UPDATE dbo.MovimientoTrabajador
        SET IdPeriodoPago = @IdPeriodoPago,
            IdTrabajadorOperativo = @IdTrabajadorOperativo,
            Fecha = @Fecha,
            TipoMovimiento = @TipoMovimiento,
            CategoriaMovimiento = @CategoriaMovimiento,
            IdConceptoMovimiento = @IdConceptoMovimiento,
            Descripcion = @Descripcion,
            IdAreaOperativa = @IdAreaOperativa,
            IdOperacionTextil = @IdOperacionTextil,
            Cantidad = @Cantidad,
            UnidadMedida = @UnidadMedida,
            Tarifa = @Tarifa,
            Importe = @Importe,
            EsDescuento = @EsDescuento,
            EsAutomatico = @EsAutomatico,
            OrigenMovimiento = @OrigenMovimiento,
            ReferenciaId = @ReferenciaId,
            Estado = @Estado,
            Observacion = @Observacion,
            ModificadoPor = @Usuario,
            FechaModificacion = GETDATE()
        WHERE IdMovimientoTrabajador = @IdMovimientoTrabajador;

        SET @Mensaje = 'Movimiento actualizado correctamente.';
    END;

    INSERT INTO dbo.Auditoria(Usuario, Accion, Modulo, Descripcion, Equipo)
    VALUES(@Usuario, 'GUARDAR', 'DESTAJO Y PAGOS', CONCAT('Movimiento ', @IdMovimientoTrabajador, ' periodo ', @IdPeriodoPago), HOST_NAME());

    SET @Resultado = 1;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_MOVIMIENTO_ELIMINAR_LOGICO
(
    @IdMovimientoTrabajador INT,
    @Usuario VARCHAR(80),
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.MovimientoTrabajador M
        INNER JOIN dbo.PeriodoPago P ON P.IdPeriodoPago = M.IdPeriodoPago
        WHERE M.IdMovimientoTrabajador = @IdMovimientoTrabajador
        AND P.Estado IN ('Cerrado', 'Anulado')
    )
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'No se puede eliminar un movimiento de un periodo cerrado o anulado.';
        RETURN;
    END;

    UPDATE dbo.MovimientoTrabajador
    SET Eliminado = 1,
        Estado = 'Anulado',
        ModificadoPor = @Usuario,
        FechaModificacion = GETDATE()
    WHERE IdMovimientoTrabajador = @IdMovimientoTrabajador;

    INSERT INTO dbo.Auditoria(Usuario, Accion, Modulo, Descripcion, Equipo)
    VALUES(@Usuario, 'ANULAR', 'DESTAJO Y PAGOS', CONCAT('Movimiento anulado ', @IdMovimientoTrabajador), HOST_NAME());

    SET @Resultado = 1;
    SET @Mensaje = 'Movimiento anulado correctamente.';
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_RESUMEN_PERIODO_LISTAR
(
    @IdPeriodoPago INT
)
AS
BEGIN
    SET NOCOUNT ON;

    WITH Totales AS
    (
        SELECT
            M.IdPeriodoPago,
            M.IdTrabajadorOperativo,
            SUM(CASE WHEN M.CategoriaMovimiento = 'Saldo' AND M.EsDescuento = 0 THEN M.Importe ELSE 0 END) AS SaldoAnterior,
            SUM(CASE WHEN M.TipoMovimiento <> 'Pago' AND M.EsDescuento = 0 THEN M.Importe ELSE 0 END) AS TotalIngresos,
            SUM(CASE WHEN M.EsDescuento = 1 OR M.TipoMovimiento = 'Descuento' THEN M.Importe ELSE 0 END) AS TotalDescuentos,
            SUM(CASE WHEN M.TipoMovimiento = 'Pago' THEN M.Importe ELSE 0 END) AS TotalPagado
        FROM dbo.MovimientoTrabajador M
        WHERE M.Eliminado = 0
        AND M.IdPeriodoPago = @IdPeriodoPago
        GROUP BY M.IdPeriodoPago, M.IdTrabajadorOperativo
    )
    SELECT
        P.IdPeriodoPago,
        T.IdTrabajadorOperativo,
        CONCAT(E.Nombre, ' ', E.Apellido) AS NombreTrabajador,
        TR.TipoTrabajador,
        TR.MedioPagoPreferido,
        T.SaldoAnterior,
        T.TotalIngresos,
        T.TotalDescuentos,
        T.TotalIngresos - T.TotalDescuentos AS NetoCalculado,
        T.TotalPagado,
        T.TotalIngresos - T.TotalDescuentos - T.TotalPagado AS SaldoPendiente,
        P.Estado AS EstadoPeriodo
    FROM Totales T
    INNER JOIN dbo.PeriodoPago P ON P.IdPeriodoPago = T.IdPeriodoPago
    INNER JOIN dbo.TrabajadorOperativo TR ON TR.IdTrabajadorOperativo = T.IdTrabajadorOperativo
    INNER JOIN dbo.Empleados E ON E.IdEmpleado = TR.IdEmpleado
    ORDER BY E.Apellido, E.Nombre;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_PRESTAMO_LISTAR
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        P.IdPrestamoTrabajador,
        P.IdTrabajadorOperativo,
        CONCAT(E.Nombre, ' ', E.Apellido) AS NombreTrabajador,
        P.FechaPrestamo,
        P.MontoTotal,
        P.NumeroCuotas,
        P.MontoCuota,
        P.SaldoPendiente,
        P.Estado,
        ISNULL(P.Observacion, '') AS Observacion,
        P.FechaRegistro
    FROM dbo.PrestamoTrabajador P
    INNER JOIN dbo.TrabajadorOperativo T ON T.IdTrabajadorOperativo = P.IdTrabajadorOperativo
    INNER JOIN dbo.Empleados E ON E.IdEmpleado = T.IdEmpleado
    ORDER BY P.FechaPrestamo DESC, P.IdPrestamoTrabajador DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_PRESTAMO_REGISTRAR
(
    @IdTrabajadorOperativo INT,
    @FechaPrestamo DATE,
    @MontoTotal DECIMAL(18,2),
    @NumeroCuotas INT,
    @MontoCuota DECIMAL(18,2),
    @Observacion VARCHAR(300),
    @IdConceptoMovimiento INT,
    @Usuario VARCHAR(80),
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO dbo.PrestamoTrabajador
        (
            IdTrabajadorOperativo,
            FechaPrestamo,
            MontoTotal,
            NumeroCuotas,
            MontoCuota,
            SaldoPendiente,
            Observacion
        )
        VALUES
        (
            @IdTrabajadorOperativo,
            @FechaPrestamo,
            @MontoTotal,
            @NumeroCuotas,
            @MontoCuota,
            @MontoTotal,
            @Observacion
        );

        DECLARE @IdPrestamoTrabajador INT = SCOPE_IDENTITY();
        DECLARE @Numero INT = 1;
        DECLARE @MontoActual DECIMAL(18,2);

        WHILE @Numero <= @NumeroCuotas
        BEGIN
            SET @MontoActual =
                CASE
                    WHEN @Numero = @NumeroCuotas
                    THEN @MontoTotal - (@MontoCuota * (@NumeroCuotas - 1))
                    ELSE @MontoCuota
                END;

            INSERT INTO dbo.CuotaProgramadaTrabajador
            (
                TipoOrigen,
                ReferenciaId,
                IdTrabajadorOperativo,
                IdConceptoMovimiento,
                NumeroCuota,
                TotalCuotas,
                MontoCuota,
                FechaProgramada,
                Observacion
            )
            VALUES
            (
                'Prestamo',
                @IdPrestamoTrabajador,
                @IdTrabajadorOperativo,
                @IdConceptoMovimiento,
                @Numero,
                @NumeroCuotas,
                @MontoActual,
                DATEADD(WEEK, @Numero - 1, @FechaPrestamo),
                @Observacion
            );

            SET @Numero += 1;
        END;

        INSERT INTO dbo.Auditoria(Usuario, Accion, Modulo, Descripcion, Equipo)
        VALUES(@Usuario, 'REGISTRAR', 'DESTAJO Y PAGOS', CONCAT('Prestamo registrado ', @IdPrestamoTrabajador), HOST_NAME());

        COMMIT TRANSACTION;

        SET @Resultado = 1;
        SET @Mensaje = 'Prestamo registrado correctamente.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_CUOTA_LISTAR
(
    @IdTrabajadorOperativo INT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        C.IdCuotaProgramada,
        C.TipoOrigen,
        C.ReferenciaId,
        C.IdTrabajadorOperativo,
        CONCAT(E.Nombre, ' ', E.Apellido) AS NombreTrabajador,
        C.IdConceptoMovimiento,
        CM.NombreConcepto,
        C.NumeroCuota,
        C.TotalCuotas,
        C.MontoCuota,
        C.FechaProgramada,
        C.IdPeriodoAplicado,
        ISNULL(P.CodigoPeriodo, '') AS CodigoPeriodoAplicado,
        C.Estado,
        ISNULL(C.Observacion, '') AS Observacion
    FROM dbo.CuotaProgramadaTrabajador C
    INNER JOIN dbo.TrabajadorOperativo T ON T.IdTrabajadorOperativo = C.IdTrabajadorOperativo
    INNER JOIN dbo.Empleados E ON E.IdEmpleado = T.IdEmpleado
    INNER JOIN dbo.ConceptoMovimiento CM ON CM.IdConceptoMovimiento = C.IdConceptoMovimiento
    LEFT JOIN dbo.PeriodoPago P ON P.IdPeriodoPago = C.IdPeriodoAplicado
    WHERE (@IdTrabajadorOperativo IS NULL OR C.IdTrabajadorOperativo = @IdTrabajadorOperativo)
    ORDER BY C.Estado, C.FechaProgramada, C.NumeroCuota;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_CUOTA_APLICAR
(
    @IdCuotaProgramada INT,
    @IdPeriodoPago INT,
    @Usuario VARCHAR(80),
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF EXISTS (SELECT 1 FROM dbo.PeriodoPago WHERE IdPeriodoPago = @IdPeriodoPago AND Estado IN ('Cerrado', 'Anulado'))
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'No se puede aplicar una cuota a un periodo cerrado o anulado.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        DECLARE
            @IdTrabajadorOperativo INT,
            @IdConceptoMovimiento INT,
            @MontoCuota DECIMAL(18,2),
            @ReferenciaId INT,
            @NumeroCuota INT,
            @TotalCuotas INT;

        SELECT
            @IdTrabajadorOperativo = IdTrabajadorOperativo,
            @IdConceptoMovimiento = IdConceptoMovimiento,
            @MontoCuota = MontoCuota,
            @ReferenciaId = ReferenciaId,
            @NumeroCuota = NumeroCuota,
            @TotalCuotas = TotalCuotas
        FROM dbo.CuotaProgramadaTrabajador
        WHERE IdCuotaProgramada = @IdCuotaProgramada
        AND Estado = 'Pendiente';

        IF @IdTrabajadorOperativo IS NULL
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'La cuota no existe o ya fue aplicada.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        INSERT INTO dbo.MovimientoTrabajador
        (
            IdPeriodoPago,
            IdTrabajadorOperativo,
            Fecha,
            TipoMovimiento,
            CategoriaMovimiento,
            IdConceptoMovimiento,
            Descripcion,
            Cantidad,
            UnidadMedida,
            Tarifa,
            Importe,
            EsDescuento,
            EsAutomatico,
            OrigenMovimiento,
            ReferenciaId,
            Estado,
            CreadoPor
        )
        VALUES
        (
            @IdPeriodoPago,
            @IdTrabajadorOperativo,
            GETDATE(),
            'Descuento',
            'Financiero',
            @IdConceptoMovimiento,
            CONCAT('Cuota ', @NumeroCuota, ' de ', @TotalCuotas),
            1,
            'Cuota',
            @MontoCuota,
            @MontoCuota,
            1,
            1,
            'Cuota',
            @IdCuotaProgramada,
            'Aprobado',
            @Usuario
        );

        UPDATE dbo.CuotaProgramadaTrabajador
        SET Estado = 'Aplicada',
            IdPeriodoAplicado = @IdPeriodoPago
        WHERE IdCuotaProgramada = @IdCuotaProgramada;

        IF EXISTS (SELECT 1 FROM dbo.PrestamoTrabajador WHERE IdPrestamoTrabajador = @ReferenciaId)
        BEGIN
            UPDATE dbo.PrestamoTrabajador
            SET SaldoPendiente = CASE WHEN SaldoPendiente - @MontoCuota < 0 THEN 0 ELSE SaldoPendiente - @MontoCuota END
            WHERE IdPrestamoTrabajador = @ReferenciaId;

            UPDATE dbo.PrestamoTrabajador
            SET Estado = 'Cancelado'
            WHERE IdPrestamoTrabajador = @ReferenciaId
            AND SaldoPendiente <= 0;
        END;

        INSERT INTO dbo.Auditoria(Usuario, Accion, Modulo, Descripcion, Equipo)
        VALUES(@Usuario, 'APLICAR CUOTA', 'DESTAJO Y PAGOS', CONCAT('Cuota aplicada ', @IdCuotaProgramada), HOST_NAME());

        COMMIT TRANSACTION;

        SET @Resultado = 1;
        SET @Mensaje = 'Cuota aplicada correctamente.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_LOTE_LISTAR
(
    @IdPeriodoPago INT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        L.IdLotePago,
        L.IdPeriodoPago,
        P.CodigoPeriodo,
        L.MedioPago,
        L.FechaGeneracion,
        ISNULL(L.UsuarioGenerador, '') AS UsuarioGenerador,
        L.Estado,
        L.TotalLote,
        ISNULL(L.Observacion, '') AS Observacion
    FROM dbo.LotePago L
    INNER JOIN dbo.PeriodoPago P ON P.IdPeriodoPago = L.IdPeriodoPago
    WHERE (@IdPeriodoPago IS NULL OR L.IdPeriodoPago = @IdPeriodoPago)
    ORDER BY L.FechaGeneracion DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_LOTE_DETALLE_LISTAR
(
    @IdLotePago INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        D.IdLotePagoDetalle,
        D.IdLotePago,
        D.IdTrabajadorOperativo,
        CONCAT(E.Nombre, ' ', E.Apellido) AS NombreTrabajador,
        D.MontoPago,
        D.MedioPago,
        D.Estado,
        ISNULL(T.NumeroCuenta, '') AS NumeroCuenta,
        ISNULL(T.TelefonoPago, '') AS TelefonoPago
    FROM dbo.LotePagoDetalle D
    INNER JOIN dbo.TrabajadorOperativo T ON T.IdTrabajadorOperativo = D.IdTrabajadorOperativo
    INNER JOIN dbo.Empleados E ON E.IdEmpleado = T.IdEmpleado
    WHERE D.IdLotePago = @IdLotePago
    ORDER BY E.Apellido, E.Nombre;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_LOTE_GENERAR
(
    @IdPeriodoPago INT,
    @MedioPago VARCHAR(40),
    @UsuarioGenerador VARCHAR(80),
    @Observacion VARCHAR(300),
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF EXISTS
        (
            SELECT 1
            FROM dbo.LotePago
            WHERE IdPeriodoPago = @IdPeriodoPago
            AND MedioPago = @MedioPago
            AND Estado <> 'Anulado'
        )
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'Ya existe un lote activo para ese periodo y medio de pago.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        INSERT INTO dbo.LotePago(IdPeriodoPago, MedioPago, UsuarioGenerador, Observacion)
        VALUES(@IdPeriodoPago, @MedioPago, @UsuarioGenerador, @Observacion);

        DECLARE @IdLotePago INT = SCOPE_IDENTITY();

        WITH Totales AS
        (
            SELECT
                M.IdTrabajadorOperativo,
                SUM(CASE WHEN M.TipoMovimiento <> 'Pago' AND M.EsDescuento = 0 THEN M.Importe ELSE 0 END) AS TotalIngresos,
                SUM(CASE WHEN M.EsDescuento = 1 OR M.TipoMovimiento = 'Descuento' THEN M.Importe ELSE 0 END) AS TotalDescuentos,
                SUM(CASE WHEN M.TipoMovimiento = 'Pago' THEN M.Importe ELSE 0 END) AS TotalPagado
            FROM dbo.MovimientoTrabajador M
            WHERE M.IdPeriodoPago = @IdPeriodoPago
            AND M.Eliminado = 0
            GROUP BY M.IdTrabajadorOperativo
        )
        INSERT INTO dbo.LotePagoDetalle(IdLotePago, IdTrabajadorOperativo, MontoPago, MedioPago)
        SELECT
            @IdLotePago,
            T.IdTrabajadorOperativo,
            T.TotalIngresos - T.TotalDescuentos - T.TotalPagado,
            @MedioPago
        FROM Totales T
        INNER JOIN dbo.TrabajadorOperativo TR ON TR.IdTrabajadorOperativo = T.IdTrabajadorOperativo
        WHERE T.TotalIngresos - T.TotalDescuentos - T.TotalPagado > 0
        AND (@MedioPago = 'Mixto' OR TR.MedioPagoPreferido = @MedioPago);

        IF NOT EXISTS (SELECT 1 FROM dbo.LotePagoDetalle WHERE IdLotePago = @IdLotePago)
        BEGIN
            DELETE FROM dbo.LotePago WHERE IdLotePago = @IdLotePago;
            SET @Resultado = 0;
            SET @Mensaje = 'No existen pagos pendientes para generar el lote.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        UPDATE dbo.LotePago
        SET TotalLote =
        (
            SELECT SUM(MontoPago)
            FROM dbo.LotePagoDetalle
            WHERE IdLotePago = @IdLotePago
        )
        WHERE IdLotePago = @IdLotePago;

        INSERT INTO dbo.Auditoria(Usuario, Accion, Modulo, Descripcion, Equipo)
        VALUES(@UsuarioGenerador, 'GENERAR LOTE', 'DESTAJO Y PAGOS', CONCAT('Lote generado ', @IdLotePago), HOST_NAME());

        COMMIT TRANSACTION;

        SET @Resultado = 1;
        SET @Mensaje = 'Lote generado correctamente.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_LOTE_CAMBIAR_ESTADO
(
    @IdLotePago INT,
    @Estado VARCHAR(30),
    @Usuario VARCHAR(80),
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE dbo.LotePago
        SET Estado = @Estado
        WHERE IdLotePago = @IdLotePago;

        IF @Estado IN ('Pagado', 'Pagado / Cerrado')
        BEGIN
            DECLARE @IdPeriodoPago INT;
            DECLARE @IdConceptoPago INT;

            SELECT @IdPeriodoPago = IdPeriodoPago
            FROM dbo.LotePago
            WHERE IdLotePago = @IdLotePago;

            SELECT @IdConceptoPago = IdConceptoMovimiento
            FROM dbo.ConceptoMovimiento
            WHERE CodigoConcepto = 'PAGO_DIRECTO';

            INSERT INTO dbo.PagoTrabajador
            (
                IdPeriodoPago,
                IdTrabajadorOperativo,
                IdLotePagoDetalle,
                MedioPago,
                MontoPagado,
                Observacion,
                UsuarioRegistro
            )
            SELECT
                @IdPeriodoPago,
                D.IdTrabajadorOperativo,
                D.IdLotePagoDetalle,
                D.MedioPago,
                D.MontoPago,
                'Pago desde lote',
                @Usuario
            FROM dbo.LotePagoDetalle D
            WHERE D.IdLotePago = @IdLotePago
            AND D.Estado <> 'Pagado'
            AND NOT EXISTS
            (
                SELECT 1
                FROM dbo.PagoTrabajador P
                WHERE P.IdLotePagoDetalle = D.IdLotePagoDetalle
            );

            INSERT INTO dbo.MovimientoTrabajador
            (
                IdPeriodoPago,
                IdTrabajadorOperativo,
                Fecha,
                TipoMovimiento,
                CategoriaMovimiento,
                IdConceptoMovimiento,
                Descripcion,
                Cantidad,
                UnidadMedida,
                Tarifa,
                Importe,
                EsDescuento,
                EsAutomatico,
                OrigenMovimiento,
                ReferenciaId,
                Estado,
                CreadoPor
            )
            SELECT
                @IdPeriodoPago,
                D.IdTrabajadorOperativo,
                GETDATE(),
                'Pago',
                'Pago',
                @IdConceptoPago,
                CONCAT('Pago por ', D.MedioPago),
                1,
                'Pago',
                D.MontoPago,
                D.MontoPago,
                0,
                1,
                'LotePago',
                D.IdLotePagoDetalle,
                'Aprobado',
                @Usuario
            FROM dbo.LotePagoDetalle D
            WHERE D.IdLotePago = @IdLotePago
            AND NOT EXISTS
            (
                SELECT 1
                FROM dbo.MovimientoTrabajador M
                WHERE M.OrigenMovimiento = 'LotePago'
                AND M.ReferenciaId = D.IdLotePagoDetalle
                AND M.Eliminado = 0
            );

            UPDATE dbo.LotePagoDetalle
            SET Estado = 'Pagado / Cerrado'
            WHERE IdLotePago = @IdLotePago;
        END
        ELSE
        BEGIN
            UPDATE dbo.LotePagoDetalle
            SET Estado = @Estado
            WHERE IdLotePago = @IdLotePago;
        END;

        INSERT INTO dbo.Auditoria(Usuario, Accion, Modulo, Descripcion, Equipo)
        VALUES(@Usuario, 'CAMBIAR ESTADO LOTE', 'DESTAJO Y PAGOS', CONCAT('Lote ', @IdLotePago, ' cambio a ', @Estado), HOST_NAME());

        COMMIT TRANSACTION;

        SET @Resultado = 1;
        SET @Mensaje = 'Lote actualizado correctamente.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_PAGO_TRABAJADOR_REGISTRAR
(
    @IdPeriodoPago INT,
    @IdTrabajadorOperativo INT,
    @IdLotePagoDetalle INT = NULL,
    @MedioPago VARCHAR(40),
    @MontoPagado DECIMAL(18,2),
    @Observacion VARCHAR(300),
    @Usuario VARCHAR(80),
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF EXISTS (SELECT 1 FROM dbo.PeriodoPago WHERE IdPeriodoPago = @IdPeriodoPago AND Estado IN ('Cerrado', 'Anulado', 'Pagado / Cerrado'))
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'No se puede registrar pagos en un periodo cerrado.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        DECLARE @IdConceptoPago INT;

        SELECT @IdConceptoPago = IdConceptoMovimiento
        FROM dbo.ConceptoMovimiento
        WHERE CodigoConcepto = 'PAGO_DIRECTO';

        IF @IdConceptoPago IS NULL
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'No existe el concepto PAGO_DIRECTO.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        DECLARE @SaldoPendiente DECIMAL(18,2);

        WITH Totales AS
        (
            SELECT
                SUM(CASE WHEN M.TipoMovimiento <> 'Pago' AND M.EsDescuento = 0 THEN M.Importe ELSE 0 END) AS TotalIngresos,
                SUM(CASE WHEN M.EsDescuento = 1 OR M.TipoMovimiento = 'Descuento' THEN M.Importe ELSE 0 END) AS TotalDescuentos,
                SUM(CASE WHEN M.TipoMovimiento = 'Pago' THEN M.Importe ELSE 0 END) AS TotalPagado
            FROM dbo.MovimientoTrabajador M
            WHERE M.IdPeriodoPago = @IdPeriodoPago
            AND M.IdTrabajadorOperativo = @IdTrabajadorOperativo
            AND M.Eliminado = 0
        )
        SELECT @SaldoPendiente =
            ISNULL(TotalIngresos, 0) - ISNULL(TotalDescuentos, 0) - ISNULL(TotalPagado, 0)
        FROM Totales;

        IF ISNULL(@SaldoPendiente, 0) <= 0
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'El trabajador no tiene saldo pendiente.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        IF @MontoPagado <= 0 OR @MontoPagado > @SaldoPendiente
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'El monto a pagar debe ser mayor a cero y no superar el saldo pendiente.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        IF @IdLotePagoDetalle IS NOT NULL
        AND NOT EXISTS
        (
            SELECT 1
            FROM dbo.LotePagoDetalle D
            INNER JOIN dbo.LotePago L ON L.IdLotePago = D.IdLotePago
            WHERE D.IdLotePagoDetalle = @IdLotePagoDetalle
            AND L.IdPeriodoPago = @IdPeriodoPago
            AND D.IdTrabajadorOperativo = @IdTrabajadorOperativo
        )
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'El detalle de lote no corresponde al trabajador seleccionado.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        INSERT INTO dbo.PagoTrabajador
        (
            IdPeriodoPago,
            IdTrabajadorOperativo,
            IdLotePagoDetalle,
            MedioPago,
            MontoPagado,
            Observacion,
            UsuarioRegistro
        )
        VALUES
        (
            @IdPeriodoPago,
            @IdTrabajadorOperativo,
            @IdLotePagoDetalle,
            @MedioPago,
            @MontoPagado,
            @Observacion,
            @Usuario
        );

        DECLARE @IdPagoTrabajador INT = SCOPE_IDENTITY();

        INSERT INTO dbo.MovimientoTrabajador
        (
            IdPeriodoPago,
            IdTrabajadorOperativo,
            Fecha,
            TipoMovimiento,
            CategoriaMovimiento,
            IdConceptoMovimiento,
            Descripcion,
            Cantidad,
            UnidadMedida,
            Tarifa,
            Importe,
            EsDescuento,
            EsAutomatico,
            OrigenMovimiento,
            ReferenciaId,
            Estado,
            Observacion,
            CreadoPor
        )
        VALUES
        (
            @IdPeriodoPago,
            @IdTrabajadorOperativo,
            GETDATE(),
            'Pago',
            'Pago',
            @IdConceptoPago,
            CONCAT('Pago por ', @MedioPago),
            1,
            'Pago',
            @MontoPagado,
            @MontoPagado,
            0,
            1,
            'PagoTrabajador',
            @IdPagoTrabajador,
            'Aprobado',
            @Observacion,
            @Usuario
        );

        IF @IdLotePagoDetalle IS NOT NULL
        BEGIN
            UPDATE D
            SET Estado =
                CASE
                    WHEN @SaldoPendiente - @MontoPagado <= 0 THEN 'Pagado / Cerrado'
                    ELSE 'Pago Parcial'
                END
            FROM dbo.LotePagoDetalle D
            WHERE D.IdLotePagoDetalle = @IdLotePagoDetalle;

            UPDATE L
            SET Estado =
                CASE
                    WHEN EXISTS
                    (
                        SELECT 1
                        FROM dbo.LotePagoDetalle D
                        WHERE D.IdLotePago = L.IdLotePago
                        AND D.Estado IN ('Pendiente', 'Pago Parcial')
                    )
                    THEN 'Pago Parcial'
                    ELSE 'Pagado / Cerrado'
                END
            FROM dbo.LotePago L
            INNER JOIN dbo.LotePagoDetalle D ON D.IdLotePago = L.IdLotePago
            WHERE D.IdLotePagoDetalle = @IdLotePagoDetalle;
        END;

        INSERT INTO dbo.Auditoria(Usuario, Accion, Modulo, Descripcion, Equipo)
        VALUES(@Usuario, 'REGISTRAR PAGO', 'DESTAJO Y PAGOS', CONCAT('Pago trabajador ', @IdPagoTrabajador), HOST_NAME());

        COMMIT TRANSACTION;

        SET @Resultado = 1;
        SET @Mensaje =
            CASE
                WHEN @SaldoPendiente - @MontoPagado <= 0 THEN 'Pago completo registrado correctamente.'
                ELSE 'Pago parcial registrado correctamente.'
            END;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH;
END
GO

DECLARE @IdMenuDestajo INT;
DECLARE @MenuPrestamosCorrupto VARCHAR(80) = 'Pr' + CHAR(195) + CHAR(169) + 'stamos y Cuotas';

IF EXISTS (SELECT 1 FROM dbo.Menu WHERE NombreMenu = @MenuPrestamosCorrupto)
AND NOT EXISTS (SELECT 1 FROM dbo.Menu WHERE NombreMenu = 'Prestamos y Cuotas')
BEGIN
    UPDATE dbo.Menu
    SET NombreMenu = 'Prestamos y Cuotas'
    WHERE NombreMenu = @MenuPrestamosCorrupto;
END;

IF EXISTS (SELECT 1 FROM dbo.Menu WHERE NombreMenu = @MenuPrestamosCorrupto)
AND EXISTS (SELECT 1 FROM dbo.Menu WHERE NombreMenu = 'Prestamos y Cuotas')
BEGIN
    UPDATE dbo.Menu
    SET Estado = 0
    WHERE NombreMenu = @MenuPrestamosCorrupto;
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Menu WHERE NombreMenu = 'Destajo y Pagos')
BEGIN
    INSERT INTO dbo.Menu(NombreMenu, IdMenuPadre, Orden, Estado)
    VALUES('Destajo y Pagos', NULL, 6, 1);
END;

SELECT @IdMenuDestajo = IdMenu
FROM dbo.Menu
WHERE NombreMenu = 'Destajo y Pagos';

DECLARE @MenuModulo TABLE(NombreMenu VARCHAR(80), Orden INT);

INSERT INTO @MenuModulo(NombreMenu, Orden)
VALUES
    ('Panel de Destajo', 1),
    ('Periodos de Pago', 2),
    ('Movimientos Operativos', 3),
    ('Prestamos y Cuotas', 4),
    ('Lotes de Pago', 5),
    ('Reportes de Pagos', 6);

INSERT INTO dbo.Menu(NombreMenu, IdMenuPadre, Orden, Estado)
SELECT M.NombreMenu, @IdMenuDestajo, M.Orden, 1
FROM @MenuModulo M
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.Menu X
    WHERE X.NombreMenu = M.NombreMenu
);

INSERT INTO dbo.PermisosMenu(IdRol, IdMenu, PuedeVer)
SELECT
    R.IdRol,
    M.IdMenu,
    CASE WHEN R.NombreRol = 'Administrador' THEN 1 ELSE 0 END
FROM dbo.Roles R
CROSS JOIN dbo.Menu M
WHERE M.NombreMenu IN
(
    'Destajo y Pagos',
    'Panel de Destajo',
    'Periodos de Pago',
    'Movimientos Operativos',
    'Prestamos y Cuotas',
    'Lotes de Pago',
    'Reportes de Pagos'
)
AND NOT EXISTS
(
    SELECT 1
    FROM dbo.PermisosMenu PM
    WHERE PM.IdRol = R.IdRol
    AND PM.IdMenu = M.IdMenu
);
GO
