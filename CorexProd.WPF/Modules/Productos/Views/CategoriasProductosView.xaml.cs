using CorexProd.WPF.Modules.Productos.ViewModels;
using System.Windows.Controls;

namespace CorexProd.WPF.Modules.Productos.Views
{
    public partial class CategoriasProductosView : UserControl
    {
        public CategoriasProductosView()
        {
            InitializeComponent();
            DataContext = new CategoriasProductosViewModel();
        }
    }
}