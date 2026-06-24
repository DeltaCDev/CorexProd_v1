using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class MenuSistemaDatos
    {
        public List<MenuSistema> Listar()
        {
            List<MenuSistema> lista = [];

            using SqlConnection cn = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("""
                SELECT
                    m.IdMenu,
                    m.NombreMenu,
                    m.IdMenuPadre,
                    ISNULL(p.NombreMenu, '') AS NombrePadre,
                    m.Orden,
                    m.Estado
                FROM dbo.Menu m
                LEFT JOIN dbo.Menu p ON p.IdMenu = m.IdMenuPadre
                ORDER BY
                    CASE WHEN m.IdMenuPadre IS NULL THEN m.Orden ELSE p.Orden END,
                    CASE WHEN m.IdMenuPadre IS NULL THEN 0 ELSE 1 END,
                    m.Orden,
                    m.NombreMenu;
                """, cn);

            cn.Open();

            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new MenuSistema
                {
                    IdMenu = Convert.ToInt32(dr["IdMenu"]),
                    NombreMenu = dr["NombreMenu"]?.ToString() ?? string.Empty,
                    IdMenuPadre = dr["IdMenuPadre"] == DBNull.Value ? null : Convert.ToInt32(dr["IdMenuPadre"]),
                    NombrePadre = dr["NombrePadre"]?.ToString() ?? string.Empty,
                    Orden = Convert.ToInt32(dr["Orden"]),
                    Estado = Convert.ToBoolean(dr["Estado"])
                });
            }

            return lista;
        }

        public void GuardarOrdenes(IEnumerable<MenuSistema> menus)
        {
            using SqlConnection cn = Conexion.ObtenerConexion();
            cn.Open();

            using SqlTransaction transaccion = cn.BeginTransaction();

            try
            {
                foreach (MenuSistema menu in menus)
                {
                    using SqlCommand cmd = new("""
                        UPDATE dbo.Menu
                        SET Orden = @Orden,
                            Estado = @Estado
                        WHERE IdMenu = @IdMenu;
                        """, cn, transaccion);

                    cmd.Parameters.Add("@IdMenu", SqlDbType.Int).Value = menu.IdMenu;
                    cmd.Parameters.Add("@Orden", SqlDbType.Int).Value = menu.Orden;
                    cmd.Parameters.Add("@Estado", SqlDbType.Bit).Value = menu.Estado;
                    cmd.ExecuteNonQuery();
                }

                transaccion.Commit();
            }
            catch
            {
                transaccion.Rollback();
                throw;
            }
        }
    }
}
