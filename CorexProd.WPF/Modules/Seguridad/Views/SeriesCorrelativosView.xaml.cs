using CorexProd.WPF.Modules.Seguridad.ViewModels;
using System.Windows.Controls;

namespace CorexProd.WPF.Modules.Seguridad.Views
{
    public partial class SeriesCorrelativosView : UserControl
    {
        public SeriesCorrelativosView()
        {
            InitializeComponent();
            DataContext = new SeriesCorrelativosViewModel();
        }
    }
}
