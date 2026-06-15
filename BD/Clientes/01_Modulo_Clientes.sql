USE [CorexProdDB]
GO

IF OBJECT_ID('dbo.Clientes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Clientes
    (
        IdCliente INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        TipoDocumento VARCHAR(30) NOT NULL,
        NumeroDocumento VARCHAR(20) NOT NULL,
        NombreRazonSocial VARCHAR(200) NOT NULL,
        Direccion VARCHAR(200) NULL,
        Telefono VARCHAR(20) NULL,
        Correo VARCHAR(120) NULL,
        Estado BIT NOT NULL,
        FechaRegistro DATETIME NOT NULL
    );
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_SEG_CLIENTE_LISTAR
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        IdCliente,
        TipoDocumento,
        NumeroDocumento,
        NombreRazonSocial,
        Direccion,
        Telefono,
        Correo,
        Estado,
        FechaRegistro
    FROM dbo.Clientes
    ORDER BY NombreRazonSocial ASC;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_SEG_CLIENTE_REGISTRAR
(
    @TipoDocumento VARCHAR(30),
    @NumeroDocumento VARCHAR(20),
    @NombreRazonSocial VARCHAR(200),
    @Direccion VARCHAR(200),
    @Telefono VARCHAR(20),
    @Correo VARCHAR(120),
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    IF @TipoDocumento <> 'S/N'
    AND EXISTS
    (
        SELECT 1
        FROM dbo.Clientes
        WHERE TipoDocumento = @TipoDocumento
        AND NumeroDocumento = @NumeroDocumento
    )
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'Ya existe un cliente con ese documento';
        RETURN;
    END

    INSERT INTO dbo.Clientes
    (
        TipoDocumento,
        NumeroDocumento,
        NombreRazonSocial,
        Direccion,
        Telefono,
        Correo,
        Estado,
        FechaRegistro
    )
    VALUES
    (
        @TipoDocumento,
        @NumeroDocumento,
        @NombreRazonSocial,
        @Direccion,
        @Telefono,
        @Correo,
        1,
        GETDATE()
    );

    SET @Resultado = 1;
    SET @Mensaje = 'Cliente registrado correctamente';
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_SEG_CLIENTE_EDITAR
(
    @IdCliente INT,
    @TipoDocumento VARCHAR(30),
    @NumeroDocumento VARCHAR(20),
    @NombreRazonSocial VARCHAR(200),
    @Direccion VARCHAR(200),
    @Telefono VARCHAR(20),
    @Correo VARCHAR(120),
    @Estado BIT,
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    IF @TipoDocumento <> 'S/N'
    AND EXISTS
    (
        SELECT 1
        FROM dbo.Clientes
        WHERE TipoDocumento = @TipoDocumento
        AND NumeroDocumento = @NumeroDocumento
        AND IdCliente <> @IdCliente
    )
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'Ya existe otro cliente con ese documento';
        RETURN;
    END

    UPDATE dbo.Clientes
    SET
        TipoDocumento = @TipoDocumento,
        NumeroDocumento = @NumeroDocumento,
        NombreRazonSocial = @NombreRazonSocial,
        Direccion = @Direccion,
        Telefono = @Telefono,
        Correo = @Correo,
        Estado = @Estado
    WHERE IdCliente = @IdCliente;

    SET @Resultado = 1;
    SET @Mensaje = 'Cliente actualizado correctamente';
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_SEG_CLIENTE_ELIMINAR
(
    @IdCliente INT,
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.Clientes
    WHERE IdCliente = @IdCliente;

    SET @Resultado = 1;
    SET @Mensaje = 'Cliente eliminado correctamente';
END
GO

DECLARE @IdMenuSeguridad INT;

SELECT @IdMenuSeguridad = IdMenu
FROM dbo.Menu
WHERE NombreMenu = 'Seguridad'
AND IdMenuPadre IS NULL;

IF @IdMenuSeguridad IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM dbo.Menu WHERE NombreMenu = 'Clientes' AND IdMenuPadre = @IdMenuSeguridad)
BEGIN
    INSERT INTO dbo.Menu(NombreMenu, IdMenuPadre, Orden, Estado)
    VALUES ('Clientes', @IdMenuSeguridad, 5, 1);
END
GO

DECLARE @IdMenuClientes INT;
DECLARE @IdMenuSeguridadPermiso INT;

SELECT @IdMenuClientes = IdMenu
FROM dbo.Menu
WHERE NombreMenu = 'Clientes';

SELECT @IdMenuSeguridadPermiso = IdMenu
FROM dbo.Menu
WHERE NombreMenu = 'Seguridad'
AND IdMenuPadre IS NULL;

IF @IdMenuClientes IS NOT NULL
AND @IdMenuSeguridadPermiso IS NOT NULL
BEGIN
    INSERT INTO dbo.PermisosMenu(IdRol, IdMenu, PuedeVer)
    SELECT PM.IdRol, @IdMenuClientes, 1
    FROM dbo.PermisosMenu PM
    WHERE PM.IdMenu = @IdMenuSeguridadPermiso
    AND PM.PuedeVer = 1
    AND NOT EXISTS
    (
        SELECT 1
        FROM dbo.PermisosMenu PX
        WHERE PX.IdRol = PM.IdRol
        AND PX.IdMenu = @IdMenuClientes
    );

    UPDATE PM
    SET PuedeVer = 1
    FROM dbo.PermisosMenu PM
    WHERE PM.IdMenu = @IdMenuClientes
    AND EXISTS
    (
        SELECT 1
        FROM dbo.PermisosMenu PS
        WHERE PS.IdRol = PM.IdRol
        AND PS.IdMenu = @IdMenuSeguridadPermiso
        AND PS.PuedeVer = 1
    );
END
GO
