SET XACT_ABORT ON;
GO

IF COL_LENGTH('dbo.OrdenesCompraInterna', 'FechaEntrega') IS NULL
BEGIN
    ALTER TABLE dbo.OrdenesCompraInterna
    ADD FechaEntrega DATE NULL;
END;
GO

UPDATE dbo.OrdenesCompraInterna
SET FechaEntrega = DATEADD(DAY, 7, FechaEmision)
WHERE FechaEntrega IS NULL;
GO

IF EXISTS
(
    SELECT 1
    FROM sys.default_constraints DC
    INNER JOIN sys.columns C
        ON C.default_object_id = DC.object_id
    WHERE DC.parent_object_id = OBJECT_ID('dbo.OrdenesCompraInterna')
      AND C.name = 'FechaEntrega'
)
BEGIN
    DECLARE @DefaultName SYSNAME;
    SELECT @DefaultName = DC.name
    FROM sys.default_constraints DC
    INNER JOIN sys.columns C
        ON C.default_object_id = DC.object_id
    WHERE DC.parent_object_id = OBJECT_ID('dbo.OrdenesCompraInterna')
      AND C.name = 'FechaEntrega';

    DECLARE @DropDefaultSql NVARCHAR(MAX);
    SET @DropDefaultSql = N'ALTER TABLE dbo.OrdenesCompraInterna DROP CONSTRAINT ' + QUOTENAME(@DefaultName);
    EXEC(@DropDefaultSql);
END;
GO

ALTER TABLE dbo.OrdenesCompraInterna
ALTER COLUMN FechaEntrega DATE NOT NULL;
GO

ALTER TABLE dbo.OrdenesCompraInterna
ADD CONSTRAINT DF_OrdenesCompraInterna_FechaEntrega
DEFAULT (DATEADD(DAY, 1, CONVERT(DATE, GETDATE()))) FOR FechaEntrega;
GO

IF EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID('dbo.OrdenesCompraInterna')
      AND name = 'CK_OrdenesCompraInterna_FechaEntrega'
)
    ALTER TABLE dbo.OrdenesCompraInterna DROP CONSTRAINT CK_OrdenesCompraInterna_FechaEntrega;
GO

ALTER TABLE dbo.OrdenesCompraInterna WITH CHECK
ADD CONSTRAINT CK_OrdenesCompraInterna_FechaEntrega
CHECK (FechaEntrega > FechaEmision);
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.OrdenesCompraInterna')
      AND name = 'IX_OrdenesCompraInterna_FechaEntrega_Estado'
)
BEGIN
    CREATE INDEX IX_OrdenesCompraInterna_FechaEntrega_Estado
        ON dbo.OrdenesCompraInterna(FechaEntrega, Estado)
        INCLUDE (NumeroOci, NombreCliente, FechaEmision);
END;
GO

PRINT 'Fecha de entrega y alertas de OC configuradas correctamente.';
GO
