using System.Collections.Generic;
using System.Windows;

namespace CorexProd.WPF.Modules.Produccion.Views
{
    public partial class DocumentoGeneradoResumenWindow : Window
    {
        public DocumentoGeneradoResumenWindow(
            string titulo,
            string subtitulo,
            string detalle,
            IEnumerable<DocumentoGeneradoProducto> productos)
        {
            InitializeComponent();
            TituloText.Text = titulo;
            SubtituloText.Text = subtitulo;
            DetalleText.Text = detalle;
            ProductosGrid.ItemsSource = productos;
        }

        private void Aceptar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }

    public sealed class DocumentoGeneradoProducto
    {
        public string Codigo { get; set; } = string.Empty;
        public string Producto { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
    }
}
