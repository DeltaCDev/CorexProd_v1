using CorexProd.WPF.Modules.Seguridad.ViewModels;
using System.Windows.Controls;

namespace CorexProd.WPF.Modules.Seguridad.Views
{
    public partial class ClientesView : UserControl
    {
        public ClientesView()
        {
            InitializeComponent();
            DataContext = new ClientesViewModel();
        }
    }
}
