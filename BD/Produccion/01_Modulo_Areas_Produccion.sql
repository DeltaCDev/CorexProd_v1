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

IF OBJECT_ID(N'dbo.AreaProduccion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AreaProduccion
    (
        IdAreaProduccion INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AreaProduccion PRIMARY KEY,
        CodigoArea VARCHAR(20) NOT NULL,
        NombreArea NVARCHAR(100) NOT NULL,
        Descripcion NVARCHAR(500) NOT NULL CONSTRAINT DF_AreaProduccion_Descripcion DEFAULT(N''),
        OrdenSecuencia INT NOT NULL,
        EsInicio BIT NOT NULL CONSTRAINT DF_AreaProduccion_EsInicio DEFAULT(0),
        ManejaMerma BIT NOT NULL CONSTRAINT DF_AreaProduccion_ManejaMerma DEFAULT(0),
        EsTermino BIT NOT NULL CONSTRAINT DF_AreaProduccion_EsTermino DEFAULT(0),
        ModoEnvio VARCHAR(10) NOT NULL,
        Activo BIT NOT NULL CONSTRAINT DF_AreaProduccion_Activo DEFAULT(1),
        UsuarioRegistro INT NOT NULL,
        FechaRegistro DATETIME2(0) NOT NULL CONSTRAINT DF_AreaProduccion_FechaRegistro DEFAULT(SYSDATETIME()),
        UsuarioModificacion INT NULL,
        FechaModificacion DATETIME2(0) NULL,
        CONSTRAINT CK_AreaProduccion_Orden CHECK (OrdenSecuencia > 0),
        CONSTRAINT CK_AreaProduccion_ModoEnvio CHECK (ModoEnvio IN ('UNICO', 'PARCIAL')),
        CONSTRAINT CK_AreaProduccion_InactivoSinExtremos CHECK (Activo = 1 OR (EsInicio = 0 AND EsTermino = 0)),
        CONSTRAINT FK_AreaProduccion_UsuarioRegistro FOREIGN KEY (UsuarioRegistro) REFERENCES dbo.Usuarios(IdUsuario),
        CONSTRAINT FK_AreaProduccion_UsuarioModificacion FOREIGN KEY (UsuarioModificacion) REFERENCES dbo.Usuarios(IdUsuario)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AreaProduccion') AND name = N'UX_AreaProduccion_Codigo')
    CREATE UNIQUE INDEX UX_AreaProduccion_Codigo ON dbo.AreaProduccion(CodigoArea);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AreaProduccion') AND name = N'UX_AreaProduccion_Orden_Activo')
    CREATE UNIQUE INDEX UX_AreaProduccion_Orden_Activo ON dbo.AreaProduccion(OrdenSecuencia) WHERE Activo = 1;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AreaProduccion') AND name = N'UX_AreaProduccion_Inicio_Activo')
    CREATE UNIQUE INDEX UX_AreaProduccion_Inicio_Activo ON dbo.AreaProduccion(EsInicio) WHERE Activo = 1 AND EsInicio = 1;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AreaProduccion') AND name = N'UX_AreaProduccion_Termino_Activo')
    CREATE UNIQUE INDEX UX_AreaProduccion_Termino_Activo ON dbo.AreaProduccion(EsTermino) WHERE Activo = 1 AND EsTermino = 1;
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_AREA_PRODUCCION_LISTAR
AS
BEGIN
    SET NOCOUNT ON;
    SELECT IdAreaProduccion, CodigoArea, NombreArea, Descripcion, OrdenSecuencia,
           EsInicio, ManejaMerma, EsTermino, ModoEnvio, Activo,
           UsuarioRegistro, FechaRegistro, UsuarioModificacion, FechaModificacion
    FROM dbo.AreaProduccion
    ORDER BY Activo DESC, OrdenSecuencia, NombreArea;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_AREA_PRODUCCION_GUARDAR
    @IdAreaProduccion INT,
    @CodigoArea VARCHAR(20),
    @NombreArea NVARCHAR(100),
    @Descripcion NVARCHAR(500),
    @OrdenSecuencia INT,
    @EsInicio BIT,
    @ManejaMerma BIT,
    @EsTermino BIT,
    @ModoEnvio VARCHAR(10),
    @Activo BIT,
    @IdUsuario INT,
    @Mensaje NVARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @CodigoArea = UPPER(LTRIM(RTRIM(ISNULL(@CodigoArea, ''))));
    SET @NombreArea = LTRIM(RTRIM(ISNULL(@NombreArea, N'')));
    SET @Descripcion = LTRIM(RTRIM(ISNULL(@Descripcion, N'')));
    SET @ModoEnvio = UPPER(LTRIM(RTRIM(ISNULL(@ModoEnvio, ''))));

    BEGIN TRY
        SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
        BEGIN TRANSACTION;

        IF @CodigoArea = '' THROW 51000, 'El código del área es obligatorio.', 1;
        IF @NombreArea = N'' THROW 51000, 'El nombre del área es obligatorio.', 1;
        IF @OrdenSecuencia <= 0 THROW 51000, 'El orden de secuencia debe ser mayor que cero.', 1;
        IF @ModoEnvio NOT IN ('UNICO', 'PARCIAL') THROW 51000, 'El modo de envío seleccionado no es válido.', 1;
        IF @Activo = 0 AND @EsInicio = 1 THROW 51000, 'Un área inactiva no puede configurarse como inicio.', 1;
        IF @Activo = 0 AND @EsTermino = 1 THROW 51000, 'Un área inactiva no puede configurarse como término.', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE IdUsuario = @IdUsuario) THROW 51000, 'El usuario de la operación no es válido.', 1;
        IF @IdAreaProduccion > 0 AND NOT EXISTS (SELECT 1 FROM dbo.AreaProduccion WITH (UPDLOCK, HOLDLOCK) WHERE IdAreaProduccion = @IdAreaProduccion)
            THROW 51000, 'El área de producción no existe.', 1;
        IF EXISTS (SELECT 1 FROM dbo.AreaProduccion WITH (UPDLOCK, HOLDLOCK) WHERE CodigoArea = @CodigoArea AND IdAreaProduccion <> @IdAreaProduccion)
            THROW 51000, 'Ya existe un área registrada con el código ingresado.', 1;
        IF @Activo = 1 AND EXISTS (SELECT 1 FROM dbo.AreaProduccion WITH (UPDLOCK, HOLDLOCK) WHERE Activo = 1 AND OrdenSecuencia = @OrdenSecuencia AND IdAreaProduccion <> @IdAreaProduccion)
            THROW 51000, 'Ya existe un área activa con el orden de secuencia indicado.', 1;
        IF @Activo = 1 AND @EsInicio = 1 AND EXISTS (SELECT 1 FROM dbo.AreaProduccion WITH (UPDLOCK, HOLDLOCK) WHERE Activo = 1 AND EsInicio = 1 AND IdAreaProduccion <> @IdAreaProduccion)
            THROW 51000, 'Solo puede existir un área de inicio activa.', 1;
        IF @Activo = 1 AND @EsTermino = 1 AND EXISTS (SELECT 1 FROM dbo.AreaProduccion WITH (UPDLOCK, HOLDLOCK) WHERE Activo = 1 AND EsTermino = 1 AND IdAreaProduccion <> @IdAreaProduccion)
            THROW 51000, 'Solo puede existir un área de término activa.', 1;

        DECLARE @Antes NVARCHAR(1000) = N'';
        DECLARE @Accion NVARCHAR(30);
        IF @IdAreaProduccion = 0
        BEGIN
            INSERT dbo.AreaProduccion
                (CodigoArea, NombreArea, Descripcion, OrdenSecuencia, EsInicio, ManejaMerma, EsTermino, ModoEnvio, Activo, UsuarioRegistro)
            VALUES
                (@CodigoArea, @NombreArea, @Descripcion, @OrdenSecuencia, @EsInicio, @ManejaMerma, @EsTermino, @ModoEnvio, @Activo, @IdUsuario);
            SET @IdAreaProduccion = CONVERT(INT, SCOPE_IDENTITY());
            SET @Accion = N'CREAR';
        END
        ELSE
        BEGIN
            SELECT @Antes = CONCAT(N'Antes: orden=', OrdenSecuencia, N', inicio=', EsInicio, N', término=', EsTermino,
                                   N', merma=', ManejaMerma, N', modo=', ModoEnvio, N', activo=', Activo, N'. ')
            FROM dbo.AreaProduccion WHERE IdAreaProduccion = @IdAreaProduccion;
            UPDATE dbo.AreaProduccion
               SET CodigoArea = @CodigoArea, NombreArea = @NombreArea, Descripcion = @Descripcion,
                   OrdenSecuencia = @OrdenSecuencia, EsInicio = @EsInicio, ManejaMerma = @ManejaMerma,
                   EsTermino = @EsTermino, ModoEnvio = @ModoEnvio, Activo = @Activo,
                   UsuarioModificacion = @IdUsuario, FechaModificacion = SYSDATETIME()
             WHERE IdAreaProduccion = @IdAreaProduccion;
            SET @Accion = N'EDITAR';
        END;

        IF EXISTS
        (
            SELECT 1 FROM dbo.AreaProduccion a
            WHERE a.Activo = 1 AND a.EsInicio = 1
              AND a.OrdenSecuencia <> (SELECT MIN(OrdenSecuencia) FROM dbo.AreaProduccion WHERE Activo = 1)
        ) THROW 51000, 'El área de inicio debe tener el menor orden de secuencia.', 1;
        IF EXISTS
        (
            SELECT 1 FROM dbo.AreaProduccion a
            WHERE a.Activo = 1 AND a.EsTermino = 1
              AND a.OrdenSecuencia <> (SELECT MAX(OrdenSecuencia) FROM dbo.AreaProduccion WHERE Activo = 1)
        ) THROW 51000, 'El área de término debe tener el mayor orden de secuencia.', 1;

        INSERT dbo.Auditoria(Usuario, Accion, Modulo, Descripcion, Equipo)
        VALUES (CONVERT(NVARCHAR(100), @IdUsuario), @Accion, N'Áreas de Producción',
                CONCAT(@Antes, N'Área ', @CodigoArea, N': orden=', @OrdenSecuencia, N', inicio=', @EsInicio,
                       N', término=', @EsTermino, N', merma=', @ManejaMerma, N', modo=', @ModoEnvio, N', activo=', @Activo, N'.'),
                HOST_NAME());

        COMMIT TRANSACTION;
        SET @Mensaje = CASE WHEN @Accion = N'CREAR' THEN N'OK|Área de producción registrada correctamente.' ELSE N'OK|Área de producción actualizada correctamente.' END;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_AREA_PRODUCCION_CAMBIAR_ESTADO
    @IdAreaProduccion INT,
    @Activo BIT,
    @IdUsuario INT,
    @Mensaje NVARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
        BEGIN TRANSACTION;
        DECLARE @Codigo VARCHAR(20), @Orden INT, @EsInicio BIT, @EsTermino BIT, @EstadoActual BIT;
        SELECT @Codigo = CodigoArea, @Orden = OrdenSecuencia, @EsInicio = EsInicio, @EsTermino = EsTermino, @EstadoActual = Activo
        FROM dbo.AreaProduccion WITH (UPDLOCK, HOLDLOCK) WHERE IdAreaProduccion = @IdAreaProduccion;

        IF @Codigo IS NULL THROW 51000, 'El área de producción no existe.', 1;
        IF @EstadoActual = @Activo THROW 51000, 'El área ya se encuentra en el estado solicitado.', 1;
        IF @Activo = 0 AND @EsInicio = 1 THROW 51000, 'No se puede desactivar el área de inicio sin configurar otra área.', 1;
        IF @Activo = 0 AND @EsTermino = 1 THROW 51000, 'No se puede desactivar el área de término sin configurar otra área.', 1;
        IF @Activo = 1 AND EXISTS (SELECT 1 FROM dbo.AreaProduccion WITH (UPDLOCK, HOLDLOCK) WHERE Activo = 1 AND OrdenSecuencia = @Orden AND IdAreaProduccion <> @IdAreaProduccion)
            THROW 51000, 'Ya existe un área activa con el orden de secuencia indicado.', 1;

        UPDATE dbo.AreaProduccion
           SET Activo = @Activo, UsuarioModificacion = @IdUsuario, FechaModificacion = SYSDATETIME()
         WHERE IdAreaProduccion = @IdAreaProduccion;

        IF EXISTS (SELECT 1 FROM dbo.AreaProduccion WHERE Activo = 1 AND EsInicio = 1 AND OrdenSecuencia <> (SELECT MIN(OrdenSecuencia) FROM dbo.AreaProduccion WHERE Activo = 1))
            THROW 51000, 'La activación haría que el área de inicio deje de tener la menor secuencia.', 1;
        IF EXISTS (SELECT 1 FROM dbo.AreaProduccion WHERE Activo = 1 AND EsTermino = 1 AND OrdenSecuencia <> (SELECT MAX(OrdenSecuencia) FROM dbo.AreaProduccion WHERE Activo = 1))
            THROW 51000, 'La activación haría que el área de término deje de tener la mayor secuencia.', 1;

        INSERT dbo.Auditoria(Usuario, Accion, Modulo, Descripcion, Equipo)
        VALUES (CONVERT(NVARCHAR(100), @IdUsuario), CASE WHEN @Activo = 1 THEN N'ACTIVAR' ELSE N'DESACTIVAR' END,
                N'Áreas de Producción', CONCAT(N'Área ', @Codigo, CASE WHEN @Activo = 1 THEN N' activada.' ELSE N' desactivada.' END), HOST_NAME());
        COMMIT TRANSACTION;
        SET @Mensaje = CASE WHEN @Activo = 1 THEN N'OK|Área de producción activada correctamente.' ELSE N'OK|Área de producción desactivada correctamente.' END;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH;
END;
GO

-- La entrada ya existe en instalaciones recientes; este bloque mantiene compatibilidad con bases anteriores.
IF NOT EXISTS (SELECT 1 FROM dbo.Menu WHERE NombreMenu = N'Áreas de Producción')
BEGIN
    DECLARE @IdProduccion INT = (SELECT TOP (1) IdMenu FROM dbo.Menu WHERE NombreMenu = N'Producción' AND IdMenuPadre IS NULL);
    IF @IdProduccion IS NOT NULL
        INSERT dbo.Menu(NombreMenu, IdMenuPadre, Orden, Estado) VALUES(N'Áreas de Producción', @IdProduccion, 1, 1);
END;
GO
