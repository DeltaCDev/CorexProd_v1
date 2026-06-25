/*
    CorexProd - Supercategorías de productos

    Estructura:
        Supercategoría -> Categoría -> Producto

    Compatibilidad:
        - Las categorías existentes se asignan a la supercategoría GENERAL.
        - Los productos existentes toman la supercategoría desde su categoría.
        - El producto almacena IdSuperCategoriaProducto para consultas rápidas.
*/

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.SuperCategoriasProducto', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SuperCategoriasProducto
    (
        IdSuperCategoriaProducto INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SuperCategoriasProducto PRIMARY KEY,
        NombreSuperCategoria VARCHAR(100) NOT NULL,
        Descripcion VARCHAR(250) NULL,
        Estado BIT NOT NULL CONSTRAINT DF_SuperCategoriasProducto_Estado DEFAULT (1),
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_SuperCategoriasProducto_FechaRegistro DEFAULT (GETDATE()),
        CONSTRAINT UQ_SuperCategoriasProducto_Nombre UNIQUE (NombreSuperCategoria)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SuperCategoriasProducto WHERE NombreSuperCategoria = 'GENERAL')
BEGIN
    INSERT INTO dbo.SuperCategoriasProducto (NombreSuperCategoria, Descripcion, Estado, FechaRegistro)
    VALUES ('GENERAL', 'Supercategoria general para categorias existentes.', 1, GETDATE());
END
GO

DECLARE @IdSuperCategoriaGeneral INT =
(
    SELECT TOP 1 IdSuperCategoriaProducto
    FROM dbo.SuperCategoriasProducto
    WHERE NombreSuperCategoria = 'GENERAL'
    ORDER BY IdSuperCategoriaProducto
);

IF COL_LENGTH('dbo.CategoriasProducto', 'IdSuperCategoriaProducto') IS NULL
BEGIN
    EXEC('ALTER TABLE dbo.CategoriasProducto ADD IdSuperCategoriaProducto INT NULL;');
END

EXEC sp_executesql
    N'UPDATE dbo.CategoriasProducto
      SET IdSuperCategoriaProducto = @IdSuperCategoriaGeneral
      WHERE IdSuperCategoriaProducto IS NULL;',
    N'@IdSuperCategoriaGeneral INT',
    @IdSuperCategoriaGeneral = @IdSuperCategoriaGeneral;

IF EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.CategoriasProducto')
      AND name = 'IdSuperCategoriaProducto'
      AND is_nullable = 1
)
BEGIN
    EXEC('ALTER TABLE dbo.CategoriasProducto ALTER COLUMN IdSuperCategoriaProducto INT NOT NULL;');
