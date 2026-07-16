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
            Title = CorexProd.WPF.Helpers.AppVersionHelper.Title;
            DataContext = new LoginViewModel();
            Loaded += async (_, _) =>
            {
                TxtUsuario.Focus();
                Keyboard.Focus(TxtUsuario);
                await CorexProd.WPF.Helpers.AppUpdateService.CheckForUpdatesAsync(this);
            };
        }
    }
}
