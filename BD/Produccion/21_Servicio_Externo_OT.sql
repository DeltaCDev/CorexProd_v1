/*
   Servicio externo para prendas de una OT.
   La prenda conserva su area de produccion; mientras esta fuera, el saldo
   externo se descuenta de la disponibilidad del area. El retorno la deja
   nuevamente disponible para continuar el flujo normal.
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.OrdenTrabajoServicioExterno', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrdenTrabajoServicioExterno
    (
        IdServicioExterno BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OTServicioExterno PRIMARY KEY,
        IdOrdenTrabajo INT NOT NULL,
        Proveedor NVARCHAR(200) NOT NULL,
        FechaSalida DATETIME2(0) NOT NULL CONSTRAINT DF_OTServicioExterno_Salida DEFAULT(SYSDATETIME()),
        FechaRetornoEsperada DATE NULL,
        IdUsuarioSalida INT NOT NULL,
        Observacion NVARCHAR(500) NOT NULL CONSTRAINT DF_OTServicioExterno_Obs DEFAULT(N''),
        Estado VARCHAR(20) NOT NULL CONSTRAINT DF_OTServicioExterno_Estado DEFAULT('ENVIADO'),
        FechaCierre DATETIME2(0) NULL,
        CONSTRAINT FK_OTServicioExterno_OT FOREIGN KEY(IdOrdenTrabajo) REFERENCES dbo.OrdenTrabajo(IdOrdenTrabajo),
        CONSTRAINT FK_OTServicioExterno_Usuario FOREIGN KEY(IdUsuarioSalida) REFERENCES dbo.Usuarios(IdUsuario),
        CONSTRAINT CK_OTServicioExterno_Estado CHECK(Estado IN ('ENVIADO','PARCIAL','RETORNADO','ANULADO'))
    );
    CREATE INDEX IX_OTServicioExterno_OT ON dbo.OrdenTrabajoServicioExterno(IdOrdenTrabajo, Estado, FechaSalida);
END;
GO

IF OBJECT_ID(N'dbo.OrdenTrabajoServicioExternoDetalle', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrdenTrabajoServicioExternoDetalle
    (
        IdDetalleServicioExterno BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OTServicioExternoDetalle PRIMARY KEY,
        IdServicioExterno BIGINT NOT NULL,
        IdDetalleArea BIGINT NOT NULL,
        IdDetalleOT INT NOT NULL,
        CantidadEnviada DECIMAL(18,2) NOT NULL,
        CantidadRetornada DECIMAL(18,2) NOT NULL CONSTRAINT DF_OTServicioExternoDet_Retornada DEFAULT(0),
        SaldoExterno AS CONVERT(DECIMAL(18,2), CantidadEnviada - CantidadRetornada) PERSISTED,
        Estado VARCHAR(20) NOT NULL CONSTRAINT DF_OTServicioExternoDet_Estado DEFAULT('ENVIADO'),
        FechaUltimoRetorno DATETIME2(0) NULL,
        IdUsuarioUltimoRetorno INT NULL,
        Observacion NVARCHAR(500) NOT NULL CONSTRAINT DF_OTServicioExternoDet_Obs DEFAULT(N''),
        CONSTRAINT FK_OTServicioExternoDet_Cab FOREIGN KEY(IdServicioExterno) REFERENCES dbo.OrdenTrabajoServicioExterno(IdServicioExterno),
        CONSTRAINT FK_OTServicioExternoDet_Area FOREIGN KEY(IdDetalleArea) REFERENCES dbo.OrdenTrabajoDetalleArea(IdDetalleArea),
        CONSTRAINT FK_OTServicioExternoDet_OTDet FOREIGN KEY(IdDetalleOT) REFERENCES dbo.OrdenTrabajoDetalle(IdDetalleOT),
        CONSTRAINT CK_OTServicioExternoDet_Cantidades CHECK(CantidadEnviada > 0 AND CantidadRetornada >= 0 AND CantidadRetornada <= CantidadEnviada),
        CONSTRAINT CK_OTServicioExternoDet_Estado CHECK(Estado IN ('ENVIADO','PARCIAL','RETORNADO','ANULADO'))
    );
    CREATE INDEX IX_OTServicioExternoDet_Area ON dbo.OrdenTrabajoServicioExternoDetalle(IdDetalleArea, Estado) INCLUDE(CantidadEnviada, CantidadRetornada, SaldoExterno);
END;
GO

IF COL_LENGTH('dbo.OrdenTrabajoServicioExternoDetalle','IdUsuarioUltimoRetorno') IS NULL
    ALTER TABLE dbo.OrdenTrabajoServicioExternoDetalle ADD IdUsuarioUltimoRetorno INT NULL;
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name='FK_OTServicioExternoDet_UsuarioRetorno')
    ALTER TABLE dbo.OrdenTrabajoServicioExternoDetalle ADD CONSTRAINT FK_OTServicioExternoDet_UsuarioRetorno FOREIGN KEY(IdUsuarioUltimoRetorno) REFERENCES dbo.Usuarios(IdUsuario);
GO

IF NOT EXISTS(SELECT 1 FROM sys.types WHERE is_table_type=1 AND name='TipoOTServicioExterno')
    EXEC('CREATE TYPE dbo.TipoOTServicioExterno AS TABLE(IdDetalleArea BIGINT PRIMARY KEY, Cantidad DECIMAL(18,2) NOT NULL)');
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_SERVICIO_EXTERNO_ENVIAR
    @IdOrdenTrabajo INT,
    @Proveedor NVARCHAR(200),
    @FechaRetornoEsperada DATE = NULL,
    @IdUsuario INT,
    @Observacion NVARCHAR(500) = N'',
    @Detalles dbo.TipoOTServicioExterno READONLY,
    @IdServicioExterno BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF NULLIF(LTRIM(RTRIM(@Proveedor)), N'') IS NULL THROW 51000, 'Ingrese el proveedor o servicio externo.', 1;
    IF NOT EXISTS(SELECT 1 FROM dbo.OrdenTrabajo WHERE IdOrdenTrabajo=@IdOrdenTrabajo) THROW 51000, 'La OT no existe.', 1;
    IF EXISTS(SELECT 1 FROM dbo.OrdenTrabajo WHERE IdOrdenTrabajo=@IdOrdenTrabajo AND UPPER(Estado) IN ('ANULADA','ANULADO')) THROW 51000, 'No se puede enviar una OT anulada.', 1;
    IF NOT EXISTS(SELECT 1 FROM @Detalles WHERE Cantidad > 0) THROW 51000, 'Ingrese al menos una cantidad para enviar.', 1;

    BEGIN TRAN;
    IF EXISTS
    (
        SELECT 1
        FROM @Detalles X
        JOIN dbo.OrdenTrabajoDetalleArea A WITH(UPDLOCK,HOLDLOCK) ON A.IdDetalleArea=X.IdDetalleArea
        OUTER APPLY
        (
            SELECT SUM(D.CantidadEnviada-D.CantidadRetornada) AS Saldo
            FROM dbo.OrdenTrabajoServicioExternoDetalle D
            WHERE D.IdDetalleArea=A.IdDetalleArea AND D.Estado IN ('ENVIADO','PARCIAL')
        ) E
        WHERE A.IdOrdenTrabajo<>@IdOrdenTrabajo
           OR X.Cantidad <= 0
           OR X.Cantidad > A.CantidadPendiente-ISNULL(E.Saldo,0)
           OR A.Estado IN ('FINALIZADA','BLOQUEADA','ANULADA')
    ) THROW 51000, 'Una cantidad supera el pendiente disponible del area o el area no esta habilitada.', 1;

    INSERT dbo.OrdenTrabajoServicioExterno(IdOrdenTrabajo,Proveedor,FechaRetornoEsperada,IdUsuarioSalida,Observacion)
    VALUES(@IdOrdenTrabajo,LTRIM(RTRIM(@Proveedor)),@FechaRetornoEsperada,@IdUsuario,ISNULL(@Observacion,N''));
    SET @IdServicioExterno=SCOPE_IDENTITY();
    INSERT dbo.OrdenTrabajoServicioExternoDetalle(IdServicioExterno,IdDetalleArea,IdDetalleOT,CantidadEnviada,Observacion)
    SELECT @IdServicioExterno,X.IdDetalleArea,A.IdDetalleOT,X.Cantidad,ISNULL(@Observacion,N'')
    FROM @Detalles X JOIN dbo.OrdenTrabajoDetalleArea A ON A.IdDetalleArea=X.IdDetalleArea;
    COMMIT;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_SERVICIO_EXTERNO_RETORNAR
    @IdDetalleServicioExterno BIGINT,
    @Cantidad DECIMAL(18,2),
    @IdUsuario INT,
    @Observacion NVARCHAR(500) = N''
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @Cantidad <= 0 THROW 51000, 'La cantidad retornada debe ser mayor que cero.', 1;
    BEGIN TRAN;
    DECLARE @Saldo DECIMAL(18,2), @IdServicio BIGINT;
    SELECT @Saldo=D.SaldoExterno,@IdServicio=D.IdServicioExterno
    FROM dbo.OrdenTrabajoServicioExternoDetalle D WITH(UPDLOCK,HOLDLOCK)
    JOIN dbo.OrdenTrabajoServicioExterno C ON C.IdServicioExterno=D.IdServicioExterno
    JOIN dbo.OrdenTrabajo O ON O.IdOrdenTrabajo=C.IdOrdenTrabajo
    WHERE D.IdDetalleServicioExterno=@IdDetalleServicioExterno AND D.Estado IN ('ENVIADO','PARCIAL')
      AND UPPER(O.Estado) NOT IN ('ANULADA','ANULADO');
    IF @Saldo IS NULL THROW 51000, 'El detalle no existe o ya fue retornado completamente.', 1;
    IF @Cantidad>@Saldo THROW 51000, 'El retorno supera el saldo que esta en servicio externo.', 1;
    UPDATE dbo.OrdenTrabajoServicioExternoDetalle
    SET CantidadRetornada=CantidadRetornada+@Cantidad,
        Estado=CASE WHEN CantidadRetornada+@Cantidad=CantidadEnviada THEN 'RETORNADO' ELSE 'PARCIAL' END,
        FechaUltimoRetorno=SYSDATETIME(),
        IdUsuarioUltimoRetorno=@IdUsuario,
        Observacion=CASE WHEN NULLIF(LTRIM(RTRIM(@Observacion)),N'') IS NULL THEN Observacion ELSE @Observacion END
    WHERE IdDetalleServicioExterno=@IdDetalleServicioExterno;
    UPDATE C
    SET Estado=CASE WHEN NOT EXISTS(SELECT 1 FROM dbo.OrdenTrabajoServicioExternoDetalle D WHERE D.IdServicioExterno=C.IdServicioExterno AND D.Estado IN ('ENVIADO','PARCIAL')) THEN 'RETORNADO' ELSE 'PARCIAL' END,
        FechaCierre=CASE WHEN NOT EXISTS(SELECT 1 FROM dbo.OrdenTrabajoServicioExternoDetalle D WHERE D.IdServicioExterno=C.IdServicioExterno AND D.Estado IN ('ENVIADO','PARCIAL')) THEN SYSDATETIME() ELSE NULL END
    FROM dbo.OrdenTrabajoServicioExterno C WHERE C.IdServicioExterno=@IdServicio;
    COMMIT;
END;
GO
