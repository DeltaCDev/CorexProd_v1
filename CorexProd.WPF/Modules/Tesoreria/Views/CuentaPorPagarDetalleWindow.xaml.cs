using CorexProd.Entidad.Entidades;
using System.Windows;

namespace CorexProd.WPF.Modules.Tesoreria.Views
{
    public partial class CuentaPorPagarDetalleWindow : Window
    {
        public CuentaPorPagarDetalleWindow(CuentaPorPagar cuenta)
        {
            InitializeComponent();
            DataContext = cuenta;
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
