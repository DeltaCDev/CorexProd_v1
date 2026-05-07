using CorexProd.Entidad.Entidades;
using System.Data.SqlClient;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class UsuarioDatos
    {
        public Usuario? Login(string usuario, string clave)
        {
            Usuario? usuarioEncontrado = null;

            using (SqlConnection cn = Conexion.ObtenerConexion())
            {
                cn.Open();

                using (SqlCommand cmd = new SqlCommand( "USP_SEG_USUARIO_LOGIN", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue( "@Usuario", usuario);
                    cmd.Parameters.AddWithValue( "@Clave", clave);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            usuarioEncontrado = new Usuario
                            {
                                IdUsuario = Convert.ToInt32(dr["IdUsuario"]),
                                NombreUsuario =  dr["NombreUsuario"].ToString() ?? string.Empty,
                                Clave = dr["Clave"].ToString() ?? string.Empty,
                                NombreCompleto = dr["NombreCompleto"].ToString() ?? string.Empty,
                                IdRol = Convert.ToInt32(dr["IdRol"]),
                                NombreRol = dr["NombreRol"].ToString() ?? string.Empty,
                                Estado = Convert.ToBoolean(dr["Estado"])
                            };
                        }
                    }
                }
            }

            return usuarioEncontrado;
        }
    }
}