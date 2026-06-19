using System.Windows;

namespace CorexProd.WPF.Modules.Ventas.Views
{
    public partial class AnularOciWindow : Window
    {
        public string MotivoAnulacion { get; private set; } = string.Empty;

        public AnularOciWindow(string numeroOci)
        {
            InitializeComponent();
            NumeroOciTextBlock.Text = $"OCI {numeroOci}";
            MotivoTextBox.Focus();
        }

        private void Confirmar_Click(object sender, RoutedEventArgs e)
        {
            string motivo = MotivoTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(motivo))
            {
                ErrorTextBlock.Text = "Debe ingresar el motivo de anulación.";
                ErrorTextBlock.Visibility = Visibility.Visible;
                MotivoTextBox.Focus();
                return;
            }

            MotivoAnulacion = motivo;
            DialogResult = true;
        }
    }
}
