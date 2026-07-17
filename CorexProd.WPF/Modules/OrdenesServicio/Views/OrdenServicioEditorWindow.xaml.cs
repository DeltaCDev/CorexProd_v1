using CorexProd.WPF.Modules.OrdenesServicio.ViewModels;
using System.Windows;
using System.Windows.Controls;
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
    }
}
