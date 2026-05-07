using CorexProd.WPF.Modules.Seguridad.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Seguridad.Views
{
    public partial class RolesView : UserControl
    {
        public RolesView()
        {
            InitializeComponent();
            DataContext = new RolesViewModel();
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                scrollViewer.ScrollToVerticalOffset(
                    scrollViewer.VerticalOffset - e.Delta
                );

                e.Handled = true;
            }
        }
    }
}