using CorexProd.WPF.ViewModels;
using System.Windows;

namespace CorexProd.WPF.Views
{
    public partial class LoginView : Window
    {
        public LoginView()
        {
            InitializeComponent();
            DataContext = new LoginViewModel();
        }
    }
}