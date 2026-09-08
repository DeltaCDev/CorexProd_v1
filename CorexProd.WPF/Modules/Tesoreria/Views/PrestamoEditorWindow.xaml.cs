using CorexProd.Entidad.Entidades;
using CorexProd.WPF.Modules.Tesoreria.ViewModels;
using System.Windows;

namespace CorexProd.WPF.Modules.Tesoreria.Views
{
    public partial class PrestamoEditorWindow : Window
    {
        public PrestamoEditorWindow(ObligacionFinanciera? prestamo = null)
        {
            InitializeComponent();
            PrestamoEditorViewModel viewModel = new(prestamo);
            viewModel.CerrarVentana = resultado =>
            {
                DialogResult = resultado;
                Close();
            };
            DataContext = viewModel;
        }
    }
}
