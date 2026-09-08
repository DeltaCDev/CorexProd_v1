using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Helpers;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CorexProd.WPF.Modules.Tesoreria.Views
{
    public partial class PrestamoDetalleWindow : Window
    {
        private readonly ObligacionFinancieraNegocio _negocio = new();
        private readonly CultureInfo _cultura = new("es-PE");
        private readonly int _idPrestamo;
        private ObligacionFinanciera _prestamo;

        public PrestamoDetalleWindow(ObligacionFinanciera prestamo)
        {
            InitializeComponent();
            _prestamo = prestamo;
            _idPrestamo = prestamo.IdObligacionFinanciera;
            Refrescar();
        }

        private void Refrescar()
        {
            ObligacionFinanciera? detalle = _negocio.Obtener(_idPrestamo);
            if (detalle == null)
            {
                NotificationService.Warning("No se encontro el prestamo seleccionado.");
                Close();
                return;
            }

            _prestamo = detalle;
            TituloTextBlock.Text = $"Prestamo #{_prestamo.IdObligacionFinanciera}";
            SubtituloTextBlock.Text = _prestamo.Concepto;
            EstadoTextBlock.Text = _prestamo.Estado;
            EntidadTextBlock.Text = _prestamo.EntidadFinanciera;
            TitularTextBlock.Text = _prestamo.NombreTitular;
            MontoTextBlock.Text = $"{_prestamo.Moneda} {(_prestamo.MontoOriginal ?? _prestamo.TotalCronograma).ToString("N2", _cultura)}";
            PagadoTextBlock.Text = $"{_prestamo.Moneda} {_prestamo.TotalPagado.ToString("N2", _cultura)}";
            SaldoTextBlock.Text = $"{_prestamo.Moneda} {_prestamo.SaldoPendiente.ToString("N2", _cultura)}";

            PintarEstado(_prestamo.Estado);
            ActualizarBotones();
            CuotasDataGrid.ItemsSource = _prestamo.Cuotas.OrderBy(c => c.NumeroCuota).ToList();
            PagosDataGrid.ItemsSource = _prestamo.Pagos.OrderByDescending(p => p.FechaPago).ThenByDescending(p => p.IdPago).ToList();
            HistorialDataGrid.ItemsSource = _prestamo.Historial.OrderByDescending(h => h.FechaHora).ToList();
        }

        private void ActualizarBotones()
        {
            RegistrarPagoButton.IsEnabled = !_prestamo.EstaAnulada && !_prestamo.EstaCancelada && _prestamo.Cuotas.Any(c => c.SaldoPendiente > 0);
            RegistrarPagoButton.ToolTip = RegistrarPagoButton.IsEnabled ? "Registrar pago de la cuota seleccionada" : "No hay cuotas pendientes disponibles para pago";

            EditarButton.IsEnabled = !_prestamo.EstaAnulada;
            EditarButton.ToolTip = EditarButton.IsEnabled ? "Editar prestamo" : "No se puede editar un prestamo anulado";

            AnularPrestamoButton.IsEnabled = !_prestamo.EstaAnulada;
            AnularPrestamoButton.ToolTip = AnularPrestamoButton.IsEnabled ? "Anular prestamo" : "El prestamo ya esta anulado";
        }

        private void PintarEstado(string estado)
        {
            Color fondo = Color.FromRgb(254, 226, 226);
            Color borde = Color.FromRgb(239, 68, 68);
            Color texto = Color.FromRgb(185, 28, 28);

            if (estado.Equals("PARCIAL", StringComparison.OrdinalIgnoreCase))
            {
                fondo = Color.FromRgb(254, 243, 199);
                borde = Color.FromRgb(245, 158, 11);
                texto = Color.FromRgb(146, 64, 14);
            }
            else if (estado.Equals("CANCELADA", StringComparison.OrdinalIgnoreCase))
            {
                fondo = Color.FromRgb(220, 252, 231);
                borde = Color.FromRgb(34, 197, 94);
                texto = Color.FromRgb(22, 101, 52);
            }
            else if (estado.Equals("ANULADA", StringComparison.OrdinalIgnoreCase))
            {
                fondo = Color.FromRgb(229, 231, 235);
                borde = Color.FromRgb(148, 163, 184);
                texto = Color.FromRgb(71, 85, 105);
            }

            EstadoBorder.Background = new SolidColorBrush(fondo);
            EstadoBorder.BorderBrush = new SolidColorBrush(borde);
            EstadoBorder.BorderThickness = new Thickness(1);
            EstadoTextBlock.Foreground = new SolidColorBrush(texto);
        }

        private void RegistrarPago_Click(object sender, RoutedEventArgs e)
        {
            if (_prestamo.EstaAnulada || _prestamo.EstaCancelada)
            {
                NotificationService.Warning("No se puede registrar pagos en un prestamo anulado o cancelado.");
                return;
            }

            if (CuotasDataGrid.SelectedItem is not ObligacionFinancieraCuota cuota)
            {
                ObligacionFinancieraCuota? siguienteCuota = _prestamo.Cuotas
                    .OrderBy(c => c.FechaVencimiento)
                    .FirstOrDefault(c => c.SaldoPendiente > 0 && !c.Estado.Equals("ANULADA", StringComparison.OrdinalIgnoreCase));
                if (siguienteCuota == null)
                {
                    NotificationService.Warning("No hay cuotas pendientes para registrar pago.");
                    return;
                }

                cuota = siguienteCuota;
            }

            if (cuota.SaldoPendiente <= 0)
            {
                NotificationService.Warning("La cuota seleccionada no tiene saldo pendiente.");
                return;
            }

            RegistrarPagoObligacionFinancieraWindow ventana = new(_prestamo, cuota)
            {
                Owner = this
            };

            if (ventana.ShowDialog() == true)
                Refrescar();
        }

        private void Editar_Click(object sender, RoutedEventArgs e)
        {
            if (_prestamo.EstaAnulada)
            {
                NotificationService.Warning("No se puede editar un prestamo anulado.");
                return;
            }

            PrestamoEditorWindow ventana = new(_prestamo)
            {
                Owner = this
            };

            if (ventana.ShowDialog() == true)
                Refrescar();
        }

        private void AnularPrestamo_Click(object sender, RoutedEventArgs e)
        {
            if (_prestamo.EstaAnulada)
            {
                NotificationService.Warning("El prestamo ya se encuentra anulado.");
                return;
            }

            if (!ConfirmDialogService.Confirmar("Desea anular el prestamo?", "Confirmar anulacion"))
                return;

            AnularPrestamoWindow ventana = new($"#{_prestamo.IdObligacionFinanciera} - {_prestamo.EntidadFinanciera}")
            {
                Owner = this
            };

            if (ventana.ShowDialog() != true)
                return;

            string usuario = SessionManager.UsuarioActual?.NombreCompleto
                ?? SessionManager.UsuarioActual?.NombreUsuario
                ?? "Sistema";

            ObligacionFinancieraResultado resultado = _negocio.Anular(_prestamo.IdObligacionFinanciera, usuario, ventana.MotivoAnulacion);
            if (resultado.Resultado)
            {
                NotificationService.Success(resultado.Mensaje);
                Refrescar();
            }
            else
            {
                NotificationService.Warning(resultado.Mensaje);
            }
        }

        private void AnularPago_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not ObligacionFinancieraPago pago)
                return;

            if (pago.EstaAnulado)
            {
                NotificationService.Warning("El pago ya se encuentra anulado.");
                return;
            }

            if (!ConfirmDialogService.Confirmar("Desea anular el pago seleccionado?", "Confirmar anulacion de pago"))
                return;

            AnularPagoObligacionFinancieraWindow ventana = new($"Pago #{pago.IdPago} - cuota {pago.NumeroCuota}")
            {
                Owner = this
            };

            if (ventana.ShowDialog() != true)
                return;

            string usuario = SessionManager.UsuarioActual?.NombreCompleto
                ?? SessionManager.UsuarioActual?.NombreUsuario
                ?? "Sistema";

            ObligacionFinancieraPagoResultado resultado = _negocio.AnularPago(pago.IdPago, ventana.MotivoAnulacion, usuario);
            if (resultado.Resultado)
            {
                NotificationService.Success(resultado.Mensaje);
                Refrescar();
            }
            else
            {
                NotificationService.Warning(resultado.Mensaje);
            }
        }
    }
}
