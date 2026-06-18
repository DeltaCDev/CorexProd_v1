using CorexProd.WPF.Modules.Ventas.ViewModels;
using System.Windows.Controls;

namespace CorexProd.WPF.Modules.Ventas.Views
{
    public partial class OrdenesCompraInternaView : UserControl
    {
        public OrdenesCompraInternaView()
        {
            InitializeComponent();
            DataContext = new OrdenesCompraInternaViewModel();
        }
    }
}
