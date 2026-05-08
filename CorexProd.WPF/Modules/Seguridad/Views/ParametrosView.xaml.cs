using CorexProd.WPF.Modules.Seguridad.ViewModels;
using System.Windows.Controls;

namespace CorexProd.WPF.Modules.Seguridad.Views
{
    public partial class ParametrosView : UserControl
    {
        public ParametrosView()
        {
            InitializeComponent();
            DataContext = new ParametrosViewModel();
        }
    }
}