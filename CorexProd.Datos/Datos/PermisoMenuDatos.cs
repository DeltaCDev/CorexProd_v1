using CorexProd.Entidad.Entidades;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace CorexProd.Datos.Datos
{
    public class PermisoMenuDatos
    {
        public List<string> ObtenerMenusPorRol(int idRol)
        {
            List<string> menus = new List<string>();

            using (SqlConnection cn = Conexion.ObtenerConexion())
            {
                cn.Open();

                string query = @"
                    SELECT m.NombreMenu
                    FROM PermisosMenu pm
                    INNER JOIN Menu m
                        ON m.IdMenu = pm.IdMenu
                    WHERE
                        pm.IdRol = @IdRol
                        AND pm.PuedeVer = 1
                        AND m.Estado = 1";

                using (SqlCommand cmd = new SqlCommand(query, cn))
                {
                    cmd.Parameters.AddWithValue("@IdRol", idRol);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            menus.Add(
                                dr["NombreMenu"].ToString()
                            );
                        }
                    }
                }
            }

            return menus;
        }
    }
}