using CorexProd.WPF.Modules.Seguridad.ViewModels;
using System.Windows.Controls;

namespace CorexProd.WPF.Modules.Seguridad.Views
{
    public partial class FormasPagoOsView : UserControl
    {
        public FormasPagoOsView()
        {
            InitializeComponent();
            DataContext = new FormasPagoOsViewModel();
        }
    }
}
