using System;
using System.Collections.Generic;
using System.Linq;

namespace CorexProd.Entidad.Entidades
{
    public class TipoObligacion
    {
        public int IdTipoObligacion { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public bool Estado { get; set; } = true;
        public DateTime FechaRegistro { get; set; }
        public string EstadoTexto => Estado ? "Activo" : "Inactivo";
    }

    public class CuentaPorPagar
    {
        public int IdCuentaPorPagar { get; set; }
        public int IdProveedor { get; set; }
        public string TipoDocumentoProveedor { get; set; } = string.Empty;
        public string NumeroDocumentoProveedor { get; set; } = string.Empty;
        public string NombreProveedor { get; set; } = string.Empty;
        public int IdTipoObligacion { get; set; }
        public string CodigoTipoObligacion { get; set; } = string.Empty;
        public string TipoObligacion { get; set; } = string.Empty;
        public DateTime FechaDocumento { get; set; } = DateTime.Today;
        public string Moneda { get; set; } = "PEN";
        public decimal ImporteTotal { get; set; }
        public decimal TotalPagado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public decimal TotalDocumentosPositivos => CalcularTotalDocumentos(1);
        public decimal TotalNotasCredito => CalcularTotalDocumentos(-1);
        public decimal TotalNetoDocumental => Math.Round(TotalDocumentosPositivos - TotalNotasCredito, 2);
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
        public List<CuentaPorPagarDocumento> Documentos { get; set; } = [];
        public List<CuentaPorPagarCuota> Cuotas { get; set; } = [];
        public List<CuentaPorPagarPago> Pagos { get; set; } = [];
        public List<CuentaPorPagarHistorial> Historial { get; set; } = [];
        public bool EstaAnulada => Estado.Equals("ANULADA", StringComparison.OrdinalIgnoreCase);
        public bool EsFacturaCredito => CodigoTipoObligacion.Equals("FACTURA_CREDITO", StringComparison.OrdinalIgnoreCase);

        private decimal CalcularTotalDocumentos(short factor)
        {
            return Math.Round(Documentos.Where(d => d.FactorEfecto == factor && d.Estado != "ANULADO").Sum(d => d.Importe), 2);
        }
    }

    public class CuentaPorPagarDocumento
    {
        public int IdCuentaPorPagarDocumento { get; set; }
        public int IdCuentaPorPagar { get; set; }
        public int IdTipoDocumento { get; set; }
        public string NombreTipoDocumento { get; set; } = string.Empty;
        public string Serie { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string NumeroDocumento { get; set; } = string.Empty;
        public DateTime FechaEmision { get; set; } = DateTime.Today;
        public decimal Importe { get; set; }
        public short FactorEfecto { get; set; } = 1;
        public string Observacion { get; set; } = string.Empty;
        public string Estado { get; set; } = "ACTIVO";
        public bool EsNotaCredito => FactorEfecto < 0;
        public decimal ImporteConEfecto => Importe * FactorEfecto;
        public string EfectoTexto => FactorEfecto < 0 ? $"- {Importe:N2}" : $"+ {Importe:N2}";
    }

    public class CuentaPorPagarCuota
    {
        public int IdCuota { get; set; }
        public int IdCuentaPorPagar { get; set; }
        public int NumeroCuota { get; set; }
        public int TotalCuotas { get; set; }
        public string NumeroLetra { get; set; } = string.Empty;
        public string TipoCuota { get; set; } = "LETRA";
        public DateTime? FechaGiro { get; set; } = DateTime.Today;
        public DateTime FechaVencimiento { get; set; } = DateTime.Today;
        public decimal Importe { get; set; }
        public decimal TotalPagado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public string Estado { get; set; } = "PENDIENTE";
        public string Observacion { get; set; } = string.Empty;
        public bool EsCuotaFactura => TipoCuota.Equals("CUOTA_FACTURA", StringComparison.OrdinalIgnoreCase);
        public string ReferenciaCuota => string.IsNullOrWhiteSpace(NumeroLetra) ? $"Cuota factura {NumeroCuota}/{TotalCuotas}" : NumeroLetra;
    }

    public class CuentaPorPagarListado
    {
        public int IdCuentaPorPagar { get; set; }
        public int IdProveedor { get; set; }
        public string NombreProveedor { get; set; } = string.Empty;
        public string NumeroDocumentoProveedor { get; set; } = string.Empty;
        public int IdTipoObligacion { get; set; }
        public string TipoObligacion { get; set; } = string.Empty;
        public DateTime FechaDocumento { get; set; }
        public string Moneda { get; set; } = string.Empty;
        public decimal ImporteTotal { get; set; }
        public decimal TotalPagado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string OrigenTipo { get; set; } = string.Empty;
        public int? OrigenId { get; set; }
        public string Observacion { get; set; } = string.Empty;
        public DateTime? ProximoVencimiento { get; set; }
        public string UsuarioRegistro { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
    }

