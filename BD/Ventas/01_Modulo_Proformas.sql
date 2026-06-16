IF OBJECT_ID('dbo.ProformaDetalle', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProformaDetalle
    (
        IdProformaDetalle INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdProforma INT NOT NULL,
        IdProducto INT NOT NULL,
        Cantidad DECIMAL(18,2) NOT NULL,
        PrecioUnitario DECIMAL(18,2) NOT NULL DEFAULT(0),
        Descuento DECIMAL(18,2) NOT NULL DEFAULT(0),
        Importe DECIMAL(18,2) NOT NULL DEFAULT(0),
        Observacion VARCHAR(500) NOT NULL DEFAULT(''),
        FechaRegistro DATETIME NOT NULL DEFAULT(GETDATE())
    );
END
GO

IF OBJECT_ID('dbo.Proformas', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Proformas
    (
        IdProforma INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SerieNumero VARCHAR(40) NOT NULL,
        FechaEmision DATE NOT NULL,
        FechaVencimiento DATE NOT NULL,
        OrdenCompraCliente VARCHAR(100) NOT NULL DEFAULT(''),
        IdCliente INT NOT NULL,
        Observacion VARCHAR(1000) NOT NULL DEFAULT(''),
        Subtotal DECIMAL(18,2) NOT NULL DEFAULT(0),
        Descuento DECIMAL(18,2) NOT NULL DEFAULT(0),
        Igv DECIMAL(18,2) NOT NULL DEFAULT(0),
        Total DECIMAL(18,2) NOT NULL DEFAULT(0),
        Estado VARCHAR(20) NOT NULL DEFAULT('Registrado'),
        TieneOrdenCompraInterna BIT NOT NULL DEFAULT(0),
        FechaRegistro DATETIME NOT NULL DEFAULT(GETDATE())
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_Proformas_Clientes'
)
BEGIN
    ALTER TABLE dbo.Proformas
    ADD CONSTRAINT FK_Proformas_Clientes
    FOREIGN KEY (IdCliente) REFERENCES dbo.Clientes(IdCliente);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_ProformaDetalle_Proformas'
)
BEGIN
    ALTER TABLE dbo.ProformaDetalle
    ADD CONSTRAINT FK_ProformaDetalle_Proformas
    FOREIGN KEY (IdProforma) REFERENCES dbo.Proformas(IdProforma);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_ProformaDetalle_Productos'
)
BEGIN
    ALTER TABLE dbo.ProformaDetalle
    ADD CONSTRAINT FK_ProformaDetalle_Productos
    FOREIGN KEY (IdProducto) REFERENCES dbo.Productos(IdProducto);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Parametros WHERE CodigoParametro = 'PROFORMA_SERIE')
BEGIN
    INSERT INTO dbo.Parametros(CodigoParametro, NombreParametro, ValorParametro, Descripcion)
    VALUES ('PROFORMA_SERIE', 'Serie de proforma', 'PF', 'Serie usada para generar el ID de proforma');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Parametros WHERE CodigoParametro = 'PROFORMA_CORRELATIVO')
