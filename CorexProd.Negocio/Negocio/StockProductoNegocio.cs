using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System.Collections.Generic;

namespace CorexProd.Negocio.Negocio
{
    public class StockProductoNegocio
    {
        private readonly StockProductoDatos _stockProductoDatos = new();

        public List<StockProducto> Listar()
        {
            return _stockProductoDatos.Listar();
        }
    }
}
