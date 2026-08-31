using System.Windows;

namespace CorexProd.WPF.Modules.Tesoreria.Views
{
    public partial class AnularCuentaPorPagarWindow : Window
    {
        public AnularCuentaPorPagarWindow(string cuenta)
        {
            InitializeComponent();
            CuentaTextBlock.Text = $"Cuenta: {cuenta}";
        }

        public string MotivoAnulacion { get; private set; } = string.Empty;

        private void Confirmar_Click(object sender, RoutedEventArgs e)
        {
            MotivoAnulacion = MotivoTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(MotivoAnulacion))
            {
                ErrorTextBlock.Text = "Debe ingresar el motivo de anulacion.";
                ErrorTextBlock.Visibility = Visibility.Visible;
                return;
            }

            DialogResult = true;
        }
    }
}
