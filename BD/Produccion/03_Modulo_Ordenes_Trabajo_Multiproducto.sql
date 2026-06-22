SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.OrdenTrabajo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrdenTrabajo
    (
        IdOrdenTrabajo INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrdenTrabajo PRIMARY KEY,
        NumeroOT VARCHAR(30) NOT NULL,
        IdOrdenCompraInterna INT NOT NULL,
        IdCliente INT NOT NULL,
        NombreCliente VARCHAR(200) NOT NULL,
        FechaEmision DATE NOT NULL CONSTRAINT DF_OrdenTrabajo_Fecha DEFAULT(CONVERT(DATE,GETDATE())),
        Estado VARCHAR(20) NOT NULL CONSTRAINT DF_OrdenTrabajo_Estado DEFAULT('PENDIENTE'),
        IdUsuarioCreacion INT NOT NULL,
        IdUsuarioAutorizaCreacion INT NULL,
        TipoOT VARCHAR(20) NOT NULL CONSTRAINT DF_OrdenTrabajo_Tipo DEFAULT('OCI'),
        IdOrdenTrabajoRelacionada INT NULL,
        Observacion NVARCHAR(500) NOT NULL CONSTRAINT DF_OrdenTrabajo_Observacion DEFAULT(N''),
        FechaRegistro DATETIME2(0) NOT NULL CONSTRAINT DF_OrdenTrabajo_Registro DEFAULT(SYSDATETIME()),
        CONSTRAINT UQ_OrdenTrabajo_Numero UNIQUE(NumeroOT),
        CONSTRAINT UQ_OrdenTrabajo_OCI UNIQUE(IdOrdenCompraInterna),
        CONSTRAINT FK_OrdenTrabajo_OCI FOREIGN KEY(IdOrdenCompraInterna) REFERENCES dbo.OrdenesCompraInterna(IdOrdenCompraInterna),
        CONSTRAINT FK_OrdenTrabajo_Usuario FOREIGN KEY(IdUsuarioCreacion) REFERENCES dbo.Usuarios(IdUsuario),
        CONSTRAINT CK_OrdenTrabajo_Estado CHECK(Estado IN ('PENDIENTE','EMITIDA','EN_PROCESO','PARCIAL','TERMINADA','ANULADA'))
    );
END;
GO

IF COL_LENGTH('dbo.OrdenTrabajo','TipoOT') IS NULL ALTER TABLE dbo.OrdenTrabajo ADD TipoOT VARCHAR(20) NOT NULL CONSTRAINT DF_OrdenTrabajo_Tipo DEFAULT('OCI') WITH VALUES;
IF COL_LENGTH('dbo.OrdenTrabajo','IdOrdenTrabajoRelacionada') IS NULL ALTER TABLE dbo.OrdenTrabajo ADD IdOrdenTrabajoRelacionada INT NULL;
IF COL_LENGTH('dbo.OrdenTrabajo','IdUsuarioAutorizaCreacion') IS NULL ALTER TABLE dbo.OrdenTrabajo ADD IdUsuarioAutorizaCreacion INT NULL;
GO

