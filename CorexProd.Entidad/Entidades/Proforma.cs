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
        public string Estado { get; set; } = "Registrado";
        public bool TieneOrdenCompraInterna { get; set; }
        public DateTime FechaRegistro { get; set; }
        public List<ProformaDetalle> Detalles { get; set; } = [];
    }
}
