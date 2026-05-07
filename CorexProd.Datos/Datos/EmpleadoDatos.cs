using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class EmpleadoDatos
    {
        public List<Empleado> Listar()
        {
            List<Empleado> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_SEG_EMPLEADO_LISTAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            conexion.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new Empleado
                {
                    IdEmpleado = Convert.ToInt32(dr["IdEmpleado"]),
                    TipoDocumento = dr["TipoDocumento"]?.ToString() ?? string.Empty,
                    NumeroDocumento = dr["NumeroDocumento"]?.ToString() ?? string.Empty,
                    Nombre = dr["Nombre"]?.ToString() ?? string.Empty,
                    Apellido = dr["Apellido"]?.ToString() ?? string.Empty,
                    Sexo = dr["Sexo"]?.ToString() ?? string.Empty,
                    Telefono = dr["Telefono"]?.ToString() ?? string.Empty,
                    Email = dr["Email"]?.ToString() ?? string.Empty,
                    Direccion = dr["Direccion"]?.ToString() ?? string.Empty,
                    IdCargo = Convert.ToInt32(dr["IdCargo"]),
                    NombreCargo = dr["NombreCargo"]?.ToString() ?? string.Empty,
                    FechaNacimiento = dr["FechaNacimiento"] == DBNull.Value ? null : Convert.ToDateTime(dr["FechaNacimiento"]),
                    FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"]),
                    Estado = Convert.ToBoolean(dr["Estado"])
                });
            }

            return lista;
        }

        public string Registrar(Empleado empleado)
        {
            string mensaje = string.Empty;

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_SEG_EMPLEADO_REGISTRAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@TipoDocumento", empleado.TipoDocumento);
            cmd.Parameters.AddWithValue("@NumeroDocumento", empleado.NumeroDocumento);
            cmd.Parameters.AddWithValue("@Nombre", empleado.Nombre);
            cmd.Parameters.AddWithValue("@Apellido", empleado.Apellido);
            cmd.Parameters.AddWithValue("@Sexo", empleado.Sexo);
            cmd.Parameters.AddWithValue("@Telefono", empleado.Telefono);
            cmd.Parameters.AddWithValue("@Email", empleado.Email);
            cmd.Parameters.AddWithValue("@Direccion", empleado.Direccion);
            cmd.Parameters.AddWithValue("@IdCargo", empleado.IdCargo);
            cmd.Parameters.AddWithValue("@FechaNacimiento", empleado.FechaNacimiento ?? (object)DBNull.Value);

            SqlParameter resultado = new("@Resultado", SqlDbType.Bit)
            {
                Direction = ParameterDirection.Output
            };

            SqlParameter mensajeParam = new("@Mensaje", SqlDbType.VarChar, 500)
            {
                Direction = ParameterDirection.Output
            };

            cmd.Parameters.Add(resultado);
            cmd.Parameters.Add(mensajeParam);

            conexion.Open();
            cmd.ExecuteNonQuery();

            mensaje = mensajeParam.Value?.ToString() ?? string.Empty;

            return mensaje;
        }

        public string Editar(Empleado empleado)
        {
            string mensaje = string.Empty;

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_SEG_EMPLEADO_EDITAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@IdEmpleado", empleado.IdEmpleado);
            cmd.Parameters.AddWithValue("@TipoDocumento", empleado.TipoDocumento);
            cmd.Parameters.AddWithValue("@NumeroDocumento", empleado.NumeroDocumento);
            cmd.Parameters.AddWithValue("@Nombre", empleado.Nombre);
            cmd.Parameters.AddWithValue("@Apellido", empleado.Apellido);
            cmd.Parameters.AddWithValue("@Sexo", empleado.Sexo);
            cmd.Parameters.AddWithValue("@Telefono", empleado.Telefono);
            cmd.Parameters.AddWithValue("@Email", empleado.Email);
            cmd.Parameters.AddWithValue("@Direccion", empleado.Direccion);
            cmd.Parameters.AddWithValue("@IdCargo", empleado.IdCargo);
            cmd.Parameters.AddWithValue("@FechaNacimiento", empleado.FechaNacimiento ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Estado", empleado.Estado);

            SqlParameter resultado = new("@Resultado", SqlDbType.Bit)
            {
                Direction = ParameterDirection.Output
            };

            SqlParameter mensajeParam = new("@Mensaje", SqlDbType.VarChar, 500)
            {
                Direction = ParameterDirection.Output
            };

            cmd.Parameters.Add(resultado);
            cmd.Parameters.Add(mensajeParam);

            conexion.Open();
            cmd.ExecuteNonQuery();

            mensaje = mensajeParam.Value?.ToString() ?? string.Empty;

            return mensaje;
        }

        public string Eliminar(int idEmpleado)
        {
            string mensaje = string.Empty;

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_SEG_EMPLEADO_ELIMINAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@IdEmpleado", idEmpleado);

            SqlParameter resultado = new("@Resultado", SqlDbType.Bit)
            {
                Direction = ParameterDirection.Output
            };

            SqlParameter mensajeParam = new("@Mensaje", SqlDbType.VarChar, 500)
            {
                Direction = ParameterDirection.Output
            };

            cmd.Parameters.Add(resultado);
            cmd.Parameters.Add(mensajeParam);

            conexion.Open();
            cmd.ExecuteNonQuery();

            mensaje = mensajeParam.Value?.ToString() ?? string.Empty;

            return mensaje;
        }
    }
}