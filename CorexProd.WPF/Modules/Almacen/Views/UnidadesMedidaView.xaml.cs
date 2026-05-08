using CorexProd.WPF.Modules.Almacen.ViewModels;
using System.Windows.Controls;

namespace CorexProd.WPF.Modules.Almacen.Views
{
    public partial class UnidadesMedidaView : UserControl
    {
        public UnidadesMedidaView()
        {
            InitializeComponent();
            DataContext = new UnidadesMedidaViewModel();
        }
    }
}