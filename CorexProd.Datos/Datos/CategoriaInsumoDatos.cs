using CorexProd.Entidad;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CorexProd.Datos
{
    public class CategoriaInsumoDatos
    {
        public List<CategoriaInsumo> Listar()
        {
            List<CategoriaInsumo> lista = new();

            using SqlConnection cn = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_CATEGORIA_INSUMO_LISTAR", cn);
            cmd.CommandType = CommandType.StoredProcedure;

            cn.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new CategoriaInsumo
                {
                    IdCategoriaInsumo = Convert.ToInt32(dr["IdCategoriaInsumo"]),
                    NombreCategoria = dr["NombreCategoria"].ToString(),
                    Descripcion = dr["Descripcion"].ToString(),
                    Estado = Convert.ToBoolean(dr["Estado"])
                });
            }

            return lista;
        }

        public void Registrar(CategoriaInsumo categoria)
        {
            using SqlConnection cn = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_CATEGORIA_INSUMO_REGISTRAR", cn);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@NombreCategoria", categoria.NombreCategoria);
            cmd.Parameters.AddWithValue("@Descripcion", categoria.Descripcion ?? "");

            cn.Open();
            cmd.ExecuteNonQuery();
        }

        public void Editar(CategoriaInsumo categoria)
        {
            using SqlConnection cn = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_CATEGORIA_INSUMO_EDITAR", cn);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@IdCategoriaInsumo", categoria.IdCategoriaInsumo);
            cmd.Parameters.AddWithValue("@NombreCategoria", categoria.NombreCategoria);
            cmd.Parameters.AddWithValue("@Descripcion", categoria.Descripcion ?? "");
            cmd.Parameters.AddWithValue("@Estado", categoria.Estado);

            cn.Open();
            cmd.ExecuteNonQuery();
        }

        public void Eliminar(int idCategoriaInsumo)
        {
            using SqlConnection cn = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_CATEGORIA_INSUMO_ELIMINAR", cn);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@IdCategoriaInsumo", idCategoriaInsumo);

            cn.Open();
            cmd.ExecuteNonQuery();
        }
    }
}