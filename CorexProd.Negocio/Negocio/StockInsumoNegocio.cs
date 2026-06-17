using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System.Collections.Generic;

namespace CorexProd.Negocio.Negocio
{
    public class StockInsumoNegocio
    {
        private readonly StockInsumoDatos _stockInsumoDatos = new();

        public List<StockInsumo> Listar()
        {
            return _stockInsumoDatos.Listar();
        }
    }
}
