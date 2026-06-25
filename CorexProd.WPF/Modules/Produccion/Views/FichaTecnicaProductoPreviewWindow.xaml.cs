using System;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace CorexProd.WPF.Modules.Produccion.Views
{
    public partial class FichaTecnicaProductoPreviewWindow : Window
    {
        public FichaTecnicaProductoPreviewWindow(
            string codigoProducto,
            string nombreProducto,
            string observacion,
            string? rutaPdf)
        {
            InitializeComponent();

            CodigoText.Text = codigoProducto;
            ProductoText.Text = nombreProducto;
            ObservacionText.Text = string.IsNullOrWhiteSpace(observacion)
                ? "El producto no tiene observaciones registradas."
                : observacion.Trim();

            Loaded += async (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(rutaPdf) || !File.Exists(rutaPdf))
                {
                    MostrarSinFicha();
                    return;
                }

                try
                {
                    await PdfWebView.EnsureCoreWebView2Async();
                    PdfWebView.Source = new Uri(rutaPdf);
                }
                catch
                {
                    MostrarSinFicha();
                }
            };
        }

        private void MostrarSinFicha()
        {
            PdfWebView.Visibility = Visibility.Collapsed;
            SinFichaText.Visibility = Visibility.Visible;
            EstadoText.Text = "Ficha técnica no existe";
            EstadoText.Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28));
            EstadoBorder.Background = new SolidColorBrush(Color.FromRgb(254, 226, 226));
        }
    }
}
