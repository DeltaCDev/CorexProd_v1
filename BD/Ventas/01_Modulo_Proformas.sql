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
        IgvPorcentaje DECIMAL(9,4) NOT NULL DEFAULT(0),
        CondicionTributaria VARCHAR(50) NOT NULL DEFAULT('Exonerado de IGV'),
        Total DECIMAL(18,2) NOT NULL DEFAULT(0),
        Estado VARCHAR(20) NOT NULL DEFAULT('Emitido'),
        TieneOrdenCompraInterna BIT NOT NULL DEFAULT(0),
        FechaRegistro DATETIME NOT NULL DEFAULT(GETDATE())
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Parametros WHERE CodigoParametro = 'IGV_PORCENTAJE')
    INSERT INTO dbo.Parametros (CodigoParametro, NombreParametro, ValorParametro, Descripcion, Estado)
    VALUES ('IGV_PORCENTAJE', 'Porcentaje de IGV', '18', 'Porcentaje de IGV aplicado a nuevos documentos.', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Parametros WHERE CodigoParametro = 'IGV_ACTIVO')
    INSERT INTO dbo.Parametros (CodigoParametro, NombreParametro, ValorParametro, Descripcion, Estado)
    VALUES ('IGV_ACTIVO', 'IGV activo', '1', 'Indica si los nuevos documentos aplican IGV: 1 activo, 0 inactivo.', 1);
GO

IF COL_LENGTH('dbo.Proformas', 'IgvPorcentaje') IS NULL
    ALTER TABLE dbo.Proformas ADD IgvPorcentaje DECIMAL(9,4) NOT NULL
        CONSTRAINT DF_Proformas_IgvPorcentaje DEFAULT(0) WITH VALUES;
GO

IF COL_LENGTH('dbo.Proformas', 'CondicionTributaria') IS NULL
    ALTER TABLE dbo.Proformas ADD CondicionTributaria VARCHAR(50) NOT NULL
        CONSTRAINT DF_Proformas_CondicionTributaria DEFAULT('Exonerado de IGV') WITH VALUES;
GO

UPDATE dbo.Proformas
SET IgvPorcentaje = CASE WHEN Subtotal <> 0 THEN ROUND(Igv * 100 / Subtotal, 4) ELSE IgvPorcentaje END,
    CondicionTributaria = 'Gravado con IGV'
WHERE Igv > 0 AND IgvPorcentaje = 0;
GO

DECLARE @RestriccionEstadoProforma SYSNAME;

SELECT @RestriccionEstadoProforma = DC.name
FROM sys.default_constraints DC
INNER JOIN sys.columns C
    ON C.object_id = DC.parent_object_id
   AND C.column_id = DC.parent_column_id
WHERE DC.parent_object_id = OBJECT_ID('dbo.Proformas')
  AND C.name = 'Estado';

IF @RestriccionEstadoProforma IS NOT NULL
BEGIN
    DECLARE @SqlEstadoProforma NVARCHAR(500) =
        N'ALTER TABLE dbo.Proformas DROP CONSTRAINT ' + QUOTENAME(@RestriccionEstadoProforma) + N';';

    EXEC sys.sp_executesql @SqlEstadoProforma;
END

ALTER TABLE dbo.Proformas
ADD CONSTRAINT DF_Proformas_Estado DEFAULT('Emitido') FOR Estado;
GO

IF COL_LENGTH('dbo.Proformas', 'UsuarioGenerador') IS NULL
BEGIN
    ALTER TABLE dbo.Proformas
    ADD UsuarioGenerador VARCHAR(80) NOT NULL
        CONSTRAINT DF_Proformas_UsuarioGenerador DEFAULT('Sistema');
END
GO

IF COL_LENGTH('dbo.Proformas', 'MotivoAnulacion') IS NULL
BEGIN
    ALTER TABLE dbo.Proformas
    ADD MotivoAnulacion VARCHAR(500) NULL;
END
GO

IF COL_LENGTH('dbo.Proformas', 'UsuarioAnulacion') IS NULL
BEGIN
    ALTER TABLE dbo.Proformas
    ADD UsuarioAnulacion VARCHAR(80) NULL;
END
GO

