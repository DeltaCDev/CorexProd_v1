using CorexProd.WPF.Modules.Tesoreria.ViewModels;
using System.Windows;

namespace CorexProd.WPF.Modules.Tesoreria.Views
{
    public partial class CuentaPorPagarEditorWindow : Window
    {
        public CuentaPorPagarEditorWindow()
        {
            InitializeComponent();
            CuentaPorPagarEditorViewModel viewModel = new();
            viewModel.CerrarVentana = resultado =>
            {
                DialogResult = resultado;
                Close();
            };
            DataContext = viewModel;
        }
    }
}
