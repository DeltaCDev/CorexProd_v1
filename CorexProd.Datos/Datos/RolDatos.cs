using CorexProd.Entidad.Entidades;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class RolDatos
    {
        public List<Rol> Listar()
        {
            List<Rol> lista = [];

            using (SqlConnection cn = Conexion.ObtenerConexion())
            {
                cn.Open();

                using (SqlCommand cmd = new SqlCommand(
                    "USP_SEG_ROL_LISTAR", cn))
                {
                    cmd.CommandType =
                        CommandType.StoredProcedure;

                    using (SqlDataReader dr =
                        cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new Rol
                            {
                                IdRol =
                                    Convert.ToInt32(dr["IdRol"]),

                                NombreRol =
                                    dr["NombreRol"].ToString()
                                    ?? string.Empty,

                                Estado =
                                    Convert.ToBoolean(dr["Estado"]),

                                FechaRegistro =
                                    Convert.ToDateTime(
                                        dr["FechaRegistro"])
                            });
                        }
                    }
                }
            }

            return lista;
        }

        public void Registrar(Rol rol)
        {
            using (SqlConnection cn =
                Conexion.ObtenerConexion())
            {
                cn.Open();

                using (SqlCommand cmd = new SqlCommand(
                    "USP_SEG_ROL_REGISTRAR", cn))
                {
                    cmd.CommandType =
                        CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue(
                        "@NombreRol", rol.NombreRol);

                    cmd.Parameters.AddWithValue(
                        "@Estado", rol.Estado);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Editar(Rol rol)
        {
            using (SqlConnection cn =
                Conexion.ObtenerConexion())
            {
                cn.Open();

                using (SqlCommand cmd = new SqlCommand(
                    "USP_SEG_ROL_EDITAR", cn))
                {
                    cmd.CommandType =
                        CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue(
                        "@IdRol", rol.IdRol);

                    cmd.Parameters.AddWithValue(
                        "@NombreRol", rol.NombreRol);

                    cmd.Parameters.AddWithValue(
                        "@Estado", rol.Estado);

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}