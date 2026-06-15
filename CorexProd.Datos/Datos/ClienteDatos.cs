using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class ClienteDatos
    {
        public List<Cliente> Listar()
        {
            List<Cliente> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_SEG_CLIENTE_LISTAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            conexion.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new Cliente
                {
                    IdCliente = Convert.ToInt32(dr["IdCliente"]),
                    TipoDocumento = dr["TipoDocumento"]?.ToString() ?? string.Empty,
                    NumeroDocumento = dr["NumeroDocumento"]?.ToString() ?? string.Empty,
                    NombreRazonSocial = dr["NombreRazonSocial"]?.ToString() ?? string.Empty,
                    Direccion = dr["Direccion"]?.ToString() ?? string.Empty,
                    Telefono = dr["Telefono"]?.ToString() ?? string.Empty,
                    Correo = dr["Correo"]?.ToString() ?? string.Empty,
                    Estado = Convert.ToBoolean(dr["Estado"]),
                    FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
                });
            }

            return lista;
        }

        public string Registrar(Cliente cliente)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_SEG_CLIENTE_REGISTRAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@TipoDocumento", cliente.TipoDocumento);
            cmd.Parameters.AddWithValue("@NumeroDocumento", cliente.NumeroDocumento);
            cmd.Parameters.AddWithValue("@NombreRazonSocial", cliente.NombreRazonSocial);
            cmd.Parameters.AddWithValue("@Direccion", cliente.Direccion);
            cmd.Parameters.AddWithValue("@Telefono", cliente.Telefono);
            cmd.Parameters.AddWithValue("@Correo", cliente.Correo);

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

            return mensajeParam.Value?.ToString() ?? string.Empty;
        }

        public string Editar(Cliente cliente)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_SEG_CLIENTE_EDITAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@IdCliente", cliente.IdCliente);
            cmd.Parameters.AddWithValue("@TipoDocumento", cliente.TipoDocumento);
            cmd.Parameters.AddWithValue("@NumeroDocumento", cliente.NumeroDocumento);
            cmd.Parameters.AddWithValue("@NombreRazonSocial", cliente.NombreRazonSocial);
            cmd.Parameters.AddWithValue("@Direccion", cliente.Direccion);
            cmd.Parameters.AddWithValue("@Telefono", cliente.Telefono);
            cmd.Parameters.AddWithValue("@Correo", cliente.Correo);
            cmd.Parameters.AddWithValue("@Estado", cliente.Estado);

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

            return mensajeParam.Value?.ToString() ?? string.Empty;
        }

        public string Eliminar(int idCliente)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_SEG_CLIENTE_ELIMINAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@IdCliente", idCliente);

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

            return mensajeParam.Value?.ToString() ?? string.Empty;
        }
    }
}
