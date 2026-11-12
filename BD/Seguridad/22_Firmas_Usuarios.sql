/* Firma digitalizada del usuario para documentos impresos. */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.Usuarios', 'FirmaImagen') IS NULL
    ALTER TABLE dbo.Usuarios ADD FirmaImagen VARBINARY(MAX) NULL;
IF COL_LENGTH('dbo.Usuarios', 'FirmaMimeType') IS NULL
    ALTER TABLE dbo.Usuarios ADD FirmaMimeType VARCHAR(100) NULL;
GO
