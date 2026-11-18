using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace CorexProd.WPF.Modules.Seguridad.Views
{
    public partial class FirmaPreviewWindow : Window
    {
        public FirmaPreviewWindow(byte[] firma, string titulo)
        {
            InitializeComponent();
            Title = titulo;
            using MemoryStream stream = new(firma);
            BitmapImage imagen = new();
            imagen.BeginInit();
            imagen.CacheOption = BitmapCacheOption.OnLoad;
            imagen.StreamSource = stream;
            imagen.EndInit();
            imagen.Freeze();
            FirmaImage.Source = imagen;
        }
    }
}
