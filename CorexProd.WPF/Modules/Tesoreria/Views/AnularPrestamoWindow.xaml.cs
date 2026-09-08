using System.Windows;

namespace CorexProd.WPF.Modules.Tesoreria.Views
{
    public partial class AnularPrestamoWindow : Window
    {
        public AnularPrestamoWindow(string prestamo)
        {
            InitializeComponent();
            PrestamoTextBlock.Text = $"Prestamo: {prestamo}";
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
