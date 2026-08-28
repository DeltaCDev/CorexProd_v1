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
        FechaInicioVigencia DATE NULL,
        FechaFinVigencia DATE NULL,
        Estado BIT NOT NULL CONSTRAINT DF_OperacionTextil_Estado DEFAULT (1),
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_OperacionTextil_FechaRegistro DEFAULT (GETDATE()),
        CONSTRAINT FK_OperacionTextil_AreaOperativa
            FOREIGN KEY (IdAreaOperativa) REFERENCES dbo.AreaOperativa(IdAreaOperativa)
    );

    CREATE UNIQUE INDEX UQ_OperacionTextil_Codigo
        ON dbo.OperacionTextil(CodigoOperacion);
END
GO

IF COL_LENGTH('dbo.OperacionTextil', 'FechaInicioVigencia') IS NULL
BEGIN
    ALTER TABLE dbo.OperacionTextil ADD FechaInicioVigencia DATE NULL;
END
GO

IF COL_LENGTH('dbo.OperacionTextil', 'FechaFinVigencia') IS NULL
BEGIN
    ALTER TABLE dbo.OperacionTextil ADD FechaFinVigencia DATE NULL;
END
GO

IF OBJECT_ID('dbo.PeriodoPago', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PeriodoPago
    (
        IdPeriodoPago INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CodigoPeriodo VARCHAR(40) NOT NULL,
        NumeroSemana INT NULL,
        Anio INT NULL,
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

IF COL_LENGTH('dbo.PeriodoPago', 'NumeroSemana') IS NULL
BEGIN
    ALTER TABLE dbo.PeriodoPago ADD NumeroSemana INT NULL;
END
GO

IF COL_LENGTH('dbo.PeriodoPago', 'Anio') IS NULL
BEGIN
    ALTER TABLE dbo.PeriodoPago ADD Anio INT NULL;
END
GO

IF COL_LENGTH('dbo.PeriodoPago', 'BoletasGeneradas') IS NULL
BEGIN
    ALTER TABLE dbo.PeriodoPago
        ADD BoletasGeneradas BIT NOT NULL CONSTRAINT DF_PeriodoPago_BoletasGeneradas DEFAULT (0),
            FechaBoletasGeneradas DATETIME NULL,
            CantidadBoletasGeneradas INT NOT NULL CONSTRAINT DF_PeriodoPago_CantidadBoletas DEFAULT (0),
            SaldosTrasladados BIT NOT NULL CONSTRAINT DF_PeriodoPago_SaldosTrasladados DEFAULT (0),
            FechaCierre DATETIME NULL,
            UsuarioCierre VARCHAR(80) NULL;
END
GO

IF OBJECT_ID('dbo.Auditoria', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Auditoria
    (
        IdAuditoria INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Usuario NVARCHAR(100) NULL,
        Accion NVARCHAR(100) NULL,
        Modulo NVARCHAR(100) NULL,
        Descripcion NVARCHAR(MAX) NULL,
        Fecha DATETIME NOT NULL CONSTRAINT DF_Auditoria_Fecha DEFAULT (GETDATE()),
        Equipo NVARCHAR(100) NULL
    );
END
GO

IF COL_LENGTH('dbo.Auditoria', 'RegistroAfectado') IS NULL
BEGIN
    ALTER TABLE dbo.Auditoria
        ADD RegistroAfectado NVARCHAR(150) NULL,
            ValorAnterior NVARCHAR(MAX) NULL,
            ValorNuevo NVARCHAR(MAX) NULL,
            Motivo NVARCHAR(500) NULL;
END
GO

UPDATE dbo.PeriodoPago
SET NumeroSemana = ISNULL(NumeroSemana, DATEPART(ISO_WEEK, FechaInicio)),
    Anio = ISNULL(Anio, DATEPART(YEAR, DATEADD(DAY, 4 - DATEPART(WEEKDAY, FechaInicio), FechaInicio))),
    Estado = CASE
        WHEN Estado IN ('Pendiente', 'Borrador') THEN 'Borrador'
        WHEN Estado IN ('Aprobado', 'Abierto') THEN 'Abierto'
        WHEN Estado IN ('Pago Parcial', 'En pago') THEN 'En pago'
        WHEN Estado IN ('Pagado / Cerrado', 'Cerrado') THEN 'Cerrado'
        ELSE Estado
    END;
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
        FechaInicioDescuento DATE NOT NULL,
        IdConceptoMovimiento INT NULL,
        MontoTotal DECIMAL(18,2) NOT NULL,
        NumeroCuotas INT NOT NULL,
        MontoCuota DECIMAL(18,2) NOT NULL,
        SaldoPendiente DECIMAL(18,2) NOT NULL,
        Estado VARCHAR(30) NOT NULL CONSTRAINT DF_PrestamoTrabajador_Estado DEFAULT ('Registrado'),
        Observacion VARCHAR(300) NULL,
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_PrestamoTrabajador_FechaRegistro DEFAULT (GETDATE()),
        CONSTRAINT FK_PrestamoTrabajador_Trabajador
            FOREIGN KEY (IdTrabajadorOperativo) REFERENCES dbo.TrabajadorOperativo(IdTrabajadorOperativo),
        CONSTRAINT FK_PrestamoTrabajador_Concepto
            FOREIGN KEY (IdConceptoMovimiento) REFERENCES dbo.ConceptoMovimiento(IdConceptoMovimiento)
    );
END
GO

IF COL_LENGTH('dbo.PrestamoTrabajador', 'FechaInicioDescuento') IS NULL
BEGIN
    ALTER TABLE dbo.PrestamoTrabajador
        ADD FechaInicioDescuento DATE NULL;

    EXEC('UPDATE dbo.PrestamoTrabajador SET FechaInicioDescuento = FechaPrestamo WHERE FechaInicioDescuento IS NULL;');

    ALTER TABLE dbo.PrestamoTrabajador
        ALTER COLUMN FechaInicioDescuento DATE NOT NULL;
END
GO

IF COL_LENGTH('dbo.PrestamoTrabajador', 'IdConceptoMovimiento') IS NULL
BEGIN
    ALTER TABLE dbo.PrestamoTrabajador
        ADD IdConceptoMovimiento INT NULL;

    ALTER TABLE dbo.PrestamoTrabajador
        ADD CONSTRAINT FK_PrestamoTrabajador_Concepto
            FOREIGN KEY (IdConceptoMovimiento) REFERENCES dbo.ConceptoMovimiento(IdConceptoMovimiento);
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
        IdMovimientoTrabajador INT NULL,
        FechaAplicacion DATETIME NULL,
        Estado VARCHAR(30) NOT NULL CONSTRAINT DF_CuotaProgramadaTrabajador_Estado DEFAULT ('Pendiente'),
        Observacion VARCHAR(300) NULL,
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_CuotaProgramadaTrabajador_FechaRegistro DEFAULT (GETDATE()),
        CONSTRAINT FK_CuotaProgramadaTrabajador_Trabajador
            FOREIGN KEY (IdTrabajadorOperativo) REFERENCES dbo.TrabajadorOperativo(IdTrabajadorOperativo),
        CONSTRAINT FK_CuotaProgramadaTrabajador_Concepto
            FOREIGN KEY (IdConceptoMovimiento) REFERENCES dbo.ConceptoMovimiento(IdConceptoMovimiento),
        CONSTRAINT FK_CuotaProgramadaTrabajador_Periodo
            FOREIGN KEY (IdPeriodoAplicado) REFERENCES dbo.PeriodoPago(IdPeriodoPago),
        CONSTRAINT FK_CuotaProgramadaTrabajador_Movimiento
            FOREIGN KEY (IdMovimientoTrabajador) REFERENCES dbo.MovimientoTrabajador(IdMovimientoTrabajador)
    );
END
GO

IF COL_LENGTH('dbo.CuotaProgramadaTrabajador', 'IdMovimientoTrabajador') IS NULL
BEGIN
    ALTER TABLE dbo.CuotaProgramadaTrabajador
        ADD IdMovimientoTrabajador INT NULL;

    ALTER TABLE dbo.CuotaProgramadaTrabajador
        ADD CONSTRAINT FK_CuotaProgramadaTrabajador_Movimiento
            FOREIGN KEY (IdMovimientoTrabajador) REFERENCES dbo.MovimientoTrabajador(IdMovimientoTrabajador);
END
GO

IF COL_LENGTH('dbo.CuotaProgramadaTrabajador', 'FechaAplicacion') IS NULL
BEGIN
    ALTER TABLE dbo.CuotaProgramadaTrabajador
        ADD FechaAplicacion DATETIME NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UQ_CuotaProgramadaTrabajador_Movimiento'
      AND object_id = OBJECT_ID('dbo.CuotaProgramadaTrabajador')
)
BEGIN
    CREATE UNIQUE INDEX UQ_CuotaProgramadaTrabajador_Movimiento
        ON dbo.CuotaProgramadaTrabajador(IdMovimientoTrabajador)
        WHERE IdMovimientoTrabajador IS NOT NULL;
END
GO

