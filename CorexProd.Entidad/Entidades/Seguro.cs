using System;
using System.Collections.Generic;

namespace CorexProd.Entidad.Entidades
{
    public class Seguro
    {
        public int IdSeguro { get; set; }
        public int IdObligacionFinanciera { get; set; }
        public string TipoSeguro { get; set; } = string.Empty;
        public string NumeroPoliza { get; set; } = string.Empty;
        public DateTime FechaInicioCobertura { get; set; } = DateTime.Today;
        public DateTime FechaFinCobertura { get; set; } = DateTime.Today;
        public string Periodicidad { get; set; } = "UNICO";
        public ObligacionFinanciera Obligacion { get; set; } = new();
        public List<SeguroPeriodo> Periodos { get; set; } = [];
        public List<ObligacionFinancieraPago> Pagos { get; set; } = [];
        public List<ObligacionFinancieraHistorial> Historial { get; set; } = [];
        public bool EstaAnulado => Obligacion.Estado.Equals("ANULADA", StringComparison.OrdinalIgnoreCase);
        public bool EstaCancelado => Obligacion.Estado.Equals("CANCELADA", StringComparison.OrdinalIgnoreCase);
    }

    public class SeguroResumen
    {
        public int IdSeguro { get; set; }
        public int IdObligacionFinanciera { get; set; }
        public string TipoSeguro { get; set; } = string.Empty;
        public string NumeroPoliza { get; set; } = string.Empty;
        public string Aseguradora { get; set; } = string.Empty;
        public string Titular { get; set; } = string.Empty;
        public string EmpresaResponsable { get; set; } = string.Empty;
        public string Concepto { get; set; } = string.Empty;
        public string Moneda { get; set; } = string.Empty;
        public decimal? MontoOriginal { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaInicioCobertura { get; set; }
        public DateTime FechaFinCobertura { get; set; }
        public string Periodicidad { get; set; } = string.Empty;
        public int TotalCuotas { get; set; }
        public decimal TotalPagado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string Observacion { get; set; } = string.Empty;
    }

    public class SeguroPeriodo
    {
        public int IdCuotaObligacionFinanciera { get; set; }
        public int IdObligacionFinanciera { get; set; }
        public int NumeroCuota { get; set; }
        public int TotalCuotas { get; set; }
        public bool EsCuota { get; set; }
        public DateTime FechaVencimiento { get; set; } = DateTime.Today;
        public decimal Importe { get; set; }
        public decimal TotalPagado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public string Estado { get; set; } = "PENDIENTE";
        public string Observacion { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public string CuotaTexto => EsCuota ? $"{NumeroCuota} de {TotalCuotas}" : string.Empty;
        public bool EstaAnulado => Estado.Equals("ANULADA", StringComparison.OrdinalIgnoreCase);
    }

    public class SeguroResultado
    {
        public int IdSeguro { get; set; }
        public int IdObligacionFinanciera { get; set; }
        public bool Resultado { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }

    public class SeguroPeriodoResultado
    {
        public int IdCuotaObligacionFinanciera { get; set; }
        public bool Resultado { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }
}
