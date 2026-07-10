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
        public decimal CantidadDisponibleParaEnviar => Math.Max(0, Math.Min(StockActual, CantidadPendiente));

        public decimal CantidadFaltanteParaEnviar => Math.Max(0, CantidadPendiente - StockActual);

        public decimal CantidadTomarDeReserva => Math.Min(CantidadFaltanteParaEnviar, StockProcesoReservado);

        public decimal StockProcesoDisponibleRestante => Math.Max(0, StockProcesoReservado - CantidadTomarDeReserva);

        public bool DeficitCubiertoConReserva => CantidadFaltanteParaEnviar > 0
            && CantidadTomarDeReserva >= CantidadFaltanteParaEnviar;

        public bool TieneStockProcesoReservado => StockProcesoReservado > 0;
        public string StockProcesoReservadoTooltip => TieneStockProcesoReservado
            ? $"Reservado en proceso: {StockProcesoReservado:N2}\n{StockProcesoReservadoDetalle}"
            : "Sin stock en proceso reservado";

        public string DisponibilidadTexto => $"{FormatearCantidad(StockActual)} / {FormatearCantidad(Cantidad)} disponibles";
        public string StockDisponibleTexto => $"Stock disponible: {FormatearCantidad(StockActual)} Und";
        public string FaltanteVisualTexto => $"Faltan producir/despachar: {FormatearCantidad(CantidadFaltanteParaEnviar)} Und";
        public string CantidadVisualTexto => $"{FormatearCantidad(Cantidad)} Und";
        public string ObservacionVisual => string.IsNullOrWhiteSpace(Observacion) ? "Sin observaciones." : Observacion.Trim();

        public string EstadoEnvioStock
        {
            get
            {
                if (CantidadPendiente <= 0)
                    return "Completo";

                if (CantidadFaltanteParaEnviar <= 0)
                    return $"Completo: stock {StockActual:N2}";

                if (CantidadTomarDeReserva > 0)
                {
                    if (DeficitCubiertoConReserva)
                        return $"Déficit {CantidadFaltanteParaEnviar:N2}: tomar reserva {CantidadTomarDeReserva:N2} / queda {StockProcesoDisponibleRestante:N2}";
                    return $"Parcial: déficit {CantidadFaltanteParaEnviar:N2} / reserva {CantidadTomarDeReserva:N2} / falta {CantidadFaltanteParaEnviar - CantidadTomarDeReserva:N2}";
                }

                return $"Sin stock suficiente: déficit {CantidadFaltanteParaEnviar:N2}";
            }
        }
        public string EstadoEnvioColor
        {
            get
            {
                if (CantidadPendiente <= 0 || CantidadFaltanteParaEnviar <= 0)
                    return "#15803D";

                return CantidadTomarDeReserva > 0 ? "#B45309" : "#B91C1C";
            }
        }

        public string EstadoEnvioFondo
        {
            get
            {
                if (CantidadPendiente <= 0 || CantidadFaltanteParaEnviar <= 0)
                    return "#DCFCE7";

                return CantidadTomarDeReserva > 0 ? "#FEF3C7" : "#FEE2E2";
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

        private static string FormatearCantidad(decimal valor) =>
            decimal.Truncate(valor) == valor ? valor.ToString("N0") : valor.ToString("N2");
    }
}
