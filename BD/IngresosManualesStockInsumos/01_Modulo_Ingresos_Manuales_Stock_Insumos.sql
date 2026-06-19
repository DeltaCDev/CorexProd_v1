USE CorexProdDB;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.Proveedores', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Proveedores
    (
        IdProveedor INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Proveedores PRIMARY KEY,
        TipoDocumento VARCHAR(30) NOT NULL,
        NumeroDocumento VARCHAR(20) NOT NULL,
        NombreRazonSocial VARCHAR(180) NOT NULL,
        Direccion VARCHAR(200) NULL,
        Telefono VARCHAR(30) NULL,
        Correo VARCHAR(120) NULL,
        Estado BIT NOT NULL CONSTRAINT DF_Proveedores_Estado DEFAULT (1),
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_Proveedores_FechaRegistro DEFAULT (GETDATE())
    );
END
GO

IF OBJECT_ID('dbo.Almacenes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Almacenes
    (
        IdAlmacen INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Almacenes PRIMARY KEY,
        NombreAlmacen VARCHAR(100) NOT NULL,
        Estado BIT NOT NULL CONSTRAINT DF_Almacenes_Estado DEFAULT (1),
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_Almacenes_FechaRegistro DEFAULT (GETDATE())
    );

    INSERT INTO dbo.Almacenes (NombreAlmacen)
    VALUES ('Almacen Principal');
END
GO

IF OBJECT_ID('dbo.TiposDocumentoStock', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TiposDocumentoStock
    (
        IdTipoDocumento INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TiposDocumentoStock PRIMARY KEY,
        NombreTipoDocumento VARCHAR(80) NOT NULL,
        Estado BIT NOT NULL CONSTRAINT DF_TiposDocumentoStock_Estado DEFAULT (1)
    );

    INSERT INTO dbo.TiposDocumentoStock (NombreTipoDocumento)
    VALUES ('Factura'), ('Boleta'), ('Guia de remision'), ('Nota de ingreso'), ('Documento interno'), ('Sin documento');
END
GO

IF OBJECT_ID('dbo.SerieIngresoManualStockInsumo', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SerieIngresoManualStockInsumo
    (
        IdSerieIngresoManualStockInsumo INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SerieIngresoManualStockInsumo PRIMARY KEY,
        Serie VARCHAR(20) NOT NULL,
        UltimoNumero INT NOT NULL CONSTRAINT DF_SerieIngresoManualStockInsumo_UltimoNumero DEFAULT (0),
        Estado BIT NOT NULL CONSTRAINT DF_SerieIngresoManualStockInsumo_Estado DEFAULT (1)
    );

    INSERT INTO dbo.SerieIngresoManualStockInsumo (Serie, UltimoNumero)
    VALUES ('ISI01', 0);
END
GO

IF OBJECT_ID('dbo.StockInsumosAlmacen', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockInsumosAlmacen
    (
        IdStockInsumoAlmacen INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StockInsumosAlmacen PRIMARY KEY,
        IdInsumo INT NOT NULL,
        IdAlmacen INT NOT NULL,
        StockActual DECIMAL(18, 2) NOT NULL CONSTRAINT DF_StockInsumosAlmacen_StockActual DEFAULT (0),
        FechaActualizacion DATETIME NOT NULL CONSTRAINT DF_StockInsumosAlmacen_FechaActualizacion DEFAULT (GETDATE()),
        CONSTRAINT UQ_StockInsumosAlmacen UNIQUE (IdInsumo, IdAlmacen),
        CONSTRAINT FK_StockInsumosAlmacen_Insumos FOREIGN KEY (IdInsumo) REFERENCES dbo.Insumos(IdInsumo),
        CONSTRAINT FK_StockInsumosAlmacen_Almacenes FOREIGN KEY (IdAlmacen) REFERENCES dbo.Almacenes(IdAlmacen)
    );
END
GO

IF OBJECT_ID('dbo.IngresosManualesStockInsumos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.IngresosManualesStockInsumos
    (
        IdIngresoManualStockInsumo INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_IngresosManualesStockInsumos PRIMARY KEY,
        FechaEmision DATE NOT NULL,
        IdProveedor INT NOT NULL,
        IdTipoDocumento INT NOT NULL,
        TipoNumeracion VARCHAR(20) NOT NULL,
        Serie VARCHAR(20) NOT NULL,
        Numero VARCHAR(30) NOT NULL,
        NumeroDocumento VARCHAR(60) NOT NULL,
        IdAlmacen INT NOT NULL,
        Observacion VARCHAR(500) NULL,
        Estado VARCHAR(20) NOT NULL,
        Subtotal DECIMAL(18, 2) NOT NULL,
        DescuentoTotal DECIMAL(18, 2) NOT NULL,
        Total DECIMAL(18, 2) NOT NULL,
        UsuarioCreador VARCHAR(100) NOT NULL,
        FechaCreacion DATETIME NOT NULL,
        UsuarioAbastecimiento VARCHAR(100) NULL,
        FechaAbastecimiento DATETIME NULL,
        UsuarioAnulacion VARCHAR(100) NULL,
        FechaAnulacion DATETIME NULL,
        MotivoAnulacion VARCHAR(500) NULL,
        CONSTRAINT FK_IngresosManualesStockInsumos_Proveedor FOREIGN KEY (IdProveedor) REFERENCES dbo.Proveedores(IdProveedor),
        CONSTRAINT FK_IngresosManualesStockInsumos_TipoDocumento FOREIGN KEY (IdTipoDocumento) REFERENCES dbo.TiposDocumentoStock(IdTipoDocumento),
        CONSTRAINT FK_IngresosManualesStockInsumos_Almacen FOREIGN KEY (IdAlmacen) REFERENCES dbo.Almacenes(IdAlmacen)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_IngresosManualesStockInsumos_Documento' AND object_id = OBJECT_ID('dbo.IngresosManualesStockInsumos'))
BEGIN
    CREATE UNIQUE INDEX UX_IngresosManualesStockInsumos_Documento
    ON dbo.IngresosManualesStockInsumos (IdProveedor, IdTipoDocumento, Serie, Numero)
    WHERE Estado <> 'Anulado';
END
GO

IF OBJECT_ID('dbo.IngresosManualesStockInsumosDetalle', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.IngresosManualesStockInsumosDetalle
    (
        IdIngresoManualStockInsumoDetalle INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_IngresosManualesStockInsumosDetalle PRIMARY KEY,
        IdIngresoManualStockInsumo INT NOT NULL,
        IdInsumo INT NOT NULL,
        CodigoInsumo VARCHAR(50) NOT NULL,
        IdUnidadMedida INT NOT NULL,
        Cantidad DECIMAL(18, 2) NOT NULL,
        PrecioUnitario DECIMAL(18, 2) NOT NULL,
        Descuento DECIMAL(18, 2) NOT NULL,
        Importe DECIMAL(18, 2) NOT NULL,
        CONSTRAINT FK_IngresosManualesStockInsumosDetalle_Cabecera FOREIGN KEY (IdIngresoManualStockInsumo) REFERENCES dbo.IngresosManualesStockInsumos(IdIngresoManualStockInsumo),
        CONSTRAINT FK_IngresosManualesStockInsumosDetalle_Insumo FOREIGN KEY (IdInsumo) REFERENCES dbo.Insumos(IdInsumo),
        CONSTRAINT FK_IngresosManualesStockInsumosDetalle_Unidad FOREIGN KEY (IdUnidadMedida) REFERENCES dbo.UnidadesMedida(IdUnidadMedida)
    );
END
GO

IF OBJECT_ID('dbo.KardexInsumos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.KardexInsumos
    (
        IdKardexInsumo INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_KardexInsumos PRIMARY KEY,
        TipoMovimiento VARCHAR(50) NOT NULL,
        IdIngresoManualStockInsumo INT NULL,
        IdInsumo INT NOT NULL,
        IdAlmacen INT NOT NULL,
        StockAnterior DECIMAL(18, 2) NOT NULL,
        Cantidad DECIMAL(18, 2) NOT NULL,
        StockResultante DECIMAL(18, 2) NOT NULL,
        UsuarioResponsable VARCHAR(100) NOT NULL,
        FechaMovimiento DATETIME NOT NULL,
        Observacion VARCHAR(500) NULL
    );
END
GO

IF OBJECT_ID('dbo.IngresosManualesStockInsumosAnulaciones', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.IngresosManualesStockInsumosAnulaciones
    (
        IdAnulacionIngresoManualStockInsumo INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_IngresosManualesStockInsumosAnulaciones PRIMARY KEY,
        IdIngresoManualStockInsumo INT NOT NULL,
        EstadoAnterior VARCHAR(20) NOT NULL,
        EstadoNuevo VARCHAR(20) NOT NULL,
        UsuarioAnulacion VARCHAR(100) NOT NULL,
        FechaAnulacion DATETIME NOT NULL,
        Motivo VARCHAR(500) NOT NULL,
        InsumosAfectados VARCHAR(MAX) NULL,
        CONSTRAINT FK_IngresosManualesStockInsumosAnulaciones_Cabecera FOREIGN KEY (IdIngresoManualStockInsumo) REFERENCES dbo.IngresosManualesStockInsumos(IdIngresoManualStockInsumo)
    );
END
GO

IF TYPE_ID('dbo.IngresoManualStockInsumoDetalleType') IS NULL
BEGIN
    CREATE TYPE dbo.IngresoManualStockInsumoDetalleType AS TABLE
    (
        IdInsumo INT NOT NULL,
        CodigoInsumo VARCHAR(50) NOT NULL,
        IdUnidadMedida INT NOT NULL,
        Cantidad DECIMAL(18, 2) NOT NULL,
        PrecioUnitario DECIMAL(18, 2) NOT NULL,
        Descuento DECIMAL(18, 2) NOT NULL
    );
END
GO

DECLARE @IdAlmacenPadre INT = (SELECT TOP (1) IdMenu FROM dbo.Menu WHERE NombreMenu LIKE 'Almac%' AND IdMenuPadre IS NULL ORDER BY IdMenu);
UPDATE dbo.Menu
SET NombreMenu = 'Entrada Manual de Insumos'
WHERE NombreMenu = 'Ingresos de Stock de Insumos'
  AND IdMenuPadre = @IdAlmacenPadre;

IF @IdAlmacenPadre IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.Menu WHERE NombreMenu = 'Entrada Manual de Insumos' AND IdMenuPadre = @IdAlmacenPadre)
BEGIN
    INSERT INTO dbo.Menu (NombreMenu, IdMenuPadre, Orden, Estado)
    VALUES ('Entrada Manual de Insumos', @IdAlmacenPadre, 5, 1);
END

INSERT INTO dbo.PermisosMenu (IdRol, IdMenu, PuedeVer)
SELECT R.IdRol, M.IdMenu, 1
FROM dbo.Roles R
CROSS JOIN dbo.Menu M
WHERE M.NombreMenu = 'Entrada Manual de Insumos'
  AND NOT EXISTS
  (
      SELECT 1 FROM dbo.PermisosMenu PM
      WHERE PM.IdRol = R.IdRol AND PM.IdMenu = M.IdMenu
  );
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_PROVEEDOR_LISTAR
AS
BEGIN
    SET NOCOUNT ON;

    SELECT IdProveedor, TipoDocumento, NumeroDocumento, NombreRazonSocial, Direccion, Telefono, Correo, Estado
    FROM dbo.Proveedores
    WHERE Estado = 1
    ORDER BY NombreRazonSocial;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_PROVEEDOR_REGISTRAR_RAPIDO
    @TipoDocumento VARCHAR(30),
    @NumeroDocumento VARCHAR(20),
    @NombreRazonSocial VARCHAR(180),
    @Direccion VARCHAR(200) = NULL,
    @Telefono VARCHAR(30) = NULL,
    @Correo VARCHAR(120) = NULL,
    @IdProveedor INT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @IdProveedor = 0;

    IF NULLIF(LTRIM(RTRIM(@NombreRazonSocial)), '') IS NULL
    BEGIN
        SET @Mensaje = 'Debe ingresar el nombre o razon social del proveedor';
        RETURN;
    END

    SELECT @IdProveedor = IdProveedor
    FROM dbo.Proveedores
    WHERE NumeroDocumento = ISNULL(@NumeroDocumento, '')
      AND NumeroDocumento <> ''
      AND Estado = 1;

    IF @IdProveedor > 0
    BEGIN
        SET @Mensaje = 'El proveedor ya existia y fue seleccionado';
        RETURN;
    END

    INSERT INTO dbo.Proveedores (TipoDocumento, NumeroDocumento, NombreRazonSocial, Direccion, Telefono, Correo)
    VALUES (@TipoDocumento, ISNULL(@NumeroDocumento, ''), @NombreRazonSocial, @Direccion, @Telefono, @Correo);

    SET @IdProveedor = SCOPE_IDENTITY();
    SET @Mensaje = 'Proveedor registrado correctamente';
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_ALMACEN_LISTAR
AS
BEGIN
    SET NOCOUNT ON;

    SELECT IdAlmacen, NombreAlmacen, Estado
    FROM dbo.Almacenes
    WHERE Estado = 1
    ORDER BY NombreAlmacen;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_TIPO_DOCUMENTO_STOCK_LISTAR
AS
BEGIN
    SET NOCOUNT ON;

    SELECT IdTipoDocumento, NombreTipoDocumento, Estado
    FROM dbo.TiposDocumentoStock
    WHERE Estado = 1
    ORDER BY IdTipoDocumento;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_INSUMO_STOCK_BUSCAR
    @IdAlmacen INT,
    @Texto VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (40)
        P.IdInsumo,
        P.Codigo,
        P.NombreInsumo,
        ISNULL(P.Descripcion, '') AS Descripcion,
        P.IdUnidadMedida,
        UM.NombreUnidad,
        CAST(ISNULL(SPA.StockActual, 0) AS DECIMAL(18, 2)) AS StockActual
    FROM dbo.Insumos P
    INNER JOIN dbo.UnidadesMedida UM ON UM.IdUnidadMedida = P.IdUnidadMedida
    LEFT JOIN dbo.StockInsumosAlmacen SPA ON SPA.IdInsumo = P.IdInsumo AND SPA.IdAlmacen = @IdAlmacen
    WHERE P.Estado = 1
      AND
      (
          P.Codigo LIKE '%' + @Texto + '%'
          OR P.NombreInsumo LIKE '%' + @Texto + '%'
          OR ISNULL(P.Descripcion, '') LIKE '%' + @Texto + '%'
      )
    ORDER BY P.NombreInsumo;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_INGRESO_MANUAL_STOCK_INSUMO_LISTAR
    @FechaDesde DATE = NULL,
    @FechaHasta DATE = NULL,
    @IdProveedor INT = NULL,
    @IdAlmacen INT = NULL,
    @Estado VARCHAR(20) = NULL,
    @NumeroDocumento VARCHAR(60) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        I.IdIngresoManualStockInsumo, I.FechaEmision, I.IdProveedor, P.NombreRazonSocial AS NombreProveedor,
        I.IdTipoDocumento, TD.NombreTipoDocumento, I.TipoNumeracion, I.Serie, I.Numero,
        I.NumeroDocumento, I.IdAlmacen, A.NombreAlmacen, ISNULL(I.Observacion, '') AS Observacion,
        I.Estado, I.Subtotal, I.DescuentoTotal, I.Total, I.UsuarioCreador, I.FechaCreacion,
        ISNULL(I.UsuarioAbastecimiento, '') AS UsuarioAbastecimiento, I.FechaAbastecimiento,
        ISNULL(I.UsuarioAnulacion, '') AS UsuarioAnulacion, I.FechaAnulacion,
        ISNULL(I.MotivoAnulacion, '') AS MotivoAnulacion
    FROM dbo.IngresosManualesStockInsumos I
    INNER JOIN dbo.Proveedores P ON P.IdProveedor = I.IdProveedor
    INNER JOIN dbo.TiposDocumentoStock TD ON TD.IdTipoDocumento = I.IdTipoDocumento
    INNER JOIN dbo.Almacenes A ON A.IdAlmacen = I.IdAlmacen
    WHERE (@FechaDesde IS NULL OR I.FechaEmision >= @FechaDesde)
      AND (@FechaHasta IS NULL OR I.FechaEmision <= @FechaHasta)
      AND (@IdProveedor IS NULL OR I.IdProveedor = @IdProveedor)
      AND (@IdAlmacen IS NULL OR I.IdAlmacen = @IdAlmacen)
      AND (@Estado IS NULL OR I.Estado = @Estado)
      AND (@NumeroDocumento IS NULL OR I.NumeroDocumento LIKE '%' + @NumeroDocumento + '%')
    ORDER BY I.FechaEmision DESC, I.IdIngresoManualStockInsumo DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_INGRESO_MANUAL_STOCK_INSUMO_OBTENER
    @IdIngresoManualStockInsumo INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        I.IdIngresoManualStockInsumo, I.FechaEmision, I.IdProveedor, P.NombreRazonSocial AS NombreProveedor,
        I.IdTipoDocumento, TD.NombreTipoDocumento, I.TipoNumeracion, I.Serie, I.Numero,
        I.NumeroDocumento, I.IdAlmacen, A.NombreAlmacen, ISNULL(I.Observacion, '') AS Observacion,
        I.Estado, I.Subtotal, I.DescuentoTotal, I.Total, I.UsuarioCreador, I.FechaCreacion,
        ISNULL(I.UsuarioAbastecimiento, '') AS UsuarioAbastecimiento, I.FechaAbastecimiento,
        ISNULL(I.UsuarioAnulacion, '') AS UsuarioAnulacion, I.FechaAnulacion,
        ISNULL(I.MotivoAnulacion, '') AS MotivoAnulacion
    FROM dbo.IngresosManualesStockInsumos I
    INNER JOIN dbo.Proveedores P ON P.IdProveedor = I.IdProveedor
    INNER JOIN dbo.TiposDocumentoStock TD ON TD.IdTipoDocumento = I.IdTipoDocumento
    INNER JOIN dbo.Almacenes A ON A.IdAlmacen = I.IdAlmacen
    WHERE I.IdIngresoManualStockInsumo = @IdIngresoManualStockInsumo;

    SELECT
        D.IdIngresoManualStockInsumoDetalle, D.IdIngresoManualStockInsumo, D.IdInsumo, D.CodigoInsumo,
        P.NombreInsumo, D.IdUnidadMedida, UM.NombreUnidad,
        CAST(ISNULL(SPA.StockActual, 0) AS DECIMAL(18, 2)) AS StockActual,
        D.Cantidad, D.PrecioUnitario, D.Descuento, D.Importe
    FROM dbo.IngresosManualesStockInsumosDetalle D
    INNER JOIN dbo.IngresosManualesStockInsumos I ON I.IdIngresoManualStockInsumo = D.IdIngresoManualStockInsumo
    INNER JOIN dbo.Insumos P ON P.IdInsumo = D.IdInsumo
    INNER JOIN dbo.UnidadesMedida UM ON UM.IdUnidadMedida = D.IdUnidadMedida
    LEFT JOIN dbo.StockInsumosAlmacen SPA ON SPA.IdInsumo = D.IdInsumo AND SPA.IdAlmacen = I.IdAlmacen
    WHERE D.IdIngresoManualStockInsumo = @IdIngresoManualStockInsumo
    ORDER BY D.IdIngresoManualStockInsumoDetalle;
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_INGRESO_MANUAL_STOCK_INSUMO_GUARDAR
    @IdIngresoManualStockInsumo INT,
    @FechaEmision DATE,
    @IdProveedor INT,
    @IdTipoDocumento INT,
    @TipoNumeracion VARCHAR(20),
    @Serie VARCHAR(20),
    @Numero VARCHAR(30),
    @IdAlmacen INT,
    @Observacion VARCHAR(500),
    @Usuario VARCHAR(100),
    @Detalles dbo.IngresoManualStockInsumoDetalleType READONLY,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM @Detalles)
    BEGIN
        SET @Mensaje = 'Debe agregar al menos un insumo';
        RETURN;
    END

    IF EXISTS (SELECT IdInsumo FROM @Detalles GROUP BY IdInsumo HAVING COUNT(*) > 1)
    BEGIN
        SET @Mensaje = 'No se permiten insumos repetidos';
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM @Detalles WHERE Cantidad <= 0 OR PrecioUnitario < 0 OR Descuento < 0 OR Descuento > (Cantidad * PrecioUnitario))
    BEGIN
        SET @Mensaje = 'Revise cantidades, precios y descuentos del detalle';
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        IF @IdIngresoManualStockInsumo > 0 AND EXISTS (SELECT 1 FROM dbo.IngresosManualesStockInsumos WHERE IdIngresoManualStockInsumo = @IdIngresoManualStockInsumo AND Estado <> 'Pendiente')
        BEGIN
            SET @Mensaje = 'Solo se pueden editar ingresos pendientes';
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF @TipoNumeracion = 'Automatica'
        BEGIN
            IF @IdIngresoManualStockInsumo > 0
            BEGIN
                SELECT @Serie = Serie, @Numero = Numero
                FROM dbo.IngresosManualesStockInsumos
                WHERE IdIngresoManualStockInsumo = @IdIngresoManualStockInsumo;
            END
            ELSE
            BEGIN
                DECLARE @CorrelativoGenerado BIGINT;
                EXEC dbo.USP_SEG_SERIE_TOMAR_SIGUIENTE
                    @CodigoTipoDocumento='INGRESO_INSUMOS', @Serie=@Serie OUTPUT,
                    @Correlativo=@CorrelativoGenerado OUTPUT, @Numero=@Numero OUTPUT;
            END
        END

        IF NULLIF(LTRIM(RTRIM(@Serie)), '') IS NULL OR NULLIF(LTRIM(RTRIM(@Numero)), '') IS NULL
        BEGIN
            SET @Mensaje = 'Debe ingresar serie y numero del documento';
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF EXISTS
        (
            SELECT 1
            FROM dbo.IngresosManualesStockInsumos
            WHERE IdProveedor = @IdProveedor
              AND IdTipoDocumento = @IdTipoDocumento
              AND Serie = @Serie
              AND Numero = @Numero
              AND IdIngresoManualStockInsumo <> @IdIngresoManualStockInsumo
              AND Estado <> 'Anulado'
        )
        BEGIN
            SET @Mensaje = 'Ya existe un documento registrado con el mismo proveedor, tipo, serie y numero';
            ROLLBACK TRANSACTION;
            RETURN;
        END

        DECLARE @Subtotal DECIMAL(18, 2) = (SELECT SUM(Cantidad * PrecioUnitario) FROM @Detalles);
        DECLARE @DescuentoTotal DECIMAL(18, 2) = (SELECT SUM(Descuento) FROM @Detalles);
        DECLARE @Total DECIMAL(18, 2) = (SELECT SUM((Cantidad * PrecioUnitario) - Descuento) FROM @Detalles);
        DECLARE @NumeroDocumento VARCHAR(60) = @Serie + '-' + @Numero;

        IF @IdIngresoManualStockInsumo = 0
        BEGIN
            INSERT INTO dbo.IngresosManualesStockInsumos
            (
                FechaEmision, IdProveedor, IdTipoDocumento, TipoNumeracion, Serie, Numero, NumeroDocumento,
                IdAlmacen, Observacion, Estado, Subtotal, DescuentoTotal, Total, UsuarioCreador, FechaCreacion
            )
            VALUES
            (
                @FechaEmision, @IdProveedor, @IdTipoDocumento, @TipoNumeracion, @Serie, @Numero, @NumeroDocumento,
                @IdAlmacen, @Observacion, 'Pendiente', @Subtotal, @DescuentoTotal, @Total, @Usuario, GETDATE()
            );

            SET @IdIngresoManualStockInsumo = SCOPE_IDENTITY();
        END
        ELSE
        BEGIN
            UPDATE dbo.IngresosManualesStockInsumos
            SET FechaEmision = @FechaEmision,
                IdProveedor = @IdProveedor,
                IdTipoDocumento = @IdTipoDocumento,
                TipoNumeracion = @TipoNumeracion,
                Serie = @Serie,
                Numero = @Numero,
                NumeroDocumento = @NumeroDocumento,
                IdAlmacen = @IdAlmacen,
                Observacion = @Observacion,
                Subtotal = @Subtotal,
                DescuentoTotal = @DescuentoTotal,
                Total = @Total
            WHERE IdIngresoManualStockInsumo = @IdIngresoManualStockInsumo;

            DELETE FROM dbo.IngresosManualesStockInsumosDetalle
            WHERE IdIngresoManualStockInsumo = @IdIngresoManualStockInsumo;
        END

        INSERT INTO dbo.IngresosManualesStockInsumosDetalle
        (
            IdIngresoManualStockInsumo, IdInsumo, CodigoInsumo, IdUnidadMedida,
            Cantidad, PrecioUnitario, Descuento, Importe
        )
        SELECT
            @IdIngresoManualStockInsumo, D.IdInsumo, D.CodigoInsumo, D.IdUnidadMedida,
            D.Cantidad, D.PrecioUnitario, D.Descuento, (D.Cantidad * D.PrecioUnitario) - D.Descuento
        FROM @Detalles D
        INNER JOIN dbo.Insumos P ON P.IdInsumo = D.IdInsumo AND P.Estado = 1;

        COMMIT TRANSACTION;
        SET @Mensaje = 'Ingreso manual de insumos guardado correctamente';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_INGRESO_MANUAL_STOCK_INSUMO_ABASTECER
    @IdIngresoManualStockInsumo INT,
    @Usuario VARCHAR(100),
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @IdAlmacen INT;
        SELECT @IdAlmacen = IdAlmacen
        FROM dbo.IngresosManualesStockInsumos WITH (UPDLOCK, HOLDLOCK)
        WHERE IdIngresoManualStockInsumo = @IdIngresoManualStockInsumo AND Estado = 'Pendiente';

        IF @IdAlmacen IS NULL
        BEGIN
            SET @Mensaje = 'El documento no esta pendiente o no existe';
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF NOT EXISTS (SELECT 1 FROM dbo.IngresosManualesStockInsumosDetalle WHERE IdIngresoManualStockInsumo = @IdIngresoManualStockInsumo)
        BEGIN
            SET @Mensaje = 'El documento no tiene insumos';
            ROLLBACK TRANSACTION;
            RETURN;
        END

        DECLARE c CURSOR LOCAL FAST_FORWARD FOR
            SELECT IdInsumo, Cantidad
            FROM dbo.IngresosManualesStockInsumosDetalle
            WHERE IdIngresoManualStockInsumo = @IdIngresoManualStockInsumo;

        DECLARE @IdInsumo INT, @Cantidad DECIMAL(18, 2), @Anterior DECIMAL(18, 2), @Resultante DECIMAL(18, 2);
        OPEN c;
        FETCH NEXT FROM c INTO @IdInsumo, @Cantidad;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM dbo.StockInsumosAlmacen WHERE IdInsumo = @IdInsumo AND IdAlmacen = @IdAlmacen)
            BEGIN
                INSERT INTO dbo.StockInsumosAlmacen (IdInsumo, IdAlmacen, StockActual)
                VALUES (@IdInsumo, @IdAlmacen, 0);
            END

            SELECT @Anterior = StockActual
            FROM dbo.StockInsumosAlmacen WITH (UPDLOCK, HOLDLOCK)
            WHERE IdInsumo = @IdInsumo AND IdAlmacen = @IdAlmacen;

            SET @Resultante = @Anterior + @Cantidad;

            UPDATE dbo.StockInsumosAlmacen
            SET StockActual = @Resultante, FechaActualizacion = GETDATE()
            WHERE IdInsumo = @IdInsumo AND IdAlmacen = @IdAlmacen;

            IF OBJECT_ID('dbo.StockInsumos', 'U') IS NOT NULL
            BEGIN
                IF EXISTS (SELECT 1 FROM dbo.StockInsumos WHERE IdInsumo = @IdInsumo)
                    UPDATE dbo.StockInsumos SET StockActual = StockActual + @Cantidad, FechaActualizacion = GETDATE() WHERE IdInsumo = @IdInsumo;
                ELSE
                    INSERT INTO dbo.StockInsumos (IdInsumo, StockActual) VALUES (@IdInsumo, @Cantidad);
            END

            INSERT INTO dbo.KardexInsumos
            (
                TipoMovimiento, IdIngresoManualStockInsumo, IdInsumo, IdAlmacen,
                StockAnterior, Cantidad, StockResultante, UsuarioResponsable, FechaMovimiento, Observacion
            )
            VALUES
            (
                'INGRESO_MANUAL_STOCK_INSUMO', @IdIngresoManualStockInsumo, @IdInsumo, @IdAlmacen,
                @Anterior, @Cantidad, @Resultante, @Usuario, GETDATE(), 'Abastecimiento de ingreso manual de insumos'
            );

            FETCH NEXT FROM c INTO @IdInsumo, @Cantidad;
        END

        CLOSE c;
        DEALLOCATE c;

        UPDATE dbo.IngresosManualesStockInsumos
        SET Estado = 'Abastecido',
            UsuarioAbastecimiento = @Usuario,
            FechaAbastecimiento = GETDATE()
        WHERE IdIngresoManualStockInsumo = @IdIngresoManualStockInsumo;

        COMMIT TRANSACTION;
        SET @Mensaje = 'Ingreso manual de insumos abastecido correctamente';
    END TRY
    BEGIN CATCH
        IF CURSOR_STATUS('local', 'c') >= -1
        BEGIN
            CLOSE c;
            DEALLOCATE c;
        END
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_INGRESO_MANUAL_STOCK_INSUMO_ANULAR
    @IdIngresoManualStockInsumo INT,
    @Usuario VARCHAR(100),
    @Motivo VARCHAR(500),
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NULLIF(LTRIM(RTRIM(@Motivo)), '') IS NULL
    BEGIN
        SET @Mensaje = 'Debe ingresar el motivo de anulacion';
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @Estado VARCHAR(20), @IdAlmacen INT, @FechaAbastecimiento DATETIME;
        SELECT @Estado = Estado, @IdAlmacen = IdAlmacen, @FechaAbastecimiento = FechaAbastecimiento
        FROM dbo.IngresosManualesStockInsumos WITH (UPDLOCK, HOLDLOCK)
        WHERE IdIngresoManualStockInsumo = @IdIngresoManualStockInsumo;

        IF @Estado IS NULL OR @Estado = 'Anulado'
        BEGIN
            SET @Mensaje = 'El documento no existe o ya esta anulado';
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF @Estado = 'Abastecido'
        BEGIN
            IF EXISTS
            (
                SELECT 1
                FROM dbo.IngresosManualesStockInsumosDetalle D
                INNER JOIN dbo.KardexInsumos K ON K.IdInsumo = D.IdInsumo AND K.IdAlmacen = @IdAlmacen
                WHERE D.IdIngresoManualStockInsumo = @IdIngresoManualStockInsumo
                  AND K.IdIngresoManualStockInsumo <> @IdIngresoManualStockInsumo
                  AND K.FechaMovimiento > ISNULL(@FechaAbastecimiento, '19000101')
            )
            BEGIN
                SET @Mensaje = 'No se puede anular porque existen movimientos posteriores en el kardex';
                ROLLBACK TRANSACTION;
                RETURN;
            END

            IF EXISTS
            (
                SELECT 1
                FROM dbo.IngresosManualesStockInsumosDetalle D
                LEFT JOIN dbo.StockInsumosAlmacen SPA ON SPA.IdInsumo = D.IdInsumo AND SPA.IdAlmacen = @IdAlmacen
                WHERE D.IdIngresoManualStockInsumo = @IdIngresoManualStockInsumo
                  AND ISNULL(SPA.StockActual, 0) < D.Cantidad
            )
            BEGIN
                SET @Mensaje = 'No se puede anular porque el stock disponible es insuficiente';
                ROLLBACK TRANSACTION;
                RETURN;
            END

            DECLARE c CURSOR LOCAL FAST_FORWARD FOR
                SELECT IdInsumo, Cantidad
                FROM dbo.IngresosManualesStockInsumosDetalle
                WHERE IdIngresoManualStockInsumo = @IdIngresoManualStockInsumo;

            DECLARE @IdInsumo INT, @Cantidad DECIMAL(18, 2), @Anterior DECIMAL(18, 2), @Resultante DECIMAL(18, 2);
            OPEN c;
            FETCH NEXT FROM c INTO @IdInsumo, @Cantidad;

            WHILE @@FETCH_STATUS = 0
            BEGIN
                SELECT @Anterior = StockActual
                FROM dbo.StockInsumosAlmacen WITH (UPDLOCK, HOLDLOCK)
                WHERE IdInsumo = @IdInsumo AND IdAlmacen = @IdAlmacen;

                SET @Resultante = @Anterior - @Cantidad;

                UPDATE dbo.StockInsumosAlmacen
                SET StockActual = @Resultante, FechaActualizacion = GETDATE()
                WHERE IdInsumo = @IdInsumo AND IdAlmacen = @IdAlmacen;

                IF OBJECT_ID('dbo.StockInsumos', 'U') IS NOT NULL
                BEGIN
                    UPDATE dbo.StockInsumos
                    SET StockActual = StockActual - @Cantidad,
                        FechaActualizacion = GETDATE()
                    WHERE IdInsumo = @IdInsumo;
                END

                INSERT INTO dbo.KardexInsumos
                (
                    TipoMovimiento, IdIngresoManualStockInsumo, IdInsumo, IdAlmacen,
                    StockAnterior, Cantidad, StockResultante, UsuarioResponsable, FechaMovimiento, Observacion
                )
                VALUES
                (
                    'ANULACION_INGRESO_MANUAL_INSUMO', @IdIngresoManualStockInsumo, @IdInsumo, @IdAlmacen,
                    @Anterior, -@Cantidad, @Resultante, @Usuario, GETDATE(), @Motivo
                );

                FETCH NEXT FROM c INTO @IdInsumo, @Cantidad;
            END

            CLOSE c;
            DEALLOCATE c;
        END

        UPDATE dbo.IngresosManualesStockInsumos
        SET Estado = 'Anulado',
            UsuarioAnulacion = @Usuario,
            FechaAnulacion = GETDATE(),
            MotivoAnulacion = @Motivo
        WHERE IdIngresoManualStockInsumo = @IdIngresoManualStockInsumo;

        INSERT INTO dbo.IngresosManualesStockInsumosAnulaciones
        (
            IdIngresoManualStockInsumo, EstadoAnterior, EstadoNuevo, UsuarioAnulacion,
            FechaAnulacion, Motivo, InsumosAfectados
        )
        SELECT
            @IdIngresoManualStockInsumo, @Estado, 'Anulado', @Usuario,
            GETDATE(), @Motivo,
            STRING_AGG(CONCAT(P.NombreInsumo, ' x ', D.Cantidad), '; ')
        FROM dbo.IngresosManualesStockInsumosDetalle D
        INNER JOIN dbo.Insumos P ON P.IdInsumo = D.IdInsumo
        WHERE D.IdIngresoManualStockInsumo = @IdIngresoManualStockInsumo;

        COMMIT TRANSACTION;
        SET @Mensaje = 'Ingreso manual de insumos anulado correctamente';
    END TRY
    BEGIN CATCH
        IF CURSOR_STATUS('local', 'c') >= -1
        BEGIN
            CLOSE c;
            DEALLOCATE c;
        END
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH
END
GO