BEGIN
    INSERT INTO dbo.Parametros(CodigoParametro, NombreParametro, ValorParametro, Descripcion)
    VALUES ('PROFORMA_CORRELATIVO', 'Correlativo de proforma', '1', 'Siguiente correlativo para generar proformas');
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_VEN_PROFORMA_LISTAR
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        P.IdProforma,
        P.SerieNumero,
        P.FechaEmision,
        P.FechaVencimiento,
        P.OrdenCompraCliente,
        P.IdCliente,
        C.NombreRazonSocial AS NombreCliente,
        P.Observacion,
        P.Subtotal,
        P.Descuento,
        P.Igv,
        P.Total,
        P.Estado,
        P.TieneOrdenCompraInterna,
        P.FechaRegistro
    FROM dbo.Proformas P
    INNER JOIN dbo.Clientes C ON C.IdCliente = P.IdCliente
    ORDER BY P.IdProforma DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_VEN_PROFORMA_OBTENER
    @IdProforma INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        P.IdProforma,
        P.SerieNumero,
        P.FechaEmision,
        P.FechaVencimiento,
        P.OrdenCompraCliente,
        P.IdCliente,
        C.NombreRazonSocial AS NombreCliente,
        P.Observacion,
        P.Subtotal,
        P.Descuento,
        P.Igv,
        P.Total,
        P.Estado,
        P.TieneOrdenCompraInterna,
        P.FechaRegistro
    FROM dbo.Proformas P
    INNER JOIN dbo.Clientes C ON C.IdCliente = P.IdCliente
    WHERE P.IdProforma = @IdProforma;

    SELECT
        D.IdProformaDetalle,
        D.IdProforma,
        D.IdProducto,
        PR.Codigo AS CodigoProducto,
        PR.NombreProducto,
        D.Cantidad,
        D.PrecioUnitario,
        D.Descuento,
        D.Importe,
        D.Observacion,
        D.FechaRegistro
    FROM dbo.ProformaDetalle D
    INNER JOIN dbo.Productos PR ON PR.IdProducto = D.IdProducto
    WHERE D.IdProforma = @IdProforma
    ORDER BY D.IdProformaDetalle ASC;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_VEN_PROFORMA_GUARDAR
    @IdProforma INT,
    @FechaEmision DATE,
    @FechaVencimiento DATE,
    @OrdenCompraCliente VARCHAR(100),
    @IdCliente INT,
    @Observacion VARCHAR(1000),
    @Subtotal DECIMAL(18,2),
    @Descuento DECIMAL(18,2),
    @Igv DECIMAL(18,2),
    @Total DECIMAL(18,2),
    @DetallesXml XML,
    @IdGenerado INT OUTPUT,
    @SerieNumero VARCHAR(40) OUTPUT,
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @Resultado = 0;
    SET @Mensaje = '';
    SET @IdGenerado = @IdProforma;

    IF NOT EXISTS (SELECT 1 FROM dbo.Clientes WHERE IdCliente = @IdCliente AND Estado = 1)
    BEGIN
        SET @Mensaje = 'Debe seleccionar un cliente activo';
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM @DetallesXml.nodes('/Detalles/Detalle') X(D))
    BEGIN
        SET @Mensaje = 'Debe agregar al menos un producto';
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        IF @IdProforma = 0
        BEGIN
            DECLARE @Serie VARCHAR(20);
            DECLARE @Correlativo INT;

            SELECT @Serie = ValorParametro
            FROM dbo.Parametros WITH (UPDLOCK, HOLDLOCK)
            WHERE CodigoParametro = 'PROFORMA_SERIE';

            SELECT @Correlativo = TRY_CONVERT(INT, ValorParametro)
            FROM dbo.Parametros WITH (UPDLOCK, HOLDLOCK)
            WHERE CodigoParametro = 'PROFORMA_CORRELATIVO';

            SET @Serie = ISNULL(NULLIF(@Serie, ''), 'PF');
            SET @Correlativo = ISNULL(NULLIF(@Correlativo, 0), 1);
            SET @SerieNumero = CONCAT(@Serie, '-', FORMAT(@Correlativo, '000000'));

            INSERT INTO dbo.Proformas
            (
                SerieNumero,
                FechaEmision,
                FechaVencimiento,
                OrdenCompraCliente,
                IdCliente,
                Observacion,
                Subtotal,
                Descuento,
                Igv,
                Total,
                Estado
            )
            VALUES
            (
                @SerieNumero,
                @FechaEmision,
                @FechaVencimiento,
                ISNULL(@OrdenCompraCliente, ''),
                @IdCliente,
                ISNULL(@Observacion, ''),
                @Subtotal,
                @Descuento,
                @Igv,
                @Total,
                'Registrado'
            );

            SET @IdGenerado = SCOPE_IDENTITY();

            UPDATE dbo.Parametros
            SET ValorParametro = CONVERT(VARCHAR(20), @Correlativo + 1)
            WHERE CodigoParametro = 'PROFORMA_CORRELATIVO';
        END
        ELSE
        BEGIN
            IF EXISTS (SELECT 1 FROM dbo.Proformas WHERE IdProforma = @IdProforma AND Estado = 'Anulado')
            BEGIN
                SET @Mensaje = 'No se puede editar una proforma anulada';
                ROLLBACK TRANSACTION;
                RETURN;
            END

            SELECT @SerieNumero = SerieNumero
            FROM dbo.Proformas
            WHERE IdProforma = @IdProforma;

            UPDATE dbo.Proformas
            SET
                FechaEmision = @FechaEmision,
                FechaVencimiento = @FechaVencimiento,
                OrdenCompraCliente = ISNULL(@OrdenCompraCliente, ''),
                IdCliente = @IdCliente,
                Observacion = ISNULL(@Observacion, ''),
                Subtotal = @Subtotal,
                Descuento = @Descuento,
                Igv = @Igv,
                Total = @Total
            WHERE IdProforma = @IdProforma;

            DELETE FROM dbo.ProformaDetalle
            WHERE IdProforma = @IdProforma;
        END

        INSERT INTO dbo.ProformaDetalle
        (
            IdProforma,
            IdProducto,
            Cantidad,
            PrecioUnitario,
            Descuento,
            Importe,
            Observacion
        )
        SELECT
            @IdGenerado,
            X.D.value('@IdProducto', 'INT'),
            X.D.value('@Cantidad', 'DECIMAL(18,2)'),
            X.D.value('@PrecioUnitario', 'DECIMAL(18,2)'),
            X.D.value('@Descuento', 'DECIMAL(18,2)'),
            X.D.value('@Importe', 'DECIMAL(18,2)'),
            X.D.value('@Observacion', 'VARCHAR(500)')
        FROM @DetallesXml.nodes('/Detalles/Detalle') X(D);

        COMMIT TRANSACTION;

        SET @Resultado = 1;
        SET @Mensaje = CASE WHEN @IdProforma = 0 THEN 'Proforma registrada correctamente' ELSE 'Proforma actualizada correctamente' END;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SET @Mensaje = ERROR_MESSAGE();
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_VEN_PROFORMA_ANULAR
    @IdProforma INT,
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @Resultado = 0;

    IF NOT EXISTS (SELECT 1 FROM dbo.Proformas WHERE IdProforma = @IdProforma)
    BEGIN
        SET @Mensaje = 'No se encontro la proforma';
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.Proformas WHERE IdProforma = @IdProforma AND TieneOrdenCompraInterna = 1)
    BEGIN
        SET @Mensaje = 'No se puede anular porque ya tiene orden de compra interna';
        RETURN;
    END

    UPDATE dbo.Proformas
    SET Estado = 'Anulado'
    WHERE IdProforma = @IdProforma;

    SET @Resultado = 1;
    SET @Mensaje = 'Proforma anulada correctamente';
