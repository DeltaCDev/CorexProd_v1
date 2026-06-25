using CorexProd.WPF.Modules.Productos.ViewModels;
using System.Windows.Controls;

namespace CorexProd.WPF.Modules.Productos.Views
{
    public partial class SuperCategoriasProductosView : UserControl
    {
        public SuperCategoriasProductosView()
        {
            InitializeComponent();
            DataContext = new SuperCategoriasProductosViewModel();
        }
    }
}
