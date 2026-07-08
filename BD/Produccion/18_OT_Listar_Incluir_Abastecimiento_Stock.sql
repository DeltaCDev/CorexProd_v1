SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_LISTAR
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        o.IdOrdenTrabajo,
        o.NumeroOT,
        o.IdOrdenCompraInterna,
        ISNULL(oci.NumeroOci, '') AS NumeroOci,
        ISNULL(oci.OrdenCompraCliente, '') AS OrdenCompraCliente,
        o.IdCliente,
        o.NombreCliente,
        o.FechaEmision,
        o.Estado,
        o.IdUsuarioCreacion,
        u.NombreUsuario,
        o.Observacion,
        o.FechaRegistro,
        ISNULL(o.MotivoAnulacion, '') AS MotivoAnulacion,
        ISNULL(o.UsuarioAnulacion, '') AS UsuarioAnulacion,
        o.FechaAnulacion,
        o.TipoOT,
        o.IdOrdenTrabajoRelacionada,
        rel.NumeroOT AS NumeroOTRelacionada,
        ISNULL(ua.NombreUsuario, u.NombreUsuario) AS UsuarioAutoriza,
        COUNT(d.IdDetalleOT) AS CantidadProductos,
        SUM(ISNULL(d.CantidadPlanificada, 0)) AS TotalPlanificado,
        SUM(ISNULL(d.CantidadLanzada, 0)) AS TotalLanzado
    FROM dbo.OrdenTrabajo o
    LEFT JOIN dbo.OrdenesCompraInterna oci
        ON oci.IdOrdenCompraInterna = o.IdOrdenCompraInterna
    JOIN dbo.Usuarios u
        ON u.IdUsuario = o.IdUsuarioCreacion
    LEFT JOIN dbo.Usuarios ua
        ON ua.IdUsuario = o.IdUsuarioAutorizaCreacion
    LEFT JOIN dbo.OrdenTrabajo rel
        ON rel.IdOrdenTrabajo = o.IdOrdenTrabajoRelacionada
    LEFT JOIN dbo.OrdenTrabajoDetalle d
        ON d.IdOrdenTrabajo = o.IdOrdenTrabajo
    GROUP BY
        o.IdOrdenTrabajo,
        o.NumeroOT,
        o.IdOrdenCompraInterna,
        oci.NumeroOci,
        oci.OrdenCompraCliente,
        o.IdCliente,
        o.NombreCliente,
        o.FechaEmision,
        o.Estado,
        o.IdUsuarioCreacion,
        u.NombreUsuario,
        o.Observacion,
        o.FechaRegistro,
        o.MotivoAnulacion,
        o.UsuarioAnulacion,
        o.FechaAnulacion,
        o.TipoOT,
        o.IdOrdenTrabajoRelacionada,
        rel.NumeroOT,
        ua.NombreUsuario
    ORDER BY o.IdOrdenTrabajo DESC;
END;