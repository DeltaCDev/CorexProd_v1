using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class OrdenTrabajoDatos
    {
        public List<OrdenTrabajo> Listar()
        {
            List<OrdenTrabajo> lista = [];
            using SqlConnection cn = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_PRO_OT_LISTAR", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read()) lista.Add(new OrdenTrabajo
            {
                IdOrdenTrabajo = Convert.ToInt32(dr["IdOrdenTrabajo"]), NumeroOT = Texto(dr, "NumeroOT"),
                IdOrdenCompraInterna = Convert.ToInt32(dr["IdOrdenCompraInterna"]), NumeroOci = Texto(dr, "NumeroOci"),
                OrdenCompraCliente = Texto(dr,"OrdenCompraCliente"), TipoOT=Texto(dr,"TipoOT"),
                IdOrdenTrabajoRelacionada=dr["IdOrdenTrabajoRelacionada"] is DBNull?null:Convert.ToInt32(dr["IdOrdenTrabajoRelacionada"]),NumeroOTRelacionada=Texto(dr,"NumeroOTRelacionada"),
                IdCliente = Convert.ToInt32(dr["IdCliente"]), NombreCliente = Texto(dr, "NombreCliente"),
                FechaEmision = Convert.ToDateTime(dr["FechaEmision"]), Estado = Texto(dr, "Estado"),
                IdUsuarioCreacion = Convert.ToInt32(dr["IdUsuarioCreacion"]), UsuarioCreacion = Texto(dr, "NombreUsuario"),
                UsuarioAutoriza=Texto(dr,"UsuarioAutoriza"),
                Observacion = Texto(dr, "Observacion"), FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"]),
                CantidadProductos = Convert.ToInt32(dr["CantidadProductos"]), TotalPlanificado = Decimal(dr, "TotalPlanificado"), TotalLanzado = Decimal(dr, "TotalLanzado")
            });
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
                IdOrdenTrabajo = id, NumeroOT = Texto(dr,"NumeroOT"), IdOrdenCompraInterna = Convert.ToInt32(dr["IdOrdenCompraInterna"]),
                NumeroOci = Texto(dr,"NumeroOci"), OrdenCompraCliente=Texto(dr,"OrdenCompraCliente"),TipoOT=Texto(dr,"TipoOT"),IdCliente = Convert.ToInt32(dr["IdCliente"]), NombreCliente = Texto(dr,"NombreCliente"),
                FechaEmision = Convert.ToDateTime(dr["FechaEmision"]), Estado = Texto(dr,"Estado"), IdUsuarioCreacion = Convert.ToInt32(dr["IdUsuarioCreacion"]),
                UsuarioCreacion = Texto(dr,"NombreUsuario"),UsuarioAutoriza=Texto(dr,"UsuarioAutoriza"), Observacion = Texto(dr,"Observacion"), FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
            };
            if (dr.NextResult()) while (dr.Read()) ot.Detalles.Add(MapearDetalle(dr));
            if (dr.NextResult()) while (dr.Read()) ot.Areas.Add(MapearArea(dr));
            return ot;
        }

        public (int Id, string Numero) Crear(int idOci, int idUsuario, string observacion, IEnumerable<OrdenTrabajoPlanificacion> items)
        {
            using SqlConnection cn = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_PRO_OT_CREAR", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@IdOrdenCompraInterna", idOci); cmd.Parameters.AddWithValue("@IdUsuario", idUsuario); cmd.Parameters.AddWithValue("@Observacion", observacion ?? string.Empty);
            cmd.Parameters.Add(new SqlParameter("@Detalles", SqlDbType.Structured) { TypeName="dbo.TipoOTPlanificacion", Value=TablaPlanificacion(items) });
            SqlParameter id = new("@IdOrdenTrabajo", SqlDbType.Int) { Direction=ParameterDirection.Output };
            SqlParameter numero = new("@NumeroOT", SqlDbType.VarChar,30) { Direction=ParameterDirection.Output };
            cmd.Parameters.Add(id); cmd.Parameters.Add(numero); cn.Open(); cmd.ExecuteNonQuery();
            return (Convert.ToInt32(id.Value), numero.Value?.ToString() ?? string.Empty);
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
        private static DataTable TablaLanzamiento(IEnumerable<OrdenTrabajoLanzamiento> items) { DataTable t=new(); t.Columns.Add("IdDetalleOT",typeof(int));t.Columns.Add("CantidadLanzada",typeof(decimal));t.Columns.Add("Motivo",typeof(string));t.Columns.Add("Observacion",typeof(string));foreach(var x in items)t.Rows.Add(x.IdDetalleOT,x.CantidadLanzada,x.Motivo,x.Observacion);return t; }
        private static DataTable TablaTransferencia(IEnumerable<OrdenTrabajoTransferenciaItem> items) { DataTable t=new();t.Columns.Add("IdDetalleOT",typeof(int));t.Columns.Add("Cantidad",typeof(decimal));foreach(var x in items)t.Rows.Add(x.IdDetalleOT,x.Cantidad);return t; }
        private static OrdenTrabajoDetalle MapearDetalle(SqlDataReader dr)=>new(){IdDetalleOT=Convert.ToInt32(dr["IdDetalleOT"]),IdOrdenTrabajo=Convert.ToInt32(dr["IdOrdenTrabajo"]),IdOrdenCompraInternaDetalle=Convert.ToInt32(dr["IdOrdenCompraInternaDetalle"]),IdProducto=Convert.ToInt32(dr["IdProducto"]),CodigoProducto=Texto(dr,"CodigoProducto"),NombreProducto=Texto(dr,"NombreProducto"),CantidadRequerida=Decimal(dr,"CantidadRequerida"),CantidadPlanificada=Decimal(dr,"CantidadPlanificada"),CantidadLanzada=Decimal(dr,"CantidadLanzada"),CantidadProducida=Decimal(dr,"CantidadProducida"),CantidadAplicada=Decimal(dr,"CantidadAplicada"),CantidadExcedente=Decimal(dr,"CantidadExcedente"),CantidadPendiente=Decimal(dr,"CantidadPendiente"),Estado=Texto(dr,"Estado"),MotivoDiferencia=Texto(dr,"MotivoDiferencia"),ObservacionDiferencia=Texto(dr,"ObservacionDiferencia")};
        private static OrdenTrabajoDetalleArea MapearArea(SqlDataReader dr)=>new(){IdDetalleArea=Convert.ToInt64(dr["IdDetalleArea"]),IdOrdenTrabajo=Convert.ToInt32(dr["IdOrdenTrabajo"]),IdDetalleOT=Convert.ToInt32(dr["IdDetalleOT"]),IdAreaProduccion=Convert.ToInt32(dr["IdAreaProduccion"]),CodigoArea=Texto(dr,"CodigoArea"),NombreArea=Texto(dr,"NombreArea"),OrdenSecuencia=Convert.ToInt32(dr["OrdenSecuencia"]),EsInicio=Convert.ToBoolean(dr["EsInicio"]),EsTermino=Convert.ToBoolean(dr["EsTermino"]),ManejaMerma=Convert.ToBoolean(dr["ManejaMerma"]),ModoEnvio=Texto(dr,"ModoEnvio"),CantidadRecibida=Decimal(dr,"CantidadRecibida"),CantidadEnviada=Decimal(dr,"CantidadEnviada"),CantidadMerma=Decimal(dr,"CantidadMerma"),CantidadPendiente=Decimal(dr,"CantidadPendiente"),Estado=Texto(dr,"Estado"),CodigoProducto=Texto(dr,"CodigoProducto"),NombreProducto=Texto(dr,"NombreProducto")};
        private static string Texto(SqlDataReader dr,string c)=>dr[c] is DBNull?string.Empty:dr[c]?.ToString()??string.Empty;
        private static decimal Decimal(SqlDataReader dr,string c)=>dr[c] is DBNull?0:Convert.ToDecimal(dr[c]);
    }
}
