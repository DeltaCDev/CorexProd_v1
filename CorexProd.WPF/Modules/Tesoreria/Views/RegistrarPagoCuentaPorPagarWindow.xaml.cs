using CorexProd.WPF.Modules.Tesoreria.ViewModels;
using System.Windows;

namespace CorexProd.WPF.Modules.Tesoreria.Views
{
    public partial class RegistrarPagoCuentaPorPagarWindow : Window
    {
        public RegistrarPagoCuentaPorPagarWindow(CuentaPorPagarProgramacionItem cuota)
        {
            InitializeComponent();
            RegistrarPagoCuentaPorPagarViewModel viewModel = new(cuota);
            viewModel.CerrarVentana = Cerrar;
            DataContext = viewModel;
        }

        private void Cerrar(bool resultado)
        {
            DialogResult = resultado;
            Close();
        }
    }
}
