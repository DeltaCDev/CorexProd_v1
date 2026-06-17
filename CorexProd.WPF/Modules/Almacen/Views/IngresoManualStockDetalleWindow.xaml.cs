using CorexProd.Entidad.Entidades;
using System.Windows;

namespace CorexProd.WPF.Modules.Almacen.Views
{
    public partial class IngresoManualStockDetalleWindow : Window
    {
        public IngresoManualStockDetalleWindow(IngresoManualStock ingreso)
        {
            InitializeComponent();
            DataContext = ingreso;
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
