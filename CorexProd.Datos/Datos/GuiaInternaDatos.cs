using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class GuiaInternaDatos
    {
        public List<GuiaInterna> Listar(DateTime? desde, DateTime? hasta, int? idAlmacen, string estado, string origen, string texto)
        {
            List<GuiaInterna> lista = [];
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_VEN_GUIA_INTERNA_LISTAR", conexion) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@FechaDesde", (object?)desde?.Date ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FechaHasta", (object?)hasta?.Date ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IdAlmacen", (object?)idAlmacen ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Estado", estado == "Todos" || string.IsNullOrWhiteSpace(estado) ? DBNull.Value : estado);
            cmd.Parameters.AddWithValue("@Origen", origen == "Todos" || string.IsNullOrWhiteSpace(origen) ? DBNull.Value : origen);
            cmd.Parameters.AddWithValue("@Texto", string.IsNullOrWhiteSpace(texto) ? DBNull.Value : texto.Trim());
            conexion.Open(); using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read()) lista.Add(MapearCabecera(dr));
            return lista;
        }

        public GuiaInterna? Obtener(int idGuiaInterna)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_VEN_GUIA_INTERNA_OBTENER", conexion) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@IdGuiaInterna", idGuiaInterna);
            conexion.Open(); using SqlDataReader dr = cmd.ExecuteReader();
            if (!dr.Read()) return null;
            GuiaInterna guia = MapearCabecera(dr);
            if (dr.NextResult()) while (dr.Read()) guia.Detalles.Add(MapearDetalle(dr));
            return guia;
        }

        public GuiaInterna? Preparar(int idOrdenCompraInterna, int? idAlmacen = null)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_VEN_GUIA_INTERNA_PREPARAR", conexion) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@IdOrdenCompraInterna", idOrdenCompraInterna);
            cmd.Parameters.AddWithValue("@IdAlmacen", (object?)idAlmacen ?? DBNull.Value);
            conexion.Open();
            using SqlDataReader dr = cmd.ExecuteReader();

            if (!dr.Read()) return null;

            GuiaInterna guia = new()
            {
                IdOrdenCompraInterna = Convert.ToInt32(dr["IdOrdenCompraInterna"]),
                NumeroOci = dr["NumeroOci"]?.ToString() ?? string.Empty,
                OrdenCompraCliente = dr["OrdenCompraCliente"]?.ToString() ?? string.Empty,
                FechaEmision = DateTime.Today,
                IdAlmacen = Convert.ToInt32(dr["IdAlmacen"]),
                NombreAlmacen = dr["NombreAlmacen"]?.ToString() ?? string.Empty,
                RucEmisor = dr["RucEmisor"]?.ToString() ?? string.Empty,
                EmpresaEmisora = dr["EmpresaEmisora"]?.ToString() ?? string.Empty,
                RucDestino = dr["RucDestino"]?.ToString() ?? string.Empty,
                EmpresaDestino = dr["EmpresaDestino"]?.ToString() ?? string.Empty
            };

            if (dr.NextResult())
            {
                while (dr.Read())
                {
                    guia.Detalles.Add(new GuiaInternaDetalle
                    {
                        IdOrdenCompraInternaDetalle = Convert.ToInt32(dr["IdOrdenCompraInternaDetalle"]),
                        IdProducto = Convert.ToInt32(dr["IdProducto"]),
                        CodigoProducto = dr["CodigoProducto"]?.ToString() ?? string.Empty,
                        NombreProducto = dr["NombreProducto"]?.ToString() ?? string.Empty,
                        IdUnidadMedida = Convert.ToInt32(dr["IdUnidadMedida"]),
                        NombreUnidad = dr["NombreUnidad"]?.ToString() ?? string.Empty,
                        CantidadRequerida = Convert.ToDecimal(dr["CantidadRequerida"]),
                        CantidadEntregada = Convert.ToDecimal(dr["CantidadEntregada"]),
                        CantidadPendiente = Convert.ToDecimal(dr["CantidadPendiente"]),
                        StockActual = Convert.ToDecimal(dr["StockActual"]),
                        PrecioUnitario = Convert.ToDecimal(dr["PrecioUnitario"]),
                        CantidadDespachar = Convert.ToDecimal(dr["CantidadSugerida"]),
                        Observacion = dr["Observacion"]?.ToString() ?? string.Empty
                    });
                }
            }

            return guia;
        }

        public string Emitir(GuiaInterna guia, out string numeroGuia)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_VEN_GUIA_INTERNA_EMITIR", conexion) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@IdOrdenCompraInterna", guia.IdOrdenCompraInterna);
            cmd.Parameters.AddWithValue("@IdAlmacen", guia.IdAlmacen);
            cmd.Parameters.AddWithValue("@FechaEmision", guia.FechaEmision.Date);
            cmd.Parameters.AddWithValue("@UsuarioEmisor", guia.UsuarioEmisor);
            cmd.Parameters.AddWithValue("@UsuarioAutorizador", guia.UsuarioAutorizador);
            cmd.Parameters.AddWithValue("@Observacion", guia.Observacion ?? string.Empty);
            SqlParameter detalles = cmd.Parameters.AddWithValue("@Detalles", CrearTablaDetalles(guia));
            detalles.SqlDbType = SqlDbType.Structured;
            detalles.TypeName = "dbo.GuiaInternaDetalleType";
            SqlParameter numero = new("@NumeroGuia", SqlDbType.VarChar, 30) { Direction = ParameterDirection.Output };
            SqlParameter mensaje = new("@Mensaje", SqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(numero);
            cmd.Parameters.Add(mensaje);
            conexion.Open();
            cmd.ExecuteNonQuery();
            numeroGuia = numero.Value?.ToString() ?? string.Empty;
            return mensaje.Value?.ToString() ?? string.Empty;
        }

        public GuiaInterna? PrepararManual(int? idAlmacen = null)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_VEN_GUIA_INTERNA_MANUAL_PREPARAR", conexion) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@IdAlmacen", (object?)idAlmacen ?? DBNull.Value);
            conexion.Open(); using SqlDataReader dr = cmd.ExecuteReader();
            if (!dr.Read()) return null;
            GuiaInterna guia = new()
            {
                Origen = "Manual", IdAlmacen = Convert.ToInt32(dr["IdAlmacen"]),
                NombreAlmacen = dr["NombreAlmacen"]?.ToString() ?? string.Empty,
                RucEmisor = dr["RucEmisor"]?.ToString() ?? string.Empty,
                EmpresaEmisora = dr["EmpresaEmisora"]?.ToString() ?? string.Empty,
                EmpresaDestino = string.Empty, FechaEmision = DateTime.Today
            };
            if (dr.NextResult()) while (dr.Read()) guia.Detalles.Add(new GuiaInternaDetalle
            {
                IdProducto = Convert.ToInt32(dr["IdProducto"]), CodigoProducto = dr["CodigoProducto"]?.ToString() ?? string.Empty,
                NombreProducto = dr["NombreProducto"]?.ToString() ?? string.Empty,
                IdUnidadMedida = Convert.ToInt32(dr["IdUnidadMedida"]), NombreUnidad = dr["NombreUnidad"]?.ToString() ?? string.Empty,
                StockActual = Convert.ToDecimal(dr["StockActual"]), CantidadPendiente = Convert.ToDecimal(dr["StockActual"])
            });
            return guia;
        }

        public string EmitirManual(GuiaInterna guia, out string numeroGuia)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_VEN_GUIA_INTERNA_MANUAL_EMITIR", conexion) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@IdAlmacen", guia.IdAlmacen); cmd.Parameters.AddWithValue("@FechaEmision", guia.FechaEmision.Date);
            cmd.Parameters.AddWithValue("@UsuarioEmisor", guia.UsuarioEmisor); cmd.Parameters.AddWithValue("@UsuarioAutorizador", guia.UsuarioAutorizador);
            cmd.Parameters.AddWithValue("@IdCliente", (object?)guia.IdCliente ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@MotivoEmisionManual", guia.MotivoEmisionManual); cmd.Parameters.AddWithValue("@Observacion", guia.Observacion ?? string.Empty);
            SqlParameter detalles = cmd.Parameters.AddWithValue("@Detalles", CrearTablaDetallesManual(guia));
            detalles.SqlDbType = SqlDbType.Structured; detalles.TypeName = "dbo.GuiaInternaManualDetalleType";
            SqlParameter numero = new("@NumeroGuia", SqlDbType.VarChar, 30) { Direction = ParameterDirection.Output };
            SqlParameter mensaje = new("@Mensaje", SqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(numero); cmd.Parameters.Add(mensaje); conexion.Open(); cmd.ExecuteNonQuery();
            numeroGuia = numero.Value?.ToString() ?? string.Empty; return mensaje.Value?.ToString() ?? string.Empty;
        }

        public string Anular(int idGuiaInterna, string usuario, string motivo)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_VEN_GUIA_INTERNA_ANULAR", conexion) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@IdGuiaInterna", idGuiaInterna); cmd.Parameters.AddWithValue("@Usuario", usuario); cmd.Parameters.AddWithValue("@Motivo", motivo);
            SqlParameter mensaje = new("@Mensaje", SqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(mensaje); conexion.Open(); cmd.ExecuteNonQuery(); return mensaje.Value?.ToString() ?? string.Empty;
        }

        private static DataTable CrearTablaDetalles(GuiaInterna guia)
        {
            DataTable tabla = new();
            tabla.Columns.Add("IdOrdenCompraInternaDetalle", typeof(int));
            tabla.Columns.Add("CantidadDespachar", typeof(decimal));
            tabla.Columns.Add("Observacion", typeof(string));
            foreach (GuiaInternaDetalle detalle in guia.Detalles)
            {
                if (detalle.CantidadDespachar > 0)
                    tabla.Rows.Add(detalle.IdOrdenCompraInternaDetalle, detalle.CantidadDespachar, detalle.Observacion ?? string.Empty);
            }
            return tabla;
        }

        private static DataTable CrearTablaDetallesManual(GuiaInterna guia)
        {
            DataTable tabla = new(); tabla.Columns.Add("IdProducto", typeof(int)); tabla.Columns.Add("CantidadDespachar", typeof(decimal)); tabla.Columns.Add("Observacion", typeof(string));
            foreach (GuiaInternaDetalle d in guia.Detalles) if (d.CantidadDespachar > 0) tabla.Rows.Add(d.IdProducto, d.CantidadDespachar, d.Observacion ?? string.Empty);
            return tabla;
        }

        private static GuiaInterna MapearCabecera(SqlDataReader dr) => new()
        {
            IdGuiaInterna=Convert.ToInt32(dr["IdGuiaInterna"]), NumeroGuia=dr["NumeroGuia"]?.ToString() ?? string.Empty,
            Origen=dr["Origen"]?.ToString() ?? string.Empty, IdOrdenCompraInterna=Convert.ToInt32(dr["IdOrdenCompraInterna"]),
            IdCliente=dr["IdCliente"] == DBNull.Value ? null : Convert.ToInt32(dr["IdCliente"]),
            NumeroOci=dr["NumeroOci"]?.ToString() ?? string.Empty, NumeroProforma=dr["NumeroProforma"]?.ToString() ?? string.Empty,
            OrdenCompraCliente=dr["OrdenCompraCliente"]?.ToString() ?? string.Empty,
            FechaEmision=Convert.ToDateTime(dr["FechaEmision"]), IdAlmacen=Convert.ToInt32(dr["IdAlmacen"]), NombreAlmacen=dr["NombreAlmacen"]?.ToString() ?? string.Empty,
            RucEmisor=dr["RucEmisor"]?.ToString() ?? string.Empty, EmpresaEmisora=dr["EmpresaEmisora"]?.ToString() ?? string.Empty,
            RucDestino=dr["RucDestino"]?.ToString() ?? string.Empty, EmpresaDestino=dr["EmpresaDestino"]?.ToString() ?? string.Empty,
            UsuarioEmisor=dr["UsuarioEmisor"]?.ToString() ?? string.Empty, UsuarioAutorizador=dr["UsuarioAutorizador"]?.ToString() ?? string.Empty,
            Observacion=dr["Observacion"]?.ToString() ?? string.Empty, MotivoEmisionManual=dr["MotivoEmisionManual"]?.ToString() ?? string.Empty,
            Estado=dr["Estado"]?.ToString() ?? string.Empty, UsuarioAnulacion=dr["UsuarioAnulacion"]?.ToString() ?? string.Empty,
            FechaAnulacion=dr["FechaAnulacion"] == DBNull.Value ? null : Convert.ToDateTime(dr["FechaAnulacion"]),
            MotivoAnulacion=dr["MotivoAnulacion"]?.ToString() ?? string.Empty, FechaRegistro=Convert.ToDateTime(dr["FechaRegistro"])
        };

        private static GuiaInternaDetalle MapearDetalle(SqlDataReader dr) => new()
        {
            IdOrdenCompraInternaDetalle=Convert.ToInt32(dr["IdOrdenCompraInternaDetalle"]), IdProducto=Convert.ToInt32(dr["IdProducto"]),
            CodigoProducto=dr["CodigoProducto"]?.ToString() ?? string.Empty, NombreProducto=dr["NombreProducto"]?.ToString() ?? string.Empty,
            IdUnidadMedida=Convert.ToInt32(dr["IdUnidadMedida"]), NombreUnidad=dr["NombreUnidad"]?.ToString() ?? string.Empty,
            CantidadRequerida=Convert.ToDecimal(dr["CantidadRequerida"]), CantidadEntregada=Convert.ToDecimal(dr["CantidadEntregada"]),
            CantidadPendiente=Convert.ToDecimal(dr["CantidadPendiente"]), StockActual=Convert.ToDecimal(dr["StockActual"]),
            PrecioUnitario=Convert.ToDecimal(dr["PrecioUnitario"]), CantidadDespachar=Convert.ToDecimal(dr["CantidadSugerida"]),
            Observacion=dr["Observacion"]?.ToString() ?? string.Empty
        };
    }
}
