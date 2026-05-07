using CorexProd.Entidad.Entidades;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace CorexProd.Datos.Datos
{
    public class RolDatos
    {
        public List<Rol> Listar()
        {
            List<Rol> lista = new List<Rol>();

            using (SqlConnection cn = Conexion.ObtenerConexion())
            {
                cn.Open();

                string query = @"
                    SELECT 
                        IdRol,
                        NombreRol,
                        Estado,
                        FechaRegistro
                    FROM Roles
                    ORDER BY IdRol DESC";

                using (SqlCommand cmd = new SqlCommand(query, cn))
                {
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new Rol
                            {
                                IdRol = Convert.ToInt32(dr["IdRol"]),
                                NombreRol = dr["NombreRol"].ToString(),
                                Estado = Convert.ToBoolean(dr["Estado"]),
                                FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
                            });
                        }
                    }
                }
            }

            return lista;
        }

        public void Registrar(Rol rol)
        {
            using (SqlConnection cn = Conexion.ObtenerConexion())
            {
                cn.Open();

                string query = @"
                    INSERT INTO Roles (NombreRol, Estado)
                    VALUES (@NombreRol, @Estado)";

                using (SqlCommand cmd = new SqlCommand(query, cn))
                {
                    cmd.Parameters.AddWithValue("@NombreRol", rol.NombreRol);
                    cmd.Parameters.AddWithValue("@Estado", rol.Estado);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Editar(Rol rol)
        {
            using (SqlConnection cn = Conexion.ObtenerConexion())
            {
                cn.Open();

                string query = @"
                    UPDATE Roles
                    SET 
                        NombreRol = @NombreRol,
                        Estado = @Estado
                    WHERE IdRol = @IdRol";

                using (SqlCommand cmd = new SqlCommand(query, cn))
                {
                    cmd.Parameters.AddWithValue("@IdRol", rol.IdRol);
                    cmd.Parameters.AddWithValue("@NombreRol", rol.NombreRol);
                    cmd.Parameters.AddWithValue("@Estado", rol.Estado);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}