    public class CuentaPorPagarProgramacion
    {
        public int IdCuota { get; set; }
        public int IdCuentaPorPagar { get; set; }
        public int IdProveedor { get; set; }
        public string NombreProveedor { get; set; } = string.Empty;
        public string NumeroDocumentoProveedor { get; set; } = string.Empty;
        public int IdTipoObligacion { get; set; }
        public string TipoObligacion { get; set; } = string.Empty;
        public string Moneda { get; set; } = string.Empty;
        public DateTime FechaDocumento { get; set; }
        public int NumeroCuota { get; set; }
        public int TotalCuotas { get; set; }
        public string NumeroLetra { get; set; } = string.Empty;
        public string TipoCuota { get; set; } = "LETRA";
        public string DocumentoPrincipal { get; set; } = string.Empty;
        public DateTime FechaGiro { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public decimal Importe { get; set; }
        public decimal TotalPagado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string OrigenTipo { get; set; } = string.Empty;
        public int? OrigenId { get; set; }
        public string Observacion { get; set; } = string.Empty;
        public string ReferenciaProgramacion => string.IsNullOrWhiteSpace(NumeroLetra)
            ? string.IsNullOrWhiteSpace(DocumentoPrincipal) ? $"Cuota factura {NumeroCuota}/{TotalCuotas}" : DocumentoPrincipal
            : NumeroLetra;
    }

    public class CuentaPorPagarPago
    {
        public int IdPago { get; set; }
        public int IdCuota { get; set; }
        public int IdCuentaPorPagar { get; set; }
        public int NumeroCuota { get; set; }
        public string NumeroLetra { get; set; } = string.Empty;
        public DateTime FechaPago { get; set; } = DateTime.Today;
        public decimal Importe { get; set; }
        public string MedioPago { get; set; } = string.Empty;
        public int? IdBanco { get; set; }
        public int? IdCuentaBancaria { get; set; }
        public string Banco { get; set; } = string.Empty;
        public string NumeroCuenta { get; set; } = string.Empty;
        public string NumeroOperacion { get; set; } = string.Empty;
        public string Observacion { get; set; } = string.Empty;
        public string Estado { get; set; } = "ACTIVO";
        public string UsuarioRegistro { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public string UsuarioAnulacion { get; set; } = string.Empty;
        public DateTime? FechaAnulacion { get; set; }
        public string MotivoAnulacion { get; set; } = string.Empty;
        public string ReferenciaCuota => string.IsNullOrWhiteSpace(NumeroLetra) ? $"Cuota {NumeroCuota}" : NumeroLetra;
        public bool EstaAnulado => Estado.Equals("ANULADO", StringComparison.OrdinalIgnoreCase);
    }

    public class CuentaPorPagarPagoResultado
    {
        public int IdPago { get; set; }
        public bool Resultado { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public decimal TotalPagado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public string EstadoCuota { get; set; } = string.Empty;
        public string EstadoCuentaPorPagar { get; set; } = string.Empty;
    }

    public class BancoTesoreria
    {
        public int IdBanco { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public bool Estado { get; set; } = true;
        public string BancoBusqueda => string.IsNullOrWhiteSpace(Codigo) ? Nombre : $"{Codigo} - {Nombre}";
    }

    public class CuentaBancariaTesoreria
    {
        public int IdCuentaBancaria { get; set; }
        public int IdBanco { get; set; }
        public string Banco { get; set; } = string.Empty;
        public string NombreCuenta { get; set; } = string.Empty;
        public string Titular { get; set; } = string.Empty;
        public string Moneda { get; set; } = string.Empty;
        public string NumeroCuenta { get; set; } = string.Empty;
        public string Cci { get; set; } = string.Empty;
        public bool Estado { get; set; } = true;
        public string CuentaBusqueda => $"{Banco} - {NombreCuenta} - {Moneda} {NumeroCuenta}".Trim();
    }

    public class CuentaPorPagarHistorial
    {
        public long IdCuentaPorPagarHistorial { get; set; }
        public int IdCuentaPorPagar { get; set; }
        public int? IdCuota { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty;
        public string EstadoAnterior { get; set; } = string.Empty;
        public string EstadoNuevo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }
    }

    public class CuentaPorPagarResultado
    {
        public int IdCuentaPorPagar { get; set; }
        public bool Resultado { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }
}
