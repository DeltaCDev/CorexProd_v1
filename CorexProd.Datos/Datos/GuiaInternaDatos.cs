using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class GuiaInternaDatos
    {
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
    }
}
