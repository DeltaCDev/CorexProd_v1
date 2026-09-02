using CorexProd.Entidad.Entidades;
using System.Windows;

namespace CorexProd.WPF.Modules.Tesoreria.Views
{
    public partial class AnularPagoCuentaPorPagarWindow : Window
    {
        public AnularPagoCuentaPorPagarWindow(string proveedor, CuentaPorPagarPago pago)
        {
            InitializeComponent();
            Proveedor = proveedor;
            Pago = pago;
            DataContext = this;
        }

        public string Proveedor { get; }
        public CuentaPorPagarPago Pago { get; }
        public string MotivoAnulacion { get; private set; } = string.Empty;
        public string BancoCuenta => string.IsNullOrWhiteSpace(Pago.Banco) && string.IsNullOrWhiteSpace(Pago.NumeroCuenta)
            ? "Sin banco"
            : $"{Pago.Banco} {Pago.NumeroCuenta}".Trim();

        private void Confirmar_Click(object sender, RoutedEventArgs e)
        {
            MotivoAnulacion = MotivoTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(MotivoAnulacion))
            {
                ErrorTextBlock.Visibility = Visibility.Visible;
                MotivoTextBox.Focus();
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
