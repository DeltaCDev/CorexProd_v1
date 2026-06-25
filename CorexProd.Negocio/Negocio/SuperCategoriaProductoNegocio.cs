using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System.Collections.Generic;

namespace CorexProd.Negocio.Negocio
{
    public class SuperCategoriaProductoNegocio
    {
        private readonly SuperCategoriaProductoDatos _superCategoriaProductoDatos = new();

        public List<SuperCategoriaProducto> Listar()
        {
            return _superCategoriaProductoDatos.Listar();
        }

        public string Guardar(SuperCategoriaProducto superCategoria)
        {
            superCategoria.NombreSuperCategoria = superCategoria.NombreSuperCategoria.Trim().ToUpperInvariant();
            superCategoria.Descripcion = superCategoria.Descripcion.Trim();

            if (string.IsNullOrWhiteSpace(superCategoria.NombreSuperCategoria))
                return "El nombre de la supercategoría es obligatorio.";

            if (superCategoria.IdSuperCategoriaProducto == 0)
                return _superCategoriaProductoDatos.Registrar(superCategoria);

            return _superCategoriaProductoDatos.Editar(superCategoria);
        }

        public string Eliminar(int idSuperCategoriaProducto)
        {
            if (idSuperCategoriaProducto <= 0)
                return "Debe seleccionar una supercategoría válida.";

            return _superCategoriaProductoDatos.Eliminar(idSuperCategoriaProducto);
        }
    }
}
