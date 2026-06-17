using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class IngresoManualStockInsumoDatos
    {
        public List<IngresoManualStockInsumo> Listar(DateTime? fechaDesde, DateTime? fechaHasta, int? idProveedor, int? idAlmacen, string estado, string numeroDocumento)
        {
            List<IngresoManualStockInsumo> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_INGRESO_MANUAL_STOCK_INSUMO_LISTAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@FechaDesde", (object?)fechaDesde?.Date ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FechaHasta", (object?)fechaHasta?.Date ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IdProveedor", (object?)idProveedor ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IdAlmacen", (object?)idAlmacen ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Estado", string.IsNullOrWhiteSpace(estado) || estado == "Todos" ? DBNull.Value : estado);
            cmd.Parameters.AddWithValue("@NumeroDocumento", string.IsNullOrWhiteSpace(numeroDocumento) ? DBNull.Value : numeroDocumento.Trim());

            conexion.Open();
            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(MapearCabecera(dr));
            }

            return lista;
        }

        public IngresoManualStockInsumo? Obtener(int idIngresoManualStockInsumo)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_INGRESO_MANUAL_STOCK_INSUMO_OBTENER", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@IdIngresoManualStockInsumo", idIngresoManualStockInsumo);

            conexion.Open();
            using SqlDataReader dr = cmd.ExecuteReader();

            IngresoManualStockInsumo? ingreso = null;

            if (dr.Read())
            {
                ingreso = MapearCabecera(dr);
            }

            if (ingreso == null)
            {
                return null;
            }

            if (dr.NextResult())
            {
                while (dr.Read())
                {
                    ingreso.Detalles.Add(MapearDetalle(dr));
                }
            }

            return ingreso;
        }

        public string Guardar(IngresoManualStockInsumo ingreso, string usuario)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_INGRESO_MANUAL_STOCK_INSUMO_GUARDAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@IdIngresoManualStockInsumo", ingreso.IdIngresoManualStockInsumo);
            cmd.Parameters.AddWithValue("@FechaEmision", ingreso.FechaEmision.Date);
            cmd.Parameters.AddWithValue("@IdProveedor", ingreso.IdProveedor);
            cmd.Parameters.AddWithValue("@IdTipoDocumento", ingreso.IdTipoDocumento);
            cmd.Parameters.AddWithValue("@TipoNumeracion", ingreso.TipoNumeracion);
            cmd.Parameters.AddWithValue("@Serie", ingreso.Serie.Trim());
            cmd.Parameters.AddWithValue("@Numero", ingreso.Numero.Trim());
            cmd.Parameters.AddWithValue("@IdAlmacen", ingreso.IdAlmacen);
            cmd.Parameters.AddWithValue("@Observacion", ingreso.Observacion.Trim());
            cmd.Parameters.AddWithValue("@Usuario", usuario);

            SqlParameter detalles = cmd.Parameters.AddWithValue("@Detalles", CrearTablaDetalles(ingreso.Detalles));
            detalles.SqlDbType = SqlDbType.Structured;
            detalles.TypeName = "dbo.IngresoManualStockInsumoDetalleType";

            SqlParameter mensaje = new("@Mensaje", SqlDbType.VarChar, 500)
            {
                Direction = ParameterDirection.Output
            };
            cmd.Parameters.Add(mensaje);

            conexion.Open();
            cmd.ExecuteNonQuery();

            return mensaje.Value?.ToString() ?? string.Empty;
        }

        public string Abastecer(int idIngresoManualStockInsumo, string usuario)
        {
            return EjecutarAccionEstado("USP_ALM_INGRESO_MANUAL_STOCK_INSUMO_ABASTECER", idIngresoManualStockInsumo, usuario, string.Empty);
        }

        public string Anular(int idIngresoManualStockInsumo, string usuario, string motivo)
        {
            return EjecutarAccionEstado("USP_ALM_INGRESO_MANUAL_STOCK_INSUMO_ANULAR", idIngresoManualStockInsumo, usuario, motivo);
        }

        public List<ProveedorStock> ListarProveedores()
        {
            List<ProveedorStock> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_PROVEEDOR_LISTAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            conexion.Open();
            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new ProveedorStock
                {
                    IdProveedor = Convert.ToInt32(dr["IdProveedor"]),
                    TipoDocumento = dr["TipoDocumento"]?.ToString() ?? string.Empty,
                    NumeroDocumento = dr["NumeroDocumento"]?.ToString() ?? string.Empty,
                    NombreRazonSocial = dr["NombreRazonSocial"]?.ToString() ?? string.Empty,
                    Direccion = dr["Direccion"]?.ToString() ?? string.Empty,
                    Telefono = dr["Telefono"]?.ToString() ?? string.Empty,
                    Correo = dr["Correo"]?.ToString() ?? string.Empty,
                    Estado = Convert.ToBoolean(dr["Estado"])
                });
            }

            return lista;
        }

        public int RegistrarProveedorRapido(ProveedorStock proveedor, out string mensaje)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_PROVEEDOR_REGISTRAR_RAPIDO", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@TipoDocumento", proveedor.TipoDocumento);
            cmd.Parameters.AddWithValue("@NumeroDocumento", proveedor.NumeroDocumento);
            cmd.Parameters.AddWithValue("@NombreRazonSocial", proveedor.NombreRazonSocial);
            cmd.Parameters.AddWithValue("@Direccion", proveedor.Direccion);
            cmd.Parameters.AddWithValue("@Telefono", proveedor.Telefono);
            cmd.Parameters.AddWithValue("@Correo", proveedor.Correo);

            SqlParameter id = new("@IdProveedor", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };
            SqlParameter mensajeParam = new("@Mensaje", SqlDbType.VarChar, 500)
            {
                Direction = ParameterDirection.Output
            };
            cmd.Parameters.Add(id);
            cmd.Parameters.Add(mensajeParam);

            conexion.Open();
            cmd.ExecuteNonQuery();

            mensaje = mensajeParam.Value?.ToString() ?? string.Empty;
            return id.Value == DBNull.Value ? 0 : Convert.ToInt32(id.Value);
        }

        public string RegistrarProveedor(ProveedorStock proveedor)
        {
            return GuardarProveedor("USP_ALM_PROVEEDOR_REGISTRAR", proveedor);
        }

        public string EditarProveedor(ProveedorStock proveedor)
        {
            return GuardarProveedor("USP_ALM_PROVEEDOR_EDITAR", proveedor);
        }

        public string EliminarProveedor(int idProveedor)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_PROVEEDOR_ELIMINAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@IdProveedor", idProveedor);

            SqlParameter mensajeParam = new("@Mensaje", SqlDbType.VarChar, 500)
            {
                Direction = ParameterDirection.Output
            };
            cmd.Parameters.Add(mensajeParam);

            conexion.Open();
            cmd.ExecuteNonQuery();

            return mensajeParam.Value?.ToString() ?? string.Empty;
        }

        public List<AlmacenStock> ListarAlmacenes()
        {
            List<AlmacenStock> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_ALMACEN_LISTAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            conexion.Open();
            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new AlmacenStock
                {
                    IdAlmacen = Convert.ToInt32(dr["IdAlmacen"]),
                    NombreAlmacen = dr["NombreAlmacen"]?.ToString() ?? string.Empty,
                    Estado = Convert.ToBoolean(dr["Estado"])
                });
            }

            return lista;
        }

        public List<TipoDocumentoStock> ListarTiposDocumento()
        {
            List<TipoDocumentoStock> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_TIPO_DOCUMENTO_STOCK_LISTAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            conexion.Open();
            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new TipoDocumentoStock
                {
                    IdTipoDocumento = Convert.ToInt32(dr["IdTipoDocumento"]),
                    NombreTipoDocumento = dr["NombreTipoDocumento"]?.ToString() ?? string.Empty,
                    Estado = Convert.ToBoolean(dr["Estado"])
                });
            }

            return lista;
        }

        public List<InsumoStockBusqueda> BuscarInsumos(int idAlmacen, string texto)
        {
            List<InsumoStockBusqueda> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_INSUMO_STOCK_BUSCAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@IdAlmacen", idAlmacen);
            cmd.Parameters.AddWithValue("@Texto", texto.Trim());

            conexion.Open();
            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new InsumoStockBusqueda
                {
                    IdInsumo = Convert.ToInt32(dr["IdInsumo"]),
                    Codigo = dr["Codigo"]?.ToString() ?? string.Empty,
                    NombreInsumo = dr["NombreInsumo"]?.ToString() ?? string.Empty,
                    Descripcion = dr["Descripcion"]?.ToString() ?? string.Empty,
                    IdUnidadMedida = Convert.ToInt32(dr["IdUnidadMedida"]),
                    NombreUnidad = dr["NombreUnidad"]?.ToString() ?? string.Empty,
                    StockActual = Convert.ToDecimal(dr["StockActual"])
                });
            }

            return lista;
        }

        private string EjecutarAccionEstado(string procedimiento, int idIngresoManualStockInsumo, string usuario, string motivo)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new(procedimiento, conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@IdIngresoManualStockInsumo", idIngresoManualStockInsumo);
            cmd.Parameters.AddWithValue("@Usuario", usuario);

            if (procedimiento.EndsWith("_ANULAR", StringComparison.OrdinalIgnoreCase))
            {
                cmd.Parameters.AddWithValue("@Motivo", motivo.Trim());
            }

            SqlParameter mensaje = new("@Mensaje", SqlDbType.VarChar, 500)
            {
                Direction = ParameterDirection.Output
            };
            cmd.Parameters.Add(mensaje);

            conexion.Open();
            cmd.ExecuteNonQuery();

            return mensaje.Value?.ToString() ?? string.Empty;
        }

        private static string GuardarProveedor(string procedimiento, ProveedorStock proveedor)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new(procedimiento, conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            if (procedimiento.EndsWith("_EDITAR", StringComparison.OrdinalIgnoreCase))
            {
                cmd.Parameters.AddWithValue("@IdProveedor", proveedor.IdProveedor);
                cmd.Parameters.AddWithValue("@Estado", proveedor.Estado);
            }

            cmd.Parameters.AddWithValue("@TipoDocumento", proveedor.TipoDocumento);
            cmd.Parameters.AddWithValue("@NumeroDocumento", proveedor.NumeroDocumento);
            cmd.Parameters.AddWithValue("@NombreRazonSocial", proveedor.NombreRazonSocial);
            cmd.Parameters.AddWithValue("@Direccion", proveedor.Direccion);
            cmd.Parameters.AddWithValue("@Telefono", proveedor.Telefono);
            cmd.Parameters.AddWithValue("@Correo", proveedor.Correo);

            SqlParameter mensajeParam = new("@Mensaje", SqlDbType.VarChar, 500)
            {
                Direction = ParameterDirection.Output
            };
            cmd.Parameters.Add(mensajeParam);

            conexion.Open();
            cmd.ExecuteNonQuery();

            return mensajeParam.Value?.ToString() ?? string.Empty;
        }

        private static DataTable CrearTablaDetalles(IEnumerable<IngresoManualStockInsumoDetalle> detalles)
        {
            DataTable tabla = new();
            tabla.Columns.Add("IdInsumo", typeof(int));
            tabla.Columns.Add("CodigoInsumo", typeof(string));
            tabla.Columns.Add("IdUnidadMedida", typeof(int));
            tabla.Columns.Add("Cantidad", typeof(decimal));
            tabla.Columns.Add("PrecioUnitario", typeof(decimal));
            tabla.Columns.Add("Descuento", typeof(decimal));

            foreach (IngresoManualStockInsumoDetalle detalle in detalles)
            {
                tabla.Rows.Add(
                    detalle.IdInsumo,
                    detalle.CodigoInsumo,
                    detalle.IdUnidadMedida,
                    detalle.Cantidad,
                    detalle.PrecioUnitario,
                    detalle.Descuento);
            }

            return tabla;
        }

        private static IngresoManualStockInsumo MapearCabecera(SqlDataReader dr)
        {
            return new IngresoManualStockInsumo
            {
                IdIngresoManualStockInsumo = Convert.ToInt32(dr["IdIngresoManualStockInsumo"]),
                FechaEmision = Convert.ToDateTime(dr["FechaEmision"]),
                IdProveedor = Convert.ToInt32(dr["IdProveedor"]),
                NombreProveedor = dr["NombreProveedor"]?.ToString() ?? string.Empty,
                IdTipoDocumento = Convert.ToInt32(dr["IdTipoDocumento"]),
                NombreTipoDocumento = dr["NombreTipoDocumento"]?.ToString() ?? string.Empty,
                TipoNumeracion = dr["TipoNumeracion"]?.ToString() ?? string.Empty,
                Serie = dr["Serie"]?.ToString() ?? string.Empty,
                Numero = dr["Numero"]?.ToString() ?? string.Empty,
                NumeroDocumento = dr["NumeroDocumento"]?.ToString() ?? string.Empty,
                IdAlmacen = Convert.ToInt32(dr["IdAlmacen"]),
                NombreAlmacen = dr["NombreAlmacen"]?.ToString() ?? string.Empty,
                Observacion = dr["Observacion"]?.ToString() ?? string.Empty,
                Estado = dr["Estado"]?.ToString() ?? string.Empty,
                Subtotal = Convert.ToDecimal(dr["Subtotal"]),
                DescuentoTotal = Convert.ToDecimal(dr["DescuentoTotal"]),
                Total = Convert.ToDecimal(dr["Total"]),
                UsuarioCreador = dr["UsuarioCreador"]?.ToString() ?? string.Empty,
                FechaCreacion = dr["FechaCreacion"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(dr["FechaCreacion"]),
                UsuarioAbastecimiento = dr["UsuarioAbastecimiento"]?.ToString() ?? string.Empty,
                FechaAbastecimiento = dr["FechaAbastecimiento"] == DBNull.Value ? null : Convert.ToDateTime(dr["FechaAbastecimiento"]),
                UsuarioAnulacion = dr["UsuarioAnulacion"]?.ToString() ?? string.Empty,
                FechaAnulacion = dr["FechaAnulacion"] == DBNull.Value ? null : Convert.ToDateTime(dr["FechaAnulacion"]),
                MotivoAnulacion = dr["MotivoAnulacion"]?.ToString() ?? string.Empty
            };
        }

        private static IngresoManualStockInsumoDetalle MapearDetalle(SqlDataReader dr)
        {
            return new IngresoManualStockInsumoDetalle
            {
                IdIngresoManualStockInsumoDetalle = Convert.ToInt32(dr["IdIngresoManualStockInsumoDetalle"]),
                IdIngresoManualStockInsumo = Convert.ToInt32(dr["IdIngresoManualStockInsumo"]),
                IdInsumo = Convert.ToInt32(dr["IdInsumo"]),
                CodigoInsumo = dr["CodigoInsumo"]?.ToString() ?? string.Empty,
                NombreInsumo = dr["NombreInsumo"]?.ToString() ?? string.Empty,
                IdUnidadMedida = Convert.ToInt32(dr["IdUnidadMedida"]),
                NombreUnidad = dr["NombreUnidad"]?.ToString() ?? string.Empty,
                StockActual = Convert.ToDecimal(dr["StockActual"]),
                Cantidad = Convert.ToDecimal(dr["Cantidad"]),
                PrecioUnitario = Convert.ToDecimal(dr["PrecioUnitario"]),
                Descuento = Convert.ToDecimal(dr["Descuento"]),
                Importe = Convert.ToDecimal(dr["Importe"])
            };
        }
    }
}



