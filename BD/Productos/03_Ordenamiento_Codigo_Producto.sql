USE CorexProdDB;
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
END;
GO
