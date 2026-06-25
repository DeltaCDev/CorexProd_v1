using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using CorexProd.Entidad.Utilidades;
using System.Collections.Generic;

namespace CorexProd.Negocio.Negocio
{
    public class FichaTecnicaDocumentoNegocio
    {
        private readonly FichaTecnicaDocumentoDatos _datos = new();

        public FichaTecnicaDocumento? ObtenerPorProducto(string codigoProducto)
        {
            string codigoModelo = CodigoModeloProducto.Obtener(codigoProducto);
            return string.IsNullOrWhiteSpace(codigoModelo)
                ? null
                : _datos.ObtenerPorModelo(codigoModelo);
        }

        public List<FichaTecnicaDocumento> Listar() => _datos.Listar();
    }
}
