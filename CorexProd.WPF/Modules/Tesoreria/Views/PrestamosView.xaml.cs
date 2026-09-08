using CorexProd.WPF.Modules.Tesoreria.ViewModels;
using System.Windows.Controls;

namespace CorexProd.WPF.Modules.Tesoreria.Views
{
    public partial class PrestamosView : UserControl
    {
        public PrestamosView()
        {
            InitializeComponent();
            DataContext = new PrestamosViewModel();
        }
    }
}
