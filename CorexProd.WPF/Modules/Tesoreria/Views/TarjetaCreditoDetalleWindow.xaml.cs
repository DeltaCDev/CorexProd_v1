using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Helpers;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CorexProd.WPF.Modules.Tesoreria.Views
{
    public partial class TarjetaCreditoDetalleWindow : Window
    {
        private readonly TarjetaCreditoNegocio _negocio = new();
        private readonly int _id;
        private TarjetaCredito? _tarjeta;

        public TarjetaCreditoDetalleWindow(int id)
        {
            InitializeComponent();
            _id = id;
            Refrescar();
        }

        private void Refrescar()
        {
            try
            {
                _tarjeta = _negocio.ObtenerTarjeta(_id);
                if (_tarjeta == null)
                {
                    Close();
                    return;
                }

                DataContext = _tarjeta;
                TituloTextBlock.Text = _tarjeta.DescripcionTarjeta;
                EstadoTextBlock.Text = $"Estado: {_tarjeta.Estado} · Monedas: {string.Join(" / ", _tarjeta.MonedasHabilitadas)}";
                NuevoPeriodoButton.IsEnabled = _tarjeta.EstaActiva;
                NuevoPeriodoButton.ToolTip = _tarjeta.EstaActiva
                    ? "Crear período"
                    : "Solo una tarjeta ACTIVA permite nuevos períodos.";
                PeriodosDataGrid.ItemsSource = _negocio.ListarPeriodos(_id)
                    .OrderByDescending(x => x.PeriodoAnio).ThenByDescending(x => x.PeriodoMes)
                    .Select(periodo => _negocio.ObtenerPeriodo(periodo.IdTarjetaPeriodo) ?? periodo)
                    .ToList();
                HistorialDataGrid.ItemsSource = _negocio.ListarHistorial(_id);
                PagosDataGrid.ItemsSource = null;
            }
            catch (Exception ex)
            {
                NotificationService.Error(ex.Message);
            }
        }

        private void Actualizar_Click(object sender, RoutedEventArgs e) => Refrescar();

        private void NuevoPeriodo_Click(object sender, RoutedEventArgs e)
        {
            if (_tarjeta?.EstaActiva != true)
            {
                NotificationService.Warning("La tarjeta debe estar ACTIVA para crear períodos.");
                return;
            }
            AbrirPeriodo(null);
        }

        private void EditarPeriodo_Click(object sender, RoutedEventArgs e)
        {
            TarjetaCreditoPeriodo? periodo = (sender as FrameworkElement)?.Tag as TarjetaCreditoPeriodo;
            if (_tarjeta?.EstaAnulada == true || periodo?.EstaAnulado == true)
            {
                NotificationService.Warning("No se puede editar un período anulado.");
                return;
            }
            AbrirPeriodo(periodo);
        }

        private void AbrirPeriodo(TarjetaCreditoPeriodo? periodo)
        {
            if (_tarjeta == null || (periodo == null && !_tarjeta.EstaActiva))
                return;

            TarjetaCreditoPeriodoWindow ventana = new(_tarjeta, periodo) { Owner = this };
            if (ventana.ShowDialog() == true)
                Refrescar();
        }

        private void AnularPeriodo_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is not TarjetaCreditoPeriodo periodo || _tarjeta?.EstaAnulada != false)
                return;
            if (!ConfirmDialogService.Confirmar("¿Desea anular el período seleccionado?", "Confirmar anulación de período"))
                return;

            MotivoAnulacionWindow ventana = new("Anular período", $"Período {periodo.PeriodoTexto}") { Owner = this };
            if (ventana.ShowDialog() != true)
                return;

            TarjetaCreditoPeriodoResultado resultado = _negocio.AnularPeriodo(
                periodo.IdTarjetaPeriodo, SessionManager.UsuarioActual?.NombreUsuario ?? "Sistema", ventana.Motivo);
            if (resultado.Resultado)
            {
                NotificationService.Success(resultado.Mensaje);
                Refrescar();
            }
            else
                NotificationService.Warning(resultado.Mensaje);
        }

        private void Periodo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PeriodosDataGrid.SelectedItem is TarjetaCreditoPeriodo periodo)
            {
                TarjetaCreditoPeriodo? detalle = _negocio.ObtenerPeriodo(periodo.IdTarjetaPeriodo);
                PagosDataGrid.ItemsSource = detalle?.Pagos.OrderByDescending(x => x.FechaPago).ToList();
            }
        }

        private void Pagar_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is not TarjetaCreditoPeriodo periodo || _tarjeta == null)
                return;

            bool tieneSaldo = (periodo.SaldoPendientePEN ?? 0) > 0 || (periodo.SaldoPendienteUSD ?? 0) > 0;
            if (_tarjeta.EstaAnulada || periodo.EstaAbierto || periodo.EstaAnulado || !tieneSaldo)
            {
                NotificationService.Warning("El período seleccionado no tiene saldo disponible para pago.");
                return;
            }

            TarjetaCreditoPagoWindow ventana = new(_tarjeta, periodo) { Owner = this };
            if (ventana.ShowDialog() == true)
                Refrescar();
        }

        private void Revertir_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is not TarjetaCreditoPago pago || _tarjeta == null)
                return;
            if (_tarjeta.EstaAnulada || pago.EstaAnulado)
            {
                NotificationService.Warning("No se puede revertir este pago.");
                return;
            }
            if (!ConfirmDialogService.Confirmar("¿Desea revertir lógicamente el pago seleccionado?", "Confirmar reversión"))
                return;

            MotivoAnulacionWindow ventana = new("Revertir pago", $"Pago #{pago.IdPago}") { Owner = this };
            if (ventana.ShowDialog() != true)
                return;

            TarjetaCreditoPagoResultado resultado = _negocio.AnularPago(
                pago.IdPago, SessionManager.UsuarioActual?.NombreUsuario ?? "Sistema", ventana.Motivo);
            if (resultado.Resultado)
            {
                NotificationService.Success(resultado.Mensaje);
                Refrescar();
            }
            else
                NotificationService.Warning(resultado.Mensaje);
        }
    }
}
