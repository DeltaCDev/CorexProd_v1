using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class TarjetaCreditoDatos
    {
        private readonly ObligacionFinancieraDatos _obligacionDatos = new();
        private readonly EmpresaDatos _empresaDatos = new();

        public TarjetaCreditoResultado GuardarTarjeta(
            TarjetaCredito tarjeta,
            string usuario,
            IEnumerable<string>? monedas = null)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_TES_TARJETA_GUARDAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            SqlParameter id = new("@IdTarjetaCredito", SqlDbType.Int)
            {
                Direction = ParameterDirection.InputOutput,
                Value = tarjeta.IdTarjetaCredito
            };
            cmd.Parameters.Add(id);
            cmd.Parameters.Add("@IdEntidadFinanciera", SqlDbType.Int).Value = tarjeta.IdEntidadFinanciera;
            cmd.Parameters.Add("@Titular", SqlDbType.VarChar, 180).Value = tarjeta.Titular ?? string.Empty;
            cmd.Parameters.Add("@IdEmpresaResponsable", SqlDbType.Int).Value = tarjeta.IdEmpresaResponsable.HasValue ? tarjeta.IdEmpresaResponsable.Value : DBNull.Value;
            cmd.Parameters.Add("@EmpresaResponsable", SqlDbType.VarChar, 180).Value = tarjeta.EmpresaResponsable ?? string.Empty;
            cmd.Parameters.Add("@Alias", SqlDbType.VarChar, 120).Value = tarjeta.Alias ?? string.Empty;
            cmd.Parameters.Add("@Ultimos4", SqlDbType.VarChar, 4).Value = string.IsNullOrWhiteSpace(tarjeta.Ultimos4) ? DBNull.Value : tarjeta.Ultimos4;
            cmd.Parameters.Add(CrearParametroMonedas(monedas, tarjeta.Moneda));
            cmd.Parameters.Add("@DiaCorte", SqlDbType.TinyInt).Value = tarjeta.DiaCorte.HasValue ? tarjeta.DiaCorte.Value : DBNull.Value;
            cmd.Parameters.Add("@DiaPago", SqlDbType.TinyInt).Value = tarjeta.DiaPago.HasValue ? tarjeta.DiaPago.Value : DBNull.Value;
            cmd.Parameters.Add("@Estado", SqlDbType.VarChar, 20).Value = tarjeta.Estado ?? "ACTIVA";
            cmd.Parameters.Add("@Observacion", SqlDbType.VarChar, 1000).Value = tarjeta.Observacion ?? string.Empty;
            cmd.Parameters.Add("@Usuario", SqlDbType.VarChar, 80).Value = usuario;

            AgregarParametrosResultado(cmd, out SqlParameter resultado, out SqlParameter mensaje);

            conexion.Open();
            cmd.ExecuteNonQuery();

            return new TarjetaCreditoResultado
            {
                IdTarjetaCredito = id.Value == DBNull.Value ? 0 : Convert.ToInt32(id.Value),
                Resultado = resultado.Value != DBNull.Value && Convert.ToBoolean(resultado.Value),
                Mensaje = mensaje.Value?.ToString() ?? string.Empty
            };
        }

        public List<TarjetaCredito> ListarTarjetas(string? texto = null, string? estado = null, int? idEntidadFinanciera = null, string? moneda = null)
        {
            List<TarjetaCredito> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_TES_TARJETA_LISTAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@Texto", SqlDbType.VarChar, 160).Value = string.IsNullOrWhiteSpace(texto) ? DBNull.Value : texto.Trim();
            cmd.Parameters.Add("@Estado", SqlDbType.VarChar, 20).Value = string.IsNullOrWhiteSpace(estado) || estado == "Todos" ? DBNull.Value : estado.Trim();
            cmd.Parameters.Add("@IdEntidadFinanciera", SqlDbType.Int).Value = idEntidadFinanciera.HasValue ? idEntidadFinanciera.Value : DBNull.Value;
            cmd.Parameters.Add("@Moneda", SqlDbType.VarChar, 10).Value = string.IsNullOrWhiteSpace(moneda) || moneda == "Todos" ? DBNull.Value : moneda.Trim();

            conexion.Open();
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                    lista.Add(MapearTarjeta(dr));
            }

            CargarMonedasHabilitadas(conexion, lista);

            return lista;
        }

        public TarjetaCredito? ObtenerTarjeta(int idTarjetaCredito)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_TES_TARJETA_OBTENER", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@IdTarjetaCredito", SqlDbType.Int).Value = idTarjetaCredito;

            conexion.Open();
            TarjetaCredito? tarjeta;
            using (SqlDataReader dr = cmd.ExecuteReader())
                tarjeta = dr.Read() ? MapearTarjeta(dr) : null;

            if (tarjeta != null)
                CargarMonedasHabilitadas(conexion, [tarjeta]);

            return tarjeta;
        }

        public TarjetaCreditoResultado AnularTarjeta(int idTarjetaCredito, string usuario, string motivo)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_TES_TARJETA_ANULAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@IdTarjetaCredito", SqlDbType.Int).Value = idTarjetaCredito;
            cmd.Parameters.Add("@Usuario", SqlDbType.VarChar, 80).Value = usuario;
            cmd.Parameters.Add("@Motivo", SqlDbType.VarChar, 500).Value = motivo;

            AgregarParametrosResultado(cmd, out SqlParameter resultado, out SqlParameter mensaje);

            conexion.Open();
            cmd.ExecuteNonQuery();

            return new TarjetaCreditoResultado
            {
                IdTarjetaCredito = idTarjetaCredito,
                Resultado = resultado.Value != DBNull.Value && Convert.ToBoolean(resultado.Value),
                Mensaje = mensaje.Value?.ToString() ?? string.Empty
            };
        }

        public List<TarjetaCreditoPeriodo> ListarPeriodos(int? idTarjetaCredito = null, short? anio = null, string? estado = null, DateTime? fechaDesde = null, DateTime? fechaHasta = null)
        {
            List<TarjetaCreditoPeriodo> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_TES_TARJETA_PERIODO_LISTAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@IdTarjetaCredito", SqlDbType.Int).Value = idTarjetaCredito.HasValue ? idTarjetaCredito.Value : DBNull.Value;
            cmd.Parameters.Add("@Anio", SqlDbType.SmallInt).Value = anio.HasValue ? anio.Value : DBNull.Value;
            cmd.Parameters.Add("@Estado", SqlDbType.VarChar, 30).Value = string.IsNullOrWhiteSpace(estado) || estado == "Todos" ? DBNull.Value : estado.Trim();
            cmd.Parameters.Add("@FechaDesde", SqlDbType.Date).Value = fechaDesde.HasValue ? fechaDesde.Value.Date : DBNull.Value;
            cmd.Parameters.Add("@FechaHasta", SqlDbType.Date).Value = fechaHasta.HasValue ? fechaHasta.Value.Date : DBNull.Value;

            conexion.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
                lista.Add(MapearPeriodo(dr));

            return lista;
        }

        public TarjetaCreditoPeriodo? ObtenerPeriodo(int idTarjetaPeriodo)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_TES_TARJETA_PERIODO_OBTENER", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@IdTarjetaPeriodo", SqlDbType.Int).Value = idTarjetaPeriodo;

            conexion.Open();
            using SqlDataReader dr = cmd.ExecuteReader();

            TarjetaCreditoPeriodo? periodo = null;
            if (dr.Read())
                periodo = MapearPeriodo(dr);

            if (periodo == null)
                return null;

            if (dr.NextResult())
            {
                while (dr.Read())
                    periodo.Pagos.Add(MapearPago(dr));
            }

            return periodo;
        }

        public TarjetaCreditoPeriodoResultado GuardarPeriodo(TarjetaCreditoPeriodo periodo, string usuario)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_TES_TARJETA_PERIODO_GUARDAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            SqlParameter id = new("@IdTarjetaPeriodo", SqlDbType.Int)
            {
                Direction = ParameterDirection.InputOutput,
                Value = periodo.IdTarjetaPeriodo
            };
            cmd.Parameters.Add(id);
            cmd.Parameters.Add("@IdTarjetaCredito", SqlDbType.Int).Value = periodo.IdTarjetaCredito;
            cmd.Parameters.Add("@PeriodoAnio", SqlDbType.SmallInt).Value = periodo.PeriodoAnio;
            cmd.Parameters.Add("@PeriodoMes", SqlDbType.TinyInt).Value = periodo.PeriodoMes;
            cmd.Parameters.Add("@FechaCorte", SqlDbType.Date).Value = periodo.FechaCorte.Date;
            cmd.Parameters.Add("@FechaVencimiento", SqlDbType.Date).Value = periodo.FechaVencimiento.Date;
            AddDecimal(cmd, "@ImportePEN", periodo.ImportePEN);
            AddDecimal(cmd, "@ImporteUSD", periodo.ImporteUSD);
            AddDecimal(cmd, "@Monto", periodo.Monto);
            cmd.Parameters.Add("@Estado", SqlDbType.VarChar, 30).Value = string.IsNullOrWhiteSpace(periodo.Estado) ? DBNull.Value : periodo.Estado;
            cmd.Parameters.Add("@Observacion", SqlDbType.VarChar, 1000).Value = periodo.Observacion ?? string.Empty;
            cmd.Parameters.Add("@Usuario", SqlDbType.VarChar, 80).Value = usuario;

            AgregarParametrosPeriodoResultado(
                cmd,
                out SqlParameter resultado,
                out SqlParameter mensaje,
                out SqlParameter totalPagado,
                out SqlParameter saldoPendiente,
                out SqlParameter estadoPeriodo);

            conexion.Open();
            cmd.ExecuteNonQuery();

            return MapearResultadoPeriodo(id, resultado, mensaje, totalPagado, saldoPendiente, estadoPeriodo);
        }

        public TarjetaCreditoPeriodoResultado AnularPeriodo(int idTarjetaPeriodo, string usuario, string motivo)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_TES_TARJETA_PERIODO_ANULAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@IdTarjetaPeriodo", SqlDbType.Int).Value = idTarjetaPeriodo;
            cmd.Parameters.Add("@Usuario", SqlDbType.VarChar, 80).Value = usuario;
            cmd.Parameters.Add("@Motivo", SqlDbType.VarChar, 500).Value = motivo;

            AgregarParametrosResultado(cmd, out SqlParameter resultado, out SqlParameter mensaje);

            conexion.Open();
            cmd.ExecuteNonQuery();

            return new TarjetaCreditoPeriodoResultado
            {
                IdTarjetaPeriodo = idTarjetaPeriodo,
                Resultado = resultado.Value != DBNull.Value && Convert.ToBoolean(resultado.Value),
                Mensaje = mensaje.Value?.ToString() ?? string.Empty
            };
        }

        public TarjetaCreditoPagoResultado RegistrarPago(TarjetaCreditoPago pago, string usuario)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_TES_TARJETA_REGISTRAR_PAGO", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            SqlParameter idPago = new("@IdTarjetaPago", SqlDbType.Int)
            {
                Direction = ParameterDirection.InputOutput,
                Value = pago.IdPago
            };
            cmd.Parameters.Add(idPago);
            cmd.Parameters.Add("@IdTarjetaPeriodo", SqlDbType.Int).Value = pago.IdTarjetaPeriodo;
            cmd.Parameters.Add("@FechaPago", SqlDbType.Date).Value = pago.FechaPago.Date;
            cmd.Parameters.Add("@IdBanco", SqlDbType.Int).Value = pago.IdBanco.HasValue ? pago.IdBanco.Value : DBNull.Value;
            cmd.Parameters.Add("@IdCuentaBancaria", SqlDbType.Int).Value = pago.IdCuentaBancaria.HasValue ? pago.IdCuentaBancaria.Value : DBNull.Value;
            AddDecimal(cmd, "@Importe", pago.Importe);
            cmd.Parameters.Add("@Moneda", SqlDbType.VarChar, 10).Value = pago.Moneda ?? (object)DBNull.Value;
            cmd.Parameters.Add("@NumeroOperacion", SqlDbType.VarChar, 80).Value = pago.NumeroOperacion ?? string.Empty;
            cmd.Parameters.Add("@Observacion", SqlDbType.VarChar, 500).Value = pago.Observacion ?? string.Empty;
            cmd.Parameters.Add("@Usuario", SqlDbType.VarChar, 80).Value = usuario;

            AgregarParametrosPagoResultado(
                cmd,
                out SqlParameter resultado,
                out SqlParameter mensaje,
                out SqlParameter totalPagado,
                out SqlParameter saldoPendiente,
                out SqlParameter estadoPeriodo);

            conexion.Open();
            cmd.ExecuteNonQuery();

            return MapearResultadoPago(idPago, resultado, mensaje, totalPagado, saldoPendiente, estadoPeriodo);
        }

        public TarjetaCreditoPagoResultado AnularPago(int idPago, string usuario, string motivo)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_TES_TARJETA_ANULAR_PAGO", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@IdTarjetaPago", SqlDbType.Int).Value = idPago;
            cmd.Parameters.Add("@Usuario", SqlDbType.VarChar, 80).Value = usuario;
            cmd.Parameters.Add("@Motivo", SqlDbType.VarChar, 500).Value = motivo;

            AgregarParametrosPagoResultado(
                cmd,
                out SqlParameter resultado,
                out SqlParameter mensaje,
                out SqlParameter totalPagado,
                out SqlParameter saldoPendiente,
                out SqlParameter estadoPeriodo);

            conexion.Open();
            cmd.ExecuteNonQuery();

            return MapearResultadoPago(idPago, resultado, mensaje, totalPagado, saldoPendiente, estadoPeriodo);
        }

        public List<TarjetaCreditoHistorial> ListarHistorial(int idTarjetaCredito)
        {
            List<TarjetaCreditoHistorial> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("""
                SELECT
                    IdHistorialTarjetaCredito,
                    IdTarjetaCredito,
                    IdTarjetaPeriodo,
                    IdTarjetaPago,
                    Usuario,
                    Accion,
                    EstadoAnterior,
                    EstadoNuevo,
                    Descripcion,
                    FechaHora
                FROM dbo.TesTarjetaCreditoHistorial
                WHERE IdTarjetaCredito = @IdTarjetaCredito
                ORDER BY FechaHora DESC, IdHistorialTarjetaCredito DESC;
                """, conexion);
            cmd.Parameters.Add("@IdTarjetaCredito", SqlDbType.Int).Value = idTarjetaCredito;

            conexion.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
                lista.Add(MapearHistorial(dr));

            return lista;
        }

        public List<EntidadFinanciera> ListarEntidadesFinancieras(string? tipoEntidad = null)
        {
            return _obligacionDatos.ListarEntidadesFinancieras(tipoEntidad);
        }

        public List<BancoTesoreria> ListarBancos(bool soloActivos = true)
        {
            return _obligacionDatos.ListarBancos(soloActivos);
        }

        public List<CuentaBancariaTesoreria> ListarCuentasBancarias(int? idBanco = null, bool soloActivas = true)
        {
            return _obligacionDatos.ListarCuentasBancarias(idBanco, soloActivas);
        }

        public List<Empresa> ListarEmpresas()
        {
            return _empresaDatos.Listar();
        }

        private static TarjetaCredito MapearTarjeta(SqlDataReader dr) => new()
        {
            IdTarjetaCredito = Convert.ToInt32(dr["IdTarjetaCredito"]),
            IdEntidadFinanciera = Convert.ToInt32(dr["IdEntidadFinanciera"]),
            EntidadFinanciera = GetString(dr, "EntidadFinanciera"),
            Titular = GetString(dr, "Titular"),
            IdEmpresaResponsable = GetNullableInt(dr, "IdEmpresaResponsable"),
            EmpresaResponsable = GetString(dr, "EmpresaResponsable"),
            Alias = GetString(dr, "Alias"),
            Ultimos4 = GetString(dr, "Ultimos4"),
            Moneda = GetString(dr, "Moneda"),
            DiaCorte = GetNullableByte(dr, "DiaCorte"),
            DiaPago = GetNullableByte(dr, "DiaPago"),
            Estado = GetString(dr, "Estado"),
            Activo = GetBoolean(dr, "Activo"),
            Observacion = GetString(dr, "Observacion"),
            FechaCreacion = GetDateTime(dr, "FechaCreacion"),
            UsuarioCreacion = GetString(dr, "UsuarioCreacion"),
            FechaModificacion = GetNullableDateTime(dr, "FechaModificacion"),
            UsuarioModificacion = GetString(dr, "UsuarioModificacion"),
            FechaAnulacion = HasColumn(dr, "FechaAnulacion") ? GetNullableDateTime(dr, "FechaAnulacion") : null,
            UsuarioAnulacion = HasColumn(dr, "UsuarioAnulacion") ? GetString(dr, "UsuarioAnulacion") : string.Empty,
            MotivoAnulacion = HasColumn(dr, "MotivoAnulacion") ? GetString(dr, "MotivoAnulacion") : string.Empty
        };

        private static TarjetaCreditoPeriodo MapearPeriodo(SqlDataReader dr) => new()
        {
            IdTarjetaPeriodo = Convert.ToInt32(dr["IdTarjetaPeriodo"]),
            IdTarjetaCredito = Convert.ToInt32(dr["IdTarjetaCredito"]),
            Alias = GetString(dr, "Alias"),
            Ultimos4 = GetString(dr, "Ultimos4"),
            Moneda = GetString(dr, "Moneda"),
            EntidadFinanciera = GetString(dr, "EntidadFinanciera"),
            Titular = GetString(dr, "Titular"),
            EmpresaResponsable = GetString(dr, "EmpresaResponsable"),
            PeriodoAnio = Convert.ToInt16(dr["PeriodoAnio"]),
            PeriodoMes = Convert.ToByte(dr["PeriodoMes"]),
            FechaCorte = Convert.ToDateTime(dr["FechaCorte"]),
            FechaVencimiento = Convert.ToDateTime(dr["FechaVencimiento"]),
            ImportePEN = GetNullableDecimal(dr, "ImportePEN"),
            ImporteUSD = GetNullableDecimal(dr, "ImporteUSD"),
            TotalPagadoPEN = GetDecimal(dr, "TotalPagadoPEN"),
            TotalPagadoUSD = GetDecimal(dr, "TotalPagadoUSD"),
            SaldoPendientePEN = GetNullableDecimal(dr, "SaldoPendientePEN"),
            SaldoPendienteUSD = GetNullableDecimal(dr, "SaldoPendienteUSD"),
            EstadoPEN = GetNullableString(dr, "EstadoPEN"),
            EstadoUSD = GetNullableString(dr, "EstadoUSD"),
            Monto = GetNullableDecimal(dr, "Monto"),
            TotalPagado = GetDecimal(dr, "TotalPagado"),
            SaldoPendiente = GetNullableDecimal(dr, "SaldoPendiente"),
            Estado = GetString(dr, "Estado"),
            Observacion = GetString(dr, "Observacion"),
            FechaCreacion = GetDateTime(dr, "FechaCreacion"),
            UsuarioCreacion = GetString(dr, "UsuarioCreacion"),
            FechaModificacion = GetNullableDateTime(dr, "FechaModificacion"),
            UsuarioModificacion = GetString(dr, "UsuarioModificacion"),
            FechaAnulacion = HasColumn(dr, "FechaAnulacion") ? GetNullableDateTime(dr, "FechaAnulacion") : null,
            UsuarioAnulacion = HasColumn(dr, "UsuarioAnulacion") ? GetString(dr, "UsuarioAnulacion") : string.Empty,
            MotivoAnulacion = HasColumn(dr, "MotivoAnulacion") ? GetString(dr, "MotivoAnulacion") : string.Empty
        };

        private static TarjetaCreditoPago MapearPago(SqlDataReader dr) => new()
        {
            IdPago = Convert.ToInt32(dr["IdTarjetaPago"]),
            IdTarjetaPeriodo = Convert.ToInt32(dr["IdTarjetaPeriodo"]),
            FechaPago = Convert.ToDateTime(dr["FechaPago"]),
            IdBanco = GetNullableInt(dr, "IdBanco"),
            Banco = GetString(dr, "Banco"),
            IdCuentaBancaria = GetNullableInt(dr, "IdCuentaBancaria"),
            CuentaBancaria = GetString(dr, "NombreCuenta"),
            Importe = GetDecimal(dr, "Importe"),
            Moneda = GetNullableString(dr, "Moneda"),
            NumeroOperacion = GetString(dr, "NumeroOperacion"),
            Observacion = GetString(dr, "Observacion"),
            Estado = GetString(dr, "Estado"),
            FechaCreacion = GetDateTime(dr, "FechaCreacion"),
            UsuarioCreacion = GetString(dr, "UsuarioCreacion"),
            FechaAnulacion = GetNullableDateTime(dr, "FechaAnulacion"),
            UsuarioAnulacion = GetString(dr, "UsuarioAnulacion"),
            MotivoAnulacion = GetString(dr, "MotivoAnulacion")
        };

        private static TarjetaCreditoHistorial MapearHistorial(SqlDataReader dr) => new()
        {
            IdHistorialTarjetaCredito = Convert.ToInt64(dr["IdHistorialTarjetaCredito"]),
            IdTarjetaCredito = Convert.ToInt32(dr["IdTarjetaCredito"]),
            IdTarjetaPeriodo = GetNullableInt(dr, "IdTarjetaPeriodo"),
            IdTarjetaPago = GetNullableInt(dr, "IdTarjetaPago"),
            Usuario = GetString(dr, "Usuario"),
            Accion = GetString(dr, "Accion"),
            EstadoAnterior = GetString(dr, "EstadoAnterior"),
            EstadoNuevo = GetString(dr, "EstadoNuevo"),
            Descripcion = GetString(dr, "Descripcion"),
            FechaHora = GetDateTime(dr, "FechaHora")
        };

        private static TarjetaCreditoPeriodoResultado MapearResultadoPeriodo(
            SqlParameter idPeriodo,
            SqlParameter resultado,
            SqlParameter mensaje,
            SqlParameter totalPagado,
            SqlParameter saldoPendiente,
            SqlParameter estadoPeriodo)
        {
            return new TarjetaCreditoPeriodoResultado
            {
                IdTarjetaPeriodo = idPeriodo.Value == DBNull.Value ? 0 : Convert.ToInt32(idPeriodo.Value),
                Resultado = resultado.Value != DBNull.Value && Convert.ToBoolean(resultado.Value),
                Mensaje = mensaje.Value?.ToString() ?? string.Empty,
                TotalPagado = totalPagado.Value == DBNull.Value ? 0 : Convert.ToDecimal(totalPagado.Value),
                SaldoPendiente = saldoPendiente.Value == DBNull.Value ? 0 : Convert.ToDecimal(saldoPendiente.Value),
                EstadoPeriodo = estadoPeriodo.Value?.ToString() ?? string.Empty
            };
        }

        private static TarjetaCreditoPagoResultado MapearResultadoPago(
            object idPagoOrigen,
            SqlParameter resultado,
            SqlParameter mensaje,
            SqlParameter totalPagado,
            SqlParameter saldoPendiente,
            SqlParameter estadoPeriodo)
        {
            object idPago = idPagoOrigen is SqlParameter parameter ? parameter.Value : idPagoOrigen;

            return new TarjetaCreditoPagoResultado
            {
                IdPago = idPago == DBNull.Value ? 0 : Convert.ToInt32(idPago),
                Resultado = resultado.Value != DBNull.Value && Convert.ToBoolean(resultado.Value),
                Mensaje = mensaje.Value?.ToString() ?? string.Empty,
                TotalPagado = totalPagado.Value == DBNull.Value ? 0 : Convert.ToDecimal(totalPagado.Value),
                SaldoPendiente = saldoPendiente.Value == DBNull.Value ? 0 : Convert.ToDecimal(saldoPendiente.Value),
                EstadoPeriodo = estadoPeriodo.Value?.ToString() ?? string.Empty
            };
        }

        private static void AgregarParametrosResultado(SqlCommand cmd, out SqlParameter resultado, out SqlParameter mensaje)
        {
            resultado = new("@Resultado", SqlDbType.Bit) { Direction = ParameterDirection.Output };
            mensaje = new("@Mensaje", SqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(resultado);
            cmd.Parameters.Add(mensaje);
        }

        private static void AgregarParametrosPeriodoResultado(
            SqlCommand cmd,
            out SqlParameter resultado,
            out SqlParameter mensaje,
            out SqlParameter totalPagado,
            out SqlParameter saldoPendiente,
            out SqlParameter estadoPeriodo)
        {
            AgregarParametrosResultado(cmd, out resultado, out mensaje);
            totalPagado = new("@TotalPagado", SqlDbType.Decimal) { Direction = ParameterDirection.Output, Precision = 18, Scale = 2 };
            saldoPendiente = new("@SaldoPendiente", SqlDbType.Decimal) { Direction = ParameterDirection.Output, Precision = 18, Scale = 2 };
            estadoPeriodo = new("@EstadoPeriodo", SqlDbType.VarChar, 30) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(totalPagado);
            cmd.Parameters.Add(saldoPendiente);
            cmd.Parameters.Add(estadoPeriodo);
        }

        private static void AgregarParametrosPagoResultado(
            SqlCommand cmd,
            out SqlParameter resultado,
            out SqlParameter mensaje,
            out SqlParameter totalPagado,
            out SqlParameter saldoPendiente,
            out SqlParameter estadoPeriodo)
        {
            AgregarParametrosPeriodoResultado(cmd, out resultado, out mensaje, out totalPagado, out saldoPendiente, out estadoPeriodo);
        }

        private static void AddDecimal(SqlCommand cmd, string name, decimal? value)
        {
            SqlParameter parameter = new(name, SqlDbType.Decimal)
            {
                Precision = 18,
                Scale = 2,
                Value = value.HasValue ? value.Value : DBNull.Value
            };
            cmd.Parameters.Add(parameter);
        }

        private static SqlParameter CrearParametroMonedas(IEnumerable<string>? monedas, string? monedaCompatibilidad)
        {
            DataTable tablaMonedas = new();
            tablaMonedas.Columns.Add("Moneda", typeof(string));

            IEnumerable<string> origen = monedas ?? new[] { monedaCompatibilidad ?? "PEN" };
            foreach (string moneda in origen)
                tablaMonedas.Rows.Add(moneda ?? string.Empty);

            return new SqlParameter("@Monedas", SqlDbType.Structured)
            {
                TypeName = "dbo.TesTarjetaCreditoMonedaType",
                Value = tablaMonedas
            };
        }

        private static void CargarMonedasHabilitadas(SqlConnection conexion, List<TarjetaCredito> tarjetas)
        {
            if (tarjetas.Count == 0)
                return;

            Dictionary<int, TarjetaCredito> tarjetasPorId = [];
            for (int indice = 0; indice < tarjetas.Count; indice++)
            {
                TarjetaCredito tarjeta = tarjetas[indice];
                tarjeta.MonedasHabilitadas = [];
                tarjetasPorId[tarjeta.IdTarjetaCredito] = tarjeta;
            }

            using SqlCommand cmd = new("SELECT IdTarjetaCredito, Moneda FROM dbo.TesTarjetaCreditoMonedas WHERE IdTarjetaCredito IN (" + string.Join(", ", tarjetasPorId.Keys.Select((_, indice) => "@Id" + indice)) + ")", conexion);
            int parametro = 0;
            foreach (int idTarjetaCredito in tarjetasPorId.Keys)
                cmd.Parameters.Add("@Id" + parametro++, SqlDbType.Int).Value = idTarjetaCredito;

            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                if (dr.IsDBNull(1) || !tarjetasPorId.TryGetValue(Convert.ToInt32(dr["IdTarjetaCredito"]), out TarjetaCredito? tarjeta))
                    continue;

                string moneda = dr["Moneda"].ToString()?.Trim().ToUpperInvariant() ?? string.Empty;
                if (moneda != string.Empty && !tarjeta.MonedasHabilitadas.Contains(moneda, StringComparer.OrdinalIgnoreCase))
                    tarjeta.MonedasHabilitadas.Add(moneda);
            }

            foreach (TarjetaCredito tarjeta in tarjetas)
                tarjeta.MonedasHabilitadas.Sort((izquierda, derecha) => string.CompareOrdinal(izquierda, derecha));
        }

        private static string GetString(SqlDataReader dr, string column)
        {
            if (!HasColumn(dr, column))
                return string.Empty;

            object value = dr[column];
            return value == DBNull.Value ? string.Empty : value.ToString() ?? string.Empty;
        }

        private static string? GetNullableString(SqlDataReader dr, string column)
        {
            if (!HasColumn(dr, column) || dr[column] == DBNull.Value)
                return null;

            return dr[column].ToString();
        }

        private static int? GetNullableInt(SqlDataReader dr, string column)
        {
            if (!HasColumn(dr, column))
                return null;

            object value = dr[column];
            return value == DBNull.Value ? null : Convert.ToInt32(value);
        }

        private static byte? GetNullableByte(SqlDataReader dr, string column)
        {
            if (!HasColumn(dr, column))
                return null;

            object value = dr[column];
            return value == DBNull.Value ? null : Convert.ToByte(value);
        }

        private static decimal GetDecimal(SqlDataReader dr, string column)
        {
            object value = dr[column];
            return value == DBNull.Value ? 0 : Convert.ToDecimal(value);
        }

        private static decimal? GetNullableDecimal(SqlDataReader dr, string column)
        {
            if (!HasColumn(dr, column))
                return null;

            object value = dr[column];
            return value == DBNull.Value ? null : Convert.ToDecimal(value);
        }

        private static DateTime GetDateTime(SqlDataReader dr, string column)
        {
            object value = dr[column];
            return value == DBNull.Value ? default : Convert.ToDateTime(value);
        }

        private static DateTime? GetNullableDateTime(SqlDataReader dr, string column)
        {
            if (!HasColumn(dr, column))
                return null;

            object value = dr[column];
            return value == DBNull.Value ? null : Convert.ToDateTime(value);
        }

        private static bool GetBoolean(SqlDataReader dr, string column)
        {
            object value = dr[column];
            return value != DBNull.Value && Convert.ToBoolean(value);
        }

        private static bool HasColumn(SqlDataReader dr, string column)
        {
            for (int i = 0; i < dr.FieldCount; i++)
            {
                if (dr.GetName(i).Equals(column, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
