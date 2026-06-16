using CorexProd.WPF.Modules.Ventas.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CorexProd.WPF.Modules.Ventas.Views
{
    public partial class ProformaEditorWindow : Window
    {
        public ProformaEditorWindow()
        {
            InitializeComponent();
        }

        private void ProductoBusquedaTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            if (((FrameworkElement)sender).DataContext is ProformaDetalleItemViewModel item)
            {
                item.SeleccionarProductoBusqueda();
                e.Handled = true;
            }
        }

        private void ProductoBusquedaListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            if (((FrameworkElement)sender).DataContext is ProformaDetalleItemViewModel item)
            {
                item.SeleccionarProductoBusqueda();
                e.Handled = true;
            }
        }

        private void ProductoBusquedaListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is ProformaDetalleItemViewModel item)
            {
                item.SeleccionarProductoBusqueda();
            }
        }

        private void ProductoBusquedaListBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not ListBox listBox
                || BuscarAncestro<ListBoxItem>((DependencyObject)e.OriginalSource) == null)
            {
                return;
            }

            if (listBox.DataContext is ProformaDetalleItemViewModel item)
            {
                item.SeleccionarProductoBusqueda();
                e.Handled = true;
            }
        }

        private static T? BuscarAncestro<T>(DependencyObject origen)
            where T : DependencyObject
        {
            DependencyObject? actual = origen;

            while (actual != null)
            {
                if (actual is T encontrado)
                {
                    return encontrado;
                }

                actual = VisualTreeHelper.GetParent(actual);
            }

            return null;
        }
    }
}
