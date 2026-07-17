IF OBJECT_ID('dbo.TiposServicio', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TiposServicio
    (
        IdTipoServicio INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TiposServicio PRIMARY KEY,
        Codigo VARCHAR(20) NOT NULL,
        Nombre VARCHAR(100) NOT NULL,
        Descripcion VARCHAR(500) NOT NULL CONSTRAINT DF_TiposServicio_Descripcion DEFAULT(''),
        RequiereEntrega BIT NOT NULL CONSTRAINT DF_TiposServicio_RequiereEntrega DEFAULT(0),
        Estado BIT NOT NULL CONSTRAINT DF_TiposServicio_Estado DEFAULT(1)
    );

    CREATE UNIQUE INDEX UX_TiposServicio_Codigo ON dbo.TiposServicio(Codigo);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.TiposServicio)
BEGIN
    INSERT INTO dbo.TiposServicio (Codigo, Nombre, Descripcion, RequiereEntrega, Estado)
    VALUES
        ('BOR', 'Bordado', '', 1, 1),
        ('EST', 'Estampado', '', 1, 1),
        ('CON', 'Confeccion', '', 1, 1),
        ('COR', 'Corte', '', 1, 1),
        ('LAV', 'Lavado', '', 1, 1),
        ('ACA', 'Acabados', '', 1, 1),
        ('REP', 'Reparaciones', '', 1, 1),
        ('CMO', 'Compra de Mochilas', '', 0, 1),
        ('CMA', 'Compra de Maletines', '', 0, 1),
        ('OTR', 'Otros', '', 0, 1);
END;

