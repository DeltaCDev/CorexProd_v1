using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace CorexProd.WPF.Modules.Produccion.Views
{
    public partial class ProduccionView : UserControl
    {
        private readonly OrdenTrabajoNegocio _negocio = new();
        private readonly DispatcherTimer _refreshTimer = new() { Interval = TimeSpan.FromSeconds(15) };
        private List<OrdenTrabajo> _ordenes = [];
        private bool _inicializandoFiltros;
        private bool _filtroPredeterminado = true;

        public ProduccionView()
        {
            InitializeComponent();
            InicializarFiltros();
            _refreshTimer.Tick += (_, _) => Cargar(silencioso: true);
            Loaded += (_, _) => _refreshTimer.Start();
            Unloaded += (_, _) => _refreshTimer.Stop();
            Cargar();
        }

        private void Cargar(bool silencioso = false)
        {
            try
            {
                int? seleccion = (OrdenesGrid.SelectedItem as OrdenTrabajo)?.IdOrdenTrabajo;
                _ordenes = _negocio.Listar();
                AplicarFiltros(seleccion);

                UltimaActualizacionText.Text = $"Actualizado: {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                if (!silencioso)
                    NotificationService.Error($"No se pudieron cargar las OT: {ex.Message}");
            }
        }

        private void InicializarFiltros()
        {
            _inicializandoFiltros = true;
            EstadoComboBox.ItemsSource = new[] { "Todos", "Pendiente", "En proceso", "Parcial", "Terminada", "Anulada" };
            EstadoComboBox.SelectedIndex = 0;
            FechaDesdePicker.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            FechaHastaPicker.SelectedDate = DateTime.Today;
            _inicializandoFiltros = false;
        }

        private void AplicarFiltros(int? seleccion = null)
        {
            IEnumerable<OrdenTrabajo> consulta = _ordenes;
            string texto = BuscarTextBox.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(texto))
            {
                consulta = consulta.Where(x =>
                    x.NumeroOT.Contains(texto, StringComparison.OrdinalIgnoreCase)
                    || x.NumeroOci.Contains(texto, StringComparison.OrdinalIgnoreCase)
                    || x.OrdenCompraCliente.Contains(texto, StringComparison.OrdinalIgnoreCase)
                    || x.NombreCliente.Contains(texto, StringComparison.OrdinalIgnoreCase));
            }

            string estado = EstadoComboBox.SelectedItem?.ToString() ?? "Todos";
            if (!estado.Equals("Todos", StringComparison.OrdinalIgnoreCase))
                consulta = consulta.Where(x => CoincideEstado(x.Estado, estado));
            DateTime? desde = FechaDesdePicker.SelectedDate?.Date;
            DateTime? hasta = FechaHastaPicker.SelectedDate?.Date;
            if (_filtroPredeterminado)
                consulta = consulta.Where(x => EsOtActiva(x.Estado) || CoincideRango(x.FechaEmision, desde, hasta));
            else
            {
                if (desde.HasValue) consulta = consulta.Where(x => x.FechaEmision.Date >= desde.Value);
                if (hasta.HasValue) consulta = consulta.Where(x => x.FechaEmision.Date <= hasta.Value);
            }

            List<OrdenTrabajo> visibles = consulta.ToList();
            OrdenesGrid.ItemsSource = visibles;
            if (seleccion.HasValue)
                OrdenesGrid.SelectedItem = visibles.FirstOrDefault(x => x.IdOrdenTrabajo == seleccion.Value);
            ResumenText.Text = $"{visibles.Count} de {_ordenes.Count} ordenes mostradas";
        }

        private static bool EsOtActiva(string estado) => estado.Trim().ToUpperInvariant() is not ("TERMINADA" or "TERMINADO" or "ANULADA" or "ANULADO");
        private static bool CoincideRango(DateTime fecha, DateTime? desde, DateTime? hasta) =>
            (!desde.HasValue || fecha.Date >= desde.Value) && (!hasta.HasValue || fecha.Date <= hasta.Value);

        private static bool CoincideEstado(string valor, string filtro)
        {
            string estado = valor.Trim().ToUpperInvariant();
            return filtro switch
            {
                "Pendiente" => estado is "PENDIENTE" or "EMITIDA",
                "En proceso" => estado is "EN_PROCESO" or "PROCESO",
                "Parcial" => estado == "PARCIAL",
                "Terminada" => estado is "TERMINADA" or "TERMINADO" or "FINALIZADA",
                "Anulada" => estado is "ANULADA" or "ANULADO",
                _ => true
            };
        }

        private void Filtro_Changed(object sender, RoutedEventArgs e)
        {
            if (!_inicializandoFiltros)
            {
                _filtroPredeterminado = false;
                AplicarFiltros((OrdenesGrid.SelectedItem as OrdenTrabajo)?.IdOrdenTrabajo);
            }
        }

        private void LimpiarFiltros_Click(object sender, RoutedEventArgs e)
        {
            _inicializandoFiltros = true;
            BuscarTextBox.Clear();
            EstadoComboBox.SelectedIndex = 0;
            FechaDesdePicker.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            FechaHastaPicker.SelectedDate = DateTime.Today;
            _filtroPredeterminado = true;
            _inicializandoFiltros = false;
            AplicarFiltros();
        }

        private void Actualizar_Click(object sender, RoutedEventArgs e) => Cargar();
        private void Abrir_Click(object sender, RoutedEventArgs e) { if ((sender as FrameworkElement)?.DataContext is OrdenTrabajo ot) Abrir(ot); }
        private void OrdenesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e) { if (OrdenesGrid.SelectedItem is OrdenTrabajo ot) Abrir(ot); }
        private void Kardex_Click(object sender, RoutedEventArgs e) { if ((sender as FrameworkElement)?.DataContext is OrdenTrabajo ot) AbrirVentana(() => new OrdenTrabajoKardexWindow(ot) { Owner = Application.Current.MainWindow, Title = $"Kardex de {ot.NumeroOT}" }); }
        private void Historial_Click(object sender, RoutedEventArgs e) { if ((sender as FrameworkElement)?.DataContext is OrdenTrabajo ot) AbrirVentana(() => new OrdenTrabajoHistorialWindow(ot) { Owner = Application.Current.MainWindow, Title = $"Historial de {ot.NumeroOT}" }); }
        private void Abrir(OrdenTrabajo ot) { AbrirVentana(() => new OrdenTrabajoDetalleWindow(ot.IdOrdenTrabajo) { Owner = Application.Current.MainWindow }); Cargar(); }
        private static void AbrirVentana(Func<Window> crear) { try { crear().ShowDialog(); } catch (Exception ex) { NotificationService.Error($"No se pudo abrir la ventana: {ex.Message}"); } }
    }
}
