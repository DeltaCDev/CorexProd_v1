using System.Configuration;
using Microsoft.Data.SqlClient;

namespace CorexProd.Datos
{
    public class Conexion
    {
        public static SqlConnection ObtenerConexion()
        {
            return new SqlConnection(
                ConfigurationManager
                .ConnectionStrings["ConexionSQL"]
                .ConnectionString
            );
        }
    }
}