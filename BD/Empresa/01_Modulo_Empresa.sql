IF OBJECT_ID('dbo.Empresas', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Empresas
    (
        IdEmpresa INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Ruc VARCHAR(11) NOT NULL,
        Nombre VARCHAR(200) NOT NULL,
        NombreComercial VARCHAR(200) NOT NULL,
        Telefono VARCHAR(30) NOT NULL CONSTRAINT DF_Empresas_Telefono DEFAULT(''),
        Correo VARCHAR(120) NOT NULL CONSTRAINT DF_Empresas_Correo DEFAULT(''),
        Departamento VARCHAR(80) NOT NULL CONSTRAINT DF_Empresas_Departamento DEFAULT(''),
        Provincia VARCHAR(80) NOT NULL CONSTRAINT DF_Empresas_Provincia DEFAULT(''),
        Distrito VARCHAR(80) NOT NULL CONSTRAINT DF_Empresas_Distrito DEFAULT(''),
        Direccion VARCHAR(250) NOT NULL CONSTRAINT DF_Empresas_Direccion DEFAULT(''),
        Logo VARBINARY(MAX) NULL,
        CodigoCliente VARCHAR(80) NOT NULL CONSTRAINT DF_Empresas_CodigoCliente DEFAULT(''),
        LicenciaActivacion VARCHAR(500) NOT NULL CONSTRAINT DF_Empresas_LicenciaActivacion DEFAULT(''),
        EsPredeterminada BIT NOT NULL CONSTRAINT DF_Empresas_EsPredeterminada DEFAULT(0),
        Estado BIT NOT NULL CONSTRAINT DF_Empresas_Estado DEFAULT(1),
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_Empresas_FechaRegistro DEFAULT(GETDATE())
    );
END
GO

IF COL_LENGTH('dbo.Empresas', 'Logo') IS NOT NULL
AND EXISTS
(
    SELECT 1
    FROM sys.columns C
    INNER JOIN sys.types T ON T.user_type_id = C.user_type_id
    WHERE C.object_id = OBJECT_ID('dbo.Empresas')
    AND C.name = 'Logo'
    AND T.name <> 'varbinary'
)
BEGIN
    IF COL_LENGTH('dbo.Empresas', 'LogoRutaAnterior') IS NULL
    BEGIN
        EXEC sp_rename 'dbo.Empresas.Logo', 'LogoRutaAnterior', 'COLUMN';
    END
END
GO

IF COL_LENGTH('dbo.Empresas', 'Logo') IS NULL
BEGIN
    ALTER TABLE dbo.Empresas
    ADD Logo VARBINARY(MAX) NULL;
END
GO

IF COL_LENGTH('dbo.Empresas', 'LogoRutaAnterior') IS NOT NULL
BEGIN
    DECLARE @ConstraintLogoRuta SYSNAME;

    SELECT @ConstraintLogoRuta = DC.name
    FROM sys.default_constraints DC
    INNER JOIN sys.columns C ON C.default_object_id = DC.object_id
    WHERE DC.parent_object_id = OBJECT_ID('dbo.Empresas')
    AND C.name = 'LogoRutaAnterior';

    IF @ConstraintLogoRuta IS NOT NULL
    BEGIN
        DECLARE @SqlDropLogoRuta NVARCHAR(MAX) = N'ALTER TABLE dbo.Empresas DROP CONSTRAINT ' + QUOTENAME(@ConstraintLogoRuta);
        EXEC sp_executesql @SqlDropLogoRuta;
    END

    ALTER TABLE dbo.Empresas
    DROP COLUMN LogoRutaAnterior;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Empresas_Ruc' AND object_id = OBJECT_ID('dbo.Empresas'))
BEGIN
    CREATE UNIQUE INDEX UQ_Empresas_Ruc ON dbo.Empresas(Ruc);
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_SEG_EMPRESA_LISTAR
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        IdEmpresa,
        Ruc,
        Nombre,
        NombreComercial,
        Telefono,
        Correo,
        Departamento,
        Provincia,
        Distrito,
        Direccion,
        Logo,
        CodigoCliente,
        LicenciaActivacion,
        EsPredeterminada,
        Estado,
        FechaRegistro
    FROM dbo.Empresas
    ORDER BY EsPredeterminada DESC, Nombre ASC;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_SEG_EMPRESA_OBTENER_PREDETERMINADA
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        IdEmpresa,
        Ruc,
        Nombre,
        NombreComercial,
        Telefono,
        Correo,
        Departamento,
        Provincia,
        Distrito,
        Direccion,
        Logo,
        CodigoCliente,
        LicenciaActivacion,
        EsPredeterminada,
        Estado,
        FechaRegistro
    FROM dbo.Empresas
    WHERE Estado = 1
    ORDER BY EsPredeterminada DESC, IdEmpresa ASC;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_SEG_EMPRESA_REGISTRAR
    @Ruc VARCHAR(11),
    @Nombre VARCHAR(200),
    @NombreComercial VARCHAR(200),
    @Telefono VARCHAR(30),
    @Correo VARCHAR(120),
    @Departamento VARCHAR(80),
    @Provincia VARCHAR(80),
    @Distrito VARCHAR(80),
    @Direccion VARCHAR(250),
    @Logo VARBINARY(MAX),
    @CodigoCliente VARCHAR(80),
    @LicenciaActivacion VARCHAR(500),
    @EsPredeterminada BIT,
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.Empresas WHERE Ruc = @Ruc)
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'Ya existe una empresa con el RUC indicado';
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.Empresas WHERE Estado = 1)
    BEGIN
        SET @EsPredeterminada = 1;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        IF @EsPredeterminada = 1
        BEGIN
            UPDATE dbo.Empresas
            SET EsPredeterminada = 0;
        END

        INSERT INTO dbo.Empresas
        (
            Ruc,
            Nombre,
            NombreComercial,
            Telefono,
            Correo,
            Departamento,
            Provincia,
            Distrito,
            Direccion,
            Logo,
            CodigoCliente,
            LicenciaActivacion,
            EsPredeterminada,
            Estado
        )
        VALUES
        (
            @Ruc,
            @Nombre,
            @NombreComercial,
            @Telefono,
            @Correo,
            @Departamento,
            @Provincia,
            @Distrito,
            @Direccion,
            @Logo,
            @CodigoCliente,
            @LicenciaActivacion,
            @EsPredeterminada,
            1
        );

        COMMIT TRANSACTION;

        SET @Resultado = 1;
        SET @Mensaje = 'Empresa registrada correctamente';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_SEG_EMPRESA_EDITAR
    @IdEmpresa INT,
    @Ruc VARCHAR(11),
    @Nombre VARCHAR(200),
    @NombreComercial VARCHAR(200),
    @Telefono VARCHAR(30),
    @Correo VARCHAR(120),
    @Departamento VARCHAR(80),
    @Provincia VARCHAR(80),
    @Distrito VARCHAR(80),
    @Direccion VARCHAR(250),
    @Logo VARBINARY(MAX),
    @CodigoCliente VARCHAR(80),
    @LicenciaActivacion VARCHAR(500),
    @EsPredeterminada BIT,
    @Estado BIT,
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Empresas WHERE IdEmpresa = @IdEmpresa)
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'No se encontro la empresa';
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.Empresas WHERE Ruc = @Ruc AND IdEmpresa <> @IdEmpresa)
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'Ya existe una empresa con el RUC indicado';
        RETURN;
    END

    IF @EsPredeterminada = 0
    AND EXISTS (SELECT 1 FROM dbo.Empresas WHERE IdEmpresa = @IdEmpresa AND EsPredeterminada = 1)
    AND NOT EXISTS (SELECT 1 FROM dbo.Empresas WHERE IdEmpresa <> @IdEmpresa AND Estado = 1)
    BEGIN
        SET @EsPredeterminada = 1;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        IF @EsPredeterminada = 1
        BEGIN
            UPDATE dbo.Empresas
            SET EsPredeterminada = 0
            WHERE IdEmpresa <> @IdEmpresa;
        END

        UPDATE dbo.Empresas
        SET
            Ruc = @Ruc,
            Nombre = @Nombre,
            NombreComercial = @NombreComercial,
            Telefono = @Telefono,
            Correo = @Correo,
            Departamento = @Departamento,
            Provincia = @Provincia,
            Distrito = @Distrito,
            Direccion = @Direccion,
            Logo = @Logo,
            CodigoCliente = @CodigoCliente,
            LicenciaActivacion = @LicenciaActivacion,
            EsPredeterminada = @EsPredeterminada,
            Estado = @Estado
        WHERE IdEmpresa = @IdEmpresa;

        IF NOT EXISTS (SELECT 1 FROM dbo.Empresas WHERE Estado = 1 AND EsPredeterminada = 1)
        BEGIN
            ;WITH SiguienteEmpresa AS
            (
                SELECT TOP (1) IdEmpresa
                FROM dbo.Empresas
                WHERE Estado = 1
                ORDER BY IdEmpresa ASC
            )
            UPDATE E
            SET EsPredeterminada = 1
            FROM dbo.Empresas E
            INNER JOIN SiguienteEmpresa S ON S.IdEmpresa = E.IdEmpresa;
        END

        COMMIT TRANSACTION;

        SET @Resultado = 1;
        SET @Mensaje = 'Empresa actualizada correctamente';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_SEG_EMPRESA_ELIMINAR
    @IdEmpresa INT,
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Empresas WHERE IdEmpresa = @IdEmpresa)
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'No se encontro la empresa';
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @EraPredeterminada BIT;

        SELECT @EraPredeterminada = EsPredeterminada
        FROM dbo.Empresas
        WHERE IdEmpresa = @IdEmpresa;

        DELETE FROM dbo.Empresas
        WHERE IdEmpresa = @IdEmpresa;

        IF @EraPredeterminada = 1
        AND EXISTS (SELECT 1 FROM dbo.Empresas WHERE Estado = 1)
        BEGIN
            ;WITH SiguienteEmpresa AS
            (
                SELECT TOP (1) IdEmpresa
                FROM dbo.Empresas
                WHERE Estado = 1
                ORDER BY IdEmpresa ASC
            )
            UPDATE E
            SET EsPredeterminada = 1
            FROM dbo.Empresas E
            INNER JOIN SiguienteEmpresa S ON S.IdEmpresa = E.IdEmpresa;
        END

        COMMIT TRANSACTION;

        SET @Resultado = 1;
        SET @Mensaje = 'Empresa eliminada correctamente';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH
