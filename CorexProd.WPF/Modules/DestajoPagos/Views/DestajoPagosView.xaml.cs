using CorexProd.WPF.Modules.DestajoPagos.ViewModels;
using System.Windows.Controls;

namespace CorexProd.WPF.Modules.DestajoPagos.Views
{
    public partial class DestajoPagosView : UserControl
    {
        public DestajoPagosView()
            : this(0)
        {
        }

        public DestajoPagosView(int tabIndex)
        {
            InitializeComponent();
            DestajoPagosViewModel viewModel = new();
            DataContext = viewModel;
            ModuleContent.Content = CrearVista(tabIndex);
        }

        private static UserControl CrearVista(int tabIndex)
        {
            return tabIndex switch
            {
                1 => new PanelDestajoView(),
                2 => new PrestamosCuotasView(),
                3 => new LotesPagoView(),
                4 => new ReportesPagosView(),
                5 => new ConfiguracionView(),
                _ => new PeriodosPagoView()
            };
        }
    }
}
