using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System.Collections.Generic;
using System.Linq;

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

        public string ObtenerSiguienteNumero() => _datos.ObtenerSiguienteNumero();

        public string GuardarDirecta(OrdenCompraInterna orden)
        {
            if (orden.IdCliente <= 0) return "Debe seleccionar un cliente.";
            if (orden.Detalles.Count == 0) return "Debe agregar al menos un producto.";
            if (orden.Detalles.Any(d => d.IdProducto <= 0 || d.Cantidad <= 0))
                return "Todos los productos deben tener cantidad mayor a cero.";
            if (string.IsNullOrWhiteSpace(orden.UsuarioGenerador)) orden.UsuarioGenerador = "Sistema";
            return _datos.GuardarDirecta(orden);
        }

        public string ActualizarDirecta(OrdenCompraInterna orden)
        {
            if (orden.IdOrdenCompraInterna <= 0) return "Debe seleccionar una OC valida.";
            if (orden.IdCliente <= 0) return "Debe seleccionar un cliente.";
            if (orden.Detalles.Count == 0) return "Debe agregar al menos un producto.";
            if (orden.Detalles.Any(d => d.IdProducto <= 0 || d.Cantidad <= 0))
                return "Todos los productos deben tener cantidad mayor a cero.";

            OrdenCompraInterna? actual = Obtener(orden.IdOrdenCompraInterna);
            if (actual == null) return "No se encontro la OC seleccionada.";
            if (!actual.PuedeEditar) return "Solo se puede editar una OC pendiente sin acciones realizadas.";

            if (string.IsNullOrWhiteSpace(orden.UsuarioGenerador)) orden.UsuarioGenerador = "Sistema";
            if (string.IsNullOrWhiteSpace(orden.NumeroOci)) orden.NumeroOci = actual.NumeroOci;
            return _datos.ActualizarDirecta(orden);
        }

        public bool RequiereOrdenTrabajo(int idOrdenCompraInterna)
        {
            OrdenCompraInterna? orden = Obtener(idOrdenCompraInterna);
            return orden != null
                && !EsAnulada(orden)
                && orden.PuedeGenerarOt;
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
            if (orden.TieneGuiaSalida)
                return "No se puede anular la OCI porque tiene una Guia Interna emitida. Primero debe anular la guia.";
            if (orden.TieneOrdenTrabajo)
                return "No se puede anular la OCI porque tiene una Orden de Trabajo emitida.";

            if (string.IsNullOrWhiteSpace(motivoAnulacion))
                return "Debe ingresar el motivo de anulación.";
            if (string.IsNullOrWhiteSpace(usuarioAnulacion)) usuarioAnulacion = "Sistema";
            return _datos.Anular(idOrdenCompraInterna, motivoAnulacion.Trim(), usuarioAnulacion.Trim());
        }

        private static bool EsAnulada(OrdenCompraInterna orden) =>
            orden.Estado.Trim().ToUpperInvariant() is "ANULADO" or "ANULADA";
    }
}
