using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System.Collections.Generic;

namespace CorexProd.Negocio.Negocio
{
    public class OrdenCompraInternaNegocio
    {
        private readonly OrdenCompraInternaDatos _datos = new();

        public List<OrdenCompraInterna> Listar() => _datos.Listar();

        public OrdenCompraInterna? Obtener(int idOrdenCompraInterna) =>
            idOrdenCompraInterna > 0 ? _datos.Obtener(idOrdenCompraInterna) : null;

        public string Generar(int idProforma, string usuarioGenerador)
        {
            if (idProforma <= 0) return "Debe seleccionar una proforma válida.";
            if (string.IsNullOrWhiteSpace(usuarioGenerador)) usuarioGenerador = "Sistema";
            return _datos.Generar(idProforma, usuarioGenerador.Trim());
        }
    }
}
