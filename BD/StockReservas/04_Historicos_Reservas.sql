SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StockReservaMovimiento_Fecha' AND object_id = OBJECT_ID('dbo.StockReservaMovimiento'))
    CREATE INDEX IX_StockReservaMovimiento_Fecha
        ON dbo.StockReservaMovimiento(FechaMovimiento DESC, TipoMovimiento)
        INCLUDE (IdStockReserva, Cantidad, DocumentoReferencia, UsuarioMovimiento);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StockReservaMovimiento_Documento' AND object_id = OBJECT_ID('dbo.StockReservaMovimiento'))
    CREATE INDEX IX_StockReservaMovimiento_Documento
        ON dbo.StockReservaMovimiento(DocumentoReferencia, FechaMovimiento DESC)
        INCLUDE (IdStockReserva, TipoMovimiento, Cantidad, UsuarioMovimiento);
GO

CREATE OR ALTER VIEW dbo.VW_ALM_STOCK_RESERVA_HISTORICO
AS
SELECT
    M.IdStockReservaMovimiento,
    R.IdStockReserva,
    R.IdOrdenCompraInterna,
    O.NumeroOci,
    ISNULL(O.OrdenCompraCliente, '') AS OrdenCompraCliente,
    ISNULL(O.NombreCliente, '') AS NombreCliente,
    R.IdOrdenCompraInternaDetalle,
    R.IdProducto,
    P.Codigo AS CodigoProducto,
    P.NombreProducto,
    ISNULL(P.EtiquetaCliente, '') AS EtiquetaCliente,
    R.IdAlmacen,
    ISNULL(A.NombreAlmacen, '') AS NombreAlmacen,
    R.IdOrdenTrabajo,
    ISNULL(OT.NumeroOT, '') AS NumeroOT,
    R.IdDetalleOT,
    R.TipoOrigen,
    R.Estado AS EstadoReserva,
    R.CantidadReservada,
    R.CantidadConsumida,
    R.CantidadLiberada,
    CAST(R.CantidadReservada - R.CantidadConsumida - R.CantidadLiberada AS DECIMAL(18,2)) AS CantidadPendiente,
    M.TipoMovimiento,
    M.Cantidad AS CantidadMovimiento,
    ISNULL(M.EstadoAnterior, '') AS EstadoAnterior,
    M.EstadoNuevo,
    ISNULL(M.DocumentoReferencia, '') AS DocumentoReferencia,
    M.UsuarioMovimiento,
    M.FechaMovimiento,
    ISNULL(M.Observacion, '') AS ObservacionMovimiento,
    ISNULL(R.Observacion, '') AS ObservacionReserva
FROM dbo.StockReservaMovimiento M
INNER JOIN dbo.StockReserva R ON R.IdStockReserva = M.IdStockReserva
INNER JOIN dbo.Productos P ON P.IdProducto = R.IdProducto
INNER JOIN dbo.OrdenesCompraInterna O ON O.IdOrdenCompraInterna = R.IdOrdenCompraInterna
LEFT JOIN dbo.Almacenes A ON A.IdAlmacen = R.IdAlmacen
LEFT JOIN dbo.OrdenTrabajo OT ON OT.IdOrdenTrabajo = R.IdOrdenTrabajo;
GO

CREATE OR ALTER PROCEDURE dbo.USP_ALM_STOCK_RESERVA_HISTORICO_LISTAR
    @IdProducto INT = NULL,
    @IdAlmacen INT = NULL,
    @IdOrdenCompraInterna INT = NULL,
    @IdOrdenTrabajo INT = NULL,
    @TipoMovimiento VARCHAR(30) = '',
    @DocumentoReferencia VARCHAR(100) = '',
    @Desde DATETIME2(0) = NULL,
    @Hasta DATETIME2(0) = NULL,
    @Buscar VARCHAR(150) = '',
    @Top INT = 300
AS
BEGIN
    SET NOCOUNT ON;

    SET @TipoMovimiento = UPPER(LTRIM(RTRIM(ISNULL(@TipoMovimiento, ''))));
    SET @DocumentoReferencia = LTRIM(RTRIM(ISNULL(@DocumentoReferencia, '')));
    SET @Buscar = LTRIM(RTRIM(ISNULL(@Buscar, '')));
    SET @Top = CASE WHEN ISNULL(@Top, 0) <= 0 THEN 300 WHEN @Top > 2000 THEN 2000 ELSE @Top END;

    SELECT TOP (@Top)
        IdStockReservaMovimiento,
        IdStockReserva,
        IdOrdenCompraInterna,
        NumeroOci,
        OrdenCompraCliente,
        NombreCliente,
        IdOrdenCompraInternaDetalle,
        IdProducto,
        CodigoProducto,
        NombreProducto,
        EtiquetaCliente,
        IdAlmacen,
        NombreAlmacen,
        IdOrdenTrabajo,
        NumeroOT,
        IdDetalleOT,
        TipoOrigen,
        EstadoReserva,
        CantidadReservada,
        CantidadConsumida,
        CantidadLiberada,
        CantidadPendiente,
        TipoMovimiento,
        CantidadMovimiento,
        EstadoAnterior,
        EstadoNuevo,
        DocumentoReferencia,
        UsuarioMovimiento,
        FechaMovimiento,
        ObservacionMovimiento,
        ObservacionReserva
    FROM dbo.VW_ALM_STOCK_RESERVA_HISTORICO
    WHERE (@IdProducto IS NULL OR IdProducto = @IdProducto)
      AND (@IdAlmacen IS NULL OR IdAlmacen = @IdAlmacen)
      AND (@IdOrdenCompraInterna IS NULL OR IdOrdenCompraInterna = @IdOrdenCompraInterna)
      AND (@IdOrdenTrabajo IS NULL OR IdOrdenTrabajo = @IdOrdenTrabajo)
      AND (@TipoMovimiento = '' OR TipoMovimiento = @TipoMovimiento)
      AND (@DocumentoReferencia = '' OR DocumentoReferencia LIKE '%' + @DocumentoReferencia + '%')
      AND (@Desde IS NULL OR FechaMovimiento >= @Desde)
      AND (@Hasta IS NULL OR FechaMovimiento < DATEADD(DAY, 1, @Hasta))
      AND
      (
          @Buscar = ''
          OR CodigoProducto LIKE '%' + @Buscar + '%'
          OR NombreProducto LIKE '%' + @Buscar + '%'
          OR EtiquetaCliente LIKE '%' + @Buscar + '%'
          OR NumeroOci LIKE '%' + @Buscar + '%'
          OR OrdenCompraCliente LIKE '%' + @Buscar + '%'
          OR NombreCliente LIKE '%' + @Buscar + '%'
          OR NumeroOT LIKE '%' + @Buscar + '%'
          OR DocumentoReferencia LIKE '%' + @Buscar + '%'
      )
    ORDER BY FechaMovimiento DESC, IdStockReservaMovimiento DESC;
END;
GO

PRINT 'Historicos de reservas de stock configurados correctamente.';
GO