END

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_CategoriasProducto_SuperCategoriasProducto')
BEGIN
    EXEC('ALTER TABLE dbo.CategoriasProducto WITH CHECK
          ADD CONSTRAINT FK_CategoriasProducto_SuperCategoriasProducto
          FOREIGN KEY (IdSuperCategoriaProducto) REFERENCES dbo.SuperCategoriasProducto(IdSuperCategoriaProducto);');
END

IF COL_LENGTH('dbo.Productos', 'IdSuperCategoriaProducto') IS NULL
BEGIN
    EXEC('ALTER TABLE dbo.Productos ADD IdSuperCategoriaProducto INT NULL;');
END

EXEC('UPDATE P
      SET P.IdSuperCategoriaProducto = C.IdSuperCategoriaProducto
      FROM dbo.Productos P
      INNER JOIN dbo.CategoriasProducto C ON C.IdCategoriaProducto = P.IdCategoriaProducto
      WHERE P.IdSuperCategoriaProducto IS NULL
         OR P.IdSuperCategoriaProducto <> C.IdSuperCategoriaProducto;');

EXEC sp_executesql
    N'UPDATE dbo.Productos
      SET IdSuperCategoriaProducto = @IdSuperCategoriaGeneral
      WHERE IdSuperCategoriaProducto IS NULL;',
    N'@IdSuperCategoriaGeneral INT',
    @IdSuperCategoriaGeneral = @IdSuperCategoriaGeneral;

IF EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Productos')
      AND name = 'IdSuperCategoriaProducto'
      AND is_nullable = 1
)
BEGIN
    EXEC('ALTER TABLE dbo.Productos ALTER COLUMN IdSuperCategoriaProducto INT NOT NULL;');
END

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Productos_SuperCategoriasProducto')
BEGIN
    EXEC('ALTER TABLE dbo.Productos WITH CHECK
          ADD CONSTRAINT FK_Productos_SuperCategoriasProducto
          FOREIGN KEY (IdSuperCategoriaProducto) REFERENCES dbo.SuperCategoriasProducto(IdSuperCategoriaProducto);');
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_SUPER_CATEGORIA_PRODUCTO_LISTAR
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        IdSuperCategoriaProducto,
        NombreSuperCategoria,
        Descripcion,
        Estado,
        FechaRegistro
    FROM dbo.SuperCategoriasProducto
    ORDER BY NombreSuperCategoria ASC;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_SUPER_CATEGORIA_PRODUCTO_REGISTRAR
(
    @NombreSuperCategoria VARCHAR(100),
    @Descripcion VARCHAR(250),
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.SuperCategoriasProducto WHERE NombreSuperCategoria = @NombreSuperCategoria)
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'La supercategoría ya existe.';
        RETURN;
    END

    INSERT INTO dbo.SuperCategoriasProducto (NombreSuperCategoria, Descripcion, Estado, FechaRegistro)
    VALUES (@NombreSuperCategoria, @Descripcion, 1, GETDATE());

    SET @Resultado = 1;
    SET @Mensaje = 'Supercategoría registrada correctamente.';
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_SUPER_CATEGORIA_PRODUCTO_EDITAR
(
    @IdSuperCategoriaProducto INT,
    @NombreSuperCategoria VARCHAR(100),
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
        FROM dbo.SuperCategoriasProducto
        WHERE NombreSuperCategoria = @NombreSuperCategoria
          AND IdSuperCategoriaProducto <> @IdSuperCategoriaProducto
    )
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'Ya existe otra supercategoría con ese nombre.';
        RETURN;
    END

    UPDATE dbo.SuperCategoriasProducto
    SET NombreSuperCategoria = @NombreSuperCategoria,
        Descripcion = @Descripcion,
        Estado = @Estado
    WHERE IdSuperCategoriaProducto = @IdSuperCategoriaProducto;

    SET @Resultado = 1;
    SET @Mensaje = 'Supercategoría actualizada correctamente.';
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_SUPER_CATEGORIA_PRODUCTO_ELIMINAR
(
    @IdSuperCategoriaProducto INT,
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.CategoriasProducto WHERE IdSuperCategoriaProducto = @IdSuperCategoriaProducto)
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'No se puede eliminar: existen categorías asociadas.';
        RETURN;
    END

    DELETE FROM dbo.SuperCategoriasProducto
    WHERE IdSuperCategoriaProducto = @IdSuperCategoriaProducto;

    SET @Resultado = 1;
    SET @Mensaje = 'Supercategoría eliminada correctamente.';
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_CATEGORIA_PRODUCTO_LISTAR
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        C.IdCategoriaProducto,
        C.IdSuperCategoriaProducto,
        S.NombreSuperCategoria,
        C.NombreCategoria,
        C.Descripcion,
        C.Estado,
        C.FechaRegistro
    FROM dbo.CategoriasProducto C
    INNER JOIN dbo.SuperCategoriasProducto S
        ON S.IdSuperCategoriaProducto = C.IdSuperCategoriaProducto
    ORDER BY S.NombreSuperCategoria ASC, C.NombreCategoria ASC;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_CATEGORIA_PRODUCTO_REGISTRAR
(
    @NombreCategoria VARCHAR(100),
    @Descripcion VARCHAR(250),
    @IdSuperCategoriaProducto INT,
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.SuperCategoriasProducto WHERE IdSuperCategoriaProducto = @IdSuperCategoriaProducto AND Estado = 1)
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'Debe seleccionar una supercategoría activa.';
        RETURN;
    END

    IF EXISTS
    (
        SELECT 1
        FROM dbo.CategoriasProducto
        WHERE NombreCategoria = @NombreCategoria
          AND IdSuperCategoriaProducto = @IdSuperCategoriaProducto
    )
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'La categoría ya existe en esa supercategoría.';
        RETURN;
    END

    INSERT INTO dbo.CategoriasProducto
    (
        NombreCategoria,
        Descripcion,
        IdSuperCategoriaProducto,
        Estado,
        FechaRegistro
    )
    VALUES
    (
        @NombreCategoria,
        @Descripcion,
        @IdSuperCategoriaProducto,
        1,
        GETDATE()
    );

    SET @Resultado = 1;
    SET @Mensaje = 'Categoría registrada correctamente.';
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_CATEGORIA_PRODUCTO_EDITAR
(
    @IdCategoriaProducto INT,
    @NombreCategoria VARCHAR(100),
    @Descripcion VARCHAR(250),
    @IdSuperCategoriaProducto INT,
    @Estado BIT,
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.SuperCategoriasProducto WHERE IdSuperCategoriaProducto = @IdSuperCategoriaProducto)
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'Debe seleccionar una supercategoría válida.';
        RETURN;
    END

    IF EXISTS
    (
        SELECT 1
        FROM dbo.CategoriasProducto
        WHERE NombreCategoria = @NombreCategoria
          AND IdSuperCategoriaProducto = @IdSuperCategoriaProducto
          AND IdCategoriaProducto <> @IdCategoriaProducto
    )
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'Ya existe otra categoría con ese nombre en la supercategoría seleccionada.';
        RETURN;
    END

    UPDATE dbo.CategoriasProducto
    SET NombreCategoria = @NombreCategoria,
        Descripcion = @Descripcion,
        IdSuperCategoriaProducto = @IdSuperCategoriaProducto,
        Estado = @Estado
    WHERE IdCategoriaProducto = @IdCategoriaProducto;

    UPDATE P
    SET IdSuperCategoriaProducto = @IdSuperCategoriaProducto
    FROM dbo.Productos P
    WHERE P.IdCategoriaProducto = @IdCategoriaProducto;

    SET @Resultado = 1;
    SET @Mensaje = 'Categoría actualizada correctamente.';
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_CATEGORIA_PRODUCTO_ELIMINAR
(
    @IdCategoriaProducto INT,
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.Productos WHERE IdCategoriaProducto = @IdCategoriaProducto)
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'No se puede eliminar: existen productos asociados.';
        RETURN;
    END

    DELETE FROM dbo.CategoriasProducto
    WHERE IdCategoriaProducto = @IdCategoriaProducto;

    SET @Resultado = 1;
    SET @Mensaje = 'Categoría eliminada correctamente.';
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_PRODUCTO_LISTAR
    @SoloActivos BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        P.IdProducto,
        P.Codigo,
        P.NombreProducto,
        P.Descripcion,
        P.IdSuperCategoriaProducto,
        S.NombreSuperCategoria,
        P.IdCategoriaProducto,
        C.NombreCategoria,
        P.IdUnidadMedida,
        U.NombreUnidad,
        U.Abreviatura AS AbreviaturaUnidad,
        P.StockMinimo,
        P.Estado,
        P.FechaRegistro,
        CAST(ISNULL(SP.StockActual, 0) AS DECIMAL(18, 2)) AS Cantidad
    FROM dbo.Productos P
    INNER JOIN dbo.SuperCategoriasProducto S
        ON S.IdSuperCategoriaProducto = P.IdSuperCategoriaProducto
    INNER JOIN dbo.CategoriasProducto C
        ON C.IdCategoriaProducto = P.IdCategoriaProducto
    INNER JOIN dbo.UnidadesMedida U
        ON U.IdUnidadMedida = P.IdUnidadMedida
    LEFT JOIN dbo.StockProductos SP
        ON SP.IdProducto = P.IdProducto
    CROSS APPLY
    (
        SELECT UPPER(LTRIM(RTRIM(ISNULL(P.Codigo, '')))) AS CodigoOrden
    ) CO
    CROSS APPLY
    (
        SELECT PATINDEX('%[0-9]%', CO.CodigoOrden) AS PosNumero
    ) PN
    CROSS APPLY
    (
        SELECT
            CASE WHEN PN.PosNumero > 0 THEN LEFT(CO.CodigoOrden, PN.PosNumero - 1) ELSE CO.CodigoOrden END AS Cliente,
            CASE WHEN PN.PosNumero > 0 THEN SUBSTRING(CO.CodigoOrden, PN.PosNumero, 8000) ELSE '' END AS DesdeNumero
    ) CP
    CROSS APPLY
    (
        SELECT
            CASE WHEN PN.PosNumero > 0 THEN PATINDEX('%[^0-9]%', CP.DesdeNumero + 'X') - 1 ELSE 0 END AS LargoNumero
    ) LN
    CROSS APPLY
    (
        SELECT
            CASE WHEN LN.LargoNumero > 0 THEN TRY_CONVERT(INT, LEFT(CP.DesdeNumero, LN.LargoNumero)) END AS Numero,
            CASE WHEN LN.LargoNumero > 0 THEN SUBSTRING(CP.DesdeNumero, LN.LargoNumero + 1, 8000) ELSE '' END AS RestoCodigo
    ) NR
    OUTER APPLY
    (
        SELECT
            CASE
                WHEN NR.RestoCodigo LIKE '%T[0-9]%'
                 AND TRY_CONVERT(INT, SUBSTRING(NR.RestoCodigo, LEN(NR.RestoCodigo) - PATINDEX('%[^0-9]%', REVERSE(NR.RestoCodigo) + 'X') + 2, 8000)) IS NOT NULL
                 AND SUBSTRING(NR.RestoCodigo, LEN(NR.RestoCodigo) - PATINDEX('%[^0-9]%', REVERSE(NR.RestoCodigo) + 'X') + 1, 1) = 'T'
                    THEN TRY_CONVERT(INT, SUBSTRING(NR.RestoCodigo, LEN(NR.RestoCodigo) - PATINDEX('%[^0-9]%', REVERSE(NR.RestoCodigo) + 'X') + 2, 8000))
            END AS TallaNumero,
            CASE
                WHEN NR.RestoCodigo LIKE '%T[0-9]%'
                 AND TRY_CONVERT(INT, SUBSTRING(NR.RestoCodigo, LEN(NR.RestoCodigo) - PATINDEX('%[^0-9]%', REVERSE(NR.RestoCodigo) + 'X') + 2, 8000)) IS NOT NULL
                 AND SUBSTRING(NR.RestoCodigo, LEN(NR.RestoCodigo) - PATINDEX('%[^0-9]%', REVERSE(NR.RestoCodigo) + 'X') + 1, 1) = 'T'
                    THEN LEFT(NR.RestoCodigo, LEN(NR.RestoCodigo) - PATINDEX('%[^0-9]%', REVERSE(NR.RestoCodigo) + 'X'))
            END AS VarianteNumero
    ) TN
    OUTER APPLY
    (
        SELECT TOP (1)
            V.Talla,
            V.OrdenTalla
        FROM (VALUES
            ('XXXL', 7),
            ('XXL', 6),
            ('XL', 5),
            ('XS', 1),
            ('L', 4),
            ('M', 3),
            ('S', 2)
        ) V(Talla, OrdenTalla)
        WHERE TN.TallaNumero IS NULL
          AND RIGHT(NR.RestoCodigo, LEN(V.Talla)) = V.Talla
          AND LEN(NR.RestoCodigo) >= LEN(V.Talla)
        ORDER BY LEN(V.Talla) DESC
    ) TT
    CROSS APPLY
    (
        SELECT
            COALESCE(TN.VarianteNumero, CASE WHEN TT.Talla IS NOT NULL THEN LEFT(NR.RestoCodigo, LEN(NR.RestoCodigo) - LEN(TT.Talla)) ELSE NR.RestoCodigo END) AS Variante,
            CASE
                WHEN TT.OrdenTalla IS NOT NULL THEN TT.OrdenTalla
                WHEN TN.TallaNumero IS NOT NULL THEN 100
                ELSE 0
            END AS OrdenTalla
    ) OK
    WHERE @SoloActivos = 0 OR P.Estado = 1
    ORDER BY
        CP.Cliente,
        CASE WHEN NR.Numero IS NULL THEN 1 ELSE 0 END,
        NR.Numero,
        OK.Variante,
        OK.OrdenTalla,
        TN.TallaNumero,
        CO.CodigoOrden,
        P.NombreProducto;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_PRODUCTO_REGISTRAR
(
    @Codigo VARCHAR(50),
    @NombreProducto VARCHAR(150),
    @Descripcion VARCHAR(250),
    @IdCategoriaProducto INT,
    @IdSuperCategoriaProducto INT = NULL,
    @IdUnidadMedida INT,
    @StockMinimo DECIMAL(18,2),
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdSuperCategoriaCategoria INT;

    SELECT @IdSuperCategoriaCategoria = IdSuperCategoriaProducto
    FROM dbo.CategoriasProducto
    WHERE IdCategoriaProducto = @IdCategoriaProducto;

    IF @IdSuperCategoriaCategoria IS NULL
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'Debe seleccionar una categoría válida.';
        RETURN;
    END

    IF @IdSuperCategoriaProducto IS NULL OR @IdSuperCategoriaProducto <= 0
        SET @IdSuperCategoriaProducto = @IdSuperCategoriaCategoria;

    IF @IdSuperCategoriaProducto <> @IdSuperCategoriaCategoria
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'La categoría no pertenece a la supercategoría seleccionada.';
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.Productos WHERE Codigo = @Codigo)
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'Ya existe un producto con ese código.';
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.Productos WHERE NombreProducto = @NombreProducto)
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'Ya existe un producto con ese nombre.';
        RETURN;
    END

    INSERT INTO dbo.Productos
    (
        Codigo,
        NombreProducto,
        Descripcion,
        IdSuperCategoriaProducto,
        IdCategoriaProducto,
        IdUnidadMedida,
        StockMinimo,
        Estado,
        FechaRegistro
    )
    VALUES
    (
        @Codigo,
        @NombreProducto,
        @Descripcion,
        @IdSuperCategoriaProducto,
        @IdCategoriaProducto,
        @IdUnidadMedida,
        @StockMinimo,
        1,
        GETDATE()
    );

    SET @Resultado = 1;
    SET @Mensaje = 'Producto registrado correctamente.';
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_PRODUCTO_EDITAR
(
    @IdProducto INT,
    @Codigo VARCHAR(50),
    @NombreProducto VARCHAR(150),
    @Descripcion VARCHAR(250),
    @IdCategoriaProducto INT,
    @IdSuperCategoriaProducto INT = NULL,
    @IdUnidadMedida INT,
    @StockMinimo DECIMAL(18,2),
    @Estado BIT,
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdSuperCategoriaCategoria INT;

    SELECT @IdSuperCategoriaCategoria = IdSuperCategoriaProducto
    FROM dbo.CategoriasProducto
    WHERE IdCategoriaProducto = @IdCategoriaProducto;

    IF @IdSuperCategoriaCategoria IS NULL
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'Debe seleccionar una categoría válida.';
        RETURN;
    END

    IF @IdSuperCategoriaProducto IS NULL OR @IdSuperCategoriaProducto <= 0
        SET @IdSuperCategoriaProducto = @IdSuperCategoriaCategoria;

    IF @IdSuperCategoriaProducto <> @IdSuperCategoriaCategoria
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'La categoría no pertenece a la supercategoría seleccionada.';
        RETURN;
    END

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Productos
        WHERE Codigo = @Codigo
          AND IdProducto <> @IdProducto
    )
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'Ya existe otro producto con ese código.';
        RETURN;
    END

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Productos
        WHERE NombreProducto = @NombreProducto
          AND IdProducto <> @IdProducto
    )
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'Ya existe otro producto con ese nombre.';
        RETURN;
    END

    UPDATE dbo.Productos
    SET Codigo = @Codigo,
        NombreProducto = @NombreProducto,
        Descripcion = @Descripcion,
        IdSuperCategoriaProducto = @IdSuperCategoriaProducto,
        IdCategoriaProducto = @IdCategoriaProducto,
        IdUnidadMedida = @IdUnidadMedida,
        StockMinimo = @StockMinimo,
        Estado = @Estado
    WHERE IdProducto = @IdProducto;

    SET @Resultado = 1;
    SET @Mensaje = 'Producto actualizado correctamente.';
