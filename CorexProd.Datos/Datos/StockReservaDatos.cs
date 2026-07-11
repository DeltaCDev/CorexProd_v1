using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class StockReservaDatos
    {
        private readonly string? _connectionString;

        public StockReservaDatos()
        {
        }

        public StockReservaDatos(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<StockDisponibilidad> ListarDisponibilidad(int? idProducto = null, int? idAlmacen = null, string buscar = "")
        {
            List<StockDisponibilidad> lista = [];
            using SqlConnection conexion = ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_STOCK_RESERVA_DISPONIBILIDAD", conexion) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add("@IdProducto", SqlDbType.Int).Value = (object?)idProducto ?? DBNull.Value;
            cmd.Parameters.Add("@IdAlmacen", SqlDbType.Int).Value = (object?)idAlmacen ?? DBNull.Value;
            cmd.Parameters.Add("@Buscar", SqlDbType.VarChar, 150).Value = buscar?.Trim() ?? string.Empty;
            conexion.Open();

            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new StockDisponibilidad
                {
                    IdProducto = Convert.ToInt32(dr["IdProducto"]),
                    Codigo = Texto(dr, "Codigo"),
                    NombreProducto = Texto(dr, "NombreProducto"),
                    EtiquetaCliente = Texto(dr, "EtiquetaCliente"),
                    IdAlmacen = Convert.ToInt32(dr["IdAlmacen"]),
                    NombreAlmacen = Texto(dr, "NombreAlmacen"),
                    StockFisico = Decimal(dr, "StockFisico"),
                    StockReservado = Decimal(dr, "StockReservado"),
                    StockDisponible = Decimal(dr, "StockDisponible")
                });
            }

            return lista;
        }

        public List<StockReserva> Listar(int? idOrdenCompraInterna = null, int? idProducto = null, bool soloActivas = false)
        {
            List<StockReserva> lista = [];
            using SqlConnection conexion = ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_STOCK_RESERVA_LISTAR", conexion) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add("@IdOrdenCompraInterna", SqlDbType.Int).Value = (object?)idOrdenCompraInterna ?? DBNull.Value;
            cmd.Parameters.Add("@IdProducto", SqlDbType.Int).Value = (object?)idProducto ?? DBNull.Value;
            cmd.Parameters.Add("@SoloActivas", SqlDbType.Bit).Value = soloActivas;
            conexion.Open();

            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
                lista.Add(MapearReserva(dr));

            return lista;
        }

        public List<StockReservaMovimiento> ListarMovimientos(long idStockReserva)
        {
            List<StockReservaMovimiento> lista = [];
            using SqlConnection conexion = ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_STOCK_RESERVA_MOVIMIENTOS_LISTAR", conexion) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add("@IdStockReserva", SqlDbType.BigInt).Value = idStockReserva;
            conexion.Open();

            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new StockReservaMovimiento
                {
                    IdStockReservaMovimiento = Convert.ToInt64(dr["IdStockReservaMovimiento"]),
                    IdStockReserva = Convert.ToInt64(dr["IdStockReserva"]),
                    TipoMovimiento = Texto(dr, "TipoMovimiento"),
                    Cantidad = Decimal(dr, "Cantidad"),
                    EstadoAnterior = Texto(dr, "EstadoAnterior"),
                    EstadoNuevo = Texto(dr, "EstadoNuevo"),
                    DocumentoReferencia = Texto(dr, "DocumentoReferencia"),
                    UsuarioMovimiento = Texto(dr, "UsuarioMovimiento"),
                    FechaMovimiento = Convert.ToDateTime(dr["FechaMovimiento"]),
                    Observacion = Texto(dr, "Observacion")
                });
            }

            return lista;
        }

        public List<StockReservaHistorico> ListarHistorico(
            int? idProducto = null,
            int? idAlmacen = null,
            int? idOrdenCompraInterna = null,
            int? idOrdenTrabajo = null,
            string tipoMovimiento = "",
            string documentoReferencia = "",
            DateTime? desde = null,
            DateTime? hasta = null,
            string buscar = "",
            int top = 300)
        {
            List<StockReservaHistorico> lista = [];
            using SqlConnection conexion = ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_STOCK_RESERVA_HISTORICO_LISTAR", conexion) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add("@IdProducto", SqlDbType.Int).Value = (object?)idProducto ?? DBNull.Value;
            cmd.Parameters.Add("@IdAlmacen", SqlDbType.Int).Value = (object?)idAlmacen ?? DBNull.Value;
            cmd.Parameters.Add("@IdOrdenCompraInterna", SqlDbType.Int).Value = (object?)idOrdenCompraInterna ?? DBNull.Value;
            cmd.Parameters.Add("@IdOrdenTrabajo", SqlDbType.Int).Value = (object?)idOrdenTrabajo ?? DBNull.Value;
            cmd.Parameters.Add("@TipoMovimiento", SqlDbType.VarChar, 30).Value = tipoMovimiento?.Trim() ?? string.Empty;
            cmd.Parameters.Add("@DocumentoReferencia", SqlDbType.VarChar, 100).Value = documentoReferencia?.Trim() ?? string.Empty;
            cmd.Parameters.Add("@Desde", SqlDbType.DateTime2).Value = (object?)desde ?? DBNull.Value;
            cmd.Parameters.Add("@Hasta", SqlDbType.DateTime2).Value = (object?)hasta ?? DBNull.Value;
            cmd.Parameters.Add("@Buscar", SqlDbType.VarChar, 150).Value = buscar?.Trim() ?? string.Empty;
            cmd.Parameters.Add("@Top", SqlDbType.Int).Value = top;
            conexion.Open();

            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
                lista.Add(MapearHistorico(dr));

            return lista;
        }

        public long Crear(StockReservaCrearRequest request)
        {
            using SqlConnection conexion = ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_STOCK_RESERVA_CREAR", conexion) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add("@IdOrdenCompraInterna", SqlDbType.Int).Value = request.IdOrdenCompraInterna;
            cmd.Parameters.Add("@IdOrdenCompraInternaDetalle", SqlDbType.Int).Value = request.IdOrdenCompraInternaDetalle;
            cmd.Parameters.Add("@IdProducto", SqlDbType.Int).Value = request.IdProducto;
            cmd.Parameters.Add("@IdAlmacen", SqlDbType.Int).Value = (object?)request.IdAlmacen ?? DBNull.Value;
            cmd.Parameters.Add("@IdOrdenTrabajo", SqlDbType.Int).Value = (object?)request.IdOrdenTrabajo ?? DBNull.Value;
            cmd.Parameters.Add("@IdDetalleOT", SqlDbType.Int).Value = (object?)request.IdDetalleOT ?? DBNull.Value;
            cmd.Parameters.Add("@Cantidad", SqlDbType.Decimal).Value = request.Cantidad;
            cmd.Parameters.Add("@TipoOrigen", SqlDbType.VarChar, 30).Value = request.TipoOrigen ?? "STOCK_FISICO";
            cmd.Parameters.Add("@Usuario", SqlDbType.VarChar, 100).Value = request.Usuario ?? string.Empty;
            cmd.Parameters.Add("@Observacion", SqlDbType.VarChar, 500).Value = string.IsNullOrWhiteSpace(request.Observacion) ? DBNull.Value : request.Observacion.Trim();
            SqlParameter id = new("@IdStockReserva", SqlDbType.BigInt) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(id);
            conexion.Open();
            cmd.ExecuteNonQuery();
            return Convert.ToInt64(id.Value);
        }

        public void Consumir(int idOrdenCompraInterna, int idOrdenCompraInternaDetalle, decimal cantidad, string usuario, string documentoReferencia = "", string observacion = "")
        {
            using SqlConnection conexion = ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_STOCK_RESERVA_CONSUMIR", conexion) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add("@IdOrdenCompraInterna", SqlDbType.Int).Value = idOrdenCompraInterna;
            cmd.Parameters.Add("@IdOrdenCompraInternaDetalle", SqlDbType.Int).Value = idOrdenCompraInternaDetalle;
            cmd.Parameters.Add("@Cantidad", SqlDbType.Decimal).Value = cantidad;
            cmd.Parameters.Add("@Usuario", SqlDbType.VarChar, 100).Value = usuario ?? string.Empty;
            cmd.Parameters.Add("@DocumentoReferencia", SqlDbType.VarChar, 100).Value = string.IsNullOrWhiteSpace(documentoReferencia) ? DBNull.Value : documentoReferencia.Trim();
            cmd.Parameters.Add("@Observacion", SqlDbType.VarChar, 500).Value = string.IsNullOrWhiteSpace(observacion) ? DBNull.Value : observacion.Trim();
            conexion.Open();
            cmd.ExecuteNonQuery();
        }

        public void Liberar(long idStockReserva, decimal? cantidad, string usuario, string documentoReferencia = "", string observacion = "")
        {
            using SqlConnection conexion = ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_STOCK_RESERVA_LIBERAR", conexion) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add("@IdStockReserva", SqlDbType.BigInt).Value = idStockReserva;
            cmd.Parameters.Add("@Cantidad", SqlDbType.Decimal).Value = (object?)cantidad ?? DBNull.Value;
            cmd.Parameters.Add("@Usuario", SqlDbType.VarChar, 100).Value = usuario ?? string.Empty;
            cmd.Parameters.Add("@DocumentoReferencia", SqlDbType.VarChar, 100).Value = string.IsNullOrWhiteSpace(documentoReferencia) ? DBNull.Value : documentoReferencia.Trim();
            cmd.Parameters.Add("@Observacion", SqlDbType.VarChar, 500).Value = string.IsNullOrWhiteSpace(observacion) ? DBNull.Value : observacion.Trim();
            conexion.Open();
            cmd.ExecuteNonQuery();
        }

        private static StockReserva MapearReserva(SqlDataReader dr) => new()
        {
            IdStockReserva = Convert.ToInt64(dr["IdStockReserva"]),
            IdOrdenCompraInterna = Convert.ToInt32(dr["IdOrdenCompraInterna"]),
            NumeroOci = Texto(dr, "NumeroOci"),
            IdOrdenCompraInternaDetalle = Convert.ToInt32(dr["IdOrdenCompraInternaDetalle"]),
            IdProducto = Convert.ToInt32(dr["IdProducto"]),
            IdAlmacen = dr["IdAlmacen"] is DBNull ? null : Convert.ToInt32(dr["IdAlmacen"]),
            NombreAlmacen = Texto(dr, "NombreAlmacen"),
            IdOrdenTrabajo = dr["IdOrdenTrabajo"] is DBNull ? null : Convert.ToInt32(dr["IdOrdenTrabajo"]),
            NumeroOT = Texto(dr, "NumeroOT"),
            IdDetalleOT = dr["IdDetalleOT"] is DBNull ? null : Convert.ToInt32(dr["IdDetalleOT"]),
            CodigoProducto = Texto(dr, "CodigoProducto"),
            NombreProducto = Texto(dr, "NombreProducto"),
            CantidadReservada = Decimal(dr, "CantidadReservada"),
            CantidadConsumida = Decimal(dr, "CantidadConsumida"),
            CantidadLiberada = Decimal(dr, "CantidadLiberada"),
            CantidadPendiente = Decimal(dr, "CantidadPendiente"),
            TipoOrigen = Texto(dr, "TipoOrigen"),
            Estado = Texto(dr, "Estado"),
            FechaReserva = Convert.ToDateTime(dr["FechaReserva"]),
            UsuarioReserva = Texto(dr, "UsuarioReserva"),
            FechaActualizacion = Convert.ToDateTime(dr["FechaActualizacion"]),
            UsuarioActualizacion = Texto(dr, "UsuarioActualizacion"),
            Observacion = Texto(dr, "Observacion")
        };

        private static StockReservaHistorico MapearHistorico(SqlDataReader dr) => new()
        {
            IdStockReservaMovimiento = Convert.ToInt64(dr["IdStockReservaMovimiento"]),
            IdStockReserva = Convert.ToInt64(dr["IdStockReserva"]),
            IdOrdenCompraInterna = Convert.ToInt32(dr["IdOrdenCompraInterna"]),
            NumeroOci = Texto(dr, "NumeroOci"),
            OrdenCompraCliente = Texto(dr, "OrdenCompraCliente"),
            NombreCliente = Texto(dr, "NombreCliente"),
            IdOrdenCompraInternaDetalle = Convert.ToInt32(dr["IdOrdenCompraInternaDetalle"]),
            IdProducto = Convert.ToInt32(dr["IdProducto"]),
            CodigoProducto = Texto(dr, "CodigoProducto"),
            NombreProducto = Texto(dr, "NombreProducto"),
            EtiquetaCliente = Texto(dr, "EtiquetaCliente"),
            IdAlmacen = dr["IdAlmacen"] is DBNull ? null : Convert.ToInt32(dr["IdAlmacen"]),
            NombreAlmacen = Texto(dr, "NombreAlmacen"),
            IdOrdenTrabajo = dr["IdOrdenTrabajo"] is DBNull ? null : Convert.ToInt32(dr["IdOrdenTrabajo"]),
            NumeroOT = Texto(dr, "NumeroOT"),
            IdDetalleOT = dr["IdDetalleOT"] is DBNull ? null : Convert.ToInt32(dr["IdDetalleOT"]),
            TipoOrigen = Texto(dr, "TipoOrigen"),
            EstadoReserva = Texto(dr, "EstadoReserva"),
            CantidadReservada = Decimal(dr, "CantidadReservada"),
            CantidadConsumida = Decimal(dr, "CantidadConsumida"),
            CantidadLiberada = Decimal(dr, "CantidadLiberada"),
            CantidadPendiente = Decimal(dr, "CantidadPendiente"),
            TipoMovimiento = Texto(dr, "TipoMovimiento"),
            CantidadMovimiento = Decimal(dr, "CantidadMovimiento"),
            EstadoAnterior = Texto(dr, "EstadoAnterior"),
            EstadoNuevo = Texto(dr, "EstadoNuevo"),
            DocumentoReferencia = Texto(dr, "DocumentoReferencia"),
            UsuarioMovimiento = Texto(dr, "UsuarioMovimiento"),
            FechaMovimiento = Convert.ToDateTime(dr["FechaMovimiento"]),
            ObservacionMovimiento = Texto(dr, "ObservacionMovimiento"),
            ObservacionReserva = Texto(dr, "ObservacionReserva")
        };

        private static string Texto(SqlDataReader dr, string columna) =>
            dr[columna] is DBNull ? string.Empty : dr[columna]?.ToString() ?? string.Empty;

        private static decimal Decimal(SqlDataReader dr, string columna) =>
            dr[columna] is DBNull ? 0 : Convert.ToDecimal(dr[columna]);

        private SqlConnection ObtenerConexion() =>
            string.IsNullOrWhiteSpace(_connectionString)
                ? Conexion.ObtenerConexion()
                : new SqlConnection(_connectionString);
    }
}
