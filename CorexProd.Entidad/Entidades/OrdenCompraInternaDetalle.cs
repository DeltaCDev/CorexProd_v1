namespace CorexProd.Entidad.Entidades
{
    public class OrdenCompraInternaDetalle
    {
        public int IdOrdenCompraInternaDetalle { get; set; }
        public int IdOrdenCompraInterna { get; set; }
        public int IdProducto { get; set; }
        public string CodigoProducto { get; set; } = string.Empty;
        public string NombreProducto { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal StockActual { get; set; }
        public decimal CantidadDespachada { get; set; }
        public decimal CantidadPendiente => Math.Max(0, Cantidad - CantidadDespachada);
        public string EstadoItem => CantidadDespachada <= 0
            ? "Pendiente"
            : CantidadDespachada < Cantidad
                ? "Despachado parcialmente"
                : "Despachado completo";
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; }
        public decimal Importe { get; set; }
        public string Observacion { get; set; } = string.Empty;
    }
}
