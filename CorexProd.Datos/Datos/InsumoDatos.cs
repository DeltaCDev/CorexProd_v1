using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class InsumoDatos
    {
        public List<Insumo> Listar()
        {
            List<Insumo> lista = new();

            using SqlConnection cn = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_INSUMO_LISTAR", cn);
            cmd.CommandType = CommandType.StoredProcedure;

            cn.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new Insumo
                {
                    IdInsumo = Convert.ToInt32(dr["IdInsumo"]),
                    Codigo = dr["Codigo"].ToString(),
                    NombreInsumo = dr["NombreInsumo"].ToString(),
                    Descripcion = dr["Descripcion"].ToString(),
                    IdCategoriaInsumo = Convert.ToInt32(dr["IdCategoriaInsumo"]),
                    NombreCategoria = dr["NombreCategoria"].ToString(),
                    IdUnidadMedida = Convert.ToInt32(dr["IdUnidadMedida"]),
                    NombreUnidad = dr["NombreUnidad"].ToString(),
                    Abreviatura = dr["Abreviatura"].ToString(),
                    StockMinimo = Convert.ToDecimal(dr["StockMinimo"]),
                    Estado = Convert.ToBoolean(dr["Estado"])
                });
            }

            return lista;
        }

        public void Registrar(Insumo insumo)
        {
            using SqlConnection cn = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_INSUMO_REGISTRAR", cn);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Codigo", insumo.Codigo);
            cmd.Parameters.AddWithValue("@NombreInsumo", insumo.NombreInsumo);
            cmd.Parameters.AddWithValue("@Descripcion", insumo.Descripcion ?? "");
            cmd.Parameters.AddWithValue("@IdCategoriaInsumo", insumo.IdCategoriaInsumo);
            cmd.Parameters.AddWithValue("@IdUnidadMedida", insumo.IdUnidadMedida);
            cmd.Parameters.AddWithValue("@StockMinimo", insumo.StockMinimo);

            cn.Open();
            cmd.ExecuteNonQuery();
        }

        public void Editar(Insumo insumo)
        {
            using SqlConnection cn = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_INSUMO_EDITAR", cn);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@IdInsumo", insumo.IdInsumo);
            cmd.Parameters.AddWithValue("@Codigo", insumo.Codigo);
            cmd.Parameters.AddWithValue("@NombreInsumo", insumo.NombreInsumo);
            cmd.Parameters.AddWithValue("@Descripcion", insumo.Descripcion ?? "");
            cmd.Parameters.AddWithValue("@IdCategoriaInsumo", insumo.IdCategoriaInsumo);
            cmd.Parameters.AddWithValue("@IdUnidadMedida", insumo.IdUnidadMedida);
            cmd.Parameters.AddWithValue("@StockMinimo", insumo.StockMinimo);
            cmd.Parameters.AddWithValue("@Estado", insumo.Estado);

            cn.Open();
            cmd.ExecuteNonQuery();
        }

        public void Eliminar(int idInsumo)
        {
            using SqlConnection cn = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_INSUMO_ELIMINAR", cn);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@IdInsumo", idInsumo);

            cn.Open();
            cmd.ExecuteNonQuery();
        }
    }
}