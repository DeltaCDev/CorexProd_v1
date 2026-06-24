using System;
using System.Globalization;

namespace CorexProd.Entidad.Entidades
{
    public class KardexProductoReporte
    {
        public DateTime FechaMovimiento { get; set; }
        public string TipoMovimiento { get; set; } = string.Empty;
        public string TipoMovimientoTexto => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(
            (TipoMovimiento ?? string.Empty).Replace("_", " ").Trim().ToLowerInvariant());
        public string CodigoProducto { get; set; } = string.Empty;
        public string NombreProducto { get; set; } = string.Empty;
        public string Producto => string.IsNullOrWhiteSpace(CodigoProducto)
            ? NombreProducto
            : $"{CodigoProducto} - {NombreProducto}";
        public string Almacen { get; set; } = string.Empty;
        public string UnidadMedida { get; set; } = string.Empty;
        public decimal Entrada { get; set; }
        public decimal Salida { get; set; }
        public decimal Devolucion { get; set; }
        public decimal Stock { get; set; }
        public decimal CostoUnitario { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string Observacion { get; set; } = string.Empty;
    }
}
