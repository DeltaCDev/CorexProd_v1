using CorexProd.Entidad.Entidades;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class MenuPermitidoDatos
    {
        public List<string> ObtenerMenusPorRol(int idRol)
        {
            List<string> menus = [];

            using (SqlConnection cn = Conexion.ObtenerConexion())
            {
                cn.Open();

                using (SqlCommand cmd = new SqlCommand(
                    "USP_SEG_MENU_OBTENERPORROL", cn))
                {
                    cmd.CommandType =
                        CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue(
                        "@IdRol", idRol);

                    using (SqlDataReader dr =
                        cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            menus.Add(
                                dr["NombreMenu"].ToString()
                                ?? string.Empty);
                        }
                    }
                }
            }

            return menus;
        }
    }
}