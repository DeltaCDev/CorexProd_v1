using CorexProd.Entidad.Entidades;
using System.Windows;

namespace CorexProd.WPF.Modules.Almacen.Views
{
    public partial class IngresoManualStockInsumoDetalleWindow : Window
    {
        public IngresoManualStockInsumoDetalleWindow(IngresoManualStockInsumo ingreso)
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