END
GO

DECLARE @IdMenuSeguridad INT;

IF NOT EXISTS (SELECT 1 FROM dbo.Menu WHERE NombreMenu = 'Seguridad' AND IdMenuPadre IS NULL)
BEGIN
    INSERT INTO dbo.Menu(NombreMenu, IdMenuPadre, Orden, Estado)
    VALUES ('Seguridad', NULL, 7, 1);
END

SELECT @IdMenuSeguridad = IdMenu
FROM dbo.Menu
WHERE NombreMenu = 'Seguridad'
AND IdMenuPadre IS NULL;

IF @IdMenuSeguridad IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM dbo.Menu WHERE NombreMenu = 'Empresa' AND IdMenuPadre = @IdMenuSeguridad)
BEGIN
    INSERT INTO dbo.Menu(NombreMenu, IdMenuPadre, Orden, Estado)
    VALUES ('Empresa', @IdMenuSeguridad, 4, 1);
END

INSERT INTO dbo.PermisosMenu(IdRol, IdMenu, PuedeVer)
SELECT R.IdRol, M.IdMenu, 1
FROM dbo.Roles R
CROSS JOIN dbo.Menu M
WHERE M.NombreMenu IN ('Seguridad', 'Empresa')
AND NOT EXISTS
(
    SELECT 1
    FROM dbo.PermisosMenu PM
    WHERE PM.IdRol = R.IdRol
    AND PM.IdMenu = M.IdMenu
);
GO
