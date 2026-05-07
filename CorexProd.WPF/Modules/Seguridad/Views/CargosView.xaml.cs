using CorexProd.WPF.Modules.Seguridad.ViewModels;
using System.Windows.Controls;

namespace CorexProd.WPF.Modules.Seguridad.Views
{
    public partial class CargosView : UserControl
    {
        public CargosView()
        {
            InitializeComponent();
            DataContext = new CargosViewModel();
        }
    }
}