IF COL_LENGTH('dbo.Proformas', 'FechaAnulacion') IS NULL
BEGIN
    ALTER TABLE dbo.Proformas
    ADD FechaAnulacion DATETIME NULL;
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

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
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
        P.IgvPorcentaje,
        P.CondicionTributaria,
        P.Total,
        P.Estado,
        P.TieneOrdenCompraInterna,
        P.UsuarioGenerador,
        P.MotivoAnulacion,
        P.UsuarioAnulacion,
        P.FechaAnulacion,
        P.FechaRegistro
    FROM dbo.Proformas P
    INNER JOIN dbo.Clientes C ON C.IdCliente = P.IdCliente
    ORDER BY P.IdProforma DESC;
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
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
        P.IgvPorcentaje,
        P.CondicionTributaria,
        P.Total,
        P.Estado,
        P.TieneOrdenCompraInterna,
        P.UsuarioGenerador,
        P.MotivoAnulacion,
        P.UsuarioAnulacion,
        P.FechaAnulacion,
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

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
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
    @IgvPorcentaje DECIMAL(9,4),
    @CondicionTributaria VARCHAR(50),
    @Total DECIMAL(18,2),
    @DetallesXml XML,
    @UsuarioGenerador VARCHAR(80),
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
            DECLARE @Correlativo BIGINT;
            DECLARE @Numero VARCHAR(30);
            EXEC dbo.USP_SEG_SERIE_TOMAR_SIGUIENTE
                @CodigoTipoDocumento='PROFORMA', @Serie=@Serie OUTPUT,
                @Correlativo=@Correlativo OUTPUT, @Numero=@Numero OUTPUT;
            SET @SerieNumero = CONCAT(@Serie, '-', @Numero);

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
                IgvPorcentaje,
                CondicionTributaria,
                Total,
                UsuarioGenerador,
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
                @IgvPorcentaje,
                @CondicionTributaria,
                @Total,
                ISNULL(NULLIF(@UsuarioGenerador, ''), 'Sistema'),
                'Emitido'
            );

            SET @IdGenerado = SCOPE_IDENTITY();

        END
        ELSE
        BEGIN
            IF EXISTS (SELECT 1 FROM dbo.Proformas WHERE IdProforma = @IdProforma AND Estado = 'Anulado')
            BEGIN
                SET @Mensaje = 'No se puede editar una proforma anulada';
                ROLLBACK TRANSACTION;
                RETURN;
            END

            IF EXISTS
            (
                SELECT 1
                FROM dbo.Proformas WITH (UPDLOCK, HOLDLOCK)
                WHERE IdProforma = @IdProforma
                  AND TieneOrdenCompraInterna = 1
            )
            BEGIN
                SET @Mensaje = 'La proforma ya tiene una OCI emitida y solo esta disponible para consulta';
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
                IgvPorcentaje = @IgvPorcentaje,
                CondicionTributaria = @CondicionTributaria,
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

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE dbo.USP_VEN_PROFORMA_ANULAR
    @IdProforma INT,
    @MotivoAnulacion VARCHAR(500),
    @UsuarioAnulacion VARCHAR(80),
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @Resultado = 0;
    SET @MotivoAnulacion = LTRIM(RTRIM(ISNULL(@MotivoAnulacion, '')));
    SET @UsuarioAnulacion = LTRIM(RTRIM(ISNULL(@UsuarioAnulacion, '')));

    IF @MotivoAnulacion = ''
    BEGIN
        SET @Mensaje = 'Debe ingresar el motivo de anulacion';
        RETURN;
    END

    IF @UsuarioAnulacion = ''
    BEGIN
        SET @Mensaje = 'No se pudo identificar al usuario de la sesion';
        RETURN;
    END

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

    IF EXISTS (SELECT 1 FROM dbo.Proformas WHERE IdProforma = @IdProforma AND Estado = 'Anulado')
    BEGIN
        SET @Mensaje = 'La proforma ya se encuentra anulada';
        RETURN;
    END

    UPDATE dbo.Proformas
    SET Estado = 'Anulado',
        MotivoAnulacion = @MotivoAnulacion,
        UsuarioAnulacion = @UsuarioAnulacion,
        FechaAnulacion = GETDATE()
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
