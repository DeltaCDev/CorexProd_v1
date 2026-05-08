using CorexProd.WPF.ViewModels;
using System.Windows.Controls;

namespace CorexProd.WPF.Views
{
    public partial class InsumosView : UserControl
    {
        public InsumosView()
        {
            InitializeComponent();
            DataContext = new InsumosViewModel();
        }
    }
}