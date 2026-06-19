using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class SerieCorrelativoDatos
    {
        public List<TipoDocumentoNumeracion> ListarTipos()
        {
            List<TipoDocumentoNumeracion> lista = [];
            using SqlConnection cn = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_SEG_TIPO_DOCUMENTO_NUMERACION_LISTAR", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open(); using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read()) lista.Add(new TipoDocumentoNumeracion
            {
                CodigoTipoDocumento = dr["CodigoTipoDocumento"]?.ToString() ?? string.Empty,
                NombreTipoDocumento = dr["NombreTipoDocumento"]?.ToString() ?? string.Empty,
                Estado = Convert.ToBoolean(dr["Estado"])
            });
            return lista;
        }

        public List<SerieCorrelativo> Listar(string? codigoTipo = null)
        {
            List<SerieCorrelativo> lista = [];
            using SqlConnection cn = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_SEG_SERIE_LISTAR", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@CodigoTipoDocumento", string.IsNullOrWhiteSpace(codigoTipo) ? DBNull.Value : codigoTipo);
            cn.Open(); using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read()) lista.Add(new SerieCorrelativo
            {
                IdSerieCorrelativo = Convert.ToInt32(dr["IdSerieCorrelativo"]),
                CodigoTipoDocumento = dr["CodigoTipoDocumento"]?.ToString() ?? string.Empty,
                NombreTipoDocumento = dr["NombreTipoDocumento"]?.ToString() ?? string.Empty,
                Serie = dr["Serie"]?.ToString() ?? string.Empty,
                UltimoCorrelativo = Convert.ToInt64(dr["UltimoCorrelativo"]),
                CantidadDigitos = Convert.ToByte(dr["CantidadDigitos"]),
                Activa = Convert.ToBoolean(dr["Activa"]), Predeterminada = Convert.ToBoolean(dr["Predeterminada"]),
                UsuarioModificacion = dr["UsuarioModificacion"]?.ToString() ?? string.Empty,
                FechaModificacion = Convert.ToDateTime(dr["FechaModificacion"]),
                FechaUltimoUso = dr["FechaUltimoUso"] == DBNull.Value ? null : Convert.ToDateTime(dr["FechaUltimoUso"]),
                UltimoNumeroGenerado = dr["UltimoNumeroGenerado"]?.ToString() ?? string.Empty
            });
            return lista;
        }

        public List<SerieCorrelativoHistorial> ListarHistorial(int id)
        {
            List<SerieCorrelativoHistorial> lista = [];
            using SqlConnection cn = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_SEG_SERIE_HISTORIAL_LISTAR", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@IdSerieCorrelativo", id); cn.Open(); using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read()) lista.Add(new SerieCorrelativoHistorial
            {
                IdHistorial = Convert.ToInt64(dr["IdHistorial"]), Accion = dr["Accion"]?.ToString() ?? string.Empty,
                SerieAnterior = dr["SerieAnterior"]?.ToString() ?? string.Empty, SerieNueva = dr["SerieNueva"]?.ToString() ?? string.Empty,
                CorrelativoAnterior = dr["CorrelativoAnterior"] == DBNull.Value ? null : Convert.ToInt64(dr["CorrelativoAnterior"]),
                CorrelativoNuevo = dr["CorrelativoNuevo"] == DBNull.Value ? null : Convert.ToInt64(dr["CorrelativoNuevo"]),
                Usuario = dr["Usuario"]?.ToString() ?? string.Empty, Fecha = Convert.ToDateTime(dr["Fecha"])
            });
            return lista;
        }

        public string Guardar(SerieCorrelativo serie, string usuario)
        {
            using SqlConnection cn = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_SEG_SERIE_GUARDAR", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@IdSerieCorrelativo", serie.IdSerieCorrelativo);
            cmd.Parameters.AddWithValue("@CodigoTipoDocumento", serie.CodigoTipoDocumento);
            cmd.Parameters.AddWithValue("@Serie", serie.Serie);
            cmd.Parameters.AddWithValue("@UltimoCorrelativo", serie.UltimoCorrelativo);
            cmd.Parameters.AddWithValue("@CantidadDigitos", serie.CantidadDigitos);
            cmd.Parameters.AddWithValue("@Activa", serie.Activa);
            cmd.Parameters.AddWithValue("@Predeterminada", serie.Predeterminada);
            cmd.Parameters.AddWithValue("@Usuario", usuario);
            SqlParameter mensaje = new("@Mensaje", SqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(mensaje); cn.Open(); cmd.ExecuteNonQuery();
            return mensaje.Value?.ToString() ?? string.Empty;
        }
    }
}
