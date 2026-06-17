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

        private void ClienteBusquedaTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            if (DataContext is ProformaEditorViewModel viewModel)
            {
                viewModel.SeleccionarClienteBusqueda();
                e.Handled = true;
            }
        }

        private void ClienteBusquedaListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            if (DataContext is ProformaEditorViewModel viewModel)
            {
                viewModel.SeleccionarClienteBusqueda();
                e.Handled = true;
            }
        }

        private void ClienteBusquedaListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ProformaEditorViewModel viewModel
                && BuscarAncestro<ListBoxItem>((DependencyObject)e.OriginalSource)?.DataContext is CorexProd.Entidad.Entidades.Cliente cliente)
            {
                viewModel.SeleccionarClienteBusqueda(cliente);
            }
        }

        private void ClienteBusquedaListBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ListBoxItem? itemSeleccionado = BuscarAncestro<ListBoxItem>((DependencyObject)e.OriginalSource);

            if (sender is not ListBox || itemSeleccionado?.DataContext is not CorexProd.Entidad.Entidades.Cliente cliente)
            {
                return;
            }

            if (DataContext is ProformaEditorViewModel viewModel)
            {
                viewModel.SeleccionarClienteBusqueda(cliente);
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
            if (((FrameworkElement)sender).DataContext is ProformaDetalleItemViewModel item
                && BuscarAncestro<ListBoxItem>((DependencyObject)e.OriginalSource)?.DataContext is CorexProd.Entidad.Entidades.Producto producto)
            {
                item.SeleccionarProductoBusqueda(producto);
            }
        }

        private void ProductoBusquedaListBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ListBoxItem? itemSeleccionado = BuscarAncestro<ListBoxItem>((DependencyObject)e.OriginalSource);

            if (sender is not ListBox listBox
                || itemSeleccionado?.DataContext is not CorexProd.Entidad.Entidades.Producto producto)
            {
                return;
            }

            if (listBox.DataContext is ProformaDetalleItemViewModel item)
            {
                item.SeleccionarProductoBusqueda(producto);
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
