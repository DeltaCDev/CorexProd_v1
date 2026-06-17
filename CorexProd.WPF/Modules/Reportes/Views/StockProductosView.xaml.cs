using CorexProd.WPF.Modules.Reportes.ViewModels;
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
    }
}
