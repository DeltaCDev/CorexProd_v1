using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class FichaTecnicaDocumentoDatos
    {
        public FichaTecnicaDocumento? ObtenerPorModelo(string codigoModelo)
        {
            const string sql = @"
SELECT TOP (1)
    IdFichaTecnicaDocumento,
    CodigoModelo,
    NombreArchivo,
    RutaRelativa,
    Version,
    Estado,
    FechaRegistro,
    UsuarioRegistro
FROM dbo.FichaTecnicaDocumento
WHERE CodigoModelo = @CodigoModelo
  AND Estado = 1
ORDER BY Version DESC, IdFichaTecnicaDocumento DESC;";

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new(sql, conexion);
            cmd.Parameters.AddWithValue("@CodigoModelo", codigoModelo);
            conexion.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            return dr.Read() ? Mapear(dr) : null;
        }

        public List<FichaTecnicaDocumento> Listar()
        {
            const string sql = @"
SELECT
    IdFichaTecnicaDocumento,
    CodigoModelo,
    NombreArchivo,
    RutaRelativa,
    Version,
    Estado,
    FechaRegistro,
    UsuarioRegistro
FROM dbo.FichaTecnicaDocumento
ORDER BY CodigoModelo, Version DESC;";

            List<FichaTecnicaDocumento> lista = [];
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new(sql, conexion);
            conexion.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
                lista.Add(Mapear(dr));
            return lista;
        }

        private static FichaTecnicaDocumento Mapear(SqlDataReader dr) => new()
        {
            IdFichaTecnicaDocumento = Convert.ToInt32(dr["IdFichaTecnicaDocumento"]),
            CodigoModelo = dr["CodigoModelo"]?.ToString() ?? string.Empty,
            NombreArchivo = dr["NombreArchivo"]?.ToString() ?? string.Empty,
            RutaRelativa = dr["RutaRelativa"]?.ToString() ?? string.Empty,
            Version = Convert.ToInt32(dr["Version"]),
            Estado = Convert.ToBoolean(dr["Estado"]),
            FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"]),
            UsuarioRegistro = dr["UsuarioRegistro"]?.ToString() ?? string.Empty
        };
    }
}
