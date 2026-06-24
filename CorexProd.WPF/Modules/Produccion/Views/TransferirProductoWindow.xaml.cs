using CorexProd.Entidad.Entidades;
using System;
using System.Globalization;
using System.Windows;

namespace CorexProd.WPF.Modules.Produccion.Views
{
    public partial class TransferirProductoWindow : Window
    {
        private readonly decimal _disponible;
        private readonly bool _permiteAjusteInicial;
        private readonly string _tituloOperacion;
        private readonly string _nombreArea;

        public decimal Cantidad { get; private set; }
        public bool RegistrarMerma { get; private set; }
        public decimal CantidadMerma { get; private set; }
        public string MotivoMerma => "MERMA EN OPERACION";
        public string ObservacionMerma => ObservacionMermaTextBox.Text.Trim();
        public string Clave => ClavePasswordBox.Password;

        public TransferirProductoWindow(
            OrdenTrabajoDetalleArea origen,
            string destino,
            bool esTerminacion = false,
            bool permiteAjusteInicial = false)
        {
            InitializeComponent();
            _disponible = origen.CantidadPendiente;
            _permiteAjusteInicial = permiteAjusteInicial;
            _nombreArea = origen.NombreArea;
            Title = esTerminacion ? "Ingresar producto terminado" : "Transferir producto";
            _tituloOperacion = esTerminacion ? "Ingresar a productos terminados" : $"Transferir a {destino}";
            TituloText.Text = _tituloOperacion;
            CantidadLabel.Text = esTerminacion ? "Cantidad terminada:" : "Cantidad a transferir:";
            ProductoText.Text = $"{origen.CodigoProducto} - {origen.NombreProducto}";
            CantidadTextBox.Text = _disponible.ToString("0.##", CultureInfo.CurrentCulture);
            RegistrarMermaCheckBox.Visibility = origen.ManejaMerma ? Visibility.Visible : Visibility.Collapsed;
            CantidadTextBox.TextChanged += (_, _) => ActualizarPendienteResultante();
            CantidadMermaTextBox.TextChanged += CantidadMerma_Changed;
            ActualizarPendienteResultante();
            CantidadTextBox.SelectAll();
            CantidadTextBox.Focus();
        }

        private void RegistrarMerma_Changed(object sender, RoutedEventArgs e)
        {
            bool activa = RegistrarMermaCheckBox.IsChecked == true;
            MermaPanel.Visibility = activa ? Visibility.Visible : Visibility.Collapsed;
            if (!activa)
                CantidadTextBox.Text = _disponible.ToString("0.##", CultureInfo.CurrentCulture);
            ActualizarPendienteResultante();
            if (activa) CantidadMermaTextBox.Focus();
        }

        private void CantidadMerma_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            decimal.TryParse(CantidadMermaTextBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal merma);
            decimal disponibleParaTransferir = Math.Max(0, _disponible - merma);
            CantidadTextBox.Text = disponibleParaTransferir.ToString("0.##", CultureInfo.CurrentCulture);
            ActualizarPendienteResultante();
        }

        private void ActualizarPendienteResultante()
        {
            decimal.TryParse(CantidadTextBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal transferencia);
            decimal merma = 0;
            if (RegistrarMermaCheckBox.IsChecked == true)
                decimal.TryParse(CantidadMermaTextBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out merma);

            decimal totalOperacion = transferencia + merma;
            decimal resultante = Math.Max(0, _disponible - totalOperacion);
            StockText.Text = _permiteAjusteInicial && totalOperacion > _disponible
                ? $"Pendiente en {_nombreArea}: {_disponible:N2}\nSe ajustara el inicio de produccion a {totalOperacion:N2}."
                : $"Pendiente en {_nombreArea}: {_disponible:N2}\nPendiente despues de la operacion: {resultante:N2}";
        }

        private void Confirmar_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(CantidadTextBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal cantidad) || cantidad <= 0)
            {
                MessageBox.Show(this, "Ingrese una cantidad valida mayor que cero.", "Cantidad invalida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!_permiteAjusteInicial && cantidad > _disponible)
            {
                MessageBox.Show(this, $"La cantidad no puede superar el pendiente disponible ({_disponible:N2}).", "Cantidad invalida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Cantidad = cantidad;

            if (RegistrarMermaCheckBox.IsChecked == true)
            {
                if (!decimal.TryParse(CantidadMermaTextBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal merma) || merma <= 0)
                {
                    MessageBox.Show(this, "Ingrese una cantidad de merma mayor que cero.", "Cantidad invalida", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!_permiteAjusteInicial && merma + cantidad > _disponible)
                {
                    MessageBox.Show(this, $"La transferencia ({cantidad:N2}) mas la merma ({merma:N2}) no puede superar el pendiente ({_disponible:N2}).", "Cantidad invalida", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (string.IsNullOrWhiteSpace(ObservacionMerma))
                {
                    MessageBox.Show(this, "Ingrese la observacion de la merma.", "Observacion requerida", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                CantidadMerma = merma;
                RegistrarMerma = true;
            }
            if (string.IsNullOrWhiteSpace(Clave))
            {
                MessageBox.Show(this, "Ingrese la clave del usuario en sesion.", "Clave requerida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
        }
    }
}
