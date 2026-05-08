using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System.Collections.Generic;

namespace CorexProd.Negocio.Negocio
{
    public class UnidadMedidaNegocio
    {
        private readonly UnidadMedidaDatos _unidadMedidaDatos = new();

        public List<UnidadMedida> Listar()
        {
            return _unidadMedidaDatos.Listar();
        }

        public string Guardar(UnidadMedida unidad)
        {
            unidad.NombreUnidad = unidad.NombreUnidad.Trim();
            unidad.Abreviatura = unidad.Abreviatura.Trim();

            if (string.IsNullOrWhiteSpace(unidad.NombreUnidad))
                return "El nombre de la unidad de medida es obligatorio.";

            if (string.IsNullOrWhiteSpace(unidad.Abreviatura))
                return "La abreviatura es obligatoria.";

            if (unidad.IdUnidadMedida == 0)
                return _unidadMedidaDatos.Registrar(unidad);

            return _unidadMedidaDatos.Editar(unidad);
        }

        public string Eliminar(int idUnidadMedida)
        {
            if (idUnidadMedida <= 0)
                return "Debe seleccionar una unidad de medida válida.";

            return _unidadMedidaDatos.Eliminar(idUnidadMedida);
        }
    }
}