using CorexProd.WPF.Modules.Tesoreria.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Tesoreria.Views
{
    public partial class ProgramacionGeneralPagosView : UserControl
    {
        public ProgramacionGeneralPagosView()
        {
            InitializeComponent();
            DataContext = new ProgramacionGeneralPagosViewModel();
        }

        private void PagoDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ProgramacionGeneralPagosViewModel viewModel
                && sender is DataGrid dataGrid
                && dataGrid.SelectedItem is ProgramacionPagoGeneralItemViewModel pago
                && viewModel.VerDetalleCommand.CanExecute(pago))
            {
                viewModel.VerDetalleCommand.Execute(pago);
            }
        }
    }
}
