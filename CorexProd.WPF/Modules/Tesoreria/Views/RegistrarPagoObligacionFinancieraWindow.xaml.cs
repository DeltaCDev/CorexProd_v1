using CorexProd.Entidad.Entidades;
using CorexProd.WPF.Modules.Tesoreria.ViewModels;
using System.Windows;

namespace CorexProd.WPF.Modules.Tesoreria.Views
{
    public partial class RegistrarPagoObligacionFinancieraWindow : Window
    {
        public RegistrarPagoObligacionFinancieraWindow(ObligacionFinanciera prestamo, ObligacionFinancieraCuota cuota)
        {
            InitializeComponent();
            RegistrarPagoObligacionFinancieraViewModel viewModel = new(prestamo, cuota);
            viewModel.CerrarVentana = resultado =>
            {
                DialogResult = resultado;
                Close();
            };
            DataContext = viewModel;
        }
    }
}
