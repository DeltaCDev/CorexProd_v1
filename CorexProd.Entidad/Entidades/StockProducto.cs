using System;

namespace CorexProd.Entidad.Entidades
{
    public class StockProducto
    {
        public int IdProducto { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string NombreProducto { get; set; } = string.Empty;
        public string EtiquetaCliente { get; set; } = string.Empty;
        public string ProductoBusqueda => string.IsNullOrWhiteSpace(Codigo)
            ? ProductoBusquedaBase
            : $"{Codigo} - {ProductoBusquedaBase}";
        private string ProductoBusquedaBase => string.IsNullOrWhiteSpace(EtiquetaCliente)
            ? NombreProducto
            : $"{NombreProducto} [{EtiquetaCliente}]";
        public int IdCategoriaProducto { get; set; }
        public string NombreCategoria { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
    }

    public class StockProcesoReservaReporte
    {
        public int IdProducto { get; set; }
        public string CodigoProducto { get; set; } = string.Empty;
        public string NombreProducto { get; set; } = string.Empty;
        public int IdAreaProduccion { get; set; }
        public string NombreArea { get; set; } = string.Empty;
        public decimal CantidadReservada { get; set; }
        public decimal CantidadAplicada { get; set; }
        public decimal CantidadDisponible => Math.Max(0, CantidadReservada - CantidadAplicada);
        public string Estado { get; set; } = string.Empty;
        public string NumeroOT { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
    }
}
