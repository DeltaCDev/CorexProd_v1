using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;

namespace CorexProd.Negocio.Negocio
{
    public class CategoriaInsumoNegocio
    {
        private readonly CategoriaInsumoDatos _datos = new();

        public List<CategoriaInsumo> Listar()
        {
            return _datos.Listar();
        }

        public void Registrar(CategoriaInsumo categoria)
        {
            if (string.IsNullOrWhiteSpace(categoria.NombreCategoria))
                throw new Exception("Ingrese el nombre de la categoría.");

            _datos.Registrar(categoria);
        }

        public void Editar(CategoriaInsumo categoria)
        {
            if (categoria.IdCategoriaInsumo <= 0)
                throw new Exception("Seleccione una categoría.");

            if (string.IsNullOrWhiteSpace(categoria.NombreCategoria))
                throw new Exception("Ingrese el nombre de la categoría.");

            _datos.Editar(categoria);
        }

        public void Eliminar(CategoriaInsumo categoria)
        {
            if (categoria == null || categoria.IdCategoriaInsumo <= 0)
                throw new Exception("Seleccione una categoría.");

            _datos.Eliminar(categoria.IdCategoriaInsumo);
        }
    }
}