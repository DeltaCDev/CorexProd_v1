using System;
using System.Collections.Generic;
using System.Linq;

namespace CorexProd.Entidad.Entidades
{
    public class OrdenCompraInterna
    {
        public int IdOrdenCompraInterna { get; set; }
        public string NumeroOci { get; set; } = string.Empty;
        public DateTime FechaEmision { get; set; }
        public DateTime FechaEntrega { get; set; }
        public string OrdenCompraCliente { get; set; } = string.Empty;
        public int IdCliente { get; set; }
        public string NombreCliente { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Igv { get; set; }
        public decimal IgvPorcentaje { get; set; }
        public string CondicionTributaria { get; set; } = string.Empty;
        public string Observacion { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string UsuarioGenerador { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public string MotivoAnulacion { get; set; } = string.Empty;
        public string UsuarioAnulacion { get; set; } = string.Empty;
        public DateTime? FechaAnulacion { get; set; }

        public string FechaEmisionVisual => FechaEmision.ToString("dd/MM/yyyy");
        public string FechaEntregaVisual => FechaEntrega == default ? "-" : FechaEntrega.ToString("dd/MM/yyyy");
        public string CantidadProductosTexto => $"Productos ({Detalles.Count})";
        public string SubtotalVisual => $"S/ {Subtotal:N2}";
        public string DescuentoVisual => $"S/ {Descuento:N2}";
        public string IgvVisual => $"S/ {Igv:N2}";
        public string IgvEtiqueta => $"IGV ({IgvPorcentaje:N0}%)";
        public string TotalVisual => $"S/ {Total:N2}";

        public string DetalleAnulacion =>
            $"Motivo: {TextoOmitido(MotivoAnulacion)}\nFecha y Hora: {(FechaAnulacion.HasValue ? FechaAnulacion.Value.ToString("dd/MM/yyyy HH:mm") : "No registrada")}\nUsuario: {TextoOmitido(UsuarioAnulacion)}";
        public bool TieneGuiaSalida { get; set; }
        public bool TieneOrdenTrabajo { get; set; }
        public bool PuedeGenerarOt { get; set; }
        public bool PuedeGenerarGuiaSalida { get; set; }
        public decimal CantidadPendienteDespacho => Detalles.Sum(d => d.CantidadPendiente);
        public decimal StockDisponibleDespacho => Detalles.Sum(d => d.CantidadDisponibleParaEnviar);
        public bool EstaAnulada => Estado.Trim().ToUpperInvariant() is "ANULADO" or "ANULADA";
        public bool EstaDespachadaCompleta => Detalles.Count > 0 && CantidadPendienteDespacho <= 0;
        public bool TieneStockDisponibleDespacho => Detalles.Any(d => d.CantidadDisponibleParaEnviar > 0);
        public bool TieneStockDespachoCompleto => CantidadPendienteDespacho > 0 && StockDisponibleDespacho >= CantidadPendienteDespacho;
        public bool TieneStockDespachoParcial => CantidadPendienteDespacho > 0 && StockDisponibleDespacho > 0 && StockDisponibleDespacho < CantidadPendienteDespacho;
        public string GuiaSalidaEstadoTexto => EstaAnulada
            ? string.Empty
            : EstaDespachadaCompleta
                ? "Despachado"
                : TieneStockDespachoCompleto
                    ? "Completo"
                    : TieneStockDespachoParcial
                        ? "Parcial"
                        : "Sin Stock";
        public string GuiaSalidaTexto => EstaAnulada ? "Guia Interna" : $"Guia Interna\n({GuiaSalidaEstadoTexto})";
        public string GuiaSalidaFondo => EstaAnulada || EstaDespachadaCompleta || !TieneStockDisponibleDespacho ? "#8B95A1" : TieneStockDespachoCompleto ? "#0F766E" : "#F97316";
        public string GuiaSalidaBorde => EstaAnulada || EstaDespachadaCompleta || !TieneStockDisponibleDespacho ? "#8B95A1" : TieneStockDespachoCompleto ? "#0F766E" : "#F97316";
        public string GuiaSalidaHoverFondo => EstaAnulada || EstaDespachadaCompleta || !TieneStockDisponibleDespacho ? "#8B95A1" : TieneStockDespachoCompleto ? "#0D9488" : "#EA580C";
        public string GuiaSalidaPressedFondo => EstaAnulada || EstaDespachadaCompleta || !TieneStockDisponibleDespacho ? "#8B95A1" : TieneStockDespachoCompleto ? "#115E59" : "#C2410C";
        public string GuiaSalidaToolTip => EstaAnulada ? "Guia Interna" : $"Guia Interna ({GuiaSalidaEstadoTexto})";
        public string EstadoListadoTexto => EstaAnulada
            ? "Anulado"
            : EstaDespachadaCompleta
            ? "Entregado"
            : TieneStockDespachoCompleto || TieneStockDespachoParcial
                ? "En Proceso"
                : Estado;
        public string EstadoListadoFondo => EstadoListadoTexto.Trim().ToUpperInvariant() switch
        {
            "ENTREGADO" or "ENTREGADA" => "#DCFCE7",
            "EN PROCESO" or "PROCESO" => "#FEF3C7",
            "PARCIAL" => "#FFEDD5",
            "ANULADO" or "ANULADA" => "#FEE2E2",
            _ => "#DBEAFE"
        };
        public string EstadoListadoColor => EstadoListadoTexto.Trim().ToUpperInvariant() switch
        {
            "ENTREGADO" or "ENTREGADA" => "#166534",
            "EN PROCESO" or "PROCESO" => "#92400E",
            "PARCIAL" => "#C2410C",
            "ANULADO" or "ANULADA" => "#B91C1C",
            _ => "#1D4ED8"
        };
        public bool PuedeEditar => Estado.Trim().ToUpperInvariant() is "PENDIENTE" or "EMITIDA" or "EMITIDO"
            && !TieneOrdenTrabajo
            && string.IsNullOrWhiteSpace(MotivoAnulacion)
            && !FechaAnulacion.HasValue
            && Detalles.All(d => d.CantidadDespachada <= 0);
        public bool PuedeAnular => Estado.Trim().ToUpperInvariant() is not ("ANULADO" or "ANULADA")
            && !TieneGuiaSalida
            && !TieneOrdenTrabajo;
        public List<OrdenCompraInternaDetalle> Detalles { get; set; } = [];

        private static string TextoOmitido(string valor) =>
            string.IsNullOrWhiteSpace(valor) ? "No registrado" : valor;
    }
}
