using CorexProd.WPF.Modules.Seguridad.ViewModels;
using System.Windows.Controls;

namespace CorexProd.WPF.Modules.Seguridad.Views
{
    public partial class EmpleadosView : UserControl
    {
        public EmpleadosView()
        {
            InitializeComponent();
            DataContext = new EmpleadosViewModel();
        }
    }
}