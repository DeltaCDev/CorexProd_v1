using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System.Collections.Generic;

namespace CorexProd.Negocio.Negocio
{
    public class ParametroNegocio
    {
        private readonly ParametroDatos _parametroDatos = new();

        public List<Parametro> Listar()
        {
            return _parametroDatos.Listar();
        }

        public Parametro? ObtenerPorCodigo(string codigoParametro)
        {
            if (string.IsNullOrWhiteSpace(codigoParametro))
                return null;

            return _parametroDatos.ObtenerPorCodigo(codigoParametro.Trim());
        }

        public string Guardar(Parametro parametro)
        {
            parametro.CodigoParametro = parametro.CodigoParametro.Trim();
            parametro.NombreParametro = parametro.NombreParametro.Trim();
            parametro.ValorParametro = parametro.ValorParametro.Trim();
            parametro.Descripcion = parametro.Descripcion.Trim();

            if (string.IsNullOrWhiteSpace(parametro.CodigoParametro))
                return "El código del parámetro es obligatorio.";

            if (string.IsNullOrWhiteSpace(parametro.NombreParametro))
                return "El nombre del parámetro es obligatorio.";

            if (string.IsNullOrWhiteSpace(parametro.ValorParametro))
                return "El valor del parámetro es obligatorio.";

            if (parametro.IdParametro == 0)
                return _parametroDatos.Registrar(parametro);

            return _parametroDatos.Editar(parametro);
        }

        public string Eliminar(int idParametro)
        {
            if (idParametro <= 0)
                return "Debe seleccionar un parámetro válido.";

            return _parametroDatos.Eliminar(idParametro);
        }
    }
}