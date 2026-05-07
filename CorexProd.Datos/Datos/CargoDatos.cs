using CorexProd.Entidad.Entidades;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace CorexProd.Datos.Datos
{
    public class CargoDatos
    {
        public List<Cargo> Listar()
        {
            List<Cargo> lista = new List<Cargo>();

            using (SqlConnection conexion = Conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand("USP_SEG_CARGO_LISTAR", conexion);
                cmd.CommandType = CommandType.StoredProcedure;

                conexion.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new Cargo
                        {
                            IdCargo = Convert.ToInt32(dr["IdCargo"]),
                            NombreCargo = dr["NombreCargo"]?.ToString() ?? string.Empty,
                            Estado = Convert.ToBoolean(dr["Estado"]),
                            FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
                        });
                    }
                }
            }

            return lista;
        }

        public string Registrar(Cargo cargo)
        {
            string mensaje = string.Empty;

            using (SqlConnection conexion = Conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand("USP_SEG_CARGO_REGISTRAR", conexion);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@NombreCargo", cargo.NombreCargo);

                SqlParameter resultado = new SqlParameter("@Resultado", SqlDbType.Bit)
                {
                    Direction = ParameterDirection.Output
                };

                SqlParameter mensajeParam = new SqlParameter("@Mensaje", SqlDbType.VarChar, 500)
                {
                    Direction = ParameterDirection.Output
                };

                cmd.Parameters.Add(resultado);
                cmd.Parameters.Add(mensajeParam);

                conexion.Open();
                cmd.ExecuteNonQuery();

                mensaje = mensajeParam.Value?.ToString() ?? string.Empty;
            }

            return mensaje;
        }

        public string Editar(Cargo cargo)
        {
            string mensaje = string.Empty;

            using (SqlConnection conexion = Conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand("USP_SEG_CARGO_EDITAR", conexion);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@IdCargo", cargo.IdCargo);
                cmd.Parameters.AddWithValue("@NombreCargo", cargo.NombreCargo);
                cmd.Parameters.AddWithValue("@Estado", cargo.Estado);

                SqlParameter resultado = new SqlParameter("@Resultado", SqlDbType.Bit)
                {
                    Direction = ParameterDirection.Output
                };

                SqlParameter mensajeParam = new SqlParameter("@Mensaje", SqlDbType.VarChar, 500)
                {
                    Direction = ParameterDirection.Output
                };

                cmd.Parameters.Add(resultado);
                cmd.Parameters.Add(mensajeParam);

                conexion.Open();
                cmd.ExecuteNonQuery();

                mensaje = mensajeParam.Value?.ToString() ?? string.Empty;
            }

            return mensaje;
        }

        public string Eliminar(int idCargo)
        {
            string mensaje = string.Empty;

            using (SqlConnection conexion = Conexion.ObtenerConexion())
            {
                SqlCommand cmd = new SqlCommand("USP_SEG_CARGO_ELIMINAR", conexion);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@IdCargo", idCargo);

                SqlParameter resultado = new SqlParameter("@Resultado", SqlDbType.Bit)
                {
                    Direction = ParameterDirection.Output
                };

                SqlParameter mensajeParam = new SqlParameter("@Mensaje", SqlDbType.VarChar, 500)
                {
                    Direction = ParameterDirection.Output
                };

                cmd.Parameters.Add(resultado);
                cmd.Parameters.Add(mensajeParam);

                conexion.Open();
                cmd.ExecuteNonQuery();

                mensaje = mensajeParam.Value?.ToString() ?? string.Empty;
            }

            return mensaje;
        }
    }
}