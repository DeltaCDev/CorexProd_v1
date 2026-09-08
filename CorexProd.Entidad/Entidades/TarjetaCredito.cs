using System;
using System.Collections.Generic;

namespace CorexProd.Entidad.Entidades
{
    public class TarjetaCredito
    {
        public int IdTarjetaCredito { get; set; }
        public int IdEntidadFinanciera { get; set; }
        public string EntidadFinanciera { get; set; } = string.Empty;
        public string Titular { get; set; } = string.Empty;
        public int? IdEmpresaResponsable { get; set; }
        public string EmpresaResponsable { get; set; } = string.Empty;
        public string Alias { get; set; } = string.Empty;
        public string Ultimos4 { get; set; } = string.Empty;
        /// <summary>Monedas habilitadas para la tarjeta; no representa importes de periodos.</summary>
        public List<string> MonedasHabilitadas { get; set; } = [];
        /// <summary>Moneda legacy conservada temporalmente por compatibilidad.</summary>
        public string Moneda { get; set; } = "PEN";
        public byte? DiaCorte { get; set; }
        public byte? DiaPago { get; set; }
        public string Estado { get; set; } = "ACTIVA";
        public bool Activo { get; set; } = true;
        public string Observacion { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public string UsuarioCreacion { get; set; } = string.Empty;
        public DateTime? FechaModificacion { get; set; }
        public string UsuarioModificacion { get; set; } = string.Empty;
        public DateTime? FechaAnulacion { get; set; }
        public string UsuarioAnulacion { get; set; } = string.Empty;
        public string MotivoAnulacion { get; set; } = string.Empty;
        public List<TarjetaCreditoPeriodo> Periodos { get; set; } = [];
        public List<TarjetaCreditoHistorial> Historial { get; set; } = [];
        public bool EstaActiva => Estado.Equals("ACTIVA", StringComparison.OrdinalIgnoreCase);
        public bool EstaInactiva => Estado.Equals("INACTIVA", StringComparison.OrdinalIgnoreCase);
        public bool EstaAnulada => Estado.Equals("ANULADA", StringComparison.OrdinalIgnoreCase);
        public string DescripcionTarjeta
        {
            get
            {
                string ultimos = string.IsNullOrWhiteSpace(Ultimos4) ? string.Empty : $" ****{Ultimos4}";
                return string.IsNullOrWhiteSpace(EntidadFinanciera)
                    ? $"{Alias}{ultimos}".Trim()
                    : $"{EntidadFinanciera} - {Alias}{ultimos}".Trim();
            }
        }
    }

    public class TarjetaCreditoPeriodo
    {
        public int IdTarjetaPeriodo { get; set; }
        public int IdTarjetaCredito { get; set; }
        public string Alias { get; set; } = string.Empty;
        public string Ultimos4 { get; set; } = string.Empty;
        public string Moneda { get; set; } = "PEN";
        public string EntidadFinanciera { get; set; } = string.Empty;
        public string Titular { get; set; } = string.Empty;
        public string EmpresaResponsable { get; set; } = string.Empty;
        public short PeriodoAnio { get; set; }
        public byte PeriodoMes { get; set; }
        public DateTime FechaCorte { get; set; } = DateTime.Today;
        public DateTime FechaVencimiento { get; set; } = DateTime.Today;
        public decimal? ImportePEN { get; set; }
        public decimal? ImporteUSD { get; set; }
        public decimal TotalPagadoPEN { get; set; }
        public decimal TotalPagadoUSD { get; set; }
        public decimal? SaldoPendientePEN { get; set; }
        public decimal? SaldoPendienteUSD { get; set; }
        public string? EstadoPEN { get; set; }
        public string? EstadoUSD { get; set; }
        /// <summary>Propiedades legacy conservadas temporalmente por compatibilidad.</summary>
        public decimal? Monto { get; set; }
        public decimal TotalPagado { get; set; }
        public decimal? SaldoPendiente { get; set; }
        public string Estado { get; set; } = "ABIERTO";
        public string Observacion { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public string UsuarioCreacion { get; set; } = string.Empty;
        public DateTime? FechaModificacion { get; set; }
        public string UsuarioModificacion { get; set; } = string.Empty;
        public DateTime? FechaAnulacion { get; set; }
        public string UsuarioAnulacion { get; set; } = string.Empty;
        public string MotivoAnulacion { get; set; } = string.Empty;
        public List<TarjetaCreditoPago> Pagos { get; set; } = [];
        public bool EstaAbierto => Estado.Equals("ABIERTO", StringComparison.OrdinalIgnoreCase);
        public bool EstaAnulado => Estado.Equals("ANULADA", StringComparison.OrdinalIgnoreCase);
        public string PeriodoTexto => PeriodoMes <= 0 ? PeriodoAnio.ToString() : $"{PeriodoMes:00}/{PeriodoAnio}";
        public string ReferenciaTarjeta
        {
            get
            {
                string ultimos = string.IsNullOrWhiteSpace(Ultimos4) ? string.Empty : $" ****{Ultimos4}";
                return $"{Alias}{ultimos}".Trim();
            }
        }
    }

    public class TarjetaCreditoPago
    {
        public int IdPago { get; set; }
        public int IdTarjetaPago
        {
            get => IdPago;
            set => IdPago = value;
        }
        public int IdTarjetaPeriodo { get; set; }
        public DateTime FechaPago { get; set; } = DateTime.Today;
        public int? IdBanco { get; set; }
        public string Banco { get; set; } = string.Empty;
        public int? IdCuentaBancaria { get; set; }
        public string CuentaBancaria { get; set; } = string.Empty;
        public string NombreCuenta
        {
            get => CuentaBancaria;
            set => CuentaBancaria = value;
        }
        public decimal Importe { get; set; }
        public string? Moneda { get; set; }
        public string NumeroOperacion { get; set; } = string.Empty;
        public string Observacion { get; set; } = string.Empty;
        public string Estado { get; set; } = "ACTIVO";
        public DateTime FechaCreacion { get; set; }
        public string UsuarioCreacion { get; set; } = string.Empty;
        public DateTime? FechaAnulacion { get; set; }
        public string UsuarioAnulacion { get; set; } = string.Empty;
        public string MotivoAnulacion { get; set; } = string.Empty;
        public bool EstaAnulado => Estado.Equals("ANULADO", StringComparison.OrdinalIgnoreCase);
    }

    public class TarjetaCreditoHistorial
    {
        public long IdHistorialTarjetaCredito { get; set; }
        public int IdTarjetaCredito { get; set; }
        public int? IdTarjetaPeriodo { get; set; }
        public int? IdTarjetaPago { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty;
        public string TipoEvento
        {
            get => Accion;
            set => Accion = value;
        }
        public string EstadoAnterior { get; set; } = string.Empty;
        public string EstadoNuevo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }
        public DateTime Fecha
        {
            get => FechaHora;
            set => FechaHora = value;
        }
    }

    public class TarjetaCreditoResultado
    {
        public int IdTarjetaCredito { get; set; }
        public bool Resultado { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }

    public class TarjetaCreditoPeriodoResultado
    {
        public int IdTarjetaPeriodo { get; set; }
        public bool Resultado { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public decimal TotalPagado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public string EstadoPeriodo { get; set; } = string.Empty;
    }

    public class TarjetaCreditoPagoResultado
    {
        public int IdPago { get; set; }
        public bool Resultado { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public decimal TotalPagado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public string EstadoPeriodo { get; set; } = string.Empty;
    }
}
