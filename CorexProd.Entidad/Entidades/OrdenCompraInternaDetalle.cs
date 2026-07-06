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
        public decimal StockProcesoReservado { get; set; }
        public string StockProcesoReservadoDetalle { get; set; } = string.Empty;
        public decimal CantidadDespachada { get; set; }
        public decimal CantidadPendiente => Math.Max(0, Cantidad - CantidadDespachada);
        public decimal CoberturaStock => Math.Max(0, StockActual + StockProcesoReservado);
        public decimal CantidadDisponibleParaEnviar => Math.Max(0, Math.Min(StockActual, CantidadPendiente));
        public decimal CantidadFaltanteParaEnviar => Math.Max(0, CantidadPendiente - CoberturaStock);
        public bool TieneStockProcesoReservado => StockProcesoReservado > 0;
        public string StockProcesoReservadoTooltip => TieneStockProcesoReservado
            ? $"Reservado en proceso: {StockProcesoReservado:N2}\n{StockProcesoReservadoDetalle}"
            : "Sin stock en proceso reservado";
        public string EstadoEnvioStock
        {
            get
            {
                if (CantidadPendiente <= 0)
                    return "Completo";

                if (CoberturaStock <= 0)
                    return "Sin stock: no puede enviar";

                if (CantidadFaltanteParaEnviar > 0)
                    return $"Parcial: puede enviar {CantidadDisponibleParaEnviar:N2} / falta {CantidadFaltanteParaEnviar:N2}";

                if (StockActual < CantidadPendiente)
                    return $"Cubierto: stock {StockActual:N2} / proceso {StockProcesoReservado:N2}";

                return $"Completo: puede enviar {CantidadPendiente:N2}";
            }
        }
        public string EstadoEnvioColor
        {
            get
            {
                if (CantidadPendiente <= 0 || CantidadFaltanteParaEnviar <= 0)
                    return "#15803D";

                return CoberturaStock > 0 ? "#B45309" : "#B91C1C";
            }
        }
        public string EstadoEnvioFondo
        {
            get
            {
                if (CantidadPendiente <= 0 || CantidadFaltanteParaEnviar <= 0)
                    return "#DCFCE7";

                return CoberturaStock > 0 ? "#FEF3C7" : "#FEE2E2";
            }
        }
        public string EstadoItem => CantidadDespachada <= 0
            ? "Pendiente"
            : CantidadDespachada < Cantidad
                ? "Despachado parcialmente"
                : "Despachado completo";
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; }
        public decimal Importe { get; set; }
        public string Observacion { get; set; } = string.Empty;
        public bool Seleccionado { get; set; }
        public decimal CantidadPlanificada { get; set; }
    }
}
