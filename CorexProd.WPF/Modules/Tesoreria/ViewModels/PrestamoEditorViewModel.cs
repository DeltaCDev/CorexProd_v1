using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.ViewModels;
using System;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Tesoreria.ViewModels
{
    public class PrestamoEditorViewModel : BaseViewModel
    {
        private readonly ObligacionFinancieraNegocio _negocio = new();
        private readonly EmpresaNegocio _empresaNegocio = new();
        private readonly CultureInfo _cultura = new("es-PE");
        private readonly bool _esEdicion;
        private readonly bool _estaCancelada;
        private readonly bool _tienePagos;
        private readonly int _idPrestamo;
        private EntidadFinanciera? _entidadSeleccionada;
        private EmpresaComboItem? _empresaTitularSeleccionada;
        private EmpresaComboItem? _empresaResponsableSeleccionada;
        private string _tipoTitular = "EMPRESA";
        private string _nombreTitular = string.Empty;
        private string _codigoReferencia = string.Empty;
        private string _numeroContrato = string.Empty;
        private string _numeroOperacion = string.Empty;
        private string _concepto = string.Empty;
        private string _moneda = "PEN";
        private decimal? _montoOriginal;
        private DateTime _fechaInicio = DateTime.Today;
        private int _totalCuotas = 1;
        private string _observacion = string.Empty;
        private bool _guardando;

        public PrestamoEditorViewModel(ObligacionFinanciera? prestamo = null)
        {
            _esEdicion = prestamo != null;
            _idPrestamo = prestamo?.IdObligacionFinanciera ?? 0;
            _estaCancelada = prestamo?.EstaCancelada == true;
            _tienePagos = prestamo?.Pagos.Any(p => !p.EstaAnulado) == true || prestamo?.Cuotas.Any(c => c.TotalPagado > 0) == true;

            TipoTitulares = new ObservableCollection<string> { "EMPRESA", "PERSONA", "OTRO" };
            Monedas = new ObservableCollection<string> { "PEN", "USD" };

            GuardarCommand = new RelayCommand(_ => Guardar(), _ => !Guardando);
            CancelarCommand = new RelayCommand(_ => CerrarVentana?.Invoke(false));
            GenerarCuotasCommand = new RelayCommand(_ => GenerarCuotas(), _ => PuedeGenerarCronograma);
            AgregarCuotaCommand = new RelayCommand(_ => AgregarCuota(), _ => PuedeAgregarQuitarCuotas);
            QuitarCuotaCommand = new RelayCommand(p => QuitarCuota(p as PrestamoCuotaItemViewModel), _ => PuedeAgregarQuitarCuotas);
            Cuotas.CollectionChanged += Cuotas_CollectionChanged;

            CargarCombos();

            if (prestamo != null)
                CargarPrestamo(prestamo);
            else
                GenerarCuotas();
        }

        public ObservableCollection<EntidadFinanciera> EntidadesFinancieras { get; } = [];
        public ObservableCollection<EmpresaComboItem> Empresas { get; } = [];
        public ObservableCollection<string> TipoTitulares { get; }
        public ObservableCollection<string> Monedas { get; }
        public ObservableCollection<PrestamoCuotaItemViewModel> Cuotas { get; } = [];
        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }
        public ICommand GenerarCuotasCommand { get; }
        public ICommand AgregarCuotaCommand { get; }
        public ICommand QuitarCuotaCommand { get; }
        public Action<bool>? CerrarVentana { get; set; }

        public string Titulo => _esEdicion ? $"Editar prestamo #{_idPrestamo}" : "Nuevo prestamo";
        public string AlertaEdicion
        {
            get
            {
                if (_estaCancelada)
                    return "Prestamo cancelado: solo puede modificar datos administrativos. La informacion financiera historica esta bloqueada.";

                if (_tienePagos)
                    return "Prestamo con pagos: solo puede modificar fechas de vencimiento y observaciones del cronograma.";

                return string.Empty;
            }
        }
        public bool MostrarAlertaEdicion => !string.IsNullOrWhiteSpace(AlertaEdicion);
        public bool PuedeEditarFinanciero => !_estaCancelada && !_tienePagos;
        public bool PuedeEditarCronograma => !_estaCancelada;
        public bool PuedeGenerarCronograma => !_estaCancelada && !_tienePagos;
        public bool PuedeAgregarQuitarCuotas => !_estaCancelada && !_tienePagos;
        public bool EsTitularEmpresa => TipoTitular == "EMPRESA";
        public string TotalCronogramaTexto => $"{Moneda} {Cuotas.Sum(c => c.Importe).ToString("N2", _cultura)}";

        public EntidadFinanciera? EntidadSeleccionada
        {
            get => _entidadSeleccionada;
            set { _entidadSeleccionada = value; OnPropertyChanged(); }
        }

        public EmpresaComboItem? EmpresaTitularSeleccionada
        {
            get => _empresaTitularSeleccionada;
            set
            {
                _empresaTitularSeleccionada = value;
                OnPropertyChanged();
                if (EsTitularEmpresa && value?.Empresa != null)
                    NombreTitular = value.Empresa.Nombre;
            }
        }

        public EmpresaComboItem? EmpresaResponsableSeleccionada
        {
            get => _empresaResponsableSeleccionada;
            set { _empresaResponsableSeleccionada = value; OnPropertyChanged(); }
        }

        public string TipoTitular
        {
            get => _tipoTitular;
            set
            {
                _tipoTitular = string.IsNullOrWhiteSpace(value) ? "EMPRESA" : value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EsTitularEmpresa));
                if (EsTitularEmpresa && EmpresaTitularSeleccionada?.Empresa != null)
                    NombreTitular = EmpresaTitularSeleccionada.Empresa.Nombre;
            }
        }

        public string NombreTitular
        {
            get => _nombreTitular;
            set { _nombreTitular = value ?? string.Empty; OnPropertyChanged(); }
        }

        public string CodigoReferencia
        {
            get => _codigoReferencia;
            set { _codigoReferencia = value ?? string.Empty; OnPropertyChanged(); }
        }

        public string NumeroContrato
        {
            get => _numeroContrato;
            set { _numeroContrato = value ?? string.Empty; OnPropertyChanged(); }
        }

        public string NumeroOperacion
        {
            get => _numeroOperacion;
            set { _numeroOperacion = value ?? string.Empty; OnPropertyChanged(); }
        }

        public string Concepto
        {
            get => _concepto;
            set { _concepto = value ?? string.Empty; OnPropertyChanged(); }
        }

        public string Moneda
        {
            get => _moneda;
            set { _moneda = string.IsNullOrWhiteSpace(value) ? "PEN" : value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalCronogramaTexto)); }
        }

        public decimal? MontoOriginal
        {
            get => _montoOriginal;
            set { _montoOriginal = value; OnPropertyChanged(); }
        }

        public DateTime FechaInicio
        {
            get => _fechaInicio;
            set { _fechaInicio = value.Date; OnPropertyChanged(); }
        }

        public int TotalCuotas
        {
            get => _totalCuotas;
            set { _totalCuotas = value; OnPropertyChanged(); }
        }

        public string Observacion
        {
            get => _observacion;
            set { _observacion = value ?? string.Empty; OnPropertyChanged(); }
        }

        public bool Guardando
        {
            get => _guardando;
            set { _guardando = value; OnPropertyChanged(); }
        }

        private void CargarCombos()
        {
            try
            {
                EntidadesFinancieras.Clear();
                foreach (EntidadFinanciera entidad in _negocio.ListarEntidadesFinancieras().OrderBy(e => e.Nombre))
                    EntidadesFinancieras.Add(entidad);

                EntidadSeleccionada = EntidadesFinancieras.FirstOrDefault();

                Empresas.Clear();
                Empresas.Add(new EmpresaComboItem(null, "Sin empresa"));
                foreach (Empresa empresa in _empresaNegocio.Listar().Where(e => e.Estado).OrderBy(e => e.Nombre))
                    Empresas.Add(new EmpresaComboItem(empresa, empresa.Nombre));

                EmpresaTitularSeleccionada = Empresas.Skip(1).FirstOrDefault();
                EmpresaResponsableSeleccionada = Empresas.FirstOrDefault();
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudieron cargar los combos del prestamo: {ex.Message}");
            }
        }

        private void Cuotas_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (PrestamoCuotaItemViewModel cuota in e.OldItems)
                    cuota.PropertyChanged -= Cuota_PropertyChanged;
            }

            if (e.NewItems != null)
            {
                foreach (PrestamoCuotaItemViewModel cuota in e.NewItems)
                    cuota.PropertyChanged += Cuota_PropertyChanged;
            }

            OnPropertyChanged(nameof(TotalCronogramaTexto));
        }

        private void Cuota_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PrestamoCuotaItemViewModel.Importe))
                OnPropertyChanged(nameof(TotalCronogramaTexto));
        }

        private void CargarPrestamo(ObligacionFinanciera prestamo)
        {
            EntidadSeleccionada = EntidadesFinancieras.FirstOrDefault(e => e.IdEntidadFinanciera == prestamo.IdEntidadFinanciera) ?? EntidadSeleccionada;
            TipoTitular = prestamo.TipoTitular;
            EmpresaTitularSeleccionada = Empresas.FirstOrDefault(e => e.IdEmpresa == prestamo.IdTitular) ?? Empresas.FirstOrDefault();
            EmpresaResponsableSeleccionada = Empresas.FirstOrDefault(e => e.IdEmpresa == prestamo.IdEmpresaResponsable) ?? Empresas.FirstOrDefault();
            NombreTitular = prestamo.NombreTitular;
            CodigoReferencia = prestamo.CodigoReferencia;
            NumeroContrato = prestamo.NumeroContrato;
            NumeroOperacion = prestamo.NumeroOperacion;
            Concepto = prestamo.Concepto;
            Moneda = prestamo.Moneda;
            MontoOriginal = prestamo.MontoOriginal;
            FechaInicio = prestamo.FechaInicio;
            TotalCuotas = prestamo.TotalCuotas;
            Observacion = prestamo.Observacion;

            Cuotas.Clear();
            foreach (ObligacionFinancieraCuota cuota in prestamo.Cuotas.OrderBy(c => c.NumeroCuota))
                Cuotas.Add(new PrestamoCuotaItemViewModel(cuota, PuedeEditarFinanciero, PuedeAgregarQuitarCuotas));

            OnPropertyChanged(nameof(TotalCronogramaTexto));
        }

        private void GenerarCuotas()
        {
            if (!PuedeGenerarCronograma)
                return;

            if (TotalCuotas <= 0)
            {
                NotificationService.Warning("Ingrese un total de cuotas mayor a cero.");
                return;
            }

            Cuotas.Clear();
            decimal importeBase = MontoOriginal.HasValue && MontoOriginal.Value > 0
                ? Math.Round(MontoOriginal.Value / TotalCuotas, 2)
                : 0;
            decimal acumulado = 0;

            for (int i = 1; i <= TotalCuotas; i++)
            {
                decimal importe = importeBase;
                if (MontoOriginal.HasValue && MontoOriginal.Value > 0 && i == TotalCuotas)
                    importe = Math.Round(MontoOriginal.Value - acumulado, 2);

                acumulado += importe;
                Cuotas.Add(new PrestamoCuotaItemViewModel
                {
                    NumeroCuota = i,
                    TotalCuotas = TotalCuotas,
                    FechaVencimiento = FechaInicio.AddMonths(i),
                    Importe = importe,
                    PuedeEditarNumeroImporte = true,
                    PuedeQuitar = true
                });
            }

            OnPropertyChanged(nameof(TotalCronogramaTexto));
        }

        private void AgregarCuota()
        {
            TotalCuotas++;
            foreach (PrestamoCuotaItemViewModel cuota in Cuotas)
                cuota.TotalCuotas = TotalCuotas;

            Cuotas.Add(new PrestamoCuotaItemViewModel
            {
                NumeroCuota = TotalCuotas,
                TotalCuotas = TotalCuotas,
                FechaVencimiento = Cuotas.LastOrDefault()?.FechaVencimiento.AddMonths(1) ?? FechaInicio.AddMonths(TotalCuotas),
                PuedeEditarNumeroImporte = true,
                PuedeQuitar = true
            });
            OnPropertyChanged(nameof(TotalCronogramaTexto));
        }

        private void QuitarCuota(PrestamoCuotaItemViewModel? cuota)
        {
            if (cuota == null || !PuedeAgregarQuitarCuotas)
                return;

            Cuotas.Remove(cuota);
            TotalCuotas = Cuotas.Count;
            int numero = 1;
            foreach (PrestamoCuotaItemViewModel item in Cuotas.OrderBy(c => c.NumeroCuota))
            {
                item.NumeroCuota = numero++;
                item.TotalCuotas = TotalCuotas;
            }
            OnPropertyChanged(nameof(TotalCronogramaTexto));
        }

        private void Guardar()
        {
            string validacion = Validar();
            if (!string.IsNullOrWhiteSpace(validacion))
            {
                NotificationService.Warning(validacion);
                return;
            }

            try
            {
                Guardando = true;
                string usuario = SessionManager.UsuarioActual?.NombreCompleto
                    ?? SessionManager.UsuarioActual?.NombreUsuario
                    ?? "Sistema";

                ObligacionFinanciera prestamo = new()
                {
                    IdObligacionFinanciera = _idPrestamo,
                    IdEntidadFinanciera = EntidadSeleccionada!.IdEntidadFinanciera,
                    TipoTitular = TipoTitular,
                    IdTitular = EsTitularEmpresa ? EmpresaTitularSeleccionada?.IdEmpresa : null,
                    NombreTitular = NombreTitular,
                    IdEmpresaResponsable = EmpresaResponsableSeleccionada?.IdEmpresa,
                    CodigoReferencia = CodigoReferencia,
                    NumeroContrato = NumeroContrato,
                    NumeroOperacion = NumeroOperacion,
                    Concepto = Concepto,
                    Moneda = Moneda,
                    MontoOriginal = MontoOriginal,
                    FechaInicio = FechaInicio,
                    TotalCuotas = TotalCuotas,
                    Observacion = Observacion,
                    Cuotas = Cuotas.OrderBy(c => c.NumeroCuota).Select(c => c.ToEntity()).ToList()
                };

                ObligacionFinancieraResultado resultado = _esEdicion
                    ? _negocio.ActualizarPrestamo(prestamo, usuario, !_estaCancelada)
                    : _negocio.GuardarPrestamo(prestamo, usuario);

                if (resultado.Resultado)
                {
                    NotificationService.Success(resultado.Mensaje);
                    CerrarVentana?.Invoke(true);
                }
                else
                {
                    NotificationService.Warning(resultado.Mensaje);
                }
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudo guardar el prestamo: {ex.Message}");
            }
            finally
            {
                Guardando = false;
            }
        }

        private string Validar()
        {
            if (EntidadSeleccionada == null)
                return "Seleccione una entidad financiera.";

            if (EsTitularEmpresa && EmpresaTitularSeleccionada?.IdEmpresa == null && string.IsNullOrWhiteSpace(NombreTitular))
                return "Seleccione o ingrese el titular del prestamo.";

            if (!EsTitularEmpresa && string.IsNullOrWhiteSpace(NombreTitular))
                return "Ingrese el nombre del titular.";

            if (string.IsNullOrWhiteSpace(Concepto))
                return "Ingrese el concepto del prestamo.";

            if (TotalCuotas <= 0)
                return "El total de cuotas debe ser mayor a cero.";

            if (!_estaCancelada && Cuotas.Count != TotalCuotas)
                return "La cantidad de cuotas debe coincidir con el total de cuotas.";

            if (!_estaCancelada && Cuotas.Any(c => c.Importe <= 0))
                return "Todos los importes de cuotas deben ser mayores a cero.";

            if (!_estaCancelada && Cuotas.Any(c => c.FechaVencimiento.Date < FechaInicio.Date))
                return "Ninguna fecha de vencimiento puede ser anterior a la fecha de inicio.";

            if (!_estaCancelada && Cuotas.GroupBy(c => c.NumeroCuota).Any(g => g.Count() > 1))
                return "No se puede duplicar el numero de cuota.";

            return string.Empty;
        }
    }

    public class EmpresaComboItem
    {
        public EmpresaComboItem(Empresa? empresa, string nombre)
        {
            Empresa = empresa;
            Nombre = nombre;
        }

        public Empresa? Empresa { get; }
        public int? IdEmpresa => Empresa?.IdEmpresa;
        public string Nombre { get; }
    }
}
