using CorexProd.WPF.Modules.Tesoreria.ViewModels;
using System.Windows.Controls;

namespace CorexProd.WPF.Modules.Tesoreria.Views
{
    public partial class CuentasPorPagarView : UserControl
    {
        public CuentasPorPagarView()
        {
            InitializeComponent();
            DataContext = new CuentasPorPagarViewModel();
        }
    }
}
