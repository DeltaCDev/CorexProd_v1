using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Helpers;
using System;
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

        public ProduccionView()
        {
            InitializeComponent();
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
                var ordenes = _negocio.Listar();
                OrdenesGrid.ItemsSource = ordenes;
                if (seleccion.HasValue)
                    OrdenesGrid.SelectedItem = ordenes.FirstOrDefault(x => x.IdOrdenTrabajo == seleccion.Value);

                UltimaActualizacionText.Text = $"Actualizado: {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                if (!silencioso)
                    NotificationService.Error($"No se pudieron cargar las OT: {ex.Message}");
            }
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
