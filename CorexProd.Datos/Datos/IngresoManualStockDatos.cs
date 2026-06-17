using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class IngresoManualStockDatos
    {
        public List<IngresoManualStock> Listar(DateTime? fechaDesde, DateTime? fechaHasta, int? idProveedor, int? idAlmacen, string estado, string numeroDocumento)
        {
            List<IngresoManualStock> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_INGRESO_MANUAL_STOCK_LISTAR", conexion);
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

        public IngresoManualStock? Obtener(int idIngresoManualStock)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_INGRESO_MANUAL_STOCK_OBTENER", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@IdIngresoManualStock", idIngresoManualStock);

            conexion.Open();
            using SqlDataReader dr = cmd.ExecuteReader();

            IngresoManualStock? ingreso = null;

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

        public string Guardar(IngresoManualStock ingreso, string usuario)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_INGRESO_MANUAL_STOCK_GUARDAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@IdIngresoManualStock", ingreso.IdIngresoManualStock);
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
            detalles.TypeName = "dbo.IngresoManualStockDetalleType";

            SqlParameter mensaje = new("@Mensaje", SqlDbType.VarChar, 500)
            {
                Direction = ParameterDirection.Output
            };
            cmd.Parameters.Add(mensaje);

            conexion.Open();
            cmd.ExecuteNonQuery();

            return mensaje.Value?.ToString() ?? string.Empty;
        }

        public string Abastecer(int idIngresoManualStock, string usuario)
        {
            return EjecutarAccionEstado("USP_ALM_INGRESO_MANUAL_STOCK_ABASTECER", idIngresoManualStock, usuario, string.Empty);
        }

        public string Anular(int idIngresoManualStock, string usuario, string motivo)
        {
            return EjecutarAccionEstado("USP_ALM_INGRESO_MANUAL_STOCK_ANULAR", idIngresoManualStock, usuario, motivo);
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

        public List<ProductoStockBusqueda> BuscarProductos(int idAlmacen, string texto)
        {
            List<ProductoStockBusqueda> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_PRODUCTO_STOCK_BUSCAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@IdAlmacen", idAlmacen);
            cmd.Parameters.AddWithValue("@Texto", texto.Trim());

            conexion.Open();
            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new ProductoStockBusqueda
                {
                    IdProducto = Convert.ToInt32(dr["IdProducto"]),
                    Codigo = dr["Codigo"]?.ToString() ?? string.Empty,
                    NombreProducto = dr["NombreProducto"]?.ToString() ?? string.Empty,
                    Descripcion = dr["Descripcion"]?.ToString() ?? string.Empty,
                    IdUnidadMedida = Convert.ToInt32(dr["IdUnidadMedida"]),
                    NombreUnidad = dr["NombreUnidad"]?.ToString() ?? string.Empty,
                    StockActual = Convert.ToDecimal(dr["StockActual"])
                });
            }

            return lista;
        }

        private string EjecutarAccionEstado(string procedimiento, int idIngresoManualStock, string usuario, string motivo)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new(procedimiento, conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@IdIngresoManualStock", idIngresoManualStock);
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

        private static DataTable CrearTablaDetalles(IEnumerable<IngresoManualStockDetalle> detalles)
        {
            DataTable tabla = new();
            tabla.Columns.Add("IdProducto", typeof(int));
            tabla.Columns.Add("CodigoProducto", typeof(string));
            tabla.Columns.Add("IdUnidadMedida", typeof(int));
            tabla.Columns.Add("Cantidad", typeof(decimal));
            tabla.Columns.Add("PrecioUnitario", typeof(decimal));
            tabla.Columns.Add("Descuento", typeof(decimal));

            foreach (IngresoManualStockDetalle detalle in detalles)
            {
                tabla.Rows.Add(
                    detalle.IdProducto,
                    detalle.CodigoProducto,
                    detalle.IdUnidadMedida,
                    detalle.Cantidad,
                    detalle.PrecioUnitario,
                    detalle.Descuento);
            }

            return tabla;
        }

        private static IngresoManualStock MapearCabecera(SqlDataReader dr)
        {
            return new IngresoManualStock
            {
                IdIngresoManualStock = Convert.ToInt32(dr["IdIngresoManualStock"]),
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

        private static IngresoManualStockDetalle MapearDetalle(SqlDataReader dr)
        {
            return new IngresoManualStockDetalle
            {
                IdIngresoManualStockDetalle = Convert.ToInt32(dr["IdIngresoManualStockDetalle"]),
                IdIngresoManualStock = Convert.ToInt32(dr["IdIngresoManualStock"]),
                IdProducto = Convert.ToInt32(dr["IdProducto"]),
                CodigoProducto = dr["CodigoProducto"]?.ToString() ?? string.Empty,
                NombreProducto = dr["NombreProducto"]?.ToString() ?? string.Empty,
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
