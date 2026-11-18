/* Corrige el detalle de OT para que devuelva el estado y usuario de aprobacion.
   Usa valores compatibles cuando una base antigua aun no tiene esas columnas. */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_OBTENER @IdOrdenTrabajo INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @EstadoAprobacion NVARCHAR(250) =
        CASE WHEN COL_LENGTH('dbo.OrdenTrabajo','EstadoAprobacion') IS NULL
             THEN N'CAST(''PENDIENTE_APROBACION'' AS VARCHAR(25)) AS EstadoAprobacion'
             ELSE N'O.EstadoAprobacion' END;
    DECLARE @IdAprobador NVARCHAR(250) =
        CASE WHEN COL_LENGTH('dbo.OrdenTrabajo','IdUsuarioAprobacionGerencia') IS NULL
             THEN N'CAST(NULL AS INT) AS IdUsuarioAprobacionGerencia'
             ELSE N'O.IdUsuarioAprobacionGerencia' END;
    DECLARE @FechaAprobacion NVARCHAR(250) =
        CASE WHEN COL_LENGTH('dbo.OrdenTrabajo','FechaAprobacionGerencia') IS NULL
             THEN N'CAST(NULL AS DATETIME2(0)) AS FechaAprobacionGerencia'
             ELSE N'O.FechaAprobacionGerencia' END;
    DECLARE @MotivoRechazo NVARCHAR(250) =
        CASE WHEN COL_LENGTH('dbo.OrdenTrabajo','MotivoRechazoAprobacion') IS NULL
             THEN N'CAST('''' AS VARCHAR(500)) AS MotivoRechazoAprobacion'
             ELSE N'ISNULL(O.MotivoRechazoAprobacion,'''') AS MotivoRechazoAprobacion' END;
    DECLARE @ObsAprobacion NVARCHAR(250) =
        CASE WHEN COL_LENGTH('dbo.OrdenTrabajo','ObservacionAprobacion') IS NULL
             THEN N'CAST('''' AS VARCHAR(500)) AS ObservacionAprobacion'
             ELSE N'ISNULL(O.ObservacionAprobacion,'''') AS ObservacionAprobacion' END;
    DECLARE @AprobadorJoin NVARCHAR(250) =
        CASE WHEN COL_LENGTH('dbo.OrdenTrabajo','IdUsuarioAprobacionGerencia') IS NULL
             THEN N'LEFT JOIN dbo.Usuarios AP ON 1=0'
             ELSE N'LEFT JOIN dbo.Usuarios AP ON AP.IdUsuario=O.IdUsuarioAprobacionGerencia' END;

    DECLARE @Sql NVARCHAR(MAX) = N'
    SELECT O.IdOrdenTrabajo,O.NumeroOT,ISNULL(O.IdOrdenCompraInterna,0) AS IdOrdenCompraInterna,
           O.IdCliente,O.NombreCliente,O.FechaEmision,O.Estado,O.IdUsuarioCreacion,
           O.Observacion,O.FechaRegistro,ISNULL(O.MotivoAnulacion,'''') AS MotivoAnulacion,
           ISNULL(O.UsuarioAnulacion,'''') AS UsuarioAnulacion,O.FechaAnulacion,O.TipoOT,
           O.IdOrdenTrabajoRelacionada,ISNULL(OCI.NumeroOci,'''') AS NumeroOci,
           ISNULL(OCI.OrdenCompraCliente,'''') AS OrdenCompraCliente,U.NombreUsuario,
           ISNULL(UA.NombreUsuario,U.NombreUsuario) AS UsuarioAutoriza,
           ISNULL(REL.NumeroOT,'''') AS NumeroOTRelacionada,' + @EstadoAprobacion + N',' + @IdAprobador + N',' + @FechaAprobacion + N',' + @MotivoRechazo + N',' + @ObsAprobacion + N',
           ISNULL(AP.NombreUsuario,'''') AS UsuarioAprobadorGerencia
    FROM dbo.OrdenTrabajo O
    LEFT JOIN dbo.OrdenesCompraInterna OCI ON OCI.IdOrdenCompraInterna=O.IdOrdenCompraInterna
    JOIN dbo.Usuarios U ON U.IdUsuario=O.IdUsuarioCreacion
    LEFT JOIN dbo.Usuarios UA ON UA.IdUsuario=O.IdUsuarioAutorizaCreacion
    LEFT JOIN dbo.OrdenTrabajo REL ON REL.IdOrdenTrabajo=O.IdOrdenTrabajoRelacionada
    ' + @AprobadorJoin + N'
    WHERE O.IdOrdenTrabajo=@IdOrdenTrabajo;

    SELECT D.* FROM dbo.OrdenTrabajoDetalle D WHERE D.IdOrdenTrabajo=@IdOrdenTrabajo ORDER BY D.IdDetalleOT;
    SELECT A.*,D.CodigoProducto,D.NombreProducto
    FROM dbo.OrdenTrabajoDetalleArea A INNER JOIN dbo.OrdenTrabajoDetalle D ON D.IdDetalleOT=A.IdDetalleOT
    WHERE A.IdOrdenTrabajo=@IdOrdenTrabajo ORDER BY A.IdDetalleArea;';

    EXEC sys.sp_executesql @Sql,N'@IdOrdenTrabajo INT',@IdOrdenTrabajo;
END;
GO
