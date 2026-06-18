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
        public decimal Total { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string UsuarioGenerador { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public List<OrdenCompraInternaDetalle> Detalles { get; set; } = [];
    }
}
