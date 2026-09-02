using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.Modules.Tesoreria.Views;
using CorexProd.WPF.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Tesoreria.ViewModels
{
    public class ProgramacionPagosViewModel : BaseViewModel
    {
        private readonly CuentaPorPagarNegocio _cuentaNegocio = new();
        private readonly ProveedorNegocio _proveedorNegocio = new();
        private readonly EmpresaNegocio _empresaNegocio = new();
        private readonly CultureInfo _cultura = new("es-PE");
        private DateTime _fechaDesde;
        private DateTime _fechaHasta;
        private ProveedorFiltroItem? _proveedorSeleccionado;
        private string _estadoFiltro = "Todos";
        private CuentaPorPagarProgramacionItem? _pagoSeleccionado;
        private bool _isLoading;
        private bool _actualizandoSemana;

        public ProgramacionPagosViewModel()
        {
            SemanaAnteriorCommand = new RelayCommand(_ => CambiarSemana(-7));
            SemanaActualCommand = new RelayCommand(_ => EstablecerSemana(DateTime.Today));
            SemanaSiguienteCommand = new RelayCommand(_ => CambiarSemana(7));
            ActualizarCommand = new RelayCommand(_ => CargarProgramacion());
            ExportarPdfCommand = new RelayCommand(_ => ExportarPdf());
            VerDetalleCommand = new RelayCommand(parametro => VerDetalle(parametro));
            PagarCommand = new RelayCommand(parametro => Pagar(parametro));

            CargarProveedores();
            EstablecerSemana(DateTime.Today);
        }

        public ObservableCollection<ProveedorFiltroItem> Proveedores { get; } = [];
        public ObservableCollection<string> Estados { get; } = ["Todos", "PENDIENTE", "PARCIAL", "CANCELADA"];
        public ObservableCollection<ProgramacionDiaPagos> Dias { get; } = [];
        public ObservableCollection<TotalProgramacionMoneda> TotalesSemana { get; } = [];

        public ICommand SemanaAnteriorCommand { get; }
        public ICommand SemanaActualCommand { get; }
        public ICommand SemanaSiguienteCommand { get; }
        public ICommand ActualizarCommand { get; }
        public ICommand ExportarPdfCommand { get; }
        public ICommand VerDetalleCommand { get; }
        public ICommand PagarCommand { get; }

        public DateTime FechaDesde
        {
            get => _fechaDesde;
            set
            {
                _fechaDesde = value.Date;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RangoSemana));
                CargarSiCorresponde();
            }
        }

        public DateTime FechaHasta
        {
            get => _fechaHasta;
            set
            {
                _fechaHasta = value.Date;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RangoSemana));
                CargarSiCorresponde();
            }
        }

        public ProveedorFiltroItem? ProveedorSeleccionado
        {
            get => _proveedorSeleccionado;
            set
            {
                _proveedorSeleccionado = value;
                OnPropertyChanged();
                CargarProgramacion();
            }
        }

        public string EstadoFiltro
        {
            get => _estadoFiltro;
            set
            {
                _estadoFiltro = string.IsNullOrWhiteSpace(value) ? "Todos" : value;
                OnPropertyChanged();
                CargarProgramacion();
            }
        }

        public CuentaPorPagarProgramacionItem? PagoSeleccionado
        {
            get => _pagoSeleccionado;
            set { _pagoSeleccionado = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LoadingVisibility));
                OnPropertyChanged(nameof(EmptyVisibility));
            }
        }

        public Visibility LoadingVisibility => IsLoading ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EmptyVisibility => !IsLoading && Dias.All(d => d.Pagos.Count == 0) ? Visibility.Visible : Visibility.Collapsed;
        public string RangoSemana => $"{FechaDesde:dd/MM/yyyy} - {FechaHasta:dd/MM/yyyy}";
        public string ResumenSemana => $"{Dias.Sum(d => d.Pagos.Count)} cuota(s) programada(s)";

        private void CargarProveedores()
        {
            try
            {
                Proveedores.Clear();
                Proveedores.Add(new ProveedorFiltroItem(null, "Todos los proveedores"));

                foreach (ProveedorStock proveedor in _proveedorNegocio.Listar().OrderBy(p => p.NombreRazonSocial))
                    Proveedores.Add(new ProveedorFiltroItem(proveedor.IdProveedor, proveedor.ProveedorBusqueda));

                ProveedorSeleccionado = Proveedores.FirstOrDefault();
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudieron cargar los proveedores: {ex.Message}");
            }
        }

        private void EstablecerSemana(DateTime fecha)
        {
            _actualizandoSemana = true;
            FechaDesde = ObtenerInicioSemana(fecha);
            FechaHasta = FechaDesde.AddDays(6);
            _actualizandoSemana = false;
            CargarProgramacion();
        }

        private void CambiarSemana(int dias)
        {
            EstablecerSemana(FechaDesde.AddDays(dias));
        }

        private static DateTime ObtenerInicioSemana(DateTime fecha)
        {
            int diferencia = ((int)fecha.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            return fecha.Date.AddDays(-diferencia);
        }

        private void CargarSiCorresponde()
        {
            if (_actualizandoSemana)
                return;

            if (FechaDesde != default && FechaHasta != default && FechaHasta >= FechaDesde)
                CargarProgramacion();
        }

        private void CargarProgramacion()
        {
            if (FechaDesde == default || FechaHasta == default || FechaHasta < FechaDesde)
                return;

            try
            {
                IsLoading = true;
                int? idProveedor = ProveedorSeleccionado?.IdProveedor;
                string? estado = EstadoFiltro == "Todos" ? null : EstadoFiltro;

                List<CuentaPorPagarProgramacionItem> pagos = _cuentaNegocio
                    .ObtenerProgramacion(FechaDesde, FechaHasta, idProveedor, estado)
                    .Select(p => new CuentaPorPagarProgramacionItem(p, _cultura))
                    .OrderBy(p => p.FechaVencimiento)
                    .ThenBy(p => p.NombreProveedor)
                    .ThenBy(p => p.NumeroCuota)
                    .ToList();

                Dias.Clear();
                for (DateTime fecha = FechaDesde; fecha <= FechaHasta; fecha = fecha.AddDays(1))
                {
                    List<CuentaPorPagarProgramacionItem> pagosDia = pagos
                        .Where(p => p.FechaVencimiento.Date == fecha.Date)
                        .ToList();

                    Dias.Add(new ProgramacionDiaPagos(fecha, pagosDia, _cultura));
                }

                RecalcularTotalesSemana(pagos);
                NotificarResumen();
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudo cargar la programacion de pagos: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void RecalcularTotalesSemana(IEnumerable<CuentaPorPagarProgramacionItem> pagos)
        {
            TotalesSemana.Clear();
            foreach (TotalProgramacionMoneda total in CrearTotales(pagos, _cultura))
                TotalesSemana.Add(total);
        }

        private void NotificarResumen()
        {
            OnPropertyChanged(nameof(ResumenSemana));
            OnPropertyChanged(nameof(EmptyVisibility));
        }

        private void VerDetalle(object? parametro)
        {
            CuentaPorPagarProgramacionItem? pago = parametro as CuentaPorPagarProgramacionItem ?? PagoSeleccionado;
            if (pago == null)
            {
                NotificationService.Warning("Debe seleccionar una cuota programada.");
                return;
            }

            try
            {
                CuentaPorPagar? detalle = _cuentaNegocio.Obtener(pago.IdCuentaPorPagar);
                if (detalle == null)
                {
                    NotificationService.Warning("No se encontro la cuenta por pagar seleccionada.");
                    return;
                }

                CuentaPorPagarDetalleWindow ventana = new(detalle)
                {
                    Owner = Application.Current.MainWindow
                };
                ventana.ShowDialog();
                CargarProgramacion();
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudo abrir el detalle: {ex.Message}");
            }
        }

        private void Pagar(object? parametro)
        {
            CuentaPorPagarProgramacionItem? pago = parametro as CuentaPorPagarProgramacionItem ?? PagoSeleccionado;
            if (pago == null)
            {
                NotificationService.Warning("Debe seleccionar una cuota programada.");
                return;
            }

            if (!pago.PuedePagar)
            {
                NotificationService.Warning("La cuota seleccionada no tiene saldo pendiente para pagar.");
                return;
            }

            RegistrarPagoCuentaPorPagarWindow ventana = new(pago)
            {
                Owner = Application.Current.MainWindow
            };

            if (ventana.ShowDialog() == true)
                CargarProgramacion();
        }

        private void ExportarPdf()
        {
            MessageBoxResult incluirAtrasados = MessageBox.Show(
                "Desea incluir en el PDF los pagos pendientes con fecha anterior al inicio de la semana seleccionada?",
                "Exportar programacion de pagos",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (incluirAtrasados == MessageBoxResult.Cancel)
                return;

            bool incluirPendientesAnteriores = incluirAtrasados == MessageBoxResult.Yes;
            List<CuentaPorPagarProgramacionItem> pagos = ObtenerPagosParaExportar(incluirPendientesAnteriores);

            if (pagos.Count == 0)
            {
                NotificationService.Warning("No hay pagos programados para exportar.");
                return;
            }

            SaveFileDialog dialog = new()
            {
                Title = "Exportar programacion semanal de pagos",
                FileName = $"ProgramacionPagos_{FechaDesde:yyyyMMdd}_{FechaHasta:yyyyMMdd}.pdf",
                Filter = "Documento PDF (*.pdf)|*.pdf",
                DefaultExt = ".pdf"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                string proveedor = ProveedorSeleccionado?.Nombre ?? "Todos los proveedores";
                string estado = EstadoFiltro == "Todos" ? "Todos los estados" : EstadoFiltro;

                ProgramacionPagosPdfExporter.Exportar(
                    dialog.FileName,
                    _empresaNegocio.ObtenerPredeterminada(),
                    FechaDesde,
                    FechaHasta,
                    proveedor,
                    estado,
                    incluirPendientesAnteriores,
                    pagos);

                NotificationService.Success("PDF generado correctamente.");
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudo generar el PDF: {ex.Message}");
            }
        }

        private List<CuentaPorPagarProgramacionItem> ObtenerPagosParaExportar(bool incluirPendientesAnteriores)
        {
            List<CuentaPorPagarProgramacionItem> pagos = Dias
                .SelectMany(d => d.Pagos)
                .ToList();

            if (incluirPendientesAnteriores)
            {
                int? idProveedor = ProveedorSeleccionado?.IdProveedor;
                List<CuentaPorPagarProgramacionItem> pendientesAnteriores = _cuentaNegocio
                    .ObtenerProgramacion(new DateTime(1900, 1, 1), FechaDesde.AddDays(-1), idProveedor, null)
                    .Where(p => p.SaldoPendiente > 0
                        && (p.Estado.Equals("PENDIENTE", StringComparison.OrdinalIgnoreCase)
                            || p.Estado.Equals("PARCIAL", StringComparison.OrdinalIgnoreCase)))
                    .Select(p => new CuentaPorPagarProgramacionItem(p, _cultura))
                    .ToList();

                HashSet<int> cuotasIncluidas = pagos.Select(p => p.IdCuota).ToHashSet();
                foreach (CuentaPorPagarProgramacionItem pendiente in pendientesAnteriores)
                {
                    if (cuotasIncluidas.Add(pendiente.IdCuota))
                        pagos.Add(pendiente);
                }
            }

            return pagos
                .OrderBy(p => p.FechaVencimiento)
                .ThenBy(p => p.NombreProveedor)
                .ThenBy(p => p.NumeroCuota)
                .ToList();
        }

        internal static List<TotalProgramacionMoneda> CrearTotales(IEnumerable<CuentaPorPagarProgramacionItem> pagos, CultureInfo cultura)
        {
            return pagos
                .GroupBy(p => p.Moneda)
                .OrderBy(g => g.Key)
                .Select(g => new TotalProgramacionMoneda(
                    g.Key,
                    g.Sum(p => p.Importe),
                    g.Sum(p => p.TotalPagado),
                    g.Sum(p => p.SaldoPendiente),
                    cultura))
                .ToList();
        }
    }

    public class ProveedorFiltroItem
    {
        public ProveedorFiltroItem(int? idProveedor, string nombre)
        {
            IdProveedor = idProveedor;
            Nombre = nombre;
        }

        public int? IdProveedor { get; }
        public string Nombre { get; }
    }

    public class ProgramacionDiaPagos
    {
        private readonly CultureInfo _cultura;

        public ProgramacionDiaPagos(DateTime fecha, IEnumerable<CuentaPorPagarProgramacionItem> pagos, CultureInfo cultura)
        {
            Fecha = fecha.Date;
            _cultura = cultura;

            foreach (CuentaPorPagarProgramacionItem pago in pagos)
                Pagos.Add(pago);

            foreach (TotalProgramacionMoneda total in ProgramacionPagosViewModel.CrearTotales(Pagos, cultura))
                Totales.Add(total);
        }

        public DateTime Fecha { get; }
        public ObservableCollection<CuentaPorPagarProgramacionItem> Pagos { get; } = [];
        public ObservableCollection<TotalProgramacionMoneda> Totales { get; } = [];
        public string DiaTitulo => $"{_cultura.DateTimeFormat.GetDayName(Fecha.DayOfWeek).ToUpperInvariant()} {Fecha:dd/MM}";
        public string ResumenDia => Pagos.Count == 0 ? "Sin pagos programados" : $"{Pagos.Count} cuota(s)";
        public Visibility TotalesVisibility => Totales.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EmptyVisibility => Pagos.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public class CuentaPorPagarProgramacionItem
    {
        private readonly CultureInfo _cultura;

        public CuentaPorPagarProgramacionItem(CuentaPorPagarProgramacion pago, CultureInfo cultura)
        {
            _cultura = cultura;
            IdCuota = pago.IdCuota;
            IdCuentaPorPagar = pago.IdCuentaPorPagar;
            IdProveedor = pago.IdProveedor;
            NombreProveedor = pago.NombreProveedor;
            NumeroDocumentoProveedor = pago.NumeroDocumentoProveedor;
            TipoObligacion = pago.TipoObligacion;
            Moneda = pago.Moneda;
            FechaDocumento = pago.FechaDocumento;
            NumeroCuota = pago.NumeroCuota;
            TotalCuotas = pago.TotalCuotas;
            NumeroLetra = pago.NumeroLetra;
            TipoCuota = pago.TipoCuota;
            DocumentoPrincipal = pago.DocumentoPrincipal;
            FechaGiro = pago.FechaGiro;
            FechaVencimiento = pago.FechaVencimiento;
            Importe = pago.Importe;
            TotalPagado = pago.TotalPagado;
            SaldoPendiente = pago.SaldoPendiente;
            Estado = pago.Estado;
            Observacion = pago.Observacion;
        }

        public int IdCuota { get; }
        public int IdCuentaPorPagar { get; }
        public int IdProveedor { get; }
        public string NombreProveedor { get; }
        public string NumeroDocumentoProveedor { get; }
        public string TipoObligacion { get; }
        public string Moneda { get; }
        public DateTime FechaDocumento { get; }
        public int NumeroCuota { get; }
        public int TotalCuotas { get; }
        public string NumeroLetra { get; }
        public string TipoCuota { get; }
        public string DocumentoPrincipal { get; }
        public DateTime FechaGiro { get; }
        public DateTime FechaVencimiento { get; }
        public decimal Importe { get; }
        public decimal TotalPagado { get; }
        public decimal SaldoPendiente { get; }
        public string Estado { get; }
        public string Observacion { get; }
        public string CuotaTexto => $"{NumeroCuota}/{TotalCuotas}";
        public string ReferenciaObligacion => string.IsNullOrWhiteSpace(NumeroLetra)
            ? string.IsNullOrWhiteSpace(DocumentoPrincipal) ? $"Cuota factura {CuotaTexto}" : DocumentoPrincipal
            : NumeroLetra;
        public string SimboloMoneda => ObtenerSimbolo(Moneda);
        public string ImporteTexto => $"{SimboloMoneda} {Importe.ToString("N2", _cultura)}";
        public string TotalPagadoTexto => $"{SimboloMoneda} {TotalPagado.ToString("N2", _cultura)}";
        public string SaldoPendienteTexto => $"{SimboloMoneda} {SaldoPendiente.ToString("N2", _cultura)}";
        public bool EstaVencido => FechaVencimiento.Date < DateTime.Today && SaldoPendiente > 0;
        public string VencimientoTexto => EstaVencido ? "Vencido" : Estado;
        public bool PuedePagar => SaldoPendiente > 0 && !Estado.Equals("ANULADA", StringComparison.OrdinalIgnoreCase) && !Estado.Equals("CANCELADA", StringComparison.OrdinalIgnoreCase);
        public Visibility PagarVisibility => PuedePagar ? Visibility.Visible : Visibility.Collapsed;

        internal static string ObtenerSimbolo(string moneda)
        {
            return moneda?.Trim().ToUpperInvariant() switch
            {
                "PEN" => "S/",
                "USD" => "US$",
                "EUR" => "EUR",
                _ => moneda?.Trim() ?? string.Empty
            };
        }
    }

    public class TotalProgramacionMoneda
    {
        private readonly CultureInfo _cultura;

        public TotalProgramacionMoneda(string moneda, decimal importe, decimal totalPagado, decimal saldoPendiente, CultureInfo cultura)
        {
            _cultura = cultura;
            Moneda = moneda;
            Importe = importe;
            TotalPagado = totalPagado;
            SaldoPendiente = saldoPendiente;
        }

        public string Moneda { get; }
        public decimal Importe { get; }
        public decimal TotalPagado { get; }
        public decimal SaldoPendiente { get; }
        public string SimboloMoneda => CuentaPorPagarProgramacionItem.ObtenerSimbolo(Moneda);
        public string ImporteTexto => $"{SimboloMoneda} {Importe.ToString("N2", _cultura)}";
        public string TotalPagadoTexto => $"{SimboloMoneda} {TotalPagado.ToString("N2", _cultura)}";
        public string SaldoPendienteTexto => $"{SimboloMoneda} {SaldoPendiente.ToString("N2", _cultura)}";
    }
}
