SET NOCOUNT ON;
GO

CREATE OR ALTER PROCEDURE dbo.USP_VEN_GUIA_INTERNA_LISTAR
    @FechaDesde DATE=NULL,
    @FechaHasta DATE=NULL,
    @IdAlmacen INT=NULL,
    @Estado VARCHAR(20)=NULL,
    @Origen VARCHAR(20)=NULL,
    @Texto VARCHAR(100)=NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        G.IdGuiaInterna,
        G.NumeroGuia,
        G.Origen,
        ISNULL(G.IdOrdenCompraInterna, 0) AS IdOrdenCompraInterna,
        G.IdCliente,
        ISNULL(O.NumeroOci, '') AS NumeroOci,
        ISNULL(OT.NumeroOT, '') AS NumeroProforma,
        ISNULL(OT.NumeroOT, '') AS NumeroOrdenTrabajo,
        ISNULL(O.OrdenCompraCliente, '') AS OrdenCompraCliente,
        G.FechaEmision,
        G.IdAlmacen,
        A.NombreAlmacen,
        G.RucEmisor,
        G.EmpresaEmisora,
        G.RucDestino,
        G.EmpresaDestino,
        G.UsuarioEmisor,
        G.UsuarioAutorizador,
        G.Observacion,
        G.MotivoEmisionManual,
        G.Estado,
        ISNULL(G.UsuarioAnulacion, '') AS UsuarioAnulacion,
        G.FechaAnulacion,
        ISNULL(G.MotivoAnulacion, '') AS MotivoAnulacion,
        G.FechaRegistro
    FROM dbo.GuiasInternas G
    INNER JOIN dbo.Almacenes A ON A.IdAlmacen = G.IdAlmacen
    LEFT JOIN dbo.OrdenesCompraInterna O ON O.IdOrdenCompraInterna = G.IdOrdenCompraInterna
    OUTER APPLY
    (
        SELECT TOP (1) T.NumeroOT
        FROM dbo.OrdenTrabajo T
        WHERE T.IdOrdenCompraInterna = O.IdOrdenCompraInterna
          AND UPPER(ISNULL(T.Estado, '')) NOT IN ('ANULADA', 'ANULADO')
        ORDER BY CASE WHEN T.IdOrdenTrabajoRelacionada IS NULL THEN 0 ELSE 1 END, T.IdOrdenTrabajo DESC
    ) OT
    WHERE (@FechaDesde IS NULL OR G.FechaEmision >= @FechaDesde)
      AND (@FechaHasta IS NULL OR G.FechaEmision <= @FechaHasta)
      AND (@IdAlmacen IS NULL OR G.IdAlmacen = @IdAlmacen)
      AND (@Estado IS NULL OR G.Estado = @Estado)
      AND (@Origen IS NULL OR G.Origen = @Origen)
      AND (
          @Texto IS NULL
          OR G.NumeroGuia LIKE '%' + @Texto + '%'
          OR ISNULL(O.NumeroOci, '') LIKE '%' + @Texto + '%'
          OR ISNULL(OT.NumeroOT, '') LIKE '%' + @Texto + '%'
          OR G.EmpresaDestino LIKE '%' + @Texto + '%'
          OR G.RucDestino LIKE '%' + @Texto + '%'
          OR G.MotivoEmisionManual LIKE '%' + @Texto + '%'
      )
    ORDER BY G.FechaEmision DESC, G.IdGuiaInterna DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_VEN_GUIA_INTERNA_OBTENER
    @IdGuiaInterna INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        G.IdGuiaInterna,
        G.NumeroGuia,
        G.Origen,
        ISNULL(G.IdOrdenCompraInterna, 0) AS IdOrdenCompraInterna,
        G.IdCliente,
        ISNULL(O.NumeroOci, '') AS NumeroOci,
        ISNULL(OT.NumeroOT, '') AS NumeroProforma,
        ISNULL(OT.NumeroOT, '') AS NumeroOrdenTrabajo,
        ISNULL(O.OrdenCompraCliente, '') AS OrdenCompraCliente,
        G.FechaEmision,
        G.IdAlmacen,
        A.NombreAlmacen,
        G.RucEmisor,
        G.EmpresaEmisora,
        G.RucDestino,
        G.EmpresaDestino,
        G.UsuarioEmisor,
        G.UsuarioAutorizador,
        G.Observacion,
        G.MotivoEmisionManual,
        G.Estado,
        ISNULL(G.UsuarioAnulacion, '') AS UsuarioAnulacion,
        G.FechaAnulacion,
        ISNULL(G.MotivoAnulacion, '') AS MotivoAnulacion,
        G.FechaRegistro
    FROM dbo.GuiasInternas G
    INNER JOIN dbo.Almacenes A ON A.IdAlmacen = G.IdAlmacen
    LEFT JOIN dbo.OrdenesCompraInterna O ON O.IdOrdenCompraInterna = G.IdOrdenCompraInterna
    OUTER APPLY
    (
        SELECT TOP (1) T.NumeroOT
        FROM dbo.OrdenTrabajo T
        WHERE T.IdOrdenCompraInterna = O.IdOrdenCompraInterna
          AND UPPER(ISNULL(T.Estado, '')) NOT IN ('ANULADA', 'ANULADO')
        ORDER BY CASE WHEN T.IdOrdenTrabajoRelacionada IS NULL THEN 0 ELSE 1 END, T.IdOrdenTrabajo DESC
    ) OT
    WHERE G.IdGuiaInterna = @IdGuiaInterna;

    SELECT
        D.IdGuiaInternaDetalle,
        ISNULL(D.IdOrdenCompraInternaDetalle, 0) AS IdOrdenCompraInternaDetalle,
        D.IdProducto,
        D.CodigoProducto,
        D.NombreProducto,
        D.IdUnidadMedida,
        D.NombreUnidad,
        D.CantidadRequerida,
        ISNULL(OD.CantidadDespachada, D.CantidadDespachada) AS CantidadEntregada,
        CASE
            WHEN OD.IdOrdenCompraInternaDetalle IS NULL THEN CAST(0 AS DECIMAL(18,2))
            WHEN OD.Cantidad > OD.CantidadDespachada THEN OD.Cantidad - OD.CantidadDespachada
            ELSE CAST(0 AS DECIMAL(18,2))
        END AS CantidadPendiente,
        D.StockAnterior AS StockActual,
        D.PrecioUnitario,
        D.CantidadDespachada AS CantidadSugerida,
        D.Observacion
    FROM dbo.GuiaInternaDetalle D
    LEFT JOIN dbo.OrdenCompraInternaDetalle OD ON OD.IdOrdenCompraInternaDetalle = D.IdOrdenCompraInternaDetalle
    WHERE D.IdGuiaInterna = @IdGuiaInterna
    ORDER BY D.IdGuiaInternaDetalle;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_VEN_GUIA_INTERNA_PREPARAR
    @IdOrdenCompraInterna INT,
    @IdAlmacen INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @IdAlmacen IS NULL
        SELECT TOP (1) @IdAlmacen = IdAlmacen FROM dbo.Almacenes WHERE Estado = 1 ORDER BY IdAlmacen;

    SELECT
        O.IdOrdenCompraInterna,
        O.NumeroOci,
        ISNULL(OT.NumeroOT, '') AS NumeroProforma,
        ISNULL(OT.NumeroOT, '') AS NumeroOrdenTrabajo,
        O.OrdenCompraCliente,
        A.IdAlmacen,
        A.NombreAlmacen,
        ISNULL(E.Ruc, '') AS RucEmisor,
        ISNULL(E.Nombre, '') AS EmpresaEmisora,
        ISNULL(C.NumeroDocumento, '') AS RucDestino,
        O.NombreCliente AS EmpresaDestino
    FROM dbo.OrdenesCompraInterna O
    INNER JOIN dbo.Almacenes A ON A.IdAlmacen = @IdAlmacen AND A.Estado = 1
    INNER JOIN dbo.Clientes C ON C.IdCliente = O.IdCliente
    OUTER APPLY
    (
        SELECT TOP (1) Ruc, Nombre
        FROM dbo.Empresas
        WHERE Estado = 1
        ORDER BY EsPredeterminada DESC, IdEmpresa
    ) E
    OUTER APPLY
    (
        SELECT TOP (1) T.NumeroOT
        FROM dbo.OrdenTrabajo T
        WHERE T.IdOrdenCompraInterna = O.IdOrdenCompraInterna
          AND UPPER(ISNULL(T.Estado, '')) NOT IN ('ANULADA', 'ANULADO')
        ORDER BY CASE WHEN T.IdOrdenTrabajoRelacionada IS NULL THEN 0 ELSE 1 END, T.IdOrdenTrabajo DESC
    ) OT
    WHERE O.IdOrdenCompraInterna = @IdOrdenCompraInterna
      AND O.Estado <> 'Anulado';

    SELECT
        D.IdOrdenCompraInternaDetalle,
        D.IdProducto,
        D.CodigoProducto,
        D.NombreProducto,
        P.IdUnidadMedida,
        UM.NombreUnidad,
        D.Cantidad AS CantidadRequerida,
        D.CantidadDespachada AS CantidadEntregada,
        D.Cantidad - D.CantidadDespachada AS CantidadPendiente,
        CAST(ISNULL(S.StockActual, 0) AS DECIMAL(18,2)) AS StockActual,
        D.PrecioUnitario,
        CAST(CASE
            WHEN ISNULL(S.StockActual, 0) <= 0 THEN 0
            WHEN S.StockActual < D.Cantidad - D.CantidadDespachada THEN S.StockActual
            ELSE D.Cantidad - D.CantidadDespachada
        END AS DECIMAL(18,2)) AS CantidadSugerida,
        D.Observacion
    FROM dbo.OrdenCompraInternaDetalle D
    INNER JOIN dbo.Productos P ON P.IdProducto = D.IdProducto
    INNER JOIN dbo.UnidadesMedida UM ON UM.IdUnidadMedida = P.IdUnidadMedida
    LEFT JOIN dbo.StockProductosAlmacen S ON S.IdProducto = D.IdProducto AND S.IdAlmacen = @IdAlmacen
    WHERE D.IdOrdenCompraInterna = @IdOrdenCompraInterna
      AND D.Cantidad > D.CantidadDespachada
    ORDER BY D.IdOrdenCompraInternaDetalle;
END;
GO
