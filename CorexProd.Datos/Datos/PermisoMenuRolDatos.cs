using CorexProd.Entidad.Entidades;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace CorexProd.Datos.Datos
{
    public class PermisoMenuRolDatos
    {
        public List<MenuSistema> ListarMenusPorRol(int idRol)
        {
            List<MenuSistema> lista = [];

            using (SqlConnection cn = Conexion.ObtenerConexion())
            {
                cn.Open();

                using (SqlCommand cmd = new SqlCommand("USP_SEG_MENU_LISTAR_POR_ROL", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IdRol", idRol);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new MenuSistema
                            {
                                IdMenu = Convert.ToInt32(dr["IdMenu"]),
                                NombreMenu = dr["NombreMenu"].ToString() ?? string.Empty,
                                IdMenuPadre = dr["IdMenuPadre"] == DBNull.Value
                                ? null
                                : Convert.ToInt32(dr["IdMenuPadre"]),
                                Orden = Convert.ToInt32(dr["Orden"]),
                                TienePermiso = Convert.ToBoolean(dr["TienePermiso"])
                            });
                        }
                    }
                }
            }

            return lista;
        }

        public void GuardarPermiso(int idRol, int idMenu, bool puedeVer)
        {
            using (SqlConnection cn = Conexion.ObtenerConexion())
            {
                cn.Open();

                using (SqlCommand cmd = new SqlCommand("USP_SEG_PERMISO_MENU_GUARDAR", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@IdRol", idRol);
                    cmd.Parameters.AddWithValue("@IdMenu", idMenu);
                    cmd.Parameters.AddWithValue("@PuedeVer", puedeVer);

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}