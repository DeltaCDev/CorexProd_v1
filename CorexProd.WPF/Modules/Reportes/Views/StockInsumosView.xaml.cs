using CorexProd.WPF.Modules.Reportes.ViewModels;
using System.Windows.Controls;

namespace CorexProd.WPF.Modules.Reportes.Views
{
    public partial class StockInsumosView : UserControl
    {
        public StockInsumosView()
        {
            InitializeComponent();
            DataContext = new StockInsumosViewModel();
        }
    }
}
