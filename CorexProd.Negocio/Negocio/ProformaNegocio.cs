using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CorexProd.Negocio.Negocio
{
    public class ProformaNegocio
    {
        private readonly ProformaDatos _proformaDatos = new();

        public List<Proforma> Listar()
        {
            return _proformaDatos.Listar();
        }

        public Proforma? Obtener(int idProforma)
        {
            if (idProforma <= 0)
            {
                return null;
            }

            return _proformaDatos.Obtener(idProforma);
        }

        public string ObtenerSiguienteSerieNumero()
        {
            return _proformaDatos.ObtenerSiguienteSerieNumero();
        }

        public string Guardar(Proforma proforma)
        {
            proforma.OrdenCompraCliente = proforma.OrdenCompraCliente.Trim();
            proforma.Observacion = proforma.Observacion.Trim();
            proforma.UsuarioGenerador = proforma.UsuarioGenerador.Trim();

            if (proforma.FechaEmision == DateTime.MinValue)
                return "La fecha de emision es obligatoria";

            if (proforma.FechaVencimiento == DateTime.MinValue)
                return "La fecha de vencimiento es obligatoria";

            if (proforma.FechaVencimiento.Date < proforma.FechaEmision.Date)
                return "La fecha de vencimiento no puede ser menor a la fecha de emision";

            if (proforma.IdCliente <= 0)
                return "Debe seleccionar un cliente";

            if (string.IsNullOrWhiteSpace(proforma.UsuarioGenerador))
                proforma.UsuarioGenerador = "Sistema";

            if (proforma.Detalles.Count == 0)
                return "Debe agregar al menos un producto";

            foreach (ProformaDetalle detalle in proforma.Detalles)
            {
                detalle.Observacion = detalle.Observacion.Trim();

                if (detalle.IdProducto <= 0)
                    return "Todas las filas deben tener un producto";

                if (detalle.Cantidad <= 0)
                    return "La cantidad debe ser mayor a cero";

                if (detalle.PrecioUnitario < 0 || detalle.Descuento < 0)
                    return "Precio unitario y descuento no pueden ser negativos";

                detalle.Importe = Math.Max(0, (detalle.Cantidad * detalle.PrecioUnitario) - detalle.Descuento);
            }

            proforma.Subtotal = proforma.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario);
            proforma.Descuento = proforma.Detalles.Sum(d => d.Descuento);
            proforma.Igv = 0;
            proforma.Total = proforma.Detalles.Sum(d => d.Importe);

            return _proformaDatos.Guardar(proforma);
        }

        public string Anular(int idProforma, string motivoAnulacion, string usuarioAnulacion)
        {
            motivoAnulacion = motivoAnulacion.Trim();
            usuarioAnulacion = usuarioAnulacion.Trim();

            if (idProforma <= 0)
                return "Debe seleccionar una proforma valida";

            if (string.IsNullOrWhiteSpace(motivoAnulacion))
                return "Debe ingresar el motivo de anulacion";

            if (string.IsNullOrWhiteSpace(usuarioAnulacion))
                return "No se pudo identificar al usuario de la sesion";

            Proforma? proforma = _proformaDatos.Obtener(idProforma);

            if (proforma == null)
                return "No se encontro la proforma";

            if (proforma.Estado.Equals("Anulado", StringComparison.OrdinalIgnoreCase))
                return "La proforma ya se encuentra anulada";

            return _proformaDatos.Anular(idProforma, motivoAnulacion, usuarioAnulacion);
        }
    }
}
