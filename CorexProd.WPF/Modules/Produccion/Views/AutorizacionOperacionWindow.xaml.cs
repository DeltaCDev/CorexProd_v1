using System.Windows;
using System.Linq;
using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;

namespace CorexProd.WPF.Modules.Produccion.Views
{
 public partial class AutorizacionOperacionWindow:Window
 {
  private readonly UsuarioNegocio _usuarioNegocio = new();
  public string Usuario=>UsuarioComboBox.Text.Trim(); public string Clave=>ClavePasswordBox.Password; public string Observacion=>ObservacionTextBox.Text.Trim();
  public AutorizacionOperacionWindow(string titulo="Autorizar operación", bool soloAprobadores=false)
  {
   InitializeComponent();
   Title=titulo;
   try
   {
    UsuarioComboBox.ItemsSource = _usuarioNegocio.Listar()
     .Where(x => x.Estado && (!soloAprobadores || x.AprobacionOs))
     .OrderBy(x => x.NombreUsuario)
     .ToList();
   }
   catch (Exception ex)
   {
    MessageBox.Show($"No se pudo cargar la lista de usuarios:\n{ex.Message}", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
   }
  }
  private void Autorizar_Click(object sender,RoutedEventArgs e){if(string.IsNullOrWhiteSpace(Usuario)||string.IsNullOrWhiteSpace(Clave)){MessageBox.Show("Ingrese usuario y contraseña.",Title,MessageBoxButton.OK,MessageBoxImage.Warning);return;}DialogResult=true;}
 }
}
