USE CorexProdDB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

IF OBJECT_ID('dbo.FichaTecnicaDocumento', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.FichaTecnicaDocumento
    (
        IdFichaTecnicaDocumento INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_FichaTecnicaDocumento PRIMARY KEY,
        CodigoModelo VARCHAR(40) NOT NULL,
        NombreArchivo NVARCHAR(260) NOT NULL,
        RutaRelativa NVARCHAR(500) NOT NULL,
        Version INT NOT NULL
            CONSTRAINT DF_FichaTecnicaDocumento_Version DEFAULT (1),
        Estado BIT NOT NULL
            CONSTRAINT DF_FichaTecnicaDocumento_Estado DEFAULT (1),
        FechaRegistro DATETIME2(0) NOT NULL
            CONSTRAINT DF_FichaTecnicaDocumento_FechaRegistro DEFAULT (SYSDATETIME()),
        UsuarioRegistro VARCHAR(100) NOT NULL
            CONSTRAINT DF_FichaTecnicaDocumento_Usuario DEFAULT ('MIGRACION')
    );

    CREATE UNIQUE INDEX UX_FichaTecnicaDocumento_ModeloVersion
        ON dbo.FichaTecnicaDocumento(CodigoModelo, Version);
END;

DECLARE @RutaBase NVARCHAR(500) = N'D:\FICHAS_TECNICAS';

IF EXISTS (SELECT 1 FROM dbo.Parametros WHERE CodigoParametro = 'RUTA_FICHA_TECNICA')
BEGIN
    UPDATE dbo.Parametros
    SET ValorParametro = @RutaBase
    WHERE CodigoParametro = 'RUTA_FICHA_TECNICA';
END
ELSE
BEGIN
    INSERT INTO dbo.Parametros
    (
        CodigoParametro,
        NombreParametro,
        ValorParametro,
        Descripcion,
        Estado,
        FechaRegistro
    )
    VALUES
    (
        'RUTA_FICHA_TECNICA',
        'Ruta de fichas técnicas documentales',
        @RutaBase,
        'Carpeta central de PDF de fichas técnicas por modelo.',
        1,
        GETDATE()
    );
END;

DECLARE @Documentos TABLE
(
    CodigoModelo VARCHAR(40) NOT NULL,
    NombreArchivo NVARCHAR(260) NOT NULL
);

INSERT INTO @Documentos (CodigoModelo, NombreArchivo)
VALUES
('FR001', N'FR001.pdf'),
('FR002', N'FR002.pdf'),
('FR003', N'FR003.pdf'),
('FR004', N'FR004.pdf'),
('FR005', N'FR005.pdf'),
('FR006', N'FR006.pdf'),
('FR007', N'FR007.pdf'),
('FR008', N'FR008.pdf'),
('FR009', N'FR009.pdf'),
('FR010', N'FR010.pdf'),
('FR011', N'FR011.pdf'),
('FR012', N'FR012.pdf'),
('MT001P', N'MT001P.pdf'),
('MT001T', N'MT001T.pdf'),
('MT002', N'MT002.pdf'),
('MT003', N'MT003.pdf'),
('MT004', N'MT004.pdf'),
('MT005', N'MT005.pdf'),
('MT006', N'MT006.pdf'),
('MT007', N'MT007.pdf'),
('MT008', N'MT008.pdf'),
('MT009', N'MT009.pdf'),
('MT010P', N'MT010P.pdf'),
('MT011', N'MT011.pdf'),
('MT012', N'MT012.pdf'),
('MT013', N'MT013.pdf'),
('MT014', N'MT014.pdf'),
('MT015', N'MT015.pdf'),
('MT020', N'MT020.pdf'),
('SGS001', N'SGS001.pdf'),
('SGS002', N'SGS002.pdf'),
('SGS003AF', N'SGS003AF.pdf'),
('SGS003N', N'SGS003N.pdf'),
('SGS004AZ', N'SGS004AZ.pdf'),
('SGS005', N'SGS005.pdf'),
('SGS006', N'SGS006.pdf'),
('SGS007', N'SGS007.pdf'),
('SGS008AZ', N'SGS008AZ.pdf'),
('SGS009', N'SGS009.pdf'),
('SGS010', N'SGS010.pdf'),
('SGS011', N'SGS011.pdf'),
('SGS012', N'SGS012.pdf'),
('SGS013', N'SGS013.pdf'),
('SGS014', N'SGS014.pdf'),
('SGS015', N'SGS015.pdf'),
('SGS016', N'SGS016.pdf'),
('SGS017', N'SGS017.pdf'),
('SGS018', N'SGS018.pdf'),
('SGS019', N'SGS019.pdf'),
('SGS020', N'SGS020.pdf'),
('SGS021', N'SGS021.pdf'),
('SGS022', N'SGS022.pdf'),
('SGS023', N'SGS023.pdf'),
('SGS024', N'SGS024.pdf'),
('SGS025', N'SGS025.pdf'),
('SGS026', N'SGS026.pdf'),
('SGS027', N'SGS027.pdf');

MERGE dbo.FichaTecnicaDocumento AS destino
USING
(
    SELECT
        CodigoModelo,
        NombreArchivo,
        NombreArchivo AS RutaRelativa
    FROM @Documentos
) AS origen
ON destino.CodigoModelo = origen.CodigoModelo
AND destino.Version = 1
WHEN MATCHED THEN
    UPDATE SET
        NombreArchivo = origen.NombreArchivo,
        RutaRelativa = origen.RutaRelativa,
        Estado = 1
WHEN NOT MATCHED THEN
    INSERT
    (
        CodigoModelo,
        NombreArchivo,
        RutaRelativa,
        Version,
        Estado,
        UsuarioRegistro
    )
    VALUES
    (
        origen.CodigoModelo,
        origen.NombreArchivo,
        origen.RutaRelativa,
        1,
        1,
        'MIGRACION'
    );

COMMIT TRANSACTION;
GO

SELECT
    IdFichaTecnicaDocumento,
    CodigoModelo,
    NombreArchivo,
    RutaRelativa,
    Version,
    Estado
FROM dbo.FichaTecnicaDocumento
ORDER BY CodigoModelo, Version DESC;
GO
