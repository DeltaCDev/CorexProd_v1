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
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Tesoreria.ViewModels
{
    public class ProgramacionGeneralPagosViewModel : BaseViewModel
    {
        private readonly ProgramacionTesoreriaNegocio _programacionNegocio = new();
        private readonly CuentaPorPagarNegocio _cuentaNegocio = new();
        private readonly ObligacionFinancieraNegocio _obligacionNegocio = new();
        private readonly TarjetaCreditoNegocio _tarjetaNegocio = new();
        private readonly CultureInfo _cultura = new("es-PE");
        private readonly string[] _estadosOperativos = ["PENDIENTE", "PARCIAL", "CANCELADA"];
        private readonly List<ProgramacionPagoGeneralItem> _pagosFiltrados = [];
        private DateTime _fechaDesde;
        private DateTime _fechaHasta;
        private string _tipoFiltro = "Todos";
        private string _monedaFiltro = "Todos";
        private string _textoBusqueda = string.Empty;
        private bool _incluirPendientesAnteriores;
        private bool _isLoading;
        private bool _actualizandoSemana;
        private bool _actualizandoEstados;
        private ProgramacionPagoGeneralItemViewModel? _pagoSeleccionado;

        public ProgramacionGeneralPagosViewModel()
        {
            Estados = new ObservableCollection<ProgramacionEstadoFiltroItem>
            {
                new("TODOS", true, OnEstadoSeleccionado),
                new("PENDIENTE", true, OnEstadoSeleccionado),
                new("PARCIAL", true, OnEstadoSeleccionado),
                new("CANCELADA", true, OnEstadoSeleccionado)
            };
            Tipos = new ObservableCollection<string> { "Todos", "LETRA", "FACTURA CRÉDITO", "PRÉSTAMO", "TARJETA" };
            Monedas = new ObservableCollection<string> { "Todos", "PEN", "USD" };

            SemanaAnteriorCommand = new RelayCommand(_ => CambiarSemana(-7));
            SemanaActualCommand = new RelayCommand(_ => EstablecerSemana(DateTime.Today));
            SemanaSiguienteCommand = new RelayCommand(_ => CambiarSemana(7));
            ActualizarCommand = new RelayCommand(_ => CargarProgramacion());
            VerDetalleCommand = new RelayCommand(p => VerDetalle(p));
            ExportarExcelCommand = new RelayCommand(_ => ExportarExcel());
            ExportarPdfCommand = new RelayCommand(_ => ExportarPdf());

            EstablecerSemana(DateTime.Today);
        }

        public ObservableCollection<ProgramacionEstadoFiltroItem> Estados { get; }
        public ObservableCollection<string> Tipos { get; }
        public ObservableCollection<string> Monedas { get; }
        public ObservableCollection<ProgramacionGeneralDia> Dias { get; } = [];
        public ObservableCollection<ProgramacionGeneralTotalMoneda> TotalesSemana { get; } = [];
        public ObservableCollection<ProgramacionGeneralTotalMoneda> TotalesPendientesAnteriores { get; } = [];
        public ObservableCollection<ProgramacionGeneralTotalMoneda> TotalesGenerales { get; } = [];
        public ICommand SemanaAnteriorCommand { get; }
        public ICommand SemanaActualCommand { get; }
        public ICommand SemanaSiguienteCommand { get; }
        public ICommand ActualizarCommand { get; }
        public ICommand VerDetalleCommand { get; }
        public ICommand ExportarExcelCommand { get; }
        public ICommand ExportarPdfCommand { get; }

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

        public string EstadoFiltroTexto => Estados.Count > 0 && Estados.Where(e => !e.EsTodos).All(e => e.IsSelected)
            ? "Todos"
            : string.Join(", ", Estados.Where(e => !e.EsTodos && e.IsSelected).Select(e => e.Nombre));

        public string TipoFiltro
        {
            get => _tipoFiltro;
            set { _tipoFiltro = value ?? "Todos"; OnPropertyChanged(); CargarProgramacion(); }
        }

        public string MonedaFiltro
        {
            get => _monedaFiltro;
            set { _monedaFiltro = value ?? "Todos"; OnPropertyChanged(); CargarProgramacion(); }
        }

        public string TextoBusqueda
        {
            get => _textoBusqueda;
            set { _textoBusqueda = value ?? string.Empty; OnPropertyChanged(); CargarProgramacion(); }
        }

        public bool IncluirPendientesAnteriores
        {
            get => _incluirPendientesAnteriores;
            set { _incluirPendientesAnteriores = value; OnPropertyChanged(); CargarProgramacion(); }
        }

        public ProgramacionPagoGeneralItemViewModel? PagoSeleccionado
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
        public Visibility PendientesAnterioresVisibility => IncluirPendientesAnteriores && TotalesPendientesAnteriores.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        public string RangoSemana => $"{FechaDesde:dd/MM/yyyy} - {FechaHasta:dd/MM/yyyy}";
        public string ResumenSemana => $"{Dias.Sum(d => d.Pagos.Count)} pago(s) programado(s)";

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
                List<ProgramacionPagoGeneralItemViewModel> pagos = _programacionNegocio
                    .ObtenerProgramacionGeneral(
                        FechaDesde,
                        FechaHasta,
                        IncluirPendientesAnteriores,
                        null,
                        TipoFiltro,
                        MonedaFiltro,
                        TextoBusqueda)
                    .Where(CoincideEstadosSeleccionados)
                    .Select(p => new ProgramacionPagoGeneralItemViewModel(p, _cultura))
                    .OrderBy(p => p.FechaVencimiento)
                    .ThenBy(p => p.Categoria)
                    .ThenBy(p => p.EntidadProveedor)
                    .ThenBy(p => p.NumeroCuota ?? 0)
                    .ToList();

                _pagosFiltrados.Clear();
                _pagosFiltrados.AddRange(pagos.Select(p => p.Item));

                Dias.Clear();
                if (IncluirPendientesAnteriores)
                {
                    List<ProgramacionPagoGeneralItemViewModel> anteriores = pagos.Where(p => p.EsPendienteAnterior).ToList();
                    if (anteriores.Count > 0)
                        Dias.Add(new ProgramacionGeneralDia(null, "PENDIENTES ANTERIORES", anteriores, _cultura));
                }

                for (DateTime fecha = FechaDesde; fecha <= FechaHasta; fecha = fecha.AddDays(1))
                {
                    List<ProgramacionPagoGeneralItemViewModel> pagosDia = pagos
                        .Where(p => !p.EsPendienteAnterior && p.FechaVencimiento.Date == fecha.Date)
                        .ToList();

                    Dias.Add(new ProgramacionGeneralDia(fecha, null, pagosDia, _cultura));
                }

                RecalcularTotales(pagos);
                NotificarResumen();
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudo cargar la programacion general: {MensajeUsuario(ex)}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void RecalcularTotales(IEnumerable<ProgramacionPagoGeneralItemViewModel> pagos)
        {
            List<ProgramacionPagoGeneralItemViewModel> lista = pagos.ToList();
            TotalesSemana.Clear();
            TotalesPendientesAnteriores.Clear();
            TotalesGenerales.Clear();

            foreach (ProgramacionGeneralTotalMoneda total in CrearTotales(lista.Where(p => !p.EsPendienteAnterior), _cultura))
                TotalesSemana.Add(total);

            foreach (ProgramacionGeneralTotalMoneda total in CrearTotales(lista.Where(p => p.EsPendienteAnterior), _cultura))
                TotalesPendientesAnteriores.Add(total);

            foreach (ProgramacionGeneralTotalMoneda total in CrearTotales(lista, _cultura))
                TotalesGenerales.Add(total);

            OnPropertyChanged(nameof(PendientesAnterioresVisibility));
        }

        private void NotificarResumen()
        {
            OnPropertyChanged(nameof(ResumenSemana));
            OnPropertyChanged(nameof(EmptyVisibility));
        }

        private void VerDetalle(object? parametro)
        {
            ProgramacionPagoGeneralItemViewModel? pago = parametro as ProgramacionPagoGeneralItemViewModel ?? PagoSeleccionado;
            if (pago == null)
            {
                NotificationService.Warning("Debe seleccionar un pago programado.");
                return;
            }

            try
            {
                if (pago.OrigenTipo.Equals("CXP", StringComparison.OrdinalIgnoreCase))
                {
                    CuentaPorPagar? cuenta = _cuentaNegocio.Obtener(pago.OrigenId);
                    if (cuenta == null)
                    {
                        NotificationService.Warning("No se encontro la cuenta por pagar seleccionada.");
                        return;
                    }

                    CuentaPorPagarDetalleWindow ventana = new(cuenta)
                    {
                        Owner = Application.Current.MainWindow
                    };
                    ventana.ShowDialog();
                }
                else if (pago.OrigenTipo.Equals("PRESTAMO", StringComparison.OrdinalIgnoreCase))
                {
                    ObligacionFinanciera? prestamo = _obligacionNegocio.Obtener(pago.OrigenId);
                    if (prestamo == null)
                    {
                        NotificationService.Warning("No se encontro el prestamo seleccionado.");
                        return;
                    }

                    PrestamoDetalleWindow ventana = new(prestamo)
                    {
                        Owner = Application.Current.MainWindow
                    };
                    ventana.ShowDialog();
                }
                else if (pago.OrigenTipo.Equals("TARJETA", StringComparison.OrdinalIgnoreCase))
                {
                    TarjetaCredito? tarjeta = _tarjetaNegocio.ObtenerTarjeta(pago.OrigenId);
                    if (tarjeta == null)
                    {
                        NotificationService.Warning("No se encontro la tarjeta de credito seleccionada.");
                        return;
                    }

                    TarjetaCreditoDetalleWindow ventana = new(tarjeta.IdTarjetaCredito)
                    {
                        Owner = Application.Current.MainWindow
                    };
                    ventana.ShowDialog();
                }
                else
                {
                    NotificationService.Warning("El origen del pago programado no esta soportado.");
                    return;
                }

                CargarProgramacion();
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudo abrir el detalle: {MensajeUsuario(ex)}");
            }
        }

        private void ExportarExcel()
        {
            if (FechaDesde == default || FechaHasta == default || FechaHasta < FechaDesde)
            {
                NotificationService.Warning("Debe seleccionar un rango de fechas valido.");
                return;
            }

            try
            {
                List<ProgramacionPagoGeneralItem> pagos = ObtenerDatosExportacion();
                if (pagos.Count == 0)
                {
                    NotificationService.Warning("No existen pagos para exportar con los filtros seleccionados.");
                    return;
                }

                SaveFileDialog dialog = new()
                {
                    Title = "Exportar programacion general de pagos",
                    FileName = $"Programacion_Pagos_{FechaDesde:yyyy-MM-dd}_{FechaHasta:yyyy-MM-dd}.xlsx",
                    Filter = "Excel (*.xlsx)|*.xlsx",
                    DefaultExt = ".xlsx",
                    AddExtension = true,
                    OverwritePrompt = true
                };

                if (dialog.ShowDialog() != true)
                    return;

                ProgramacionGeneralExcelExporter.Exportar(
                    dialog.FileName,
                    pagos,
                    FechaDesde,
                    FechaHasta,
                    IncluirPendientesAnteriores);

                NotificationService.Success("Excel generado correctamente.");
            }
            catch (IOException ex)
            {
                NotificationService.Error($"No se pudo guardar el Excel. Verifique que el archivo no este abierto: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                NotificationService.Error($"No se pudo guardar el Excel por permisos de carpeta: {ex.Message}");
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudo exportar el Excel: {MensajeUsuario(ex)}");
            }
        }

        private void ExportarPdf()
        {
            if (FechaDesde == default || FechaHasta == default || FechaHasta < FechaDesde)
            {
                NotificationService.Warning("Debe seleccionar un rango de fechas valido.");
                return;
            }

            try
            {
                List<ProgramacionPagoGeneralItem> pagos = ObtenerDatosExportacion();
                if (pagos.Count == 0)
                {
                    NotificationService.Warning("No existen pagos para exportar con los filtros seleccionados.");
                    return;
                }

                SaveFileDialog dialog = new()
                {
                    Title = "Exportar programacion general de pagos en PDF",
                    FileName = $"Programacion_Pagos_{FechaDesde:yyyy-MM-dd}_{FechaHasta:yyyy-MM-dd}.pdf",
                    Filter = "PDF (*.pdf)|*.pdf",
                    DefaultExt = ".pdf",
                    AddExtension = true,
                    OverwritePrompt = true
                };

                if (dialog.ShowDialog() != true)
                    return;

                ProgramacionGeneralPdfExporter.Exportar(
                    dialog.FileName,
                    pagos,
                    FechaDesde,
                    FechaHasta,
                    IncluirPendientesAnteriores);

                NotificationService.Success("PDF generado correctamente.");
            }
            catch (IOException ex)
            {
                NotificationService.Error($"No se pudo guardar el PDF. Verifique que el archivo no este abierto: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                NotificationService.Error($"No se pudo guardar el PDF por permisos de carpeta: {ex.Message}");
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudo exportar el PDF: {MensajeUsuario(ex)}");
            }
        }

        private List<ProgramacionPagoGeneralItem> ObtenerDatosExportacion()
        {
            return _pagosFiltrados
                .OrderBy(p => p.FechaVencimiento)
                .ThenBy(p => p.Categoria)
                .ThenBy(p => p.EntidadProveedor)
                .ThenBy(p => p.NumeroCuota ?? 0)
                .ToList();
        }

        private void OnEstadoSeleccionado(ProgramacionEstadoFiltroItem item)
        {
            if (_actualizandoEstados)
                return;

            _actualizandoEstados = true;
            if (item.EsTodos)
            {
                foreach (ProgramacionEstadoFiltroItem estado in Estados.Where(e => !e.EsTodos))
                    estado.SetIsSelected(item.IsSelected, notifyParent: false);
            }
            else
            {
                ProgramacionEstadoFiltroItem? todos = Estados.FirstOrDefault(e => e.EsTodos);
                todos?.SetIsSelected(Estados.Where(e => !e.EsTodos).All(e => e.IsSelected), notifyParent: false);
            }

            _actualizandoEstados = false;
            OnPropertyChanged(nameof(EstadoFiltroTexto));
            CargarProgramacion();
        }

        private bool CoincideEstadosSeleccionados(ProgramacionPagoGeneralItem pago)
        {
            HashSet<string> seleccionados = Estados
                .Where(e => !e.EsTodos && e.IsSelected)
                .Select(e => e.Nombre)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return seleccionados.Count > 0 && seleccionados.Contains(pago.Estado);
        }

        internal static List<ProgramacionGeneralTotalMoneda> CrearTotales(IEnumerable<ProgramacionPagoGeneralItemViewModel> pagos, CultureInfo cultura)
        {
            return pagos
                .GroupBy(p => p.Moneda)
                .OrderBy(g => g.Key)
                .Select(g => new ProgramacionGeneralTotalMoneda(
                    g.Key,
                    g.Sum(p => p.Importe),
                    g.Sum(p => p.Pagado),
                    g.Sum(p => p.Saldo),
                    cultura))
                .ToList();
        }

        private static string MensajeUsuario(Exception ex)
        {
            return ex is ArgumentException ? ex.Message : ex.Message.Split('\r', '\n').FirstOrDefault() ?? ex.Message;
        }
    }

    public class ProgramacionGeneralDia
    {
        private readonly CultureInfo _cultura;

        public ProgramacionGeneralDia(DateTime? fecha, string? titulo, IEnumerable<ProgramacionPagoGeneralItemViewModel> pagos, CultureInfo cultura)
        {
            Fecha = fecha;
            Titulo = titulo;
            _cultura = cultura;

            foreach (ProgramacionPagoGeneralItemViewModel pago in pagos)
                Pagos.Add(pago);

            foreach (ProgramacionGeneralTotalMoneda total in ProgramacionGeneralPagosViewModel.CrearTotales(Pagos, cultura))
                Totales.Add(total);
        }

        public DateTime? Fecha { get; }
        public string? Titulo { get; }
        public ObservableCollection<ProgramacionPagoGeneralItemViewModel> Pagos { get; } = [];
        public ObservableCollection<ProgramacionGeneralTotalMoneda> Totales { get; } = [];
        public string DiaTitulo => string.IsNullOrWhiteSpace(Titulo)
            ? $"{_cultura.DateTimeFormat.GetDayName(Fecha!.Value.DayOfWeek).ToUpperInvariant()} {Fecha:dd/MM/yyyy}"
            : Titulo!;
        public string ResumenDia => Pagos.Count == 0 ? "Sin pagos programados" : $"{Pagos.Count} registro(s)";
        public Visibility TotalesVisibility => Totales.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EmptyVisibility => Pagos.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public class ProgramacionPagoGeneralItemViewModel
    {
        private readonly CultureInfo _cultura;

        public ProgramacionPagoGeneralItemViewModel(ProgramacionPagoGeneralItem item, CultureInfo cultura)
        {
            Item = item;
            _cultura = cultura;
            FechaVencimiento = item.FechaVencimiento;
            Categoria = item.Categoria;
            Subcategoria = item.Subcategoria;
            EntidadProveedor = item.EntidadProveedor;
            Titular = item.Titular;
            EmpresaResponsable = item.EmpresaResponsable;
            Concepto = item.Concepto;
            Referencia = item.Referencia;
            NumeroCuota = item.NumeroCuota;
            TotalCuotas = item.TotalCuotas;
            Moneda = item.Moneda;
            Importe = item.Importe;
            Pagado = item.Pagado;
            Saldo = item.Saldo;
            Estado = item.Estado;
            OrigenTipo = item.OrigenTipo;
            OrigenId = item.OrigenId;
            OrigenDetalleId = item.OrigenDetalleId;
            EsPendienteAnterior = item.EsPendienteAnterior;
        }

        public ProgramacionPagoGeneralItem Item { get; }
        public DateTime FechaVencimiento { get; }
        public string Categoria { get; }
        public string Subcategoria { get; }
        public string EntidadProveedor { get; }
        public string Titular { get; }
        public string EmpresaResponsable { get; }
        public string Concepto { get; }
        public string Referencia { get; }
        public int? NumeroCuota { get; }
        public int? TotalCuotas { get; }
        public string Moneda { get; }
        public decimal Importe { get; }
        public decimal Pagado { get; }
        public decimal Saldo { get; }
        public string Estado { get; }
        public string OrigenTipo { get; }
        public int OrigenId { get; }
        public int OrigenDetalleId { get; }
        public bool EsPendienteAnterior { get; }
        public string TipoTexto => Categoria switch
        {
            "FACTURA_CREDITO" => "FACTURA CRÉDITO",
            "PRESTAMO" => "PRÉSTAMO",
            "LETRA" => "LETRA",
            "TARJETA" => "TARJETA",
            _ => Categoria
        };
        public string CuotaTexto => NumeroCuota.HasValue && TotalCuotas.HasValue ? $"{NumeroCuota}/{TotalCuotas}" : string.Empty;
        public string SimboloMoneda => ObtenerSimbolo(Moneda);
        public string ImporteTexto => $"{SimboloMoneda} {Importe.ToString("N2", _cultura)}";
        public string PagadoTexto => $"{SimboloMoneda} {Pagado.ToString("N2", _cultura)}";
        public string SaldoTexto => $"{SimboloMoneda} {Saldo.ToString("N2", _cultura)}";
        public bool EstaVencido => FechaVencimiento.Date < DateTime.Today && Saldo > 0 && !Estado.Equals("CANCELADA", StringComparison.OrdinalIgnoreCase);
        public string EstadoTexto => EstaVencido ? "VENCIDO" : Estado;

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

    public class ProgramacionEstadoFiltroItem : BaseViewModel
    {
        private readonly Action<ProgramacionEstadoFiltroItem> _onChanged;
        private bool _isSelected;

        public ProgramacionEstadoFiltroItem(string nombre, bool isSelected, Action<ProgramacionEstadoFiltroItem> onChanged)
        {
            Nombre = nombre;
            _isSelected = isSelected;
            _onChanged = onChanged;
        }

        public string Nombre { get; }
        public bool EsTodos => Nombre.Equals("TODOS", StringComparison.OrdinalIgnoreCase);

        public bool IsSelected
        {
            get => _isSelected;
            set => SetIsSelected(value);
        }

        internal void SetIsSelected(bool value, bool notifyParent = true)
        {
            if (_isSelected == value)
                return;

            _isSelected = value;
            OnPropertyChanged(nameof(IsSelected));

            if (notifyParent)
                _onChanged(this);
        }
    }

    public class ProgramacionGeneralTotalMoneda
    {
        private readonly CultureInfo _cultura;

        public ProgramacionGeneralTotalMoneda(string moneda, decimal importe, decimal pagado, decimal saldo, CultureInfo cultura)
        {
            _cultura = cultura;
            Moneda = moneda;
            Importe = importe;
            Pagado = pagado;
            Saldo = saldo;
        }

        public string Moneda { get; }
        public decimal Importe { get; }
        public decimal Pagado { get; }
        public decimal Saldo { get; }
        public string SimboloMoneda => ProgramacionPagoGeneralItemViewModel.ObtenerSimbolo(Moneda);
        public string ImporteTexto => $"{SimboloMoneda} {Importe.ToString("N2", _cultura)}";
        public string PagadoTexto => $"{SimboloMoneda} {Pagado.ToString("N2", _cultura)}";
        public string SaldoTexto => $"{SimboloMoneda} {Saldo.ToString("N2", _cultura)}";
    }
}
