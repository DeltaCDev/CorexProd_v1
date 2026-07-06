using CorexProd.WPF.Modules.Reportes.ViewModels;
using System.Windows.Controls;

namespace CorexProd.WPF.Modules.Reportes.Views
{
    public partial class StockProcesoReservasView : UserControl
    {
        public StockProcesoReservasView()
        {
            InitializeComponent();
            DataContext = new StockProcesoReservasViewModel();
        }
    }
}
