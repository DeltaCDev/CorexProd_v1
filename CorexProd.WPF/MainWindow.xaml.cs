using CorexProd.WPF.ViewModels;
using System.Windows;

namespace CorexProd.WPF
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}