IF OBJECT_ID('dbo.PrestamoPagoExtraordinario', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PrestamoPagoExtraordinario
    (
        IdPagoExtraordinario INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdPrestamoTrabajador INT NOT NULL,
        FechaPago DATE NOT NULL,
        MontoPago DECIMAL(18,2) NOT NULL,
        SaldoAnterior DECIMAL(18,2) NOT NULL,
        SaldoPosterior DECIMAL(18,2) NOT NULL,
        Observacion VARCHAR(300) NULL,
        UsuarioRegistro VARCHAR(80) NULL,
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_PrestamoPagoExtraordinario_FechaRegistro DEFAULT (GETDATE()),
        CONSTRAINT FK_PrestamoPagoExtraordinario_Prestamo
            FOREIGN KEY (IdPrestamoTrabajador) REFERENCES dbo.PrestamoTrabajador(IdPrestamoTrabajador)
    );
END
GO

IF OBJECT_ID('dbo.PrestamoCronogramaHistorial', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PrestamoCronogramaHistorial
    (
        IdHistorialCronograma INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdPrestamoTrabajador INT NOT NULL,
        IdCuotaProgramada INT NULL,
        Accion VARCHAR(40) NOT NULL,
        FechaProgramadaAnterior DATE NULL,
        FechaProgramadaNueva DATE NULL,
        MontoAnterior DECIMAL(18,2) NULL,
        MontoNuevo DECIMAL(18,2) NULL,
        EstadoAnterior VARCHAR(30) NULL,
        EstadoNuevo VARCHAR(30) NULL,
        Observacion VARCHAR(300) NULL,
        UsuarioRegistro VARCHAR(80) NULL,
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_PrestamoCronogramaHistorial_FechaRegistro DEFAULT (GETDATE()),
        CONSTRAINT FK_PrestamoCronogramaHistorial_Prestamo
            FOREIGN KEY (IdPrestamoTrabajador) REFERENCES dbo.PrestamoTrabajador(IdPrestamoTrabajador),
        CONSTRAINT FK_PrestamoCronogramaHistorial_Cuota
            FOREIGN KEY (IdCuotaProgramada) REFERENCES dbo.CuotaProgramadaTrabajador(IdCuotaProgramada)
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

IF COL_LENGTH('dbo.PagoTrabajador', 'NumeroOperacion') IS NULL
BEGIN
    ALTER TABLE dbo.PagoTrabajador
        ADD NumeroOperacion VARCHAR(80) NULL;
END
GO

IF COL_LENGTH('dbo.PagoTrabajador', 'Estado') IS NULL
BEGIN
    ALTER TABLE dbo.PagoTrabajador
        ADD Estado VARCHAR(30) NOT NULL CONSTRAINT DF_PagoTrabajador_Estado DEFAULT ('Confirmado');
END
GO

IF COL_LENGTH('dbo.PagoTrabajador', 'MotivoAnulacion') IS NULL
BEGIN
    ALTER TABLE dbo.PagoTrabajador
        ADD MotivoAnulacion VARCHAR(300) NULL,
            UsuarioAnulacion VARCHAR(80) NULL,
            FechaAnulacion DATETIME NULL,
            AutorizadoPor VARCHAR(80) NULL;
END
GO

IF OBJECT_ID('dbo.PagoTrabajadorMedio', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PagoTrabajadorMedio
    (
        IdPagoTrabajadorMedio INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdPagoTrabajador INT NOT NULL,
        MedioPago VARCHAR(40) NOT NULL,
        MontoPagado DECIMAL(18,2) NOT NULL,
        NumeroOperacion VARCHAR(80) NULL,
        CONSTRAINT FK_PagoTrabajadorMedio_Pago
            FOREIGN KEY (IdPagoTrabajador) REFERENCES dbo.PagoTrabajador(IdPagoTrabajador)
    );
END
GO

IF OBJECT_ID('dbo.CalculoPeriodoTrabajador', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CalculoPeriodoTrabajador
    (
        IdCalculoPeriodoTrabajador INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdPeriodoPago INT NOT NULL,
        IdTrabajadorOperativo INT NOT NULL,
        Produccion DECIMAL(18,2) NOT NULL CONSTRAINT DF_CalculoPeriodoTrabajador_Produccion DEFAULT (0),
        Bonificaciones DECIMAL(18,2) NOT NULL CONSTRAINT DF_CalculoPeriodoTrabajador_Bonificaciones DEFAULT (0),
        IngresosAdicionales DECIMAL(18,2) NOT NULL CONSTRAINT DF_CalculoPeriodoTrabajador_IngresosAdicionales DEFAULT (0),
        AjustesPositivos DECIMAL(18,2) NOT NULL CONSTRAINT DF_CalculoPeriodoTrabajador_AjustesPositivos DEFAULT (0),
        DescuentosManuales DECIMAL(18,2) NOT NULL CONSTRAINT DF_CalculoPeriodoTrabajador_DescuentosManuales DEFAULT (0),
        CuotasPrestamos DECIMAL(18,2) NOT NULL CONSTRAINT DF_CalculoPeriodoTrabajador_CuotasPrestamos DEFAULT (0),
        AjustesNegativos DECIMAL(18,2) NOT NULL CONSTRAINT DF_CalculoPeriodoTrabajador_AjustesNegativos DEFAULT (0),
        SaldoAnterior DECIMAL(18,2) NOT NULL CONSTRAINT DF_CalculoPeriodoTrabajador_SaldoAnterior DEFAULT (0),
        TotalIngresos DECIMAL(18,2) NOT NULL CONSTRAINT DF_CalculoPeriodoTrabajador_TotalIngresos DEFAULT (0),
        TotalDescuentos DECIMAL(18,2) NOT NULL CONSTRAINT DF_CalculoPeriodoTrabajador_TotalDescuentos DEFAULT (0),
        NetoPeriodo DECIMAL(18,2) NOT NULL CONSTRAINT DF_CalculoPeriodoTrabajador_NetoPeriodo DEFAULT (0),
        TotalPagado DECIMAL(18,2) NOT NULL CONSTRAINT DF_CalculoPeriodoTrabajador_TotalPagado DEFAULT (0),
        TotalPorPagar DECIMAL(18,2) NOT NULL CONSTRAINT DF_CalculoPeriodoTrabajador_TotalPorPagar DEFAULT (0),
        SaldoPendiente DECIMAL(18,2) NOT NULL CONSTRAINT DF_CalculoPeriodoTrabajador_SaldoPendiente DEFAULT (0),
        EstadoCalculo VARCHAR(30) NOT NULL CONSTRAINT DF_CalculoPeriodoTrabajador_Estado DEFAULT ('Preliminar'),
        FechaCalculo DATETIME NOT NULL CONSTRAINT DF_CalculoPeriodoTrabajador_FechaCalculo DEFAULT (GETDATE()),
        UsuarioCalculo VARCHAR(80) NULL,
        CONSTRAINT FK_CalculoPeriodoTrabajador_Periodo
            FOREIGN KEY (IdPeriodoPago) REFERENCES dbo.PeriodoPago(IdPeriodoPago),
        CONSTRAINT FK_CalculoPeriodoTrabajador_Trabajador
            FOREIGN KEY (IdTrabajadorOperativo) REFERENCES dbo.TrabajadorOperativo(IdTrabajadorOperativo)
    );

    CREATE UNIQUE INDEX UQ_CalculoPeriodoTrabajador
        ON dbo.CalculoPeriodoTrabajador(IdPeriodoPago, IdTrabajadorOperativo);
END
GO

IF OBJECT_ID('dbo.CalculoPeriodoAlerta', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CalculoPeriodoAlerta
    (
        IdCalculoPeriodoAlerta INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdPeriodoPago INT NOT NULL,
        IdTrabajadorOperativo INT NULL,
        IdMovimientoTrabajador INT NULL,
        IdCuotaProgramada INT NULL,
        TipoAlerta VARCHAR(60) NOT NULL,
        Severidad VARCHAR(20) NOT NULL CONSTRAINT DF_CalculoPeriodoAlerta_Severidad DEFAULT ('Advertencia'),
        Mensaje VARCHAR(500) NOT NULL,
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_CalculoPeriodoAlerta_Fecha DEFAULT (GETDATE()),
        CONSTRAINT FK_CalculoPeriodoAlerta_Periodo
            FOREIGN KEY (IdPeriodoPago) REFERENCES dbo.PeriodoPago(IdPeriodoPago),
        CONSTRAINT FK_CalculoPeriodoAlerta_Trabajador
            FOREIGN KEY (IdTrabajadorOperativo) REFERENCES dbo.TrabajadorOperativo(IdTrabajadorOperativo),
        CONSTRAINT FK_CalculoPeriodoAlerta_Movimiento
            FOREIGN KEY (IdMovimientoTrabajador) REFERENCES dbo.MovimientoTrabajador(IdMovimientoTrabajador),
        CONSTRAINT FK_CalculoPeriodoAlerta_Cuota
            FOREIGN KEY (IdCuotaProgramada) REFERENCES dbo.CuotaProgramadaTrabajador(IdCuotaProgramada)
    );

    CREATE INDEX IX_CalculoPeriodoAlerta_Periodo
        ON dbo.CalculoPeriodoAlerta(IdPeriodoPago, IdTrabajadorOperativo);
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

    IF EXISTS (SELECT 1 FROM dbo.OperacionTextil WHERE IdAreaOperativa = @IdAreaOperativa)
       OR EXISTS (SELECT 1 FROM dbo.MovimientoTrabajador WHERE IdAreaOperativa = @IdAreaOperativa AND Eliminado = 0)
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'No se puede desactivar el area porque ya tiene operaciones o movimientos.';
        RETURN;
    END;

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

    IF EXISTS (SELECT 1 FROM dbo.MovimientoTrabajador WHERE IdConceptoMovimiento = @IdConceptoMovimiento AND Eliminado = 0)
       OR EXISTS (SELECT 1 FROM dbo.CuotaProgramadaTrabajador WHERE IdConceptoMovimiento = @IdConceptoMovimiento)
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'No se puede desactivar el concepto porque ya tiene movimientos o cuotas.';
        RETURN;
    END;

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
        O.FechaInicioVigencia,
        O.FechaFinVigencia,
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
    @FechaInicioVigencia DATE = NULL,
    @FechaFinVigencia DATE = NULL,
    @Estado BIT,
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    IF @FechaInicioVigencia IS NOT NULL
       AND @FechaFinVigencia IS NOT NULL
       AND @FechaInicioVigencia > @FechaFinVigencia
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'La vigencia de tarifa no es valida.';
        RETURN;
    END;

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
            FechaInicioVigencia,
            FechaFinVigencia,
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
            @FechaInicioVigencia,
            @FechaFinVigencia,
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
            FechaInicioVigencia = @FechaInicioVigencia,
            FechaFinVigencia = @FechaFinVigencia,
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

    IF EXISTS (SELECT 1 FROM dbo.MovimientoTrabajador WHERE IdOperacionTextil = @IdOperacionTextil AND Eliminado = 0)
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'No se puede desactivar la operacion porque ya tiene movimientos.';
        RETURN;
    END;

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

    IF EXISTS (SELECT 1 FROM dbo.MovimientoTrabajador WHERE IdTrabajadorOperativo = @IdTrabajadorOperativo AND Eliminado = 0)
       OR EXISTS (SELECT 1 FROM dbo.PrestamoTrabajador WHERE IdTrabajadorOperativo = @IdTrabajadorOperativo)
       OR EXISTS (SELECT 1 FROM dbo.PagoTrabajador WHERE IdTrabajadorOperativo = @IdTrabajadorOperativo)
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'No se puede desactivar el trabajador porque ya tiene movimientos, prestamos o pagos.';
        RETURN;
    END;

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
        ISNULL(P.NumeroSemana, DATEPART(ISO_WEEK, P.FechaInicio)) AS NumeroSemana,
        ISNULL(P.Anio, DATEPART(YEAR, DATEADD(DAY, 4 - DATEPART(WEEKDAY, P.FechaInicio), P.FechaInicio))) AS Anio,
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
    @NumeroSemana INT,
    @Anio INT,
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

    IF @FechaInicio > @FechaFin
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'La fecha de inicio no puede ser mayor que la fecha fin.';
        RETURN;
    END;

    IF @NumeroSemana IS NULL OR @NumeroSemana <= 0
        SET @NumeroSemana = DATEPART(ISO_WEEK, @FechaInicio);

    IF @Anio IS NULL OR @Anio <= 0
        SET @Anio = DATEPART(YEAR, DATEADD(DAY, 4 - DATEPART(WEEKDAY, @FechaInicio), @FechaInicio));

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

    IF EXISTS
    (
        SELECT 1
        FROM dbo.PeriodoPago
        WHERE IdPeriodoPago <> @IdPeriodoPago
        AND Estado <> 'Anulado'
        AND @FechaInicio <= FechaFin
        AND @FechaFin >= FechaInicio
    )
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'Ya existe un periodo con fechas superpuestas.';
        RETURN;
    END;

    IF @IdPeriodoPago = 0
    BEGIN
        INSERT INTO dbo.PeriodoPago(CodigoPeriodo, NumeroSemana, Anio, FechaInicio, FechaFin, Estado, Observacion)
        VALUES(@CodigoPeriodo, @NumeroSemana, @Anio, @FechaInicio, @FechaFin, @Estado, @Observacion);

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

        IF EXISTS
        (
            SELECT 1
            FROM dbo.PagoTrabajador
            WHERE IdPeriodoPago = @IdPeriodoPago
        )
        AND EXISTS
        (
            SELECT 1
            FROM dbo.PeriodoPago
            WHERE IdPeriodoPago = @IdPeriodoPago
            AND (FechaInicio <> @FechaInicio OR FechaFin <> @FechaFin)
        )
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'No se pueden modificar fechas de un periodo con pagos.';
            RETURN;
        END;

        UPDATE dbo.PeriodoPago
        SET CodigoPeriodo = @CodigoPeriodo,
            NumeroSemana = @NumeroSemana,
            Anio = @Anio,
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

    DECLARE @EstadoActual VARCHAR(40);

    SELECT @EstadoActual = Estado
    FROM dbo.PeriodoPago
    WHERE IdPeriodoPago = @IdPeriodoPago;

    IF @EstadoActual IS NULL
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'El periodo seleccionado no existe.';
        RETURN;
    END;

    IF @EstadoActual = 'Cerrado' AND @Estado <> 'Abierto'
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'Un periodo cerrado solo puede reabrirse con autorización.';
        RETURN;
    END;

    IF @Estado = 'Cerrado'
       AND EXISTS
       (
            SELECT 1
            FROM dbo.MovimientoTrabajador
            WHERE IdPeriodoPago = @IdPeriodoPago
            AND Eliminado = 0
            AND Estado IN ('Borrador', 'Pendiente')
       )
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'No se puede cerrar el periodo porque tiene movimientos pendientes de calculo.';
        RETURN;
    END;

    IF @Estado = 'Cerrado'
       AND EXISTS
       (
            SELECT 1
            FROM dbo.PagoTrabajador
            WHERE IdPeriodoPago = @IdPeriodoPago
            AND Estado NOT IN ('Confirmado', 'Anulado')
       )
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'No se puede cerrar el periodo porque tiene pagos sin confirmar.';
        RETURN;
    END;

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

    IF EXISTS (SELECT 1 FROM dbo.PeriodoPago WHERE IdPeriodoPago = @IdPeriodoPago AND Estado IN ('Borrador', 'En calculo', 'Calculado', 'En pago', 'Cerrado', 'Anulado'))
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'Solo se pueden registrar movimientos en periodos abiertos.';
        RETURN;
    END;

    IF EXISTS (SELECT 1 FROM dbo.PeriodoPago WHERE IdPeriodoPago = @IdPeriodoPago AND Estado IN ('Cerrado', 'Anulado'))
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'No se puede modificar un periodo cerrado o anulado.';
        RETURN;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.PeriodoPago
        WHERE IdPeriodoPago = @IdPeriodoPago
        AND (@Fecha < FechaInicio OR @Fecha > FechaFin)
    )
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'La fecha del trabajo debe estar dentro del periodo seleccionado.';
        RETURN;
    END;

    IF (@EsDescuento = 1 OR @TipoMovimiento = 'Descuento')
       AND @CategoriaMovimiento IN ('Produccion', 'Produccion por destajo')
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'Los descuentos no pueden registrarse como produccion.';
        RETURN;
    END;

    IF @CategoriaMovimiento IN ('Produccion', 'Produccion por destajo')
       AND ISNULL(@IdOperacionTextil, 0) = 0
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'Debe seleccionar una operacion para registrar produccion.';
        RETURN;
    END;

    IF ISNULL(@IdOperacionTextil, 0) > 0
    BEGIN
        IF NOT EXISTS
        (
            SELECT 1
            FROM dbo.OperacionTextil
            WHERE IdOperacionTextil = @IdOperacionTextil
            AND Estado = 1
            AND (@Fecha >= ISNULL(FechaInicioVigencia, @Fecha))
            AND (@Fecha <= ISNULL(FechaFinVigencia, @Fecha))
        )
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'La operacion seleccionada no tiene tarifa vigente para la fecha del trabajo.';
            RETURN;
        END;

        SELECT
            @Tarifa = TarifaBase,
            @UnidadMedida = UnidadMedida,
            @IdAreaOperativa = ISNULL(@IdAreaOperativa, IdAreaOperativa)
        FROM dbo.OperacionTextil
        WHERE IdOperacionTextil = @IdOperacionTextil;
    END;

    SET @Importe = CASE
        WHEN @Cantidad > 0 AND @Tarifa > 0 THEN ROUND(@Cantidad * @Tarifa, 2)
        ELSE ROUND(@Importe, 2)
    END;

    IF @Importe <= 0
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'El importe debe ser mayor a cero.';
        RETURN;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.MovimientoTrabajador
        WHERE IdMovimientoTrabajador <> @IdMovimientoTrabajador
        AND IdPeriodoPago = @IdPeriodoPago
        AND IdTrabajadorOperativo = @IdTrabajadorOperativo
        AND Fecha = @Fecha
        AND IdConceptoMovimiento = @IdConceptoMovimiento
        AND ISNULL(IdOperacionTextil, 0) = ISNULL(@IdOperacionTextil, 0)
        AND Cantidad = @Cantidad
        AND Tarifa = @Tarifa
        AND Eliminado = 0
        AND Estado <> 'Anulado'
    )
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'Ya existe un movimiento similar para el trabajador en este periodo.';
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

    IF EXISTS (SELECT 1 FROM dbo.CalculoPeriodoTrabajador WHERE IdPeriodoPago = @IdPeriodoPago)
    BEGIN
        SELECT
            C.IdPeriodoPago,
            C.IdTrabajadorOperativo,
            CONCAT(E.Nombre, ' ', E.Apellido) AS NombreTrabajador,
            ISNULL(E.NumeroDocumento, '') AS Documento,
            TR.TipoTrabajador,
            TR.MedioPagoPreferido,
            C.SaldoAnterior,
            C.TotalIngresos,
            C.TotalDescuentos,
            C.NetoPeriodo AS NetoCalculado,
            C.TotalPagado,
            C.TotalPorPagar,
            C.SaldoPendiente,
            P.Estado AS EstadoPeriodo,
            C.EstadoCalculo,
            C.FechaCalculo,
            ISNULL(C.UsuarioCalculo, '') AS UsuarioCalculo
        FROM dbo.CalculoPeriodoTrabajador C
        INNER JOIN dbo.PeriodoPago P ON P.IdPeriodoPago = C.IdPeriodoPago
        INNER JOIN dbo.TrabajadorOperativo TR ON TR.IdTrabajadorOperativo = C.IdTrabajadorOperativo
        INNER JOIN dbo.Empleados E ON E.IdEmpleado = TR.IdEmpleado
        WHERE C.IdPeriodoPago = @IdPeriodoPago
        ORDER BY E.Apellido, E.Nombre;

        RETURN;
    END;

    WITH Totales AS
    (
        SELECT
            M.IdPeriodoPago,
            M.IdTrabajadorOperativo,
            SUM(CASE WHEN M.CategoriaMovimiento = 'Saldo' AND M.EsDescuento = 0 THEN M.Importe ELSE 0 END) AS SaldoAnterior,
            SUM(CASE WHEN M.TipoMovimiento <> 'Pago' AND M.EsDescuento = 0 AND M.CategoriaMovimiento <> 'Saldo' THEN M.Importe ELSE 0 END) AS TotalIngresos,
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
        ISNULL(E.NumeroDocumento, '') AS Documento,
        TR.TipoTrabajador,
        TR.MedioPagoPreferido,
        T.SaldoAnterior,
        T.TotalIngresos,
        T.TotalDescuentos,
        T.TotalIngresos - T.TotalDescuentos AS NetoCalculado,
        T.TotalPagado,
        T.TotalIngresos - T.TotalDescuentos + T.SaldoAnterior AS TotalPorPagar,
        T.TotalIngresos - T.TotalDescuentos + T.SaldoAnterior - T.TotalPagado AS SaldoPendiente,
        P.Estado AS EstadoPeriodo,
        'Sin calcular' AS EstadoCalculo,
        NULL AS FechaCalculo,
        '' AS UsuarioCalculo
    FROM Totales T
    INNER JOIN dbo.PeriodoPago P ON P.IdPeriodoPago = T.IdPeriodoPago
    INNER JOIN dbo.TrabajadorOperativo TR ON TR.IdTrabajadorOperativo = T.IdTrabajadorOperativo
    INNER JOIN dbo.Empleados E ON E.IdEmpleado = TR.IdEmpleado
    ORDER BY E.Apellido, E.Nombre;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_CALCULO_PERIODO_ALERTAS_LISTAR
(
    @IdPeriodoPago INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        A.IdCalculoPeriodoAlerta,
        A.IdPeriodoPago,
        A.IdTrabajadorOperativo,
        ISNULL(CONCAT(E.Nombre, ' ', E.Apellido), '') AS NombreTrabajador,
        A.IdMovimientoTrabajador,
        A.IdCuotaProgramada,
        A.TipoAlerta,
        A.Severidad,
        A.Mensaje,
        A.FechaRegistro
    FROM dbo.CalculoPeriodoAlerta A
    LEFT JOIN dbo.TrabajadorOperativo T ON T.IdTrabajadorOperativo = A.IdTrabajadorOperativo
    LEFT JOIN dbo.Empleados E ON E.IdEmpleado = T.IdEmpleado
    WHERE A.IdPeriodoPago = @IdPeriodoPago
    ORDER BY CASE A.Severidad WHEN 'Error' THEN 1 WHEN 'Advertencia' THEN 2 ELSE 3 END,
             A.IdCalculoPeriodoAlerta;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_CALCULO_PERIODO_DETALLE_LISTAR
(
    @IdPeriodoPago INT,
    @IdTrabajadorOperativo INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        M.IdMovimientoTrabajador,
        M.Fecha,
        M.TipoMovimiento,
        M.CategoriaMovimiento,
        CM.NombreConcepto,
        ISNULL(O.NombreOperacion, '') AS NombreOperacion,
        M.Descripcion,
        M.Cantidad,
        M.UnidadMedida,
        M.Tarifa,
        M.Importe,
        M.EsDescuento,
        M.EsAutomatico,
        M.OrigenMovimiento,
        M.Estado,
        ISNULL(M.Observacion, '') AS Observacion
    FROM dbo.MovimientoTrabajador M
    INNER JOIN dbo.ConceptoMovimiento CM ON CM.IdConceptoMovimiento = M.IdConceptoMovimiento
    LEFT JOIN dbo.OperacionTextil O ON O.IdOperacionTextil = M.IdOperacionTextil
    WHERE M.IdPeriodoPago = @IdPeriodoPago
      AND M.IdTrabajadorOperativo = @IdTrabajadorOperativo
      AND M.Eliminado = 0
    ORDER BY M.Fecha, M.IdMovimientoTrabajador;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_CALCULO_PERIODO_CALCULAR
(
    @IdPeriodoPago INT,
    @IdTrabajadorOperativo INT = NULL,
    @Confirmar BIT = 0,
    @Usuario VARCHAR(80),
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @EstadoPeriodo VARCHAR(40);

        SELECT @EstadoPeriodo = Estado
        FROM dbo.PeriodoPago
        WHERE IdPeriodoPago = @IdPeriodoPago;

        IF @EstadoPeriodo IS NULL
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'El periodo seleccionado no existe.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        IF @EstadoPeriodo IN ('Cerrado', 'Anulado', 'Pagado / Cerrado')
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'No se puede calcular un periodo cerrado o anulado.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        IF @EstadoPeriodo = 'Calculado' AND @Confirmar = 0
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'El periodo ya tiene un calculo confirmado. Reabra el periodo antes de recalcular.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        IF @IdTrabajadorOperativo IS NOT NULL
        BEGIN
            DELETE FROM dbo.CalculoPeriodoAlerta
            WHERE IdPeriodoPago = @IdPeriodoPago
              AND IdTrabajadorOperativo = @IdTrabajadorOperativo;

            DELETE FROM dbo.CalculoPeriodoTrabajador
            WHERE IdPeriodoPago = @IdPeriodoPago
              AND IdTrabajadorOperativo = @IdTrabajadorOperativo;
        END
        ELSE
        BEGIN
            DELETE FROM dbo.CalculoPeriodoAlerta
            WHERE IdPeriodoPago = @IdPeriodoPago;

            DELETE FROM dbo.CalculoPeriodoTrabajador
            WHERE IdPeriodoPago = @IdPeriodoPago;
        END;

        WITH TrabajadoresPeriodo AS
        (
            SELECT DISTINCT M.IdTrabajadorOperativo
            FROM dbo.MovimientoTrabajador M
            WHERE M.IdPeriodoPago = @IdPeriodoPago
              AND M.Eliminado = 0
              AND (@IdTrabajadorOperativo IS NULL OR M.IdTrabajadorOperativo = @IdTrabajadorOperativo)
        ),
        Totales AS
        (
            SELECT
                TP.IdTrabajadorOperativo,
                SUM(CASE WHEN M.EsDescuento = 0 AND M.TipoMovimiento <> 'Pago' AND M.CategoriaMovimiento IN ('Produccion', 'Produccion por destajo') THEN M.Importe ELSE 0 END) AS Produccion,
                SUM(CASE WHEN M.EsDescuento = 0 AND M.TipoMovimiento <> 'Pago' AND M.CategoriaMovimiento IN ('Bono', 'Bonificacion') THEN M.Importe ELSE 0 END) AS Bonificaciones,
                SUM(CASE WHEN M.EsDescuento = 0 AND M.TipoMovimiento <> 'Pago' AND M.CategoriaMovimiento NOT IN ('Produccion', 'Produccion por destajo', 'Bono', 'Bonificacion', 'Ajuste', 'Saldo') THEN M.Importe ELSE 0 END) AS IngresosAdicionales,
                SUM(CASE WHEN M.EsDescuento = 0 AND M.TipoMovimiento <> 'Pago' AND M.CategoriaMovimiento = 'Ajuste' THEN M.Importe ELSE 0 END) AS AjustesPositivos,
                SUM(CASE WHEN (M.EsDescuento = 1 OR M.TipoMovimiento = 'Descuento') AND M.OrigenMovimiento <> 'Cuota' AND M.CategoriaMovimiento <> 'Ajuste' THEN M.Importe ELSE 0 END) AS DescuentosManuales,
                SUM(CASE WHEN (M.EsDescuento = 1 OR M.TipoMovimiento = 'Descuento') AND M.OrigenMovimiento = 'Cuota' THEN M.Importe ELSE 0 END) AS CuotasPrestamos,
                SUM(CASE WHEN (M.EsDescuento = 1 OR M.TipoMovimiento = 'Descuento') AND M.CategoriaMovimiento = 'Ajuste' THEN M.Importe ELSE 0 END) AS AjustesNegativos,
                SUM(CASE WHEN M.EsDescuento = 0 AND M.TipoMovimiento <> 'Pago' AND M.CategoriaMovimiento = 'Saldo' THEN M.Importe ELSE 0 END) AS SaldoAnterior,
                SUM(CASE WHEN M.TipoMovimiento = 'Pago' THEN M.Importe ELSE 0 END) AS TotalPagado
            FROM TrabajadoresPeriodo TP
            INNER JOIN dbo.MovimientoTrabajador M
                ON M.IdTrabajadorOperativo = TP.IdTrabajadorOperativo
               AND M.IdPeriodoPago = @IdPeriodoPago
               AND M.Eliminado = 0
            GROUP BY TP.IdTrabajadorOperativo
        )
        INSERT INTO dbo.CalculoPeriodoTrabajador
        (
            IdPeriodoPago, IdTrabajadorOperativo, Produccion, Bonificaciones, IngresosAdicionales, AjustesPositivos,
            DescuentosManuales, CuotasPrestamos, AjustesNegativos, SaldoAnterior, TotalIngresos, TotalDescuentos,
            NetoPeriodo, TotalPagado, TotalPorPagar, SaldoPendiente, EstadoCalculo, FechaCalculo, UsuarioCalculo
        )
        SELECT
            @IdPeriodoPago,
            IdTrabajadorOperativo,
            ISNULL(Produccion, 0),
            ISNULL(Bonificaciones, 0),
            ISNULL(IngresosAdicionales, 0),
            ISNULL(AjustesPositivos, 0),
            ISNULL(DescuentosManuales, 0),
            ISNULL(CuotasPrestamos, 0),
            ISNULL(AjustesNegativos, 0),
            ISNULL(SaldoAnterior, 0),
            ISNULL(Produccion, 0) + ISNULL(Bonificaciones, 0) + ISNULL(IngresosAdicionales, 0) + ISNULL(AjustesPositivos, 0),
            ISNULL(DescuentosManuales, 0) + ISNULL(CuotasPrestamos, 0) + ISNULL(AjustesNegativos, 0),
            ISNULL(Produccion, 0) + ISNULL(Bonificaciones, 0) + ISNULL(IngresosAdicionales, 0) + ISNULL(AjustesPositivos, 0) - ISNULL(DescuentosManuales, 0) - ISNULL(CuotasPrestamos, 0) - ISNULL(AjustesNegativos, 0),
            ISNULL(TotalPagado, 0),
            ISNULL(Produccion, 0) + ISNULL(Bonificaciones, 0) + ISNULL(IngresosAdicionales, 0) + ISNULL(AjustesPositivos, 0) - ISNULL(DescuentosManuales, 0) - ISNULL(CuotasPrestamos, 0) - ISNULL(AjustesNegativos, 0) + ISNULL(SaldoAnterior, 0),
            ISNULL(Produccion, 0) + ISNULL(Bonificaciones, 0) + ISNULL(IngresosAdicionales, 0) + ISNULL(AjustesPositivos, 0) - ISNULL(DescuentosManuales, 0) - ISNULL(CuotasPrestamos, 0) - ISNULL(AjustesNegativos, 0) + ISNULL(SaldoAnterior, 0) - ISNULL(TotalPagado, 0),
            CASE WHEN @Confirmar = 1 THEN 'Confirmado' ELSE 'Preliminar' END,
            GETDATE(),
            @Usuario
        FROM Totales;

        INSERT INTO dbo.CalculoPeriodoAlerta(IdPeriodoPago, IdTrabajadorOperativo, TipoAlerta, Severidad, Mensaje)
        SELECT @IdPeriodoPago, C.IdTrabajadorOperativo, 'NETO_NEGATIVO', 'Advertencia', 'Trabajador con neto negativo.'
        FROM dbo.CalculoPeriodoTrabajador C
        WHERE C.IdPeriodoPago = @IdPeriodoPago
          AND C.NetoPeriodo < 0
          AND (@IdTrabajadorOperativo IS NULL OR C.IdTrabajadorOperativo = @IdTrabajadorOperativo);

        INSERT INTO dbo.CalculoPeriodoAlerta(IdPeriodoPago, IdTrabajadorOperativo, TipoAlerta, Severidad, Mensaje)
        SELECT @IdPeriodoPago, C.IdTrabajadorOperativo, 'SIN_MEDIO_PAGO', 'Advertencia', 'Trabajador sin medio de pago.'
        FROM dbo.CalculoPeriodoTrabajador C
        INNER JOIN dbo.TrabajadorOperativo T ON T.IdTrabajadorOperativo = C.IdTrabajadorOperativo
        WHERE C.IdPeriodoPago = @IdPeriodoPago
          AND ISNULL(NULLIF(LTRIM(RTRIM(T.MedioPagoPreferido)), ''), '') = ''
          AND (@IdTrabajadorOperativo IS NULL OR C.IdTrabajadorOperativo = @IdTrabajadorOperativo);

        INSERT INTO dbo.CalculoPeriodoAlerta(IdPeriodoPago, IdTrabajadorOperativo, IdMovimientoTrabajador, TipoAlerta, Severidad, Mensaje)
        SELECT @IdPeriodoPago, M.IdTrabajadorOperativo, M.IdMovimientoTrabajador, 'TARIFA_NO_CONFIGURADA', 'Error', 'Movimiento de produccion sin tarifa configurada.'
        FROM dbo.MovimientoTrabajador M
        WHERE M.IdPeriodoPago = @IdPeriodoPago
          AND M.Eliminado = 0
          AND M.CategoriaMovimiento IN ('Produccion', 'Produccion por destajo')
          AND ISNULL(M.Tarifa, 0) <= 0
          AND (@IdTrabajadorOperativo IS NULL OR M.IdTrabajadorOperativo = @IdTrabajadorOperativo);

        INSERT INTO dbo.CalculoPeriodoAlerta(IdPeriodoPago, IdTrabajadorOperativo, IdMovimientoTrabajador, TipoAlerta, Severidad, Mensaje)
        SELECT @IdPeriodoPago, M.IdTrabajadorOperativo, M.IdMovimientoTrabajador, 'IMPORTE_CERO', 'Error', 'Movimiento con importe cero.'
        FROM dbo.MovimientoTrabajador M
        WHERE M.IdPeriodoPago = @IdPeriodoPago
          AND M.Eliminado = 0
          AND ISNULL(M.Importe, 0) <= 0
          AND (@IdTrabajadorOperativo IS NULL OR M.IdTrabajadorOperativo = @IdTrabajadorOperativo);

        INSERT INTO dbo.CalculoPeriodoAlerta(IdPeriodoPago, IdTrabajadorOperativo, IdCuotaProgramada, TipoAlerta, Severidad, Mensaje)
        SELECT @IdPeriodoPago, C.IdTrabajadorOperativo, Q.IdCuotaProgramada, 'CUOTA_SUPERIOR_INGRESO', 'Advertencia', 'Cuota de prestamo superior al ingreso del periodo.'
        FROM dbo.CuotaProgramadaTrabajador Q
        INNER JOIN dbo.CalculoPeriodoTrabajador C ON C.IdPeriodoPago = @IdPeriodoPago AND C.IdTrabajadorOperativo = Q.IdTrabajadorOperativo
        WHERE Q.IdPeriodoAplicado = @IdPeriodoPago
          AND Q.Estado = 'Aplicada'
          AND Q.MontoCuota > C.TotalIngresos
          AND (@IdTrabajadorOperativo IS NULL OR Q.IdTrabajadorOperativo = @IdTrabajadorOperativo);

        WITH Duplicados AS
        (
            SELECT IdTrabajadorOperativo
            FROM dbo.MovimientoTrabajador
            WHERE IdPeriodoPago = @IdPeriodoPago
              AND Eliminado = 0
              AND (@IdTrabajadorOperativo IS NULL OR IdTrabajadorOperativo = @IdTrabajadorOperativo)
            GROUP BY IdTrabajadorOperativo, Fecha, IdConceptoMovimiento, ISNULL(IdOperacionTextil, 0), Cantidad, Tarifa
            HAVING COUNT(*) > 1
        )
        INSERT INTO dbo.CalculoPeriodoAlerta(IdPeriodoPago, IdTrabajadorOperativo, TipoAlerta, Severidad, Mensaje)
        SELECT @IdPeriodoPago, IdTrabajadorOperativo, 'MOVIMIENTO_DUPLICADO', 'Advertencia', 'Posible movimiento duplicado en el periodo.'
        FROM Duplicados;

        INSERT INTO dbo.CalculoPeriodoAlerta(IdPeriodoPago, IdTrabajadorOperativo, TipoAlerta, Severidad, Mensaje)
        SELECT DISTINCT @IdPeriodoPago, M.IdTrabajadorOperativo, 'TRABAJADOR_INACTIVO', 'Advertencia', 'Trabajador inactivo con movimientos.'
        FROM dbo.MovimientoTrabajador M
        INNER JOIN dbo.TrabajadorOperativo T ON T.IdTrabajadorOperativo = M.IdTrabajadorOperativo
        WHERE M.IdPeriodoPago = @IdPeriodoPago
          AND M.Eliminado = 0
          AND T.Estado = 0
          AND (@IdTrabajadorOperativo IS NULL OR M.IdTrabajadorOperativo = @IdTrabajadorOperativo);

        WITH Movimientos AS
        (
            SELECT
                M.IdTrabajadorOperativo,
                SUM(CASE WHEN M.TipoMovimiento <> 'Pago' AND M.EsDescuento = 0 AND M.CategoriaMovimiento <> 'Saldo' THEN M.Importe ELSE 0 END) AS TotalIngresosMov,
                SUM(CASE WHEN M.EsDescuento = 1 OR M.TipoMovimiento = 'Descuento' THEN M.Importe ELSE 0 END) AS TotalDescuentosMov
            FROM dbo.MovimientoTrabajador M
            WHERE M.IdPeriodoPago = @IdPeriodoPago
              AND M.Eliminado = 0
              AND (@IdTrabajadorOperativo IS NULL OR M.IdTrabajadorOperativo = @IdTrabajadorOperativo)
            GROUP BY M.IdTrabajadorOperativo
        )
        INSERT INTO dbo.CalculoPeriodoAlerta(IdPeriodoPago, IdTrabajadorOperativo, TipoAlerta, Severidad, Mensaje)
        SELECT @IdPeriodoPago, C.IdTrabajadorOperativo, 'DIFERENCIA_CALCULO_MOVIMIENTOS', 'Error', 'Diferencias entre calculo persistido y movimientos.'
        FROM dbo.CalculoPeriodoTrabajador C
        INNER JOIN Movimientos M ON M.IdTrabajadorOperativo = C.IdTrabajadorOperativo
        WHERE C.IdPeriodoPago = @IdPeriodoPago
          AND (ABS(C.TotalIngresos - M.TotalIngresosMov) > 0.01 OR ABS(C.TotalDescuentos - M.TotalDescuentosMov) > 0.01);

        IF @Confirmar = 1
        BEGIN
            UPDATE dbo.PeriodoPago
            SET Estado = 'Calculado'
            WHERE IdPeriodoPago = @IdPeriodoPago;
        END;

        INSERT INTO dbo.Auditoria(Usuario, Accion, Modulo, Descripcion, Equipo)
        VALUES(@Usuario, CASE WHEN @Confirmar = 1 THEN 'CONFIRMAR CALCULO' ELSE 'CALCULAR PERIODO' END, 'DESTAJO Y PAGOS', CONCAT('Periodo ', @IdPeriodoPago), HOST_NAME());

        COMMIT TRANSACTION;

        SET @Resultado = 1;
        SET @Mensaje = CASE WHEN @Confirmar = 1 THEN 'Calculo confirmado correctamente.' ELSE 'Calculo generado correctamente.' END;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH;
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
        P.FechaInicioDescuento,
        P.IdConceptoMovimiento,
        ISNULL(CM.NombreConcepto, '') AS NombreConcepto,
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
    LEFT JOIN dbo.ConceptoMovimiento CM ON CM.IdConceptoMovimiento = P.IdConceptoMovimiento
    ORDER BY P.FechaPrestamo DESC, P.IdPrestamoTrabajador DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_PRESTAMO_REGISTRAR
(
    @IdTrabajadorOperativo INT,
    @FechaPrestamo DATE,
    @FechaInicioDescuento DATE,
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

        IF @MontoTotal <= 0 OR @NumeroCuotas <= 0 OR @MontoCuota <= 0
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'Los importes y cuotas del prestamo deben ser mayores a cero.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        IF NOT EXISTS (
            SELECT 1
            FROM dbo.ConceptoMovimiento
            WHERE IdConceptoMovimiento = @IdConceptoMovimiento
              AND EsDescuento = 1
              AND Estado = 1
        )
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'El concepto seleccionado no es un descuento activo.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        INSERT INTO dbo.PrestamoTrabajador
        (
            IdTrabajadorOperativo,
            FechaPrestamo,
            FechaInicioDescuento,
            IdConceptoMovimiento,
            MontoTotal,
            NumeroCuotas,
            MontoCuota,
            SaldoPendiente,
            Estado,
            Observacion
        )
        VALUES
        (
            @IdTrabajadorOperativo,
            @FechaPrestamo,
            @FechaInicioDescuento,
            @IdConceptoMovimiento,
            @MontoTotal,
            @NumeroCuotas,
            @MontoCuota,
            @MontoTotal,
            'Activo',
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
                DATEADD(WEEK, @Numero - 1, @FechaInicioDescuento),
                @Observacion
            );

            INSERT INTO dbo.PrestamoCronogramaHistorial
            (
                IdPrestamoTrabajador,
                IdCuotaProgramada,
                Accion,
                FechaProgramadaNueva,
                MontoNuevo,
                EstadoNuevo,
                Observacion,
                UsuarioRegistro
            )
            VALUES
            (
                @IdPrestamoTrabajador,
                SCOPE_IDENTITY(),
                'GENERAR',
                DATEADD(WEEK, @Numero - 1, @FechaInicioDescuento),
                @MontoActual,
                'Pendiente',
                @Observacion,
                @Usuario
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
        C.IdMovimientoTrabajador,
        C.FechaAplicacion,
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
            @TotalCuotas INT,
            @EstadoPrestamo VARCHAR(30),
            @SaldoPendiente DECIMAL(18,2),
            @IdMovimientoTrabajador INT;

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

        SELECT
            @EstadoPrestamo = Estado,
            @SaldoPendiente = SaldoPendiente
        FROM dbo.PrestamoTrabajador
        WHERE IdPrestamoTrabajador = @ReferenciaId;

        IF @EstadoPrestamo IN ('Cancelado', 'Pagado', 'Anulado')
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'No se puede descontar una cuota de un prestamo cancelado, pagado o anulado.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        IF @MontoCuota > @SaldoPendiente
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'La cuota no puede ser mayor al saldo pendiente.';
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

        SET @IdMovimientoTrabajador = SCOPE_IDENTITY();

        UPDATE dbo.CuotaProgramadaTrabajador
        SET Estado = 'Aplicada',
            IdPeriodoAplicado = @IdPeriodoPago,
            IdMovimientoTrabajador = @IdMovimientoTrabajador,
            FechaAplicacion = GETDATE()
        WHERE IdCuotaProgramada = @IdCuotaProgramada;

        IF EXISTS (SELECT 1 FROM dbo.PrestamoTrabajador WHERE IdPrestamoTrabajador = @ReferenciaId)
        BEGIN
            UPDATE dbo.PrestamoTrabajador
            SET SaldoPendiente = CASE WHEN SaldoPendiente - @MontoCuota < 0 THEN 0 ELSE SaldoPendiente - @MontoCuota END
            WHERE IdPrestamoTrabajador = @ReferenciaId;

            UPDATE dbo.PrestamoTrabajador
            SET Estado = 'Pagado'
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

CREATE OR ALTER PROCEDURE dbo.USP_DES_PRESTAMO_PAGO_EXTRA_REGISTRAR
(
    @IdPrestamoTrabajador INT,
    @FechaPago DATE,
    @MontoPago DECIMAL(18,2),
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

        DECLARE @SaldoAnterior DECIMAL(18,2), @Estado VARCHAR(30);

        SELECT @SaldoAnterior = SaldoPendiente, @Estado = Estado
        FROM dbo.PrestamoTrabajador
        WHERE IdPrestamoTrabajador = @IdPrestamoTrabajador;

        IF @SaldoAnterior IS NULL
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'El prestamo no existe.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        IF @Estado IN ('Cancelado', 'Pagado', 'Anulado')
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'No se puede registrar pagos en un prestamo cancelado, pagado o anulado.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        IF @MontoPago <= 0 OR @MontoPago > @SaldoAnterior
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'El pago extraordinario debe ser mayor a cero y no superar el saldo pendiente.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        INSERT INTO dbo.PrestamoPagoExtraordinario
        (
            IdPrestamoTrabajador,
            FechaPago,
            MontoPago,
            SaldoAnterior,
            SaldoPosterior,
            Observacion,
            UsuarioRegistro
        )
        VALUES
        (
            @IdPrestamoTrabajador,
            @FechaPago,
            @MontoPago,
            @SaldoAnterior,
            @SaldoAnterior - @MontoPago,
            @Observacion,
            @Usuario
        );

        UPDATE dbo.PrestamoTrabajador
        SET SaldoPendiente = @SaldoAnterior - @MontoPago,
            Estado = CASE WHEN @SaldoAnterior - @MontoPago <= 0 THEN 'Pagado' ELSE 'Activo' END
        WHERE IdPrestamoTrabajador = @IdPrestamoTrabajador;

        IF @SaldoAnterior - @MontoPago <= 0
        BEGIN
            UPDATE dbo.CuotaProgramadaTrabajador
            SET Estado = 'Cancelada',
                Observacion = CONCAT(ISNULL(Observacion, ''), ' Pago extraordinario cancela saldo.')
            WHERE TipoOrigen = 'Prestamo'
              AND ReferenciaId = @IdPrestamoTrabajador
              AND Estado IN ('Pendiente', 'Suspendida');
        END;

        INSERT INTO dbo.Auditoria(Usuario, Accion, Modulo, Descripcion, Equipo)
        VALUES(@Usuario, 'PAGO EXTRA', 'DESTAJO Y PAGOS', CONCAT('Pago extraordinario prestamo ', @IdPrestamoTrabajador), HOST_NAME());

        COMMIT TRANSACTION;

        SET @Resultado = 1;
        SET @Mensaje = 'Pago extraordinario registrado correctamente.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_CUOTA_SUSPENDER
(
    @IdCuotaProgramada INT,
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

        DECLARE @IdPrestamoTrabajador INT, @FechaAnterior DATE, @MontoAnterior DECIMAL(18,2), @EstadoAnterior VARCHAR(30);

        SELECT
            @IdPrestamoTrabajador = ReferenciaId,
            @FechaAnterior = FechaProgramada,
            @MontoAnterior = MontoCuota,
            @EstadoAnterior = Estado
        FROM dbo.CuotaProgramadaTrabajador
        WHERE IdCuotaProgramada = @IdCuotaProgramada
          AND TipoOrigen = 'Prestamo';

        IF @IdPrestamoTrabajador IS NULL OR @EstadoAnterior <> 'Pendiente'
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'Solo se pueden suspender cuotas pendientes.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        IF EXISTS (SELECT 1 FROM dbo.PrestamoTrabajador WHERE IdPrestamoTrabajador = @IdPrestamoTrabajador AND Estado IN ('Cancelado', 'Pagado', 'Anulado'))
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'No se puede suspender una cuota de un prestamo finalizado.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        UPDATE dbo.CuotaProgramadaTrabajador
        SET Estado = 'Suspendida',
            Observacion = @Observacion
        WHERE IdCuotaProgramada = @IdCuotaProgramada;

        UPDATE dbo.PrestamoTrabajador
        SET Estado = 'Suspendido'
        WHERE IdPrestamoTrabajador = @IdPrestamoTrabajador;

        INSERT INTO dbo.PrestamoCronogramaHistorial
        (
            IdPrestamoTrabajador, IdCuotaProgramada, Accion,
            FechaProgramadaAnterior, MontoAnterior, EstadoAnterior,
            EstadoNuevo, Observacion, UsuarioRegistro
        )
        VALUES
        (
            @IdPrestamoTrabajador, @IdCuotaProgramada, 'SUSPENDER',
            @FechaAnterior, @MontoAnterior, @EstadoAnterior,
            'Suspendida', @Observacion, @Usuario
        );

        COMMIT TRANSACTION;

        SET @Resultado = 1;
        SET @Mensaje = 'Cuota suspendida correctamente.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_CUOTA_REPROGRAMAR
(
    @IdCuotaProgramada INT,
    @FechaProgramada DATE,
    @MontoCuota DECIMAL(18,2),
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

        DECLARE
            @IdPrestamoTrabajador INT,
            @FechaAnterior DATE,
            @MontoAnterior DECIMAL(18,2),
            @EstadoAnterior VARCHAR(30),
            @SaldoPendiente DECIMAL(18,2);

        SELECT
            @IdPrestamoTrabajador = C.ReferenciaId,
            @FechaAnterior = C.FechaProgramada,
            @MontoAnterior = C.MontoCuota,
            @EstadoAnterior = C.Estado,
            @SaldoPendiente = P.SaldoPendiente
        FROM dbo.CuotaProgramadaTrabajador C
        INNER JOIN dbo.PrestamoTrabajador P ON P.IdPrestamoTrabajador = C.ReferenciaId
        WHERE C.IdCuotaProgramada = @IdCuotaProgramada
          AND C.TipoOrigen = 'Prestamo';

        IF @IdPrestamoTrabajador IS NULL OR @EstadoAnterior NOT IN ('Pendiente', 'Suspendida')
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'Solo se pueden reprogramar cuotas pendientes o suspendidas.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        IF @MontoCuota <= 0 OR @MontoCuota > @SaldoPendiente
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'El nuevo monto debe ser mayor a cero y no superar el saldo pendiente.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        UPDATE dbo.CuotaProgramadaTrabajador
        SET FechaProgramada = @FechaProgramada,
            MontoCuota = @MontoCuota,
            Estado = 'Pendiente',
            Observacion = @Observacion
        WHERE IdCuotaProgramada = @IdCuotaProgramada;

        UPDATE dbo.PrestamoTrabajador
        SET Estado = 'Activo'
        WHERE IdPrestamoTrabajador = @IdPrestamoTrabajador
          AND Estado = 'Suspendido';

        INSERT INTO dbo.PrestamoCronogramaHistorial
        (
            IdPrestamoTrabajador, IdCuotaProgramada, Accion,
            FechaProgramadaAnterior, FechaProgramadaNueva,
            MontoAnterior, MontoNuevo,
            EstadoAnterior, EstadoNuevo,
            Observacion, UsuarioRegistro
        )
        VALUES
        (
            @IdPrestamoTrabajador, @IdCuotaProgramada, 'REPROGRAMAR',
            @FechaAnterior, @FechaProgramada,
            @MontoAnterior, @MontoCuota,
            @EstadoAnterior, 'Pendiente',
            @Observacion, @Usuario
        );

        COMMIT TRANSACTION;

        SET @Resultado = 1;
        SET @Mensaje = 'Cuota reprogramada correctamente.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_PRESTAMO_CANCELAR
(
    @IdPrestamoTrabajador INT,
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

        DECLARE @SaldoAnterior DECIMAL(18,2), @Estado VARCHAR(30);

        SELECT @SaldoAnterior = SaldoPendiente, @Estado = Estado
        FROM dbo.PrestamoTrabajador
        WHERE IdPrestamoTrabajador = @IdPrestamoTrabajador;

        IF @SaldoAnterior IS NULL
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'El prestamo no existe.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        IF @Estado IN ('Cancelado', 'Pagado', 'Anulado')
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'El prestamo ya se encuentra finalizado.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        UPDATE dbo.PrestamoTrabajador
        SET SaldoPendiente = 0,
            Estado = 'Cancelado',
            Observacion = @Observacion
        WHERE IdPrestamoTrabajador = @IdPrestamoTrabajador;

        UPDATE dbo.CuotaProgramadaTrabajador
        SET Estado = 'Cancelada',
            Observacion = @Observacion
        WHERE TipoOrigen = 'Prestamo'
          AND ReferenciaId = @IdPrestamoTrabajador
          AND Estado IN ('Pendiente', 'Suspendida');

        INSERT INTO dbo.PrestamoCronogramaHistorial
        (
            IdPrestamoTrabajador, Accion, MontoAnterior, MontoNuevo,
            EstadoAnterior, EstadoNuevo, Observacion, UsuarioRegistro
        )
        VALUES
        (
            @IdPrestamoTrabajador, 'CANCELAR', @SaldoAnterior, 0,
            @Estado, 'Cancelado', @Observacion, @Usuario
        );

        INSERT INTO dbo.Auditoria(Usuario, Accion, Modulo, Descripcion, Equipo)
        VALUES(@Usuario, 'CANCELAR PRESTAMO', 'DESTAJO Y PAGOS', CONCAT('Prestamo cancelado ', @IdPrestamoTrabajador), HOST_NAME());

        COMMIT TRANSACTION;

        SET @Resultado = 1;
        SET @Mensaje = 'Prestamo cancelado correctamente.';
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
                SUM(CASE WHEN M.TipoMovimiento <> 'Pago' AND M.EsDescuento = 0 AND M.CategoriaMovimiento <> 'Saldo' THEN M.Importe ELSE 0 END) AS TotalIngresos,
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
                SUM(CASE WHEN M.CategoriaMovimiento = 'Saldo' AND M.EsDescuento = 0 THEN M.Importe ELSE 0 END) AS SaldoAnterior,
                SUM(CASE WHEN M.TipoMovimiento <> 'Pago' AND M.EsDescuento = 0 THEN M.Importe ELSE 0 END) AS TotalIngresos,
                SUM(CASE WHEN M.EsDescuento = 1 OR M.TipoMovimiento = 'Descuento' THEN M.Importe ELSE 0 END) AS TotalDescuentos,
                SUM(CASE WHEN M.TipoMovimiento = 'Pago' THEN M.Importe ELSE 0 END) AS TotalPagado
            FROM dbo.MovimientoTrabajador M
            WHERE M.IdPeriodoPago = @IdPeriodoPago
            AND M.IdTrabajadorOperativo = @IdTrabajadorOperativo
            AND M.Eliminado = 0
        )
        SELECT @SaldoPendiente =
            ISNULL(SaldoAnterior, 0) + ISNULL(TotalIngresos, 0) - ISNULL(TotalDescuentos, 0) - ISNULL(TotalPagado, 0)
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
                        AND D.Estado IN ('Pendiente', 'Pago Parcial', 'Parcial')
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

CREATE OR ALTER PROCEDURE dbo.USP_DES_PAGO_TRABAJADOR_LISTAR
(
    @IdPeriodoPago INT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        P.IdPagoTrabajador,
        P.IdPeriodoPago,
        PE.CodigoPeriodo,
        P.IdTrabajadorOperativo,
        CONCAT(E.Nombre, ' ', E.Apellido) AS NombreTrabajador,
        P.IdLotePagoDetalle,
        P.FechaPago,
        P.MedioPago,
        ISNULL(P.NumeroOperacion, '') AS NumeroOperacion,
        P.MontoPagado,
        ISNULL(P.Estado, 'Confirmado') AS Estado,
        ISNULL(P.Observacion, '') AS Observacion,
        ISNULL(P.UsuarioRegistro, '') AS UsuarioRegistro,
        ISNULL(P.MotivoAnulacion, '') AS MotivoAnulacion,
        ISNULL(P.UsuarioAnulacion, '') AS UsuarioAnulacion,
        ISNULL(P.AutorizadoPor, '') AS AutorizadoPor
    FROM dbo.PagoTrabajador P
    INNER JOIN dbo.PeriodoPago PE ON PE.IdPeriodoPago = P.IdPeriodoPago
    INNER JOIN dbo.TrabajadorOperativo T ON T.IdTrabajadorOperativo = P.IdTrabajadorOperativo
    INNER JOIN dbo.Empleados E ON E.IdEmpleado = T.IdEmpleado
    WHERE (@IdPeriodoPago IS NULL OR P.IdPeriodoPago = @IdPeriodoPago)
    ORDER BY P.FechaPago DESC, P.IdPagoTrabajador DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_PAGO_TRABAJADOR_REGISTRAR
(
    @IdPeriodoPago INT,
    @IdTrabajadorOperativo INT,
    @IdLotePagoDetalle INT = NULL,
    @MedioPago VARCHAR(40),
    @MontoPagado DECIMAL(18,2),
    @FechaPago DATETIME,
    @NumeroOperacion VARCHAR(80),
    @Observacion VARCHAR(300),
    @MedioPago2 VARCHAR(40) = '',
    @MontoPagado2 DECIMAL(18,2) = 0,
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

        DECLARE @TotalPago DECIMAL(18,2) = ISNULL(@MontoPagado, 0) + ISNULL(@MontoPagado2, 0);
        DECLARE @MedioPrincipal VARCHAR(40) = CASE WHEN ISNULL(@MontoPagado2, 0) > 0 THEN 'Mixto' ELSE @MedioPago END;
        DECLARE @SaldoPendiente DECIMAL(18,2);

        WITH Totales AS
        (
            SELECT
                SUM(CASE WHEN M.CategoriaMovimiento = 'Saldo' AND M.EsDescuento = 0 THEN M.Importe ELSE 0 END) AS SaldoAnterior,
                SUM(CASE WHEN M.TipoMovimiento <> 'Pago' AND M.EsDescuento = 0 AND M.CategoriaMovimiento <> 'Saldo' THEN M.Importe ELSE 0 END) AS TotalIngresos,
                SUM(CASE WHEN M.EsDescuento = 1 OR M.TipoMovimiento = 'Descuento' THEN M.Importe ELSE 0 END) AS TotalDescuentos,
                SUM(CASE WHEN M.TipoMovimiento = 'Pago' THEN M.Importe ELSE 0 END) AS TotalPagado
            FROM dbo.MovimientoTrabajador M
            WHERE M.IdPeriodoPago = @IdPeriodoPago
            AND M.IdTrabajadorOperativo = @IdTrabajadorOperativo
            AND M.Eliminado = 0
        )
        SELECT @SaldoPendiente =
            ISNULL(SaldoAnterior, 0) + ISNULL(TotalIngresos, 0) - ISNULL(TotalDescuentos, 0) - ISNULL(TotalPagado, 0)
        FROM Totales;

        IF ISNULL(@SaldoPendiente, 0) <= 0
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'El trabajador no tiene saldo pendiente.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        IF @TotalPago <= 0 OR @TotalPago > @SaldoPendiente
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'El monto a pagar debe ser mayor a cero y no superar el saldo pendiente.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        IF ISNULL(@MontoPagado2, 0) > 0 AND (NULLIF(LTRIM(RTRIM(@MedioPago2)), '') IS NULL OR @MedioPago = @MedioPago2)
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'Cada medio de pago mixto debe tener su propio importe y ser distinto.';
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
            FechaPago,
            MedioPago,
            NumeroOperacion,
            MontoPagado,
            Estado,
            Observacion,
            UsuarioRegistro
        )
        VALUES
        (
            @IdPeriodoPago,
            @IdTrabajadorOperativo,
            @IdLotePagoDetalle,
            @FechaPago,
            @MedioPrincipal,
            @NumeroOperacion,
            @TotalPago,
            'Confirmado',
            @Observacion,
            @Usuario
        );

        DECLARE @IdPagoTrabajador INT = SCOPE_IDENTITY();

        INSERT INTO dbo.PagoTrabajadorMedio(IdPagoTrabajador, MedioPago, MontoPagado, NumeroOperacion)
        VALUES(@IdPagoTrabajador, @MedioPago, @MontoPagado, @NumeroOperacion);

        IF ISNULL(@MontoPagado2, 0) > 0
        BEGIN
            INSERT INTO dbo.PagoTrabajadorMedio(IdPagoTrabajador, MedioPago, MontoPagado, NumeroOperacion)
            VALUES(@IdPagoTrabajador, @MedioPago2, @MontoPagado2, @NumeroOperacion);
        END;

        INSERT INTO dbo.MovimientoTrabajador
        (
            IdPeriodoPago, IdTrabajadorOperativo, Fecha, TipoMovimiento, CategoriaMovimiento,
            IdConceptoMovimiento, Descripcion, Cantidad, UnidadMedida, Tarifa, Importe,
            EsDescuento, EsAutomatico, OrigenMovimiento, ReferenciaId, Estado, Observacion, CreadoPor
        )
        VALUES
        (
            @IdPeriodoPago, @IdTrabajadorOperativo, @FechaPago, 'Pago', 'Pago',
            @IdConceptoPago, CONCAT('Pago por ', @MedioPrincipal), 1, 'Pago', @TotalPago, @TotalPago,
            0, 1, 'PagoTrabajador', @IdPagoTrabajador, 'Aprobado', @Observacion, @Usuario
        );

        IF @IdLotePagoDetalle IS NOT NULL
        BEGIN
            UPDATE D
            SET Estado = CASE WHEN @SaldoPendiente - @TotalPago <= 0 THEN 'Pagado / Cerrado' ELSE 'Parcial' END
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
                        AND D.Estado IN ('Pendiente', 'Pago Parcial', 'Parcial')
                    )
                    THEN 'Pago Parcial'
                    ELSE 'Pagado / Cerrado'
                END
            FROM dbo.LotePago L
            INNER JOIN dbo.LotePagoDetalle D ON D.IdLotePago = L.IdLotePago
            WHERE D.IdLotePagoDetalle = @IdLotePagoDetalle;
        END;

        UPDATE C
        SET TotalPagado = ISNULL(P.TotalPagado, 0),
            SaldoPendiente = C.TotalPorPagar - ISNULL(P.TotalPagado, 0),
            EstadoCalculo = CASE
                WHEN C.TotalPorPagar - ISNULL(P.TotalPagado, 0) <= 0 THEN 'Pagado'
                WHEN ISNULL(P.TotalPagado, 0) > 0 THEN 'Parcial'
                ELSE C.EstadoCalculo
            END
        FROM dbo.CalculoPeriodoTrabajador C
        OUTER APPLY
        (
            SELECT SUM(M.Importe) AS TotalPagado
            FROM dbo.MovimientoTrabajador M
            WHERE M.IdPeriodoPago = C.IdPeriodoPago
            AND M.IdTrabajadorOperativo = C.IdTrabajadorOperativo
            AND M.TipoMovimiento = 'Pago'
            AND M.Eliminado = 0
        ) P
        WHERE C.IdPeriodoPago = @IdPeriodoPago
        AND C.IdTrabajadorOperativo = @IdTrabajadorOperativo;

        INSERT INTO dbo.Auditoria(Usuario, Accion, Modulo, Descripcion, Equipo)
        VALUES(@Usuario, 'REGISTRAR PAGO', 'DESTAJO Y PAGOS', CONCAT('Pago trabajador ', @IdPagoTrabajador), HOST_NAME());

        COMMIT TRANSACTION;

        SET @Resultado = 1;
        SET @Mensaje =
            CASE
                WHEN @SaldoPendiente - @TotalPago <= 0 THEN 'Pago completo registrado correctamente.'
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

CREATE OR ALTER PROCEDURE dbo.USP_DES_PAGO_TRABAJADOR_ANULAR
(
    @IdPagoTrabajador INT,
    @MotivoAnulacion VARCHAR(300),
    @AutorizadoPor VARCHAR(80),
    @Usuario VARCHAR(80),
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @IdPeriodoPago INT, @IdTrabajadorOperativo INT, @IdLotePagoDetalle INT, @Estado VARCHAR(30);

        SELECT
            @IdPeriodoPago = IdPeriodoPago,
            @IdTrabajadorOperativo = IdTrabajadorOperativo,
            @IdLotePagoDetalle = IdLotePagoDetalle,
            @Estado = Estado
        FROM dbo.PagoTrabajador
        WHERE IdPagoTrabajador = @IdPagoTrabajador;

        IF @IdPeriodoPago IS NULL
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'El pago seleccionado no existe.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        IF @Estado = 'Anulado'
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'El pago ya se encuentra anulado.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        IF NULLIF(LTRIM(RTRIM(@MotivoAnulacion)), '') IS NULL OR NULLIF(LTRIM(RTRIM(@AutorizadoPor)), '') IS NULL
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'La anulación requiere motivo y autorización.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        UPDATE dbo.PagoTrabajador
        SET Estado = 'Anulado',
            MotivoAnulacion = @MotivoAnulacion,
            UsuarioAnulacion = @Usuario,
            FechaAnulacion = GETDATE(),
            AutorizadoPor = @AutorizadoPor
        WHERE IdPagoTrabajador = @IdPagoTrabajador;

        UPDATE dbo.MovimientoTrabajador
        SET Eliminado = 1,
            Estado = 'Anulado',
            Observacion = CONCAT(ISNULL(Observacion, ''), ' | Pago anulado: ', @MotivoAnulacion),
            ModificadoPor = @Usuario,
            FechaModificacion = GETDATE()
        WHERE OrigenMovimiento = 'PagoTrabajador'
        AND ReferenciaId = @IdPagoTrabajador
        AND Eliminado = 0;

        IF @IdLotePagoDetalle IS NOT NULL
           AND NOT EXISTS
           (
                SELECT 1
                FROM dbo.MovimientoTrabajador
                WHERE OrigenMovimiento = 'PagoTrabajador'
                AND ReferenciaId = @IdPagoTrabajador
           )
        BEGIN
            UPDATE dbo.MovimientoTrabajador
            SET Eliminado = 1,
                Estado = 'Anulado',
                Observacion = CONCAT(ISNULL(Observacion, ''), ' | Pago de lote anulado: ', @MotivoAnulacion),
                ModificadoPor = @Usuario,
                FechaModificacion = GETDATE()
            WHERE OrigenMovimiento = 'LotePago'
            AND ReferenciaId = @IdLotePagoDetalle
            AND Eliminado = 0;
        END;

        IF @IdLotePagoDetalle IS NOT NULL
        BEGIN
            UPDATE D
            SET Estado =
                CASE
                    WHEN ISNULL(P.TotalPagadoDetalle, 0) <= 0 THEN 'Pendiente'
                    WHEN ISNULL(P.TotalPagadoDetalle, 0) < D.MontoPago THEN 'Parcial'
                    ELSE 'Pagado / Cerrado'
                END
            FROM dbo.LotePagoDetalle D
            OUTER APPLY
            (
                SELECT SUM(PT.MontoPagado) AS TotalPagadoDetalle
                FROM dbo.PagoTrabajador PT
                WHERE PT.IdLotePagoDetalle = D.IdLotePagoDetalle
                AND PT.Estado = 'Confirmado'
            ) P
            WHERE D.IdLotePagoDetalle = @IdLotePagoDetalle;

            UPDATE L
            SET Estado =
                CASE
                    WHEN EXISTS
                    (
                        SELECT 1
                        FROM dbo.LotePagoDetalle D
                        WHERE D.IdLotePago = L.IdLotePago
                        AND D.Estado IN ('Pago Parcial', 'Parcial')
                    )
                    THEN 'Pago Parcial'
                    WHEN EXISTS
                    (
                        SELECT 1
                        FROM dbo.LotePagoDetalle D
                        WHERE D.IdLotePago = L.IdLotePago
                        AND D.Estado = 'Pendiente'
                    )
                    THEN 'Pendiente'
                    ELSE 'Pagado / Cerrado'
                END
            FROM dbo.LotePago L
            INNER JOIN dbo.LotePagoDetalle D ON D.IdLotePago = L.IdLotePago
            WHERE D.IdLotePagoDetalle = @IdLotePagoDetalle;
        END;

        UPDATE C
        SET TotalPagado = ISNULL(P.TotalPagado, 0),
            SaldoPendiente = C.TotalPorPagar - ISNULL(P.TotalPagado, 0),
            EstadoCalculo = CASE
                WHEN C.TotalPorPagar - ISNULL(P.TotalPagado, 0) <= 0 AND ISNULL(P.TotalPagado, 0) > 0 THEN 'Pagado'
                WHEN ISNULL(P.TotalPagado, 0) > 0 THEN 'Parcial'
                ELSE 'Calculado'
            END
        FROM dbo.CalculoPeriodoTrabajador C
        OUTER APPLY
        (
            SELECT SUM(M.Importe) AS TotalPagado
            FROM dbo.MovimientoTrabajador M
            WHERE M.IdPeriodoPago = C.IdPeriodoPago
            AND M.IdTrabajadorOperativo = C.IdTrabajadorOperativo
            AND M.TipoMovimiento = 'Pago'
            AND M.Eliminado = 0
        ) P
        WHERE C.IdPeriodoPago = @IdPeriodoPago
        AND C.IdTrabajadorOperativo = @IdTrabajadorOperativo;

        INSERT INTO dbo.Auditoria(Usuario, Accion, Modulo, Descripcion, Equipo)
        VALUES(@Usuario, 'ANULAR PAGO', 'DESTAJO Y PAGOS', CONCAT('Pago anulado ', @IdPagoTrabajador, ' autorizado por ', @AutorizadoPor), HOST_NAME());

        COMMIT TRANSACTION;

        SET @Resultado = 1;
        SET @Mensaje = 'Pago anulado correctamente.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_DASHBOARD_INDICADORES
(
    @IdPeriodoPago INT
)
AS
BEGIN
    SET NOCOUNT ON;

    WITH Resumen AS
    (
        SELECT
            M.IdPeriodoPago,
            COUNT(DISTINCT M.IdTrabajadorOperativo) AS TrabajadoresConMovimientos,
            SUM(CASE WHEN M.EsDescuento = 0 AND M.TipoMovimiento <> 'Pago' AND M.CategoriaMovimiento IN ('Produccion', 'Produccion por destajo') THEN M.Cantidad ELSE 0 END) AS TotalProducido,
            SUM(CASE WHEN M.EsDescuento = 0 AND M.TipoMovimiento <> 'Pago' THEN M.Importe ELSE 0 END) AS TotalIngresos,
            SUM(CASE WHEN M.EsDescuento = 1 OR M.TipoMovimiento = 'Descuento' THEN M.Importe ELSE 0 END) AS TotalDescuentos,
            SUM(CASE WHEN M.TipoMovimiento = 'Pago' THEN M.Importe ELSE 0 END) AS TotalPagado
        FROM dbo.MovimientoTrabajador M
        WHERE M.IdPeriodoPago = @IdPeriodoPago
        AND M.Eliminado = 0
        GROUP BY M.IdPeriodoPago
    )
    SELECT
        (SELECT COUNT(1) FROM dbo.TrabajadorOperativo WHERE Estado = 1) AS TrabajadoresActivos,
        ISNULL(R.TrabajadoresConMovimientos, 0) AS TrabajadoresConMovimientos,
        ISNULL(R.TotalProducido, 0) AS TotalProducido,
        ISNULL(R.TotalIngresos, 0) AS TotalIngresos,
        ISNULL(R.TotalDescuentos, 0) AS TotalDescuentos,
        ISNULL(R.TotalIngresos, 0) - ISNULL(R.TotalDescuentos, 0) AS NetoPeriodo,
        ISNULL(R.TotalPagado, 0) AS TotalPagado,
        ISNULL(R.TotalIngresos, 0) - ISNULL(R.TotalDescuentos, 0) - ISNULL(R.TotalPagado, 0) AS SaldoPendiente,
        (SELECT COUNT(1) FROM dbo.PrestamoTrabajador WHERE Estado IN ('Registrado', 'Activo', 'En descuento') AND SaldoPendiente > 0) AS PrestamosActivos,
        (SELECT COUNT(1) FROM dbo.CuotaProgramadaTrabajador WHERE IdPeriodoAplicado = @IdPeriodoPago AND Estado = 'Aplicada') AS CuotasAplicadas,
        (SELECT COUNT(1) FROM dbo.PeriodoPago WHERE Estado = 'Abierto') AS PeriodosAbiertos,
        (SELECT COUNT(1) FROM dbo.PeriodoPago WHERE Estado IN ('Calculado', 'En pago')) AS PeriodosPendientesCierre
    FROM (SELECT 1 AS Dummy) D
    LEFT JOIN Resumen R ON R.IdPeriodoPago = @IdPeriodoPago;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_DASHBOARD_SERIES
(
    @IdPeriodoPago INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        'Produccion diaria' AS Categoria,
        CONVERT(VARCHAR(10), M.Fecha, 103) AS Etiqueta,
        SUM(M.Cantidad) AS Valor,
        SUM(M.Importe) AS Importe
    FROM dbo.MovimientoTrabajador M
    WHERE M.IdPeriodoPago = @IdPeriodoPago
    AND M.Eliminado = 0
    AND M.EsDescuento = 0
    AND M.TipoMovimiento <> 'Pago'
    AND M.CategoriaMovimiento IN ('Produccion', 'Produccion por destajo')
    GROUP BY M.Fecha

    UNION ALL

    SELECT
        'Produccion por trabajador',
        CONCAT(E.Nombre, ' ', E.Apellido),
        SUM(M.Cantidad),
        SUM(M.Importe)
    FROM dbo.MovimientoTrabajador M
    INNER JOIN dbo.TrabajadorOperativo T ON T.IdTrabajadorOperativo = M.IdTrabajadorOperativo
    INNER JOIN dbo.Empleados E ON E.IdEmpleado = T.IdEmpleado
    WHERE M.IdPeriodoPago = @IdPeriodoPago
    AND M.Eliminado = 0
    AND M.EsDescuento = 0
    AND M.TipoMovimiento <> 'Pago'
    AND M.CategoriaMovimiento IN ('Produccion', 'Produccion por destajo')
    GROUP BY E.Nombre, E.Apellido

    UNION ALL

    SELECT
        'Produccion por area',
        ISNULL(A.NombreArea, 'Sin area'),
        SUM(M.Cantidad),
        SUM(M.Importe)
    FROM dbo.MovimientoTrabajador M
    LEFT JOIN dbo.AreaOperativa A ON A.IdAreaOperativa = M.IdAreaOperativa
    WHERE M.IdPeriodoPago = @IdPeriodoPago
    AND M.Eliminado = 0
    AND M.EsDescuento = 0
    AND M.TipoMovimiento <> 'Pago'
    AND M.CategoriaMovimiento IN ('Produccion', 'Produccion por destajo')
    GROUP BY ISNULL(A.NombreArea, 'Sin area')

    UNION ALL

    SELECT
        'Comparativo semanal',
        P.CodigoPeriodo,
        0,
        SUM(CASE WHEN M.EsDescuento = 0 AND M.TipoMovimiento <> 'Pago' THEN M.Importe ELSE 0 END)
            - SUM(CASE WHEN M.EsDescuento = 1 OR M.TipoMovimiento = 'Descuento' THEN M.Importe ELSE 0 END)
    FROM dbo.PeriodoPago P
    LEFT JOIN dbo.MovimientoTrabajador M ON M.IdPeriodoPago = P.IdPeriodoPago AND M.Eliminado = 0
    WHERE P.FechaInicio <= (SELECT FechaInicio FROM dbo.PeriodoPago WHERE IdPeriodoPago = @IdPeriodoPago)
    GROUP BY P.CodigoPeriodo, P.FechaInicio

    UNION ALL

    SELECT
        'Pagos por medio',
        PM.MedioPago,
        COUNT(1),
        SUM(PM.MontoPagado)
    FROM dbo.PagoTrabajador P
    INNER JOIN dbo.PagoTrabajadorMedio PM ON PM.IdPagoTrabajador = P.IdPagoTrabajador
    WHERE P.IdPeriodoPago = @IdPeriodoPago
    AND P.Estado = 'Confirmado'
    GROUP BY PM.MedioPago

    UNION ALL

    SELECT
        'Evolucion saldos',
        P.CodigoPeriodo,
        0,
        SUM(CASE WHEN M.EsDescuento = 0 AND M.TipoMovimiento <> 'Pago' THEN M.Importe ELSE 0 END)
            - SUM(CASE WHEN M.EsDescuento = 1 OR M.TipoMovimiento = 'Descuento' THEN M.Importe ELSE 0 END)
            - SUM(CASE WHEN M.TipoMovimiento = 'Pago' THEN M.Importe ELSE 0 END)
    FROM dbo.PeriodoPago P
    LEFT JOIN dbo.MovimientoTrabajador M ON M.IdPeriodoPago = P.IdPeriodoPago AND M.Eliminado = 0
    WHERE P.FechaInicio <= (SELECT FechaInicio FROM dbo.PeriodoPago WHERE IdPeriodoPago = @IdPeriodoPago)
    GROUP BY P.CodigoPeriodo, P.FechaInicio
    ORDER BY Categoria, Etiqueta;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_AUDITORIA_DESTAJO_LISTAR
(
    @IdPeriodoPago INT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 200
        A.IdAuditoria,
        ISNULL(A.Usuario, '') AS Usuario,
        A.Fecha,
        ISNULL(A.Modulo, '') AS Modulo,
        ISNULL(A.Accion, '') AS Accion,
        ISNULL(A.RegistroAfectado, ISNULL(A.Descripcion, '')) AS RegistroAfectado,
        ISNULL(A.ValorAnterior, '') AS ValorAnterior,
        ISNULL(A.ValorNuevo, '') AS ValorNuevo,
        ISNULL(A.Motivo, '') AS Motivo,
        ISNULL(A.Equipo, '') AS Equipo
    FROM dbo.Auditoria A
    WHERE A.Modulo = 'DESTAJO Y PAGOS'
    AND
    (
        @IdPeriodoPago IS NULL
        OR A.Descripcion LIKE '%' + CAST(@IdPeriodoPago AS VARCHAR(20)) + '%'
        OR A.RegistroAfectado LIKE '%' + CAST(@IdPeriodoPago AS VARCHAR(20)) + '%'
    )
    ORDER BY A.Fecha DESC, A.IdAuditoria DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_PERIODO_BOLETAS_GENERADAS_REGISTRAR
(
    @IdPeriodoPago INT,
    @Cantidad INT,
    @Usuario VARCHAR(80),
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM dbo.PeriodoPago WHERE IdPeriodoPago = @IdPeriodoPago)
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'El periodo seleccionado no existe.';
            RETURN;
        END;

        UPDATE dbo.PeriodoPago
        SET BoletasGeneradas = 1,
            FechaBoletasGeneradas = GETDATE(),
            CantidadBoletasGeneradas = @Cantidad
        WHERE IdPeriodoPago = @IdPeriodoPago;

        INSERT INTO dbo.Auditoria(Usuario, Accion, Modulo, Descripcion, Equipo, RegistroAfectado, ValorNuevo, Motivo)
        VALUES(@Usuario, 'GENERAR BOLETAS', 'DESTAJO Y PAGOS', CONCAT('Periodo ', @IdPeriodoPago, ' boletas ', @Cantidad), HOST_NAME(), CONCAT('PeriodoPago:', @IdPeriodoPago), CONCAT('Boletas=', @Cantidad), 'Generacion de boletas del periodo');

        SET @Resultado = 1;
        SET @Mensaje = 'Boletas registradas correctamente.';
    END TRY
    BEGIN CATCH
        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_DES_PERIODO_CERRAR
(
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

        DECLARE @EstadoActual VARCHAR(40);
        DECLARE @FechaFin DATE;
        DECLARE @IdPeriodoSiguiente INT;
        DECLARE @IdConceptoSaldo INT;
        DECLARE @SaldoPendiente DECIMAL(18,2);

        SELECT @EstadoActual = Estado, @FechaFin = FechaFin
        FROM dbo.PeriodoPago
        WHERE IdPeriodoPago = @IdPeriodoPago;

        IF @EstadoActual IS NULL
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'El periodo seleccionado no existe.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        IF @EstadoActual IN ('Cerrado', 'Anulado')
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'El periodo ya se encuentra cerrado o anulado.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        IF EXISTS (SELECT 1 FROM dbo.MovimientoTrabajador WHERE IdPeriodoPago = @IdPeriodoPago AND Eliminado = 0 AND Estado IN ('Borrador', 'Pendiente'))
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'No se puede cerrar: existen movimientos sin confirmar.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        IF NOT EXISTS (SELECT 1 FROM dbo.CalculoPeriodoTrabajador WHERE IdPeriodoPago = @IdPeriodoPago)
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'No se puede cerrar: el periodo no tiene calculo actualizado.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        IF EXISTS (SELECT 1 FROM dbo.CalculoPeriodoAlerta WHERE IdPeriodoPago = @IdPeriodoPago AND Severidad = 'Error')
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'No se puede cerrar: existen alertas criticas del calculo.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        IF EXISTS
        (
            SELECT 1
            FROM dbo.PagoTrabajador
            WHERE IdPeriodoPago = @IdPeriodoPago
            AND Estado NOT IN ('Confirmado', 'Anulado')
        )
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'No se puede cerrar: existen pagos sin confirmar.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        IF EXISTS (SELECT 1 FROM dbo.PeriodoPago WHERE IdPeriodoPago = @IdPeriodoPago AND BoletasGeneradas = 0)
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'No se puede cerrar: primero debe generar las boletas.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        SELECT @SaldoPendiente = SUM(CASE WHEN SaldoPendiente > 0 THEN SaldoPendiente ELSE 0 END)
        FROM dbo.CalculoPeriodoTrabajador
        WHERE IdPeriodoPago = @IdPeriodoPago;

        SET @SaldoPendiente = ISNULL(@SaldoPendiente, 0);

        SELECT TOP 1 @IdPeriodoSiguiente = IdPeriodoPago
        FROM dbo.PeriodoPago
        WHERE FechaInicio > @FechaFin
        AND Estado IN ('Borrador', 'Abierto')
        ORDER BY FechaInicio;

        IF @SaldoPendiente > 0 AND @IdPeriodoSiguiente IS NULL
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'No se puede cerrar: debe crear el siguiente periodo para trasladar saldos pendientes.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        SELECT @IdConceptoSaldo = IdConceptoMovimiento
        FROM dbo.ConceptoMovimiento
        WHERE CodigoConcepto = 'SALDO_ANT';

        IF @SaldoPendiente > 0 AND @IdConceptoSaldo IS NULL
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'No se puede cerrar: no existe el concepto SALDO_ANT para trasladar saldos.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        IF @SaldoPendiente > 0
        BEGIN
            INSERT INTO dbo.MovimientoTrabajador
            (
                IdPeriodoPago, IdTrabajadorOperativo, Fecha, TipoMovimiento, CategoriaMovimiento,
                IdConceptoMovimiento, Descripcion, Cantidad, UnidadMedida, Tarifa, Importe,
                EsDescuento, EsAutomatico, OrigenMovimiento, ReferenciaId, Estado, Observacion, CreadoPor
            )
            SELECT
                @IdPeriodoSiguiente,
                C.IdTrabajadorOperativo,
                (SELECT FechaInicio FROM dbo.PeriodoPago WHERE IdPeriodoPago = @IdPeriodoSiguiente),
                'Ingreso',
                'Saldo',
                @IdConceptoSaldo,
                CONCAT('Saldo pendiente trasladado del periodo ', @IdPeriodoPago),
                1,
                'Saldo',
                C.SaldoPendiente,
                C.SaldoPendiente,
                0,
                1,
                'CierrePeriodo',
                @IdPeriodoPago,
                'Aprobado',
                'Traslado automatico por cierre de periodo',
                @Usuario
            FROM dbo.CalculoPeriodoTrabajador C
            WHERE C.IdPeriodoPago = @IdPeriodoPago
            AND C.SaldoPendiente > 0
            AND NOT EXISTS
            (
                SELECT 1
                FROM dbo.MovimientoTrabajador M
                WHERE M.IdPeriodoPago = @IdPeriodoSiguiente
                AND M.IdTrabajadorOperativo = C.IdTrabajadorOperativo
                AND M.OrigenMovimiento = 'CierrePeriodo'
                AND M.ReferenciaId = @IdPeriodoPago
                AND M.Eliminado = 0
            );
        END;

        UPDATE dbo.PeriodoPago
        SET Estado = 'Cerrado',
            SaldosTrasladados = 1,
            FechaCierre = GETDATE(),
            UsuarioCierre = @Usuario
        WHERE IdPeriodoPago = @IdPeriodoPago;

        INSERT INTO dbo.Auditoria(Usuario, Accion, Modulo, Descripcion, Equipo, RegistroAfectado, ValorAnterior, ValorNuevo, Motivo)
        VALUES(@Usuario, 'CERRAR PERIODO', 'DESTAJO Y PAGOS', CONCAT('Periodo cerrado ', @IdPeriodoPago), HOST_NAME(), CONCAT('PeriodoPago:', @IdPeriodoPago), @EstadoActual, 'Cerrado', CONCAT('Cierre definitivo. Saldo trasladado: ', @SaldoPendiente));

        COMMIT TRANSACTION;

        SET @Resultado = 1;
        SET @Mensaje = 'Periodo cerrado correctamente.';
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
    ('Dashboard', 1),
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
    'Dashboard',
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
