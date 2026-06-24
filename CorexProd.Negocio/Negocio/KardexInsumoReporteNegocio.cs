using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System;
using System.Collections.Generic;

namespace CorexProd.Negocio.Negocio
{
    public class KardexInsumoReporteNegocio
    {
        private readonly KardexInsumoReporteDatos _datos = new();

        public List<KardexInsumoReporte> Listar(DateTime? desde, DateTime? hasta, int? idAlmacen, int? idInsumo, string tipoMovimiento, int? limite)
            => _datos.Listar(desde, hasta, idAlmacen, idInsumo, tipoMovimiento, limite);

        public List<string> ListarTiposMovimiento() => _datos.ListarTiposMovimiento();

        public List<AlmacenStock> ListarAlmacenes() => _datos.ListarAlmacenes();
    }
}