IF OBJECT_ID('dbo.OrdenesServicio', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrdenesServicio
    (
        IdOrdenServicio INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrdenesServicio PRIMARY KEY,
        NumeroOrden VARCHAR(30) NOT NULL,
        Fecha DATE NOT NULL,
        FechaComprometida DATE NULL,
        IdProveedor INT NOT NULL,
        IdTipoServicio INT NOT NULL,
        Cliente VARCHAR(160) NOT NULL CONSTRAINT DF_OrdenesServicio_Cliente DEFAULT(''),
        OciRelacionada VARCHAR(60) NOT NULL CONSTRAINT DF_OrdenesServicio_Oci DEFAULT(''),
        OtRelacionada VARCHAR(60) NOT NULL CONSTRAINT DF_OrdenesServicio_Ot DEFAULT(''),
        Responsable VARCHAR(100) NOT NULL CONSTRAINT DF_OrdenesServicio_Responsable DEFAULT(''),
        FormaPago VARCHAR(80) NOT NULL CONSTRAINT DF_OrdenesServicio_FormaPago DEFAULT(''),
        Observaciones VARCHAR(1000) NOT NULL CONSTRAINT DF_OrdenesServicio_Observaciones DEFAULT(''),
        Subtotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_OrdenesServicio_Subtotal DEFAULT(0),
        Igv DECIMAL(18,2) NOT NULL CONSTRAINT DF_OrdenesServicio_Igv DEFAULT(0),
        Total DECIMAL(18,2) NOT NULL CONSTRAINT DF_OrdenesServicio_Total DEFAULT(0),
        Estado VARCHAR(40) NOT NULL CONSTRAINT DF_OrdenesServicio_Estado DEFAULT('Borrador'),
        EstadoServicio VARCHAR(40) NOT NULL CONSTRAINT DF_OrdenesServicio_EstadoServicio DEFAULT('Borrador'),
        EstadoPago VARCHAR(40) NOT NULL CONSTRAINT DF_OrdenesServicio_EstadoPago DEFAULT('Pendiente'),
        UsuarioRegistro VARCHAR(80) NOT NULL CONSTRAINT DF_OrdenesServicio_Usuario DEFAULT('Sistema'),
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_OrdenesServicio_FechaRegistro DEFAULT(GETDATE()),
        MotivoAnulacion VARCHAR(500) NOT NULL CONSTRAINT DF_OrdenesServicio_Motivo DEFAULT('')
    );

    CREATE UNIQUE INDEX UX_OrdenesServicio_NumeroOrden ON dbo.OrdenesServicio(NumeroOrden);
END;

IF COL_LENGTH('dbo.OrdenesServicio', 'EstadoServicio') IS NULL
    ALTER TABLE dbo.OrdenesServicio ADD EstadoServicio VARCHAR(40) NOT NULL CONSTRAINT DF_OrdenesServicio_EstadoServicio_Legacy DEFAULT('Borrador');

IF COL_LENGTH('dbo.OrdenesServicio', 'EstadoPago') IS NULL
    ALTER TABLE dbo.OrdenesServicio ADD EstadoPago VARCHAR(40) NOT NULL CONSTRAINT DF_OrdenesServicio_EstadoPago_Legacy DEFAULT('Pendiente');

IF OBJECT_ID('dbo.OrdenServicioDetalle', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrdenServicioDetalle
    (
        IdOrdenServicioDetalle INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrdenServicioDetalle PRIMARY KEY,
        IdOrdenServicio INT NOT NULL,
        IdProducto INT NULL,
        Producto VARCHAR(200) NOT NULL CONSTRAINT DF_OrdenServicioDetalle_Producto DEFAULT(''),
        Descripcion VARCHAR(500) NOT NULL,
        Cantidad DECIMAL(18,2) NOT NULL,
        Unidad VARCHAR(20) NOT NULL CONSTRAINT DF_OrdenServicioDetalle_Unidad DEFAULT('UND'),
        PrecioUnitario DECIMAL(18,2) NOT NULL,
        Total DECIMAL(18,2) NOT NULL,
        Observaciones VARCHAR(500) NOT NULL CONSTRAINT DF_OrdenServicioDetalle_Obs DEFAULT(''),
        CONSTRAINT FK_OrdenServicioDetalle_Orden FOREIGN KEY (IdOrdenServicio) REFERENCES dbo.OrdenesServicio(IdOrdenServicio)
    );
END;

IF OBJECT_ID('dbo.OrdenServicioPagos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrdenServicioPagos
    (
        IdOrdenServicioPago INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrdenServicioPagos PRIMARY KEY,
        IdOrdenServicio INT NOT NULL,
        Fecha DATE NOT NULL,
        TipoPago VARCHAR(40) NOT NULL,
        Importe DECIMAL(18,2) NOT NULL,
        MedioPago VARCHAR(60) NOT NULL CONSTRAINT DF_OrdenServicioPagos_Medio DEFAULT(''),
        NumeroOperacion VARCHAR(80) NOT NULL CONSTRAINT DF_OrdenServicioPagos_Operacion DEFAULT(''),
        Observacion VARCHAR(500) NOT NULL CONSTRAINT DF_OrdenServicioPagos_Obs DEFAULT(''),
        UsuarioRegistro VARCHAR(80) NOT NULL CONSTRAINT DF_OrdenServicioPagos_Usuario DEFAULT('Sistema'),
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_OrdenServicioPagos_FechaRegistro DEFAULT(GETDATE()),
        CONSTRAINT FK_OrdenServicioPagos_Orden FOREIGN KEY (IdOrdenServicio) REFERENCES dbo.OrdenesServicio(IdOrdenServicio)
    );
END;

IF OBJECT_ID('dbo.OrdenServicioHistorial', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrdenServicioHistorial
    (
        IdOrdenServicioHistorial INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrdenServicioHistorial PRIMARY KEY,
        IdOrdenServicio INT NOT NULL,
        Usuario VARCHAR(80) NOT NULL,
        Accion VARCHAR(120) NOT NULL,
        FechaHora DATETIME NOT NULL CONSTRAINT DF_OrdenServicioHistorial_Fecha DEFAULT(GETDATE()),
        CONSTRAINT FK_OrdenServicioHistorial_Orden FOREIGN KEY (IdOrdenServicio) REFERENCES dbo.OrdenesServicio(IdOrdenServicio)
    );
END;

IF OBJECT_ID('dbo.OrdenServicioMovimientos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrdenServicioMovimientos
    (
        IdMovimiento INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrdenServicioMovimientos PRIMARY KEY,
        IdOrdenServicio INT NOT NULL,
        IdOrdenServicioDetalle INT NULL,
        TipoMovimiento VARCHAR(20) NOT NULL,
        Fecha DATE NOT NULL,
        Producto VARCHAR(200) NOT NULL,
        Descripcion VARCHAR(500) NOT NULL CONSTRAINT DF_OrdenServicioMovimientos_Descripcion DEFAULT(''),
        Cantidad DECIMAL(18,2) NOT NULL,
        CantidadAnterior DECIMAL(18,2) NOT NULL CONSTRAINT DF_OrdenServicioMovimientos_Anterior DEFAULT(0),
        CantidadMovimiento DECIMAL(18,2) NOT NULL,
        CantidadPendiente DECIMAL(18,2) NOT NULL CONSTRAINT DF_OrdenServicioMovimientos_Pendiente DEFAULT(0),
        Unidad VARCHAR(20) NOT NULL CONSTRAINT DF_OrdenServicioMovimientos_Unidad DEFAULT('UND'),
        Observacion VARCHAR(500) NOT NULL CONSTRAINT DF_OrdenServicioMovimientos_Obs DEFAULT(''),
        OtRelacionada VARCHAR(60) NOT NULL CONSTRAINT DF_OrdenServicioMovimientos_OT DEFAULT(''),
        UsuarioRegistro VARCHAR(80) NOT NULL CONSTRAINT DF_OrdenServicioMovimientos_Usuario DEFAULT('Sistema'),
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_OrdenServicioMovimientos_Fecha DEFAULT(GETDATE()),
        CONSTRAINT FK_OrdenServicioMovimientos_Orden FOREIGN KEY (IdOrdenServicio) REFERENCES dbo.OrdenesServicio(IdOrdenServicio)
    );
END;

IF OBJECT_ID('dbo.OrdenServicioFotos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrdenServicioFotos
    (
        IdOrdenServicioFoto INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrdenServicioFotos PRIMARY KEY,
        IdOrdenServicio INT NOT NULL,
        IdOrdenServicioDetalle INT NULL,
        RutaArchivo VARCHAR(500) NOT NULL,
        NombreArchivo VARCHAR(260) NOT NULL,
        Titulo VARCHAR(160) NOT NULL CONSTRAINT DF_OrdenServicioFotos_Titulo DEFAULT(''),
        UbicacionPdf VARCHAR(40) NOT NULL CONSTRAINT DF_OrdenServicioFotos_Ubicacion DEFAULT('Abajo'),
        Descripcion VARCHAR(500) NOT NULL CONSTRAINT DF_OrdenServicioFotos_Descripcion DEFAULT(''),
        UsuarioRegistro VARCHAR(80) NOT NULL CONSTRAINT DF_OrdenServicioFotos_Usuario DEFAULT('Sistema'),
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_OrdenServicioFotos_Fecha DEFAULT(GETDATE()),
        CONSTRAINT FK_OrdenServicioFotos_Orden FOREIGN KEY (IdOrdenServicio) REFERENCES dbo.OrdenesServicio(IdOrdenServicio)
    );
END;

IF COL_LENGTH('dbo.OrdenServicioFotos', 'Titulo') IS NULL
    ALTER TABLE dbo.OrdenServicioFotos ADD Titulo VARCHAR(160) NOT NULL CONSTRAINT DF_OrdenServicioFotos_Titulo_Legacy DEFAULT('');

IF COL_LENGTH('dbo.OrdenServicioFotos', 'UbicacionPdf') IS NULL
    ALTER TABLE dbo.OrdenServicioFotos ADD UbicacionPdf VARCHAR(40) NOT NULL CONSTRAINT DF_OrdenServicioFotos_Ubicacion_Legacy DEFAULT('Abajo');

DECLARE @IdModulo INT;

IF NOT EXISTS (SELECT 1 FROM dbo.Menu WHERE NombreMenu = N'Órdenes de Servicio' AND IdMenuPadre IS NULL)
BEGIN
    INSERT INTO dbo.Menu (NombreMenu, IdMenuPadre, Orden, Estado)
    VALUES (N'Órdenes de Servicio', NULL, 7, 1);
END;

SELECT @IdModulo = IdMenu
FROM dbo.Menu
WHERE NombreMenu = N'Órdenes de Servicio'
  AND IdMenuPadre IS NULL;

IF @IdModulo IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.Menu WHERE NombreMenu = N'Lista de Órdenes de Servicio' AND IdMenuPadre = @IdModulo)
        INSERT INTO dbo.Menu (NombreMenu, IdMenuPadre, Orden, Estado) VALUES (N'Lista de Órdenes de Servicio', @IdModulo, 1, 1);

    IF NOT EXISTS (SELECT 1 FROM dbo.Menu WHERE NombreMenu = N'Proveedores OS' AND IdMenuPadre = @IdModulo)
        INSERT INTO dbo.Menu (NombreMenu, IdMenuPadre, Orden, Estado) VALUES (N'Proveedores OS', @IdModulo, 2, 1);

    IF NOT EXISTS (SELECT 1 FROM dbo.Menu WHERE NombreMenu = N'Tipos de Servicio' AND IdMenuPadre = @IdModulo)
        INSERT INTO dbo.Menu (NombreMenu, IdMenuPadre, Orden, Estado) VALUES (N'Tipos de Servicio', @IdModulo, 3, 1);

    IF NOT EXISTS (SELECT 1 FROM dbo.Menu WHERE NombreMenu = N'Reportes OS' AND IdMenuPadre = @IdModulo)
        INSERT INTO dbo.Menu (NombreMenu, IdMenuPadre, Orden, Estado) VALUES (N'Reportes OS', @IdModulo, 4, 1);
END;
