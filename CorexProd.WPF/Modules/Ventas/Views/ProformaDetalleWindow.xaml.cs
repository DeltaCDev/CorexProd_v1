using CorexProd.Entidad.Entidades;
using System.Windows;

namespace CorexProd.WPF.Modules.Ventas.Views
{
    public partial class ProformaDetalleWindow : Window
    {
        public ProformaDetalleWindow(Proforma proforma)
        {
            InitializeComponent();
            DataContext = proforma;
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
