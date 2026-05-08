using CorexProd.WPF.Modules.Produccion.ViewModels;
using System.ComponentModel;
using System.Windows.Controls;

namespace CorexProd.WPF.Modules.Produccion.Views
{
    public partial class FichaTecnicaView : UserControl
    {
        public FichaTecnicaView()
        {
            InitializeComponent();

            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                DataContext = new FichaTecnicaViewModel();
            }
        }
    }
}