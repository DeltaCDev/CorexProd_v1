using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CorexProd.Negocio.Negocio
{
    public class GuiaInternaNegocio
    {
        private readonly GuiaInternaDatos _datos = new();

        public List<GuiaInterna> Listar(DateTime? desde, DateTime? hasta, int? idAlmacen, string estado, string origen, string texto) =>
            _datos.Listar(desde, hasta, idAlmacen, estado, origen, texto);

        public GuiaInterna? Obtener(int idGuiaInterna) => idGuiaInterna > 0 ? _datos.Obtener(idGuiaInterna) : null;

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

        public GuiaInterna? PrepararManual(int? idAlmacen = null) => _datos.PrepararManual(idAlmacen);

        public string EmitirManual(GuiaInterna guia, out string numeroGuia)
        {
            numeroGuia = string.Empty;
            if (guia.IdAlmacen <= 0) return "Debe seleccionar un almacén.";
            if (string.IsNullOrWhiteSpace(guia.MotivoEmisionManual)) return "Debe ingresar el motivo de la emisión manual.";
            if (string.IsNullOrWhiteSpace(guia.UsuarioEmisor)) return "No se pudo identificar al usuario emisor.";
            if (string.IsNullOrWhiteSpace(guia.UsuarioAutorizador)) return "Debe seleccionar al usuario que autoriza.";
            if (!guia.Detalles.Any(d => d.CantidadDespachar > 0)) return "Debe indicar al menos un producto para despachar.";
            if (guia.Detalles.Any(d => d.StockActual <= 0)) return "Producto seleccionado no tiene stock.";
            GuiaInternaDetalle? invalido = guia.Detalles.FirstOrDefault(d => d.CantidadDespachar < 0 || d.CantidadDespachar > d.StockActual);
            if (invalido != null) return $"La cantidad de {invalido.CodigoProducto} supera el stock disponible.";
            guia.MotivoEmisionManual = guia.MotivoEmisionManual.Trim();
            guia.Observacion = guia.Observacion?.Trim() ?? string.Empty;
            return _datos.EmitirManual(guia, out numeroGuia);
        }

        public string Anular(int idGuiaInterna, string usuario, string motivo)
        {
            if (idGuiaInterna <= 0) return "Debe seleccionar una guía interna.";
            if (string.IsNullOrWhiteSpace(motivo)) return "Debe ingresar el motivo de anulación.";
            return _datos.Anular(idGuiaInterna, string.IsNullOrWhiteSpace(usuario) ? "Sistema" : usuario.Trim(), motivo.Trim());
        }
    }
}
