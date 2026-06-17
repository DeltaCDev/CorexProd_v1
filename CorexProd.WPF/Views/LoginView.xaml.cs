using CorexProd.WPF.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace CorexProd.WPF.Views
{
    public partial class LoginView : Window
    {
        public LoginView()
        {
            InitializeComponent();
            DataContext = new LoginViewModel();
            Loaded += (_, _) =>
            {
                TxtUsuario.Focus();
                Keyboard.Focus(TxtUsuario);
            };
        }
    }
}
