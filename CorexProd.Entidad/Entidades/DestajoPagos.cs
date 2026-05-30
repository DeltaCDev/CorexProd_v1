using System;

namespace CorexProd.Entidad.Entidades
{
    public class AreaOperativa
    {
        public int IdAreaOperativa { get; set; }
        public string NombreArea { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public bool Estado { get; set; } = true;
        public DateTime FechaRegistro { get; set; }
    }

    public class TrabajadorOperativo
    {
        public int IdTrabajadorOperativo { get; set; }
        public int IdEmpleado { get; set; }
        public string NombreTrabajador { get; set; } = string.Empty;
        public string Documento { get; set; } = string.Empty;
        public string TipoTrabajador { get; set; } = "Destajo";
        public string MedioPagoPreferido { get; set; } = "Efectivo";
        public string NumeroCuenta { get; set; } = string.Empty;
        public string TelefonoPago { get; set; } = string.Empty;
        public string Observacion { get; set; } = string.Empty;
        public bool Estado { get; set; } = true;
        public DateTime FechaRegistro { get; set; }
    }

    public class ConceptoMovimiento
    {
        public int IdConceptoMovimiento { get; set; }
        public string CodigoConcepto { get; set; } = string.Empty;
        public string NombreConcepto { get; set; } = string.Empty;
        public string TipoMovimiento { get; set; } = "Ingreso";
        public string CategoriaMovimiento { get; set; } = "Produccion";
        public string TipoCalculo { get; set; } = "Cantidad x tarifa";
        public bool EsDescuento { get; set; }
        public bool Estado { get; set; } = true;
        public DateTime FechaRegistro { get; set; }
    }

    public class OperacionTextil
    {
        public int IdOperacionTextil { get; set; }
        public string CodigoOperacion { get; set; } = string.Empty;
        public string NombreOperacion { get; set; } = string.Empty;
        public int? IdAreaOperativa { get; set; }
        public string NombreArea { get; set; } = string.Empty;
        public string TipoOperacion { get; set; } = "Operacion";
        public string UnidadMedida { get; set; } = "Unidad";
        public decimal TarifaBase { get; set; }
        public bool Estado { get; set; } = true;
        public DateTime FechaRegistro { get; set; }
    }

    public class PeriodoPago
    {
        public int IdPeriodoPago { get; set; }
        public string CodigoPeriodo { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; } = DateTime.Today;
        public DateTime FechaFin { get; set; } = DateTime.Today;
        public string Estado { get; set; } = "Borrador";
        public string Observacion { get; set; } = string.Empty;
        public decimal TotalIngresos { get; set; }
        public decimal TotalDescuentos { get; set; }
        public decimal NetoCalculado { get; set; }
        public decimal TotalPagado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public DateTime FechaRegistro { get; set; }
    }

    public class MovimientoTrabajador
    {
        public int IdMovimientoTrabajador { get; set; }
        public int IdPeriodoPago { get; set; }
        public string CodigoPeriodo { get; set; } = string.Empty;
        public int IdTrabajadorOperativo { get; set; }
        public string NombreTrabajador { get; set; } = string.Empty;
        public DateTime Fecha { get; set; } = DateTime.Today;
        public string TipoMovimiento { get; set; } = "Ingreso";
        public string CategoriaMovimiento { get; set; } = string.Empty;
        public int IdConceptoMovimiento { get; set; }
        public string NombreConcepto { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public int? IdAreaOperativa { get; set; }
        public string NombreArea { get; set; } = string.Empty;
        public int? IdOperacionTextil { get; set; }
        public string NombreOperacion { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public string UnidadMedida { get; set; } = string.Empty;
        public decimal Tarifa { get; set; }
        public decimal Importe { get; set; }
        public bool EsDescuento { get; set; }
        public bool EsAutomatico { get; set; }
        public string OrigenMovimiento { get; set; } = "Manual";
        public int? ReferenciaId { get; set; }
        public string Estado { get; set; } = "Borrador";
        public string Observacion { get; set; } = string.Empty;
        public string CreadoPor { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public string ModificadoPor { get; set; } = string.Empty;
        public DateTime? FechaModificacion { get; set; }
    }

    public class ResumenPagoTrabajador
    {
        public int IdPeriodoPago { get; set; }
        public int IdTrabajadorOperativo { get; set; }
        public string NombreTrabajador { get; set; } = string.Empty;
        public string TipoTrabajador { get; set; } = string.Empty;
        public string MedioPagoPreferido { get; set; } = string.Empty;
        public decimal SaldoAnterior { get; set; }
        public decimal TotalIngresos { get; set; }
        public decimal TotalDescuentos { get; set; }
        public decimal NetoCalculado { get; set; }
        public decimal TotalPagado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public string EstadoPeriodo { get; set; } = string.Empty;
    }

    public class PrestamoTrabajador
    {
        public int IdPrestamoTrabajador { get; set; }
        public int IdTrabajadorOperativo { get; set; }
        public string NombreTrabajador { get; set; } = string.Empty;
        public DateTime FechaPrestamo { get; set; } = DateTime.Today;
        public decimal MontoTotal { get; set; }
        public int NumeroCuotas { get; set; } = 1;
        public decimal MontoCuota { get; set; }
        public decimal SaldoPendiente { get; set; }
        public string Estado { get; set; } = "Vigente";
        public string Observacion { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
    }

    public class CuotaProgramadaTrabajador
    {
        public int IdCuotaProgramada { get; set; }
        public string TipoOrigen { get; set; } = string.Empty;
        public int ReferenciaId { get; set; }
        public int IdTrabajadorOperativo { get; set; }
        public string NombreTrabajador { get; set; } = string.Empty;
        public int IdConceptoMovimiento { get; set; }
        public string NombreConcepto { get; set; } = string.Empty;
        public int NumeroCuota { get; set; }
        public int TotalCuotas { get; set; }
        public decimal MontoCuota { get; set; }
        public DateTime FechaProgramada { get; set; } = DateTime.Today;
        public int? IdPeriodoAplicado { get; set; }
        public string CodigoPeriodoAplicado { get; set; } = string.Empty;
        public string Estado { get; set; } = "Pendiente";
        public string Observacion { get; set; } = string.Empty;
    }

    public class LotePago
    {
        public int IdLotePago { get; set; }
        public int IdPeriodoPago { get; set; }
        public string CodigoPeriodo { get; set; } = string.Empty;
        public string MedioPago { get; set; } = "Efectivo";
        public DateTime FechaGeneracion { get; set; }
        public string UsuarioGenerador { get; set; } = string.Empty;
        public string Estado { get; set; } = "Generado";
        public decimal TotalLote { get; set; }
        public string Observacion { get; set; } = string.Empty;
    }

    public class LotePagoDetalle
    {
        public int IdLotePagoDetalle { get; set; }
        public int IdLotePago { get; set; }
        public int IdTrabajadorOperativo { get; set; }
        public string NombreTrabajador { get; set; } = string.Empty;
        public decimal MontoPago { get; set; }
        public string MedioPago { get; set; } = string.Empty;
        public string Estado { get; set; } = "Pendiente";
        public string NumeroCuenta { get; set; } = string.Empty;
        public string TelefonoPago { get; set; } = string.Empty;
    }
}
