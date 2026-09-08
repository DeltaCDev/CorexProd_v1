using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class ProgramacionTesoreriaDatos
    {
        public List<ProgramacionPagoGeneralItem> ObtenerProgramacionGeneral(
            DateTime fechaInicio,
            DateTime fechaFin,
            bool incluirPendientesAnteriores,
            string? estado = null,
            string? categoria = null,
            string? moneda = null,
            string? texto = null)
        {
            List<ProgramacionPagoGeneralItem> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_TES_PROGRAMACION_GENERAL_RANGO", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@FechaInicio", SqlDbType.Date).Value = fechaInicio.Date;
            cmd.Parameters.Add("@FechaFin", SqlDbType.Date).Value = fechaFin.Date;
            cmd.Parameters.Add("@IncluirPendientesAnteriores", SqlDbType.Bit).Value = incluirPendientesAnteriores;
            cmd.Parameters.Add("@Estado", SqlDbType.VarChar, 30).Value = string.IsNullOrWhiteSpace(estado) || estado == "Todos" ? DBNull.Value : estado.Trim();
            cmd.Parameters.Add("@Categoria", SqlDbType.VarChar, 40).Value = string.IsNullOrWhiteSpace(categoria) || categoria == "Todos" ? DBNull.Value : categoria.Trim();
            cmd.Parameters.Add("@Moneda", SqlDbType.VarChar, 10).Value = string.IsNullOrWhiteSpace(moneda) || moneda == "Todos" ? DBNull.Value : moneda.Trim();
            cmd.Parameters.Add("@Texto", SqlDbType.VarChar, 160).Value = string.IsNullOrWhiteSpace(texto) ? DBNull.Value : texto.Trim();

            conexion.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
                lista.Add(Mapear(dr));

            return lista;
        }

        private static ProgramacionPagoGeneralItem Mapear(SqlDataReader dr) => new()
        {
            FechaVencimiento = Convert.ToDateTime(dr["FechaVencimiento"]),
            Categoria = GetString(dr, "Categoria"),
            Subcategoria = GetString(dr, "Subcategoria"),
            EntidadProveedor = GetString(dr, "EntidadProveedor"),
            Titular = GetString(dr, "Titular"),
            EmpresaResponsable = GetString(dr, "EmpresaResponsable"),
            Concepto = GetString(dr, "Concepto"),
            Referencia = GetString(dr, "Referencia"),
            FechaDocumento = GetNullableDateTime(dr, "FechaDocumento"),
            NumeroCuota = GetNullableInt(dr, "NumeroCuota"),
            TotalCuotas = GetNullableInt(dr, "TotalCuotas"),
            Moneda = GetString(dr, "Moneda"),
            Importe = Convert.ToDecimal(dr["Importe"]),
            Pagado = Convert.ToDecimal(dr["Pagado"]),
            Saldo = Convert.ToDecimal(dr["Saldo"]),
            Estado = GetString(dr, "Estado"),
            OrigenTipo = GetString(dr, "OrigenTipo"),
            OrigenId = Convert.ToInt32(dr["OrigenId"]),
            OrigenDetalleId = Convert.ToInt32(dr["OrigenDetalleId"]),
            EsPendienteAnterior = Convert.ToBoolean(dr["EsPendienteAnterior"])
        };

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

        private static DateTime? GetNullableDateTime(SqlDataReader dr, string column)
        {
            if (!TieneColumna(dr, column))
                return null;

            object value = dr[column];
            return value == DBNull.Value ? null : Convert.ToDateTime(value);
        }

        private static bool TieneColumna(SqlDataReader dr, string column)
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
