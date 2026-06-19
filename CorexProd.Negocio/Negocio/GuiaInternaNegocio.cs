using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System.Linq;

namespace CorexProd.Negocio.Negocio
{
    public class GuiaInternaNegocio
    {
        private readonly GuiaInternaDatos _datos = new();

        public GuiaInterna? Preparar(int idOrdenCompraInterna, int? idAlmacen = null) =>
            idOrdenCompraInterna > 0 ? _datos.Preparar(idOrdenCompraInterna, idAlmacen) : null;

        public string Emitir(GuiaInterna guia, out string numeroGuia)
        {
            numeroGuia = string.Empty;
            if (guia.IdOrdenCompraInterna <= 0) return "La OCI no es válida.";
            if (guia.IdAlmacen <= 0) return "Debe seleccionar un almacén.";
            if (string.IsNullOrWhiteSpace(guia.UsuarioEmisor)) return "No se pudo identificar al usuario emisor.";
            if (string.IsNullOrWhiteSpace(guia.UsuarioAutorizador)) return "Debe seleccionar al usuario que autoriza.";
            if (!guia.Detalles.Any(d => d.CantidadDespachar > 0)) return "Debe indicar al menos un producto para despachar.";

            GuiaInternaDetalle? invalido = guia.Detalles.FirstOrDefault(d =>
                d.CantidadDespachar < 0 || d.CantidadDespachar > d.CantidadPendiente || d.CantidadDespachar > d.StockActual);
            if (invalido != null)
                return $"La cantidad a despachar de {invalido.CodigoProducto} supera el pendiente o el stock disponible.";

            guia.UsuarioEmisor = guia.UsuarioEmisor.Trim();
            guia.UsuarioAutorizador = guia.UsuarioAutorizador.Trim();
            guia.Observacion = guia.Observacion?.Trim() ?? string.Empty;
            return _datos.Emitir(guia, out numeroGuia);
        }
    }
}
