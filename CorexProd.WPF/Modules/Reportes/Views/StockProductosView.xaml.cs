using CorexProd.WPF.Modules.Reportes.ViewModels;
using CorexProd.WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace CorexProd.WPF.Modules.Reportes.Views
{
    public partial class StockProductosView : UserControl
    {
        public StockProductosView()
        {
            InitializeComponent();
            DataContext = new StockProductosViewModel();
        }

        private void VerReservas_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not StockProductosViewModel.StockProductoDisponibilidadItem item
                || item.StockReservado <= 0)
            {
                return;
            }

            if (Application.Current.MainWindow?.DataContext is MainViewModel mainViewModel)
            {
                mainViewModel.Titulo = "Stock Reservas en Proceso";
                mainViewModel.VistaActual = new StockProcesoReservasView(item.Codigo);
            }
        }
    }
}
