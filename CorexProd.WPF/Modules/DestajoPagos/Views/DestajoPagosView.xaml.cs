using CorexProd.WPF.Modules.DestajoPagos.ViewModels;
using System.Windows.Controls;

namespace CorexProd.WPF.Modules.DestajoPagos.Views
{
    public partial class DestajoPagosView : UserControl
    {
        public DestajoPagosView()
            : this(0)
        {
        }

        public DestajoPagosView(int tabIndex)
        {
            InitializeComponent();
            DataContext = new DestajoPagosViewModel();

            if (tabIndex >= 0 && tabIndex < MainTabs.Items.Count)
            {
                MainTabs.SelectedIndex = tabIndex;
            }
        }
    }
}
