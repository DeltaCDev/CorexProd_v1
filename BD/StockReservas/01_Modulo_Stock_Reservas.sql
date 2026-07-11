SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.StockReserva', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockReserva
    (
        IdStockReserva BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StockReserva PRIMARY KEY,
        IdOrdenCompraInterna INT NOT NULL,
        IdOrdenCompraInternaDetalle INT NOT NULL,
        IdProducto INT NOT NULL,
        IdAlmacen INT NULL,
        IdOrdenTrabajo INT NULL,
        IdDetalleOT INT NULL,
        CantidadReservada DECIMAL(18,2) NOT NULL,
        CantidadConsumida DECIMAL(18,2) NOT NULL CONSTRAINT DF_StockReserva_CantidadConsumida DEFAULT (0),
        CantidadLiberada DECIMAL(18,2) NOT NULL CONSTRAINT DF_StockReserva_CantidadLiberada DEFAULT (0),
        TipoOrigen VARCHAR(30) NOT NULL,
        Estado VARCHAR(30) NOT NULL CONSTRAINT DF_StockReserva_Estado DEFAULT ('ACTIVA'),
        FechaReserva DATETIME2(0) NOT NULL CONSTRAINT DF_StockReserva_FechaReserva DEFAULT (SYSDATETIME()),
        UsuarioReserva VARCHAR(100) NOT NULL,
        FechaActualizacion DATETIME2(0) NOT NULL CONSTRAINT DF_StockReserva_FechaActualizacion DEFAULT (SYSDATETIME()),
        UsuarioActualizacion VARCHAR(100) NULL,
        Observacion VARCHAR(500) NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT FK_StockReserva_OCI FOREIGN KEY (IdOrdenCompraInterna)
            REFERENCES dbo.OrdenesCompraInterna(IdOrdenCompraInterna),
        CONSTRAINT FK_StockReserva_OCID FOREIGN KEY (IdOrdenCompraInternaDetalle)
            REFERENCES dbo.OrdenCompraInternaDetalle(IdOrdenCompraInternaDetalle),
        CONSTRAINT FK_StockReserva_Producto FOREIGN KEY (IdProducto)
            REFERENCES dbo.Productos(IdProducto),
        CONSTRAINT FK_StockReserva_Almacen FOREIGN KEY (IdAlmacen)
            REFERENCES dbo.Almacenes(IdAlmacen),
        CONSTRAINT FK_StockReserva_OT FOREIGN KEY (IdOrdenTrabajo)
            REFERENCES dbo.OrdenTrabajo(IdOrdenTrabajo),
        CONSTRAINT FK_StockReserva_OTDetalle FOREIGN KEY (IdDetalleOT)
            REFERENCES dbo.OrdenTrabajoDetalle(IdDetalleOT),
        CONSTRAINT CK_StockReserva_Cantidades CHECK
        (
            CantidadReservada > 0
            AND CantidadConsumida >= 0
            AND CantidadLiberada >= 0
            AND CantidadConsumida + CantidadLiberada <= CantidadReservada
        ),
        CONSTRAINT CK_StockReserva_TipoOrigen CHECK (TipoOrigen IN ('STOCK_FISICO','PRODUCCION_OT','AJUSTE')),
        CONSTRAINT CK_StockReserva_Estado CHECK (Estado IN ('ACTIVA','PARCIALMENTE_CONSUMIDA','CONSUMIDA','LIBERADA','ANULADA'))
    );
END;
GO

