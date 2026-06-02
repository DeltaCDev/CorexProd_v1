using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.DestajoPagos.ViewModels
{
    public class DestajoPagosViewModel : BaseViewModel
    {
        private readonly DestajoPagosNegocio _destajoNegocio = new();
        private readonly EmpleadoNegocio _empleadoNegocio = new();

        private AreaOperativa? _areaSeleccionada;
        private ConceptoMovimiento? _conceptoSeleccionado;
        private OperacionTextil? _operacionSeleccionada;
        private TrabajadorOperativo? _trabajadorSeleccionado;
        private PeriodoPago? _periodoSeleccionado;
        private MovimientoTrabajador? _movimientoSeleccionado;
        private CuotaProgramadaTrabajador? _cuotaSeleccionada;
        private LotePago? _loteSeleccionado;
        private LotePagoDetalle? _loteDetalleSeleccionado;
        private ResumenPagoTrabajador? _resumenSeleccionado;

        private int _idAreaOperativa;
        private string _nombreArea = string.Empty;
        private string _descripcionArea = string.Empty;
        private bool _estadoArea = true;

        private int _idConceptoMovimiento;
        private string _codigoConcepto = string.Empty;
        private string _nombreConcepto = string.Empty;
        private string _tipoMovimientoConcepto = "Ingreso";
        private string _categoriaMovimientoConcepto = "Produccion";
        private string _tipoCalculoConcepto = "Cantidad x tarifa";
        private bool _esDescuentoConcepto;
        private bool _estadoConcepto = true;

        private int _idOperacionTextil;
        private string _codigoOperacion = string.Empty;
        private string _nombreOperacion = string.Empty;
        private int _idAreaOperacion;
        private string _tipoOperacion = "Operacion";
        private string _unidadOperacion = "Unidad";
        private decimal _tarifaOperacion;
        private bool _estadoOperacion = true;

        private int _idTrabajadorOperativo;
        private int _idEmpleadoTrabajador;
        private string _tipoTrabajador = "Destajo";
        private string _medioPagoTrabajador = "Efectivo";
        private string _numeroCuentaTrabajador = string.Empty;
        private string _telefonoPagoTrabajador = string.Empty;
        private string _observacionTrabajador = string.Empty;
        private bool _estadoTrabajador = true;

        private int _idPeriodoPago;
        private string _codigoPeriodo = string.Empty;
        private DateTime? _fechaInicioPeriodo = DateTime.Today;
        private DateTime? _fechaFinPeriodo = DateTime.Today;
        private string _estadoPeriodo = "Borrador";
        private string _observacionPeriodo = string.Empty;
        private decimal _totalIngresosPeriodo;
        private decimal _totalDescuentosPeriodo;
        private decimal _netoPeriodo;
        private decimal _totalPagadoPeriodo;
        private decimal _saldoPendientePeriodo;

        private int _idMovimientoTrabajador;
        private DateTime? _fechaMovimiento = DateTime.Today;
        private int _idTrabajadorMovimiento;
        private int _idConceptoMovimientoForm;
        private int _idAreaMovimiento;
        private int _idOperacionMovimiento;
        private string _tipoMovimientoForm = "Ingreso";
        private string _categoriaMovimientoForm = "Produccion";
        private string _descripcionMovimiento = string.Empty;
        private decimal _cantidadMovimiento;
        private string _unidadMovimiento = "Unidad";
        private decimal _tarifaMovimiento;
        private decimal _importeMovimiento;
        private bool _esDescuentoMovimiento;
        private string _estadoMovimiento = "Borrador";
        private string _observacionMovimiento = string.Empty;

        private int _idTrabajadorPrestamo;
        private int _idTrabajadorFiltroMovimientos;
        private int _idTrabajadorFiltroPrestamos;
        private DateTime? _fechaPrestamo = DateTime.Today;
        private decimal _montoPrestamo;
        private int _numeroCuotasPrestamo = 1;
        private decimal _montoCuotaPrestamo;
        private int _idConceptoCuota;
        private string _observacionPrestamo = string.Empty;
        private int _idTrabajadorFiltroCuotas;

        private string _medioPagoLote = "Efectivo";
        private string _estadoLote = "Generado";
        private string _observacionLote = string.Empty;
        private decimal _montoPagoLote;

        public ObservableCollection<AreaOperativa> Areas { get; } = [];
        public ObservableCollection<ConceptoMovimiento> Conceptos { get; } = [];
        public ObservableCollection<ConceptoMovimiento> ConceptosDescuento { get; } = [];
        public ObservableCollection<OperacionTextil> Operaciones { get; } = [];
        public ObservableCollection<TrabajadorOperativo> Trabajadores { get; } = [];
        public ObservableCollection<PeriodoPago> Periodos { get; } = [];
        public ObservableCollection<MovimientoTrabajador> Movimientos { get; } = [];
        public ObservableCollection<ResumenPagoTrabajador> Resumenes { get; } = [];
        public ObservableCollection<PrestamoTrabajador> Prestamos { get; } = [];
        public ObservableCollection<CuotaProgramadaTrabajador> Cuotas { get; } = [];
        public ObservableCollection<LotePago> Lotes { get; } = [];
        public ObservableCollection<LotePagoDetalle> LoteDetalles { get; } = [];
        public ObservableCollection<Empleado> Empleados { get; } = [];

        public ObservableCollection<string> TiposTrabajador { get; } =
        [
            "Destajo",
            "Staff",
            "Mixto",
            "Asistente",
            "Supervisor / Jefatura",
            "Apoyo / Cocina / Servicios"
        ];

        public ObservableCollection<string> MediosPago { get; } =
        [
            "BCP",
            "Yape",
            "Efectivo",
            "Transferencia",
            "Mixto"
        ];

        public ObservableCollection<string> EstadosPeriodo { get; } =
        [
            "Pendiente",
            "Pago Parcial",
            "Pagado / Cerrado"
        ];

        public ObservableCollection<string> TiposMovimiento { get; } =
        [
            "Ingreso",
            "Descuento",
            "Pago",
            "Ajuste"
        ];

        public ObservableCollection<string> CategoriasMovimiento { get; } =
        [
            "Produccion",
            "Basico",
            "Horas",
            "Movilidad",
            "Financiero",
            "Calidad",
            "Legal",
            "Pago",
            "Saldo",
            "Ajuste"
        ];

        public ObservableCollection<string> TiposCalculo { get; } =
        [
            "Cantidad x tarifa",
            "Horas x tarifa",
            "Dias x tarifa",
            "Monto fijo",
            "Cuota",
            "Ajuste manual",
            "Pago directo"
        ];

        public ObservableCollection<string> EstadosMovimiento { get; } =
        [
            "Pendiente",
            "Pago Parcial",
            "Pagado / Cerrado"
        ];

        public ObservableCollection<string> TiposOperacion { get; } =
        [
            "Prenda completa",
            "Operacion",
            "Suboperacion",
            "Proceso",
            "Especial"
        ];

        public ObservableCollection<string> EstadosLote { get; } =
        [
            "Pendiente",
            "Pago Parcial",
            "Pagado / Cerrado"
        ];

        public int IdAreaOperativa
        {
            get => _idAreaOperativa;
            set { _idAreaOperativa = value; OnPropertyChanged(); }
        }

        public string NombreArea
        {
            get => _nombreArea;
            set { _nombreArea = value; OnPropertyChanged(); }
        }

        public string DescripcionArea
        {
            get => _descripcionArea;
            set { _descripcionArea = value; OnPropertyChanged(); }
        }

        public bool EstadoArea
        {
            get => _estadoArea;
            set { _estadoArea = value; OnPropertyChanged(); }
        }

        public AreaOperativa? AreaSeleccionada
        {
            get => _areaSeleccionada;
            set
            {
                _areaSeleccionada = value;
                OnPropertyChanged();

                if (value == null)
                    return;

                IdAreaOperativa = value.IdAreaOperativa;
                NombreArea = value.NombreArea;
                DescripcionArea = value.Descripcion;
                EstadoArea = value.Estado;
            }
        }

        public int IdConceptoMovimiento
        {
            get => _idConceptoMovimiento;
            set { _idConceptoMovimiento = value; OnPropertyChanged(); }
        }

        public string CodigoConcepto
        {
            get => _codigoConcepto;
            set { _codigoConcepto = value; OnPropertyChanged(); }
        }

        public string NombreConcepto
        {
            get => _nombreConcepto;
            set { _nombreConcepto = value; OnPropertyChanged(); }
        }

        public string TipoMovimientoConcepto
        {
            get => _tipoMovimientoConcepto;
            set { _tipoMovimientoConcepto = value; OnPropertyChanged(); }
        }

        public string CategoriaMovimientoConcepto
        {
            get => _categoriaMovimientoConcepto;
            set { _categoriaMovimientoConcepto = value; OnPropertyChanged(); }
        }

        public string TipoCalculoConcepto
        {
            get => _tipoCalculoConcepto;
            set { _tipoCalculoConcepto = value; OnPropertyChanged(); }
        }

        public bool EsDescuentoConcepto
        {
            get => _esDescuentoConcepto;
            set { _esDescuentoConcepto = value; OnPropertyChanged(); }
        }

        public bool EstadoConcepto
        {
            get => _estadoConcepto;
            set { _estadoConcepto = value; OnPropertyChanged(); }
        }

        public ConceptoMovimiento? ConceptoSeleccionado
        {
            get => _conceptoSeleccionado;
            set
            {
                _conceptoSeleccionado = value;
                OnPropertyChanged();

                if (value == null)
                    return;

                IdConceptoMovimiento = value.IdConceptoMovimiento;
                CodigoConcepto = value.CodigoConcepto;
                NombreConcepto = value.NombreConcepto;
                TipoMovimientoConcepto = value.TipoMovimiento;
                CategoriaMovimientoConcepto = value.CategoriaMovimiento;
                TipoCalculoConcepto = value.TipoCalculo;
                EsDescuentoConcepto = value.EsDescuento;
                EstadoConcepto = value.Estado;
            }
        }

        public int IdOperacionTextil
        {
            get => _idOperacionTextil;
            set { _idOperacionTextil = value; OnPropertyChanged(); }
        }

        public string CodigoOperacion
        {
            get => _codigoOperacion;
            set { _codigoOperacion = value; OnPropertyChanged(); }
        }

        public string NombreOperacion
        {
            get => _nombreOperacion;
            set { _nombreOperacion = value; OnPropertyChanged(); }
        }

        public int IdAreaOperacion
        {
            get => _idAreaOperacion;
            set { _idAreaOperacion = value; OnPropertyChanged(); }
        }

        public string TipoOperacion
        {
            get => _tipoOperacion;
            set { _tipoOperacion = value; OnPropertyChanged(); }
        }

        public string UnidadOperacion
        {
            get => _unidadOperacion;
            set { _unidadOperacion = value; OnPropertyChanged(); }
        }

        public decimal TarifaOperacion
        {
            get => _tarifaOperacion;
            set { _tarifaOperacion = value; OnPropertyChanged(); }
        }

        public bool EstadoOperacion
        {
            get => _estadoOperacion;
            set { _estadoOperacion = value; OnPropertyChanged(); }
        }

        public OperacionTextil? OperacionSeleccionada
        {
            get => _operacionSeleccionada;
            set
            {
                _operacionSeleccionada = value;
                OnPropertyChanged();

                if (value == null)
                    return;

                IdOperacionTextil = value.IdOperacionTextil;
                CodigoOperacion = value.CodigoOperacion;
                NombreOperacion = value.NombreOperacion;
                IdAreaOperacion = value.IdAreaOperativa ?? 0;
                TipoOperacion = value.TipoOperacion;
                UnidadOperacion = value.UnidadMedida;
                TarifaOperacion = value.TarifaBase;
                EstadoOperacion = value.Estado;
            }
        }

        public int IdTrabajadorOperativo
        {
            get => _idTrabajadorOperativo;
            set { _idTrabajadorOperativo = value; OnPropertyChanged(); }
        }

        public int IdEmpleadoTrabajador
        {
            get => _idEmpleadoTrabajador;
            set { _idEmpleadoTrabajador = value; OnPropertyChanged(); }
        }

        public string TipoTrabajador
        {
            get => _tipoTrabajador;
            set { _tipoTrabajador = value; OnPropertyChanged(); }
        }

        public string MedioPagoTrabajador
        {
            get => _medioPagoTrabajador;
            set { _medioPagoTrabajador = value; OnPropertyChanged(); }
        }

        public string NumeroCuentaTrabajador
        {
            get => _numeroCuentaTrabajador;
            set { _numeroCuentaTrabajador = value; OnPropertyChanged(); }
        }

        public string TelefonoPagoTrabajador
        {
            get => _telefonoPagoTrabajador;
            set { _telefonoPagoTrabajador = value; OnPropertyChanged(); }
        }

        public string ObservacionTrabajador
        {
            get => _observacionTrabajador;
            set { _observacionTrabajador = value; OnPropertyChanged(); }
        }

        public bool EstadoTrabajador
        {
            get => _estadoTrabajador;
            set { _estadoTrabajador = value; OnPropertyChanged(); }
        }

        public TrabajadorOperativo? TrabajadorSeleccionado
        {
            get => _trabajadorSeleccionado;
            set
            {
                _trabajadorSeleccionado = value;
                OnPropertyChanged();

                if (value == null)
                    return;

                IdTrabajadorOperativo = value.IdTrabajadorOperativo;
                IdEmpleadoTrabajador = value.IdEmpleado;
                TipoTrabajador = value.TipoTrabajador;
                MedioPagoTrabajador = value.MedioPagoPreferido;
                NumeroCuentaTrabajador = value.NumeroCuenta;
                TelefonoPagoTrabajador = value.TelefonoPago;
                ObservacionTrabajador = value.Observacion;
                EstadoTrabajador = value.Estado;
            }
        }

        public int IdPeriodoPago
        {
            get => _idPeriodoPago;
            set { _idPeriodoPago = value; OnPropertyChanged(); }
        }

        public string CodigoPeriodo
        {
            get => _codigoPeriodo;
            set { _codigoPeriodo = value; OnPropertyChanged(); }
        }

        public DateTime? FechaInicioPeriodo
        {
            get => _fechaInicioPeriodo;
            set { _fechaInicioPeriodo = value; OnPropertyChanged(); }
        }

        public DateTime? FechaFinPeriodo
        {
            get => _fechaFinPeriodo;
            set { _fechaFinPeriodo = value; OnPropertyChanged(); }
        }

        public string EstadoPeriodo
        {
            get => _estadoPeriodo;
            set { _estadoPeriodo = value; OnPropertyChanged(); }
        }

        public string ObservacionPeriodo
        {
            get => _observacionPeriodo;
            set { _observacionPeriodo = value; OnPropertyChanged(); }
        }

        public decimal TotalIngresosPeriodo
        {
            get => _totalIngresosPeriodo;
            set { _totalIngresosPeriodo = value; OnPropertyChanged(); }
        }

        public decimal TotalDescuentosPeriodo
        {
            get => _totalDescuentosPeriodo;
            set { _totalDescuentosPeriodo = value; OnPropertyChanged(); }
        }

        public decimal NetoPeriodo
        {
            get => _netoPeriodo;
            set { _netoPeriodo = value; OnPropertyChanged(); }
        }

        public decimal TotalPagadoPeriodo
        {
            get => _totalPagadoPeriodo;
            set { _totalPagadoPeriodo = value; OnPropertyChanged(); }
        }

        public decimal SaldoPendientePeriodo
        {
            get => _saldoPendientePeriodo;
            set { _saldoPendientePeriodo = value; OnPropertyChanged(); }
        }

        public PeriodoPago? PeriodoSeleccionado
        {
            get => _periodoSeleccionado;
            set
            {
                _periodoSeleccionado = value;
                OnPropertyChanged();

                if (value == null)
                    return;

                IdPeriodoPago = value.IdPeriodoPago;
                CodigoPeriodo = value.CodigoPeriodo;
                FechaInicioPeriodo = value.FechaInicio;
                FechaFinPeriodo = value.FechaFin;
                EstadoPeriodo = value.Estado;
                ObservacionPeriodo = value.Observacion;
                TotalIngresosPeriodo = value.TotalIngresos;
                TotalDescuentosPeriodo = value.TotalDescuentos;
                NetoPeriodo = value.NetoCalculado;
                TotalPagadoPeriodo = value.TotalPagado;
                SaldoPendientePeriodo = value.SaldoPendiente;

                CargarMovimientos();
                CargarResumen();
                CargarLotes();
            }
        }

        public int IdMovimientoTrabajador
        {
            get => _idMovimientoTrabajador;
            set { _idMovimientoTrabajador = value; OnPropertyChanged(); }
        }

        public DateTime? FechaMovimiento
        {
            get => _fechaMovimiento;
            set { _fechaMovimiento = value; OnPropertyChanged(); }
        }

        public int IdTrabajadorMovimiento
        {
            get => _idTrabajadorMovimiento;
            set { _idTrabajadorMovimiento = value; OnPropertyChanged(); }
        }

        public int IdConceptoMovimientoForm
        {
            get => _idConceptoMovimientoForm;
            set
            {
                _idConceptoMovimientoForm = value;
                OnPropertyChanged();
                AplicarConceptoAlMovimiento();
            }
        }

        public int IdAreaMovimiento
        {
            get => _idAreaMovimiento;
            set { _idAreaMovimiento = value; OnPropertyChanged(); }
        }

        public int IdOperacionMovimiento
        {
            get => _idOperacionMovimiento;
            set
            {
                _idOperacionMovimiento = value;
                OnPropertyChanged();
                AplicarOperacionAlMovimiento();
            }
        }

        public string TipoMovimientoForm
        {
            get => _tipoMovimientoForm;
            set { _tipoMovimientoForm = value; OnPropertyChanged(); }
        }

        public string CategoriaMovimientoForm
        {
            get => _categoriaMovimientoForm;
            set { _categoriaMovimientoForm = value; OnPropertyChanged(); }
        }

        public string DescripcionMovimiento
        {
            get => _descripcionMovimiento;
            set { _descripcionMovimiento = value; OnPropertyChanged(); }
        }

        public decimal CantidadMovimiento
        {
            get => _cantidadMovimiento;
            set
            {
                _cantidadMovimiento = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalMovimientoCalculado));
            }
        }

        public string UnidadMovimiento
        {
            get => _unidadMovimiento;
            set { _unidadMovimiento = value; OnPropertyChanged(); }
        }

        public decimal TarifaMovimiento
        {
            get => _tarifaMovimiento;
            set
            {
                _tarifaMovimiento = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalMovimientoCalculado));
            }
        }

        public decimal ImporteMovimiento
        {
            get => _importeMovimiento;
            set
            {
                _importeMovimiento = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalMovimientoCalculado));
            }
        }

        public decimal TotalMovimientoCalculado
        {
            get
            {
                if (CantidadMovimiento > 0 && TarifaMovimiento > 0)
                    return Math.Round(CantidadMovimiento * TarifaMovimiento, 2);

                return ImporteMovimiento;
            }
        }

        public bool EsDescuentoMovimiento
        {
            get => _esDescuentoMovimiento;
            set { _esDescuentoMovimiento = value; OnPropertyChanged(); }
        }

        public string EstadoMovimiento
        {
            get => _estadoMovimiento;
            set { _estadoMovimiento = value; OnPropertyChanged(); }
        }

        public string ObservacionMovimiento
        {
            get => _observacionMovimiento;
            set { _observacionMovimiento = value; OnPropertyChanged(); }
        }

        public MovimientoTrabajador? MovimientoSeleccionado
        {
            get => _movimientoSeleccionado;
            set
            {
                _movimientoSeleccionado = value;
                OnPropertyChanged();

                if (value == null)
                    return;

                IdMovimientoTrabajador = value.IdMovimientoTrabajador;
                FechaMovimiento = value.Fecha;
                IdTrabajadorMovimiento = value.IdTrabajadorOperativo;
                IdConceptoMovimientoForm = value.IdConceptoMovimiento;
                IdAreaMovimiento = value.IdAreaOperativa ?? 0;
                IdOperacionMovimiento = value.IdOperacionTextil ?? 0;
                TipoMovimientoForm = value.TipoMovimiento;
                CategoriaMovimientoForm = value.CategoriaMovimiento;
                DescripcionMovimiento = value.Descripcion;
                CantidadMovimiento = value.Cantidad;
                UnidadMovimiento = value.UnidadMedida;
                TarifaMovimiento = value.Tarifa;
                ImporteMovimiento = value.Importe;
                EsDescuentoMovimiento = value.EsDescuento;
                EstadoMovimiento = value.Estado;
                ObservacionMovimiento = value.Observacion;
            }
        }

        public int IdTrabajadorPrestamo
        {
            get => _idTrabajadorPrestamo;
            set { _idTrabajadorPrestamo = value; OnPropertyChanged(); }
        }

        public int IdTrabajadorFiltroMovimientos
        {
            get => _idTrabajadorFiltroMovimientos;
            set { _idTrabajadorFiltroMovimientos = value; OnPropertyChanged(); }
        }

        public int IdTrabajadorFiltroPrestamos
        {
            get => _idTrabajadorFiltroPrestamos;
            set { _idTrabajadorFiltroPrestamos = value; OnPropertyChanged(); }
        }

        public DateTime? FechaPrestamo
        {
            get => _fechaPrestamo;
            set { _fechaPrestamo = value; OnPropertyChanged(); }
        }

        public decimal MontoPrestamo
        {
            get => _montoPrestamo;
            set
            {
                _montoPrestamo = value;
                OnPropertyChanged();
                RecalcularCuotaPrestamo();
            }
        }

        public int NumeroCuotasPrestamo
        {
            get => _numeroCuotasPrestamo;
            set
            {
                _numeroCuotasPrestamo = value;
                OnPropertyChanged();
                RecalcularCuotaPrestamo();
            }
        }

        public decimal MontoCuotaPrestamo
        {
            get => _montoCuotaPrestamo;
            set { _montoCuotaPrestamo = value; OnPropertyChanged(); }
        }

        public int IdConceptoCuota
        {
            get => _idConceptoCuota;
            set { _idConceptoCuota = value; OnPropertyChanged(); }
        }

        public string ObservacionPrestamo
        {
            get => _observacionPrestamo;
            set { _observacionPrestamo = value; OnPropertyChanged(); }
        }

        public int IdTrabajadorFiltroCuotas
        {
            get => _idTrabajadorFiltroCuotas;
            set { _idTrabajadorFiltroCuotas = value; OnPropertyChanged(); }
        }

        public CuotaProgramadaTrabajador? CuotaSeleccionada
        {
            get => _cuotaSeleccionada;
            set { _cuotaSeleccionada = value; OnPropertyChanged(); }
        }

        public string MedioPagoLote
        {
            get => _medioPagoLote;
            set { _medioPagoLote = value; OnPropertyChanged(); }
        }

        public string EstadoLote
        {
            get => _estadoLote;
            set { _estadoLote = value; OnPropertyChanged(); }
        }

        public string ObservacionLote
        {
            get => _observacionLote;
            set { _observacionLote = value; OnPropertyChanged(); }
        }

        public decimal MontoPagoLote
        {
            get => _montoPagoLote;
            set { _montoPagoLote = value; OnPropertyChanged(); }
        }

        public LotePago? LoteSeleccionado
        {
            get => _loteSeleccionado;
            set
            {
                _loteSeleccionado = value;
                OnPropertyChanged();

                if (value == null)
                    return;

                MedioPagoLote = value.MedioPago;
                EstadoLote = value.Estado;
                ObservacionLote = value.Observacion;
                CargarLoteDetalles();
            }
        }

        public ResumenPagoTrabajador? ResumenSeleccionado
        {
            get => _resumenSeleccionado;
            set
            {
                _resumenSeleccionado = value;
                OnPropertyChanged();

                if (value != null)
                {
                    MontoPagoLote = value.SaldoPendiente > 0 ? value.SaldoPendiente : 0;
                    MedioPagoLote = value.MedioPagoPreferido;
                }
            }
        }

        public LotePagoDetalle? LoteDetalleSeleccionado
        {
            get => _loteDetalleSeleccionado;
            set
            {
                _loteDetalleSeleccionado = value;
                OnPropertyChanged();

                if (value != null)
                {
                    MontoPagoLote = value.MontoPago;
                    MedioPagoLote = value.MedioPago;
                }
            }
        }

        public ICommand RefrescarCommand { get; }
        public ICommand GuardarAreaCommand { get; }
        public ICommand LimpiarAreaCommand { get; }
        public ICommand EliminarAreaCommand { get; }
        public ICommand GuardarConceptoCommand { get; }
        public ICommand LimpiarConceptoCommand { get; }
        public ICommand EliminarConceptoCommand { get; }
        public ICommand GuardarOperacionCommand { get; }
        public ICommand LimpiarOperacionCommand { get; }
        public ICommand EliminarOperacionCommand { get; }
        public ICommand GuardarTrabajadorCommand { get; }
        public ICommand LimpiarTrabajadorCommand { get; }
        public ICommand EliminarTrabajadorCommand { get; }
        public ICommand GuardarPeriodoCommand { get; }
        public ICommand LimpiarPeriodoCommand { get; }
        public ICommand CambiarEstadoPeriodoCommand { get; }
        public ICommand GuardarMovimientoCommand { get; }
        public ICommand LimpiarMovimientoCommand { get; }
        public ICommand EliminarMovimientoCommand { get; }
        public ICommand RegistrarPrestamoCommand { get; }
        public ICommand LimpiarPrestamoCommand { get; }
        public ICommand FiltrarCuotasCommand { get; }
        public ICommand FiltrarMovimientosCommand { get; }
        public ICommand FiltrarPrestamosCommand { get; }
        public ICommand LimpiarFiltroMovimientosCommand { get; }
        public ICommand LimpiarFiltroPrestamosCommand { get; }
        public ICommand AplicarCuotaCommand { get; }
        public ICommand GenerarLoteCommand { get; }
        public ICommand CambiarEstadoLoteCommand { get; }
        public ICommand RegistrarPagoCompletoCommand { get; }
        public ICommand RegistrarPagoParcialCommand { get; }
        public ICommand DescargarBoletasSeleccionadasCommand { get; }
        public ICommand DescargarTodasBoletasCommand { get; }

        public DestajoPagosViewModel()
        {
            RefrescarCommand = new RelayCommand(_ => CargarTodo());
            GuardarAreaCommand = new RelayCommand(_ => GuardarArea());
            LimpiarAreaCommand = new RelayCommand(_ => LimpiarArea());
            EliminarAreaCommand = new RelayCommand(_ => EliminarArea());
            GuardarConceptoCommand = new RelayCommand(_ => GuardarConcepto());
            LimpiarConceptoCommand = new RelayCommand(_ => LimpiarConcepto());
            EliminarConceptoCommand = new RelayCommand(_ => EliminarConcepto());
            GuardarOperacionCommand = new RelayCommand(_ => GuardarOperacion());
            LimpiarOperacionCommand = new RelayCommand(_ => LimpiarOperacion());
            EliminarOperacionCommand = new RelayCommand(_ => EliminarOperacion());
            GuardarTrabajadorCommand = new RelayCommand(_ => GuardarTrabajador());
            LimpiarTrabajadorCommand = new RelayCommand(_ => LimpiarTrabajador());
            EliminarTrabajadorCommand = new RelayCommand(_ => EliminarTrabajador());
            GuardarPeriodoCommand = new RelayCommand(_ => GuardarPeriodo());
            LimpiarPeriodoCommand = new RelayCommand(_ => LimpiarPeriodo());
            CambiarEstadoPeriodoCommand = new RelayCommand(parametro => CambiarEstadoPeriodo(parametro?.ToString() ?? string.Empty));
            GuardarMovimientoCommand = new RelayCommand(_ => GuardarMovimiento());
            LimpiarMovimientoCommand = new RelayCommand(_ => LimpiarMovimiento());
            EliminarMovimientoCommand = new RelayCommand(_ => EliminarMovimiento());
            RegistrarPrestamoCommand = new RelayCommand(_ => RegistrarPrestamo());
            LimpiarPrestamoCommand = new RelayCommand(_ => LimpiarPrestamo());
            FiltrarCuotasCommand = new RelayCommand(_ => CargarCuotas());
            FiltrarMovimientosCommand = new RelayCommand(_ => CargarMovimientos());
            FiltrarPrestamosCommand = new RelayCommand(_ => CargarPrestamos());
            LimpiarFiltroMovimientosCommand = new RelayCommand(_ =>
            {
                IdTrabajadorFiltroMovimientos = 0;
                CargarMovimientos();
            });
            LimpiarFiltroPrestamosCommand = new RelayCommand(_ =>
            {
                IdTrabajadorFiltroPrestamos = 0;
                IdTrabajadorFiltroCuotas = 0;
                CargarPrestamos();
                CargarCuotas();
            });
            AplicarCuotaCommand = new RelayCommand(_ => AplicarCuota());
            GenerarLoteCommand = new RelayCommand(_ => GenerarLote());
            CambiarEstadoLoteCommand = new RelayCommand(_ => CambiarEstadoLote());
            RegistrarPagoCompletoCommand = new RelayCommand(_ => RegistrarPagoCompleto());
            RegistrarPagoParcialCommand = new RelayCommand(_ => RegistrarPagoParcial());
            DescargarBoletasSeleccionadasCommand = new RelayCommand(DescargarBoletasSeleccionadas);
            DescargarTodasBoletasCommand = new RelayCommand(_ => DescargarTodasBoletas());

            CargarTodo();
        }

        private void CargarTodo()
        {
            try
            {
                CargarEmpleados();
                CargarAreas();
                CargarConceptos();
                CargarOperaciones();
                CargarTrabajadores();
                CargarPeriodos();
                CargarPrestamos();
                CargarCuotas();

                if (PeriodoSeleccionado != null)
                {
                    CargarMovimientos();
                    CargarResumen();
                    CargarLotes();
                }
            }
            catch (Exception ex)
            {
                NotificationService.Warning(
                    $"No se pudo cargar el módulo de destajo. Verifique que el script SQL del módulo esté aplicado.\n{ex.Message}");
            }
        }

        private void CargarEmpleados()
        {
            Empleados.Clear();

            foreach (Empleado empleado in _empleadoNegocio.Listar())
            {
                if (empleado.Estado)
                {
                    Empleados.Add(empleado);
                }
            }
        }

        private void CargarAreas()
        {
            Areas.Clear();

            foreach (AreaOperativa area in _destajoNegocio.ListarAreas())
            {
                Areas.Add(area);
            }
        }

        private void CargarConceptos()
        {
            Conceptos.Clear();
            ConceptosDescuento.Clear();

            foreach (ConceptoMovimiento concepto in _destajoNegocio.ListarConceptos())
            {
                Conceptos.Add(concepto);

                if (concepto.Estado && (concepto.EsDescuento || concepto.TipoMovimiento == "Descuento"))
                {
                    ConceptosDescuento.Add(concepto);
                }
            }

            if (IdConceptoCuota == 0)
            {
                IdConceptoCuota = ConceptosDescuento.FirstOrDefault()?.IdConceptoMovimiento ?? 0;
            }
        }

        private void CargarOperaciones()
        {
            Operaciones.Clear();

            foreach (OperacionTextil operacion in _destajoNegocio.ListarOperaciones())
            {
                Operaciones.Add(operacion);
            }
        }

        private void CargarTrabajadores()
        {
            Trabajadores.Clear();

            foreach (TrabajadorOperativo trabajador in _destajoNegocio.ListarTrabajadores())
            {
                Trabajadores.Add(trabajador);
            }
        }

        private void CargarPeriodos()
        {
            int periodoActual = PeriodoSeleccionado?.IdPeriodoPago ?? 0;
            Periodos.Clear();

            foreach (PeriodoPago periodo in _destajoNegocio.ListarPeriodos())
            {
                Periodos.Add(periodo);
            }

            PeriodoSeleccionado = Periodos.FirstOrDefault(p => p.IdPeriodoPago == periodoActual)
                ?? Periodos.FirstOrDefault();
        }

        private void CargarMovimientos()
        {
            Movimientos.Clear();

            IEnumerable<MovimientoTrabajador> movimientos = _destajoNegocio.ListarMovimientos(PeriodoSeleccionado?.IdPeriodoPago ?? 0);

            if (IdTrabajadorFiltroMovimientos > 0)
            {
                movimientos = movimientos.Where(m => m.IdTrabajadorOperativo == IdTrabajadorFiltroMovimientos);
            }

            foreach (MovimientoTrabajador movimiento in movimientos)
            {
                Movimientos.Add(movimiento);
            }
        }

        private void CargarResumen()
        {
            Resumenes.Clear();

            foreach (ResumenPagoTrabajador resumen in _destajoNegocio.ListarResumenPeriodo(PeriodoSeleccionado?.IdPeriodoPago ?? 0))
            {
                Resumenes.Add(resumen);
            }

            TotalIngresosPeriodo = Resumenes.Sum(r => r.TotalIngresos);
            TotalDescuentosPeriodo = Resumenes.Sum(r => r.TotalDescuentos);
            NetoPeriodo = Resumenes.Sum(r => r.NetoCalculado);
            TotalPagadoPeriodo = Resumenes.Sum(r => r.TotalPagado);
            SaldoPendientePeriodo = Resumenes.Sum(r => r.SaldoPendiente);
        }

        private void CargarPrestamos()
        {
            Prestamos.Clear();

            IEnumerable<PrestamoTrabajador> prestamos = _destajoNegocio.ListarPrestamos();

            if (IdTrabajadorFiltroPrestamos > 0)
            {
                prestamos = prestamos.Where(p => p.IdTrabajadorOperativo == IdTrabajadorFiltroPrestamos);
            }

            foreach (PrestamoTrabajador prestamo in prestamos)
            {
                Prestamos.Add(prestamo);
            }
        }

        private void CargarCuotas()
        {
            Cuotas.Clear();

            int idFiltro = IdTrabajadorFiltroCuotas > 0
                ? IdTrabajadorFiltroCuotas
                : IdTrabajadorFiltroPrestamos;
            int? filtro = idFiltro > 0 ? idFiltro : null;

            foreach (CuotaProgramadaTrabajador cuota in _destajoNegocio.ListarCuotas(filtro))
            {
                Cuotas.Add(cuota);
            }
        }

        private void CargarLotes()
        {
            Lotes.Clear();
            LoteDetalles.Clear();

            int? idPeriodo = PeriodoSeleccionado?.IdPeriodoPago;

            foreach (LotePago lote in _destajoNegocio.ListarLotes(idPeriodo))
            {
                Lotes.Add(lote);
            }
        }

        private void CargarLoteDetalles()
        {
            LoteDetalles.Clear();

            foreach (LotePagoDetalle detalle in _destajoNegocio.ListarLoteDetalles(LoteSeleccionado?.IdLotePago ?? 0))
            {
                LoteDetalles.Add(detalle);
            }
        }

        private void GuardarArea()
        {
            Ejecutar(() => _destajoNegocio.GuardarArea(new AreaOperativa
            {
                IdAreaOperativa = IdAreaOperativa,
                NombreArea = NombreArea,
                Descripcion = DescripcionArea,
                Estado = EstadoArea
            }), () =>
            {
                CargarAreas();
                LimpiarArea();
            });
        }

        private void EliminarArea()
        {
            Ejecutar(() => _destajoNegocio.EliminarArea(IdAreaOperativa), () =>
            {
                CargarAreas();
                LimpiarArea();
            });
        }

        private void LimpiarArea()
        {
            IdAreaOperativa = 0;
            NombreArea = string.Empty;
            DescripcionArea = string.Empty;
            EstadoArea = true;
            AreaSeleccionada = null;
        }

        private void GuardarConcepto()
        {
            Ejecutar(() => _destajoNegocio.GuardarConcepto(new ConceptoMovimiento
            {
                IdConceptoMovimiento = IdConceptoMovimiento,
                CodigoConcepto = CodigoConcepto,
                NombreConcepto = NombreConcepto,
                TipoMovimiento = TipoMovimientoConcepto,
                CategoriaMovimiento = CategoriaMovimientoConcepto,
                TipoCalculo = TipoCalculoConcepto,
                EsDescuento = EsDescuentoConcepto,
                Estado = EstadoConcepto
            }), () =>
            {
                CargarConceptos();
                LimpiarConcepto();
            });
        }

        private void EliminarConcepto()
        {
            Ejecutar(() => _destajoNegocio.EliminarConcepto(IdConceptoMovimiento), () =>
            {
                CargarConceptos();
                LimpiarConcepto();
            });
        }

        private void LimpiarConcepto()
        {
            IdConceptoMovimiento = 0;
            CodigoConcepto = string.Empty;
            NombreConcepto = string.Empty;
            TipoMovimientoConcepto = "Ingreso";
            CategoriaMovimientoConcepto = "Produccion";
            TipoCalculoConcepto = "Cantidad x tarifa";
            EsDescuentoConcepto = false;
            EstadoConcepto = true;
            ConceptoSeleccionado = null;
        }

        private void GuardarOperacion()
        {
            Ejecutar(() => _destajoNegocio.GuardarOperacion(new OperacionTextil
            {
                IdOperacionTextil = IdOperacionTextil,
                CodigoOperacion = CodigoOperacion,
                NombreOperacion = NombreOperacion,
                IdAreaOperativa = IdAreaOperacion > 0 ? IdAreaOperacion : null,
                TipoOperacion = TipoOperacion,
                UnidadMedida = UnidadOperacion,
                TarifaBase = TarifaOperacion,
                Estado = EstadoOperacion
            }), () =>
            {
                CargarOperaciones();
                LimpiarOperacion();
            });
        }

        private void EliminarOperacion()
        {
            Ejecutar(() => _destajoNegocio.EliminarOperacion(IdOperacionTextil), () =>
            {
                CargarOperaciones();
                LimpiarOperacion();
            });
        }

        private void LimpiarOperacion()
        {
            IdOperacionTextil = 0;
            CodigoOperacion = string.Empty;
            NombreOperacion = string.Empty;
            IdAreaOperacion = 0;
            TipoOperacion = "Operacion";
            UnidadOperacion = "Unidad";
            TarifaOperacion = 0;
            EstadoOperacion = true;
            OperacionSeleccionada = null;
        }

        private void GuardarTrabajador()
        {
            Ejecutar(() => _destajoNegocio.GuardarTrabajador(new TrabajadorOperativo
            {
                IdTrabajadorOperativo = IdTrabajadorOperativo,
                IdEmpleado = IdEmpleadoTrabajador,
                TipoTrabajador = TipoTrabajador,
                MedioPagoPreferido = MedioPagoTrabajador,
                NumeroCuenta = NumeroCuentaTrabajador,
                TelefonoPago = TelefonoPagoTrabajador,
                Observacion = ObservacionTrabajador,
                Estado = EstadoTrabajador
            }), () =>
            {
                CargarTrabajadores();
                LimpiarTrabajador();
            });
        }

        private void EliminarTrabajador()
        {
            Ejecutar(() => _destajoNegocio.EliminarTrabajador(IdTrabajadorOperativo), () =>
            {
                CargarTrabajadores();
                LimpiarTrabajador();
            });
        }

        private void LimpiarTrabajador()
        {
            IdTrabajadorOperativo = 0;
            IdEmpleadoTrabajador = 0;
            TipoTrabajador = "Destajo";
            MedioPagoTrabajador = "Efectivo";
            NumeroCuentaTrabajador = string.Empty;
            TelefonoPagoTrabajador = string.Empty;
            ObservacionTrabajador = string.Empty;
            EstadoTrabajador = true;
            TrabajadorSeleccionado = null;
        }

        private void GuardarPeriodo()
        {
            Ejecutar(() => _destajoNegocio.GuardarPeriodo(new PeriodoPago
            {
                IdPeriodoPago = IdPeriodoPago,
                CodigoPeriodo = CodigoPeriodo,
                FechaInicio = FechaInicioPeriodo ?? DateTime.Today,
                FechaFin = FechaFinPeriodo ?? DateTime.Today,
                Estado = EstadoPeriodo,
                Observacion = ObservacionPeriodo
            }), () =>
            {
                CargarPeriodos();
                LimpiarPeriodo();
            });
        }

        private void CambiarEstadoPeriodo(string estado)
        {
            Ejecutar(() => _destajoNegocio.CambiarEstadoPeriodo(PeriodoSeleccionado?.IdPeriodoPago ?? 0, estado, UsuarioActual()), () =>
            {
                CargarPeriodos();
                CargarMovimientos();
                CargarResumen();
            });
        }

        private void LimpiarPeriodo()
        {
            IdPeriodoPago = 0;
            CodigoPeriodo = string.Empty;
            FechaInicioPeriodo = DateTime.Today;
            FechaFinPeriodo = DateTime.Today;
            EstadoPeriodo = "Borrador";
            ObservacionPeriodo = string.Empty;
            PeriodoSeleccionado = null;
        }

        private void GuardarMovimiento()
        {
            Ejecutar(() => _destajoNegocio.GuardarMovimiento(new MovimientoTrabajador
            {
                IdMovimientoTrabajador = IdMovimientoTrabajador,
                IdPeriodoPago = PeriodoSeleccionado?.IdPeriodoPago ?? IdPeriodoPago,
                IdTrabajadorOperativo = IdTrabajadorMovimiento,
                Fecha = FechaMovimiento ?? DateTime.Today,
                TipoMovimiento = TipoMovimientoForm,
                CategoriaMovimiento = CategoriaMovimientoForm,
                IdConceptoMovimiento = IdConceptoMovimientoForm,
                Descripcion = DescripcionMovimiento,
                IdAreaOperativa = IdAreaMovimiento > 0 ? IdAreaMovimiento : null,
                IdOperacionTextil = IdOperacionMovimiento > 0 ? IdOperacionMovimiento : null,
                Cantidad = CantidadMovimiento,
                UnidadMedida = UnidadMovimiento,
                Tarifa = TarifaMovimiento,
                Importe = ImporteMovimiento,
                EsDescuento = EsDescuentoMovimiento,
                EsAutomatico = false,
                OrigenMovimiento = "Manual",
                Estado = EstadoMovimiento,
                Observacion = ObservacionMovimiento,
                ModificadoPor = UsuarioActual()
            }), () =>
            {
                CargarMovimientos();
                CargarResumen();
                LimpiarMovimiento();
            });
        }

        private void EliminarMovimiento()
        {
            Ejecutar(() => _destajoNegocio.EliminarMovimiento(IdMovimientoTrabajador, UsuarioActual()), () =>
            {
                CargarMovimientos();
                CargarResumen();
                LimpiarMovimiento();
            });
        }

        private void LimpiarMovimiento()
        {
            IdMovimientoTrabajador = 0;
            FechaMovimiento = DateTime.Today;
            IdTrabajadorMovimiento = 0;
            IdConceptoMovimientoForm = 0;
            IdAreaMovimiento = 0;
            IdOperacionMovimiento = 0;
            TipoMovimientoForm = "Ingreso";
            CategoriaMovimientoForm = "Produccion";
            DescripcionMovimiento = string.Empty;
            CantidadMovimiento = 0;
            UnidadMovimiento = "Unidad";
            TarifaMovimiento = 0;
            ImporteMovimiento = 0;
            EsDescuentoMovimiento = false;
            EstadoMovimiento = "Borrador";
            ObservacionMovimiento = string.Empty;
            MovimientoSeleccionado = null;
        }

        private void RegistrarPrestamo()
        {
            Ejecutar(() => _destajoNegocio.RegistrarPrestamo(new PrestamoTrabajador
            {
                IdTrabajadorOperativo = IdTrabajadorPrestamo,
                FechaPrestamo = FechaPrestamo ?? DateTime.Today,
                MontoTotal = MontoPrestamo,
                NumeroCuotas = NumeroCuotasPrestamo,
                MontoCuota = MontoCuotaPrestamo,
                Observacion = ObservacionPrestamo
            }, IdConceptoCuota, UsuarioActual()), () =>
            {
                CargarPrestamos();
                CargarCuotas();
                LimpiarPrestamo();
            });
        }

        private void LimpiarPrestamo()
        {
            IdTrabajadorPrestamo = 0;
            FechaPrestamo = DateTime.Today;
            MontoPrestamo = 0;
            NumeroCuotasPrestamo = 1;
            MontoCuotaPrestamo = 0;
            ObservacionPrestamo = string.Empty;
        }

        private void AplicarCuota()
        {
            Ejecutar(() => _destajoNegocio.AplicarCuota(CuotaSeleccionada?.IdCuotaProgramada ?? 0, PeriodoSeleccionado?.IdPeriodoPago ?? 0, UsuarioActual()), () =>
            {
                CargarCuotas();
                CargarMovimientos();
                CargarResumen();
            });
        }

        private void GenerarLote()
        {
            Ejecutar(() => _destajoNegocio.GenerarLotePago(PeriodoSeleccionado?.IdPeriodoPago ?? 0, MedioPagoLote, UsuarioActual(), ObservacionLote), () =>
            {
                CargarLotes();
                CargarResumen();
            });
        }

        private void CambiarEstadoLote()
        {
            Ejecutar(() => _destajoNegocio.CambiarEstadoLote(LoteSeleccionado?.IdLotePago ?? 0, EstadoLote, UsuarioActual()), () =>
            {
                CargarLotes();
                CargarLoteDetalles();
                CargarMovimientos();
                CargarResumen();
            });
        }

        private void RegistrarPagoCompleto()
        {
            decimal monto = ObtenerSaldoPagoSeleccionado();
            MontoPagoLote = monto;
            RegistrarPago(monto);
        }

        private void RegistrarPagoParcial()
        {
            RegistrarPago(MontoPagoLote);
        }

        private void RegistrarPago(decimal monto)
        {
            int idTrabajador = ObtenerIdTrabajadorPagoSeleccionado();

            Ejecutar(() => _destajoNegocio.RegistrarPagoTrabajador(
                PeriodoSeleccionado?.IdPeriodoPago ?? 0,
                idTrabajador,
                LoteDetalleSeleccionado?.IdLotePagoDetalle,
                MedioPagoLote,
                monto,
                ObservacionLote,
                UsuarioActual()), () =>
                {
                    CargarMovimientos();
                    CargarResumen();
                    CargarLotes();
                    CargarLoteDetalles();
                    MontoPagoLote = 0;
                });
        }

        private int ObtenerIdTrabajadorPagoSeleccionado()
        {
            if (LoteDetalleSeleccionado != null)
                return LoteDetalleSeleccionado.IdTrabajadorOperativo;

            return ResumenSeleccionado?.IdTrabajadorOperativo ?? 0;
        }

        private decimal ObtenerSaldoPagoSeleccionado()
        {
            if (LoteDetalleSeleccionado != null)
                return LoteDetalleSeleccionado.MontoPago;

            return ResumenSeleccionado?.SaldoPendiente ?? 0;
        }

        private void DescargarBoletasSeleccionadas(object? parametro)
        {
            DescargarBoletas(ObtenerResumenesSeleccionados(parametro));
        }

        private void DescargarTodasBoletas()
        {
            DescargarBoletas(Resumenes.ToList());
        }

        private void DescargarBoletas(IReadOnlyList<ResumenPagoTrabajador> resumenes)
        {
            if (PeriodoSeleccionado == null)
            {
                NotificationService.Warning("Seleccione un periodo.");
                return;
            }

            if (resumenes.Count == 0)
            {
                NotificationService.Warning("Seleccione uno o mas trabajadores.");
                return;
            }

            try
            {
                string nombreArchivo = CrearNombreBoletas(PeriodoSeleccionado, resumenes);
                SaveFileDialog dialog = new()
                {
                    Title = "Guardar boleta de pago",
                    FileName = nombreArchivo,
                    DefaultExt = ".pdf",
                    Filter = "Archivo PDF (*.pdf)|*.pdf"
                };

                if (dialog.ShowDialog() != true)
                    return;

                // ✅ SE CORRIGIÓ AQUÍ: Se agregó 'true' al final
                BoletaPagoPdfExporter.Exportar(
                    dialog.FileName,
                    PeriodoSeleccionado,
                    resumenes,
                    Movimientos.ToList(),
                    true); // true = Con copia (2 por hoja), false = Sin copia (1 por hoja)

                NotificationService.Success("PDF generado correctamente.");
                AbrirArchivo(dialog.FileName);
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudo generar el PDF: {ex.Message}");
            }
        }

        private List<ResumenPagoTrabajador> ObtenerResumenesSeleccionados(object? parametro)
        {
            List<ResumenPagoTrabajador> resumenes = [];

            if (parametro is IEnumerable elementos)
            {
                foreach (object? elemento in elementos)
                {
                    if (elemento is ResumenPagoTrabajador resumen
                        && resumenes.All(r => r.IdTrabajadorOperativo != resumen.IdTrabajadorOperativo))
                    {
                        resumenes.Add(resumen);
                    }
                }
            }

            if (resumenes.Count == 0 && ResumenSeleccionado != null)
            {
                resumenes.Add(ResumenSeleccionado);
            }

            return resumenes;
        }

        private static string CrearNombreBoletas(PeriodoPago periodo, IReadOnlyList<ResumenPagoTrabajador> resumenes)
        {
            string periodoNombre = LimpiarNombreArchivo(periodo.CodigoPeriodo);

            if (resumenes.Count == 1)
            {
                string trabajador = LimpiarNombreArchivo(resumenes[0].NombreTrabajador);
                return $"Boleta_{periodoNombre}_{trabajador}.pdf";
            }

            return $"Boletas_{periodoNombre}_{resumenes.Count}_trabajadores.pdf";
        }

        private static string LimpiarNombreArchivo(string value)
        {
            string limpio = value.Trim();

            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                limpio = limpio.Replace(invalid, '_');
            }

            return string.IsNullOrWhiteSpace(limpio)
                ? "sin_nombre"
                : limpio;
        }

        private static void AbrirArchivo(string ruta)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = ruta,
                    UseShellExecute = true
                });
            }
            catch
            {
                // El PDF ya fue guardado; abrirlo depende de la configuracion local de Windows.
            }
        }

        private void AplicarConceptoAlMovimiento()
        {
            ConceptoMovimiento? concepto = Conceptos.FirstOrDefault(c => c.IdConceptoMovimiento == IdConceptoMovimientoForm);

            if (concepto == null)
                return;

            TipoMovimientoForm = concepto.TipoMovimiento;
            CategoriaMovimientoForm = concepto.CategoriaMovimiento;
            EsDescuentoMovimiento = concepto.EsDescuento;

            if (string.IsNullOrWhiteSpace(DescripcionMovimiento))
            {
                DescripcionMovimiento = concepto.NombreConcepto;
            }
        }

        private void AplicarOperacionAlMovimiento()
        {
            OperacionTextil? operacion = Operaciones.FirstOrDefault(o => o.IdOperacionTextil == IdOperacionMovimiento);

            if (operacion == null)
                return;

            if (operacion.IdAreaOperativa.HasValue)
                IdAreaMovimiento = operacion.IdAreaOperativa.Value;

            UnidadMovimiento = operacion.UnidadMedida;

            if (operacion.TarifaBase > 0)
                TarifaMovimiento = operacion.TarifaBase;
        }

        private void RecalcularCuotaPrestamo()
        {
            if (MontoPrestamo > 0 && NumeroCuotasPrestamo > 0)
            {
                MontoCuotaPrestamo = Math.Round(MontoPrestamo / NumeroCuotasPrestamo, 2);
            }
        }

        private void Ejecutar(Func<string> accion, Action refrescar)
        {
            try
            {
                string mensaje = accion();

                if (EsMensajeExitoso(mensaje))
                {
                    NotificationService.Success(mensaje);
                    refrescar();
                    return;
                }

                NotificationService.Warning(mensaje);
            }
            catch (Exception ex)
            {
                NotificationService.Error(ex.Message);
            }
        }

        private static bool EsMensajeExitoso(string mensaje)
        {
            return mensaje.Contains("correctamente", StringComparison.OrdinalIgnoreCase)
                || mensaje.Contains("generado", StringComparison.OrdinalIgnoreCase)
                || mensaje.Contains("aplicada", StringComparison.OrdinalIgnoreCase)
                || mensaje.Contains("registrado", StringComparison.OrdinalIgnoreCase);
        }

        private static string UsuarioActual()
        {
            return SessionManager.UsuarioActual?.NombreUsuario ?? "Sistema";
        }
    }
}
