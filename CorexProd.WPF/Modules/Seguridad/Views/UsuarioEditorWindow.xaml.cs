using System.Windows;
using Microsoft.Win32;
using System.IO;
using System.Windows.Media.Imaging;
using CorexProd.WPF.Modules.Seguridad.ViewModels;

namespace CorexProd.WPF.Modules.Seguridad.Views
{
    public partial class UsuarioEditorWindow : Window
    {
        public UsuarioEditorWindow()
        {
            InitializeComponent();
        }

        private void CargarFirma_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not UsuariosViewModel vm)
                return;

            OpenFileDialog dialogo = new()
            {
                Title = "Seleccionar firma",
                Filter = "Imágenes (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialogo.ShowDialog() != true)
                return;

            try
            {
                byte[] bytes = File.ReadAllBytes(dialogo.FileName);
                BitmapImage imagen = new();
                using MemoryStream stream = new(bytes);
                imagen.BeginInit();
                imagen.CacheOption = BitmapCacheOption.OnLoad;
                imagen.StreamSource = stream;
                imagen.EndInit();
                imagen.Freeze();

                vm.FirmaImagen = bytes;
                vm.FirmaMimeType = Path.GetExtension(dialogo.FileName).ToLowerInvariant() switch
                {
                    ".png" => "image/png",
                    ".jpg" or ".jpeg" => "image/jpeg",
                    _ => "application/octet-stream"
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"No se pudo cargar la firma: {ex.Message}", "Firma", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
