using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class CuentaPorPagarDatos
    {
        public CuentaPorPagarResultado Guardar(CuentaPorPagar cuenta, string usuario)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_TES_CXP_GUARDAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            SqlParameter id = new("@IdCuentaPorPagar", SqlDbType.Int)
            {
                Direction = ParameterDirection.InputOutput,
                Value = cuenta.IdCuentaPorPagar
            };
            cmd.Parameters.Add(id);
            cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = cuenta.IdProveedor;
            cmd.Parameters.Add("@IdTipoObligacion", SqlDbType.Int).Value = cuenta.IdTipoObligacion;
            cmd.Parameters.Add("@FechaDocumento", SqlDbType.Date).Value = cuenta.FechaDocumento.Date;
            cmd.Parameters.Add("@Moneda", SqlDbType.VarChar, 10).Value = cuenta.Moneda;
            cmd.Parameters.Add("@ImporteTotal", SqlDbType.Decimal).Value = cuenta.ImporteTotal;
            cmd.Parameters.Add("@OrigenTipo", SqlDbType.VarChar, 60).Value = cuenta.OrigenTipo;
            cmd.Parameters.Add("@OrigenId", SqlDbType.Int).Value = cuenta.OrigenId.HasValue ? cuenta.OrigenId.Value : DBNull.Value;
            cmd.Parameters.Add("@Observacion", SqlDbType.VarChar, 1000).Value = cuenta.Observacion;
            cmd.Parameters.Add("@Usuario", SqlDbType.VarChar, 80).Value = usuario;

            SqlParameter documentos = cmd.Parameters.AddWithValue("@Documentos", CrearTablaDocumentos(cuenta.Documentos));
            documentos.SqlDbType = SqlDbType.Structured;
            documentos.TypeName = "dbo.TesCuentaPorPagarDocumentoType";

            SqlParameter cuotas = cmd.Parameters.AddWithValue("@Cuotas", CrearTablaCuotas(cuenta.Cuotas));
            cuotas.SqlDbType = SqlDbType.Structured;
            cuotas.TypeName = "dbo.TesCuentaPorPagarCuotaType";

            SqlParameter resultado = new("@Resultado", SqlDbType.Bit) { Direction = ParameterDirection.Output };
            SqlParameter mensaje = new("@Mensaje", SqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(resultado);
            cmd.Parameters.Add(mensaje);

            conexion.Open();
            cmd.ExecuteNonQuery();

            return new CuentaPorPagarResultado
            {
                IdCuentaPorPagar = id.Value == DBNull.Value ? 0 : Convert.ToInt32(id.Value),
                Resultado = resultado.Value != DBNull.Value && Convert.ToBoolean(resultado.Value),
                Mensaje = mensaje.Value?.ToString() ?? string.Empty
            };
        }

        public List<CuentaPorPagarListado> Listar(DateTime? fechaDesde, DateTime? fechaHasta, int? idProveedor, string? estado, string? texto)
        {
            List<CuentaPorPagarListado> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_TES_CXP_LISTAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@FechaDesde", SqlDbType.Date).Value = fechaDesde.HasValue ? fechaDesde.Value.Date : DBNull.Value;
            cmd.Parameters.Add("@FechaHasta", SqlDbType.Date).Value = fechaHasta.HasValue ? fechaHasta.Value.Date : DBNull.Value;
            cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = idProveedor.HasValue ? idProveedor.Value : DBNull.Value;
            cmd.Parameters.Add("@Estado", SqlDbType.VarChar, 30).Value = string.IsNullOrWhiteSpace(estado) || estado == "Todos" ? DBNull.Value : estado.Trim();
            cmd.Parameters.Add("@Texto", SqlDbType.VarChar, 120).Value = string.IsNullOrWhiteSpace(texto) ? DBNull.Value : texto.Trim();

            conexion.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
                lista.Add(MapearListado(dr));

            return lista;
        }

        public List<TipoObligacion> ListarTiposObligacion(bool soloActivos = true)
        {
            List<TipoObligacion> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("""
                SELECT IdTipoObligacion, Codigo, Nombre, Descripcion, Estado, FechaRegistro
                FROM dbo.TesTiposObligacion
                WHERE @SoloActivos = 0 OR Estado = 1
                ORDER BY Nombre;
                """, conexion);
            cmd.Parameters.Add("@SoloActivos", SqlDbType.Bit).Value = soloActivos;

            conexion.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new TipoObligacion
                {
                    IdTipoObligacion = Convert.ToInt32(dr["IdTipoObligacion"]),
                    Codigo = dr["Codigo"]?.ToString() ?? string.Empty,
                    Nombre = dr["Nombre"]?.ToString() ?? string.Empty,
                    Descripcion = dr["Descripcion"]?.ToString() ?? string.Empty,
                    Estado = Convert.ToBoolean(dr["Estado"]),
                    FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
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

        public CuentaPorPagar? Obtener(int idCuentaPorPagar)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_TES_CXP_OBTENER", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@IdCuentaPorPagar", SqlDbType.Int).Value = idCuentaPorPagar;

            conexion.Open();
            using SqlDataReader dr = cmd.ExecuteReader();

            CuentaPorPagar? cuenta = null;
            if (dr.Read())
                cuenta = MapearCuenta(dr);

            if (cuenta == null)
                return null;

            if (dr.NextResult())
            {
                while (dr.Read())
                    cuenta.Documentos.Add(MapearDocumento(dr));
            }

            if (dr.NextResult())
            {
                while (dr.Read())
                    cuenta.Cuotas.Add(MapearCuota(dr));
            }

            if (dr.NextResult())
            {
                while (dr.Read())
                {
                    // El SP expone pagos como cuarto resultset; esta etapa aun no implementa pagos.
                }
            }

            if (dr.NextResult())
            {
                while (dr.Read())
                    cuenta.Historial.Add(MapearHistorial(dr));
            }

            return cuenta;
        }

        public List<CuentaPorPagarProgramacion> ObtenerProgramacion(DateTime fechaDesde, DateTime fechaHasta, int? idProveedor, string? estado)
        {
            List<CuentaPorPagarProgramacion> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_TES_CXP_PROGRAMACION_RANGO", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@FechaDesde", SqlDbType.Date).Value = fechaDesde.Date;
            cmd.Parameters.Add("@FechaHasta", SqlDbType.Date).Value = fechaHasta.Date;
            cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = idProveedor.HasValue ? idProveedor.Value : DBNull.Value;
            cmd.Parameters.Add("@Estado", SqlDbType.VarChar, 30).Value = string.IsNullOrWhiteSpace(estado) || estado == "Todos" ? DBNull.Value : estado.Trim();

            conexion.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
                lista.Add(MapearProgramacion(dr));

            return lista;
        }

        public CuentaPorPagarResultado Anular(int idCuentaPorPagar, string usuario, string motivo)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_TES_CXP_ANULAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@IdCuentaPorPagar", SqlDbType.Int).Value = idCuentaPorPagar;
            cmd.Parameters.Add("@Usuario", SqlDbType.VarChar, 80).Value = usuario;
            cmd.Parameters.Add("@Motivo", SqlDbType.VarChar, 500).Value = motivo;

            SqlParameter resultado = new("@Resultado", SqlDbType.Bit) { Direction = ParameterDirection.Output };
            SqlParameter mensaje = new("@Mensaje", SqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(resultado);
            cmd.Parameters.Add(mensaje);

            conexion.Open();
            cmd.ExecuteNonQuery();

            return new CuentaPorPagarResultado
            {
                IdCuentaPorPagar = idCuentaPorPagar,
                Resultado = resultado.Value != DBNull.Value && Convert.ToBoolean(resultado.Value),
                Mensaje = mensaje.Value?.ToString() ?? string.Empty
            };
        }

        private static DataTable CrearTablaDocumentos(IEnumerable<CuentaPorPagarDocumento> documentos)
        {
            DataTable tabla = new();
            tabla.Columns.Add("IdTipoDocumento", typeof(int));
            tabla.Columns.Add("Serie", typeof(string));
            tabla.Columns.Add("Numero", typeof(string));
            tabla.Columns.Add("NumeroDocumento", typeof(string));
            tabla.Columns.Add("FechaDocumento", typeof(DateTime));
            tabla.Columns.Add("Importe", typeof(decimal));
            tabla.Columns.Add("Observacion", typeof(string));

            foreach (CuentaPorPagarDocumento documento in documentos)
            {
                tabla.Rows.Add(
                    documento.IdTipoDocumento,
                    string.IsNullOrWhiteSpace(documento.Serie) ? DBNull.Value : documento.Serie,
                    string.IsNullOrWhiteSpace(documento.Numero) ? DBNull.Value : documento.Numero,
                    documento.NumeroDocumento,
                    documento.FechaEmision.Date,
                    documento.Importe,
                    string.IsNullOrWhiteSpace(documento.Observacion) ? DBNull.Value : documento.Observacion);
            }

            return tabla;
        }

        private static DataTable CrearTablaCuotas(IEnumerable<CuentaPorPagarCuota> cuotas)
        {
            DataTable tabla = new();
            tabla.Columns.Add("NumeroCuota", typeof(int));
            tabla.Columns.Add("TotalCuotas", typeof(int));
            tabla.Columns.Add("NumeroLetra", typeof(string));
            tabla.Columns.Add("FechaGiro", typeof(DateTime));
            tabla.Columns.Add("FechaVencimiento", typeof(DateTime));
            tabla.Columns.Add("Importe", typeof(decimal));
            tabla.Columns.Add("Observacion", typeof(string));

            foreach (CuentaPorPagarCuota cuota in cuotas)
            {
                tabla.Rows.Add(
                    cuota.NumeroCuota,
                    cuota.TotalCuotas,
                    string.IsNullOrWhiteSpace(cuota.NumeroLetra) ? DBNull.Value : cuota.NumeroLetra,
                    cuota.FechaGiro.Date,
                    cuota.FechaVencimiento.Date,
                    cuota.Importe,
                    string.IsNullOrWhiteSpace(cuota.Observacion) ? DBNull.Value : cuota.Observacion);
            }

            return tabla;
        }

        private static CuentaPorPagar MapearCuenta(SqlDataReader dr) => new()
        {
            IdCuentaPorPagar = Convert.ToInt32(dr["IdCuentaPorPagar"]),
            IdProveedor = Convert.ToInt32(dr["IdProveedor"]),
            TipoDocumentoProveedor = dr["TipoDocumentoProveedor"]?.ToString() ?? string.Empty,
            NumeroDocumentoProveedor = dr["NumeroDocumentoProveedor"]?.ToString() ?? string.Empty,
            NombreProveedor = dr["NombreProveedor"]?.ToString() ?? string.Empty,
            IdTipoObligacion = Convert.ToInt32(dr["IdTipoObligacion"]),
            CodigoTipoObligacion = dr["CodigoTipoObligacion"]?.ToString() ?? string.Empty,
            TipoObligacion = dr["TipoObligacion"]?.ToString() ?? string.Empty,
            FechaDocumento = Convert.ToDateTime(dr["FechaDocumento"]),
            Moneda = dr["Moneda"]?.ToString() ?? string.Empty,
            ImporteTotal = Convert.ToDecimal(dr["ImporteTotal"]),
            TotalPagado = Convert.ToDecimal(dr["TotalPagado"]),
            SaldoPendiente = Convert.ToDecimal(dr["SaldoPendiente"]),
            Estado = dr["Estado"]?.ToString() ?? string.Empty,
            OrigenTipo = dr["OrigenTipo"]?.ToString() ?? string.Empty,
            OrigenId = dr["OrigenId"] == DBNull.Value ? null : Convert.ToInt32(dr["OrigenId"]),
            Observacion = dr["Observacion"]?.ToString() ?? string.Empty,
            UsuarioRegistro = dr["UsuarioRegistro"]?.ToString() ?? string.Empty,
            FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"]),
            UsuarioModificacion = dr["UsuarioModificacion"]?.ToString() ?? string.Empty,
            FechaModificacion = dr["FechaModificacion"] == DBNull.Value ? null : Convert.ToDateTime(dr["FechaModificacion"]),
            UsuarioAnulacion = dr["UsuarioAnulacion"]?.ToString() ?? string.Empty,
            FechaAnulacion = dr["FechaAnulacion"] == DBNull.Value ? null : Convert.ToDateTime(dr["FechaAnulacion"]),
            MotivoAnulacion = dr["MotivoAnulacion"]?.ToString() ?? string.Empty
        };

        private static CuentaPorPagarDocumento MapearDocumento(SqlDataReader dr) => new()
        {
            IdCuentaPorPagarDocumento = Convert.ToInt32(dr["IdCuentaPorPagarDocumento"]),
            IdCuentaPorPagar = Convert.ToInt32(dr["IdCuentaPorPagar"]),
            IdTipoDocumento = Convert.ToInt32(dr["IdTipoDocumento"]),
            NombreTipoDocumento = dr["NombreTipoDocumento"]?.ToString() ?? string.Empty,
            Serie = dr["Serie"]?.ToString() ?? string.Empty,
            Numero = dr["Numero"]?.ToString() ?? string.Empty,
            NumeroDocumento = dr["NumeroDocumento"]?.ToString() ?? string.Empty,
            FechaEmision = Convert.ToDateTime(dr["FechaDocumento"]),
            Importe = Convert.ToDecimal(dr["Importe"]),
            Observacion = dr["Observacion"]?.ToString() ?? string.Empty,
            Estado = dr["Estado"]?.ToString() ?? string.Empty
        };

        private static CuentaPorPagarCuota MapearCuota(SqlDataReader dr) => new()
        {
            IdCuota = Convert.ToInt32(dr["IdCuota"]),
            IdCuentaPorPagar = Convert.ToInt32(dr["IdCuentaPorPagar"]),
            NumeroCuota = Convert.ToInt32(dr["NumeroCuota"]),
            TotalCuotas = Convert.ToInt32(dr["TotalCuotas"]),
            NumeroLetra = dr["NumeroLetra"]?.ToString() ?? string.Empty,
            FechaGiro = Convert.ToDateTime(dr["FechaGiro"]),
            FechaVencimiento = Convert.ToDateTime(dr["FechaVencimiento"]),
            Importe = Convert.ToDecimal(dr["Importe"]),
            TotalPagado = Convert.ToDecimal(dr["TotalPagado"]),
            SaldoPendiente = Convert.ToDecimal(dr["SaldoPendiente"]),
            Estado = dr["Estado"]?.ToString() ?? string.Empty,
            Observacion = dr["Observacion"]?.ToString() ?? string.Empty
        };

        private static CuentaPorPagarHistorial MapearHistorial(SqlDataReader dr) => new()
        {
            IdCuentaPorPagarHistorial = Convert.ToInt64(dr["IdCuentaPorPagarHistorial"]),
            IdCuentaPorPagar = Convert.ToInt32(dr["IdCuentaPorPagar"]),
            IdCuota = dr["IdCuota"] == DBNull.Value ? null : Convert.ToInt32(dr["IdCuota"]),
            Usuario = dr["Usuario"]?.ToString() ?? string.Empty,
            Accion = dr["Accion"]?.ToString() ?? string.Empty,
            EstadoAnterior = dr["EstadoAnterior"]?.ToString() ?? string.Empty,
            EstadoNuevo = dr["EstadoNuevo"]?.ToString() ?? string.Empty,
            Descripcion = dr["Descripcion"]?.ToString() ?? string.Empty,
            FechaHora = Convert.ToDateTime(dr["FechaHora"])
        };

        private static CuentaPorPagarListado MapearListado(SqlDataReader dr) => new()
        {
            IdCuentaPorPagar = Convert.ToInt32(dr["IdCuentaPorPagar"]),
            IdProveedor = Convert.ToInt32(dr["IdProveedor"]),
            NombreProveedor = dr["NombreProveedor"]?.ToString() ?? string.Empty,
            NumeroDocumentoProveedor = dr["NumeroDocumentoProveedor"]?.ToString() ?? string.Empty,
            IdTipoObligacion = Convert.ToInt32(dr["IdTipoObligacion"]),
            TipoObligacion = dr["TipoObligacion"]?.ToString() ?? string.Empty,
            FechaDocumento = Convert.ToDateTime(dr["FechaDocumento"]),
            Moneda = dr["Moneda"]?.ToString() ?? string.Empty,
            ImporteTotal = Convert.ToDecimal(dr["ImporteTotal"]),
            TotalPagado = Convert.ToDecimal(dr["TotalPagado"]),
            SaldoPendiente = Convert.ToDecimal(dr["SaldoPendiente"]),
            Estado = dr["Estado"]?.ToString() ?? string.Empty,
            OrigenTipo = dr["OrigenTipo"]?.ToString() ?? string.Empty,
            OrigenId = dr["OrigenId"] == DBNull.Value ? null : Convert.ToInt32(dr["OrigenId"]),
            Observacion = dr["Observacion"]?.ToString() ?? string.Empty,
            ProximoVencimiento = dr["ProximoVencimiento"] == DBNull.Value ? null : Convert.ToDateTime(dr["ProximoVencimiento"]),
            UsuarioRegistro = dr["UsuarioRegistro"]?.ToString() ?? string.Empty,
            FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
        };

        private static CuentaPorPagarProgramacion MapearProgramacion(SqlDataReader dr) => new()
        {
            IdCuota = Convert.ToInt32(dr["IdCuota"]),
            IdCuentaPorPagar = Convert.ToInt32(dr["IdCuentaPorPagar"]),
            IdProveedor = Convert.ToInt32(dr["IdProveedor"]),
            NombreProveedor = dr["NombreProveedor"]?.ToString() ?? string.Empty,
            NumeroDocumentoProveedor = dr["NumeroDocumentoProveedor"]?.ToString() ?? string.Empty,
            IdTipoObligacion = Convert.ToInt32(dr["IdTipoObligacion"]),
            TipoObligacion = dr["TipoObligacion"]?.ToString() ?? string.Empty,
            Moneda = dr["Moneda"]?.ToString() ?? string.Empty,
            FechaDocumento = Convert.ToDateTime(dr["FechaDocumento"]),
            NumeroCuota = Convert.ToInt32(dr["NumeroCuota"]),
            TotalCuotas = Convert.ToInt32(dr["TotalCuotas"]),
            NumeroLetra = dr["NumeroLetra"]?.ToString() ?? string.Empty,
            FechaGiro = Convert.ToDateTime(dr["FechaGiro"]),
            FechaVencimiento = Convert.ToDateTime(dr["FechaVencimiento"]),
            Importe = Convert.ToDecimal(dr["Importe"]),
            TotalPagado = Convert.ToDecimal(dr["TotalPagado"]),
            SaldoPendiente = Convert.ToDecimal(dr["SaldoPendiente"]),
            Estado = dr["Estado"]?.ToString() ?? string.Empty,
            OrigenTipo = dr["OrigenTipo"]?.ToString() ?? string.Empty,
            OrigenId = dr["OrigenId"] == DBNull.Value ? null : Convert.ToInt32(dr["OrigenId"]),
            Observacion = dr["Observacion"]?.ToString() ?? string.Empty
        };
    }
}
