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
        public DateTime? FechaInicioVigencia { get; set; }
        public DateTime? FechaFinVigencia { get; set; }
        public bool Estado { get; set; } = true;
        public DateTime FechaRegistro { get; set; }
    }

    public class PeriodoPago
    {
        public int IdPeriodoPago { get; set; }
        public string CodigoPeriodo { get; set; } = string.Empty;
        public int NumeroSemana { get; set; }
        public int Anio { get; set; }
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
        public string Documento { get; set; } = string.Empty;
        public string TipoTrabajador { get; set; } = string.Empty;
        public string MedioPagoPreferido { get; set; } = string.Empty;
        public decimal SaldoAnterior { get; set; }
        public decimal TotalIngresos { get; set; }
        public decimal TotalDescuentos { get; set; }
        public decimal NetoCalculado { get; set; }
        public decimal TotalPagado { get; set; }
        public decimal TotalPorPagar { get; set; }
        public decimal SaldoPendiente { get; set; }
        public string EstadoPeriodo { get; set; } = string.Empty;
        public string EstadoCalculo { get; set; } = "Sin calcular";
        public DateTime? FechaCalculo { get; set; }
        public string UsuarioCalculo { get; set; } = string.Empty;
        public decimal TotalAPagar => TotalPorPagar;
        public string EstadoPago
        {
            get
            {
                if (SaldoPendiente <= 0 && TotalPagado > 0)
                    return "Pagado";

                if (TotalPagado > 0 && SaldoPendiente > 0)
                    return "Parcial";

                return "Pendiente";
            }
        }
    }

    public class AlertaCalculoPeriodo
    {
        public int IdCalculoPeriodoAlerta { get; set; }
        public int IdPeriodoPago { get; set; }
        public int? IdTrabajadorOperativo { get; set; }
        public string NombreTrabajador { get; set; } = string.Empty;
        public int? IdMovimientoTrabajador { get; set; }
        public int? IdCuotaProgramada { get; set; }
        public string TipoAlerta { get; set; } = string.Empty;
        public string Severidad { get; set; } = "Advertencia";
        public string Mensaje { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
    }

    public class PrestamoTrabajador
    {
        public int IdPrestamoTrabajador { get; set; }
        public int IdTrabajadorOperativo { get; set; }
        public string NombreTrabajador { get; set; } = string.Empty;
        public DateTime FechaPrestamo { get; set; } = DateTime.Today;
        public DateTime FechaInicioDescuento { get; set; } = DateTime.Today;
        public int? IdConceptoMovimiento { get; set; }
        public string NombreConcepto { get; set; } = string.Empty;
        public decimal MontoTotal { get; set; }
        public int NumeroCuotas { get; set; } = 1;
        public decimal MontoCuota { get; set; }
        public decimal SaldoPendiente { get; set; }
        public string Estado { get; set; } = "Registrado";
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
        public int? IdMovimientoTrabajador { get; set; }
        public DateTime? FechaAplicacion { get; set; }
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

    public class PagoTrabajador
    {
        public int IdPagoTrabajador { get; set; }
        public int IdPeriodoPago { get; set; }
        public string CodigoPeriodo { get; set; } = string.Empty;
        public int IdTrabajadorOperativo { get; set; }
        public string NombreTrabajador { get; set; } = string.Empty;
        public int? IdLotePagoDetalle { get; set; }
        public DateTime FechaPago { get; set; } = DateTime.Today;
        public string MedioPago { get; set; } = string.Empty;
        public string NumeroOperacion { get; set; } = string.Empty;
        public decimal MontoPagado { get; set; }
        public string Estado { get; set; } = "Confirmado";
        public string Observacion { get; set; } = string.Empty;
        public string UsuarioRegistro { get; set; } = string.Empty;
        public string MotivoAnulacion { get; set; } = string.Empty;
        public string UsuarioAnulacion { get; set; } = string.Empty;
        public string AutorizadoPor { get; set; } = string.Empty;
    }

    public class DashboardDestajoIndicador
    {
        public int TrabajadoresActivos { get; set; }
        public int TrabajadoresConMovimientos { get; set; }
        public decimal TotalProducido { get; set; }
        public decimal TotalIngresos { get; set; }
        public decimal TotalDescuentos { get; set; }
        public decimal NetoPeriodo { get; set; }
        public decimal TotalPagado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public int PrestamosActivos { get; set; }
        public int CuotasAplicadas { get; set; }
        public int PeriodosAbiertos { get; set; }
        public int PeriodosPendientesCierre { get; set; }
    }

    public class DashboardDestajoSerie
    {
        public string Categoria { get; set; } = string.Empty;
        public string Etiqueta { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public decimal Importe { get; set; }
    }

    public class AuditoriaDestajo
    {
        public int IdAuditoria { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string Modulo { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty;
        public string RegistroAfectado { get; set; } = string.Empty;
        public string ValorAnterior { get; set; } = string.Empty;
        public string ValorNuevo { get; set; } = string.Empty;
        public string Motivo { get; set; } = string.Empty;
        public string Equipo { get; set; } = string.Empty;
    }
}
