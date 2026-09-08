using System.Windows;

namespace CorexProd.WPF.Modules.Tesoreria.Views
{
    public partial class MotivoAnulacionWindow : Window
    {
        public MotivoAnulacionWindow(string titulo, string descripcion)
        {
            InitializeComponent();
            Title = titulo;
            TituloTextBlock.Text = titulo;
            DescripcionTextBlock.Text = descripcion;
        }

        public string Motivo { get; private set; } = string.Empty;
        private void Confirmar_Click(object sender, RoutedEventArgs e)
        {
            Motivo = MotivoTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(Motivo))
            {
                ErrorTextBlock.Text = "Debe ingresar el motivo.";
                ErrorTextBlock.Visibility = Visibility.Visible;
                return;
            }

            DialogResult = true;
        }
    }
}
