using CorexProd.Entidad.Entidades;
using System.Data.SqlClient;

namespace CorexProd.Datos.Datos
{
    public class UsuarioDatos
    {
        public Usuario Login(string usuario, string clave)
        {
            Usuario usuarioEncontrado = null;

            using (SqlConnection cn = Conexion.ObtenerConexion())
            {
                cn.Open();

                string query = @"
                               SELECT
                                   u.IdUsuario,
                                   u.NombreUsuario,
                                   u.Clave,

                                   e.Nombre + ' ' + e.Apellido AS NombreCompleto,

                                   u.IdRol,
                                   r.NombreRol,
                                   u.Estado

                               FROM Usuarios u

                               INNER JOIN Empleados e
                                   ON e.IdEmpleado = u.IdEmpleado

                               INNER JOIN Roles r
                                   ON r.IdRol = u.IdRol

                               WHERE
                                   u.NombreUsuario = @Usuario
                                   AND u.Clave = @Clave
                                   AND u.Estado = 1";

                using (SqlCommand cmd = new SqlCommand(query, cn))
                {
                    cmd.Parameters.AddWithValue("@Usuario", usuario);
                    cmd.Parameters.AddWithValue("@Clave", clave);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            usuarioEncontrado = new Usuario
                            {
                                IdUsuario = Convert.ToInt32(dr["IdUsuario"]),
                                NombreUsuario = dr["NombreUsuario"].ToString(),
                                Clave = dr["Clave"].ToString(),
                                NombreCompleto = dr["NombreCompleto"].ToString(),
                                IdRol = Convert.ToInt32(dr["IdRol"]),
                                NombreRol = dr["NombreRol"].ToString(),
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