using CorexProd.WPF.ViewModels;
using System.Windows.Controls;

namespace CorexProd.WPF.Views
{
    public partial class CategoriasInsumoView : UserControl
    {
        public CategoriasInsumoView()
        {
            InitializeComponent();
            DataContext = new CategoriasInsumoViewModel();
        }
    }
}