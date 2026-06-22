using System.Windows;

namespace CorexProd.WPF.Modules.Produccion.Views
{
 public partial class AutorizacionOperacionWindow:Window
 {
  public string Usuario=>UsuarioTextBox.Text.Trim(); public string Clave=>ClavePasswordBox.Password; public string Observacion=>ObservacionTextBox.Text.Trim();
  public AutorizacionOperacionWindow(string titulo="Autorizar operación"){InitializeComponent();Title=titulo;}
  private void Autorizar_Click(object sender,RoutedEventArgs e){if(string.IsNullOrWhiteSpace(Usuario)||string.IsNullOrWhiteSpace(Clave)){MessageBox.Show("Ingrese usuario y contraseña.",Title,MessageBoxButton.OK,MessageBoxImage.Warning);return;}DialogResult=true;}
 }
}
