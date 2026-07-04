using System.Windows;

namespace CorexProd.WPF.Modules.Produccion.Views
{
    public partial class AnularOrdenTrabajoWindow : Window
    {
        public string MotivoAnulacion { get; private set; } = string.Empty;

        public AnularOrdenTrabajoWindow(string numeroOt, string advertencia = "")
        {
            InitializeComponent();
            NumeroText.Text = numeroOt;

            if (!string.IsNullOrWhiteSpace(advertencia))
            {
                AdvertenciaText.Text = advertencia;
                AdvertenciaPanel.Visibility = Visibility.Visible;
            }
        }

        private void Confirmar_Click(object sender, RoutedEventArgs e)
        {
            MotivoAnulacion = MotivoText.Text.Trim();
            if (string.IsNullOrWhiteSpace(MotivoAnulacion))
            {
                ErrorText.Text = "Debe ingresar el motivo.";
                return;
            }

            DialogResult = true;
        }
    }
}
