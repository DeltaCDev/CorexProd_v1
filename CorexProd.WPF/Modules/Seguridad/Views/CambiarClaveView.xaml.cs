using CorexProd.WPF.Modules.Seguridad.ViewModels;
using System.Windows.Controls;

namespace CorexProd.WPF.Modules.Seguridad.Views
{
    public partial class CambiarClaveView : UserControl
    {
        private readonly CambiarClaveViewModel _viewModel;

        public CambiarClaveView()
        {
            InitializeComponent();

            _viewModel = new CambiarClaveViewModel();
            DataContext = _viewModel;

            _viewModel.SolicitarClaves += ObtenerClaves;
            _viewModel.LimpiarPasswordBoxes += LimpiarPasswordBoxes;
        }

        private (string claveActual, string claveNueva, string confirmarClave) ObtenerClaves()
        {
            return
            (
                TxtClaveActual.Password,
                TxtClaveNueva.Password,
                TxtConfirmarClave.Password
            );
        }

        private void LimpiarPasswordBoxes()
        {
            TxtClaveActual.Clear();
            TxtClaveNueva.Clear();
            TxtConfirmarClave.Clear();
        }
    }
}