using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace CorexProd.Datos.Datos
{
    public class OrdenTrabajoDatos
    {
        public List<OrdenTrabajo> Listar()
        {
            const string sql = @"
SELECT
    O.IdOrdenTrabajo,
    O.NumeroOT,
    O.IdOrdenCompraInterna,
    OCI.NumeroOci,
    OCI.OrdenCompraCliente,
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
LEFT JOIN dbo.OrdenesCompraInterna OCI ON OCI.IdOrdenCompraInterna = O.IdOrdenCompraInterna
JOIN dbo.Usuarios U ON U.IdUsuario = O.IdUsuarioCreacion
LEFT JOIN dbo.Usuarios UA ON UA.IdUsuario = O.IdUsuarioAutorizaCreacion
LEFT JOIN dbo.OrdenTrabajo REL ON REL.IdOrdenTrabajo = O.IdOrdenTrabajoRelacionada
LEFT JOIN dbo.OrdenTrabajoDetalle D ON D.IdOrdenTrabajo = O.IdOrdenTrabajo
GROUP BY
    O.IdOrdenTrabajo,O.NumeroOT,O.IdOrdenCompraInterna,OCI.NumeroOci,OCI.OrdenCompraCliente,
    O.IdCliente,O.NombreCliente,O.FechaEmision,O.Estado,O.IdUsuarioCreacion,U.NombreUsuario,
    O.Observacion,O.FechaRegistro,O.MotivoAnulacion,O.UsuarioAnulacion,O.FechaAnulacion,O.TipoOT,O.IdOrdenTrabajoRelacionada,REL.NumeroOT,UA.NombreUsuario
ORDER BY O.IdOrdenTrabajo DESC;";

            List<OrdenTrabajo> lista = [];
            using SqlConnection cn = Conexion.ObtenerConexion();
            using SqlCommand cmd = new(sql, cn);
            cn.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read()) lista.Add(new OrdenTrabajo
            {
                IdOrdenTrabajo = Convert.ToInt32(dr["IdOrdenTrabajo"]), NumeroOT = Texto(dr, "NumeroOT"),
                IdOrdenCompraInterna = dr["IdOrdenCompraInterna"] is DBNull ? 0 : Convert.ToInt32(dr["IdOrdenCompraInterna"]), NumeroOci = Texto(dr, "NumeroOci"),
                OrdenCompraCliente = Texto(dr,"OrdenCompraCliente"), TipoOT=Texto(dr,"TipoOT"),
                IdOrdenTrabajoRelacionada=dr["IdOrdenTrabajoRelacionada"] is DBNull?null:Convert.ToInt32(dr["IdOrdenTrabajoRelacionada"]),NumeroOTRelacionada=Texto(dr,"NumeroOTRelacionada"),
                IdCliente = Convert.ToInt32(dr["IdCliente"]), NombreCliente = Texto(dr, "NombreCliente"),
                FechaEmision = Convert.ToDateTime(dr["FechaEmision"]), Estado = Texto(dr, "Estado"),
                IdUsuarioCreacion = Convert.ToInt32(dr["IdUsuarioCreacion"]), UsuarioCreacion = Texto(dr, "NombreUsuario"),
                UsuarioAutoriza=Texto(dr,"UsuarioAutoriza"),
                Observacion = Texto(dr, "Observacion"), FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"]),
                MotivoAnulacion = Texto(dr, "MotivoAnulacion"), UsuarioAnulacion = Texto(dr, "UsuarioAnulacion"),
                FechaAnulacion = dr["FechaAnulacion"] is DBNull ? null : Convert.ToDateTime(dr["FechaAnulacion"]),
                CantidadProductos = Convert.ToInt32(dr["CantidadProductos"]), TotalPlanificado = Decimal(dr, "TotalPlanificado"), TotalLanzado = Decimal(dr, "TotalLanzado"),
                TotalProducido = Decimal(dr, "TotalProducido"), TotalPendiente = Decimal(dr, "TotalPendiente"),
                TieneRegularizacionTerminada = Convert.ToBoolean(dr["TieneRegularizacionTerminada"])
            });
            return lista;
        }

        public List<(string Nombre, int Cantidad)> ListarTopProductosPorMes(DateTime desde, DateTime hastaExclusivo)
        {
            const string sql = """
SELECT TOP (5)
    CASE
        WHEN NULLIF(LTRIM(RTRIM(D.CodigoProducto)), '') IS NULL THEN LTRIM(RTRIM(D.NombreProducto))
        ELSE LTRIM(RTRIM(D.CodigoProducto)) + ' - ' + LTRIM(RTRIM(D.NombreProducto))
    END AS Producto,
    CONVERT(INT, ROUND(SUM(ISNULL(D.CantidadPlanificada, 0)), 0)) AS Cantidad
FROM dbo.OrdenTrabajo O
JOIN dbo.OrdenTrabajoDetalle D ON D.IdOrdenTrabajo = O.IdOrdenTrabajo
WHERE O.FechaEmision >= @Desde
  AND O.FechaEmision < @Hasta
  AND UPPER(REPLACE(LTRIM(RTRIM(O.Estado)), ' ', '_')) NOT IN ('ANULADO', 'ANULADA')
  AND NULLIF(LTRIM(RTRIM(D.NombreProducto)), '') IS NOT NULL
GROUP BY
    CASE
        WHEN NULLIF(LTRIM(RTRIM(D.CodigoProducto)), '') IS NULL THEN LTRIM(RTRIM(D.NombreProducto))
        ELSE LTRIM(RTRIM(D.CodigoProducto)) + ' - ' + LTRIM(RTRIM(D.NombreProducto))
    END
ORDER BY SUM(ISNULL(D.CantidadPlanificada, 0)) DESC;
""";

            List<(string Nombre, int Cantidad)> lista = [];
            using SqlConnection cn = Conexion.ObtenerConexion();
            using SqlCommand cmd = new(sql, cn);
            cmd.Parameters.Add("@Desde", SqlDbType.Date).Value = desde.Date;
            cmd.Parameters.Add("@Hasta", SqlDbType.Date).Value = hastaExclusivo.Date;
            cn.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
                lista.Add((Texto(dr, "Producto"), Convert.ToInt32(dr["Cantidad"])));
            return lista;
        }

        public OrdenTrabajo? Obtener(int id)
        {
            using SqlConnection cn = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_PRO_OT_OBTENER", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@IdOrdenTrabajo", id); cn.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            if (!dr.Read()) return null;
            OrdenTrabajo ot = new()
            {
                IdOrdenTrabajo = id, NumeroOT = Texto(dr,"NumeroOT"), IdOrdenCompraInterna = dr["IdOrdenCompraInterna"] is DBNull ? 0 : Convert.ToInt32(dr["IdOrdenCompraInterna"]),
                NumeroOci = Texto(dr,"NumeroOci"), OrdenCompraCliente=Texto(dr,"OrdenCompraCliente"),TipoOT=Texto(dr,"TipoOT"),
                IdOrdenTrabajoRelacionada=dr["IdOrdenTrabajoRelacionada"] is DBNull?null:Convert.ToInt32(dr["IdOrdenTrabajoRelacionada"]),
                IdCliente = Convert.ToInt32(dr["IdCliente"]), NombreCliente = Texto(dr,"NombreCliente"),
                FechaEmision = Convert.ToDateTime(dr["FechaEmision"]), Estado = Texto(dr,"Estado"), IdUsuarioCreacion = Convert.ToInt32(dr["IdUsuarioCreacion"]),
                UsuarioCreacion = Texto(dr,"NombreUsuario"),UsuarioAutoriza=Texto(dr,"UsuarioAutoriza"), Observacion = Texto(dr,"Observacion"), FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"]),
                MotivoAnulacion = Texto(dr, "MotivoAnulacion"), UsuarioAnulacion = Texto(dr, "UsuarioAnulacion"),
                FechaAnulacion = dr["FechaAnulacion"] is DBNull ? null : Convert.ToDateTime(dr["FechaAnulacion"])
            };
            if (dr.NextResult()) while (dr.Read()) ot.Detalles.Add(MapearDetalle(dr));
            if (dr.NextResult()) while (dr.Read()) ot.Areas.Add(MapearArea(dr));
            ot.TotalProducido = ot.Detalles.Sum(x => x.CantidadProducida);
            ot.TotalPendiente = ot.Detalles.Sum(x => x.CantidadPendiente);
            ot.TieneRegularizacionTerminada = TieneRegularizacionTerminada(id);
            if (ot.TotalPendiente > 0
                && ot.TotalProducido > 0
                && !ot.Estado.Equals("EN_PROCESO", StringComparison.OrdinalIgnoreCase)
                && !ot.Estado.Equals("PROCESO", StringComparison.OrdinalIgnoreCase))
                ot.Estado = "PARCIAL";
            return ot;
        }

        private bool TieneRegularizacionTerminada(int idOrdenTrabajo)
        {
            const string sql = @"
SELECT CAST(CASE WHEN EXISTS
(
    SELECT 1
    FROM dbo.OrdenTrabajo R
    WHERE R.IdOrdenTrabajoRelacionada = @IdOrdenTrabajo
      AND UPPER(R.Estado) = 'TERMINADA'
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.OrdenTrabajoDetalle RD
          WHERE RD.IdOrdenTrabajo = R.IdOrdenTrabajo
            AND RD.Estado <> 'ANULADO'
            AND RD.CantidadPendiente > 0
      )
) THEN 1 ELSE 0 END AS BIT);";

            using SqlConnection cn = Conexion.ObtenerConexion();
            using SqlCommand cmd = new(sql, cn);
            cmd.Parameters.AddWithValue("@IdOrdenTrabajo", idOrdenTrabajo);
            cn.Open();
            return Convert.ToBoolean(cmd.ExecuteScalar());
        }

        public List<OrdenTrabajoValidacionProducto> ListarPendientesRegularizacion(int idOrdenTrabajo)
        {
            const string sql = @"
SELECT
    D.IdOrdenCompraInternaDetalle,
    D.IdProducto,
    D.CodigoProducto,
    D.NombreProducto,
    D.ObservacionDiferencia AS Observacion,
    D.CantidadPendiente AS CantidadRequerida,
    F.IdFichaTecnica,
    CONVERT(DECIMAL(18,3), ISNULL(SP.StockActual, 0)) AS StockAlmacen,
    CONVERT(DECIMAL(18,3), ISNULL(AP.StockCorte, 0)) AS StockCorte,
    CONVERT(DECIMAL(18,3), ISNULL(AP.StockConfeccion, 0)) AS StockConfeccion,
    CONVERT(DECIMAL(18,3), ISNULL(AP.StockAcabado, 0)) AS StockAcabado,
    CONVERT(DECIMAL(18,3), ISNULL(SP.StockActual, 0) + ISNULL(AP.StockCorte, 0) + ISNULL(AP.StockConfeccion, 0) + ISNULL(AP.StockAcabado, 0)) AS StockTotal,
    D.CantidadPendiente AS Deficit,
    CASE
        WHEN F.IdFichaTecnica IS NULL
             OR NOT EXISTS(SELECT 1 FROM dbo.FichaTecnicaDetalle FD WHERE FD.IdFichaTecnica = F.IdFichaTecnica AND FD.Estado = 1)
            THEN 'Sin ficha tecnica'
        WHEN EXISTS
        (
            SELECT 1
            FROM dbo.FichaTecnicaDetalle FD
            LEFT JOIN dbo.StockInsumos SI ON SI.IdInsumo = FD.IdInsumo
            WHERE FD.IdFichaTecnica = F.IdFichaTecnica
              AND FD.Estado = 1
              AND ISNULL(SI.StockActual, 0) < FD.Cantidad * D.CantidadPendiente
        ) THEN 'Faltantes'
        ELSE 'Completo para producir'
    END AS EstadoInsumos
FROM dbo.OrdenTrabajoDetalle D
OUTER APPLY
(
    SELECT TOP(1) FT.IdFichaTecnica
    FROM dbo.FichaTecnica FT
    WHERE FT.IdProducto = D.IdProducto AND FT.Estado = 1
    ORDER BY FT.Version DESC, FT.IdFichaTecnica DESC
) F
OUTER APPLY (SELECT SUM(S.StockActual) AS StockActual FROM dbo.StockProductosAlmacen S WHERE S.IdProducto = D.IdProducto) SP
OUTER APPLY
(
    SELECT
        SUM(CASE WHEN A.NombreArea LIKE '%CORTE%' THEN DA.CantidadPendiente ELSE 0 END) AS StockCorte,
        SUM(CASE WHEN A.NombreArea LIKE '%CONFECCI%' THEN DA.CantidadPendiente ELSE 0 END) AS StockConfeccion,
        SUM(CASE WHEN A.NombreArea LIKE '%ACABADO%' THEN DA.CantidadPendiente ELSE 0 END) AS StockAcabado
    FROM dbo.OrdenTrabajoDetalle OD
    JOIN dbo.OrdenTrabajoDetalleArea DA ON DA.IdDetalleOT = OD.IdDetalleOT
    JOIN dbo.AreaProduccion A ON A.IdAreaProduccion = DA.IdAreaProduccion
    WHERE OD.IdProducto = D.IdProducto
      AND OD.Estado NOT IN ('TERMINADO', 'ANULADO')
) AP
WHERE D.IdOrdenTrabajo = @IdOrdenTrabajo
  AND D.CantidadPendiente > 0
  AND D.Estado <> 'ANULADO'
ORDER BY D.IdDetalleOT;";

            List<OrdenTrabajoValidacionProducto> lista = [];
            using SqlConnection cn = Conexion.ObtenerConexion();
            using SqlCommand cmd = new(sql, cn);
            cmd.Parameters.AddWithValue("@IdOrdenTrabajo", idOrdenTrabajo);
            cn.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new OrdenTrabajoValidacionProducto
                {
                    IdOrdenCompraInternaDetalle = Convert.ToInt32(dr["IdOrdenCompraInternaDetalle"]),
                    IdProducto = Convert.ToInt32(dr["IdProducto"]),
                    CodigoProducto = Texto(dr, "CodigoProducto"),
                    NombreProducto = Texto(dr, "NombreProducto"),
                    Observacion = Texto(dr, "Observacion"),
                    CantidadRequerida = Decimal(dr, "CantidadRequerida"),
                    IdFichaTecnica = dr["IdFichaTecnica"] is DBNull ? null : Convert.ToInt32(dr["IdFichaTecnica"]),
                    StockAlmacen = Decimal(dr, "StockAlmacen"),
                    StockCorte = Decimal(dr, "StockCorte"),
                    StockConfeccion = Decimal(dr, "StockConfeccion"),
                    StockAcabado = Decimal(dr, "StockAcabado"),
                    StockTotal = Decimal(dr, "StockTotal"),
                    Deficit = Decimal(dr, "Deficit"),
                    EstadoInsumos = Texto(dr, "EstadoInsumos")
                });
            }

            return lista;
        }

        public (int Id, string Numero) Crear(int idOci, int idUsuario, string observacion, IEnumerable<OrdenTrabajoPlanificacion> items, int? idOrdenTrabajoRelacionada = null, bool procesarTodaReserva = false)
        {
            if (idOrdenTrabajoRelacionada.HasValue)
                return CrearRegularizacion(idOci, idOrdenTrabajoRelacionada.Value, idUsuario, observacion, items);

            using SqlConnection cn = Conexion.ObtenerConexion();
            cn.Open();
            using (SqlCommand validar = new(
                """
                SELECT CAST(CASE WHEN EXISTS
                (
                    SELECT 1
                    FROM dbo.OrdenTrabajo
                    WHERE IdOrdenCompraInterna = @IdOrdenCompraInterna
                      AND UPPER(REPLACE(Estado, ' ', '_')) IN ('PENDIENTE', 'EMITIDA', 'EN_PROCESO', 'PARCIAL')
                ) THEN 1 ELSE 0 END AS BIT);
                """,
                cn))
            {
                validar.Parameters.Add("@IdOrdenCompraInterna", SqlDbType.Int).Value = idOci;
                if (Convert.ToBoolean(validar.ExecuteScalar()))
                    throw new InvalidOperationException("La OCI ya tiene una OT activa en proceso. No se puede generar otra OT hasta cerrar o anular la existente.");
            }

            using SqlCommand cmd = new("USP_PRO_OT_CREAR", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@IdOrdenCompraInterna", idOci); cmd.Parameters.AddWithValue("@IdUsuario", idUsuario); cmd.Parameters.AddWithValue("@Observacion", observacion ?? string.Empty);
            cmd.Parameters.Add(new SqlParameter("@Detalles", SqlDbType.Structured) { TypeName="dbo.TipoOTPlanificacion", Value=TablaPlanificacion(items) });
            cmd.Parameters.Add("@ProcesarTodaReserva", SqlDbType.Bit).Value = procesarTodaReserva;
            SqlParameter id = new("@IdOrdenTrabajo", SqlDbType.Int) { Direction=ParameterDirection.Output };
            SqlParameter numero = new("@NumeroOT", SqlDbType.VarChar,30) { Direction=ParameterDirection.Output };
            cmd.Parameters.Add(id); cmd.Parameters.Add(numero); cmd.ExecuteNonQuery();
            return (Convert.ToInt32(id.Value), numero.Value?.ToString() ?? string.Empty);
        }

        private static (int Id, string Numero) CrearRegularizacion(
            int idOci,
            int idOrdenTrabajoOrigen,
            int idUsuario,
            string observacion,
            IEnumerable<OrdenTrabajoPlanificacion> items)
        {
            using SqlConnection cn = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_PRO_OT_CREAR_REGULARIZACION", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@IdOrdenCompraInterna", idOci);
            cmd.Parameters.AddWithValue("@IdOrdenTrabajoOrigen", idOrdenTrabajoOrigen);
            cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
            cmd.Parameters.AddWithValue("@Observacion", observacion ?? string.Empty);
            cmd.Parameters.Add(new SqlParameter("@Detalles", SqlDbType.Structured) { TypeName = "dbo.TipoOTPlanificacion", Value = TablaPlanificacion(items) });
            SqlParameter id = new("@IdOrdenTrabajo", SqlDbType.Int) { Direction = ParameterDirection.Output };
            SqlParameter numero = new("@NumeroOT", SqlDbType.VarChar, 30) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(id);
            cmd.Parameters.Add(numero);
            cn.Open();

            using (SqlCommand validar = new(@"
SELECT COUNT(1)
FROM dbo.OrdenTrabajo
WHERE IdOrdenTrabajoRelacionada = @IdOrdenTrabajoOrigen
  AND UPPER(Estado) NOT IN ('ANULADA', 'ANULADO');", cn))
            {
                validar.Parameters.Add("@IdOrdenTrabajoOrigen", SqlDbType.Int).Value = idOrdenTrabajoOrigen;
                if (Convert.ToInt32(validar.ExecuteScalar()) > 0)
                    throw new InvalidOperationException("La OT origen ya fue regularizada.");
            }

            cmd.ExecuteNonQuery();
            return (Convert.ToInt32(id.Value), numero.Value?.ToString() ?? string.Empty);
        }

        public List<OrdenTrabajoValidacionProducto> ValidarInsumosManual(IEnumerable<OrdenTrabajoManualPlanificacion> items)
        {
            List<OrdenTrabajoValidacionProducto> lista = [];
            using SqlConnection cn = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_PRO_OT_MANUAL_VALIDAR_INSUMOS", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add(new SqlParameter("@Detalles", SqlDbType.Structured) { TypeName = "dbo.TipoOTManualPlanificacion", Value = TablaPlanificacionManual(items) });
            cn.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new OrdenTrabajoValidacionProducto
                {
                    IdOrdenCompraInternaDetalle = 0,
                    IdProducto = Convert.ToInt32(dr["IdProducto"]),
                    CodigoProducto = Texto(dr, "CodigoProducto"),
                    NombreProducto = Texto(dr, "NombreProducto"),
                    Observacion = Texto(dr, "Observacion"),
                    CantidadRequerida = Decimal(dr, "CantidadRequerida"),
                    IdFichaTecnica = dr["IdFichaTecnica"] is DBNull ? null : Convert.ToInt32(dr["IdFichaTecnica"]),
                    StockAlmacen = Decimal(dr, "StockAlmacen"),
                    StockCorte = Decimal(dr, "StockCorte"),
                    StockConfeccion = Decimal(dr, "StockConfeccion"),
                    StockAcabado = Decimal(dr, "StockAcabado"),
                    StockTotal = Decimal(dr, "StockTotal"),
                    Deficit = Decimal(dr, "Deficit"),
                    EstadoInsumos = Texto(dr, "EstadoInsumos")
                });
            }

            return lista;
        }

        public (int Id, string Numero) CrearManual(int idUsuario, string observacion, IEnumerable<OrdenTrabajoManualPlanificacion> items)
        {
            using SqlConnection cn = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_PRO_OT_MANUAL_CREAR", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
            cmd.Parameters.AddWithValue("@Observacion", observacion ?? string.Empty);
            cmd.Parameters.Add(new SqlParameter("@Detalles", SqlDbType.Structured) { TypeName = "dbo.TipoOTManualPlanificacion", Value = TablaPlanificacionManual(items) });
            SqlParameter id = new("@IdOrdenTrabajo", SqlDbType.Int) { Direction = ParameterDirection.Output };
            SqlParameter numero = new("@NumeroOT", SqlDbType.VarChar, 30) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(id);
            cmd.Parameters.Add(numero);
            cn.Open();
            cmd.ExecuteNonQuery();
            return (Convert.ToInt32(id.Value), numero.Value?.ToString() ?? string.Empty);
        }

        public void Anular(int idOrdenTrabajo, bool convertirProcesoAMerma, int idUsuarioSesion, string motivoAnulacion, string usuarioAnulacion)
        {
            using SqlConnection cn = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_PRO_OT_ANULAR", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@IdOrdenTrabajo", idOrdenTrabajo);
            cmd.Parameters.AddWithValue("@ConvertirProcesoAMerma", convertirProcesoAMerma);
            cmd.Parameters.AddWithValue("@IdUsuarioSesion", idUsuarioSesion);
            cmd.Parameters.AddWithValue("@MotivoAnulacion", motivoAnulacion);
            cmd.Parameters.AddWithValue("@UsuarioAnulacion", usuarioAnulacion);
            cn.Open();
            cmd.ExecuteNonQuery();
        }

        public void Lanzar(int idOt, int idSesion, int idAutoriza, IEnumerable<OrdenTrabajoLanzamiento> items)
        {
            using SqlConnection cn=Conexion.ObtenerConexion(); using SqlCommand cmd=new("USP_PRO_OT_LANZAR",cn){CommandType=CommandType.StoredProcedure};
            cmd.Parameters.AddWithValue("@IdOrdenTrabajo",idOt); cmd.Parameters.AddWithValue("@IdUsuarioSesion",idSesion); cmd.Parameters.AddWithValue("@IdUsuarioAutoriza",idAutoriza);
            cmd.Parameters.Add(new SqlParameter("@Detalles",SqlDbType.Structured){TypeName="dbo.TipoOTLanzamiento",Value=TablaLanzamiento(items)}); cn.Open(); cmd.ExecuteNonQuery();
        }

        public long Transferir(int idOt,int idArea,int idSesion,int idAutoriza,string observacion,IEnumerable<OrdenTrabajoTransferenciaItem> items)
        {
            using SqlConnection cn=Conexion.ObtenerConexion(); using SqlCommand cmd=new("USP_PRO_OT_TRANSFERIR",cn){CommandType=CommandType.StoredProcedure};
            cmd.Parameters.AddWithValue("@IdOrdenTrabajo",idOt); cmd.Parameters.AddWithValue("@IdAreaOrigen",idArea); cmd.Parameters.AddWithValue("@IdUsuarioSesion",idSesion); cmd.Parameters.AddWithValue("@IdUsuarioAutoriza",idAutoriza); cmd.Parameters.AddWithValue("@Observacion",observacion??string.Empty);
            cmd.Parameters.Add(new SqlParameter("@Detalles",SqlDbType.Structured){TypeName="dbo.TipoOTTransferencia",Value=TablaTransferencia(items)});
            SqlParameter op=new("@IdOperacion",SqlDbType.BigInt){Direction=ParameterDirection.Output}; cmd.Parameters.Add(op); cn.Open(); cmd.ExecuteNonQuery(); return Convert.ToInt64(op.Value);
        }

        public long Terminar(int idOt,int idArea,int idSesion,int idAutoriza,string observacion,IEnumerable<OrdenTrabajoTransferenciaItem> items)
        {
            using SqlConnection cn=Conexion.ObtenerConexion(); using SqlCommand cmd=new("USP_PRO_OT_TERMINAR",cn){CommandType=CommandType.StoredProcedure};
            cmd.Parameters.AddWithValue("@IdOrdenTrabajo",idOt); cmd.Parameters.AddWithValue("@IdAreaTermino",idArea); cmd.Parameters.AddWithValue("@IdUsuarioSesion",idSesion); cmd.Parameters.AddWithValue("@IdUsuarioAutoriza",idAutoriza); cmd.Parameters.AddWithValue("@Observacion",observacion??string.Empty);
            cmd.Parameters.Add(new SqlParameter("@Detalles",SqlDbType.Structured){TypeName="dbo.TipoOTTransferencia",Value=TablaTransferencia(items)});
            SqlParameter op=new("@IdOperacion",SqlDbType.BigInt){Direction=ParameterDirection.Output}; cmd.Parameters.Add(op); cn.Open(); cmd.ExecuteNonQuery(); return Convert.ToInt64(op.Value);
        }

        public long TransferirConMerma(int idOt,int idArea,long idDetalleArea,int idSesion,int idAutoriza,decimal cantidadMerma,string motivo,string observacion,IEnumerable<OrdenTrabajoTransferenciaItem> items)
            => OperarConMerma("USP_PRO_OT_TRANSFERIR","@IdAreaOrigen",idOt,idArea,idDetalleArea,idSesion,idAutoriza,cantidadMerma,motivo,observacion,items);

        public long TerminarConMerma(int idOt,int idArea,long idDetalleArea,int idSesion,int idAutoriza,decimal cantidadMerma,string motivo,string observacion,IEnumerable<OrdenTrabajoTransferenciaItem> items)
            => OperarConMerma("USP_PRO_OT_TERMINAR","@IdAreaTermino",idOt,idArea,idDetalleArea,idSesion,idAutoriza,cantidadMerma,motivo,observacion,items);

        private static long OperarConMerma(string procedimiento,string parametroArea,int idOt,int idArea,long idDetalleArea,int idSesion,int idAutoriza,decimal cantidadMerma,string motivo,string observacion,IEnumerable<OrdenTrabajoTransferenciaItem> items)
        {
            using SqlConnection cn=Conexion.ObtenerConexion(); cn.Open(); using SqlTransaction tx=cn.BeginTransaction();
            try
            {
                using(SqlCommand merma=new("USP_PRO_OT_MERMA_REGISTRAR",cn,tx){CommandType=CommandType.StoredProcedure})
                {
                    merma.Parameters.AddWithValue("@IdDetalleArea",idDetalleArea); merma.Parameters.AddWithValue("@Cantidad",cantidadMerma);
                    merma.Parameters.AddWithValue("@Motivo",motivo); merma.Parameters.AddWithValue("@Observacion",observacion??string.Empty);
                    merma.Parameters.AddWithValue("@IdUsuarioSesion",idSesion); merma.Parameters.AddWithValue("@IdUsuarioAutoriza",idAutoriza); merma.ExecuteNonQuery();
                }
                using SqlCommand cmd=new(procedimiento,cn,tx){CommandType=CommandType.StoredProcedure};
                cmd.Parameters.AddWithValue("@IdOrdenTrabajo",idOt); cmd.Parameters.AddWithValue(parametroArea,idArea);
                cmd.Parameters.AddWithValue("@IdUsuarioSesion",idSesion); cmd.Parameters.AddWithValue("@IdUsuarioAutoriza",idAutoriza); cmd.Parameters.AddWithValue("@Observacion",observacion??string.Empty);
                cmd.Parameters.Add(new SqlParameter("@Detalles",SqlDbType.Structured){TypeName="dbo.TipoOTTransferencia",Value=TablaTransferencia(items)});
                SqlParameter op=new("@IdOperacion",SqlDbType.BigInt){Direction=ParameterDirection.Output}; cmd.Parameters.Add(op); cmd.ExecuteNonQuery();
                tx.Commit(); return Convert.ToInt64(op.Value);
            }
            catch { if(tx.Connection!=null) tx.Rollback(); throw; }
        }

        public void RegistrarMerma(long idArea,decimal cantidad,string motivo,string observacion,int idSesion,int idAutoriza)
        {
            using SqlConnection cn=Conexion.ObtenerConexion(); using SqlCommand cmd=new("USP_PRO_OT_MERMA_REGISTRAR",cn){CommandType=CommandType.StoredProcedure};
            cmd.Parameters.AddWithValue("@IdDetalleArea",idArea); cmd.Parameters.AddWithValue("@Cantidad",cantidad); cmd.Parameters.AddWithValue("@Motivo",motivo); cmd.Parameters.AddWithValue("@Observacion",observacion??string.Empty); cmd.Parameters.AddWithValue("@IdUsuarioSesion",idSesion); cmd.Parameters.AddWithValue("@IdUsuarioAutoriza",idAutoriza); cn.Open(); cmd.ExecuteNonQuery();
        }

        public void ReservarStockProceso(long idDetalleArea, decimal cantidad, string observacion, int idSesion, int idAutoriza)
        {
            using SqlConnection cn = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_PRO_OT_RESERVAR_STOCK_PROCESO", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@IdDetalleArea", idDetalleArea);
            cmd.Parameters.AddWithValue("@Cantidad", cantidad);
            cmd.Parameters.AddWithValue("@Observacion", observacion ?? string.Empty);
            cmd.Parameters.AddWithValue("@IdUsuarioSesion", idSesion);
            cmd.Parameters.AddWithValue("@IdUsuarioAutoriza", idAutoriza);
            cn.Open();
            cmd.ExecuteNonQuery();
        }

        public List<OrdenTrabajoValidacionProducto> ValidarInsumos(int idOci)
        {
            List<OrdenTrabajoValidacionProducto> lista=[];using SqlConnection cn=Conexion.ObtenerConexion();using SqlCommand cmd=new("USP_PRO_OT_VALIDAR_INSUMOS",cn){CommandType=CommandType.StoredProcedure};cmd.Parameters.AddWithValue("@IdOrdenCompraInterna",idOci);cn.Open();using SqlDataReader dr=cmd.ExecuteReader();while(dr.Read())lista.Add(new OrdenTrabajoValidacionProducto{IdOrdenCompraInternaDetalle=Convert.ToInt32(dr["IdOrdenCompraInternaDetalle"]),IdProducto=Convert.ToInt32(dr["IdProducto"]),CodigoProducto=Texto(dr,"CodigoProducto"),NombreProducto=Texto(dr,"NombreProducto"),Observacion=Texto(dr,"Observacion"),CantidadRequerida=Decimal(dr,"CantidadRequerida"),IdFichaTecnica=dr["IdFichaTecnica"]is DBNull?null:Convert.ToInt32(dr["IdFichaTecnica"]),StockAlmacen=Decimal(dr,"StockAlmacen"),StockCorte=Decimal(dr,"StockCorte"),StockConfeccion=Decimal(dr,"StockConfeccion"),StockAcabado=Decimal(dr,"StockAcabado"),StockTotal=Decimal(dr,"StockTotal"),Deficit=Decimal(dr,"Deficit"),EstadoInsumos=Texto(dr,"EstadoInsumos")});return lista;
        }

        public List<OrdenTrabajoInsumoDetalle> DetalleInsumos(int idDetalleOci)
        {
            List<OrdenTrabajoInsumoDetalle> lista=[];using SqlConnection cn=Conexion.ObtenerConexion();using SqlCommand cmd=new("USP_PRO_OT_DETALLE_INSUMOS",cn){CommandType=CommandType.StoredProcedure};cmd.Parameters.AddWithValue("@IdOrdenCompraInternaDetalle",idDetalleOci);cn.Open();using SqlDataReader dr=cmd.ExecuteReader();while(dr.Read())lista.Add(new OrdenTrabajoInsumoDetalle{IdInsumo=Convert.ToInt32(dr["IdInsumo"]),CodigoInsumo=Texto(dr,"CodigoInsumo"),NombreInsumo=Texto(dr,"NombreInsumo"),UnidadMedida=Texto(dr,"UnidadMedida"),ConsumoUnitario=Decimal(dr,"ConsumoUnitario"),CantidadProduccion=Decimal(dr,"CantidadProduccion"),CantidadNecesaria=Decimal(dr,"CantidadNecesaria"),StockActual=Decimal(dr,"StockActual"),StockProyectado=Decimal(dr,"StockProyectado"),CantidadFaltante=Decimal(dr,"CantidadFaltante"),Estado=Texto(dr,"Estado")});return lista;
        }

        public List<OrdenTrabajoMovimiento> ListarMovimientos(int idOrdenTrabajo)
        {
            const string sql = @"
SELECT t.FechaRegistro FechaHora,d.CodigoProducto,d.NombreProducto,ao.NombreArea Origen,ad.NombreArea Destino,
       td.CantidadEnviada Cantidad,'AVANCE_AREA' Accion,ISNULL(ua.NombreUsuario,us.NombreUsuario) Usuario,t.Observacion
FROM dbo.OrdenTrabajoTransferencia t
JOIN dbo.OrdenTrabajoTransferenciaDetalle td ON td.IdOperacionTransferencia=t.IdOperacionTransferencia
JOIN dbo.OrdenTrabajoDetalle d ON d.IdDetalleOT=td.IdDetalleOT
JOIN dbo.AreaProduccion ao ON ao.IdAreaProduccion=t.IdAreaOrigen
JOIN dbo.AreaProduccion ad ON ad.IdAreaProduccion=t.IdAreaDestino
JOIN dbo.Usuarios us ON us.IdUsuario=t.IdUsuarioSesion
LEFT JOIN dbo.Usuarios ua ON ua.IdUsuario=t.IdUsuarioAutoriza
WHERE t.IdOrdenTrabajo=@IdOrdenTrabajo
UNION ALL
SELECT m.FechaRegistro FechaHora,d.CodigoProducto,d.NombreProducto,a.NombreArea Origen,'' Destino,
       m.Cantidad,'REGISTRO_MERMA' Accion,ISNULL(ua.NombreUsuario,us.NombreUsuario) Usuario,
       CONCAT(m.Motivo,CASE WHEN NULLIF(m.Observacion,'') IS NULL THEN '' ELSE CONCAT(' - ',m.Observacion) END) Observacion
FROM dbo.OrdenTrabajoMerma m
JOIN dbo.OrdenTrabajoDetalle d ON d.IdDetalleOT=m.IdDetalleOT
JOIN dbo.OrdenTrabajoDetalleArea da ON da.IdDetalleArea=m.IdDetalleArea
JOIN dbo.AreaProduccion a ON a.IdAreaProduccion=da.IdAreaProduccion
JOIN dbo.Usuarios us ON us.IdUsuario=m.IdUsuarioSesion
LEFT JOIN dbo.Usuarios ua ON ua.IdUsuario=m.IdUsuarioAutoriza
WHERE m.IdOrdenTrabajo=@IdOrdenTrabajo
UNION ALL
SELECT c.FechaRegistro FechaHora,d.CodigoProducto,d.NombreProducto,'INSUMOS' Origen,'PRODUCCION' Destino,
       SUM(c.CantidadConsumida) Cantidad,'CONSUMO_INSUMOS' Accion,u.NombreUsuario Usuario,'' Observacion
FROM dbo.OrdenTrabajoConsumoInsumo c
JOIN dbo.OrdenTrabajoDetalle d ON d.IdDetalleOT=c.IdDetalleOT
JOIN dbo.Usuarios u ON u.IdUsuario=c.IdUsuario
WHERE c.IdOrdenTrabajo=@IdOrdenTrabajo
GROUP BY c.FechaRegistro,d.CodigoProducto,d.NombreProducto,u.NombreUsuario
UNION ALL
SELECT t.FechaRegistro FechaHora,d.CodigoProducto,d.NombreProducto,a.NombreArea Origen,'PRODUCTO TERMINADO' Destino,
       td.Cantidad,'CIERRE_PRODUCCION' Accion,ISNULL(ua.NombreUsuario,us.NombreUsuario) Usuario,t.Observacion
FROM dbo.OrdenTrabajoTerminacion t
JOIN dbo.OrdenTrabajoTerminacionDetalle td ON td.IdOperacionTerminacion=t.IdOperacionTerminacion
JOIN dbo.OrdenTrabajoDetalle d ON d.IdDetalleOT=td.IdDetalleOT
JOIN dbo.AreaProduccion a ON a.IdAreaProduccion=t.IdAreaTermino
JOIN dbo.Usuarios us ON us.IdUsuario=t.IdUsuarioSesion
LEFT JOIN dbo.Usuarios ua ON ua.IdUsuario=t.IdUsuarioAutoriza
WHERE t.IdOrdenTrabajo=@IdOrdenTrabajo
UNION ALL
SELECT k.FechaMovimiento FechaHora,d.CodigoProducto,d.NombreProducto,'PRODUCCION' Origen,al.NombreAlmacen Destino,
       k.Cantidad,'INGRESO_KARDEX' Accion,k.UsuarioResponsable Usuario,k.Observacion
FROM dbo.KardexProductos k
JOIN dbo.OrdenTrabajoTerminacion t ON t.IdOperacionTerminacion=k.IdOperacionTerminacion
JOIN dbo.OrdenTrabajoTerminacionDetalle td ON td.IdOperacionTerminacion=t.IdOperacionTerminacion
JOIN dbo.OrdenTrabajoDetalle d ON d.IdDetalleOT=td.IdDetalleOT AND d.IdProducto=k.IdProducto
JOIN dbo.Almacenes al ON al.IdAlmacen=k.IdAlmacen
WHERE t.IdOrdenTrabajo=@IdOrdenTrabajo
ORDER BY FechaHora DESC;";

            List<OrdenTrabajoMovimiento> lista = [];
            using SqlConnection cn = Conexion.ObtenerConexion();
            using SqlCommand cmd = new(sql, cn);
            cmd.Parameters.AddWithValue("@IdOrdenTrabajo", idOrdenTrabajo);
            cn.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new OrdenTrabajoMovimiento
                {
                    FechaHora = Convert.ToDateTime(dr["FechaHora"]),
                    CodigoProducto = Texto(dr, "CodigoProducto"),
                    NombreProducto = Texto(dr, "NombreProducto"),
                    Origen = Texto(dr, "Origen"),
                    Destino = Texto(dr, "Destino"),
                    Cantidad = Decimal(dr, "Cantidad"),
                    Accion = Texto(dr, "Accion"),
                    Usuario = Texto(dr, "Usuario"),
                    Observacion = Texto(dr, "Observacion")
                });
            }
            return lista;
        }

        public List<OrdenTrabajoKardexIngreso> ListarIngresosKardex(int idOrdenTrabajo)
        {
            const string sql = @"
SELECT d.CodigoProducto,d.NombreProducto,k.Cantidad,al.NombreAlmacen Almacen,k.FechaMovimiento,k.UsuarioResponsable Usuario
FROM dbo.KardexProductos k
JOIN dbo.OrdenTrabajoTerminacion t ON t.IdOperacionTerminacion=k.IdOperacionTerminacion
JOIN dbo.OrdenTrabajoTerminacionDetalle td ON td.IdOperacionTerminacion=t.IdOperacionTerminacion
JOIN dbo.OrdenTrabajoDetalle d ON d.IdDetalleOT=td.IdDetalleOT AND d.IdProducto=k.IdProducto
JOIN dbo.Almacenes al ON al.IdAlmacen=k.IdAlmacen
WHERE t.IdOrdenTrabajo=@IdOrdenTrabajo
ORDER BY k.FechaMovimiento DESC;";

            List<OrdenTrabajoKardexIngreso> lista = [];
            using SqlConnection cn = Conexion.ObtenerConexion();
            using SqlCommand cmd = new(sql, cn);
            cmd.Parameters.AddWithValue("@IdOrdenTrabajo", idOrdenTrabajo);
            cn.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new OrdenTrabajoKardexIngreso
                {
                    CodigoProducto = Texto(dr, "CodigoProducto"),
                    NombreProducto = Texto(dr, "NombreProducto"),
                    Cantidad = Decimal(dr, "Cantidad"),
                    Almacen = Texto(dr, "Almacen"),
                    FechaMovimiento = Convert.ToDateTime(dr["FechaMovimiento"]),
                    Usuario = Texto(dr, "Usuario")
                });
            }
            return lista;
        }

        public void ConfirmarConsumo(int idDetalleOt,int idUsuario){using SqlConnection cn=Conexion.ObtenerConexion();using SqlCommand cmd=new("USP_PRO_OT_CONSUMO_CONFIRMAR",cn){CommandType=CommandType.StoredProcedure};cmd.Parameters.AddWithValue("@IdDetalleOT",idDetalleOt);cmd.Parameters.AddWithValue("@IdUsuario",idUsuario);cn.Open();cmd.ExecuteNonQuery();}

        private static DataTable TablaPlanificacion(IEnumerable<OrdenTrabajoPlanificacion> items) { DataTable t=new(); t.Columns.Add("IdOrdenCompraInternaDetalle",typeof(int)); t.Columns.Add("CantidadPlanificada",typeof(decimal)); foreach(var x in items)t.Rows.Add(x.IdOrdenCompraInternaDetalle,x.CantidadPlanificada); return t; }
        private static DataTable TablaPlanificacionManual(IEnumerable<OrdenTrabajoManualPlanificacion> items) { DataTable t=new(); t.Columns.Add("IdProducto",typeof(int)); t.Columns.Add("CantidadPlanificada",typeof(decimal)); foreach(var x in items)t.Rows.Add(x.IdProducto,x.CantidadPlanificada); return t; }
        private static DataTable TablaLanzamiento(IEnumerable<OrdenTrabajoLanzamiento> items) { DataTable t=new(); t.Columns.Add("IdDetalleOT",typeof(int));t.Columns.Add("CantidadLanzada",typeof(decimal));t.Columns.Add("Motivo",typeof(string));t.Columns.Add("Observacion",typeof(string));foreach(var x in items)t.Rows.Add(x.IdDetalleOT,x.CantidadLanzada,x.Motivo,x.Observacion);return t; }
        private static DataTable TablaTransferencia(IEnumerable<OrdenTrabajoTransferenciaItem> items) { DataTable t=new();t.Columns.Add("IdDetalleOT",typeof(int));t.Columns.Add("Cantidad",typeof(decimal));foreach(var x in items)t.Rows.Add(x.IdDetalleOT,x.Cantidad);return t; }
        private static OrdenTrabajoDetalle MapearDetalle(SqlDataReader dr)=>new(){IdDetalleOT=Convert.ToInt32(dr["IdDetalleOT"]),IdOrdenTrabajo=Convert.ToInt32(dr["IdOrdenTrabajo"]),IdOrdenCompraInternaDetalle=dr["IdOrdenCompraInternaDetalle"] is DBNull ? 0 : Convert.ToInt32(dr["IdOrdenCompraInternaDetalle"]),IdProducto=Convert.ToInt32(dr["IdProducto"]),CodigoProducto=Texto(dr,"CodigoProducto"),NombreProducto=Texto(dr,"NombreProducto"),CantidadRequerida=Decimal(dr,"CantidadRequerida"),CantidadPlanificada=Decimal(dr,"CantidadPlanificada"),CantidadLanzada=Decimal(dr,"CantidadLanzada"),CantidadProducida=Decimal(dr,"CantidadProducida"),CantidadAplicada=Decimal(dr,"CantidadAplicada"),CantidadExcedente=Decimal(dr,"CantidadExcedente"),CantidadPendiente=Decimal(dr,"CantidadPendiente"),Estado=Texto(dr,"Estado"),MotivoDiferencia=Texto(dr,"MotivoDiferencia"),ObservacionDiferencia=Texto(dr,"ObservacionDiferencia")};
        private static OrdenTrabajoDetalleArea MapearArea(SqlDataReader dr)=>new(){IdDetalleArea=Convert.ToInt64(dr["IdDetalleArea"]),IdOrdenTrabajo=Convert.ToInt32(dr["IdOrdenTrabajo"]),IdDetalleOT=Convert.ToInt32(dr["IdDetalleOT"]),IdAreaProduccion=Convert.ToInt32(dr["IdAreaProduccion"]),CodigoArea=Texto(dr,"CodigoArea"),NombreArea=Texto(dr,"NombreArea"),OrdenSecuencia=Convert.ToInt32(dr["OrdenSecuencia"]),EsInicio=Convert.ToBoolean(dr["EsInicio"]),EsTermino=Convert.ToBoolean(dr["EsTermino"]),ManejaMerma=Convert.ToBoolean(dr["ManejaMerma"]),PermiteReservarStockProceso=BooleanoOpcional(dr,"PermiteReservarStockProceso"),ModoEnvio=Texto(dr,"ModoEnvio"),CantidadRecibida=Decimal(dr,"CantidadRecibida"),CantidadEnviada=Decimal(dr,"CantidadEnviada"),CantidadMerma=Decimal(dr,"CantidadMerma"),CantidadReservada=DecimalOpcional(dr,"CantidadReservada"),CantidadPendiente=Decimal(dr,"CantidadPendiente"),Estado=Texto(dr,"Estado"),CodigoProducto=Texto(dr,"CodigoProducto"),NombreProducto=Texto(dr,"NombreProducto")};
        private static string Texto(SqlDataReader dr,string c)=>dr[c] is DBNull?string.Empty:dr[c]?.ToString()??string.Empty;
        private static decimal Decimal(SqlDataReader dr,string c)=>dr[c] is DBNull?0:Convert.ToDecimal(dr[c]);
        private static decimal DecimalOpcional(SqlDataReader dr,string c){try{int o=dr.GetOrdinal(c);return dr.IsDBNull(o)?0:Convert.ToDecimal(dr.GetValue(o));}catch(IndexOutOfRangeException){return 0;}}
        private static bool BooleanoOpcional(SqlDataReader dr,string c){try{int o=dr.GetOrdinal(c);return !dr.IsDBNull(o)&&Convert.ToBoolean(dr.GetValue(o));}catch(IndexOutOfRangeException){return false;}}
    }
}
