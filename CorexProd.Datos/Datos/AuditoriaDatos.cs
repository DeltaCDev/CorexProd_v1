using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class AuditoriaDatos
    {
        public void Registrar(Auditoria auditoria)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_AUDITORIA_REGISTRAR", conexion);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Usuario", auditoria.Usuario);
            cmd.Parameters.AddWithValue("@Accion", auditoria.Accion);
            cmd.Parameters.AddWithValue("@Modulo", auditoria.Modulo);
            cmd.Parameters.AddWithValue("@Descripcion", auditoria.Descripcion);
            cmd.Parameters.AddWithValue("@Equipo", auditoria.Equipo);

            conexion.Open();
            cmd.ExecuteNonQuery();
        }
    }
}