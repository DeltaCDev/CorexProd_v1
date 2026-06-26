USE CorexProdDB;
GO

IF COL_LENGTH('dbo.Productos', 'EtiquetaCliente') IS NULL
BEGIN
    ALTER TABLE dbo.Productos ADD EtiquetaCliente VARCHAR(150) NULL;
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
        ISNULL(P.EtiquetaCliente, '') AS EtiquetaCliente,
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
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_PRODUCTO_REGISTRAR
(
    @Codigo VARCHAR(50),
    @NombreProducto VARCHAR(150),
    @EtiquetaCliente VARCHAR(150) = '',
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
        SET @Mensaje = 'Debe seleccionar una categoria valida.';
        RETURN;
    END

    IF @IdSuperCategoriaProducto IS NULL OR @IdSuperCategoriaProducto <= 0
        SET @IdSuperCategoriaProducto = @IdSuperCategoriaCategoria;

    IF @IdSuperCategoriaProducto <> @IdSuperCategoriaCategoria
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'La categoria no pertenece a la supercategoria seleccionada.';
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.Productos WHERE Codigo = @Codigo)
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'Ya existe un producto con ese codigo.';
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
        EtiquetaCliente,
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
        NULLIF(LTRIM(RTRIM(@EtiquetaCliente)), ''),
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
    @EtiquetaCliente VARCHAR(150) = '',
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
        SET @Mensaje = 'Debe seleccionar una categoria valida.';
        RETURN;
    END

    IF @IdSuperCategoriaProducto IS NULL OR @IdSuperCategoriaProducto <= 0
        SET @IdSuperCategoriaProducto = @IdSuperCategoriaCategoria;

    IF @IdSuperCategoriaProducto <> @IdSuperCategoriaCategoria
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'La categoria no pertenece a la supercategoria seleccionada.';
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
        SET @Mensaje = 'Ya existe otro producto con ese codigo.';
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
        EtiquetaCliente = NULLIF(LTRIM(RTRIM(@EtiquetaCliente)), ''),
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

CREATE OR ALTER PROCEDURE dbo.USP_ALM_PRODUCTO_STOCK_BUSCAR
    @IdAlmacen INT,
    @Texto VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (40)
        P.IdProducto,
        P.Codigo,
        P.NombreProducto,
        ISNULL(P.EtiquetaCliente, '') AS EtiquetaCliente,
        ISNULL(P.Descripcion, '') AS Descripcion,
        P.IdUnidadMedida,
        UM.NombreUnidad,
        CAST(ISNULL(SPA.StockActual, 0) AS DECIMAL(18, 2)) AS StockActual
    FROM dbo.Productos P
    INNER JOIN dbo.UnidadesMedida UM ON UM.IdUnidadMedida = P.IdUnidadMedida
    LEFT JOIN dbo.StockProductosAlmacen SPA ON SPA.IdProducto = P.IdProducto AND SPA.IdAlmacen = @IdAlmacen
    WHERE P.Estado = 1
      AND
      (
          P.Codigo LIKE '%' + @Texto + '%'
          OR P.NombreProducto LIKE '%' + @Texto + '%'
          OR ISNULL(P.EtiquetaCliente, '') LIKE '%' + @Texto + '%'
          OR ISNULL(P.Descripcion, '') LIKE '%' + @Texto + '%'
      )
    ORDER BY P.NombreProducto;
END
GO
