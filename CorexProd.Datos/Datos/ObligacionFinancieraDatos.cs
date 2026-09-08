using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class ObligacionFinancieraDatos
    {
        public ObligacionFinancieraResultado GuardarPrestamo(ObligacionFinanciera obligacion, string usuario)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_TES_OBFIN_GUARDAR_PRESTAMO", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            SqlParameter id = new("@IdObligacionFinanciera", SqlDbType.Int)
            {
                Direction = ParameterDirection.InputOutput,
                Value = obligacion.IdObligacionFinanciera
            };
            cmd.Parameters.Add(id);
            AgregarParametrosCabecera(cmd, obligacion, usuario);

            SqlParameter cuotas = cmd.Parameters.AddWithValue("@Cuotas", CrearTablaCuotas(obligacion.Cuotas));
            cuotas.SqlDbType = SqlDbType.Structured;
            cuotas.TypeName = "dbo.TesObligacionFinancieraCuotaType";

            AgregarParametrosResultado(cmd, out SqlParameter resultado, out SqlParameter mensaje);

            conexion.Open();
            cmd.ExecuteNonQuery();

            return new ObligacionFinancieraResultado
            {
                IdObligacionFinanciera = id.Value == DBNull.Value ? 0 : Convert.ToInt32(id.Value),
                Resultado = resultado.Value != DBNull.Value && Convert.ToBoolean(resultado.Value),
                Mensaje = mensaje.Value?.ToString() ?? string.Empty
            };
        }

        public ObligacionFinancieraResultado ActualizarPrestamo(ObligacionFinanciera obligacion, string usuario)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_TES_OBFIN_ACTUALIZAR_PRESTAMO", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@IdObligacionFinanciera", SqlDbType.Int).Value = obligacion.IdObligacionFinanciera;
            AgregarParametrosCabecera(cmd, obligacion, usuario);

            SqlParameter cuotas = cmd.Parameters.AddWithValue("@Cuotas", CrearTablaCuotasEdicion(obligacion.Cuotas));
            cuotas.SqlDbType = SqlDbType.Structured;
            cuotas.TypeName = "dbo.TesObligacionFinancieraCuotaEdicionType";

            AgregarParametrosResultado(cmd, out SqlParameter resultado, out SqlParameter mensaje);

            conexion.Open();
            cmd.ExecuteNonQuery();

            return new ObligacionFinancieraResultado
            {
                IdObligacionFinanciera = obligacion.IdObligacionFinanciera,
                Resultado = resultado.Value != DBNull.Value && Convert.ToBoolean(resultado.Value),
                Mensaje = mensaje.Value?.ToString() ?? string.Empty
            };
        }

        public List<ObligacionFinancieraResumen> Listar(string? texto = null, string? estado = null, int? idTipoObligacionFinanciera = null, int? idEntidadFinanciera = null, string? tipoTitular = null, int? idEmpresaResponsable = null)
        {
            List<ObligacionFinancieraResumen> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_TES_OBFIN_LISTAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@Texto", SqlDbType.VarChar, 160).Value = string.IsNullOrWhiteSpace(texto) ? DBNull.Value : texto.Trim();
            cmd.Parameters.Add("@Estado", SqlDbType.VarChar, 30).Value = string.IsNullOrWhiteSpace(estado) || estado == "Todos" ? DBNull.Value : estado.Trim();
            cmd.Parameters.Add("@IdTipoObligacionFinanciera", SqlDbType.Int).Value = idTipoObligacionFinanciera.HasValue ? idTipoObligacionFinanciera.Value : DBNull.Value;
            cmd.Parameters.Add("@IdEntidadFinanciera", SqlDbType.Int).Value = idEntidadFinanciera.HasValue ? idEntidadFinanciera.Value : DBNull.Value;
            cmd.Parameters.Add("@TipoTitular", SqlDbType.VarChar, 20).Value = string.IsNullOrWhiteSpace(tipoTitular) ? DBNull.Value : tipoTitular.Trim();
            cmd.Parameters.Add("@IdEmpresaResponsable", SqlDbType.Int).Value = idEmpresaResponsable.HasValue ? idEmpresaResponsable.Value : DBNull.Value;

            conexion.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
                lista.Add(MapearResumen(dr));

            return lista;
        }

        public ObligacionFinanciera? Obtener(int idObligacionFinanciera)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_TES_OBFIN_OBTENER", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@IdObligacionFinanciera", SqlDbType.Int).Value = idObligacionFinanciera;

            conexion.Open();
            using SqlDataReader dr = cmd.ExecuteReader();

            ObligacionFinanciera? obligacion = null;
            if (dr.Read())
                obligacion = MapearObligacion(dr);

            if (obligacion == null)
                return null;

            if (dr.NextResult())
            {
                while (dr.Read())
                    obligacion.Cuotas.Add(MapearCuota(dr));
            }

            if (dr.NextResult())
            {
                while (dr.Read())
                    obligacion.Pagos.Add(MapearPago(dr));
            }

            if (dr.NextResult())
            {
                while (dr.Read())
                    obligacion.Historial.Add(MapearHistorial(dr));
            }

            return obligacion;
        }

        public ObligacionFinancieraDetalle? ObtenerDetalle(int idObligacionFinanciera)
        {
            ObligacionFinanciera? cabecera = Obtener(idObligacionFinanciera);
            if (cabecera == null)
                return null;

            return new ObligacionFinancieraDetalle
            {
                Cabecera = cabecera,
                Cuotas = cabecera.Cuotas,
                Pagos = cabecera.Pagos,
                Historial = cabecera.Historial
            };
        }

        public ObligacionFinancieraResultado Anular(int idObligacionFinanciera, string usuario, string motivo)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_TES_OBFIN_ANULAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@IdObligacionFinanciera", SqlDbType.Int).Value = idObligacionFinanciera;
            cmd.Parameters.Add("@Usuario", SqlDbType.VarChar, 80).Value = usuario;
            cmd.Parameters.Add("@Motivo", SqlDbType.VarChar, 500).Value = motivo;

            AgregarParametrosResultado(cmd, out SqlParameter resultado, out SqlParameter mensaje);

            conexion.Open();
            cmd.ExecuteNonQuery();

            return new ObligacionFinancieraResultado
            {
                IdObligacionFinanciera = idObligacionFinanciera,
                Resultado = resultado.Value != DBNull.Value && Convert.ToBoolean(resultado.Value),
                Mensaje = mensaje.Value?.ToString() ?? string.Empty
            };
        }

        public ObligacionFinancieraPagoResultado RegistrarPago(ObligacionFinancieraPago pago, string usuario)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_TES_OBFIN_REGISTRAR_PAGO", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            SqlParameter idPago = new("@IdPagoObligacionFinanciera", SqlDbType.Int)
            {
                Direction = ParameterDirection.InputOutput,
                Value = pago.IdPago
            };
            cmd.Parameters.Add(idPago);
            cmd.Parameters.Add("@IdCuotaObligacionFinanciera", SqlDbType.Int).Value = pago.IdCuota;
            cmd.Parameters.Add("@FechaPago", SqlDbType.Date).Value = pago.FechaPago.Date;
            AddDecimal(cmd, "@Monto", pago.Monto);
            cmd.Parameters.Add("@IdBanco", SqlDbType.Int).Value = pago.IdBanco.HasValue ? pago.IdBanco.Value : DBNull.Value;
            cmd.Parameters.Add("@IdCuentaBancaria", SqlDbType.Int).Value = pago.IdCuentaBancaria.HasValue ? pago.IdCuentaBancaria.Value : DBNull.Value;
            cmd.Parameters.Add("@NumeroOperacion", SqlDbType.VarChar, 80).Value = pago.NumeroOperacion ?? string.Empty;
            cmd.Parameters.Add("@Observacion", SqlDbType.VarChar, 500).Value = pago.Observacion ?? string.Empty;
            cmd.Parameters.Add("@Usuario", SqlDbType.VarChar, 80).Value = usuario;

            AgregarParametrosPagoResultado(
                cmd,
                out SqlParameter resultado,
                out SqlParameter mensaje,
                out SqlParameter totalPagado,
                out SqlParameter saldoPendiente,
                out SqlParameter estadoCuota,
                out SqlParameter estadoObligacion);

            conexion.Open();
            cmd.ExecuteNonQuery();

            return MapearResultadoPago(idPago, resultado, mensaje, totalPagado, saldoPendiente, estadoCuota, estadoObligacion);
        }

        public ObligacionFinancieraPagoResultado AnularPago(int idPago, string motivo, string usuario)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_TES_OBFIN_ANULAR_PAGO", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@IdPagoObligacionFinanciera", SqlDbType.Int).Value = idPago;
            cmd.Parameters.Add("@Usuario", SqlDbType.VarChar, 80).Value = usuario;
            cmd.Parameters.Add("@Motivo", SqlDbType.VarChar, 500).Value = motivo;

            AgregarParametrosPagoResultado(
                cmd,
                out SqlParameter resultado,
                out SqlParameter mensaje,
                out SqlParameter totalPagado,
                out SqlParameter saldoPendiente,
                out SqlParameter estadoCuota,
                out SqlParameter estadoObligacion);

            conexion.Open();
            cmd.ExecuteNonQuery();

            return MapearResultadoPago(idPago, resultado, mensaje, totalPagado, saldoPendiente, estadoCuota, estadoObligacion);
        }

        public List<EntidadFinanciera> ListarEntidadesFinancieras(string? tipoEntidad = null)
        {
            List<EntidadFinanciera> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_TES_OBFIN_LISTAR_ENTIDADES", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@TipoEntidad", SqlDbType.VarChar, 30).Value = string.IsNullOrWhiteSpace(tipoEntidad) ? DBNull.Value : tipoEntidad.Trim();

            conexion.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new EntidadFinanciera
                {
                    IdEntidadFinanciera = Convert.ToInt32(dr["IdEntidadFinanciera"]),
                    TipoEntidad = GetString(dr, "TipoEntidad"),
                    Nombre = GetString(dr, "Nombre"),
                    Ruc = GetString(dr, "Ruc"),
                    Activo = Convert.ToBoolean(dr["Activo"]),
                    Observacion = GetString(dr, "Observacion")
                });
            }

            return lista;
        }

        public List<TipoObligacionFinanciera> ListarTiposObligacionFinanciera()
        {
            List<TipoObligacionFinanciera> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_TES_OBFIN_LISTAR_TIPOS", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            conexion.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new TipoObligacionFinanciera
                {
                    IdTipoObligacionFinanciera = Convert.ToInt32(dr["IdTipoObligacionFinanciera"]),
                    Codigo = GetString(dr, "Codigo"),
                    Nombre = GetString(dr, "Nombre"),
                    Descripcion = GetString(dr, "Descripcion"),
                    Activo = Convert.ToBoolean(dr["Activo"])
                });
            }

            return lista;
        }

        public List<BancoTesoreria> ListarBancos(bool soloActivos = true)
        {
            List<BancoTesoreria> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("""
                SELECT IdBanco, Codigo, Nombre, Estado
                FROM dbo.TesBancos
                WHERE @SoloActivos = 0 OR Estado = 1
                ORDER BY Nombre;
                """, conexion);
            cmd.Parameters.Add("@SoloActivos", SqlDbType.Bit).Value = soloActivos;

            conexion.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new BancoTesoreria
                {
                    IdBanco = Convert.ToInt32(dr["IdBanco"]),
                    Codigo = GetString(dr, "Codigo"),
                    Nombre = GetString(dr, "Nombre"),
                    Estado = Convert.ToBoolean(dr["Estado"])
                });
            }

            return lista;
        }

        public List<CuentaBancariaTesoreria> ListarCuentasBancarias(int? idBanco = null, bool soloActivas = true)
        {
            List<CuentaBancariaTesoreria> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("""
                SELECT
                    CB.IdCuentaBancaria,
                    CB.IdBanco,
                    B.Nombre AS Banco,
                    CB.NombreCuenta,
                    CB.Titular,
                    CB.Moneda,
                    CB.NumeroCuenta,
                    CB.Cci,
                    CB.Estado
                FROM dbo.TesCuentasBancarias CB
                INNER JOIN dbo.TesBancos B ON B.IdBanco = CB.IdBanco
                WHERE (@IdBanco IS NULL OR CB.IdBanco = @IdBanco)
                  AND (@SoloActivas = 0 OR CB.Estado = 1)
                ORDER BY B.Nombre, CB.Moneda, CB.NombreCuenta, CB.NumeroCuenta;
                """, conexion);
            cmd.Parameters.Add("@IdBanco", SqlDbType.Int).Value = idBanco.HasValue ? idBanco.Value : DBNull.Value;
            cmd.Parameters.Add("@SoloActivas", SqlDbType.Bit).Value = soloActivas;

            conexion.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new CuentaBancariaTesoreria
                {
                    IdCuentaBancaria = Convert.ToInt32(dr["IdCuentaBancaria"]),
                    IdBanco = Convert.ToInt32(dr["IdBanco"]),
                    Banco = GetString(dr, "Banco"),
                    NombreCuenta = GetString(dr, "NombreCuenta"),
                    Titular = GetString(dr, "Titular"),
                    Moneda = GetString(dr, "Moneda"),
                    NumeroCuenta = GetString(dr, "NumeroCuenta"),
                    Cci = GetString(dr, "Cci"),
                    Estado = Convert.ToBoolean(dr["Estado"])
                });
            }

            return lista;
        }

        private static void AgregarParametrosCabecera(SqlCommand cmd, ObligacionFinanciera obligacion, string usuario)
        {
            cmd.Parameters.Add("@IdEntidadFinanciera", SqlDbType.Int).Value = obligacion.IdEntidadFinanciera;
            cmd.Parameters.Add("@TipoTitular", SqlDbType.VarChar, 20).Value = obligacion.TipoTitular ?? string.Empty;
            cmd.Parameters.Add("@IdTitular", SqlDbType.Int).Value = obligacion.IdTitular.HasValue ? obligacion.IdTitular.Value : DBNull.Value;
            cmd.Parameters.Add("@NombreTitular", SqlDbType.VarChar, 180).Value = obligacion.NombreTitular ?? string.Empty;
            cmd.Parameters.Add("@IdEmpresaResponsable", SqlDbType.Int).Value = obligacion.IdEmpresaResponsable.HasValue ? obligacion.IdEmpresaResponsable.Value : DBNull.Value;
            cmd.Parameters.Add("@CodigoReferencia", SqlDbType.VarChar, 80).Value = obligacion.CodigoReferencia ?? string.Empty;
            cmd.Parameters.Add("@NumeroContrato", SqlDbType.VarChar, 80).Value = obligacion.NumeroContrato ?? string.Empty;
            cmd.Parameters.Add("@NumeroOperacion", SqlDbType.VarChar, 80).Value = obligacion.NumeroOperacion ?? string.Empty;
            cmd.Parameters.Add("@Concepto", SqlDbType.VarChar, 250).Value = obligacion.Concepto ?? string.Empty;
            cmd.Parameters.Add("@Moneda", SqlDbType.VarChar, 10).Value = obligacion.Moneda ?? "PEN";
            AddDecimal(cmd, "@MontoOriginal", obligacion.MontoOriginal);
            cmd.Parameters.Add("@FechaInicio", SqlDbType.Date).Value = obligacion.FechaInicio.Date;
            cmd.Parameters.Add("@TotalCuotas", SqlDbType.Int).Value = obligacion.TotalCuotas;
            cmd.Parameters.Add("@Observacion", SqlDbType.VarChar, 1000).Value = obligacion.Observacion ?? string.Empty;
            cmd.Parameters.Add("@Usuario", SqlDbType.VarChar, 80).Value = usuario;
        }

        private static void AgregarParametrosResultado(SqlCommand cmd, out SqlParameter resultado, out SqlParameter mensaje)
        {
            resultado = new("@Resultado", SqlDbType.Bit) { Direction = ParameterDirection.Output };
            mensaje = new("@Mensaje", SqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(resultado);
            cmd.Parameters.Add(mensaje);
        }

        private static void AgregarParametrosPagoResultado(
            SqlCommand cmd,
            out SqlParameter resultado,
            out SqlParameter mensaje,
            out SqlParameter totalPagado,
            out SqlParameter saldoPendiente,
            out SqlParameter estadoCuota,
            out SqlParameter estadoObligacion)
        {
            resultado = new("@Resultado", SqlDbType.Bit) { Direction = ParameterDirection.Output };
            mensaje = new("@Mensaje", SqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
            totalPagado = new("@TotalPagado", SqlDbType.Decimal) { Direction = ParameterDirection.Output, Precision = 18, Scale = 2 };
            saldoPendiente = new("@SaldoPendiente", SqlDbType.Decimal) { Direction = ParameterDirection.Output, Precision = 18, Scale = 2 };
            estadoCuota = new("@EstadoCuota", SqlDbType.VarChar, 30) { Direction = ParameterDirection.Output };
            estadoObligacion = new("@EstadoObligacionFinanciera", SqlDbType.VarChar, 30) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(resultado);
            cmd.Parameters.Add(mensaje);
            cmd.Parameters.Add(totalPagado);
            cmd.Parameters.Add(saldoPendiente);
            cmd.Parameters.Add(estadoCuota);
            cmd.Parameters.Add(estadoObligacion);
        }

        private static DataTable CrearTablaCuotas(IEnumerable<ObligacionFinancieraCuota> cuotas)
        {
            DataTable tabla = new();
            tabla.Columns.Add("NumeroCuota", typeof(int));
            tabla.Columns.Add("TotalCuotas", typeof(int));
            tabla.Columns.Add("FechaVencimiento", typeof(DateTime));
            tabla.Columns.Add("Importe", typeof(decimal));
            tabla.Columns.Add("Observacion", typeof(string));

            foreach (ObligacionFinancieraCuota cuota in cuotas)
            {
                tabla.Rows.Add(
                    cuota.NumeroCuota,
                    cuota.TotalCuotas,
                    cuota.FechaVencimiento.Date,
                    cuota.Importe,
                    string.IsNullOrWhiteSpace(cuota.Observacion) ? DBNull.Value : cuota.Observacion);
            }

            return tabla;
        }

        private static DataTable CrearTablaCuotasEdicion(IEnumerable<ObligacionFinancieraCuota> cuotas)
        {
            DataTable tabla = new();
            tabla.Columns.Add("IdCuotaObligacionFinanciera", typeof(int));
            tabla.Columns.Add("NumeroCuota", typeof(int));
            tabla.Columns.Add("TotalCuotas", typeof(int));
            tabla.Columns.Add("FechaVencimiento", typeof(DateTime));
            tabla.Columns.Add("Importe", typeof(decimal));
            tabla.Columns.Add("Observacion", typeof(string));

            foreach (ObligacionFinancieraCuota cuota in cuotas)
            {
                tabla.Rows.Add(
                    cuota.IdCuotaObligacionFinanciera > 0 ? cuota.IdCuotaObligacionFinanciera : DBNull.Value,
                    cuota.NumeroCuota,
                    cuota.TotalCuotas,
                    cuota.FechaVencimiento.Date,
                    cuota.Importe,
                    string.IsNullOrWhiteSpace(cuota.Observacion) ? DBNull.Value : cuota.Observacion);
            }

            return tabla;
        }

        private static ObligacionFinanciera MapearObligacion(SqlDataReader dr) => new()
        {
            IdObligacionFinanciera = Convert.ToInt32(dr["IdObligacionFinanciera"]),
            IdTipoObligacionFinanciera = Convert.ToInt32(dr["IdTipoObligacionFinanciera"]),
            CodigoTipoObligacionFinanciera = GetString(dr, "CodigoTipoObligacionFinanciera"),
            TipoObligacionFinanciera = GetString(dr, "TipoObligacionFinanciera"),
            IdEntidadFinanciera = Convert.ToInt32(dr["IdEntidadFinanciera"]),
            TipoEntidad = GetString(dr, "TipoEntidad"),
            EntidadFinanciera = GetString(dr, "EntidadFinanciera"),
            TipoTitular = GetString(dr, "TipoTitular"),
            IdTitular = GetNullableInt(dr, "IdTitular"),
            NombreTitular = GetString(dr, "NombreTitular"),
            IdEmpresaResponsable = GetNullableInt(dr, "IdEmpresaResponsable"),
            EmpresaResponsable = GetString(dr, "EmpresaResponsable"),
            CodigoReferencia = GetString(dr, "CodigoReferencia"),
            NumeroContrato = GetString(dr, "NumeroContrato"),
            NumeroOperacion = GetString(dr, "NumeroOperacion"),
            Concepto = GetString(dr, "Concepto"),
            Moneda = GetString(dr, "Moneda"),
            MontoOriginal = GetNullableDecimal(dr, "MontoOriginal"),
            FechaInicio = Convert.ToDateTime(dr["FechaInicio"]),
            TotalCuotas = Convert.ToInt32(dr["TotalCuotas"]),
            TotalCronograma = Convert.ToDecimal(dr["TotalCronograma"]),
            TotalPagado = Convert.ToDecimal(dr["TotalPagado"]),
            SaldoPendiente = Convert.ToDecimal(dr["SaldoPendiente"]),
            Estado = GetString(dr, "Estado"),
            OrigenTipo = GetString(dr, "OrigenTipo"),
            OrigenId = GetNullableInt(dr, "OrigenId"),
            Observacion = GetString(dr, "Observacion"),
            UsuarioRegistro = GetString(dr, "UsuarioRegistro"),
            FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"]),
            UsuarioModificacion = GetString(dr, "UsuarioModificacion"),
            FechaModificacion = GetNullableDateTime(dr, "FechaModificacion"),
            UsuarioAnulacion = GetString(dr, "UsuarioAnulacion"),
            FechaAnulacion = GetNullableDateTime(dr, "FechaAnulacion"),
            MotivoAnulacion = GetString(dr, "MotivoAnulacion")
        };

        private static ObligacionFinancieraResumen MapearResumen(SqlDataReader dr) => new()
        {
            IdObligacionFinanciera = Convert.ToInt32(dr["IdObligacionFinanciera"]),
            IdTipoObligacionFinanciera = Convert.ToInt32(dr["IdTipoObligacionFinanciera"]),
            CodigoTipoObligacionFinanciera = GetString(dr, "CodigoTipoObligacionFinanciera"),
            TipoObligacionFinanciera = GetString(dr, "TipoObligacionFinanciera"),
            IdEntidadFinanciera = Convert.ToInt32(dr["IdEntidadFinanciera"]),
            TipoEntidad = GetString(dr, "TipoEntidad"),
            EntidadFinanciera = GetString(dr, "EntidadFinanciera"),
            TipoTitular = GetString(dr, "TipoTitular"),
            IdTitular = GetNullableInt(dr, "IdTitular"),
            NombreTitular = GetString(dr, "NombreTitular"),
            IdEmpresaResponsable = GetNullableInt(dr, "IdEmpresaResponsable"),
            EmpresaResponsable = GetString(dr, "EmpresaResponsable"),
            CodigoReferencia = GetString(dr, "CodigoReferencia"),
            NumeroContrato = GetString(dr, "NumeroContrato"),
            NumeroOperacion = GetString(dr, "NumeroOperacion"),
            Concepto = GetString(dr, "Concepto"),
            Moneda = GetString(dr, "Moneda"),
            MontoOriginal = GetNullableDecimal(dr, "MontoOriginal"),
            FechaInicio = Convert.ToDateTime(dr["FechaInicio"]),
            TotalCuotas = Convert.ToInt32(dr["TotalCuotas"]),
            TotalCronograma = Convert.ToDecimal(dr["TotalCronograma"]),
            TotalPagado = Convert.ToDecimal(dr["TotalPagado"]),
            SaldoPendiente = Convert.ToDecimal(dr["SaldoPendiente"]),
            ProximaCuota = GetNullableInt(dr, "ProximaCuota"),
            ProximoVencimiento = GetNullableDateTime(dr, "ProximoVencimiento"),
            ImportePendienteProximaCuota = GetNullableDecimal(dr, "ImportePendienteProximaCuota"),
            Estado = GetString(dr, "Estado"),
            Observacion = GetString(dr, "Observacion"),
            UsuarioRegistro = GetString(dr, "UsuarioRegistro"),
            FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
        };

        private static ObligacionFinancieraCuota MapearCuota(SqlDataReader dr) => new()
        {
            IdCuotaObligacionFinanciera = Convert.ToInt32(dr["IdCuotaObligacionFinanciera"]),
            IdObligacionFinanciera = Convert.ToInt32(dr["IdObligacionFinanciera"]),
            NumeroCuota = Convert.ToInt32(dr["NumeroCuota"]),
            TotalCuotas = Convert.ToInt32(dr["TotalCuotas"]),
            FechaVencimiento = Convert.ToDateTime(dr["FechaVencimiento"]),
            Importe = Convert.ToDecimal(dr["Importe"]),
            TotalPagado = Convert.ToDecimal(dr["TotalPagado"]),
            SaldoPendiente = Convert.ToDecimal(dr["SaldoPendiente"]),
            Estado = GetString(dr, "Estado"),
            Observacion = GetString(dr, "Observacion"),
            FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
        };

        private static ObligacionFinancieraPago MapearPago(SqlDataReader dr) => new()
        {
            IdPago = Convert.ToInt32(dr["IdPagoObligacionFinanciera"]),
            IdCuota = Convert.ToInt32(dr["IdCuotaObligacionFinanciera"]),
            IdObligacionFinanciera = Convert.ToInt32(dr["IdObligacionFinanciera"]),
            NumeroCuota = Convert.ToInt32(dr["NumeroCuota"]),
            FechaPago = Convert.ToDateTime(dr["FechaPago"]),
            Monto = Convert.ToDecimal(dr["Monto"]),
            IdBanco = GetNullableInt(dr, "IdBanco"),
            Banco = GetString(dr, "Banco"),
            IdCuentaBancaria = GetNullableInt(dr, "IdCuentaBancaria"),
            NumeroCuenta = GetString(dr, "NumeroCuenta"),
            NumeroOperacion = GetString(dr, "NumeroOperacion"),
            Observacion = GetString(dr, "Observacion"),
            Estado = GetString(dr, "Estado"),
            UsuarioRegistro = GetString(dr, "UsuarioRegistro"),
            FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"]),
            UsuarioAnulacion = GetString(dr, "UsuarioAnulacion"),
            FechaAnulacion = GetNullableDateTime(dr, "FechaAnulacion"),
            MotivoAnulacion = GetString(dr, "MotivoAnulacion")
        };

        private static ObligacionFinancieraHistorial MapearHistorial(SqlDataReader dr) => new()
        {
            IdHistorialObligacionFinanciera = Convert.ToInt64(dr["IdHistorialObligacionFinanciera"]),
            IdObligacionFinanciera = Convert.ToInt32(dr["IdObligacionFinanciera"]),
            IdCuotaObligacionFinanciera = GetNullableInt(dr, "IdCuotaObligacionFinanciera"),
            IdPagoObligacionFinanciera = GetNullableInt(dr, "IdPagoObligacionFinanciera"),
            Usuario = GetString(dr, "Usuario"),
            Accion = GetString(dr, "Accion"),
            EstadoAnterior = GetString(dr, "EstadoAnterior"),
            EstadoNuevo = GetString(dr, "EstadoNuevo"),
            Descripcion = GetString(dr, "Descripcion"),
            FechaHora = Convert.ToDateTime(dr["FechaHora"])
        };

        private static ObligacionFinancieraPagoResultado MapearResultadoPago(
            object idPagoOrigen,
            SqlParameter resultado,
            SqlParameter mensaje,
            SqlParameter totalPagado,
            SqlParameter saldoPendiente,
            SqlParameter estadoCuota,
            SqlParameter estadoObligacion)
        {
            object idPago = idPagoOrigen is SqlParameter parameter ? parameter.Value : idPagoOrigen;

            return new ObligacionFinancieraPagoResultado
            {
                IdPago = idPago == DBNull.Value ? 0 : Convert.ToInt32(idPago),
                Resultado = resultado.Value != DBNull.Value && Convert.ToBoolean(resultado.Value),
                Mensaje = mensaje.Value?.ToString() ?? string.Empty,
                TotalPagado = totalPagado.Value == DBNull.Value ? 0 : Convert.ToDecimal(totalPagado.Value),
                SaldoPendiente = saldoPendiente.Value == DBNull.Value ? 0 : Convert.ToDecimal(saldoPendiente.Value),
                EstadoCuota = estadoCuota.Value?.ToString() ?? string.Empty,
                EstadoObligacionFinanciera = estadoObligacion.Value?.ToString() ?? string.Empty
            };
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

        private static string GetString(SqlDataReader dr, string column)
        {
            object value = dr[column];
            return value == DBNull.Value ? string.Empty : value.ToString() ?? string.Empty;
        }

        private static int? GetNullableInt(SqlDataReader dr, string column)
        {
            object value = dr[column];
            return value == DBNull.Value ? null : Convert.ToInt32(value);
        }

        private static decimal? GetNullableDecimal(SqlDataReader dr, string column)
        {
            object value = dr[column];
            return value == DBNull.Value ? null : Convert.ToDecimal(value);
        }

        private static DateTime? GetNullableDateTime(SqlDataReader dr, string column)
        {
            object value = dr[column];
            return value == DBNull.Value ? null : Convert.ToDateTime(value);
        }
    }
}
