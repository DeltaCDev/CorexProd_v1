using System;
using System.Collections.Generic;

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
        public List<CuentaPorPagarHistorial> Historial { get; set; } = [];
        public bool EstaAnulada => Estado.Equals("ANULADA", StringComparison.OrdinalIgnoreCase);
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
        public string Observacion { get; set; } = string.Empty;
        public string Estado { get; set; } = "ACTIVO";
    }

    public class CuentaPorPagarCuota
    {
        public int IdCuota { get; set; }
        public int IdCuentaPorPagar { get; set; }
        public int NumeroCuota { get; set; }
        public int TotalCuotas { get; set; }
        public string NumeroLetra { get; set; } = string.Empty;
        public DateTime FechaGiro { get; set; } = DateTime.Today;
        public DateTime FechaVencimiento { get; set; } = DateTime.Today;
        public decimal Importe { get; set; }
        public decimal TotalPagado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public string Estado { get; set; } = "PENDIENTE";
        public string Observacion { get; set; } = string.Empty;
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
        public DateTime FechaGiro { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public decimal Importe { get; set; }
        public decimal TotalPagado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string OrigenTipo { get; set; } = string.Empty;
        public int? OrigenId { get; set; }
        public string Observacion { get; set; } = string.Empty;
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
