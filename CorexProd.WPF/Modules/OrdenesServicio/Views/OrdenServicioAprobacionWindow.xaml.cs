using CorexProd.WPF.Helpers;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.OrdenesServicio.Views
{
    public partial class OrdenServicioAprobacionWindow : Window
    {
        public string UsuarioAprobador { get; private set; } = string.Empty;
        public string ClaveAprobacion { get; private set; } = string.Empty;
        public bool Confirmado { get; private set; }

        public OrdenServicioAprobacionWindow(string usuarioSugerido = "")
        {
            InitializeComponent();
            UsuarioTextBox.Text = usuarioSugerido;
            UsuarioTextBox.Focus();
        }

        private void Aprobar_Click(object sender, RoutedEventArgs e)
        {
            UsuarioAprobador = UsuarioTextBox.Text.Trim();
            ClaveAprobacion = ClavePasswordBox.Password.Trim();

            if (string.IsNullOrWhiteSpace(UsuarioAprobador))
            {
                NotificationService.Warning("Ingrese el usuario aprobador.");
                return;
            }

            if (ClaveAprobacion.Length != 4 || !ClaveAprobacion.All(char.IsDigit))
            {
                NotificationService.Warning("La clave de aprobacion debe tener 4 digitos numericos.");
                return;
            }

            Confirmado = true;
            DialogResult = true;
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ClavePasswordBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }
    }
}
