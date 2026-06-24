using CorexProd.Datos.Datos;
using System;
using System.Data;

namespace CorexProd.Negocio.Negocio
{
    public class ReporteComercialNegocio
    {
        private readonly ReporteComercialDatos _datos = new();

        public DataTable ProductosMasDespachados(DateTime? desde, DateTime? hasta) => _datos.ProductosMasDespachados(desde, hasta);
        public DataTable Clientes(DateTime? desde, DateTime? hasta) => _datos.Clientes(desde, hasta);
        public DataTable OciDespachadas(DateTime? desde, DateTime? hasta) => _datos.OciDespachadas(desde, hasta);
        public DataTable OciDespachadaDetalle(int idOrdenCompraInterna) => _datos.OciDespachadaDetalle(idOrdenCompraInterna);
        public DataTable UsuariosConMasProformas(DateTime? desde, DateTime? hasta) => _datos.UsuariosConMasProformas(desde, hasta);
    }
}
