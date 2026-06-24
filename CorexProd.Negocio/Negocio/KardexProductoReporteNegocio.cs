using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System;
using System.Collections.Generic;

namespace CorexProd.Negocio.Negocio
{
    public class KardexProductoReporteNegocio
    {
        private readonly KardexProductoReporteDatos _datos = new();

        public List<KardexProductoReporte> Listar(DateTime? desde, DateTime? hasta, int? idAlmacen, int? idProducto, string tipoMovimiento, int? limite)
            => _datos.Listar(desde, hasta, idAlmacen, idProducto, tipoMovimiento, limite);

        public List<string> ListarTiposMovimiento() => _datos.ListarTiposMovimiento();

        public List<AlmacenStock> ListarAlmacenes() => _datos.ListarAlmacenes();
    }
}
