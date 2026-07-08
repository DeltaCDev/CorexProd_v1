SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_LISTAR
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        O.IdOrdenTrabajo,
        O.NumeroOT,
        O.IdOrdenCompraInterna,
        ISNULL(OCI.NumeroOci, '') AS NumeroOci,
        ISNULL(OCI.OrdenCompraCliente, '') AS OrdenCompraCliente,
        O.IdCliente,
        O.NombreCliente,
        O.FechaEmision,
        CASE
            WHEN SUM(ISNULL(D.CantidadPendiente, 0)) > 0
             AND SUM(ISNULL(D.CantidadProducida, 0)) > 0
             AND UPPER(O.Estado) NOT IN ('EN_PROCESO', 'PROCESO') THEN 'PARCIAL'
            ELSE O.Estado
        END AS Estado,
        O.IdUsuarioCreacion,
        U.NombreUsuario,
        O.Observacion,
        O.FechaRegistro,
        ISNULL(O.MotivoAnulacion, '') AS MotivoAnulacion,
        ISNULL(O.UsuarioAnulacion, '') AS UsuarioAnulacion,
        O.FechaAnulacion,
        O.TipoOT,
        O.IdOrdenTrabajoRelacionada,
        REL.NumeroOT AS NumeroOTRelacionada,
        ISNULL(UA.NombreUsuario, U.NombreUsuario) AS UsuarioAutoriza,
        COUNT(D.IdDetalleOT) AS CantidadProductos,
        SUM(ISNULL(D.CantidadPlanificada, 0)) AS TotalPlanificado,
        SUM(ISNULL(D.CantidadLanzada, 0)) AS TotalLanzado,
        SUM(ISNULL(D.CantidadProducida, 0)) AS TotalProducido,
        SUM(ISNULL(D.CantidadPendiente, 0)) AS TotalPendiente,
        CAST(CASE WHEN EXISTS
        (
            SELECT 1
            FROM dbo.OrdenTrabajo R
            WHERE R.IdOrdenTrabajoRelacionada = O.IdOrdenTrabajo
              AND UPPER(R.Estado) = 'TERMINADA'
              AND NOT EXISTS
              (
                  SELECT 1
                  FROM dbo.OrdenTrabajoDetalle RD
                  WHERE RD.IdOrdenTrabajo = R.IdOrdenTrabajo
                    AND RD.Estado <> 'ANULADO'
                    AND RD.CantidadPendiente > 0
              )
        ) THEN 1 ELSE 0 END AS BIT) AS TieneRegularizacionTerminada
    FROM dbo.OrdenTrabajo O
    LEFT JOIN dbo.OrdenesCompraInterna OCI
        ON OCI.IdOrdenCompraInterna = O.IdOrdenCompraInterna
    JOIN dbo.Usuarios U
        ON U.IdUsuario = O.IdUsuarioCreacion
    LEFT JOIN dbo.Usuarios UA
        ON UA.IdUsuario = O.IdUsuarioAutorizaCreacion
    LEFT JOIN dbo.OrdenTrabajo REL
        ON REL.IdOrdenTrabajo = O.IdOrdenTrabajoRelacionada
    LEFT JOIN dbo.OrdenTrabajoDetalle D
        ON D.IdOrdenTrabajo = O.IdOrdenTrabajo
    GROUP BY
        O.IdOrdenTrabajo,
        O.NumeroOT,
        O.IdOrdenCompraInterna,
        OCI.NumeroOci,
        OCI.OrdenCompraCliente,
        O.IdCliente,
        O.NombreCliente,
        O.FechaEmision,
        O.Estado,
        O.IdUsuarioCreacion,
        U.NombreUsuario,
        O.Observacion,
        O.FechaRegistro,
        O.MotivoAnulacion,
        O.UsuarioAnulacion,
        O.FechaAnulacion,
        O.TipoOT,
        O.IdOrdenTrabajoRelacionada,
        REL.NumeroOT,
        UA.NombreUsuario
    ORDER BY O.IdOrdenTrabajo DESC;
END;
GO
