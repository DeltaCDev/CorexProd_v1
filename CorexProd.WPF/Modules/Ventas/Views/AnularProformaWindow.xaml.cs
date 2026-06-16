using System.Windows;

namespace CorexProd.WPF.Modules.Ventas.Views
{
    public partial class AnularProformaWindow : Window
    {
        public string MotivoAnulacion { get; private set; } = string.Empty;

        public AnularProformaWindow(string serieNumero)
        {
            InitializeComponent();
            SerieTextBlock.Text = $"Proforma {serieNumero}";
            MotivoTextBox.Focus();
        }

        private void Confirmar_Click(object sender, RoutedEventArgs e)
        {
            string motivo = MotivoTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(motivo))
            {
                ErrorTextBlock.Text = "Debe ingresar el motivo de anulacion.";
                ErrorTextBlock.Visibility = Visibility.Visible;
                MotivoTextBox.Focus();
                return;
            }

            MotivoAnulacion = motivo;
            DialogResult = true;
        }
    }
}
