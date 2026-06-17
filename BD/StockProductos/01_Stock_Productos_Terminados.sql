USE CorexProdDB;
GO

IF OBJECT_ID('dbo.StockProductos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockProductos
    (
        IdStockProducto INT IDENTITY(1,1) NOT NULL,
        IdProducto INT NOT NULL,
        StockActual DECIMAL(18, 2) NOT NULL CONSTRAINT DF_StockProductos_StockActual DEFAULT (0),
        FechaActualizacion DATETIME NOT NULL CONSTRAINT DF_StockProductos_FechaActualizacion DEFAULT (GETDATE()),
        CONSTRAINT PK_StockProductos PRIMARY KEY CLUSTERED (IdStockProducto ASC),
        CONSTRAINT UQ_StockProductos_IdProducto UNIQUE (IdProducto),
        CONSTRAINT FK_StockProductos_Productos FOREIGN KEY (IdProducto)
            REFERENCES dbo.Productos(IdProducto)
    );
END
GO

INSERT INTO dbo.StockProductos (IdProducto, StockActual)
SELECT P.IdProducto, 0
FROM dbo.Productos P
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.StockProductos SP
    WHERE SP.IdProducto = P.IdProducto
);
GO
