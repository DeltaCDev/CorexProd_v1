using System;
using System.Collections.Generic;

namespace CorexProd.Entidad.Entidades
{
    public class TipoObligacionFinanciera
    {
        public int IdTipoObligacionFinanciera { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
        public string EstadoTexto => Activo ? "Activo" : "Inactivo";
    }

    public class EntidadFinanciera
    {
        public int IdEntidadFinanciera { get; set; }
        public string TipoEntidad { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Ruc { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
        public string Observacion { get; set; } = string.Empty;
        public string EntidadBusqueda => string.IsNullOrWhiteSpace(TipoEntidad) ? Nombre : $"{TipoEntidad} - {Nombre}";
    }

    public class ObligacionFinanciera
    {
        public int IdObligacionFinanciera { get; set; }
        public int IdTipoObligacionFinanciera { get; set; }
        public string CodigoTipoObligacionFinanciera { get; set; } = "PRESTAMO";
        public string TipoObligacionFinanciera { get; set; } = string.Empty;
        public int IdEntidadFinanciera { get; set; }
        public string TipoEntidad { get; set; } = string.Empty;
        public string EntidadFinanciera { get; set; } = string.Empty;
        public string TipoTitular { get; set; } = "EMPRESA";
        public int? IdTitular { get; set; }
        public string NombreTitular { get; set; } = string.Empty;
        public int? IdEmpresaResponsable { get; set; }
        public string EmpresaResponsable { get; set; } = string.Empty;
        public string CodigoReferencia { get; set; } = string.Empty;
        public string NumeroContrato { get; set; } = string.Empty;
        public string NumeroOperacion { get; set; } = string.Empty;
        public string Concepto { get; set; } = string.Empty;
        public string Moneda { get; set; } = "PEN";
        public decimal? MontoOriginal { get; set; }
        public DateTime FechaInicio { get; set; } = DateTime.Today;
        public int TotalCuotas { get; set; }
        public decimal TotalCronograma { get; set; }
        public decimal TotalPagado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public string Estado { get; set; } = "PENDIENTE";
        public string OrigenTipo { get; set; } = "MANUAL";
        public int? OrigenId { get; set; }
        public string Observacion { get; set; } = string.Empty;
        public string UsuarioRegistro { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public string UsuarioModificacion { get; set; } = string.Empty;
        public DateTime? FechaModificacion { get; set; }
        public string UsuarioAnulacion { get; set; } = string.Empty;
        public DateTime? FechaAnulacion { get; set; }
        public string MotivoAnulacion { get; set; } = string.Empty;
        public List<ObligacionFinancieraCuota> Cuotas { get; set; } = [];
        public List<ObligacionFinancieraPago> Pagos { get; set; } = [];
        public List<ObligacionFinancieraHistorial> Historial { get; set; } = [];
        public bool EstaAnulada => Estado.Equals("ANULADA", StringComparison.OrdinalIgnoreCase);
        public bool EstaCancelada => Estado.Equals("CANCELADA", StringComparison.OrdinalIgnoreCase);
    }

    public class ObligacionFinancieraResumen
    {
        public int IdObligacionFinanciera { get; set; }
        public int IdTipoObligacionFinanciera { get; set; }
        public string CodigoTipoObligacionFinanciera { get; set; } = string.Empty;
        public string TipoObligacionFinanciera { get; set; } = string.Empty;
        public int IdEntidadFinanciera { get; set; }
        public string TipoEntidad { get; set; } = string.Empty;
        public string EntidadFinanciera { get; set; } = string.Empty;
        public string TipoTitular { get; set; } = string.Empty;
        public int? IdTitular { get; set; }
        public string NombreTitular { get; set; } = string.Empty;
        public int? IdEmpresaResponsable { get; set; }
        public string EmpresaResponsable { get; set; } = string.Empty;
        public string CodigoReferencia { get; set; } = string.Empty;
        public string NumeroContrato { get; set; } = string.Empty;
        public string NumeroOperacion { get; set; } = string.Empty;
        public string Concepto { get; set; } = string.Empty;
        public string Moneda { get; set; } = string.Empty;
        public decimal? MontoOriginal { get; set; }
        public DateTime FechaInicio { get; set; }
        public int TotalCuotas { get; set; }
        public decimal TotalCronograma { get; set; }
        public decimal TotalPagado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public int? ProximaCuota { get; set; }
        public DateTime? ProximoVencimiento { get; set; }
        public decimal? ImportePendienteProximaCuota { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string Observacion { get; set; } = string.Empty;
        public string UsuarioRegistro { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
    }

    public class ObligacionFinancieraDetalle
    {
        public ObligacionFinanciera? Cabecera { get; set; }
        public List<ObligacionFinancieraCuota> Cuotas { get; set; } = [];
        public List<ObligacionFinancieraPago> Pagos { get; set; } = [];
        public List<ObligacionFinancieraHistorial> Historial { get; set; } = [];
    }

    public class ObligacionFinancieraCuota
    {
        public int IdCuotaObligacionFinanciera { get; set; }
        public int IdObligacionFinanciera { get; set; }
        public int NumeroCuota { get; set; }
        public int TotalCuotas { get; set; }
        public DateTime FechaVencimiento { get; set; } = DateTime.Today;
        public decimal Importe { get; set; }
        public decimal TotalPagado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public string Estado { get; set; } = "PENDIENTE";
        public string Observacion { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public bool TieneMovimiento => TotalPagado > 0 || Estado.Equals("CANCELADA", StringComparison.OrdinalIgnoreCase);
        public string ReferenciaCuota => $"Cuota {NumeroCuota}/{TotalCuotas}";
    }

    public class ObligacionFinancieraPago
    {
        public int IdPago { get; set; }
        public int IdCuota { get; set; }
        public int IdObligacionFinanciera { get; set; }
        public int NumeroCuota { get; set; }
        public DateTime FechaPago { get; set; } = DateTime.Today;
        public decimal Monto { get; set; }
        public int? IdBanco { get; set; }
        public string Banco { get; set; } = string.Empty;
        public int? IdCuentaBancaria { get; set; }
        public string NumeroCuenta { get; set; } = string.Empty;
        public string CuentaBancariaDescripcion => string.IsNullOrWhiteSpace(NumeroCuenta) ? string.Empty : NumeroCuenta;
        public string NumeroOperacion { get; set; } = string.Empty;
        public string Observacion { get; set; } = string.Empty;
        public string Estado { get; set; } = "ACTIVO";
        public string UsuarioRegistro { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public string UsuarioAnulacion { get; set; } = string.Empty;
        public DateTime? FechaAnulacion { get; set; }
        public string MotivoAnulacion { get; set; } = string.Empty;
        public bool EstaAnulado => Estado.Equals("ANULADO", StringComparison.OrdinalIgnoreCase);
    }

    public class ObligacionFinancieraHistorial
    {
        public long IdHistorialObligacionFinanciera { get; set; }
        public int IdObligacionFinanciera { get; set; }
        public int? IdCuotaObligacionFinanciera { get; set; }
        public int? IdPagoObligacionFinanciera { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty;
        public string EstadoAnterior { get; set; } = string.Empty;
        public string EstadoNuevo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }
    }

    public class ObligacionFinancieraResultado
    {
        public int IdObligacionFinanciera { get; set; }
        public bool Resultado { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public decimal TotalCronograma { get; set; }
        public decimal TotalPagado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public string EstadoObligacionFinanciera { get; set; } = string.Empty;
    }

    public class ObligacionFinancieraPagoResultado
    {
        public int IdPago { get; set; }
        public bool Resultado { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public decimal TotalPagado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public string EstadoCuota { get; set; } = string.Empty;
        public string EstadoObligacionFinanciera { get; set; } = string.Empty;
    }
}
