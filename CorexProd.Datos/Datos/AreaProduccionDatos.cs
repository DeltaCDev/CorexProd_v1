using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class AreaProduccionDatos
    {
        public List<AreaProduccion> Listar()
        {
            List<AreaProduccion> areas = [];
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_PRO_AREA_PRODUCCION_LISTAR", conexion)
            {
                CommandType = CommandType.StoredProcedure
            };

            conexion.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                areas.Add(Mapear(dr));
            }

            return areas;
        }

        public string Guardar(AreaProduccion area)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_PRO_AREA_PRODUCCION_GUARDAR", conexion)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@IdAreaProduccion", area.IdAreaProduccion);
            cmd.Parameters.AddWithValue("@CodigoArea", area.CodigoArea);
            cmd.Parameters.AddWithValue("@NombreArea", area.NombreArea);
            cmd.Parameters.AddWithValue("@Descripcion", area.Descripcion);
            cmd.Parameters.AddWithValue("@OrdenSecuencia", area.OrdenSecuencia);
            cmd.Parameters.AddWithValue("@EsInicio", area.EsInicio);
            cmd.Parameters.AddWithValue("@ManejaMerma", area.ManejaMerma);
            cmd.Parameters.AddWithValue("@EsTermino", area.EsTermino);
            cmd.Parameters.AddWithValue("@ModoEnvio", area.ModoEnvio);
            cmd.Parameters.AddWithValue("@Activo", area.Activo);
            cmd.Parameters.AddWithValue("@IdUsuario", area.UsuarioModificacion ?? area.UsuarioRegistro);
            SqlParameter mensaje = new("@Mensaje", SqlDbType.NVarChar, 500)
            {
                Direction = ParameterDirection.Output
            };
            cmd.Parameters.Add(mensaje);

            conexion.Open();
            cmd.ExecuteNonQuery();
            return mensaje.Value?.ToString() ?? "No se recibió respuesta de la base de datos.";
        }

        public string CambiarEstado(int idAreaProduccion, bool activo, int idUsuario)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_PRO_AREA_PRODUCCION_CAMBIAR_ESTADO", conexion)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@IdAreaProduccion", idAreaProduccion);
            cmd.Parameters.AddWithValue("@Activo", activo);
            cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
            SqlParameter mensaje = new("@Mensaje", SqlDbType.NVarChar, 500)
            {
                Direction = ParameterDirection.Output
            };
            cmd.Parameters.Add(mensaje);

            conexion.Open();
            cmd.ExecuteNonQuery();
            return mensaje.Value?.ToString() ?? "No se recibió respuesta de la base de datos.";
        }

        private static AreaProduccion Mapear(SqlDataReader dr)
        {
            return new AreaProduccion
            {
                IdAreaProduccion = Convert.ToInt32(dr["IdAreaProduccion"]),
                CodigoArea = dr["CodigoArea"].ToString() ?? string.Empty,
                NombreArea = dr["NombreArea"].ToString() ?? string.Empty,
                Descripcion = dr["Descripcion"].ToString() ?? string.Empty,
                OrdenSecuencia = Convert.ToInt32(dr["OrdenSecuencia"]),
                EsInicio = Convert.ToBoolean(dr["EsInicio"]),
                ManejaMerma = Convert.ToBoolean(dr["ManejaMerma"]),
                EsTermino = Convert.ToBoolean(dr["EsTermino"]),
                ModoEnvio = dr["ModoEnvio"].ToString() ?? string.Empty,
                Activo = Convert.ToBoolean(dr["Activo"]),
                UsuarioRegistro = Convert.ToInt32(dr["UsuarioRegistro"]),
                FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"]),
                UsuarioModificacion = dr["UsuarioModificacion"] == DBNull.Value ? null : Convert.ToInt32(dr["UsuarioModificacion"]),
                FechaModificacion = dr["FechaModificacion"] == DBNull.Value ? null : Convert.ToDateTime(dr["FechaModificacion"])
            };
        }
    }
}
