using CorexProd.Datos;
using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;

namespace CorexProd.Negocio.Negocio
{
    public class InsumoNegocio
    {
        private readonly InsumoDatos _datos = new();

        public List<Insumo> Listar()
        {
            return _datos.Listar();
        }

        public void Registrar(Insumo insumo)
        {
            Validar(insumo);
            _datos.Registrar(insumo);
        }

        public void Editar(Insumo insumo)
        {
            if (insumo.IdInsumo <= 0)
                throw new Exception("Seleccione un insumo.");

            Validar(insumo);
            _datos.Editar(insumo);
        }

        public void Eliminar(Insumo insumo)
        {
            if (insumo == null || insumo.IdInsumo <= 0)
                throw new Exception("Seleccione un insumo.");

            _datos.Eliminar(insumo.IdInsumo);
        }

        private void Validar(Insumo insumo)
        {
            if (string.IsNullOrWhiteSpace(insumo.Codigo))
                throw new Exception("Ingrese el código del insumo.");

            if (string.IsNullOrWhiteSpace(insumo.NombreInsumo))
                throw new Exception("Ingrese el nombre del insumo.");

            if (insumo.IdCategoriaInsumo <= 0)
                throw new Exception("Seleccione una categoría.");

            if (insumo.IdUnidadMedida <= 0)
                throw new Exception("Seleccione una unidad de medida.");

            if (insumo.StockMinimo < 0)
                throw new Exception("El stock mínimo no puede ser negativo.");
        }
    }
}