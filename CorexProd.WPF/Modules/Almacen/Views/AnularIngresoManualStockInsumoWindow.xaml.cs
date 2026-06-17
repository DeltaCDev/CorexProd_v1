using System.Windows;

namespace CorexProd.WPF.Modules.Almacen.Views
{
    public partial class AnularIngresoManualStockInsumoWindow : Window
    {
        public AnularIngresoManualStockInsumoWindow(string documento)
        {
            InitializeComponent();
            DocumentoTextBlock.Text = $"Documento: {documento}";
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



