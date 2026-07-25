using CorexProd.WPF.Modules.OrdenesServicio.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace CorexProd.WPF.Modules.OrdenesServicio.Views
{
    public partial class OrdenServicioEditorWindow : Window
    {
        public OrdenServicioEditorWindow(OrdenesServicioViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not OrdenesServicioViewModel viewModel)
                return;

            viewModel.GuardarCommand.Execute(null);
            if (!viewModel.MostrarFormulario)
            {
                DialogResult = true;
                Close();
            }
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is OrdenesServicioViewModel viewModel)
                viewModel.CancelarCommand.Execute(null);

            DialogResult = false;
            Close();
        }

        private void DetallesGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (DataContext is OrdenesServicioViewModel viewModel)
                    viewModel.RecalcularDetallesFormulario();
                DetallesGrid.Items.Refresh();
            }), DispatcherPriority.Background);
        }

        private void DetallesGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (DataContext is OrdenesServicioViewModel { DetallesFormulario.Count: <= 4 })
            {
                ScrollDetallesArriba();
                e.Handled = true;
            }
        }

        private void DetallesGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (DataContext is OrdenesServicioViewModel { DetallesFormulario.Count: <= 4 } && e.VerticalOffset != 0)
                Dispatcher.BeginInvoke(ScrollDetallesArriba, DispatcherPriority.Background);
        }

        private void ScrollDetallesArriba()
        {
            FindVisualChild<ScrollViewer>(DetallesGrid)?.ScrollToTop();
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T match)
                    return match;

                T? nested = FindVisualChild<T>(child);
                if (nested != null)
                    return nested;
            }

            return null;
        }
    }
}
