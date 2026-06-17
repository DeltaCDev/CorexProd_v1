USE CorexProdDB;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.Proveedores', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Proveedores
    (
        IdProveedor INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Proveedores PRIMARY KEY,
        TipoDocumento VARCHAR(30) NOT NULL,
        NumeroDocumento VARCHAR(20) NOT NULL,
        NombreRazonSocial VARCHAR(180) NOT NULL,
        Direccion VARCHAR(200) NULL,
        Telefono VARCHAR(30) NULL,
        Correo VARCHAR(120) NULL,
        Estado BIT NOT NULL CONSTRAINT DF_Proveedores_Estado DEFAULT (1),
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_Proveedores_FechaRegistro DEFAULT (GETDATE())
    );
END
GO

DECLARE @IdSeguridadPadre INT = (SELECT TOP (1) IdMenu FROM dbo.Menu WHERE NombreMenu = 'Seguridad' AND IdMenuPadre IS NULL ORDER BY IdMenu);
IF @IdSeguridadPadre IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.Menu WHERE NombreMenu = 'Proveedores' AND IdMenuPadre = @IdSeguridadPadre)
BEGIN
    INSERT INTO dbo.Menu (NombreMenu, IdMenuPadre, Orden, Estado)
    VALUES ('Proveedores', @IdSeguridadPadre, 8, 1);
END

INSERT INTO dbo.PermisosMenu (IdRol, IdMenu, PuedeVer)
SELECT R.IdRol, M.IdMenu, 1
FROM dbo.Roles R
CROSS JOIN dbo.Menu M
WHERE M.NombreMenu = 'Proveedores'
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.PermisosMenu PM
      WHERE PM.IdRol = R.IdRol AND PM.IdMenu = M.IdMenu
  );
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_PROVEEDOR_LISTAR
AS
BEGIN
    SET NOCOUNT ON;

    SELECT IdProveedor, TipoDocumento, NumeroDocumento, NombreRazonSocial, Direccion, Telefono, Correo, Estado
    FROM dbo.Proveedores
    ORDER BY NombreRazonSocial;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_PROVEEDOR_REGISTRAR
    @TipoDocumento VARCHAR(30),
    @NumeroDocumento VARCHAR(20),
    @NombreRazonSocial VARCHAR(180),
    @Direccion VARCHAR(200) = NULL,
    @Telefono VARCHAR(30) = NULL,
    @Correo VARCHAR(120) = NULL,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF NULLIF(LTRIM(RTRIM(@NombreRazonSocial)), '') IS NULL
    BEGIN
        SET @Mensaje = 'El nombre o razon social es obligatorio';
        RETURN;
    END

    IF ISNULL(@NumeroDocumento, '') <> ''
       AND EXISTS (SELECT 1 FROM dbo.Proveedores WHERE TipoDocumento = @TipoDocumento AND NumeroDocumento = @NumeroDocumento)
    BEGIN
        SET @Mensaje = 'Ya existe un proveedor con el mismo documento';
        RETURN;
    END

    INSERT INTO dbo.Proveedores
    (
        TipoDocumento, NumeroDocumento, NombreRazonSocial,
        Direccion, Telefono, Correo, Estado, FechaRegistro
    )
    VALUES
    (
        @TipoDocumento, ISNULL(@NumeroDocumento, ''), @NombreRazonSocial,
        @Direccion, @Telefono, @Correo, 1, GETDATE()
    );

    SET @Mensaje = 'Proveedor registrado correctamente';
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_PROVEEDOR_EDITAR
    @IdProveedor INT,
    @TipoDocumento VARCHAR(30),
    @NumeroDocumento VARCHAR(20),
    @NombreRazonSocial VARCHAR(180),
    @Direccion VARCHAR(200) = NULL,
    @Telefono VARCHAR(30) = NULL,
    @Correo VARCHAR(120) = NULL,
    @Estado BIT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Proveedores WHERE IdProveedor = @IdProveedor)
    BEGIN
        SET @Mensaje = 'El proveedor no existe';
        RETURN;
    END

    IF NULLIF(LTRIM(RTRIM(@NombreRazonSocial)), '') IS NULL
    BEGIN
        SET @Mensaje = 'El nombre o razon social es obligatorio';
        RETURN;
    END

    IF ISNULL(@NumeroDocumento, '') <> ''
       AND EXISTS
       (
           SELECT 1
           FROM dbo.Proveedores
           WHERE TipoDocumento = @TipoDocumento
             AND NumeroDocumento = @NumeroDocumento
             AND IdProveedor <> @IdProveedor
       )
    BEGIN
        SET @Mensaje = 'Ya existe un proveedor con el mismo documento';
        RETURN;
    END

    UPDATE dbo.Proveedores
    SET TipoDocumento = @TipoDocumento,
        NumeroDocumento = ISNULL(@NumeroDocumento, ''),
        NombreRazonSocial = @NombreRazonSocial,
        Direccion = @Direccion,
        Telefono = @Telefono,
        Correo = @Correo,
        Estado = @Estado
    WHERE IdProveedor = @IdProveedor;

    SET @Mensaje = 'Proveedor actualizado correctamente';
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_PROVEEDOR_ELIMINAR
    @IdProveedor INT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Proveedores WHERE IdProveedor = @IdProveedor)
    BEGIN
        SET @Mensaje = 'El proveedor no existe';
        RETURN;
    END

    IF OBJECT_ID('dbo.IngresosManualesStock', 'U') IS NOT NULL
       AND EXISTS (SELECT 1 FROM dbo.IngresosManualesStock WHERE IdProveedor = @IdProveedor)
    BEGIN
        UPDATE dbo.Proveedores
        SET Estado = 0
        WHERE IdProveedor = @IdProveedor;

        SET @Mensaje = 'Proveedor desactivado correctamente';
        RETURN;
    END

    DELETE FROM dbo.Proveedores
    WHERE IdProveedor = @IdProveedor;

    SET @Mensaje = 'Proveedor eliminado correctamente';
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_PROVEEDOR_REGISTRAR_RAPIDO
    @TipoDocumento VARCHAR(30),
    @NumeroDocumento VARCHAR(20),
    @NombreRazonSocial VARCHAR(180),
    @Direccion VARCHAR(200) = NULL,
    @Telefono VARCHAR(30) = NULL,
    @Correo VARCHAR(120) = NULL,
    @IdProveedor INT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @IdProveedor = 0;

    IF NULLIF(LTRIM(RTRIM(@NombreRazonSocial)), '') IS NULL
    BEGIN
        SET @Mensaje = 'Debe ingresar el nombre o razon social del proveedor';
        RETURN;
    END

    SELECT @IdProveedor = IdProveedor
    FROM dbo.Proveedores
    WHERE TipoDocumento = @TipoDocumento
      AND NumeroDocumento = ISNULL(@NumeroDocumento, '')
      AND NumeroDocumento <> '';

    IF @IdProveedor > 0
    BEGIN
        UPDATE dbo.Proveedores
        SET Estado = 1
        WHERE IdProveedor = @IdProveedor;

        SET @Mensaje = 'El proveedor ya existia y fue seleccionado';
        RETURN;
    END

    INSERT INTO dbo.Proveedores (TipoDocumento, NumeroDocumento, NombreRazonSocial, Direccion, Telefono, Correo)
    VALUES (@TipoDocumento, ISNULL(@NumeroDocumento, ''), @NombreRazonSocial, @Direccion, @Telefono, @Correo);

    SET @IdProveedor = SCOPE_IDENTITY();
    SET @Mensaje = 'Proveedor registrado correctamente';
END
GO
