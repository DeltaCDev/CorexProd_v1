using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class SeguroDatos
    {
        public SeguroResultado Guardar(Seguro seguro, string usuario)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_TES_SEGURO_GUARDAR", conexion) { CommandType = CommandType.StoredProcedure };

            ObligacionFinanciera obligacion = seguro.Obligacion;
            cmd.Parameters.Add("@IdEntidadAseguradora", SqlDbType.Int).Value = obligacion.IdEntidadFinanciera;
            cmd.Parameters.Add("@TipoTitular", SqlDbType.VarChar, 20).Value = obligacion.TipoTitular;
            cmd.Parameters.Add("@IdTitular", SqlDbType.Int).Value = obligacion.IdTitular ?? (object)DBNull.Value;
            cmd.Parameters.Add("@NombreTitular", SqlDbType.VarChar, 180).Value = obligacion.NombreTitular;
            cmd.Parameters.Add("@IdEmpresaResponsable", SqlDbType.Int).Value = obligacion.IdEmpresaResponsable ?? (object)DBNull.Value;
            cmd.Parameters.Add("@CodigoReferencia", SqlDbType.VarChar, 80).Value = obligacion.CodigoReferencia;
            cmd.Parameters.Add("@NumeroContrato", SqlDbType.VarChar, 80).Value = obligacion.NumeroContrato;
            cmd.Parameters.Add("@Concepto", SqlDbType.VarChar, 250).Value = obligacion.Concepto;
            cmd.Parameters.Add("@Moneda", SqlDbType.VarChar, 10).Value = obligacion.Moneda;
            AddDecimal(cmd, "@MontoOriginal", obligacion.MontoOriginal);
            cmd.Parameters.Add("@FechaInicio", SqlDbType.Date).Value = obligacion.FechaInicio.Date;
            cmd.Parameters.Add("@Observacion", SqlDbType.VarChar, 1000).Value = obligacion.Observacion;
            cmd.Parameters.Add("@TipoSeguro", SqlDbType.VarChar, 80).Value = seguro.TipoSeguro;
            cmd.Parameters.Add("@NumeroPoliza", SqlDbType.VarChar, 100).Value = seguro.NumeroPoliza;
            cmd.Parameters.Add("@FechaInicioCobertura", SqlDbType.Date).Value = seguro.FechaInicioCobertura.Date;
            cmd.Parameters.Add("@FechaFinCobertura", SqlDbType.Date).Value = seguro.FechaFinCobertura.Date;
            cmd.Parameters.Add("@Periodicidad", SqlDbType.VarChar, 20).Value = seguro.Periodicidad;
            cmd.Parameters.Add("@Usuario", SqlDbType.VarChar, 80).Value = usuario;

            SqlParameter periodos = cmd.Parameters.AddWithValue("@Periodos", CrearTablaPeriodos(seguro.Periodos));
            periodos.SqlDbType = SqlDbType.Structured;
            periodos.TypeName = "dbo.TesSeguroPeriodoType";

            SqlParameter idObligacion = Salida(cmd, "@IdObligacionFinanciera", SqlDbType.Int);
            SqlParameter idSeguro = Salida(cmd, "@IdSeguro", SqlDbType.Int);
            SqlParameter resultado = Salida(cmd, "@Resultado", SqlDbType.Bit);
            SqlParameter mensaje = Salida(cmd, "@Mensaje", SqlDbType.VarChar, 500);

            conexion.Open();
            cmd.ExecuteNonQuery();
            return new SeguroResultado
            {
                IdObligacionFinanciera = ValorInt(idObligacion),
                IdSeguro = ValorInt(idSeguro),
                Resultado = resultado.Value != DBNull.Value && Convert.ToBoolean(resultado.Value),
                Mensaje = mensaje.Value?.ToString() ?? string.Empty
            };
        }

        public SeguroResultado Actualizar(Seguro seguro, string usuario)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_TES_SEGURO_ACTUALIZAR", conexion) { CommandType = CommandType.StoredProcedure };
            ObligacionFinanciera obligacion = seguro.Obligacion;
            cmd.Parameters.Add("@IdSeguro", SqlDbType.Int).Value = seguro.IdSeguro;
            cmd.Parameters.Add("@IdObligacionFinanciera", SqlDbType.Int).Value = seguro.IdObligacionFinanciera;
            cmd.Parameters.Add("@IdEntidadAseguradora", SqlDbType.Int).Value = obligacion.IdEntidadFinanciera;
            cmd.Parameters.Add("@TipoTitular", SqlDbType.VarChar, 20).Value = obligacion.TipoTitular;
            cmd.Parameters.Add("@IdTitular", SqlDbType.Int).Value = obligacion.IdTitular ?? (object)DBNull.Value;
            cmd.Parameters.Add("@NombreTitular", SqlDbType.VarChar, 180).Value = obligacion.NombreTitular;
            cmd.Parameters.Add("@IdEmpresaResponsable", SqlDbType.Int).Value = obligacion.IdEmpresaResponsable ?? (object)DBNull.Value;
            cmd.Parameters.Add("@CodigoReferencia", SqlDbType.VarChar, 80).Value = obligacion.CodigoReferencia;
            cmd.Parameters.Add("@NumeroContrato", SqlDbType.VarChar, 80).Value = obligacion.NumeroContrato;
            cmd.Parameters.Add("@Concepto", SqlDbType.VarChar, 250).Value = obligacion.Concepto;
            cmd.Parameters.Add("@Moneda", SqlDbType.VarChar, 10).Value = obligacion.Moneda;
            AddDecimal(cmd, "@MontoOriginal", obligacion.MontoOriginal);
            cmd.Parameters.Add("@FechaInicio", SqlDbType.Date).Value = obligacion.FechaInicio.Date;
            cmd.Parameters.Add("@Observacion", SqlDbType.VarChar, 1000).Value = obligacion.Observacion;
            cmd.Parameters.Add("@TipoSeguro", SqlDbType.VarChar, 80).Value = seguro.TipoSeguro;
            cmd.Parameters.Add("@NumeroPoliza", SqlDbType.VarChar, 100).Value = seguro.NumeroPoliza;
            cmd.Parameters.Add("@FechaInicioCobertura", SqlDbType.Date).Value = seguro.FechaInicioCobertura.Date;
            cmd.Parameters.Add("@FechaFinCobertura", SqlDbType.Date).Value = seguro.FechaFinCobertura.Date;
            cmd.Parameters.Add("@Periodicidad", SqlDbType.VarChar, 20).Value = seguro.Periodicidad;
            cmd.Parameters.Add("@Usuario", SqlDbType.VarChar, 80).Value = usuario;
            SqlParameter periodos = cmd.Parameters.AddWithValue("@Periodos", CrearTablaPeriodos(seguro.Periodos)); periodos.SqlDbType = SqlDbType.Structured; periodos.TypeName = "dbo.TesSeguroPeriodoType";
            SqlParameter resultado = Salida(cmd, "@Resultado", SqlDbType.Bit); SqlParameter mensaje = Salida(cmd, "@Mensaje", SqlDbType.VarChar, 500);
            conexion.Open(); cmd.ExecuteNonQuery();
            return new SeguroResultado { IdSeguro = seguro.IdSeguro, IdObligacionFinanciera = seguro.IdObligacionFinanciera, Resultado = resultado.Value != DBNull.Value && Convert.ToBoolean(resultado.Value), Mensaje = mensaje.Value?.ToString() ?? string.Empty };
        }

        public List<SeguroResumen> Listar(string? texto = null, string? estado = null, int? idEntidadAseguradora = null)
        {
            List<SeguroResumen> lista = [];
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_TES_SEGURO_LISTAR", conexion) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add("@Texto", SqlDbType.VarChar, 160).Value = string.IsNullOrWhiteSpace(texto) ? DBNull.Value : texto.Trim();
            cmd.Parameters.Add("@Estado", SqlDbType.VarChar, 30).Value = string.IsNullOrWhiteSpace(estado) || estado.Equals("Todos", StringComparison.OrdinalIgnoreCase) ? DBNull.Value : estado.Trim();
            cmd.Parameters.Add("@IdEntidadAseguradora", SqlDbType.Int).Value = idEntidadAseguradora ?? (object)DBNull.Value;
            conexion.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read()) lista.Add(MapearResumen(dr));
            return lista;
        }

        public Seguro? Obtener(int idObligacionFinanciera)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_TES_SEGURO_OBTENER", conexion) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add("@IdObligacionFinanciera", SqlDbType.Int).Value = idObligacionFinanciera;
            conexion.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            if (!dr.Read()) return null;

            Seguro seguro = MapearSeguro(dr);
            if (dr.NextResult()) while (dr.Read()) seguro.Periodos.Add(MapearPeriodo(dr));
            if (dr.NextResult()) while (dr.Read()) seguro.Pagos.Add(MapearPago(dr));
            if (dr.NextResult()) while (dr.Read()) seguro.Historial.Add(MapearHistorial(dr));
            return seguro;
        }

        public SeguroResultado Anular(int idObligacionFinanciera, string usuario, string motivo)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_TES_SEGURO_ANULAR", conexion) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add("@IdObligacionFinanciera", SqlDbType.Int).Value = idObligacionFinanciera;
            cmd.Parameters.Add("@Usuario", SqlDbType.VarChar, 80).Value = usuario;
            cmd.Parameters.Add("@Motivo", SqlDbType.VarChar, 500).Value = motivo;
            SqlParameter resultado = Salida(cmd, "@Resultado", SqlDbType.Bit);
            SqlParameter mensaje = Salida(cmd, "@Mensaje", SqlDbType.VarChar, 500);
            conexion.Open(); cmd.ExecuteNonQuery();
            return new SeguroResultado { IdObligacionFinanciera = idObligacionFinanciera, Resultado = resultado.Value != DBNull.Value && Convert.ToBoolean(resultado.Value), Mensaje = mensaje.Value?.ToString() ?? string.Empty };
        }

        public SeguroPeriodoResultado AnularPeriodo(int idCuotaObligacionFinanciera, string usuario, string motivo)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_TES_SEGURO_PERIODO_ANULAR", conexion) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add("@IdCuotaObligacionFinanciera", SqlDbType.Int).Value = idCuotaObligacionFinanciera;
            cmd.Parameters.Add("@Usuario", SqlDbType.VarChar, 80).Value = usuario;
            cmd.Parameters.Add("@Motivo", SqlDbType.VarChar, 500).Value = motivo;
            SqlParameter resultado = Salida(cmd, "@Resultado", SqlDbType.Bit);
            SqlParameter mensaje = Salida(cmd, "@Mensaje", SqlDbType.VarChar, 500);
            conexion.Open(); cmd.ExecuteNonQuery();
            return new SeguroPeriodoResultado { IdCuotaObligacionFinanciera = idCuotaObligacionFinanciera, Resultado = resultado.Value != DBNull.Value && Convert.ToBoolean(resultado.Value), Mensaje = mensaje.Value?.ToString() ?? string.Empty };
        }

        private static DataTable CrearTablaPeriodos(IEnumerable<SeguroPeriodo> periodos)
        {
            DataTable tabla = new();
            tabla.Columns.Add("NumeroCuota", typeof(int));
            tabla.Columns.Add("TotalCuotas", typeof(int));
            tabla.Columns.Add("EsCuota", typeof(bool));
            tabla.Columns.Add("FechaVencimiento", typeof(DateTime));
            tabla.Columns.Add("Importe", typeof(decimal));
            tabla.Columns.Add("Observacion", typeof(string));
            foreach (SeguroPeriodo periodo in periodos)
                tabla.Rows.Add(periodo.NumeroCuota, periodo.TotalCuotas, periodo.EsCuota, periodo.FechaVencimiento.Date, periodo.Importe, periodo.Observacion ?? string.Empty);
            return tabla;
        }

        private static Seguro MapearSeguro(SqlDataReader dr) => new()
        {
            IdSeguro = Convert.ToInt32(dr["IdSeguro"]), IdObligacionFinanciera = Convert.ToInt32(dr["IdObligacionFinanciera"]),
            TipoSeguro = Texto(dr, "TipoSeguro"), NumeroPoliza = Texto(dr, "NumeroPoliza"),
            FechaInicioCobertura = Convert.ToDateTime(dr["FechaInicioCobertura"]), FechaFinCobertura = Convert.ToDateTime(dr["FechaFinCobertura"]), Periodicidad = Texto(dr, "Periodicidad"),
            Obligacion = new ObligacionFinanciera
            {
                IdObligacionFinanciera = Convert.ToInt32(dr["IdObligacionFinanciera"]), IdEntidadFinanciera = Convert.ToInt32(dr["IdEntidadAseguradora"]), EntidadFinanciera = Texto(dr, "Aseguradora"),
                TipoTitular = Texto(dr, "TipoTitular"), IdTitular = EnteroNulo(dr, "IdTitular"), NombreTitular = Texto(dr, "NombreTitular"), IdEmpresaResponsable = EnteroNulo(dr, "IdEmpresaResponsable"),
                EmpresaResponsable = Texto(dr, "EmpresaResponsable"), CodigoReferencia = Texto(dr, "CodigoReferencia"), NumeroContrato = Texto(dr, "NumeroContrato"), Concepto = Texto(dr, "Concepto"),
                Moneda = Texto(dr, "Moneda"), MontoOriginal = DecimalNulo(dr, "MontoOriginal"), FechaInicio = Convert.ToDateTime(dr["FechaInicio"]), TotalCuotas = Convert.ToInt32(dr["TotalCuotas"]),
                Estado = Texto(dr, "Estado"), Observacion = Texto(dr, "Observacion"), UsuarioRegistro = Texto(dr, "UsuarioRegistro"), FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"]),
                UsuarioAnulacion = Texto(dr, "UsuarioAnulacion"), FechaAnulacion = FechaNula(dr, "FechaAnulacion"), MotivoAnulacion = Texto(dr, "MotivoAnulacion")
            }
        };

        private static SeguroResumen MapearResumen(SqlDataReader dr) => new()
        {
            IdSeguro = Convert.ToInt32(dr["IdSeguro"]), IdObligacionFinanciera = Convert.ToInt32(dr["IdObligacionFinanciera"]), TipoSeguro = Texto(dr, "TipoSeguro"), NumeroPoliza = Texto(dr, "NumeroPoliza"),
            Aseguradora = Texto(dr, "Aseguradora"), Titular = Texto(dr, "NombreTitular"), EmpresaResponsable = Texto(dr, "EmpresaResponsable"), Concepto = Texto(dr, "Concepto"), Moneda = Texto(dr, "Moneda"),
            MontoOriginal = DecimalNulo(dr, "MontoOriginal"), FechaInicio = Convert.ToDateTime(dr["FechaInicio"]), FechaInicioCobertura = Convert.ToDateTime(dr["FechaInicioCobertura"]), FechaFinCobertura = Convert.ToDateTime(dr["FechaFinCobertura"]),
            Periodicidad = Texto(dr, "Periodicidad"), TotalCuotas = Convert.ToInt32(dr["TotalCuotas"]), TotalPagado = Convert.ToDecimal(dr["TotalPagado"]), SaldoPendiente = Convert.ToDecimal(dr["SaldoPendiente"]), Estado = Texto(dr, "Estado"), Observacion = Texto(dr, "Observacion")
        };

        private static SeguroPeriodo MapearPeriodo(SqlDataReader dr) => new()
        {
            IdCuotaObligacionFinanciera = Convert.ToInt32(dr["IdCuotaObligacionFinanciera"]), IdObligacionFinanciera = Convert.ToInt32(dr["IdObligacionFinanciera"]), NumeroCuota = Convert.ToInt32(dr["NumeroCuota"]), TotalCuotas = Convert.ToInt32(dr["TotalCuotas"]),
            EsCuota = Convert.ToBoolean(dr["EsCuota"]), FechaVencimiento = Convert.ToDateTime(dr["FechaVencimiento"]), Importe = Convert.ToDecimal(dr["Importe"]), TotalPagado = Convert.ToDecimal(dr["TotalPagado"]),
            SaldoPendiente = Convert.ToDecimal(dr["SaldoPendiente"]), Estado = Texto(dr, "Estado"), Observacion = Texto(dr, "Observacion"), FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
        };

        private static ObligacionFinancieraPago MapearPago(SqlDataReader dr) => new()
        {
            IdPago = Convert.ToInt32(dr["IdPagoObligacionFinanciera"]), IdCuota = Convert.ToInt32(dr["IdCuotaObligacionFinanciera"]), IdObligacionFinanciera = Convert.ToInt32(dr["IdObligacionFinanciera"]), NumeroCuota = Convert.ToInt32(dr["NumeroCuota"]),
            FechaPago = Convert.ToDateTime(dr["FechaPago"]), Monto = Convert.ToDecimal(dr["Monto"]), IdBanco = EnteroNulo(dr, "IdBanco"), IdCuentaBancaria = EnteroNulo(dr, "IdCuentaBancaria"),
            NumeroOperacion = Texto(dr, "NumeroOperacion"), Observacion = Texto(dr, "Observacion"), Estado = Texto(dr, "Estado"), UsuarioRegistro = Texto(dr, "UsuarioRegistro"), FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"]),
            UsuarioAnulacion = Texto(dr, "UsuarioAnulacion"), FechaAnulacion = FechaNula(dr, "FechaAnulacion"), MotivoAnulacion = Texto(dr, "MotivoAnulacion")
        };

        private static ObligacionFinancieraHistorial MapearHistorial(SqlDataReader dr) => new()
        {
            IdHistorialObligacionFinanciera = Convert.ToInt64(dr["IdHistorialObligacionFinanciera"]), IdObligacionFinanciera = Convert.ToInt32(dr["IdObligacionFinanciera"]),
            IdCuotaObligacionFinanciera = EnteroNulo(dr, "IdCuotaObligacionFinanciera"), IdPagoObligacionFinanciera = EnteroNulo(dr, "IdPagoObligacionFinanciera"), Usuario = Texto(dr, "Usuario"), Accion = Texto(dr, "Accion"),
            EstadoAnterior = Texto(dr, "EstadoAnterior"), EstadoNuevo = Texto(dr, "EstadoNuevo"), Descripcion = Texto(dr, "Descripcion"), FechaHora = Convert.ToDateTime(dr["FechaHora"])
        };

        private static SqlParameter Salida(SqlCommand cmd, string nombre, SqlDbType tipo, int tamano = 0)
        {
            SqlParameter parametro = new(nombre, tipo) { Direction = ParameterDirection.Output };
            if (tamano > 0) parametro.Size = tamano;
            cmd.Parameters.Add(parametro);
            return parametro;
        }

        private static void AddDecimal(SqlCommand cmd, string nombre, decimal? valor)
        {
            SqlParameter parametro = new(nombre, SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = valor ?? (object)DBNull.Value };
            cmd.Parameters.Add(parametro);
        }

        private static int ValorInt(SqlParameter parametro) => parametro.Value == DBNull.Value ? 0 : Convert.ToInt32(parametro.Value);
        private static string Texto(SqlDataReader dr, string columna) => dr[columna] == DBNull.Value ? string.Empty : dr[columna].ToString() ?? string.Empty;
        private static int? EnteroNulo(SqlDataReader dr, string columna) => dr[columna] == DBNull.Value ? null : Convert.ToInt32(dr[columna]);
        private static decimal? DecimalNulo(SqlDataReader dr, string columna) => dr[columna] == DBNull.Value ? null : Convert.ToDecimal(dr[columna]);
        private static DateTime? FechaNula(SqlDataReader dr, string columna) => dr[columna] == DBNull.Value ? null : Convert.ToDateTime(dr[columna]);
    }
}
