using System;

namespace CorexProd.Entidad.Entidades
{
    public class ProgramacionPagoGeneralItem
    {
        public DateTime FechaVencimiento { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public string Subcategoria { get; set; } = string.Empty;
        public string EntidadProveedor { get; set; } = string.Empty;
        public string Titular { get; set; } = string.Empty;
        public string EmpresaResponsable { get; set; } = string.Empty;
        public string Concepto { get; set; } = string.Empty;
        public string Referencia { get; set; } = string.Empty;
        public DateTime? FechaDocumento { get; set; }
        public int? NumeroCuota { get; set; }
        public int? TotalCuotas { get; set; }
        public string Moneda { get; set; } = string.Empty;
        public decimal Importe { get; set; }
        public decimal Pagado { get; set; }
        public decimal Saldo { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string OrigenTipo { get; set; } = string.Empty;
        public int OrigenId { get; set; }
        public int OrigenDetalleId { get; set; }
        public bool EsPendienteAnterior { get; set; }
    }
}
