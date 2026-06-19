using System.ComponentModel;
using System.Windows.Controls;
using CorexProd.WPF.Modules.Seguridad.ViewModels;

namespace CorexProd.WPF.Modules.Seguridad.Views
{
    public partial class ProveedoresView : UserControl
    {
        public ProveedoresView()
        {
            InitializeComponent();

            /*
             * Evita que Visual Studio ejecute el constructor completo
             * del ViewModel mientras está mostrando el diseñador XAML.
             */
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            DataContext = new ProveedoresViewModel();
        }
    }
}
