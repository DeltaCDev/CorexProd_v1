using CorexProd.Entidad.Entidades;
using CorexProd.WPF.Modules.Produccion.ViewModels;
using System.Windows;

namespace CorexProd.WPF.Modules.Produccion.Views
{
    public partial class FichaTecnicaEditorWindow : Window
    {
        public FichaTecnicaEditorWindow(FichaTecnica fichaSeleccionada)
        {
            InitializeComponent();

            var vm = new FichaTecnicaEditorViewModel(fichaSeleccionada);
            vm.CerrarVentana = Close;
            DataContext = vm;
        }

        public FichaTecnicaEditorViewModel ViewModel => (FichaTecnicaEditorViewModel)DataContext;
    }
}
