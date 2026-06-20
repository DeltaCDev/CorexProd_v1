SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.GuiaInternaImpresiones', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GuiaInternaImpresiones
    (
        IdGuiaInternaImpresion INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_GuiaInternaImpresiones PRIMARY KEY,
        IdGuiaInterna INT NOT NULL,
        IdUsuario INT NOT NULL,
        NombreUsuario VARCHAR(150) NOT NULL,
        FechaImpresion DATETIME2(0) NOT NULL,
        TipoImpresion VARCHAR(20) NOT NULL,
        NombreImpresora NVARCHAR(260) NOT NULL,
        CONSTRAINT FK_GuiaInternaImpresiones_Guia
            FOREIGN KEY (IdGuiaInterna) REFERENCES dbo.GuiasInternas(IdGuiaInterna),
        CONSTRAINT FK_GuiaInternaImpresiones_Usuario
            FOREIGN KEY (IdUsuario) REFERENCES dbo.Usuarios(IdUsuario),
        CONSTRAINT CK_GuiaInternaImpresiones_Tipo
            CHECK (TipoImpresion IN ('ORIGINAL', 'REIMPRESION'))
    );

END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.GuiaInternaImpresiones') AND name = 'UX_GuiaInternaImpresiones_Original')
    CREATE UNIQUE INDEX UX_GuiaInternaImpresiones_Original
        ON dbo.GuiaInternaImpresiones(IdGuiaInterna)
        WHERE TipoImpresion = 'ORIGINAL';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.GuiaInternaImpresiones') AND name = 'IX_GuiaInternaImpresiones_GuiaFecha')
    CREATE INDEX IX_GuiaInternaImpresiones_GuiaFecha
        ON dbo.GuiaInternaImpresiones(IdGuiaInterna, FechaImpresion DESC);
