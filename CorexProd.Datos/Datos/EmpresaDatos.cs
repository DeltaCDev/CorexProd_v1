using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class EmpresaDatos
    {
        public List<Empresa> Listar()
        {
            List<Empresa> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_SEG_EMPRESA_LISTAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            conexion.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(MapearEmpresa(dr));
            }

            return lista;
        }

        public Empresa? ObtenerPredeterminada()
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_SEG_EMPRESA_OBTENER_PREDETERMINADA", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            conexion.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            return dr.Read() ? MapearEmpresa(dr) : null;
        }

        public string Registrar(Empresa empresa)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_SEG_EMPRESA_REGISTRAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            AgregarParametrosEmpresa(cmd, empresa);

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

        public string Editar(Empresa empresa)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_SEG_EMPRESA_EDITAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@IdEmpresa", empresa.IdEmpresa);
            AgregarParametrosEmpresa(cmd, empresa);
            cmd.Parameters.AddWithValue("@Estado", empresa.Estado);

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

        public string Eliminar(int idEmpresa)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_SEG_EMPRESA_ELIMINAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@IdEmpresa", idEmpresa);

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

        private static void AgregarParametrosEmpresa(SqlCommand cmd, Empresa empresa)
        {
            cmd.Parameters.AddWithValue("@Ruc", empresa.Ruc);
            cmd.Parameters.AddWithValue("@Nombre", empresa.Nombre);
            cmd.Parameters.AddWithValue("@NombreComercial", empresa.NombreComercial);
            cmd.Parameters.AddWithValue("@Telefono", empresa.Telefono);
            cmd.Parameters.AddWithValue("@Correo", empresa.Correo);
            cmd.Parameters.AddWithValue("@Departamento", empresa.Departamento);
            cmd.Parameters.AddWithValue("@Provincia", empresa.Provincia);
            cmd.Parameters.AddWithValue("@Distrito", empresa.Distrito);
            cmd.Parameters.AddWithValue("@Direccion", empresa.Direccion);
            SqlParameter logoParam = new("@Logo", SqlDbType.VarBinary, -1)
            {
                Value = empresa.Logo == null || empresa.Logo.Length == 0 ? DBNull.Value : empresa.Logo
            };
            cmd.Parameters.Add(logoParam);
            SqlParameter iconoParam = new("@Icono", SqlDbType.VarBinary, -1)
            {
                Value = empresa.Icono == null || empresa.Icono.Length == 0 ? DBNull.Value : empresa.Icono
            };
            cmd.Parameters.Add(iconoParam);
            cmd.Parameters.AddWithValue("@CodigoCliente", empresa.CodigoCliente);
            cmd.Parameters.AddWithValue("@LicenciaActivacion", empresa.LicenciaActivacion);
            cmd.Parameters.AddWithValue("@EsPredeterminada", empresa.EsPredeterminada);
        }

        private static Empresa MapearEmpresa(SqlDataReader dr)
        {
            return new Empresa
            {
                IdEmpresa = Convert.ToInt32(dr["IdEmpresa"]),
                Ruc = dr["Ruc"]?.ToString() ?? string.Empty,
                Nombre = dr["Nombre"]?.ToString() ?? string.Empty,
                NombreComercial = dr["NombreComercial"]?.ToString() ?? string.Empty,
                Telefono = dr["Telefono"]?.ToString() ?? string.Empty,
                Correo = dr["Correo"]?.ToString() ?? string.Empty,
                Departamento = dr["Departamento"]?.ToString() ?? string.Empty,
                Provincia = dr["Provincia"]?.ToString() ?? string.Empty,
                Distrito = dr["Distrito"]?.ToString() ?? string.Empty,
                Direccion = dr["Direccion"]?.ToString() ?? string.Empty,
                Logo = dr["Logo"] == DBNull.Value ? null : (byte[])dr["Logo"],
                Icono = dr["Icono"] == DBNull.Value ? null : (byte[])dr["Icono"],
                CodigoCliente = dr["CodigoCliente"]?.ToString() ?? string.Empty,
                LicenciaActivacion = dr["LicenciaActivacion"]?.ToString() ?? string.Empty,
                EsPredeterminada = Convert.ToBoolean(dr["EsPredeterminada"]),
                Estado = Convert.ToBoolean(dr["Estado"]),
                FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
            };
        }
    }
}
