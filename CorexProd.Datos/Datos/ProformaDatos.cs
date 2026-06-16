using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Security;
using System.Text;

namespace CorexProd.Datos.Datos
{
    public class ProformaDatos
    {
        public List<Proforma> Listar()
        {
            List<Proforma> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_VEN_PROFORMA_LISTAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            conexion.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(MapearProforma(dr));
            }

            return lista;
        }

        public Proforma? Obtener(int idProforma)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_VEN_PROFORMA_OBTENER", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@IdProforma", idProforma);

            conexion.Open();

            Proforma? proforma = null;

            using SqlDataReader dr = cmd.ExecuteReader();

            if (dr.Read())
            {
                proforma = MapearProforma(dr);
            }

            if (proforma == null)
            {
                return null;
            }

            if (dr.NextResult())
            {
                while (dr.Read())
                {
                    proforma.Detalles.Add(new ProformaDetalle
                    {
                        IdProformaDetalle = Convert.ToInt32(dr["IdProformaDetalle"]),
                        IdProforma = Convert.ToInt32(dr["IdProforma"]),
                        IdProducto = Convert.ToInt32(dr["IdProducto"]),
                        CodigoProducto = dr["CodigoProducto"]?.ToString() ?? string.Empty,
                        NombreProducto = dr["NombreProducto"]?.ToString() ?? string.Empty,
                        Cantidad = Convert.ToDecimal(dr["Cantidad"]),
                        PrecioUnitario = Convert.ToDecimal(dr["PrecioUnitario"]),
                        Descuento = Convert.ToDecimal(dr["Descuento"]),
                        Importe = Convert.ToDecimal(dr["Importe"]),
                        Observacion = dr["Observacion"]?.ToString() ?? string.Empty
                    });
                }
            }

            return proforma;
        }

        public string Guardar(Proforma proforma)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_VEN_PROFORMA_GUARDAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@IdProforma", proforma.IdProforma);
            cmd.Parameters.AddWithValue("@FechaEmision", proforma.FechaEmision.Date);
            cmd.Parameters.AddWithValue("@FechaVencimiento", proforma.FechaVencimiento.Date);
            cmd.Parameters.AddWithValue("@OrdenCompraCliente", proforma.OrdenCompraCliente);
            cmd.Parameters.AddWithValue("@IdCliente", proforma.IdCliente);
            cmd.Parameters.AddWithValue("@Observacion", proforma.Observacion);
            cmd.Parameters.AddWithValue("@Subtotal", proforma.Subtotal);
            cmd.Parameters.AddWithValue("@Descuento", proforma.Descuento);
            cmd.Parameters.AddWithValue("@Igv", proforma.Igv);
            cmd.Parameters.AddWithValue("@Total", proforma.Total);
            cmd.Parameters.AddWithValue("@DetallesXml", CrearDetallesXml(proforma.Detalles));

            SqlParameter idGenerado = new("@IdGenerado", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            SqlParameter serieNumero = new("@SerieNumero", SqlDbType.VarChar, 40)
            {
                Direction = ParameterDirection.Output
            };

            SqlParameter resultado = new("@Resultado", SqlDbType.Bit)
            {
                Direction = ParameterDirection.Output
            };

            SqlParameter mensajeParam = new("@Mensaje", SqlDbType.VarChar, 500)
            {
                Direction = ParameterDirection.Output
            };

            cmd.Parameters.Add(idGenerado);
            cmd.Parameters.Add(serieNumero);
            cmd.Parameters.Add(resultado);
            cmd.Parameters.Add(mensajeParam);

            conexion.Open();
            ConfigurarOpcionesInsert(conexion);
            cmd.ExecuteNonQuery();

            if (idGenerado.Value != DBNull.Value)
            {
                proforma.IdProforma = Convert.ToInt32(idGenerado.Value);
            }

            proforma.SerieNumero = serieNumero.Value?.ToString() ?? proforma.SerieNumero;

            return mensajeParam.Value?.ToString() ?? string.Empty;
        }

        public string Anular(int idProforma)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_VEN_PROFORMA_ANULAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@IdProforma", idProforma);

            SqlParameter resultado = new("@Resultado", SqlDbType.Bit)
            {
                Direction = ParameterDirection.Output
            };

            SqlParameter mensajeParam = new("@Mensaje", SqlDbType.VarChar, 500)
            {
                Direction = ParameterDirection.Output
            };

            cmd.Parameters.Add(resultado);
            cmd.Parameters.Add(mensajeParam);

            conexion.Open();
            cmd.ExecuteNonQuery();

            return mensajeParam.Value?.ToString() ?? string.Empty;
        }

        private static Proforma MapearProforma(SqlDataReader dr)
        {
            return new Proforma
            {
                IdProforma = Convert.ToInt32(dr["IdProforma"]),
                SerieNumero = dr["SerieNumero"]?.ToString() ?? string.Empty,
                FechaEmision = Convert.ToDateTime(dr["FechaEmision"]),
                FechaVencimiento = Convert.ToDateTime(dr["FechaVencimiento"]),
                OrdenCompraCliente = dr["OrdenCompraCliente"]?.ToString() ?? string.Empty,
                IdCliente = Convert.ToInt32(dr["IdCliente"]),
                NombreCliente = dr["NombreCliente"]?.ToString() ?? string.Empty,
                Observacion = dr["Observacion"]?.ToString() ?? string.Empty,
                Subtotal = Convert.ToDecimal(dr["Subtotal"]),
                Descuento = Convert.ToDecimal(dr["Descuento"]),
                Igv = Convert.ToDecimal(dr["Igv"]),
                Total = Convert.ToDecimal(dr["Total"]),
                Estado = dr["Estado"]?.ToString() ?? string.Empty,
                TieneOrdenCompraInterna = Convert.ToBoolean(dr["TieneOrdenCompraInterna"]),
                FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
            };
        }

        private static string CrearDetallesXml(List<ProformaDetalle> detalles)
        {
            StringBuilder xml = new("<Detalles>");

            foreach (ProformaDetalle detalle in detalles)
            {
                xml.Append("<Detalle ");
                xml.Append(CrearAtributo("IdProducto", detalle.IdProducto.ToString(CultureInfo.InvariantCulture)));
                xml.Append(CrearAtributo("Cantidad", detalle.Cantidad.ToString(CultureInfo.InvariantCulture)));
                xml.Append(CrearAtributo("PrecioUnitario", detalle.PrecioUnitario.ToString(CultureInfo.InvariantCulture)));
                xml.Append(CrearAtributo("Descuento", detalle.Descuento.ToString(CultureInfo.InvariantCulture)));
                xml.Append(CrearAtributo("Importe", detalle.Importe.ToString(CultureInfo.InvariantCulture)));
                xml.Append(CrearAtributo("Observacion", detalle.Observacion));
                xml.Append("/>");
            }

            xml.Append("</Detalles>");
            return xml.ToString();
        }

        private static string CrearAtributo(string nombre, string valor)
        {
            return $"{nombre}=\"{SecurityElement.Escape(valor) ?? string.Empty}\" ";
        }

        private static void ConfigurarOpcionesInsert(SqlConnection conexion)
        {
            using SqlCommand cmd = new(
                """
                SET ANSI_NULLS ON;
                SET ANSI_PADDING ON;
                SET ANSI_WARNINGS ON;
                SET ARITHABORT ON;
                SET CONCAT_NULL_YIELDS_NULL ON;
                SET QUOTED_IDENTIFIER ON;
                SET NUMERIC_ROUNDABORT OFF;
                """,
                conexion);

            cmd.ExecuteNonQuery();
        }
    }
}
