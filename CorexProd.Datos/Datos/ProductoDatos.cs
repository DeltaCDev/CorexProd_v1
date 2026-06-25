using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class ProductoDatos
    {
        public List<Producto> Listar()
        {
            List<Producto> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_PRODUCTO_LISTAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            conexion.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new Producto
                {
                    IdProducto = Convert.ToInt32(dr["IdProducto"]),
                    Codigo = dr["Codigo"]?.ToString() ?? string.Empty,
                    NombreProducto = dr["NombreProducto"]?.ToString() ?? string.Empty,
                    Descripcion = dr["Descripcion"]?.ToString() ?? string.Empty,
                    IdSuperCategoriaProducto = Convert.ToInt32(dr["IdSuperCategoriaProducto"]),
                    NombreSuperCategoria = dr["NombreSuperCategoria"]?.ToString() ?? string.Empty,
                    IdCategoriaProducto = Convert.ToInt32(dr["IdCategoriaProducto"]),
                    NombreCategoria = dr["NombreCategoria"]?.ToString() ?? string.Empty,
                    IdUnidadMedida = Convert.ToInt32(dr["IdUnidadMedida"]),
                    NombreUnidad = dr["NombreUnidad"]?.ToString() ?? string.Empty,
                    AbreviaturaUnidad = dr["AbreviaturaUnidad"]?.ToString() ?? string.Empty,
                    StockMinimo = Convert.ToDecimal(dr["StockMinimo"]),
                    Estado = Convert.ToBoolean(dr["Estado"]),
                    FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
                });
            }

            return lista;
        }

        public string Registrar(Producto producto)
        {
            string mensaje = string.Empty;

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_PRODUCTO_REGISTRAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Codigo", producto.Codigo);
            cmd.Parameters.AddWithValue("@NombreProducto", producto.NombreProducto);
            cmd.Parameters.AddWithValue("@Descripcion", producto.Descripcion);
            cmd.Parameters.AddWithValue("@IdCategoriaProducto", producto.IdCategoriaProducto);
            cmd.Parameters.AddWithValue("@IdSuperCategoriaProducto", producto.IdSuperCategoriaProducto);
            cmd.Parameters.AddWithValue("@IdUnidadMedida", producto.IdUnidadMedida);
            cmd.Parameters.AddWithValue("@StockMinimo", producto.StockMinimo);

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

            if (Convert.ToBoolean(resultado.Value))
            {
                InicializarStockProducto(conexion, producto.Codigo);
            }

            return mensaje;
        }

        public string Editar(Producto producto)
        {
            string mensaje = string.Empty;

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_PRODUCTO_EDITAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@IdProducto", producto.IdProducto);
            cmd.Parameters.AddWithValue("@Codigo", producto.Codigo);
            cmd.Parameters.AddWithValue("@NombreProducto", producto.NombreProducto);
            cmd.Parameters.AddWithValue("@Descripcion", producto.Descripcion);
            cmd.Parameters.AddWithValue("@IdCategoriaProducto", producto.IdCategoriaProducto);
            cmd.Parameters.AddWithValue("@IdSuperCategoriaProducto", producto.IdSuperCategoriaProducto);
            cmd.Parameters.AddWithValue("@IdUnidadMedida", producto.IdUnidadMedida);
            cmd.Parameters.AddWithValue("@StockMinimo", producto.StockMinimo);
            cmd.Parameters.AddWithValue("@Estado", producto.Estado);

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

        public string Eliminar(int idProducto)
        {
            string mensaje = string.Empty;

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_ALM_PRODUCTO_ELIMINAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@IdProducto", idProducto);

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

        private static void InicializarStockProducto(SqlConnection conexion, string codigo)
        {
            using SqlCommand cmd = new(@"
IF OBJECT_ID('dbo.StockProductos', 'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.StockProductos (IdProducto, StockActual)
    SELECT P.IdProducto, 0
    FROM dbo.Productos P
    WHERE P.Codigo = @Codigo
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.StockProductos SP
          WHERE SP.IdProducto = P.IdProducto
      );
END", conexion);

            cmd.CommandType = CommandType.Text;
            cmd.Parameters.AddWithValue("@Codigo", codigo);
            cmd.ExecuteNonQuery();
        }
    }
}
