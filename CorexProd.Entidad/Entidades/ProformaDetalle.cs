using System;

namespace CorexProd.Entidad.Entidades
{
    public class ProformaDetalle
    {
        public int IdProformaDetalle { get; set; }
        public int IdProforma { get; set; }
        public int IdProducto { get; set; }
        public string CodigoProducto { get; set; } = string.Empty;
        public string NombreProducto { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; }
        public decimal Importe { get; set; }
        public string Observacion { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
    }
}