IF OBJECT_ID(N'dbo.OrdenTrabajoDetalle', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrdenTrabajoDetalle
    (
        IdDetalleOT INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrdenTrabajoDetalle PRIMARY KEY,
        IdOrdenTrabajo INT NOT NULL,
        IdOrdenCompraInternaDetalle INT NOT NULL,
        IdProducto INT NOT NULL,
        CodigoProducto VARCHAR(50) NOT NULL,
        NombreProducto VARCHAR(200) NOT NULL,
        CantidadRequerida DECIMAL(18,2) NOT NULL,
        CantidadPlanificada DECIMAL(18,2) NOT NULL,
        CantidadLanzada DECIMAL(18,2) NOT NULL CONSTRAINT DF_OTDetalle_Lanzada DEFAULT(0),
        CantidadProducida DECIMAL(18,2) NOT NULL CONSTRAINT DF_OTDetalle_Producida DEFAULT(0),
        CantidadAplicada DECIMAL(18,2) NOT NULL CONSTRAINT DF_OTDetalle_Aplicada DEFAULT(0),
        CantidadExcedente DECIMAL(18,2) NOT NULL CONSTRAINT DF_OTDetalle_Excedente DEFAULT(0),
        CantidadPendiente DECIMAL(18,2) NOT NULL,
        Estado VARCHAR(20) NOT NULL CONSTRAINT DF_OTDetalle_Estado DEFAULT('PENDIENTE'),
        MotivoDiferencia NVARCHAR(200) NOT NULL CONSTRAINT DF_OTDetalle_Motivo DEFAULT(N''),
        ObservacionDiferencia NVARCHAR(500) NOT NULL CONSTRAINT DF_OTDetalle_Obs DEFAULT(N''),
        IdUsuarioAutorizaLanzamiento INT NULL,
        FechaInicio DATETIME2(0) NULL,
        FechaFin DATETIME2(0) NULL,
        CONSTRAINT UQ_OTDetalle_OCI UNIQUE(IdOrdenCompraInternaDetalle),
        CONSTRAINT FK_OTDetalle_OT FOREIGN KEY(IdOrdenTrabajo) REFERENCES dbo.OrdenTrabajo(IdOrdenTrabajo),
        CONSTRAINT FK_OTDetalle_OCIDet FOREIGN KEY(IdOrdenCompraInternaDetalle) REFERENCES dbo.OrdenCompraInternaDetalle(IdOrdenCompraInternaDetalle),
        CONSTRAINT FK_OTDetalle_Producto FOREIGN KEY(IdProducto) REFERENCES dbo.Productos(IdProducto),
        CONSTRAINT FK_OTDetalle_Autoriza FOREIGN KEY(IdUsuarioAutorizaLanzamiento) REFERENCES dbo.Usuarios(IdUsuario),
        CONSTRAINT CK_OTDetalle_Cantidades CHECK(CantidadRequerida > 0 AND CantidadPlanificada > 0 AND CantidadLanzada >= 0 AND CantidadProducida >= 0 AND CantidadAplicada >= 0 AND CantidadExcedente >= 0 AND CantidadPendiente >= 0),
        CONSTRAINT CK_OTDetalle_Estado CHECK(Estado IN ('PENDIENTE','EN_PROCESO','PARCIAL','TERMINADO','ANULADO'))
    );
    CREATE INDEX IX_OTDetalle_OT ON dbo.OrdenTrabajoDetalle(IdOrdenTrabajo, Estado);
END;
GO

IF OBJECT_ID(N'dbo.OrdenTrabajoDetalleArea', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrdenTrabajoDetalleArea
    (
        IdDetalleArea BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrdenTrabajoDetalleArea PRIMARY KEY,
        IdOrdenTrabajo INT NOT NULL,
        IdDetalleOT INT NOT NULL,
        IdAreaProduccion INT NOT NULL,
        CodigoArea VARCHAR(20) NOT NULL,
        NombreArea NVARCHAR(100) NOT NULL,
        OrdenSecuencia INT NOT NULL,
        EsInicio BIT NOT NULL,
        EsTermino BIT NOT NULL,
        ManejaMerma BIT NOT NULL,
        ModoEnvio VARCHAR(10) NOT NULL,
        CantidadRecibida DECIMAL(18,2) NOT NULL CONSTRAINT DF_OTArea_Recibida DEFAULT(0),
        CantidadEnviada DECIMAL(18,2) NOT NULL CONSTRAINT DF_OTArea_Enviada DEFAULT(0),
        CantidadMerma DECIMAL(18,2) NOT NULL CONSTRAINT DF_OTArea_Merma DEFAULT(0),
        CantidadPendiente AS CONVERT(DECIMAL(18,2), CantidadRecibida-CantidadEnviada-CantidadMerma) PERSISTED,
        Estado VARCHAR(20) NOT NULL CONSTRAINT DF_OTArea_Estado DEFAULT('PENDIENTE'),
        FechaInicio DATETIME2(0) NULL,
        FechaFin DATETIME2(0) NULL,
        CONSTRAINT UQ_OTArea_DetalleArea UNIQUE(IdDetalleOT, IdAreaProduccion),
        CONSTRAINT FK_OTArea_OT FOREIGN KEY(IdOrdenTrabajo) REFERENCES dbo.OrdenTrabajo(IdOrdenTrabajo),
        CONSTRAINT FK_OTArea_Detalle FOREIGN KEY(IdDetalleOT) REFERENCES dbo.OrdenTrabajoDetalle(IdDetalleOT),
        CONSTRAINT FK_OTArea_Area FOREIGN KEY(IdAreaProduccion) REFERENCES dbo.AreaProduccion(IdAreaProduccion),
        CONSTRAINT CK_OTArea_Modo CHECK(ModoEnvio IN ('UNICO','PARCIAL')),
        CONSTRAINT CK_OTArea_Cantidades CHECK(CantidadRecibida >= 0 AND CantidadEnviada >= 0 AND CantidadMerma >= 0 AND CantidadEnviada+CantidadMerma <= CantidadRecibida),
        CONSTRAINT CK_OTArea_Estado CHECK(Estado IN ('PENDIENTE','EN_PROCESO','PARCIAL','FINALIZADA','BLOQUEADA','ANULADA'))
    );
    CREATE INDEX IX_OTArea_Consulta ON dbo.OrdenTrabajoDetalleArea(IdOrdenTrabajo, IdAreaProduccion, Estado) INCLUDE(IdDetalleOT,CantidadRecibida,CantidadEnviada,CantidadMerma);
END;
GO

IF OBJECT_ID(N'dbo.OrdenTrabajoTransferencia', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrdenTrabajoTransferencia
    (
        IdOperacionTransferencia BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OTTransferencia PRIMARY KEY,
        Identificador UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_OTTransferencia_Id DEFAULT(NEWSEQUENTIALID()),
        IdOrdenTrabajo INT NOT NULL,
        IdAreaOrigen INT NOT NULL,
        IdAreaDestino INT NOT NULL,
        IdUsuarioSesion INT NOT NULL,
        IdUsuarioAutoriza INT NOT NULL,
        FechaRegistro DATETIME2(0) NOT NULL CONSTRAINT DF_OTTransferencia_Fecha DEFAULT(SYSDATETIME()),
        Observacion NVARCHAR(500) NOT NULL CONSTRAINT DF_OTTransferencia_Obs DEFAULT(N''),
        Estado VARCHAR(15) NOT NULL CONSTRAINT DF_OTTransferencia_Estado DEFAULT('CONFIRMADA'),
        CONSTRAINT UQ_OTTransferencia_Identificador UNIQUE(Identificador),
        CONSTRAINT FK_OTTrans_OT FOREIGN KEY(IdOrdenTrabajo) REFERENCES dbo.OrdenTrabajo(IdOrdenTrabajo),
        CONSTRAINT FK_OTTrans_Origen FOREIGN KEY(IdAreaOrigen) REFERENCES dbo.AreaProduccion(IdAreaProduccion),
        CONSTRAINT FK_OTTrans_Destino FOREIGN KEY(IdAreaDestino) REFERENCES dbo.AreaProduccion(IdAreaProduccion),
        CONSTRAINT FK_OTTrans_Sesion FOREIGN KEY(IdUsuarioSesion) REFERENCES dbo.Usuarios(IdUsuario),
        CONSTRAINT FK_OTTrans_Autoriza FOREIGN KEY(IdUsuarioAutoriza) REFERENCES dbo.Usuarios(IdUsuario)
    );
END;
GO

IF OBJECT_ID(N'dbo.OrdenTrabajoTransferenciaDetalle', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrdenTrabajoTransferenciaDetalle
    (
        IdDetalleTransferencia BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OTTransferenciaDetalle PRIMARY KEY,
        IdOperacionTransferencia BIGINT NOT NULL,
        IdDetalleOT INT NOT NULL,
        IdDetalleAreaOrigen BIGINT NOT NULL,
        IdDetalleAreaDestino BIGINT NOT NULL,
        CantidadEnviada DECIMAL(18,2) NOT NULL,
        IdUsuarioSesion INT NOT NULL,
        IdUsuarioAutoriza INT NOT NULL,
        Estado VARCHAR(15) NOT NULL CONSTRAINT DF_OTTransDet_Estado DEFAULT('CONFIRMADO'),
        CONSTRAINT UQ_OTTransDet_Producto UNIQUE(IdOperacionTransferencia, IdDetalleOT),
        CONSTRAINT FK_OTTransDet_Operacion FOREIGN KEY(IdOperacionTransferencia) REFERENCES dbo.OrdenTrabajoTransferencia(IdOperacionTransferencia),
        CONSTRAINT FK_OTTransDet_Detalle FOREIGN KEY(IdDetalleOT) REFERENCES dbo.OrdenTrabajoDetalle(IdDetalleOT),
        CONSTRAINT FK_OTTransDet_Origen FOREIGN KEY(IdDetalleAreaOrigen) REFERENCES dbo.OrdenTrabajoDetalleArea(IdDetalleArea),
        CONSTRAINT FK_OTTransDet_Destino FOREIGN KEY(IdDetalleAreaDestino) REFERENCES dbo.OrdenTrabajoDetalleArea(IdDetalleArea),
        CONSTRAINT FK_OTTransDet_Sesion FOREIGN KEY(IdUsuarioSesion) REFERENCES dbo.Usuarios(IdUsuario),
        CONSTRAINT FK_OTTransDet_Autoriza FOREIGN KEY(IdUsuarioAutoriza) REFERENCES dbo.Usuarios(IdUsuario),
        CONSTRAINT CK_OTTransDet_Cantidad CHECK(CantidadEnviada > 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.OrdenTrabajoMerma', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrdenTrabajoMerma
    (
        IdMerma BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OTMerma PRIMARY KEY,
        IdOrdenTrabajo INT NOT NULL,
        IdDetalleOT INT NOT NULL,
        IdDetalleArea BIGINT NOT NULL,
        Cantidad DECIMAL(18,2) NOT NULL,
        Motivo NVARCHAR(200) NOT NULL,
        IdUsuarioSesion INT NOT NULL,
        IdUsuarioAutoriza INT NOT NULL,
        FechaRegistro DATETIME2(0) NOT NULL CONSTRAINT DF_OTMerma_Fecha DEFAULT(SYSDATETIME()),
        CONSTRAINT FK_OTMerma_OT FOREIGN KEY(IdOrdenTrabajo) REFERENCES dbo.OrdenTrabajo(IdOrdenTrabajo),
        CONSTRAINT FK_OTMerma_Detalle FOREIGN KEY(IdDetalleOT) REFERENCES dbo.OrdenTrabajoDetalle(IdDetalleOT),
        CONSTRAINT FK_OTMerma_Area FOREIGN KEY(IdDetalleArea) REFERENCES dbo.OrdenTrabajoDetalleArea(IdDetalleArea),
        CONSTRAINT FK_OTMerma_Sesion FOREIGN KEY(IdUsuarioSesion) REFERENCES dbo.Usuarios(IdUsuario),
        CONSTRAINT FK_OTMerma_Autoriza FOREIGN KEY(IdUsuarioAutoriza) REFERENCES dbo.Usuarios(IdUsuario),
        CONSTRAINT CK_OTMerma_Cantidad CHECK(Cantidad > 0)
    );
END;
GO

IF NOT EXISTS(SELECT 1 FROM sys.types WHERE is_table_type=1 AND name='TipoOTPlanificacion')
    EXEC('CREATE TYPE dbo.TipoOTPlanificacion AS TABLE(IdOrdenCompraInternaDetalle INT PRIMARY KEY, CantidadPlanificada DECIMAL(18,2) NOT NULL)');
GO
IF NOT EXISTS(SELECT 1 FROM sys.types WHERE is_table_type=1 AND name='TipoOTLanzamiento')
    EXEC('CREATE TYPE dbo.TipoOTLanzamiento AS TABLE(IdDetalleOT INT PRIMARY KEY, CantidadLanzada DECIMAL(18,2) NOT NULL, Motivo NVARCHAR(200) NULL, Observacion NVARCHAR(500) NULL)');
GO
IF NOT EXISTS(SELECT 1 FROM sys.types WHERE is_table_type=1 AND name='TipoOTTransferencia')
    EXEC('CREATE TYPE dbo.TipoOTTransferencia AS TABLE(IdDetalleOT INT PRIMARY KEY, Cantidad DECIMAL(18,2) NOT NULL)');
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_CREAR
    @IdOrdenCompraInterna INT, @IdUsuario INT, @Observacion NVARCHAR(500),
    @Detalles dbo.TipoOTPlanificacion READONLY, @IdOrdenTrabajo INT OUTPUT, @NumeroOT VARCHAR(30) OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        IF NOT EXISTS(SELECT 1 FROM dbo.Usuarios WHERE IdUsuario=@IdUsuario AND Estado=1) THROW 51000, 'El usuario de sesión no es válido.', 1;
        IF EXISTS(SELECT 1 FROM dbo.OrdenTrabajo WHERE IdOrdenCompraInterna=@IdOrdenCompraInterna) THROW 51000, 'La OCI ya tiene una Orden de Trabajo.', 1;
        IF NOT EXISTS(SELECT 1 FROM @Detalles) THROW 51000, 'Seleccione al menos un producto.', 1;
        IF (SELECT COUNT(*) FROM @Detalles)<>(SELECT COUNT(*) FROM dbo.OrdenCompraInternaDetalle WHERE IdOrdenCompraInterna=@IdOrdenCompraInterna AND Cantidad>CantidadDespachada) THROW 51000, 'La OT debe incluir todos los productos pendientes de la OCI.', 1;
        IF EXISTS(SELECT 1 FROM @Detalles x LEFT JOIN dbo.OrdenCompraInternaDetalle d ON d.IdOrdenCompraInternaDetalle=x.IdOrdenCompraInternaDetalle AND d.IdOrdenCompraInterna=@IdOrdenCompraInterna WHERE d.IdOrdenCompraInternaDetalle IS NULL OR x.CantidadPlanificada<=0 OR d.Cantidad-d.CantidadDespachada<=0) THROW 51000, 'La planificación contiene productos sin pendiente o cantidades no válidas.', 1;
        IF NOT EXISTS(SELECT 1 FROM dbo.AreaProduccion WHERE Activo=1 AND EsInicio=1) OR NOT EXISTS(SELECT 1 FROM dbo.AreaProduccion WHERE Activo=1 AND EsTermino=1) THROW 51000, 'Configure las áreas activas de inicio y término.', 1;

        DECLARE @Correlativo INT = ISNULL((SELECT MAX(TRY_CONVERT(INT,RIGHT(NumeroOT,6))) FROM dbo.OrdenTrabajo WITH(UPDLOCK,HOLDLOCK)),0)+1;
        SET @NumeroOT=CONCAT('OT-',RIGHT(CONCAT('000000',@Correlativo),6));
        INSERT dbo.OrdenTrabajo(NumeroOT,IdOrdenCompraInterna,IdCliente,NombreCliente,IdUsuarioCreacion,Observacion,Estado)
        SELECT @NumeroOT,o.IdOrdenCompraInterna,o.IdCliente,o.NombreCliente,@IdUsuario,ISNULL(@Observacion,N''),'PENDIENTE' FROM dbo.OrdenesCompraInterna o WHERE o.IdOrdenCompraInterna=@IdOrdenCompraInterna AND o.Estado<>'Anulado';
        IF @@ROWCOUNT=0 THROW 51000, 'La OCI no existe o está anulada.', 1;
        SET @IdOrdenTrabajo=CONVERT(INT,SCOPE_IDENTITY());

        INSERT dbo.OrdenTrabajoDetalle(IdOrdenTrabajo,IdOrdenCompraInternaDetalle,IdProducto,CodigoProducto,NombreProducto,CantidadRequerida,CantidadPlanificada,CantidadPendiente)
        SELECT @IdOrdenTrabajo,d.IdOrdenCompraInternaDetalle,d.IdProducto,d.CodigoProducto,d.NombreProducto,
               CASE WHEN d.Cantidad-d.CantidadDespachada<0 THEN 0 ELSE d.Cantidad-d.CantidadDespachada END,x.CantidadPlanificada,x.CantidadPlanificada
        FROM @Detalles x JOIN dbo.OrdenCompraInternaDetalle d ON d.IdOrdenCompraInternaDetalle=x.IdOrdenCompraInternaDetalle;

        INSERT dbo.OrdenTrabajoDetalleArea(IdOrdenTrabajo,IdDetalleOT,IdAreaProduccion,CodigoArea,NombreArea,OrdenSecuencia,EsInicio,EsTermino,ManejaMerma,ModoEnvio)
        SELECT @IdOrdenTrabajo,d.IdDetalleOT,a.IdAreaProduccion,a.CodigoArea,a.NombreArea,a.OrdenSecuencia,a.EsInicio,a.EsTermino,a.ManejaMerma,a.ModoEnvio
        FROM dbo.OrdenTrabajoDetalle d CROSS JOIN dbo.AreaProduccion a WHERE d.IdOrdenTrabajo=@IdOrdenTrabajo AND a.Activo=1;
        UPDATE a SET CantidadRecibida=d.CantidadPlanificada,Estado='PENDIENTE'
        FROM dbo.OrdenTrabajoDetalleArea a JOIN dbo.OrdenTrabajoDetalle d ON d.IdDetalleOT=a.IdDetalleOT
        WHERE a.IdOrdenTrabajo=@IdOrdenTrabajo AND a.EsInicio=1;
        UPDATE dbo.OrdenesCompraInterna SET TieneOrdenTrabajo=1,Estado='PROCESO' WHERE IdOrdenCompraInterna=@IdOrdenCompraInterna;
        COMMIT;
    END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_LISTAR
AS
BEGIN
    SET NOCOUNT ON;
    SELECT o.IdOrdenTrabajo,o.NumeroOT,o.IdOrdenCompraInterna,oci.NumeroOci,oci.OrdenCompraCliente,o.IdCliente,o.NombreCliente,o.FechaEmision,o.Estado,o.IdUsuarioCreacion,u.NombreUsuario,o.Observacion,o.FechaRegistro,
           o.TipoOT,o.IdOrdenTrabajoRelacionada,rel.NumeroOT NumeroOTRelacionada,ISNULL(ua.NombreUsuario,u.NombreUsuario) UsuarioAutoriza,
           COUNT(d.IdDetalleOT) CantidadProductos,SUM(d.CantidadPlanificada) TotalPlanificado,SUM(d.CantidadLanzada) TotalLanzado
    FROM dbo.OrdenTrabajo o JOIN dbo.OrdenesCompraInterna oci ON oci.IdOrdenCompraInterna=o.IdOrdenCompraInterna
    JOIN dbo.Usuarios u ON u.IdUsuario=o.IdUsuarioCreacion LEFT JOIN dbo.Usuarios ua ON ua.IdUsuario=o.IdUsuarioAutorizaCreacion LEFT JOIN dbo.OrdenTrabajo rel ON rel.IdOrdenTrabajo=o.IdOrdenTrabajoRelacionada LEFT JOIN dbo.OrdenTrabajoDetalle d ON d.IdOrdenTrabajo=o.IdOrdenTrabajo
    GROUP BY o.IdOrdenTrabajo,o.NumeroOT,o.IdOrdenCompraInterna,oci.NumeroOci,oci.OrdenCompraCliente,o.IdCliente,o.NombreCliente,o.FechaEmision,o.Estado,o.IdUsuarioCreacion,u.NombreUsuario,o.Observacion,o.FechaRegistro,o.TipoOT,o.IdOrdenTrabajoRelacionada,rel.NumeroOT,ua.NombreUsuario ORDER BY o.IdOrdenTrabajo DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_OBTENER @IdOrdenTrabajo INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT o.*,oci.NumeroOci,oci.OrdenCompraCliente,u.NombreUsuario,ISNULL(ua.NombreUsuario,u.NombreUsuario) UsuarioAutoriza FROM dbo.OrdenTrabajo o JOIN dbo.OrdenesCompraInterna oci ON oci.IdOrdenCompraInterna=o.IdOrdenCompraInterna JOIN dbo.Usuarios u ON u.IdUsuario=o.IdUsuarioCreacion LEFT JOIN dbo.Usuarios ua ON ua.IdUsuario=o.IdUsuarioAutorizaCreacion WHERE o.IdOrdenTrabajo=@IdOrdenTrabajo;
    SELECT * FROM dbo.OrdenTrabajoDetalle WHERE IdOrdenTrabajo=@IdOrdenTrabajo ORDER BY IdDetalleOT;
    SELECT a.*,d.CodigoProducto,d.NombreProducto FROM dbo.OrdenTrabajoDetalleArea a JOIN dbo.OrdenTrabajoDetalle d ON d.IdDetalleOT=a.IdDetalleOT WHERE a.IdOrdenTrabajo=@IdOrdenTrabajo ORDER BY a.OrdenSecuencia,d.NombreProducto;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_LANZAR
    @IdOrdenTrabajo INT,@IdUsuarioSesion INT,@IdUsuarioAutoriza INT,@Detalles dbo.TipoOTLanzamiento READONLY
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRY BEGIN TRAN;
        IF NOT EXISTS(SELECT 1 FROM @Detalles) THROW 51000,'Seleccione al menos un producto.',1;
        IF NOT EXISTS(SELECT 1 FROM dbo.Usuarios WHERE IdUsuario=@IdUsuarioAutoriza AND Estado=1) THROW 51000,'El usuario autorizador no es válido.',1;
        IF EXISTS(SELECT 1 FROM @Detalles x LEFT JOIN dbo.OrdenTrabajoDetalle d ON d.IdDetalleOT=x.IdDetalleOT AND d.IdOrdenTrabajo=@IdOrdenTrabajo WHERE d.IdDetalleOT IS NULL OR d.Estado<>'PENDIENTE' OR x.CantidadLanzada<=0) THROW 51000,'Uno de los productos no pertenece a la OT, ya fue iniciado o tiene cantidad inválida.',1;
        IF EXISTS(SELECT 1 FROM @Detalles x JOIN dbo.OrdenTrabajoDetalle d ON d.IdDetalleOT=x.IdDetalleOT WHERE x.CantidadLanzada<>d.CantidadPlanificada AND (NULLIF(LTRIM(RTRIM(x.Motivo)),N'') IS NULL OR @IdUsuarioAutoriza IS NULL)) THROW 51000,'Todo lanzamiento diferente a lo planificado requiere motivo y autorización.',1;
        UPDATE d SET CantidadLanzada=x.CantidadLanzada,CantidadPendiente=x.CantidadLanzada,Estado='EN_PROCESO',MotivoDiferencia=ISNULL(x.Motivo,N''),ObservacionDiferencia=ISNULL(x.Observacion,N''),IdUsuarioAutorizaLanzamiento=@IdUsuarioAutoriza,FechaInicio=SYSDATETIME()
        FROM dbo.OrdenTrabajoDetalle d JOIN @Detalles x ON x.IdDetalleOT=d.IdDetalleOT;
        UPDATE a SET CantidadRecibida=x.CantidadLanzada,Estado='EN_PROCESO',FechaInicio=SYSDATETIME() FROM dbo.OrdenTrabajoDetalleArea a JOIN @Detalles x ON x.IdDetalleOT=a.IdDetalleOT WHERE a.EsInicio=1;
        UPDATE dbo.OrdenTrabajo SET Estado='EN_PROCESO' WHERE IdOrdenTrabajo=@IdOrdenTrabajo;
        COMMIT; END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_TRANSFERIR
    @IdOrdenTrabajo INT,@IdAreaOrigen INT,@IdUsuarioSesion INT,@IdUsuarioAutoriza INT,@Observacion NVARCHAR(500),@Detalles dbo.TipoOTTransferencia READONLY,@IdOperacion BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRY BEGIN TRAN;
        IF NOT EXISTS(SELECT 1 FROM @Detalles) THROW 51000,'Seleccione al menos un producto.',1;
        IF NOT EXISTS(SELECT 1 FROM dbo.Usuarios WHERE IdUsuario=@IdUsuarioSesion AND Estado=1) OR NOT EXISTS(SELECT 1 FROM dbo.Usuarios WHERE IdUsuario=@IdUsuarioAutoriza AND Estado=1) THROW 51000,'El usuario de sesión o autorizador no es válido.',1;
        DECLARE @OrdenOrigen INT=(SELECT OrdenSecuencia FROM dbo.AreaProduccion WHERE IdAreaProduccion=@IdAreaOrigen);
        DECLARE @IdAreaDestino INT=(SELECT TOP(1) IdAreaProduccion FROM dbo.AreaProduccion WHERE Activo=1 AND OrdenSecuencia>@OrdenOrigen ORDER BY OrdenSecuencia);
        DECLARE @DestinoEsTermino BIT=(SELECT EsTermino FROM dbo.AreaProduccion WHERE IdAreaProduccion=@IdAreaDestino);
        IF @IdAreaDestino IS NULL THROW 51000,'El área seleccionada no tiene una siguiente área.',1;
        DECLARE @Error NVARCHAR(2048);
        SELECT TOP(1) @Error=CONCAT('Producto ',ISNULL(a.CodigoProducto,CONVERT(VARCHAR(20),x.IdDetalleOT)),': ',CASE WHEN a.IdDetalleArea IS NULL THEN 'no pertenece a la OT o no está en el área de origen' WHEN d.Estado IN('TERMINADO','ANULADO') OR a.Estado IN('FINALIZADA','BLOQUEADA','ANULADA') THEN 'está finalizado o bloqueado' WHEN x.Cantidad<=0 THEN 'la cantidad debe ser mayor a cero' WHEN x.Cantidad>a.CantidadPendiente THEN 'la cantidad supera el pendiente disponible' WHEN a.ModoEnvio='UNICO' AND (a.CantidadEnviada>0 OR x.Cantidad<>a.CantidadPendiente) THEN 'el modo ÚNICO exige un solo envío por todo el saldo' WHEN dest.IdDetalleArea IS NULL THEN 'no tiene configurada el área de destino' END)
        FROM @Detalles x LEFT JOIN (SELECT da.*,d.CodigoProducto FROM dbo.OrdenTrabajoDetalleArea da JOIN dbo.OrdenTrabajoDetalle d ON d.IdDetalleOT=da.IdDetalleOT WHERE da.IdOrdenTrabajo=@IdOrdenTrabajo AND da.IdAreaProduccion=@IdAreaOrigen) a ON a.IdDetalleOT=x.IdDetalleOT
        LEFT JOIN dbo.OrdenTrabajoDetalle d ON d.IdDetalleOT=x.IdDetalleOT LEFT JOIN dbo.OrdenTrabajoDetalleArea dest ON dest.IdDetalleOT=x.IdDetalleOT AND dest.IdAreaProduccion=@IdAreaDestino
        WHERE a.IdDetalleArea IS NULL OR d.Estado IN('TERMINADO','ANULADO') OR a.Estado IN('FINALIZADA','BLOQUEADA','ANULADA') OR x.Cantidad<=0 OR x.Cantidad>a.CantidadPendiente OR (a.ModoEnvio='UNICO' AND (a.CantidadEnviada>0 OR x.Cantidad<>a.CantidadPendiente)) OR dest.IdDetalleArea IS NULL;
        IF @Error IS NOT NULL THROW 51000,@Error,1;
        INSERT dbo.OrdenTrabajoTransferencia(IdOrdenTrabajo,IdAreaOrigen,IdAreaDestino,IdUsuarioSesion,IdUsuarioAutoriza,Observacion) VALUES(@IdOrdenTrabajo,@IdAreaOrigen,@IdAreaDestino,@IdUsuarioSesion,@IdUsuarioAutoriza,ISNULL(@Observacion,N'')); SET @IdOperacion=SCOPE_IDENTITY();
        INSERT dbo.OrdenTrabajoTransferenciaDetalle(IdOperacionTransferencia,IdDetalleOT,IdDetalleAreaOrigen,IdDetalleAreaDestino,CantidadEnviada,IdUsuarioSesion,IdUsuarioAutoriza)
        SELECT @IdOperacion,x.IdDetalleOT,o.IdDetalleArea,d.IdDetalleArea,x.Cantidad,@IdUsuarioSesion,@IdUsuarioAutoriza FROM @Detalles x JOIN dbo.OrdenTrabajoDetalleArea o ON o.IdDetalleOT=x.IdDetalleOT AND o.IdAreaProduccion=@IdAreaOrigen JOIN dbo.OrdenTrabajoDetalleArea d ON d.IdDetalleOT=x.IdDetalleOT AND d.IdAreaProduccion=@IdAreaDestino;
        UPDATE a SET CantidadEnviada=CantidadEnviada+x.Cantidad,Estado=CASE WHEN CantidadRecibida-(CantidadEnviada+x.Cantidad)-CantidadMerma=0 THEN 'FINALIZADA' ELSE 'PARCIAL' END,FechaFin=CASE WHEN CantidadRecibida-(CantidadEnviada+x.Cantidad)-CantidadMerma=0 THEN SYSDATETIME() ELSE NULL END FROM dbo.OrdenTrabajoDetalleArea a JOIN @Detalles x ON x.IdDetalleOT=a.IdDetalleOT WHERE a.IdAreaProduccion=@IdAreaOrigen;
        UPDATE a SET CantidadRecibida=CantidadRecibida+x.Cantidad,Estado='EN_PROCESO',FechaInicio=COALESCE(FechaInicio,SYSDATETIME()) FROM dbo.OrdenTrabajoDetalleArea a JOIN @Detalles x ON x.IdDetalleOT=a.IdDetalleOT WHERE a.IdAreaProduccion=@IdAreaDestino;
        IF @DestinoEsTermino=1
        BEGIN
            UPDATE d SET CantidadProducida=d.CantidadProducida+x.Cantidad,
                CantidadAplicada=CASE WHEN d.CantidadProducida+x.Cantidad>d.CantidadRequerida THEN d.CantidadRequerida ELSE d.CantidadProducida+x.Cantidad END,
                CantidadExcedente=CASE WHEN d.CantidadProducida+x.Cantidad>d.CantidadRequerida THEN d.CantidadProducida+x.Cantidad-d.CantidadRequerida ELSE 0 END,
                CantidadPendiente=CASE WHEN d.CantidadRequerida-d.CantidadProducida-x.Cantidad>0 THEN d.CantidadRequerida-d.CantidadProducida-x.Cantidad ELSE 0 END,
                Estado=CASE WHEN d.CantidadProducida+x.Cantidad+ISNULL(m.TotalMerma,0)>=d.CantidadLanzada THEN 'TERMINADO' ELSE 'PARCIAL' END,
                FechaFin=CASE WHEN d.CantidadProducida+x.Cantidad+ISNULL(m.TotalMerma,0)>=d.CantidadLanzada THEN SYSDATETIME() ELSE NULL END
            FROM dbo.OrdenTrabajoDetalle d JOIN @Detalles x ON x.IdDetalleOT=d.IdDetalleOT
            OUTER APPLY(SELECT SUM(CantidadMerma) TotalMerma FROM dbo.OrdenTrabajoDetalleArea WHERE IdDetalleOT=d.IdDetalleOT)m;
        END;
        UPDATE o SET Estado=CASE WHEN NOT EXISTS(SELECT 1 FROM dbo.OrdenTrabajoDetalle WHERE IdOrdenTrabajo=o.IdOrdenTrabajo AND Estado<>'TERMINADO') THEN 'TERMINADA' WHEN EXISTS(SELECT 1 FROM dbo.OrdenTrabajoDetalle WHERE IdOrdenTrabajo=o.IdOrdenTrabajo AND Estado='TERMINADO') THEN 'PARCIAL' ELSE 'EN_PROCESO' END FROM dbo.OrdenTrabajo o WHERE o.IdOrdenTrabajo=@IdOrdenTrabajo;
        COMMIT; END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_MERMA_REGISTRAR @IdDetalleArea BIGINT,@Cantidad DECIMAL(18,2),@Motivo NVARCHAR(200),@IdUsuarioSesion INT,@IdUsuarioAutoriza INT
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY BEGIN TRAN;
  DECLARE @IdOT INT,@IdDet INT,@Pendiente DECIMAL(18,2),@Maneja BIT;
  SELECT @IdOT=IdOrdenTrabajo,@IdDet=IdDetalleOT,@Pendiente=CantidadPendiente,@Maneja=ManejaMerma FROM dbo.OrdenTrabajoDetalleArea WITH(UPDLOCK,HOLDLOCK) WHERE IdDetalleArea=@IdDetalleArea;
  IF @Maneja<>1 THROW 51000,'El área no permite registrar merma.',1;
  IF @Cantidad<=0 OR @Cantidad>@Pendiente THROW 51000,'La merma debe ser mayor a cero y no superar el pendiente.',1;
  IF NULLIF(LTRIM(RTRIM(@Motivo)),N'') IS NULL THROW 51000,'Ingrese el motivo de la merma.',1;
  UPDATE dbo.OrdenTrabajoDetalleArea SET CantidadMerma=CantidadMerma+@Cantidad,Estado=CASE WHEN CantidadPendiente-@Cantidad=0 THEN 'FINALIZADA' ELSE 'PARCIAL' END WHERE IdDetalleArea=@IdDetalleArea;
  INSERT dbo.OrdenTrabajoMerma(IdOrdenTrabajo,IdDetalleOT,IdDetalleArea,Cantidad,Motivo,IdUsuarioSesion,IdUsuarioAutoriza) VALUES(@IdOT,@IdDet,@IdDetalleArea,@Cantidad,@Motivo,@IdUsuarioSesion,@IdUsuarioAutoriza);
  UPDATE d SET CantidadPendiente=CASE WHEN CantidadRequerida-CantidadProducida>0 THEN CantidadRequerida-CantidadProducida ELSE 0 END,
      Estado=CASE WHEN CantidadProducida+(SELECT SUM(CantidadMerma) FROM dbo.OrdenTrabajoDetalleArea WHERE IdDetalleOT=@IdDet)>=CantidadLanzada THEN 'TERMINADO' WHEN CantidadProducida>0 THEN 'PARCIAL' ELSE Estado END,
      FechaFin=CASE WHEN CantidadProducida+(SELECT SUM(CantidadMerma) FROM dbo.OrdenTrabajoDetalleArea WHERE IdDetalleOT=@IdDet)>=CantidadLanzada THEN SYSDATETIME() ELSE FechaFin END
  FROM dbo.OrdenTrabajoDetalle d WHERE IdDetalleOT=@IdDet;
  UPDATE o SET Estado=CASE WHEN NOT EXISTS(SELECT 1 FROM dbo.OrdenTrabajoDetalle WHERE IdOrdenTrabajo=@IdOT AND Estado<>'TERMINADO') THEN 'TERMINADA' WHEN EXISTS(SELECT 1 FROM dbo.OrdenTrabajoDetalle WHERE IdOrdenTrabajo=@IdOT AND Estado='TERMINADO') THEN 'PARCIAL' ELSE 'EN_PROCESO' END FROM dbo.OrdenTrabajo o WHERE IdOrdenTrabajo=@IdOT;
  COMMIT; END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH
END;
GO
