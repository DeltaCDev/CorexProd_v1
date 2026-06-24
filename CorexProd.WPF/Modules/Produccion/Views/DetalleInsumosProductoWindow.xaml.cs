using CorexProd.Entidad.Entidades;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CorexProd.WPF.Modules.Produccion.Views
{
    public partial class DetalleInsumosProductoWindow : Window
    {
        public DetalleInsumosProductoWindow(
            OrdenTrabajoValidacionProducto producto,
            string ordenCompra,
            IReadOnlyCollection<OrdenTrabajoInsumoDetalle> insumos,
            bool imprimirAlAbrir = false)
        {
            InitializeComponent();
            ProductoText.Text = $"Producto: {producto.Producto}";
            OrdenText.Text = $"Orden de compra del cliente: {ordenCompra}";
            ProduccionText.Text = $"Producción solicitada: {producto.CantidadRequerida:N3} unidades";
            InsumosGrid.ItemsSource = insumos;

            int faltantes = insumos.Count(x => x.CantidadFaltante > 0);
            ResultadoText.Text = $"Estado de suministros: {producto.EstadoInsumos}";
            FaltantesText.Text = !producto.TieneFichaTecnica || insumos.Count == 0
                ? "El producto no tiene una ficha técnica activa con suministros configurados."
                : $"Suministros con faltante: {faltantes}";

            if (producto.EstadoInsumos == "Faltantes")
            {
                ResultadoText.Foreground = Brushes.Crimson;
                FaltantesText.Foreground = Brushes.Crimson;
            }
            else if (producto.EstadoInsumos == "Sin ficha tecnica")
            {
                ResultadoText.Foreground = Brushes.DarkOrange;
                FaltantesText.Foreground = Brushes.DarkOrange;
            }
            else
            {
                ResultadoText.Foreground = Brushes.ForestGreen;
                FaltantesText.Foreground = Brushes.ForestGreen;
            }

            if (imprimirAlAbrir)
            {
                Loaded += (_, _) =>
                {
                    Imprimir();
                    Close();
                };
            }
        }

        private void Imprimir_Click(object sender, RoutedEventArgs e)
        {
            Imprimir();
        }

        private void Imprimir()
        {
            PrintDialog dialogo = new();
            if (dialogo.ShowDialog() == true)
                dialogo.PrintVisual(Contenido, $"Detalle de suministros - {ProductoText.Text}");
        }
    }
}