END
GO

DECLARE @IdMenuVentas INT;

IF NOT EXISTS (SELECT 1 FROM dbo.Menu WHERE NombreMenu = 'Ventas' AND IdMenuPadre IS NULL)
BEGIN
    INSERT INTO dbo.Menu(NombreMenu, IdMenuPadre, Orden, Estado)
    VALUES ('Ventas', NULL, 1, 1);
END

SELECT @IdMenuVentas = IdMenu
FROM dbo.Menu
WHERE NombreMenu = 'Ventas'
AND IdMenuPadre IS NULL;

IF @IdMenuVentas IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM dbo.Menu WHERE NombreMenu = 'Proformas' AND IdMenuPadre = @IdMenuVentas)
BEGIN
    INSERT INTO dbo.Menu(NombreMenu, IdMenuPadre, Orden, Estado)
    VALUES ('Proformas', @IdMenuVentas, 1, 1);
END

INSERT INTO dbo.PermisosMenu(IdRol, IdMenu, PuedeVer)
SELECT R.IdRol, M.IdMenu, 1
FROM dbo.Roles R
CROSS JOIN dbo.Menu M
WHERE M.NombreMenu IN ('Ventas', 'Proformas')
AND NOT EXISTS
(
    SELECT 1
    FROM dbo.PermisosMenu PM
    WHERE PM.IdRol = R.IdRol
    AND PM.IdMenu = M.IdMenu
);
GO
