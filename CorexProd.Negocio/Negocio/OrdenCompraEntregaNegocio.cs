using CorexProd.Datos.Datos;
using System;
using System.Collections.Generic;

namespace CorexProd.Negocio.Negocio
{
    public sealed class OrdenCompraEntregaNegocio
    {
        private readonly OrdenCompraEntregaDatos _datos = new();

        public DateTime? ObtenerFechaEntrega(int idOrdenCompraInterna) =>
            _datos.ObtenerFechaEntrega(idOrdenCompraInterna);

        public void GuardarFechaEntrega(int idOrdenCompraInterna, string numeroOci, DateTime fechaEmision, DateTime fechaEntrega)
        {
            if (fechaEntrega.Date <= fechaEmision.Date)
                throw new InvalidOperationException("La fecha de entrega debe ser diferente y posterior a la fecha de emisión.");

            if (idOrdenCompraInterna > 0)
                _datos.ActualizarFechaEntrega(idOrdenCompraInterna, fechaEntrega);
            else
                _datos.ActualizarFechaEntregaPorNumero(numeroOci, fechaEntrega);
        }

        public List<OrdenCompraAlertaEntrega> ListarAlertas(DateTime hoy) =>
            _datos.ListarAlertas(hoy);

        public int ContarEntregadasATiempo(DateTime desde, DateTime hasta) =>
            _datos.ContarEntregadasATiempo(desde, hasta);
    }
}
