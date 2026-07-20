using CorexProd.WPF.Modules.OrdenesServicio.ViewModels;
using System.ComponentModel;
using System.Windows.Controls;

namespace CorexProd.WPF.Modules.OrdenesServicio.Views
{
    public partial class OrdenesServicioView : UserControl
    {
        public OrdenesServicioView()
            : this(0)
        {
        }

        public OrdenesServicioView(int tabIndex)
        {
            InitializeComponent();
            if (!DesignerProperties.GetIsInDesignMode(this))
                DataContext = new OrdenesServicioViewModel();
            TabsOrdenesServicio.SelectedIndex = tabIndex;
        }
    }
}
