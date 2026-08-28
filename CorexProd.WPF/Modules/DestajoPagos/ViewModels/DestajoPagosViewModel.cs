using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.Modules.Seguridad.ViewModels;
using CorexProd.WPF.Modules.Seguridad.Views;
using CorexProd.WPF.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.DestajoPagos.ViewModels
{
    public class DestajoPagosViewModel : BaseViewModel
    {
        private readonly DestajoPagosNegocio _destajoNegocio = new();
        private readonly EmpleadoNegocio _empleadoNegocio = new();
        private readonly EmpresaNegocio _empresaNegocio = new();

        private AreaOperativa? _areaSeleccionada;
        private ConceptoMovimiento? _conceptoSeleccionado;
        private OperacionTextil? _operacionSeleccionada;
        private TrabajadorOperativo? _trabajadorSeleccionado;
        private PeriodoPago? _periodoSeleccionado;
        private MovimientoTrabajador? _movimientoSeleccionado;
        private PrestamoTrabajador? _prestamoSeleccionado;
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
        private DateTime? _fechaInicioTarifaOperacion = DateTime.Today;
        private DateTime? _fechaFinTarifaOperacion;
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
        private int _numeroSemanaPeriodo;
        private int _anioPeriodo;
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
        private int _idAreaFiltroMovimientos;
        private int _idOperacionFiltroMovimientos;
        private int _idTrabajadorFiltroPrestamos;
        private DateTime? _fechaPrestamo = DateTime.Today;
        private DateTime? _fechaInicioDescuentoPrestamo = DateTime.Today;
        private decimal _montoPrestamo;
        private int _numeroCuotasPrestamo = 1;
        private decimal _montoCuotaPrestamo;
        private int _idConceptoCuota;
        private string _observacionPrestamo = string.Empty;
        private int _idTrabajadorFiltroCuotas;
        private DateTime? _fechaPagoExtraordinario = DateTime.Today;
        private decimal _montoPagoExtraordinario;
        private DateTime? _fechaReprogramacionCuota = DateTime.Today;
        private decimal _montoReprogramacionCuota;
        private string _observacionOperacionPrestamo = string.Empty;

        private string _medioPagoLote = "Efectivo";
        private string _medioPagoLote2 = string.Empty;
        private string _estadoLote = "Generado";
        private string _observacionLote = string.Empty;
        private string _numeroOperacionPago = string.Empty;
        private string _motivoAnulacionPago = string.Empty;
        private string _autorizadoPorAnulacionPago = string.Empty;
        private decimal _montoPagoLote;
        private decimal _montoPagoLote2;
        private DateTime? _fechaPagoLote = DateTime.Today;
        private PagoTrabajador? _pagoSeleccionado;
        private DashboardDestajoIndicador _dashboard = new();

        public ObservableCollection<AreaOperativa> Areas { get; } = [];
        public ObservableCollection<ConceptoMovimiento> Conceptos { get; } = [];
        public ObservableCollection<ConceptoMovimiento> ConceptosDescuento { get; } = [];
        public ObservableCollection<OperacionTextil> Operaciones { get; } = [];
        public ObservableCollection<TrabajadorOperativo> Trabajadores { get; } = [];
        public ObservableCollection<PeriodoPago> Periodos { get; } = [];
        public ObservableCollection<MovimientoTrabajador> Movimientos { get; } = [];
        public ObservableCollection<ResumenPagoTrabajador> Resumenes { get; } = [];
        public ObservableCollection<AlertaCalculoPeriodo> AlertasCalculo { get; } = [];
        public ObservableCollection<PrestamoTrabajador> Prestamos { get; } = [];
        public ObservableCollection<CuotaProgramadaTrabajador> Cuotas { get; } = [];
        public ObservableCollection<LotePago> Lotes { get; } = [];
        public ObservableCollection<LotePagoDetalle> LoteDetalles { get; } = [];
        public ObservableCollection<PagoTrabajador> Pagos { get; } = [];
        public ObservableCollection<Empleado> Empleados { get; } = [];
        public ObservableCollection<DashboardDestajoSerie> ProduccionDiaria { get; } = [];
        public ObservableCollection<DashboardDestajoSerie> ProduccionPorTrabajador { get; } = [];
        public ObservableCollection<DashboardDestajoSerie> ProduccionPorArea { get; } = [];
        public ObservableCollection<DashboardDestajoSerie> ComparativoSemanal { get; } = [];
        public ObservableCollection<DashboardDestajoSerie> PagosPorMedio { get; } = [];
        public ObservableCollection<DashboardDestajoSerie> EvolucionSaldos { get; } = [];
        public ObservableCollection<AuditoriaDestajo> Auditorias { get; } = [];

        public ObservableCollection<string> TiposTrabajador { get; } =
        [
            "Destajo",
            "Horario fijo",
            "Staff",
            "Mixto",
            "Asistente",
            "Supervisor / Jefatura",
            "Apoyo / Cocina / Servicios"
        ];

        public ObservableCollection<string> MediosPago { get; } =
        [
            "BCP",
            "Interbank",
            "Yape",
            "Plin",
            "Efectivo",
            "Transferencia",
            "Cheque",
            "Mixto"
        ];

        public ObservableCollection<string> UnidadesMedidaDestajo { get; } =
        [
            "Unidad",
            "Prenda",
            "Docena",
            "Hora",
            "Dia",
            "Metro",
            "Kilo"
        ];

        public ObservableCollection<string> EstadosPeriodo { get; } =
        [
            "Borrador",
            "Abierto",
            "En calculo",
            "Calculado",
            "En pago",
            "Cerrado",
            "Anulado"
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
            "Produccion por destajo",
            "Ingreso adicional",
            "Bonificacion",
            "Descuento manual",
            "Ajuste positivo",
            "Ajuste negativo",
            "Horas o jornadas",
            "Reintegro",
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
            "Borrador",
            "Pendiente",
            "Pago Parcial",
            "Pagado / Cerrado",
            "Anulado"
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
            "Generado",
            "Pendiente",
            "Parcial",
            "Pago Parcial",
            "Pagado",
            "Pagado / Cerrado",
            "Anulado"
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

        public DateTime? FechaInicioTarifaOperacion
        {
            get => _fechaInicioTarifaOperacion;
            set { _fechaInicioTarifaOperacion = value; OnPropertyChanged(); }
        }

        public DateTime? FechaFinTarifaOperacion
        {
            get => _fechaFinTarifaOperacion;
            set { _fechaFinTarifaOperacion = value; OnPropertyChanged(); }
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
                FechaInicioTarifaOperacion = value.FechaInicioVigencia;
                FechaFinTarifaOperacion = value.FechaFinVigencia;
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

        public int NumeroSemanaPeriodo
        {
            get => _numeroSemanaPeriodo;
            set { _numeroSemanaPeriodo = value; OnPropertyChanged(); }
        }

        public int AnioPeriodo
        {
            get => _anioPeriodo;
            set { _anioPeriodo = value; OnPropertyChanged(); }
        }

        public DateTime? FechaInicioPeriodo
        {
            get => _fechaInicioPeriodo;
            set
            {
                _fechaInicioPeriodo = value;
                OnPropertyChanged();
                ActualizarSemanaPeriodoDesdeFechas();
            }
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

        public DashboardDestajoIndicador Dashboard
        {
            get => _dashboard;
            set { _dashboard = value; OnPropertyChanged(); }
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
                NumeroSemanaPeriodo = value.NumeroSemana;
                AnioPeriodo = value.Anio;
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
                CargarAlertasCalculo();
                CargarLotes();
                CargarPagos();
                CargarDashboard();
                CargarAuditoria();
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
            set
            {
                _fechaMovimiento = value;
                OnPropertyChanged();
                AplicarOperacionAlMovimiento();
            }
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
                ActualizarImporteMovimiento();
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
                ActualizarImporteMovimiento();
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

        public int IdAreaFiltroMovimientos
        {
            get => _idAreaFiltroMovimientos;
            set { _idAreaFiltroMovimientos = value; OnPropertyChanged(); }
        }

        public int IdOperacionFiltroMovimientos
        {
            get => _idOperacionFiltroMovimientos;
            set { _idOperacionFiltroMovimientos = value; OnPropertyChanged(); }
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

        public DateTime? FechaInicioDescuentoPrestamo
        {
            get => _fechaInicioDescuentoPrestamo;
            set { _fechaInicioDescuentoPrestamo = value; OnPropertyChanged(); }
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

        public DateTime? FechaPagoExtraordinario
        {
            get => _fechaPagoExtraordinario;
            set { _fechaPagoExtraordinario = value; OnPropertyChanged(); }
        }

        public decimal MontoPagoExtraordinario
        {
            get => _montoPagoExtraordinario;
            set { _montoPagoExtraordinario = value; OnPropertyChanged(); }
        }

        public DateTime? FechaReprogramacionCuota
        {
            get => _fechaReprogramacionCuota;
            set { _fechaReprogramacionCuota = value; OnPropertyChanged(); }
        }

        public decimal MontoReprogramacionCuota
        {
            get => _montoReprogramacionCuota;
            set { _montoReprogramacionCuota = value; OnPropertyChanged(); }
        }

        public string ObservacionOperacionPrestamo
        {
            get => _observacionOperacionPrestamo;
            set { _observacionOperacionPrestamo = value; OnPropertyChanged(); }
        }

        public int IdTrabajadorFiltroCuotas
        {
            get => _idTrabajadorFiltroCuotas;
            set { _idTrabajadorFiltroCuotas = value; OnPropertyChanged(); }
        }

        public CuotaProgramadaTrabajador? CuotaSeleccionada
        {
            get => _cuotaSeleccionada;
            set
            {
                _cuotaSeleccionada = value;
                OnPropertyChanged();

                if (value == null)
                    return;

                FechaReprogramacionCuota = value.FechaProgramada;
                MontoReprogramacionCuota = value.MontoCuota;
            }
        }

        public PrestamoTrabajador? PrestamoSeleccionado
        {
            get => _prestamoSeleccionado;
            set { _prestamoSeleccionado = value; OnPropertyChanged(); }
        }

        public string MedioPagoLote
        {
            get => _medioPagoLote;
            set { _medioPagoLote = value; OnPropertyChanged(); }
        }

        public string MedioPagoLote2
        {
            get => _medioPagoLote2;
            set { _medioPagoLote2 = value; OnPropertyChanged(); }
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

        public decimal MontoPagoLote2
        {
            get => _montoPagoLote2;
            set { _montoPagoLote2 = value; OnPropertyChanged(); }
        }

        public DateTime? FechaPagoLote
        {
            get => _fechaPagoLote;
            set { _fechaPagoLote = value; OnPropertyChanged(); }
        }

        public string NumeroOperacionPago
        {
            get => _numeroOperacionPago;
            set { _numeroOperacionPago = value; OnPropertyChanged(); }
        }

        public string MotivoAnulacionPago
        {
            get => _motivoAnulacionPago;
            set { _motivoAnulacionPago = value; OnPropertyChanged(); }
        }

        public string AutorizadoPorAnulacionPago
        {
            get => _autorizadoPorAnulacionPago;
            set { _autorizadoPorAnulacionPago = value; OnPropertyChanged(); }
        }

        public PagoTrabajador? PagoSeleccionado
        {
            get => _pagoSeleccionado;
            set { _pagoSeleccionado = value; OnPropertyChanged(); }
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
        public ICommand NuevoEmpleadoCommand { get; }
        public ICommand GuardarPeriodoCommand { get; }
        public ICommand LimpiarPeriodoCommand { get; }
        public ICommand CrearSemanaActualCommand { get; }
        public ICommand PrepararSemanaSiguienteCommand { get; }
        public ICommand CambiarEstadoPeriodoCommand { get; }
        public ICommand GuardarMovimientoCommand { get; }
        public ICommand LimpiarMovimientoCommand { get; }
        public ICommand EliminarMovimientoCommand { get; }
        public ICommand DuplicarMovimientoCommand { get; }
        public ICommand ImportarMovimientosCommand { get; }
        public ICommand ExportarMovimientosCommand { get; }
        public ICommand RegistrarPrestamoCommand { get; }
        public ICommand LimpiarPrestamoCommand { get; }
        public ICommand FiltrarCuotasCommand { get; }
        public ICommand FiltrarMovimientosCommand { get; }
        public ICommand FiltrarPrestamosCommand { get; }
        public ICommand LimpiarFiltroMovimientosCommand { get; }
        public ICommand LimpiarFiltroPrestamosCommand { get; }
        public ICommand AplicarCuotaCommand { get; }
        public ICommand RegistrarPagoExtraordinarioCommand { get; }
        public ICommand SuspenderCuotaCommand { get; }
        public ICommand ReprogramarCuotaCommand { get; }
        public ICommand CancelarPrestamoCommand { get; }
        public ICommand GenerarLoteCommand { get; }
        public ICommand CambiarEstadoLoteCommand { get; }
        public ICommand RegistrarPagoCompletoCommand { get; }
        public ICommand RegistrarPagoParcialCommand { get; }
        public ICommand RegistrarPagosSeleccionadosCommand { get; }
        public ICommand AnularPagoCommand { get; }
        public ICommand DescargarBoletasSeleccionadasCommand { get; }
        public ICommand DescargarTodasBoletasCommand { get; }
        public ICommand ExportarResumenPeriodoCommand { get; }
        public ICommand ExportarReporteOperativoCommand { get; }
        public ICommand ExportarReportePagosCommand { get; }
        public ICommand ExportarReportePrestamosCommand { get; }
        public ICommand CalcularPeriodoCommand { get; }
        public ICommand RecalcularTrabajadorCommand { get; }
        public ICommand ConfirmarCalculoPeriodoCommand { get; }
        public ICommand CerrarPeriodoCommand { get; }

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
            NuevoEmpleadoCommand = new RelayCommand(_ => AbrirNuevoEmpleado());
            GuardarPeriodoCommand = new RelayCommand(_ => GuardarPeriodo());
            LimpiarPeriodoCommand = new RelayCommand(_ => LimpiarPeriodo());
            CrearSemanaActualCommand = new RelayCommand(_ => CrearSemana(DateTime.Today));
            PrepararSemanaSiguienteCommand = new RelayCommand(_ => PrepararSemana(DateTime.Today.AddDays(7)));
            CambiarEstadoPeriodoCommand = new RelayCommand(parametro => CambiarEstadoPeriodo(parametro?.ToString() ?? string.Empty));
            GuardarMovimientoCommand = new RelayCommand(_ => GuardarMovimiento());
            LimpiarMovimientoCommand = new RelayCommand(_ => LimpiarMovimiento());
            EliminarMovimientoCommand = new RelayCommand(_ => EliminarMovimiento());
            DuplicarMovimientoCommand = new RelayCommand(_ => DuplicarMovimiento());
            ImportarMovimientosCommand = new RelayCommand(_ => ImportarMovimientos());
            ExportarMovimientosCommand = new RelayCommand(_ => ExportarMovimientos());
            RegistrarPrestamoCommand = new RelayCommand(_ => RegistrarPrestamo());
            LimpiarPrestamoCommand = new RelayCommand(_ => LimpiarPrestamo());
            FiltrarCuotasCommand = new RelayCommand(_ => CargarCuotas());
            FiltrarMovimientosCommand = new RelayCommand(_ => CargarMovimientos());
            FiltrarPrestamosCommand = new RelayCommand(_ => CargarPrestamos());
            LimpiarFiltroMovimientosCommand = new RelayCommand(_ =>
            {
                IdTrabajadorFiltroMovimientos = 0;
                IdAreaFiltroMovimientos = 0;
                IdOperacionFiltroMovimientos = 0;
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
            RegistrarPagoExtraordinarioCommand = new RelayCommand(_ => RegistrarPagoExtraordinario());
            SuspenderCuotaCommand = new RelayCommand(_ => SuspenderCuota());
            ReprogramarCuotaCommand = new RelayCommand(_ => ReprogramarCuota());
            CancelarPrestamoCommand = new RelayCommand(_ => CancelarPrestamo());
            GenerarLoteCommand = new RelayCommand(_ => GenerarLote());
            CambiarEstadoLoteCommand = new RelayCommand(_ => CambiarEstadoLote());
            RegistrarPagoCompletoCommand = new RelayCommand(_ => RegistrarPagoCompleto());
            RegistrarPagoParcialCommand = new RelayCommand(_ => RegistrarPagoParcial());
            RegistrarPagosSeleccionadosCommand = new RelayCommand(RegistrarPagosSeleccionados);
            AnularPagoCommand = new RelayCommand(_ => AnularPago());
            DescargarBoletasSeleccionadasCommand = new RelayCommand(DescargarBoletasSeleccionadas);
            DescargarTodasBoletasCommand = new RelayCommand(_ => DescargarTodasBoletas());
            ExportarResumenPeriodoCommand = new RelayCommand(_ => ExportarResumenPeriodo());
            ExportarReporteOperativoCommand = new RelayCommand(_ => ExportarReporteOperativo());
            ExportarReportePagosCommand = new RelayCommand(_ => ExportarReportePagos());
            ExportarReportePrestamosCommand = new RelayCommand(_ => ExportarReportePrestamos());
            CalcularPeriodoCommand = new RelayCommand(_ => CalcularPeriodo());
            RecalcularTrabajadorCommand = new RelayCommand(_ => RecalcularTrabajador());
            ConfirmarCalculoPeriodoCommand = new RelayCommand(_ => ConfirmarCalculoPeriodo());
            CerrarPeriodoCommand = new RelayCommand(_ => CerrarPeriodo());

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
                    CargarPagos();
                    CargarDashboard();
                    CargarAuditoria();
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

            if (IdAreaFiltroMovimientos > 0)
            {
                movimientos = movimientos.Where(m => m.IdAreaOperativa == IdAreaFiltroMovimientos);
            }

            if (IdOperacionFiltroMovimientos > 0)
            {
                movimientos = movimientos.Where(m => m.IdOperacionTextil == IdOperacionFiltroMovimientos);
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
            CargarDashboard();
        }

        private void CargarAlertasCalculo()
        {
            AlertasCalculo.Clear();

            foreach (AlertaCalculoPeriodo alerta in _destajoNegocio.ListarAlertasCalculoPeriodo(PeriodoSeleccionado?.IdPeriodoPago ?? 0))
            {
                AlertasCalculo.Add(alerta);
            }
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

        private void CargarPagos()
        {
            Pagos.Clear();

            foreach (PagoTrabajador pago in _destajoNegocio.ListarPagos(PeriodoSeleccionado?.IdPeriodoPago))
            {
                Pagos.Add(pago);
            }

            CargarAuditoria();
        }

        private void CargarDashboard()
        {
            Dashboard = _destajoNegocio.ObtenerDashboard(PeriodoSeleccionado?.IdPeriodoPago ?? 0);

            ProduccionDiaria.Clear();
            ProduccionPorTrabajador.Clear();
            ProduccionPorArea.Clear();
            ComparativoSemanal.Clear();
            PagosPorMedio.Clear();
            EvolucionSaldos.Clear();

            foreach (DashboardDestajoSerie serie in _destajoNegocio.ListarDashboardSeries(PeriodoSeleccionado?.IdPeriodoPago ?? 0))
            {
                switch (serie.Categoria)
                {
                    case "Produccion diaria":
                        ProduccionDiaria.Add(serie);
                        break;
                    case "Produccion por trabajador":
                        ProduccionPorTrabajador.Add(serie);
                        break;
                    case "Produccion por area":
                        ProduccionPorArea.Add(serie);
                        break;
                    case "Comparativo semanal":
                        ComparativoSemanal.Add(serie);
                        break;
                    case "Pagos por medio":
                        PagosPorMedio.Add(serie);
                        break;
                    case "Evolucion saldos":
                        EvolucionSaldos.Add(serie);
                        break;
                }
            }
        }

        private void CargarAuditoria()
        {
            Auditorias.Clear();

            foreach (AuditoriaDestajo auditoria in _destajoNegocio.ListarAuditoriaDestajo(PeriodoSeleccionado?.IdPeriodoPago))
            {
                Auditorias.Add(auditoria);
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
                FechaInicioVigencia = FechaInicioTarifaOperacion,
                FechaFinVigencia = FechaFinTarifaOperacion,
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
            FechaInicioTarifaOperacion = DateTime.Today;
            FechaFinTarifaOperacion = null;
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

        private void AbrirNuevoEmpleado()
        {
            int[] empleadosActuales = Empleados.Select(e => e.IdEmpleado).ToArray();
            EmpleadosViewModel viewModel = new();
            EmpleadoEditorWindow ventana = new()
            {
                DataContext = viewModel,
                Owner = Application.Current.MainWindow
            };

            viewModel.CerrarVentana = ventana.Close;
            ventana.ShowDialog();

            if (!viewModel.Guardado)
                return;

            CargarEmpleados();

            Empleado? empleadoCreado = Empleados
                .Where(e => !empleadosActuales.Contains(e.IdEmpleado))
                .OrderByDescending(e => e.IdEmpleado)
                .FirstOrDefault();

            if (empleadoCreado != null)
            {
                IdEmpleadoTrabajador = empleadoCreado.IdEmpleado;
            }
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
                NumeroSemana = NumeroSemanaPeriodo,
                Anio = AnioPeriodo,
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

        private void CrearSemana(DateTime fechaReferencia)
        {
            PrepararSemana(fechaReferencia);
            string codigoCreado = CodigoPeriodo;

            Ejecutar(() => _destajoNegocio.GuardarPeriodo(new PeriodoPago
            {
                IdPeriodoPago = 0,
                CodigoPeriodo = CodigoPeriodo,
                NumeroSemana = NumeroSemanaPeriodo,
                Anio = AnioPeriodo,
                FechaInicio = FechaInicioPeriodo ?? DateTime.Today,
                FechaFin = FechaFinPeriodo ?? DateTime.Today,
                Estado = "Borrador",
                Observacion = ObservacionPeriodo
            }), () =>
            {
                CargarPeriodos();
                PeriodoSeleccionado = Periodos.FirstOrDefault(p => p.CodigoPeriodo == codigoCreado)
                    ?? PeriodoSeleccionado;
            });
        }

        private void PrepararSemana(DateTime fechaReferencia)
        {
            DateTime inicio = InicioSemana(fechaReferencia);
            DateTime fin = inicio.AddDays(6);
            int semana = ISOWeek.GetWeekOfYear(inicio);
            int anio = ISOWeek.GetYear(inicio);

            IdPeriodoPago = 0;
            NumeroSemanaPeriodo = semana;
            AnioPeriodo = anio;
            CodigoPeriodo = $"SEM-{anio}-{semana:00}";
            FechaInicioPeriodo = inicio;
            FechaFinPeriodo = fin;
            EstadoPeriodo = "Borrador";
            ObservacionPeriodo = $"Semana de trabajo del {inicio:dd/MM/yyyy} al {fin:dd/MM/yyyy}";
            PeriodoSeleccionado = null;
        }

        private static DateTime InicioSemana(DateTime fecha)
        {
            int delta = ((int)fecha.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            return fecha.Date.AddDays(-delta);
        }

        private void ActualizarSemanaPeriodoDesdeFechas()
        {
            if (!FechaInicioPeriodo.HasValue)
                return;

            int semana = ISOWeek.GetWeekOfYear(FechaInicioPeriodo.Value);
            int anio = ISOWeek.GetYear(FechaInicioPeriodo.Value);

            NumeroSemanaPeriodo = semana;
            AnioPeriodo = anio;

            if (string.IsNullOrWhiteSpace(CodigoPeriodo) || CodigoPeriodo.StartsWith("SEM-", StringComparison.OrdinalIgnoreCase))
            {
                CodigoPeriodo = $"SEM-{anio}-{semana:00}";
            }
        }

        private void CambiarEstadoPeriodo(string estado)
        {
            if (PeriodoSeleccionado?.Estado is "Cerrado" or "Anulado" or "Calculado"
                && estado.Equals("Abierto", StringComparison.OrdinalIgnoreCase)
                && !ConfirmDialogService.Confirmar("Reabrir un periodo requiere autorización. ¿Desea continuar?", "Autorizar reapertura"))
            {
                return;
            }

            Ejecutar(() => _destajoNegocio.CambiarEstadoPeriodo(PeriodoSeleccionado?.IdPeriodoPago ?? 0, estado, UsuarioActual()), () =>
            {
                CargarPeriodos();
                CargarMovimientos();
                CargarResumen();
                CargarAlertasCalculo();
            });
        }

        private void CalcularPeriodo()
        {
            Ejecutar(() => _destajoNegocio.CalcularPeriodo(PeriodoSeleccionado?.IdPeriodoPago ?? 0, UsuarioActual()), () =>
            {
                CargarPeriodos();
                CargarResumen();
                CargarAlertasCalculo();
            });
        }

        private void RecalcularTrabajador()
        {
            Ejecutar(() => _destajoNegocio.RecalcularTrabajador(
                PeriodoSeleccionado?.IdPeriodoPago ?? 0,
                ResumenSeleccionado?.IdTrabajadorOperativo ?? 0,
                UsuarioActual()), () =>
            {
                CargarResumen();
                CargarAlertasCalculo();
            });
        }

        private void ConfirmarCalculoPeriodo()
        {
            if (!ConfirmDialogService.Confirmar("Al confirmar el calculo se bloquearan los movimientos del periodo. ¿Desea continuar?", "Confirmar calculo"))
                return;

            Ejecutar(() => _destajoNegocio.ConfirmarCalculoPeriodo(PeriodoSeleccionado?.IdPeriodoPago ?? 0, UsuarioActual()), () =>
            {
                CargarPeriodos();
                CargarMovimientos();
                CargarResumen();
                CargarAlertasCalculo();
            });
        }

        private void CerrarPeriodo()
        {
            if (!ConfirmDialogService.Confirmar("El cierre validara movimientos, calculos, pagos, boletas y saldos pendientes. Al cerrar se bloquearan modificaciones. Desea continuar?", "Cerrar periodo"))
                return;

            Ejecutar(() => _destajoNegocio.CerrarPeriodo(PeriodoSeleccionado?.IdPeriodoPago ?? 0, UsuarioActual()), () =>
            {
                CargarPeriodos();
                CargarMovimientos();
                CargarResumen();
                CargarAlertasCalculo();
                CargarLotes();
                CargarPagos();
                CargarAuditoria();
            });
        }

        private void LimpiarPeriodo()
        {
            IdPeriodoPago = 0;
            CodigoPeriodo = string.Empty;
            NumeroSemanaPeriodo = 0;
            AnioPeriodo = 0;
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
                Importe = TotalMovimientoCalculado,
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

        private void DuplicarMovimiento()
        {
            MovimientoTrabajador? movimiento = MovimientoSeleccionado;

            if (movimiento == null)
            {
                NotificationService.Warning("Seleccione un movimiento para duplicar.");
                return;
            }

            IdMovimientoTrabajador = 0;
            FechaMovimiento = movimiento.Fecha;
            IdTrabajadorMovimiento = movimiento.IdTrabajadorOperativo;
            IdConceptoMovimientoForm = movimiento.IdConceptoMovimiento;
            IdAreaMovimiento = movimiento.IdAreaOperativa ?? 0;
            IdOperacionMovimiento = movimiento.IdOperacionTextil ?? 0;
            TipoMovimientoForm = movimiento.TipoMovimiento;
            CategoriaMovimientoForm = movimiento.CategoriaMovimiento;
            DescripcionMovimiento = movimiento.Descripcion;
            CantidadMovimiento = movimiento.Cantidad;
            UnidadMovimiento = movimiento.UnidadMedida;
            TarifaMovimiento = movimiento.Tarifa;
            ImporteMovimiento = movimiento.Importe;
            EsDescuentoMovimiento = movimiento.EsDescuento;
            EstadoMovimiento = "Borrador";
            ObservacionMovimiento = $"Copia de movimiento {movimiento.IdMovimientoTrabajador}";
            MovimientoSeleccionado = null;

            NotificationService.Info("Movimiento copiado. Revise los datos y presione Guardar.");
        }

        private void ImportarMovimientos()
        {
            if (PeriodoSeleccionado == null)
            {
                NotificationService.Warning("Seleccione un periodo.");
                return;
            }

            OpenFileDialog dialog = new()
            {
                Title = "Importar movimientos desde Excel",
                Filter = "CSV de Excel (*.csv)|*.csv"
            };

            if (dialog.ShowDialog() != true)
                return;

            int procesados = 0;
            List<string> errores = [];

            foreach (string linea in File.ReadLines(dialog.FileName).Skip(1))
            {
                if (string.IsNullOrWhiteSpace(linea))
                    continue;

                List<string> columnas = SepararCsv(linea);

                if (columnas.Count < 14)
                {
                    errores.Add($"Linea {procesados + errores.Count + 2}: columnas incompletas.");
                    continue;
                }

                MovimientoTrabajador? movimiento = CrearMovimientoImportado(columnas, out string error);

                if (movimiento == null)
                {
                    errores.Add(error);
                    continue;
                }

                string mensaje = _destajoNegocio.GuardarMovimiento(movimiento);

                if (EsMensajeExitoso(mensaje))
                {
                    procesados++;
                    continue;
                }

                errores.Add($"{movimiento.NombreTrabajador}: {mensaje}");
            }

            CargarMovimientos();
            CargarResumen();

            if (errores.Count == 0)
            {
                NotificationService.Success($"Movimientos importados correctamente: {procesados}.");
                return;
            }

            NotificationService.Warning($"Importados: {procesados}. Observados: {errores.Count}. {string.Join(" | ", errores.Take(3))}");
        }

        private MovimientoTrabajador? CrearMovimientoImportado(IReadOnlyList<string> columnas, out string error)
        {
            error = string.Empty;

            DateTime fecha = ParseFecha(columnas[0], PeriodoSeleccionado?.FechaInicio ?? DateTime.Today);
            string trabajadorNombre = columnas[1].Trim();
            string conceptoNombre = columnas[2].Trim();
            string tipo = string.IsNullOrWhiteSpace(columnas[3]) ? "Ingreso" : columnas[3].Trim();
            string categoria = string.IsNullOrWhiteSpace(columnas[4]) ? "Produccion por destajo" : columnas[4].Trim();
            string areaNombre = columnas[5].Trim();
            string operacionNombre = columnas[6].Trim();

            TrabajadorOperativo? trabajador = Trabajadores.FirstOrDefault(t => EsIgual(t.NombreTrabajador, trabajadorNombre));
            ConceptoMovimiento? concepto = Conceptos.FirstOrDefault(c => EsIgual(c.NombreConcepto, conceptoNombre));
            AreaOperativa? area = Areas.FirstOrDefault(a => EsIgual(a.NombreArea, areaNombre));
            OperacionTextil? operacion = Operaciones.FirstOrDefault(o => EsIgual(o.NombreOperacion, operacionNombre));

            if (trabajador == null)
            {
                error = $"Trabajador no encontrado: {trabajadorNombre}.";
                return null;
            }

            if (concepto == null)
            {
                error = $"Concepto no encontrado: {conceptoNombre}.";
                return null;
            }

            return new MovimientoTrabajador
            {
                IdPeriodoPago = PeriodoSeleccionado?.IdPeriodoPago ?? 0,
                IdTrabajadorOperativo = trabajador.IdTrabajadorOperativo,
                NombreTrabajador = trabajador.NombreTrabajador,
                Fecha = fecha,
                TipoMovimiento = tipo,
                CategoriaMovimiento = categoria,
                IdConceptoMovimiento = concepto.IdConceptoMovimiento,
                Descripcion = concepto.NombreConcepto,
                IdAreaOperativa = area?.IdAreaOperativa,
                IdOperacionTextil = operacion?.IdOperacionTextil,
                Cantidad = ParseDecimal(columnas[7]),
                UnidadMedida = string.IsNullOrWhiteSpace(columnas[8]) ? "Unidad" : columnas[8].Trim(),
                Tarifa = ParseDecimal(columnas[9]),
                Importe = ParseDecimal(columnas[10]),
                EsDescuento = EsVerdadero(columnas[11]),
                EsAutomatico = false,
                OrigenMovimiento = "Importacion Excel",
                Estado = string.IsNullOrWhiteSpace(columnas[12]) ? "Borrador" : columnas[12].Trim(),
                Observacion = columnas[13].Trim(),
                ModificadoPor = UsuarioActual()
            };
        }

        private void ExportarMovimientos()
        {
            if (Movimientos.Count == 0)
            {
                NotificationService.Warning("No hay movimientos para exportar.");
                return;
            }

            SaveFileDialog dialog = new()
            {
                Title = "Exportar movimientos operativos",
                FileName = $"Movimientos_{PeriodoSeleccionado?.CodigoPeriodo ?? "periodo"}.csv",
                DefaultExt = ".csv",
                Filter = "Archivo CSV para Excel (*.csv)|*.csv"
            };

            if (dialog.ShowDialog() != true)
                return;

            List<string> lineas =
            [
                "Fecha;Trabajador;Concepto;Tipo;Categoria;Area;Operacion;Cantidad;Unidad;Tarifa;Importe;Descuento;Estado;Observacion"
            ];

            foreach (MovimientoTrabajador movimiento in Movimientos)
            {
                lineas.Add(string.Join(";",
                    Csv(movimiento.Fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)),
                    Csv(movimiento.NombreTrabajador),
                    Csv(movimiento.NombreConcepto),
                    Csv(movimiento.TipoMovimiento),
                    Csv(movimiento.CategoriaMovimiento),
                    Csv(movimiento.NombreArea),
                    Csv(movimiento.NombreOperacion),
                    movimiento.Cantidad.ToString("0.###", CultureInfo.InvariantCulture),
                    Csv(movimiento.UnidadMedida),
                    movimiento.Tarifa.ToString("0.####", CultureInfo.InvariantCulture),
                    movimiento.Importe.ToString("0.##", CultureInfo.InvariantCulture),
                    movimiento.EsDescuento ? "SI" : "NO",
                    Csv(movimiento.Estado),
                    Csv(movimiento.Observacion)));
            }

            File.WriteAllLines(dialog.FileName, lineas, System.Text.Encoding.UTF8);
            NotificationService.Success("Movimientos exportados correctamente.");
            AbrirArchivo(dialog.FileName);
        }

        private void ExportarResumenPeriodo()
        {
            if (Resumenes.Count == 0)
            {
                NotificationService.Warning("No hay resumen de periodo para exportar.");
                return;
            }

            ExportarCsv(
                $"ResumenPeriodo_{PeriodoSeleccionado?.CodigoPeriodo ?? "periodo"}.csv",
                "Resumen financiero del periodo",
                [
                    "Trabajador;Documento;Tipo;Medio preferido;Saldo anterior;Ingresos;Descuentos;Neto periodo;Total por pagar;Total pagado;Saldo pendiente;Estado pago;Estado calculo"
                ],
                Resumenes.Select(r => string.Join(";",
                    Csv(r.NombreTrabajador),
                    Csv(r.Documento),
                    Csv(r.TipoTrabajador),
                    Csv(r.MedioPagoPreferido),
                    NumeroCsv(r.SaldoAnterior),
                    NumeroCsv(r.TotalIngresos),
                    NumeroCsv(r.TotalDescuentos),
                    NumeroCsv(r.NetoCalculado),
                    NumeroCsv(r.TotalPorPagar),
                    NumeroCsv(r.TotalPagado),
                    NumeroCsv(r.SaldoPendiente),
                    Csv(r.EstadoPago),
                    Csv(r.EstadoCalculo))));
        }

        private void ExportarReporteOperativo()
        {
            if (Movimientos.Count == 0)
            {
                NotificationService.Warning("No hay movimientos operativos para exportar.");
                return;
            }

            List<string> lineas =
            [
                "SECCION;AGRUPACION;CANTIDAD;IMPORTE;DETALLE"
            ];

            IEnumerable<MovimientoTrabajador> produccion = Movimientos
                .Where(m => !m.EsDescuento
                    && !m.TipoMovimiento.Equals("Pago", StringComparison.OrdinalIgnoreCase)
                    && (m.CategoriaMovimiento.Equals("Produccion", StringComparison.OrdinalIgnoreCase)
                        || m.CategoriaMovimiento.Equals("Produccion por destajo", StringComparison.OrdinalIgnoreCase)));

            lineas.AddRange(produccion
                .GroupBy(m => m.NombreTrabajador)
                .Select(g => string.Join(";", "Produccion por trabajador", Csv(g.Key), NumeroCsv(g.Sum(x => x.Cantidad)), NumeroCsv(g.Sum(x => x.Importe)), Csv(string.Empty))));

            lineas.AddRange(produccion
                .GroupBy(m => string.IsNullOrWhiteSpace(m.NombreArea) ? "Sin area" : m.NombreArea)
                .Select(g => string.Join(";", "Produccion por area", Csv(g.Key), NumeroCsv(g.Sum(x => x.Cantidad)), NumeroCsv(g.Sum(x => x.Importe)), Csv(string.Empty))));

            lineas.AddRange(produccion
                .GroupBy(m => string.IsNullOrWhiteSpace(m.NombreOperacion) ? "Sin operacion" : m.NombreOperacion)
                .Select(g => string.Join(";", "Produccion por operacion", Csv(g.Key), NumeroCsv(g.Sum(x => x.Cantidad)), NumeroCsv(g.Sum(x => x.Importe)), Csv(TarifasAplicadas(g)))));

            lineas.AddRange(Movimientos
                .GroupBy(m => m.NombreConcepto)
                .Select(g => string.Join(";", "Movimientos por concepto", Csv(g.Key), NumeroCsv(g.Sum(x => x.Cantidad)), NumeroCsv(g.Sum(x => x.Importe)), Csv(g.FirstOrDefault()?.TipoMovimiento ?? string.Empty))));

            ExportarCsv($"ReporteOperativo_{PeriodoSeleccionado?.CodigoPeriodo ?? "periodo"}.csv", "Reporte operativo", lineas, []);
        }

        private void ExportarReportePagos()
        {
            if (Resumenes.Count == 0 && Pagos.Count == 0)
            {
                NotificationService.Warning("No hay informacion de pagos para exportar.");
                return;
            }

            List<string> lineas =
            [
                "SECCION;FECHA;TRABAJADOR;MEDIO;OPERACION;IMPORTE;ESTADO;OBSERVACION"
            ];

            lineas.AddRange(Pagos.Select(p => string.Join(";",
                "Pagos realizados",
                Csv(p.FechaPago.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)),
                Csv(p.NombreTrabajador),
                Csv(p.MedioPago),
                Csv(p.NumeroOperacion),
                NumeroCsv(p.MontoPagado),
                Csv(p.Estado),
                Csv(p.Observacion))));

            lineas.AddRange(Resumenes.Where(r => r.SaldoPendiente > 0 && r.TotalPagado <= 0).Select(r => string.Join(";",
                "Pagos pendientes",
                "",
                Csv(r.NombreTrabajador),
                Csv(r.MedioPagoPreferido),
                "",
                NumeroCsv(r.SaldoPendiente),
                Csv(r.EstadoPago),
                "")));

            lineas.AddRange(Resumenes.Where(r => r.SaldoPendiente > 0 && r.TotalPagado > 0).Select(r => string.Join(";",
                "Pagos parciales",
                "",
                Csv(r.NombreTrabajador),
                Csv(r.MedioPagoPreferido),
                "",
                NumeroCsv(r.SaldoPendiente),
                Csv(r.EstadoPago),
                "")));

            ExportarCsv($"ReportePagos_{PeriodoSeleccionado?.CodigoPeriodo ?? "periodo"}.csv", "Reporte de pagos", lineas, []);
        }

        private void ExportarReportePrestamos()
        {
            if (Prestamos.Count == 0 && Cuotas.Count == 0)
            {
                NotificationService.Warning("No hay prestamos o cuotas para exportar.");
                return;
            }

            List<string> lineas =
            [
                "SECCION;TRABAJADOR;FECHA;CONCEPTO;CUOTA;MONTO;SALDO;ESTADO;OBSERVACION"
            ];

            lineas.AddRange(Prestamos.Where(p => !p.Estado.Equals("Anulado", StringComparison.OrdinalIgnoreCase)).Select(p => string.Join(";",
                "Prestamos activos",
                Csv(p.NombreTrabajador),
                Csv(p.FechaPrestamo.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)),
                Csv(p.NombreConcepto),
                Csv($"{p.NumeroCuotas} cuotas"),
                NumeroCsv(p.MontoTotal),
                NumeroCsv(p.SaldoPendiente),
                Csv(p.Estado),
                Csv(p.Observacion))));

            lineas.AddRange(Cuotas.Where(c => c.Estado.Equals("Pendiente", StringComparison.OrdinalIgnoreCase)).Select(c => string.Join(";",
                "Cuotas pendientes",
                Csv(c.NombreTrabajador),
                Csv(c.FechaProgramada.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)),
                Csv(c.NombreConcepto),
                Csv($"{c.NumeroCuota}/{c.TotalCuotas}"),
                NumeroCsv(c.MontoCuota),
                "",
                Csv(c.Estado),
                Csv(c.Observacion))));

            ExportarCsv($"ReportePrestamos_{PeriodoSeleccionado?.CodigoPeriodo ?? "periodo"}.csv", "Reporte de prestamos y cuotas", lineas, []);
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
                FechaInicioDescuento = FechaInicioDescuentoPrestamo ?? FechaPrestamo ?? DateTime.Today,
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
            FechaInicioDescuentoPrestamo = DateTime.Today;
            MontoPrestamo = 0;
            NumeroCuotasPrestamo = 1;
            MontoCuotaPrestamo = 0;
            ObservacionPrestamo = string.Empty;
        }

        private void AplicarCuota()
        {
            Ejecutar(() => _destajoNegocio.AplicarCuota(CuotaSeleccionada?.IdCuotaProgramada ?? 0, PeriodoSeleccionado?.IdPeriodoPago ?? 0, UsuarioActual()), () =>
            {
                CargarPrestamos();
                CargarCuotas();
                CargarMovimientos();
                CargarResumen();
            });
        }

        private void RegistrarPagoExtraordinario()
        {
            Ejecutar(() => _destajoNegocio.RegistrarPagoExtraordinarioPrestamo(
                PrestamoSeleccionado?.IdPrestamoTrabajador ?? 0,
                FechaPagoExtraordinario ?? DateTime.Today,
                MontoPagoExtraordinario,
                ObservacionOperacionPrestamo,
                UsuarioActual()), () =>
            {
                CargarPrestamos();
                CargarCuotas();
                MontoPagoExtraordinario = 0;
                ObservacionOperacionPrestamo = string.Empty;
            });
        }

        private void SuspenderCuota()
        {
            Ejecutar(() => _destajoNegocio.SuspenderCuota(
                CuotaSeleccionada?.IdCuotaProgramada ?? 0,
                ObservacionOperacionPrestamo,
                UsuarioActual()), () =>
            {
                CargarPrestamos();
                CargarCuotas();
                ObservacionOperacionPrestamo = string.Empty;
            });
        }

        private void ReprogramarCuota()
        {
            Ejecutar(() => _destajoNegocio.ReprogramarCuota(
                CuotaSeleccionada?.IdCuotaProgramada ?? 0,
                FechaReprogramacionCuota ?? DateTime.Today,
                MontoReprogramacionCuota,
                ObservacionOperacionPrestamo,
                UsuarioActual()), () =>
            {
                CargarPrestamos();
                CargarCuotas();
                ObservacionOperacionPrestamo = string.Empty;
            });
        }

        private void CancelarPrestamo()
        {
            Ejecutar(() => _destajoNegocio.CancelarPrestamo(
                PrestamoSeleccionado?.IdPrestamoTrabajador ?? 0,
                ObservacionOperacionPrestamo,
                UsuarioActual()), () =>
            {
                CargarPrestamos();
                CargarCuotas();
                ObservacionOperacionPrestamo = string.Empty;
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

        private void RegistrarPagosSeleccionados(object? parametro)
        {
            IReadOnlyList<ResumenPagoTrabajador> seleccionados = ObtenerResumenesSeleccionados(parametro)
                .Where(r => r.SaldoPendiente > 0)
                .ToList();

            if (PeriodoSeleccionado == null)
            {
                NotificationService.Warning("Seleccione una semana de trabajo.");
                return;
            }

            if (seleccionados.Count == 0)
            {
                NotificationService.Warning("Seleccione trabajadores con saldo pendiente.");
                return;
            }

            int registrados = 0;
            List<string> errores = [];

            foreach (ResumenPagoTrabajador resumen in seleccionados)
            {
                string mensaje = _destajoNegocio.RegistrarPagoTrabajador(
                    PeriodoSeleccionado.IdPeriodoPago,
                    resumen.IdTrabajadorOperativo,
                    null,
                    MedioPagoLote,
                    resumen.SaldoPendiente,
                    FechaPagoLote ?? DateTime.Today,
                    NumeroOperacionPago,
                    ObservacionLote,
                    string.Empty,
                    0,
                    UsuarioActual());

                if (EsMensajeExitoso(mensaje))
                    registrados++;
                else
                    errores.Add($"{resumen.NombreTrabajador}: {mensaje}");
            }

            CargarMovimientos();
            CargarResumen();
            CargarLotes();
            CargarLoteDetalles();
            CargarPagos();

            if (errores.Count == 0)
            {
                NotificationService.Success($"Pagos registrados correctamente: {registrados}.");
                return;
            }

            NotificationService.Warning($"Pagos registrados: {registrados}. Pendientes: {errores.Count}. {string.Join(" | ", errores.Take(3))}");
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
                FechaPagoLote ?? DateTime.Today,
                NumeroOperacionPago,
                ObservacionLote,
                MedioPagoLote2,
                MontoPagoLote2,
                UsuarioActual()), () =>
                {
                    CargarMovimientos();
                    CargarResumen();
                    CargarLotes();
                    CargarLoteDetalles();
                    CargarPagos();
                    MontoPagoLote = 0;
                    MontoPagoLote2 = 0;
                    NumeroOperacionPago = string.Empty;
                });
        }

        private void AnularPago()
        {
            Ejecutar(() => _destajoNegocio.AnularPagoTrabajador(
                PagoSeleccionado?.IdPagoTrabajador ?? 0,
                MotivoAnulacionPago,
                AutorizadoPorAnulacionPago,
                UsuarioActual()), () =>
                {
                    CargarMovimientos();
                    CargarResumen();
                    CargarLotes();
                    CargarLoteDetalles();
                    CargarPagos();
                    MotivoAnulacionPago = string.Empty;
                    AutorizadoPorAnulacionPago = string.Empty;
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
                    _empresaNegocio.ObtenerPredeterminada() ?? new Empresa { Nombre = "COREX PROD", NombreComercial = "COREX PROD" },
                    PeriodoSeleccionado,
                    resumenes,
                    Movimientos.ToList(),
                    Pagos.ToList(),
                    true); // true = Con copia (2 por hoja), false = Sin copia (1 por hoja)

                string mensaje = _destajoNegocio.RegistrarBoletasGeneradas(PeriodoSeleccionado.IdPeriodoPago, resumenes.Count, UsuarioActual());
                if (!EsMensajeExitoso(mensaje))
                {
                    NotificationService.Warning(mensaje);
                }

                CargarPeriodos();
                CargarAuditoria();
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

        private void ExportarCsv(string nombreArchivo, string titulo, IReadOnlyList<string> cabecera, IEnumerable<string> filas)
        {
            SaveFileDialog dialog = new()
            {
                Title = titulo,
                FileName = nombreArchivo,
                DefaultExt = ".csv",
                Filter = "Archivo CSV para Excel (*.csv)|*.csv"
            };

            if (dialog.ShowDialog() != true)
                return;

            List<string> lineas = [.. cabecera, .. filas];
            File.WriteAllLines(dialog.FileName, lineas, System.Text.Encoding.UTF8);
            NotificationService.Success($"{titulo} exportado correctamente.");
            AbrirArchivo(dialog.FileName);
        }

        private static string NumeroCsv(decimal valor)
        {
            return valor.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static string TarifasAplicadas(IEnumerable<MovimientoTrabajador> movimientos)
        {
            return string.Join(", ", movimientos
                .Where(m => m.Tarifa > 0)
                .Select(m => m.Tarifa.ToString("0.####", CultureInfo.InvariantCulture))
                .Distinct());
        }

        private static string Csv(string valor)
        {
            string limpio = (valor ?? string.Empty)
                .Replace("\"", "\"\"")
                .Replace("\r", " ")
                .Replace("\n", " ");

            return $"\"{limpio}\"";
        }

        private static List<string> SepararCsv(string linea)
        {
            List<string> columnas = [];
            bool dentroComillas = false;
            System.Text.StringBuilder actual = new();

            for (int i = 0; i < linea.Length; i++)
            {
                char caracter = linea[i];

                if (caracter == '"')
                {
                    if (dentroComillas && i + 1 < linea.Length && linea[i + 1] == '"')
                    {
                        actual.Append('"');
                        i++;
                        continue;
                    }

                    dentroComillas = !dentroComillas;
                    continue;
                }

                if (caracter == ';' && !dentroComillas)
                {
                    columnas.Add(actual.ToString());
                    actual.Clear();
                    continue;
                }

                actual.Append(caracter);
            }

            columnas.Add(actual.ToString());
            return columnas;
        }

        private static bool EsIgual(string actual, string esperado)
        {
            return actual.Trim().Equals(esperado.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool EsVerdadero(string valor)
        {
            return valor.Trim().Equals("SI", StringComparison.OrdinalIgnoreCase)
                || valor.Trim().Equals("TRUE", StringComparison.OrdinalIgnoreCase)
                || valor.Trim().Equals("1", StringComparison.OrdinalIgnoreCase);
        }

        private static DateTime ParseFecha(string valor, DateTime predeterminado)
        {
            return DateTime.TryParse(valor, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime fecha)
                || DateTime.TryParse(valor, CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha)
                    ? fecha
                    : predeterminado;
        }

        private static decimal ParseDecimal(string valor)
        {
            if (decimal.TryParse(valor, NumberStyles.Any, CultureInfo.CurrentCulture, out decimal numero))
                return numero;

            if (decimal.TryParse(valor, NumberStyles.Any, CultureInfo.InvariantCulture, out numero))
                return numero;

            return 0;
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

            DateTime fecha = FechaMovimiento ?? DateTime.Today;

            if ((operacion.FechaInicioVigencia.HasValue && fecha.Date < operacion.FechaInicioVigencia.Value.Date)
                || (operacion.FechaFinVigencia.HasValue && fecha.Date > operacion.FechaFinVigencia.Value.Date))
            {
                TarifaMovimiento = 0;
                NotificationService.Warning("La operacion seleccionada no tiene tarifa vigente para la fecha del trabajo.");
                return;
            }

            if (operacion.IdAreaOperativa.HasValue)
                IdAreaMovimiento = operacion.IdAreaOperativa.Value;

            UnidadMovimiento = operacion.UnidadMedida;

            if (operacion.TarifaBase > 0)
                TarifaMovimiento = operacion.TarifaBase;
        }

        private void ActualizarImporteMovimiento()
        {
            if (CantidadMovimiento <= 0 || TarifaMovimiento <= 0)
                return;

            decimal importe = Math.Round(CantidadMovimiento * TarifaMovimiento, 2);

            if (_importeMovimiento == importe)
                return;

            _importeMovimiento = importe;
            OnPropertyChanged(nameof(ImporteMovimiento));
            OnPropertyChanged(nameof(TotalMovimientoCalculado));
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
