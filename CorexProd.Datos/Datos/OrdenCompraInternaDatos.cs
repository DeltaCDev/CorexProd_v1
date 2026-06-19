using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class OrdenCompraInternaDatos
    {
        public List<OrdenCompraInterna> Listar()
        {
            List<OrdenCompraInterna> lista = [];
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_VEN_OCI_LISTAR", conexion) { CommandType = CommandType.StoredProcedure };
            conexion.Open();
            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(Mapear(dr));
            }

            return lista;
        }

        public OrdenCompraInterna? Obtener(int idOrdenCompraInterna)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_VEN_OCI_OBTENER", conexion) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@IdOrdenCompraInterna", idOrdenCompraInterna);
            conexion.Open();
            using SqlDataReader dr = cmd.ExecuteReader();

            if (!dr.Read())
            {
                return null;
            }

            OrdenCompraInterna oci = Mapear(dr);

            if (dr.NextResult())
            {
                while (dr.Read())
                {
                    oci.Detalles.Add(new OrdenCompraInternaDetalle
                    {
                        IdOrdenCompraInternaDetalle = Convert.ToInt32(dr["IdOrdenCompraInternaDetalle"]),
                        IdOrdenCompraInterna = Convert.ToInt32(dr["IdOrdenCompraInterna"]),
                        IdProducto = Convert.ToInt32(dr["IdProducto"]),
                        CodigoProducto = dr["CodigoProducto"]?.ToString() ?? string.Empty,
                        NombreProducto = dr["NombreProducto"]?.ToString() ?? string.Empty,
                        Cantidad = Convert.ToDecimal(dr["Cantidad"]),
                        StockActual = Convert.ToDecimal(dr["StockActual"]),
                        CantidadDespachada = Convert.ToDecimal(dr["CantidadDespachada"]),
                        PrecioUnitario = Convert.ToDecimal(dr["PrecioUnitario"]),
                        Descuento = Convert.ToDecimal(dr["Descuento"]),
                        Importe = Convert.ToDecimal(dr["Importe"]),
                        Observacion = dr["Observacion"]?.ToString() ?? string.Empty
                    });
                }
            }

            return oci;
        }

        public string Generar(int idProforma, string usuarioGenerador)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_VEN_OCI_GENERAR", conexion) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@IdProforma", idProforma);
            cmd.Parameters.AddWithValue("@UsuarioGenerador", usuarioGenerador);
            cmd.Parameters.Add(new SqlParameter("@IdGenerado", SqlDbType.Int) { Direction = ParameterDirection.Output });
            cmd.Parameters.Add(new SqlParameter("@NumeroOci", SqlDbType.VarChar, 40) { Direction = ParameterDirection.Output });
            cmd.Parameters.Add(new SqlParameter("@Resultado", SqlDbType.Bit) { Direction = ParameterDirection.Output });
            SqlParameter mensaje = new("@Mensaje", SqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(mensaje);
            conexion.Open();
            cmd.ExecuteNonQuery();
            return mensaje.Value?.ToString() ?? string.Empty;
        }

        public string Anular(int idOrdenCompraInterna, string motivoAnulacion, string usuarioAnulacion)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_VEN_OCI_ANULAR", conexion) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@IdOrdenCompraInterna", idOrdenCompraInterna);
            cmd.Parameters.AddWithValue("@MotivoAnulacion", motivoAnulacion);
            cmd.Parameters.AddWithValue("@UsuarioAnulacion", usuarioAnulacion);
            SqlParameter mensaje = new("@Mensaje", SqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(mensaje);
            conexion.Open();
            cmd.ExecuteNonQuery();
            return mensaje.Value?.ToString() ?? string.Empty;
        }

        private static OrdenCompraInterna Mapear(SqlDataReader dr)
        {
            return new OrdenCompraInterna
            {
                IdOrdenCompraInterna = Convert.ToInt32(dr["IdOrdenCompraInterna"]),
                NumeroOci = dr["NumeroOci"]?.ToString() ?? string.Empty,
                IdProforma = Convert.ToInt32(dr["IdProforma"]),
                NumeroProforma = dr["NumeroProforma"]?.ToString() ?? string.Empty,
                FechaEmision = Convert.ToDateTime(dr["FechaEmision"]),
                OrdenCompraCliente = dr["OrdenCompraCliente"]?.ToString() ?? string.Empty,
                IdCliente = Convert.ToInt32(dr["IdCliente"]),
                NombreCliente = dr["NombreCliente"]?.ToString() ?? string.Empty,
                Subtotal = Convert.ToDecimal(dr["Subtotal"]),
                Descuento = Convert.ToDecimal(dr["Descuento"]),
                Igv = Convert.ToDecimal(dr["Igv"]),
                IgvPorcentaje = Convert.ToDecimal(dr["IgvPorcentaje"]),
                CondicionTributaria = dr["CondicionTributaria"]?.ToString() ?? string.Empty,
                Total = Convert.ToDecimal(dr["Total"]),
                Estado = dr["Estado"]?.ToString() ?? string.Empty,
                UsuarioGenerador = dr["UsuarioGenerador"]?.ToString() ?? string.Empty,
                FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"]),
                MotivoAnulacion = dr["MotivoAnulacion"]?.ToString() ?? string.Empty,
                UsuarioAnulacion = dr["UsuarioAnulacion"]?.ToString() ?? string.Empty,
                FechaAnulacion = dr["FechaAnulacion"] == DBNull.Value ? null : Convert.ToDateTime(dr["FechaAnulacion"]),
                TieneGuiaSalida = Convert.ToBoolean(dr["TieneGuiaSalida"]),
                TieneOrdenTrabajo = Convert.ToBoolean(dr["TieneOrdenTrabajo"]),
                PuedeGenerarOt = Convert.ToBoolean(dr["PuedeGenerarOt"]),
                PuedeGenerarGuiaSalida = Convert.ToBoolean(dr["PuedeGenerarGuiaSalida"])
            };
        }
    }
}
