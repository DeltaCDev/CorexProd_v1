using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System.Collections.Generic;

namespace CorexProd.Negocio.Negocio
{
    public class ProductoNegocio
    {
        private readonly ProductoDatos _productoDatos = new();

        public List<Producto> Listar()
        {
            return _productoDatos.Listar();
        }

        public string Guardar(Producto producto)
        {
            producto.Codigo = producto.Codigo.Trim();
            producto.NombreProducto = producto.NombreProducto.Trim();
            producto.Descripcion = producto.Descripcion.Trim();

            if (string.IsNullOrWhiteSpace(producto.Codigo))
                return "El código del producto es obligatorio.";

            if (string.IsNullOrWhiteSpace(producto.NombreProducto))
                return "El nombre del producto es obligatorio.";

            if (producto.IdCategoriaProducto <= 0)
                return "Debe seleccionar una categoría.";

            if (producto.IdUnidadMedida <= 0)
                return "Debe seleccionar una unidad de medida.";

            if (producto.StockMinimo < 0)
                return "El stock mínimo no puede ser negativo.";

            if (producto.IdProducto == 0)
                return _productoDatos.Registrar(producto);

            return _productoDatos.Editar(producto);
        }

        public string Eliminar(int idProducto)
        {
            if (idProducto <= 0)
                return "Debe seleccionar un producto válido.";

            return _productoDatos.Eliminar(idProducto);
        }
    }
}