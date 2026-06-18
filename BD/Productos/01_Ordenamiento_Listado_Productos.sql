USE CorexProdDB;
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_PRODUCTO_LISTAR
    @SoloActivos BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH ProductoTexto AS
    (
        SELECT
            P.IdProducto,
            P.Codigo,
            P.NombreProducto,
            P.Descripcion,
            P.IdCategoriaProducto,
            C.NombreCategoria,
            P.IdUnidadMedida,
            U.NombreUnidad,
            U.Abreviatura AS AbreviaturaUnidad,
            P.StockMinimo,
            P.Estado,
            P.FechaRegistro,
            CAST(ISNULL(SP.StockActual, 0) AS DECIMAL(18,2)) AS Cantidad,
            UPPER(LTRIM(RTRIM(P.Codigo))) AS CodigoOrden,
            UPPER(LTRIM(RTRIM(P.NombreProducto))) AS NombreOrden
        FROM dbo.Productos P
        INNER JOIN dbo.CategoriasProducto C
            ON C.IdCategoriaProducto = P.IdCategoriaProducto
        INNER JOIN dbo.UnidadesMedida U
            ON U.IdUnidadMedida = P.IdUnidadMedida
        LEFT JOIN dbo.StockProductos SP
            ON SP.IdProducto = P.IdProducto
        WHERE @SoloActivos = 0 OR P.Estado = 1
    ),
    Tokens AS
    (
        SELECT
            P.*,
            RIGHT(P.NombreOrden, CHARINDEX(' ', REVERSE(P.NombreOrden) + ' ') - 1) AS UltimoTokenNombre,
            CHARINDEX('T', REVERSE(P.CodigoOrden)) AS PosicionTDesdeFin
        FROM ProductoTexto P
    ),
    Tallas AS
    (
        SELECT
            T.*,
            CASE
                WHEN T.UltimoTokenNombre IN ('XXS', 'XS', 'S', 'M', 'L', 'XL', 'XXL', 'XXXL')
                    THEN T.UltimoTokenNombre
                WHEN LEFT(T.UltimoTokenNombre, 2) = 'T.'
                     AND SUBSTRING(T.UltimoTokenNombre, 3, 10) IN ('XXS', 'XS', 'S', 'M', 'L', 'XL', 'XXL', 'XXXL')
                    THEN SUBSTRING(T.UltimoTokenNombre, 3, 10)
            END AS TallaNombreAlfabetica,
            CASE
                WHEN LEFT(T.UltimoTokenNombre, 1) = 'T'
                    THEN TRY_CONVERT(INT, SUBSTRING(T.UltimoTokenNombre, 2, 20))
                WHEN TRY_CONVERT(INT, T.UltimoTokenNombre) IS NOT NULL
                     AND RIGHT(
                         RTRIM(LEFT(T.NombreOrden, LEN(T.NombreOrden) - LEN(T.UltimoTokenNombre))),
                         5
                     ) = 'TALLA'
                    THEN TRY_CONVERT(INT, T.UltimoTokenNombre)
            END AS TallaNombreNumerica,
            CA.Talla AS TallaCodigoAlfabetica,
            CA.Prioridad AS PrioridadCodigoAlfabetica,
            CASE
                WHEN T.PosicionTDesdeFin > 1
                    THEN TRY_CONVERT(INT, RIGHT(T.CodigoOrden, T.PosicionTDesdeFin - 1))
            END AS TallaCodigoNumerica
        FROM Tokens T
        OUTER APPLY
        (
            SELECT TOP (1)
                V.Talla,
                V.Prioridad
            FROM (VALUES
                ('XXS', 1),
                ('XS', 2),
                ('S', 3),
                ('M', 4),
                ('L', 5),
                ('XL', 6),
                ('XXL', 7),
                ('XXXL', 8)
            ) V(Talla, Prioridad)
            WHERE RIGHT(T.CodigoOrden, LEN(V.Talla)) = V.Talla
              AND LEN(T.CodigoOrden) > LEN(V.Talla)
              AND
              (
                  SUBSTRING(T.CodigoOrden, LEN(T.CodigoOrden) - LEN(V.Talla), 1) LIKE '[0-9 ._/-]'
                  OR T.UltimoTokenNombre = V.Talla
                  OR T.UltimoTokenNombre = 'T.' + V.Talla
              )
            ORDER BY LEN(V.Talla) DESC
        ) CA
    ),
    Bases AS
    (
        SELECT
            T.*,
            CASE
                WHEN T.TallaCodigoAlfabetica IS NOT NULL
                    THEN RTRIM(LEFT(T.CodigoOrden, LEN(T.CodigoOrden) - LEN(T.TallaCodigoAlfabetica)))
                WHEN T.TallaNombreNumerica IS NOT NULL
                     AND RIGHT(T.CodigoOrden, LEN(T.UltimoTokenNombre)) = T.UltimoTokenNombre
                    THEN RTRIM(LEFT(T.CodigoOrden, LEN(T.CodigoOrden) - LEN(T.UltimoTokenNombre)))
                WHEN T.TallaCodigoNumerica IS NOT NULL
                    THEN RTRIM(LEFT(T.CodigoOrden, LEN(T.CodigoOrden) - T.PosicionTDesdeFin))
                ELSE T.CodigoOrden
            END AS CodigoBase,
            CASE
                WHEN T.TallaNombreAlfabetica IS NOT NULL OR T.TallaNombreNumerica IS NOT NULL
                    THEN RTRIM(LEFT(T.NombreOrden, LEN(T.NombreOrden) - LEN(T.UltimoTokenNombre)))
                ELSE T.NombreOrden
            END AS NombreSinTalla
        FROM Tallas T
    ),
    Claves AS
    (
        SELECT
            B.*,
            CASE
                WHEN B.TallaNombreAlfabetica IS NOT NULL OR B.TallaNombreNumerica IS NOT NULL
                    THEN CASE
                        WHEN RIGHT(B.NombreSinTalla, 5) = 'TALLA'
                            THEN RTRIM(LEFT(B.NombreSinTalla, LEN(B.NombreSinTalla) - 5))
                        WHEN RIGHT(B.NombreSinTalla, 2) = 'T.'
                            THEN RTRIM(LEFT(B.NombreSinTalla, LEN(B.NombreSinTalla) - 2))
                        WHEN RIGHT(B.NombreSinTalla, 1) = 'T'
                            THEN RTRIM(LEFT(B.NombreSinTalla, LEN(B.NombreSinTalla) - 1))
                        ELSE B.NombreSinTalla
                    END
                ELSE B.NombreSinTalla
            END AS NombreBase,
            CASE
                WHEN B.TallaNombreAlfabetica IS NOT NULL THEN 1
                WHEN B.TallaNombreNumerica IS NOT NULL THEN 2
                WHEN B.TallaCodigoAlfabetica IS NOT NULL THEN 1
                WHEN B.TallaCodigoNumerica IS NOT NULL THEN 2
                ELSE 0
            END AS TipoTalla,
            CASE COALESCE(B.TallaNombreAlfabetica, B.TallaCodigoAlfabetica)
                WHEN 'XXS' THEN 1
                WHEN 'XS' THEN 2
                WHEN 'S' THEN 3
                WHEN 'M' THEN 4
                WHEN 'L' THEN 5
                WHEN 'XL' THEN 6
                WHEN 'XXL' THEN 7
                WHEN 'XXXL' THEN 8
                ELSE 0
            END AS PrioridadTallaAlfabetica,
            COALESCE(B.TallaNombreNumerica, B.TallaCodigoNumerica, 0) AS ValorTallaNumerica,
            PATINDEX('%[0-9]%', B.CodigoBase) AS PosicionPrimerNumero
        FROM Bases B
    ),
    Orden AS
    (
        SELECT
            C.*,
            CASE
                WHEN C.PosicionPrimerNumero = 0 THEN C.CodigoBase
                ELSE LEFT(C.CodigoBase, C.PosicionPrimerNumero - 1)
            END AS PrefijoCodigo,
            CASE
                WHEN C.PosicionPrimerNumero = 0 THEN NULL
                ELSE TRY_CONVERT(BIGINT,
                    LEFT(
                        SUBSTRING(C.CodigoBase, C.PosicionPrimerNumero, 8000),
                        PATINDEX('%[^0-9]%', SUBSTRING(C.CodigoBase, C.PosicionPrimerNumero, 8000) + 'X') - 1
                    ))
            END AS NumeroCodigo
        FROM Claves C
    )
    SELECT
        O.IdProducto,
        O.Codigo,
        O.NombreProducto,
        O.Descripcion,
        O.IdCategoriaProducto,
        O.NombreCategoria,
        O.IdUnidadMedida,
        O.NombreUnidad,
        O.AbreviaturaUnidad,
        O.StockMinimo,
        O.Estado,
        O.FechaRegistro,
        O.Cantidad
    FROM Orden O
    ORDER BY
        O.PrefijoCodigo,
        CASE WHEN O.NumeroCodigo IS NULL THEN 0 ELSE 1 END,
        O.NumeroCodigo,
        O.CodigoBase,
        O.NombreBase,
        O.TipoTalla,
        O.PrioridadTallaAlfabetica,
        O.ValorTallaNumerica,
        O.Codigo,
        O.NombreProducto;
END;
GO
