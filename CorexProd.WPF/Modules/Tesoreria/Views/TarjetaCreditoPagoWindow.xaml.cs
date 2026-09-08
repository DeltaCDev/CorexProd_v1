using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Helpers;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CorexProd.WPF.Modules.Tesoreria.Views
{
    public partial class TarjetaCreditoPagoWindow : Window
    {
        private readonly TarjetaCreditoNegocio _negocio = new();
        private readonly TarjetaCreditoPeriodo _periodo;
        private readonly TarjetaCredito _tarjeta;

        public TarjetaCreditoPagoWindow(TarjetaCredito tarjeta, TarjetaCreditoPeriodo periodo)
        {
            InitializeComponent();
            _tarjeta = tarjeta;
            _periodo = periodo;
            FechaDatePicker.SelectedDate = DateTime.Today;
            MonedaComboBox.SelectedIndex = periodo.SaldoPendientePEN > 0 ? 0 : 1;
            ActualizarMonedaSeleccionada();

            try
            {
                BancoComboBox.ItemsSource = _negocio.ListarBancos();
            }
            catch (Exception ex)
            {
                NotificationService.Error(ex.Message);
            }
        }

        private string MonedaSeleccionada =>
            (MonedaComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;

        private decimal SaldoSeleccionado => MonedaSeleccionada == "PEN"
            ? _periodo.SaldoPendientePEN ?? 0
            : _periodo.SaldoPendienteUSD ?? 0;

        private void Moneda_Changed(object sender, SelectionChangedEventArgs e) => ActualizarMonedaSeleccionada();

        private void ActualizarMonedaSeleccionada()
        {
            string moneda = MonedaSeleccionada;
            decimal saldo = SaldoSeleccionado;
            ResumenTextBlock.Text = $"{_tarjeta.DescripcionTarjeta} · {_periodo.PeriodoTexto} · {moneda}";
            SaldoDisponibleTextBlock.Text = $"{moneda} {saldo:N2}";
            ImporteTextBox.Text = saldo.ToString("N2");
            CuentaComboBox.ItemsSource = null;
            CargarCuentas();
        }

        private void Banco_Changed(object sender, SelectionChangedEventArgs e)
        {
            CargarCuentas();
        }

        private void CargarCuentas()
        {
            try
            {
                int? idBanco = BancoComboBox.SelectedValue as int?;
                CuentaComboBox.ItemsSource = _negocio.ListarCuentasBancarias(idBanco)
                    .Where(x => string.IsNullOrWhiteSpace(x.Moneda) || x.Moneda == MonedaSeleccionada)
                    .ToList();
            }
            catch (Exception ex)
            {
                NotificationService.Error(ex.Message);
            }
        }

        private void Registrar_Click(object sender, RoutedEventArgs e)
        {
            string moneda = MonedaSeleccionada;
            decimal saldo = SaldoSeleccionado;
            if (FechaDatePicker.SelectedDate == null || string.IsNullOrWhiteSpace(moneda) ||
                !decimal.TryParse(ImporteTextBox.Text, out decimal importe) || importe <= 0)
            {
                NotificationService.Warning("Ingrese moneda, fecha e importe válidos.");
                return;
            }

            if (saldo <= 0 || importe > saldo)
            {
                NotificationService.Warning("El importe no puede superar el saldo pendiente de la moneda seleccionada.");
                return;
            }

            TarjetaCreditoPago pago = new()
            {
                IdTarjetaPeriodo = _periodo.IdTarjetaPeriodo,
                FechaPago = FechaDatePicker.SelectedDate.Value,
                Moneda = moneda,
                Importe = importe,
                IdBanco = BancoComboBox.SelectedValue as int?,
                IdCuentaBancaria = CuentaComboBox.SelectedValue as int?,
                NumeroOperacion = OperacionTextBox.Text,
                Observacion = ObservacionTextBox.Text
            };

            TarjetaCreditoPagoResultado resultado = _negocio.RegistrarPago(
                pago, SessionManager.UsuarioActual?.NombreUsuario ?? "Sistema");
            if (resultado.Resultado)
            {
                NotificationService.Success(resultado.Mensaje);
                DialogResult = true;
            }
            else
                NotificationService.Warning(resultado.Mensaje);
        }
    }
}
