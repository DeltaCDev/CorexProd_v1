using CorexProd.WPF.Helpers;
using CorexProd.WPF.ViewModels;
using System.ComponentModel;
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

        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (DataContext is MainViewModel { OmitirConfirmacionCierre: true })
            {
                return;
            }

            bool confirmar = ConfirmDialogService.Confirmar(
                "¿Desea cerrar el sistema?",
                "Cerrar sistema");

            if (!confirmar)
            {
                e.Cancel = true;
            }
        }
    }
}