END
GO

DECLARE @IdProductosPadre INT =
(
    SELECT TOP 1 IdMenu
    FROM dbo.Menu
    WHERE NombreMenu = 'Productos'
      AND IdMenuPadre IS NULL
    ORDER BY IdMenu
);

IF @IdProductosPadre IS NOT NULL
BEGIN
    UPDATE dbo.Menu
    SET NombreMenu = 'Supercategor' + CHAR(237) + 'as'
    WHERE IdMenuPadre = @IdProductosPadre
      AND NombreMenu LIKE 'Supercategor%';

    IF NOT EXISTS (SELECT 1 FROM dbo.Menu WHERE NombreMenu = 'Supercategor' + CHAR(237) + 'as' AND IdMenuPadre = @IdProductosPadre)
    BEGIN
        INSERT INTO dbo.Menu (NombreMenu, IdMenuPadre, Orden, Estado)
        VALUES ('Supercategor' + CHAR(237) + 'as', @IdProductosPadre, 1, 1);
    END

    UPDATE dbo.Menu SET Orden = 1 WHERE NombreMenu = 'Supercategor' + CHAR(237) + 'as' AND IdMenuPadre = @IdProductosPadre;
    UPDATE dbo.Menu SET Orden = 2 WHERE IdMenuPadre = @IdProductosPadre AND NombreMenu LIKE 'Categor%Productos';
    UPDATE dbo.Menu SET Orden = 3 WHERE NombreMenu = N'Productos' AND IdMenuPadre = @IdProductosPadre;

    INSERT INTO dbo.PermisosMenu (IdRol, IdMenu, PuedeVer)
    SELECT DISTINCT PM.IdRol, M.IdMenu, 1
    FROM dbo.Menu M
    INNER JOIN dbo.PermisosMenu PM
        ON PM.IdMenu = @IdProductosPadre
       AND PM.PuedeVer = 1
    WHERE M.NombreMenu = 'Supercategor' + CHAR(237) + 'as'
      AND M.IdMenuPadre = @IdProductosPadre
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.PermisosMenu P
          WHERE P.IdRol = PM.IdRol
            AND P.IdMenu = M.IdMenu
      );
END
GO