IF OBJECT_ID('dbo.StockReservaMovimiento', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockReservaMovimiento
    (
        IdStockReservaMovimiento BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StockReservaMovimiento PRIMARY KEY,
        IdStockReserva BIGINT NOT NULL,
        TipoMovimiento VARCHAR(30) NOT NULL,
        Cantidad DECIMAL(18,2) NOT NULL,
        EstadoAnterior VARCHAR(30) NULL,
        EstadoNuevo VARCHAR(30) NOT NULL,
        DocumentoReferencia VARCHAR(100) NULL,
        UsuarioMovimiento VARCHAR(100) NOT NULL,
        FechaMovimiento DATETIME2(0) NOT NULL CONSTRAINT DF_StockReservaMovimiento_FechaMovimiento DEFAULT (SYSDATETIME()),
        Observacion VARCHAR(500) NULL,
        CONSTRAINT FK_StockReservaMovimiento_Reserva FOREIGN KEY (IdStockReserva)
            REFERENCES dbo.StockReserva(IdStockReserva),
        CONSTRAINT CK_StockReservaMovimiento_Tipo CHECK (TipoMovimiento IN ('CREADA','AUMENTADA','CONSUMIDA','LIBERADA','ANULADA')),
        CONSTRAINT CK_StockReservaMovimiento_Cantidad CHECK (Cantidad > 0)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StockReserva_Producto_Estado' AND object_id = OBJECT_ID('dbo.StockReserva'))
    CREATE INDEX IX_StockReserva_Producto_Estado
        ON dbo.StockReserva(IdProducto, Estado, IdAlmacen)
        INCLUDE (CantidadReservada, CantidadConsumida, CantidadLiberada, IdOrdenCompraInterna, IdOrdenCompraInternaDetalle, TipoOrigen);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StockReserva_OCI_Detalle' AND object_id = OBJECT_ID('dbo.StockReserva'))
    CREATE INDEX IX_StockReserva_OCI_Detalle
        ON dbo.StockReserva(IdOrdenCompraInterna, IdOrdenCompraInternaDetalle, Estado)
        INCLUDE (IdProducto, IdAlmacen, CantidadReservada, CantidadConsumida, CantidadLiberada, TipoOrigen);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StockReserva_OT' AND object_id = OBJECT_ID('dbo.StockReserva'))
    CREATE INDEX IX_StockReserva_OT
        ON dbo.StockReserva(IdOrdenTrabajo, IdDetalleOT)
        INCLUDE (IdOrdenCompraInterna, IdOrdenCompraInternaDetalle, IdProducto, Estado);
GO

CREATE OR ALTER VIEW dbo.VW_ALM_STOCK_DISPONIBLE_REAL
AS
SELECT
    P.IdProducto,
    P.Codigo,
    P.NombreProducto,
    ISNULL(P.EtiquetaCliente, '') AS EtiquetaCliente,
    SPA.IdAlmacen,
    A.NombreAlmacen,
    CAST(ISNULL(SPA.StockActual, 0) AS DECIMAL(18,2)) AS StockFisico,
    CAST(ISNULL(R.StockReservado, 0) AS DECIMAL(18,2)) AS StockReservado,
    CAST(
        CASE
            WHEN ISNULL(SPA.StockActual, 0) - ISNULL(R.StockReservado, 0) > 0
            THEN ISNULL(SPA.StockActual, 0) - ISNULL(R.StockReservado, 0)
            ELSE 0
        END AS DECIMAL(18,2)) AS StockDisponible
FROM dbo.Productos P
LEFT JOIN dbo.StockProductosAlmacen SPA
    ON SPA.IdProducto = P.IdProducto
LEFT JOIN dbo.Almacenes A
    ON A.IdAlmacen = SPA.IdAlmacen
OUTER APPLY
(
    SELECT SUM(SR.CantidadReservada - SR.CantidadConsumida - SR.CantidadLiberada) AS StockReservado
    FROM dbo.StockReserva SR
    WHERE SR.IdProducto = P.IdProducto
      AND (SR.IdAlmacen = SPA.IdAlmacen OR SR.IdAlmacen IS NULL)
      AND SR.Estado IN ('ACTIVA','PARCIALMENTE_CONSUMIDA')
      AND SR.CantidadReservada - SR.CantidadConsumida - SR.CantidadLiberada > 0
) R
WHERE P.Estado = 1;
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_STOCK_RESERVA_DISPONIBILIDAD
    @IdProducto INT = NULL,
    @IdAlmacen INT = NULL,
    @Buscar VARCHAR(150) = ''
AS
BEGIN
    SET NOCOUNT ON;

    SET @Buscar = LTRIM(RTRIM(ISNULL(@Buscar, '')));

    SELECT
        IdProducto,
        Codigo,
        NombreProducto,
        EtiquetaCliente,
        ISNULL(IdAlmacen, 0) AS IdAlmacen,
        ISNULL(NombreAlmacen, '') AS NombreAlmacen,
        StockFisico,
        StockReservado,
        StockDisponible
    FROM dbo.VW_ALM_STOCK_DISPONIBLE_REAL
    WHERE (@IdProducto IS NULL OR IdProducto = @IdProducto)
      AND (@IdAlmacen IS NULL OR IdAlmacen = @IdAlmacen)
      AND
      (
          @Buscar = ''
          OR Codigo LIKE '%' + @Buscar + '%'
          OR NombreProducto LIKE '%' + @Buscar + '%'
          OR EtiquetaCliente LIKE '%' + @Buscar + '%'
      )
    ORDER BY Codigo, NombreAlmacen;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_STOCK_RESERVA_LISTAR
    @IdOrdenCompraInterna INT = NULL,
    @IdProducto INT = NULL,
    @SoloActivas BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        R.IdStockReserva,
        R.IdOrdenCompraInterna,
        O.NumeroOci,
        R.IdOrdenCompraInternaDetalle,
        R.IdProducto,
        R.IdAlmacen,
        ISNULL(A.NombreAlmacen, '') AS NombreAlmacen,
        R.IdOrdenTrabajo,
        ISNULL(OT.NumeroOT, '') AS NumeroOT,
        R.IdDetalleOT,
        P.Codigo AS CodigoProducto,
        P.NombreProducto,
        R.CantidadReservada,
        R.CantidadConsumida,
        R.CantidadLiberada,
        CAST(R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada AS DECIMAL(18,2)) AS CantidadPendiente,
        R.TipoOrigen,
        R.Estado,
        R.FechaReserva,
        R.UsuarioReserva,
        R.FechaActualizacion,
        ISNULL(R.UsuarioActualizacion, '') AS UsuarioActualizacion,
        ISNULL(R.Observacion, '') AS Observacion
    FROM dbo.StockReserva R
    INNER JOIN dbo.OrdenesCompraInterna O ON O.IdOrdenCompraInterna = R.IdOrdenCompraInterna
    INNER JOIN dbo.Productos P ON P.IdProducto = R.IdProducto
    LEFT JOIN dbo.Almacenes A ON A.IdAlmacen = R.IdAlmacen
    LEFT JOIN dbo.OrdenTrabajo OT ON OT.IdOrdenTrabajo = R.IdOrdenTrabajo
    WHERE (@IdOrdenCompraInterna IS NULL OR R.IdOrdenCompraInterna = @IdOrdenCompraInterna)
      AND (@IdProducto IS NULL OR R.IdProducto = @IdProducto)
      AND (@SoloActivas = 0 OR R.Estado IN ('ACTIVA','PARCIALMENTE_CONSUMIDA'))
    ORDER BY R.FechaReserva DESC, R.IdStockReserva DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_STOCK_RESERVA_MOVIMIENTOS_LISTAR
    @IdStockReserva BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        IdStockReservaMovimiento,
        IdStockReserva,
        TipoMovimiento,
        Cantidad,
        ISNULL(EstadoAnterior, '') AS EstadoAnterior,
        EstadoNuevo,
        ISNULL(DocumentoReferencia, '') AS DocumentoReferencia,
        UsuarioMovimiento,
        FechaMovimiento,
        ISNULL(Observacion, '') AS Observacion
    FROM dbo.StockReservaMovimiento
    WHERE IdStockReserva = @IdStockReserva
    ORDER BY FechaMovimiento DESC, IdStockReservaMovimiento DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_STOCK_RESERVA_CREAR
    @IdOrdenCompraInterna INT,
    @IdOrdenCompraInternaDetalle INT,
    @IdProducto INT,
    @IdAlmacen INT = NULL,
    @IdOrdenTrabajo INT = NULL,
    @IdDetalleOT INT = NULL,
    @Cantidad DECIMAL(18,2),
    @TipoOrigen VARCHAR(30),
    @Usuario VARCHAR(100),
    @Observacion VARCHAR(500) = NULL,
    @IdStockReserva BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @Usuario = NULLIF(LTRIM(RTRIM(ISNULL(@Usuario, ''))), '');
    SET @TipoOrigen = UPPER(LTRIM(RTRIM(ISNULL(@TipoOrigen, ''))));

    BEGIN TRY
        BEGIN TRAN;

        IF @Usuario IS NULL
            THROW 51000, 'Debe indicar el usuario que crea la reserva.', 1;
        IF @Cantidad <= 0
            THROW 51000, 'La cantidad a reservar debe ser mayor a cero.', 1;
        IF @TipoOrigen NOT IN ('STOCK_FISICO','PRODUCCION_OT','AJUSTE')
            THROW 51000, 'Tipo de origen de reserva no valido.', 1;

        IF NOT EXISTS
        (
            SELECT 1
            FROM dbo.OrdenCompraInternaDetalle D
            WHERE D.IdOrdenCompraInternaDetalle = @IdOrdenCompraInternaDetalle
              AND D.IdOrdenCompraInterna = @IdOrdenCompraInterna
              AND D.IdProducto = @IdProducto
        )
            THROW 51000, 'El detalle no pertenece a la OC o al producto indicado.', 1;

        DECLARE @StockFisico DECIMAL(18,2) = 0;
        DECLARE @StockReservado DECIMAL(18,2) = 0;
        DECLARE @StockDisponible DECIMAL(18,2) = 0;

        SELECT @StockFisico = ISNULL(SUM(SPA.StockActual), 0)
        FROM dbo.StockProductosAlmacen SPA WITH (UPDLOCK, HOLDLOCK)
        WHERE SPA.IdProducto = @IdProducto
          AND (@IdAlmacen IS NULL OR SPA.IdAlmacen = @IdAlmacen);

        SELECT @StockReservado = ISNULL(SUM(CantidadReservada - CantidadConsumida - CantidadLiberada), 0)
        FROM dbo.StockReserva WITH (UPDLOCK, HOLDLOCK)
        WHERE IdProducto = @IdProducto
          AND (@IdAlmacen IS NULL OR IdAlmacen = @IdAlmacen OR IdAlmacen IS NULL)
          AND Estado IN ('ACTIVA','PARCIALMENTE_CONSUMIDA')
          AND CantidadReservada - CantidadConsumida - CantidadLiberada > 0;

        SET @StockDisponible = CASE WHEN @StockFisico - @StockReservado > 0 THEN @StockFisico - @StockReservado ELSE 0 END;

        IF @TipoOrigen = 'STOCK_FISICO' AND @Cantidad > @StockDisponible
            THROW 51000, 'No hay stock disponible suficiente para crear la reserva.', 1;

        INSERT dbo.StockReserva
        (
            IdOrdenCompraInterna, IdOrdenCompraInternaDetalle, IdProducto, IdAlmacen,
            IdOrdenTrabajo, IdDetalleOT, CantidadReservada, TipoOrigen, Estado,
            UsuarioReserva, UsuarioActualizacion, Observacion
        )
        VALUES
        (
            @IdOrdenCompraInterna, @IdOrdenCompraInternaDetalle, @IdProducto, @IdAlmacen,
            @IdOrdenTrabajo, @IdDetalleOT, @Cantidad, @TipoOrigen, 'ACTIVA',
            @Usuario, @Usuario, @Observacion
        );

        SET @IdStockReserva = CONVERT(BIGINT, SCOPE_IDENTITY());

        INSERT dbo.StockReservaMovimiento
        (
            IdStockReserva, TipoMovimiento, Cantidad, EstadoAnterior, EstadoNuevo,
            DocumentoReferencia, UsuarioMovimiento, Observacion
        )
        VALUES
        (
            @IdStockReserva, 'CREADA', @Cantidad, NULL, 'ACTIVA',
            CONCAT('OC ', @IdOrdenCompraInterna), @Usuario, @Observacion
        );

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_STOCK_RESERVA_CONSUMIR
    @IdOrdenCompraInterna INT,
    @IdOrdenCompraInternaDetalle INT,
    @Cantidad DECIMAL(18,2),
    @Usuario VARCHAR(100),
    @DocumentoReferencia VARCHAR(100) = NULL,
    @Observacion VARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @Usuario = NULLIF(LTRIM(RTRIM(ISNULL(@Usuario, ''))), '');

    BEGIN TRY
        BEGIN TRAN;

        IF @Usuario IS NULL
            THROW 51000, 'Debe indicar el usuario que consume la reserva.', 1;
        IF @Cantidad <= 0
            THROW 51000, 'La cantidad a consumir debe ser mayor a cero.', 1;

        DECLARE @Disponible DECIMAL(18,2);
        SELECT @Disponible = ISNULL(SUM(CantidadReservada - CantidadConsumida - CantidadLiberada), 0)
        FROM dbo.StockReserva WITH (UPDLOCK, HOLDLOCK)
        WHERE IdOrdenCompraInterna = @IdOrdenCompraInterna
          AND IdOrdenCompraInternaDetalle = @IdOrdenCompraInternaDetalle
          AND Estado IN ('ACTIVA','PARCIALMENTE_CONSUMIDA')
          AND CantidadReservada - CantidadConsumida - CantidadLiberada > 0;

        IF @Cantidad > ISNULL(@Disponible, 0)
            THROW 51000, 'La OC no tiene reserva suficiente para consumir.', 1;

        DECLARE @Restante DECIMAL(18,2) = @Cantidad;
        DECLARE @IdStockReserva BIGINT;
        DECLARE @Pendiente DECIMAL(18,2);
        DECLARE @Aplicar DECIMAL(18,2);
        DECLARE @EstadoAnterior VARCHAR(30);
        DECLARE @EstadoNuevo VARCHAR(30);

        DECLARE cur_reserva CURSOR LOCAL FAST_FORWARD FOR
            SELECT IdStockReserva, CantidadReservada - CantidadConsumida - CantidadLiberada AS Pendiente, Estado
            FROM dbo.StockReserva WITH (UPDLOCK, HOLDLOCK)
            WHERE IdOrdenCompraInterna = @IdOrdenCompraInterna
              AND IdOrdenCompraInternaDetalle = @IdOrdenCompraInternaDetalle
              AND Estado IN ('ACTIVA','PARCIALMENTE_CONSUMIDA')
              AND CantidadReservada - CantidadConsumida - CantidadLiberada > 0
            ORDER BY FechaReserva, IdStockReserva;

        OPEN cur_reserva;
        FETCH NEXT FROM cur_reserva INTO @IdStockReserva, @Pendiente, @EstadoAnterior;
        WHILE @@FETCH_STATUS = 0 AND @Restante > 0
        BEGIN
            SET @Aplicar = CASE WHEN @Pendiente >= @Restante THEN @Restante ELSE @Pendiente END;

            UPDATE dbo.StockReserva
            SET CantidadConsumida = CantidadConsumida + @Aplicar,
                Estado = CASE
                    WHEN CantidadReservada - (CantidadConsumida + @Aplicar) - CantidadLiberada <= 0 THEN 'CONSUMIDA'
                    ELSE 'PARCIALMENTE_CONSUMIDA'
                END,
                FechaActualizacion = SYSDATETIME(),
                UsuarioActualizacion = @Usuario
            WHERE IdStockReserva = @IdStockReserva;

            SELECT @EstadoNuevo = Estado FROM dbo.StockReserva WHERE IdStockReserva = @IdStockReserva;

            INSERT dbo.StockReservaMovimiento
            (
                IdStockReserva, TipoMovimiento, Cantidad, EstadoAnterior, EstadoNuevo,
                DocumentoReferencia, UsuarioMovimiento, Observacion
            )
            VALUES
            (
                @IdStockReserva, 'CONSUMIDA', @Aplicar, @EstadoAnterior, @EstadoNuevo,
                @DocumentoReferencia, @Usuario, @Observacion
            );

            SET @Restante -= @Aplicar;
            FETCH NEXT FROM cur_reserva INTO @IdStockReserva, @Pendiente, @EstadoAnterior;
        END;

        CLOSE cur_reserva;
        DEALLOCATE cur_reserva;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF CURSOR_STATUS('local','cur_reserva') >= 0 CLOSE cur_reserva;
        IF CURSOR_STATUS('local','cur_reserva') >= -1 DEALLOCATE cur_reserva;
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_STOCK_RESERVA_LIBERAR
    @IdStockReserva BIGINT,
    @Cantidad DECIMAL(18,2) = NULL,
    @Usuario VARCHAR(100),
    @DocumentoReferencia VARCHAR(100) = NULL,
    @Observacion VARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @Usuario = NULLIF(LTRIM(RTRIM(ISNULL(@Usuario, ''))), '');

    BEGIN TRY
        BEGIN TRAN;

        IF @Usuario IS NULL
            THROW 51000, 'Debe indicar el usuario que libera la reserva.', 1;

        DECLARE @Pendiente DECIMAL(18,2);
        DECLARE @EstadoAnterior VARCHAR(30);
        SELECT
            @Pendiente = CantidadReservada - CantidadConsumida - CantidadLiberada,
            @EstadoAnterior = Estado
        FROM dbo.StockReserva WITH (UPDLOCK, HOLDLOCK)
        WHERE IdStockReserva = @IdStockReserva;

        IF @Pendiente IS NULL
            THROW 51000, 'La reserva indicada no existe.', 1;
        IF @EstadoAnterior NOT IN ('ACTIVA','PARCIALMENTE_CONSUMIDA')
            THROW 51000, 'Solo se pueden liberar reservas activas.', 1;

        SET @Cantidad = ISNULL(@Cantidad, @Pendiente);
        IF @Cantidad <= 0 OR @Cantidad > @Pendiente
            THROW 51000, 'La cantidad a liberar no es valida.', 1;

        UPDATE dbo.StockReserva
        SET CantidadLiberada = CantidadLiberada + @Cantidad,
            Estado = CASE
                WHEN CantidadReservada - CantidadConsumida - (CantidadLiberada + @Cantidad) <= 0 THEN 'LIBERADA'
                ELSE Estado
            END,
            FechaActualizacion = SYSDATETIME(),
            UsuarioActualizacion = @Usuario
        WHERE IdStockReserva = @IdStockReserva;

        DECLARE @EstadoNuevo VARCHAR(30);
        SELECT @EstadoNuevo = Estado FROM dbo.StockReserva WHERE IdStockReserva = @IdStockReserva;

        INSERT dbo.StockReservaMovimiento
        (
            IdStockReserva, TipoMovimiento, Cantidad, EstadoAnterior, EstadoNuevo,
            DocumentoReferencia, UsuarioMovimiento, Observacion
        )
        VALUES
        (
            @IdStockReserva, 'LIBERADA', @Cantidad, @EstadoAnterior, @EstadoNuevo,
            @DocumentoReferencia, @Usuario, @Observacion
        );

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

PRINT 'Modulo base de reservas de stock instalado correctamente.';
GO
