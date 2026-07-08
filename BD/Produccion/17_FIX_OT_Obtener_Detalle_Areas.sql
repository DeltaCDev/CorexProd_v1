SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/*
    Fix USP_PRO_OT_OBTENER
    ----------------------
    El API mapea las areas de la OT esperando tambien CodigoProducto y NombreProducto.
    Se devuelve la tercera grilla con esos campos para evitar HTTP 500 al abrir cualquier OT.
*/

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_OBTENER @IdOrdenTrabajo INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        O.*,
        ISNULL(OCI.NumeroOci, '') AS NumeroOci,
        ISNULL(OCI.OrdenCompraCliente, '') AS OrdenCompraCliente,
        U.NombreUsuario,
        ISNULL(UA.NombreUsuario, U.NombreUsuario) AS UsuarioAutoriza
    FROM dbo.OrdenTrabajo O
    LEFT JOIN dbo.OrdenesCompraInterna OCI ON OCI.IdOrdenCompraInterna = O.IdOrdenCompraInterna
    JOIN dbo.Usuarios U ON U.IdUsuario = O.IdUsuarioCreacion
    LEFT JOIN dbo.Usuarios UA ON UA.IdUsuario = O.IdUsuarioAutorizaCreacion
    WHERE O.IdOrdenTrabajo = @IdOrdenTrabajo;

    SELECT
        D.*
    FROM dbo.OrdenTrabajoDetalle D
    WHERE D.IdOrdenTrabajo = @IdOrdenTrabajo
    ORDER BY D.IdDetalleOT;

    SELECT
        A.*,
        D.CodigoProducto,
        D.NombreProducto
    FROM dbo.OrdenTrabajoDetalleArea A
    INNER JOIN dbo.OrdenTrabajoDetalle D ON D.IdDetalleOT = A.IdDetalleOT
    WHERE A.IdOrdenTrabajo = @IdOrdenTrabajo
    ORDER BY A.IdDetalleArea;
END;
GO
