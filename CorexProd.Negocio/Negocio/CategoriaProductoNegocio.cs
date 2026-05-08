using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System.Collections.Generic;

namespace CorexProd.Negocio.Negocio
{
    public class CategoriaProductoNegocio
    {
        private readonly CategoriaProductoDatos _categoriaProductoDatos = new();

        public List<CategoriaProducto> Listar()
        {
            return _categoriaProductoDatos.Listar();
        }

        public string Guardar(CategoriaProducto categoria)
        {
            categoria.NombreCategoria = categoria.NombreCategoria.Trim();
            categoria.Descripcion = categoria.Descripcion.Trim();

            if (string.IsNullOrWhiteSpace(categoria.NombreCategoria))
                return "El nombre de la categoría es obligatorio.";

            if (categoria.IdCategoriaProducto == 0)
                return _categoriaProductoDatos.Registrar(categoria);

            return _categoriaProductoDatos.Editar(categoria);
        }

        public string Eliminar(int idCategoriaProducto)
        {
            if (idCategoriaProducto <= 0)
                return "Debe seleccionar una categoría válida.";

            return _categoriaProductoDatos.Eliminar(idCategoriaProducto);
        }
    }
}