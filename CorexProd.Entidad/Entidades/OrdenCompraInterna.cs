using System;
using System.Collections.Generic;

namespace CorexProd.Entidad.Entidades
{
    public class OrdenCompraInterna
    {
        public int IdOrdenCompraInterna { get; set; }
        public string NumeroOci { get; set; } = string.Empty;
        public int IdProforma { get; set; }
        public string NumeroProforma { get; set; } = string.Empty;
        public DateTime FechaEmision { get; set; }
        public string OrdenCompraCliente { get; set; } = string.Empty;
        public int IdCliente { get; set; }
        public string NombreCliente { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Igv { get; set; }
        public decimal IgvPorcentaje { get; set; }
        public string CondicionTributaria { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string UsuarioGenerador { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public string MotivoAnulacion { get; set; } = string.Empty;
        public string UsuarioAnulacion { get; set; } = string.Empty;
        public DateTime? FechaAnulacion { get; set; }
        public string DetalleAnulacion =>
            $"Motivo: {TextoOmitido(MotivoAnulacion)}\nUsuario: {TextoOmitido(UsuarioAnulacion)}\nFecha: {(FechaAnulacion.HasValue ? FechaAnulacion.Value.ToString("dd/MM/yyyy HH:mm") : "No registrada")}";
        public bool TieneGuiaSalida { get; set; }
        public bool TieneOrdenTrabajo { get; set; }
        public bool PuedeGenerarOt { get; set; }
        public bool PuedeGenerarGuiaSalida { get; set; }
        public bool PuedeAnular => !Estado.Equals("Anulado", StringComparison.OrdinalIgnoreCase)
            && !TieneGuiaSalida
            && !TieneOrdenTrabajo;
        public List<OrdenCompraInternaDetalle> Detalles { get; set; } = [];

        private static string TextoOmitido(string valor) =>
            string.IsNullOrWhiteSpace(valor) ? "No registrado" : valor;
    }
}
