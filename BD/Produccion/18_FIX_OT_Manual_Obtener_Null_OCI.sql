SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/*
    Fix OT Manual / Abastecimiento de Stock
    ---------------------------------------
    La API Android convierte IdOrdenCompraInterna a INT.
    En OT manual ese valor es NULL, por eso generaba HTTP 500.
    Se devuelve 0 cuando la OT no tiene OCI asociada.
*/

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_OBTENER @IdOrdenTrabajo INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        O.IdOrdenTrabajo,
        O.NumeroOT,
        ISNULL(O.IdOrdenCompraInterna, 0) AS IdOrdenCompraInterna,
        O.IdCliente,
        O.NombreCliente,
        O.FechaEmision,
        O.Estado,
        O.IdUsuarioCreacion,
        O.Observacion,
        O.FechaRegistro,
        ISNULL(O.MotivoAnulacion, '') AS MotivoAnulacion,
        ISNULL(O.UsuarioAnulacion, '') AS UsuarioAnulacion,
        O.FechaAnulacion,
        O.TipoOT,
        O.IdOrdenTrabajoRelacionada,
        ISNULL(OCI.NumeroOci, '') AS NumeroOci,
        ISNULL(OCI.OrdenCompraCliente, '') AS OrdenCompraCliente,
        U.NombreUsuario,
        ISNULL(UA.NombreUsuario, U.NombreUsuario) AS UsuarioAutoriza,
        ISNULL(REL.NumeroOT, '') AS NumeroOTRelacionada
    FROM dbo.OrdenTrabajo O
    LEFT JOIN dbo.OrdenesCompraInterna OCI ON OCI.IdOrdenCompraInterna = O.IdOrdenCompraInterna
    JOIN dbo.Usuarios U ON U.IdUsuario = O.IdUsuarioCreacion
    LEFT JOIN dbo.Usuarios UA ON UA.IdUsuario = O.IdUsuarioAutorizaCreacion
    LEFT JOIN dbo.OrdenTrabajo REL ON REL.IdOrdenTrabajo = O.IdOrdenTrabajoRelacionada
    WHERE O.IdOrdenTrabajo = @IdOrdenTrabajo;

    SELECT
        D.*
    FROM dbo.OrdenTrabajoDetalle D
    WHERE D.IdOrdenTrabajo = @IdOrdenTrabajo
    ORDER BY D.IdDetalleOT;

    SELECT
        A.*,
        CONVERT(DECIMAL(18,2), ISNULL(R.CantidadReservada, 0)) AS CantidadReservada,
        D.CodigoProducto,
        D.NombreProducto
    FROM dbo.OrdenTrabajoDetalleArea A
    INNER JOIN dbo.OrdenTrabajoDetalle D ON D.IdDetalleOT = A.IdDetalleOT
    OUTER APPLY
    (
        SELECT SUM(SPR.Cantidad - SPR.CantidadAplicada) AS CantidadReservada
        FROM dbo.StockProcesoReserva SPR
        WHERE SPR.IdDetalleArea = A.IdDetalleArea
          AND SPR.Estado IN ('DISPONIBLE','RESERVADO')
          AND SPR.Cantidad - SPR.CantidadAplicada > 0
    ) R
    WHERE A.IdOrdenTrabajo = @IdOrdenTrabajo
    ORDER BY A.IdDetalleArea;
END;
GO
