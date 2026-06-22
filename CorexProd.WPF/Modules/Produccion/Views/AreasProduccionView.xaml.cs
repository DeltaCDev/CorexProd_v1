using CorexProd.WPF.Modules.Produccion.ViewModels;
using System.Windows.Controls;

namespace CorexProd.WPF.Modules.Produccion.Views
{
    public partial class AreasProduccionView : UserControl
    {
        public AreasProduccionView()
        {
            InitializeComponent();
            DataContext = new AreasProduccionViewModel();
        }
    }
}
