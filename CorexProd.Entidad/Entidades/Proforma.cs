using System;
using System.Collections.Generic;

namespace CorexProd.Entidad.Entidades
{
    public class Proforma
    {
        public int IdProforma { get; set; }
        public string SerieNumero { get; set; } = string.Empty;
        public DateTime FechaEmision { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public string OrdenCompraCliente { get; set; } = string.Empty;
        public int IdCliente { get; set; }
        public string NombreCliente { get; set; } = string.Empty;
        public string Observacion { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Igv { get; set; }
        public decimal Total { get; set; }
        public string Estado { get; set; } = "Emitido";
        public bool TieneOrdenCompraInterna { get; set; }
        public string UsuarioGenerador { get; set; } = string.Empty;
        public string MotivoAnulacion { get; set; } = string.Empty;
        public string UsuarioAnulacion { get; set; } = string.Empty;
        public DateTime? FechaAnulacion { get; set; }
        public string DetalleAnulacion =>
            $"Motivo: {TextoOmitido(MotivoAnulacion)}\nUsuario: {TextoOmitido(UsuarioAnulacion)}\nFecha: {(FechaAnulacion.HasValue ? FechaAnulacion.Value.ToString("dd/MM/yyyy HH:mm") : "No registrada")}";
        public DateTime FechaRegistro { get; set; }
        public List<ProformaDetalle> Detalles { get; set; } = [];

        private static string TextoOmitido(string valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? "No registrado" : valor;
        }
    }
}
