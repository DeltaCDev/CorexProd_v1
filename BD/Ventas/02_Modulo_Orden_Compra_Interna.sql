USE CorexProdDB;
GO

IF OBJECT_ID('dbo.OrdenesCompraInterna', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrdenesCompraInterna
    (
        IdOrdenCompraInterna INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrdenesCompraInterna PRIMARY KEY,
        NumeroOci VARCHAR(40) NOT NULL,
        IdProforma INT NOT NULL,
        FechaEmision DATE NOT NULL,
        OrdenCompraCliente VARCHAR(100) NOT NULL CONSTRAINT DF_OCI_OrdenCompraCliente DEFAULT(''),
        IdCliente INT NOT NULL,
        NombreCliente VARCHAR(250) NOT NULL,
        Subtotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_OCI_Subtotal DEFAULT(0),
        Descuento DECIMAL(18,2) NOT NULL CONSTRAINT DF_OCI_Descuento DEFAULT(0),
        Igv DECIMAL(18,2) NOT NULL CONSTRAINT DF_OCI_Igv DEFAULT(0),
        Total DECIMAL(18,2) NOT NULL CONSTRAINT DF_OCI_Total DEFAULT(0),
        Estado VARCHAR(20) NOT NULL CONSTRAINT DF_OCI_Estado DEFAULT('Registrada'),
        UsuarioGenerador VARCHAR(80) NOT NULL,
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_OCI_FechaRegistro DEFAULT(GETDATE()),
        CONSTRAINT UQ_OCI_Numero UNIQUE (NumeroOci),
        CONSTRAINT UQ_OCI_Proforma UNIQUE (IdProforma),
        CONSTRAINT FK_OCI_Proforma FOREIGN KEY (IdProforma) REFERENCES dbo.Proformas(IdProforma),
        CONSTRAINT FK_OCI_Cliente FOREIGN KEY (IdCliente) REFERENCES dbo.Clientes(IdCliente)
    );
END;
GO

IF OBJECT_ID('dbo.OrdenCompraInternaDetalle', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrdenCompraInternaDetalle
    (
        IdOrdenCompraInternaDetalle INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrdenCompraInternaDetalle PRIMARY KEY,
        IdOrdenCompraInterna INT NOT NULL,
        IdProducto INT NOT NULL,
        CodigoProducto VARCHAR(100) NOT NULL,
        NombreProducto VARCHAR(500) NOT NULL,
        Cantidad DECIMAL(18,2) NOT NULL,
        PrecioUnitario DECIMAL(18,2) NOT NULL,
        Descuento DECIMAL(18,2) NOT NULL,
        Importe DECIMAL(18,2) NOT NULL,
        Observacion VARCHAR(500) NOT NULL CONSTRAINT DF_OCID_Observacion DEFAULT(''),
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_OCID_FechaRegistro DEFAULT(GETDATE()),
        CONSTRAINT FK_OCID_OCI FOREIGN KEY (IdOrdenCompraInterna)
            REFERENCES dbo.OrdenesCompraInterna(IdOrdenCompraInterna),
        CONSTRAINT FK_OCID_Producto FOREIGN KEY (IdProducto) REFERENCES dbo.Productos(IdProducto)
    );
END;
GO

UPDATE P
SET Estado = CASE
    WHEN P.Estado = 'Anulado' THEN 'Anulado'
    WHEN P.TieneOrdenCompraInterna = 1
         OR EXISTS (SELECT 1 FROM dbo.OrdenesCompraInterna O WHERE O.IdProforma = P.IdProforma)
        THEN 'Registrado'
    ELSE 'Emitido'
END
FROM dbo.Proformas P;
GO

CREATE OR ALTER PROCEDURE dbo.USP_VEN_OCI_LISTAR
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        O.IdOrdenCompraInterna,
        O.NumeroOci,
        O.IdProforma,
        P.SerieNumero AS NumeroProforma,
        O.FechaEmision,
        O.OrdenCompraCliente,
        O.IdCliente,
        O.NombreCliente,
        O.Subtotal,
        O.Descuento,
        O.Igv,
        O.Total,
        O.Estado,
        O.UsuarioGenerador,
        O.FechaRegistro
    FROM dbo.OrdenesCompraInterna O
    INNER JOIN dbo.Proformas P ON P.IdProforma = O.IdProforma
    ORDER BY O.FechaEmision DESC, O.IdOrdenCompraInterna DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_VEN_OCI_OBTENER
    @IdOrdenCompraInterna INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        O.IdOrdenCompraInterna,
        O.NumeroOci,
        O.IdProforma,
        P.SerieNumero AS NumeroProforma,
        O.FechaEmision,
        O.OrdenCompraCliente,
        O.IdCliente,
        O.NombreCliente,
        O.Subtotal,
        O.Descuento,
        O.Igv,
        O.Total,
        O.Estado,
        O.UsuarioGenerador,
        O.FechaRegistro
    FROM dbo.OrdenesCompraInterna O
    INNER JOIN dbo.Proformas P ON P.IdProforma = O.IdProforma
    WHERE O.IdOrdenCompraInterna = @IdOrdenCompraInterna;

    SELECT
        D.IdOrdenCompraInternaDetalle,
        D.IdOrdenCompraInterna,
        D.IdProducto,
        D.CodigoProducto,
        D.NombreProducto,
        D.Cantidad,
        D.PrecioUnitario,
        D.Descuento,
        D.Importe,
        D.Observacion
    FROM dbo.OrdenCompraInternaDetalle D
    WHERE D.IdOrdenCompraInterna = @IdOrdenCompraInterna
    ORDER BY D.IdOrdenCompraInternaDetalle;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_VEN_OCI_GENERAR
    @IdProforma INT,
    @UsuarioGenerador VARCHAR(80),
    @IdGenerado INT OUTPUT,
    @NumeroOci VARCHAR(40) OUTPUT,
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @IdGenerado = 0;
    SET @NumeroOci = '';
    SET @Resultado = 0;

    IF NOT EXISTS (SELECT 1 FROM dbo.Proformas WHERE IdProforma = @IdProforma)
    BEGIN
        SET @Mensaje = 'No se encontró la proforma seleccionada.';
        RETURN;
    END;

    IF EXISTS (SELECT 1 FROM dbo.Proformas WHERE IdProforma = @IdProforma AND Estado = 'Anulado')
    BEGIN
        SET @Mensaje = 'No se puede generar una OCI desde una proforma anulada.';
        RETURN;
    END;

    IF EXISTS (SELECT 1 FROM dbo.OrdenesCompraInterna WHERE IdProforma = @IdProforma)
       OR EXISTS (SELECT 1 FROM dbo.Proformas WHERE IdProforma = @IdProforma AND TieneOrdenCompraInterna = 1)
    BEGIN
        SET @Mensaje = 'La proforma ya tiene una orden de compra interna.';
        RETURN;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO dbo.OrdenesCompraInterna
        (
            NumeroOci, IdProforma, FechaEmision, OrdenCompraCliente, IdCliente,
            NombreCliente, Subtotal, Descuento, Igv, Total, Estado, UsuarioGenerador
        )
        SELECT
            '', P.IdProforma, CAST(GETDATE() AS DATE), P.OrdenCompraCliente, P.IdCliente,
            C.NombreRazonSocial, P.Subtotal, P.Descuento, P.Igv, P.Total, 'Registrada', @UsuarioGenerador
        FROM dbo.Proformas P
        INNER JOIN dbo.Clientes C ON C.IdCliente = P.IdCliente
        WHERE P.IdProforma = @IdProforma;

        SET @IdGenerado = SCOPE_IDENTITY();
        SET @NumeroOci = CONCAT('OCI-', RIGHT(CONCAT('000000', @IdGenerado), 6));

        UPDATE dbo.OrdenesCompraInterna
        SET NumeroOci = @NumeroOci
        WHERE IdOrdenCompraInterna = @IdGenerado;

        INSERT INTO dbo.OrdenCompraInternaDetalle
        (
            IdOrdenCompraInterna, IdProducto, CodigoProducto, NombreProducto,
            Cantidad, PrecioUnitario, Descuento, Importe, Observacion
        )
        SELECT
            @IdGenerado, D.IdProducto, PR.Codigo, PR.NombreProducto,
            D.Cantidad, D.PrecioUnitario, D.Descuento, D.Importe, D.Observacion
        FROM dbo.ProformaDetalle D
        INNER JOIN dbo.Productos PR ON PR.IdProducto = D.IdProducto
        WHERE D.IdProforma = @IdProforma;

        UPDATE dbo.Proformas
        SET TieneOrdenCompraInterna = 1,
            Estado = 'Registrado'
        WHERE IdProforma = @IdProforma;

        COMMIT TRANSACTION;
        SET @Resultado = 1;
        SET @Mensaje = CONCAT('Orden de compra interna ', @NumeroOci, ' generada correctamente.');
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH;
END;
GO
