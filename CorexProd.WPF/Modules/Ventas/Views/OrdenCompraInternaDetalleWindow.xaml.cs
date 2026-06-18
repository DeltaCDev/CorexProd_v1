using CorexProd.Entidad.Entidades;
using System.Windows;

namespace CorexProd.WPF.Modules.Ventas.Views
{
    public partial class OrdenCompraInternaDetalleWindow : Window
    {
        public OrdenCompraInternaDetalleWindow(OrdenCompraInterna orden)
        {
            InitializeComponent();
            DataContext = orden;
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e) => Close();
    }
}
