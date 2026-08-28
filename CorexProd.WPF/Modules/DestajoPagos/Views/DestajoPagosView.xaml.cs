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
                1 => new PeriodosPagoView(),
                2 => new PanelDestajoView(),
                3 => new PrestamosCuotasView(),
                4 => new LotesPagoView(),
                5 => new ReportesPagosView(),
                6 => new ConfiguracionView(),
                _ => new DashboardDestajoView()
            };
        }
    }
}
