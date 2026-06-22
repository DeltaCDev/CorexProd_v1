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

        public bool RequiereOrdenTrabajo(int idOrdenCompraInterna)
        {
            OrdenCompraInterna? orden = Obtener(idOrdenCompraInterna);
            return orden != null
                && !EsAnulada(orden)
                && !orden.TieneOrdenTrabajo
                && orden.Detalles.Exists(item => item.CantidadPendiente > 0);
        }

        public bool PuedeGenerarGuiaSalida(int idOrdenCompraInterna)
        {
            OrdenCompraInterna? orden = Obtener(idOrdenCompraInterna);
            return orden != null
                && !EsAnulada(orden)
                && orden.Detalles.Exists(item => item.StockActual > 0 && item.CantidadPendiente > 0);
        }

        public string Anular(int idOrdenCompraInterna, string motivoAnulacion, string usuarioAnulacion)
        {
            if (idOrdenCompraInterna <= 0) return "Debe seleccionar una OCI válida.";

            OrdenCompraInterna? orden = Obtener(idOrdenCompraInterna);
            if (orden == null) return "No se encontró la OCI seleccionada.";
            if (EsAnulada(orden)) return "La OCI ya se encuentra anulada.";
            if (orden.TieneOrdenTrabajo)
                return "No se puede anular la OCI porque tiene una Orden de Trabajo emitida.";

            if (string.IsNullOrWhiteSpace(motivoAnulacion))
                return "Debe ingresar el motivo de anulación.";
            if (string.IsNullOrWhiteSpace(usuarioAnulacion)) usuarioAnulacion = "Sistema";
            return _datos.Anular(idOrdenCompraInterna, motivoAnulacion.Trim(), usuarioAnulacion.Trim());
        }

        private static bool EsAnulada(OrdenCompraInterna orden) =>
            orden.Estado.Equals("Anulado", System.StringComparison.OrdinalIgnoreCase);
    }
}
