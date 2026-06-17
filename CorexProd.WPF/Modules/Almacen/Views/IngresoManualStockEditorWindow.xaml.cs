using CorexProd.Entidad.Entidades;
using CorexProd.WPF.Modules.Almacen.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Almacen.Views
{
    public partial class IngresoManualStockEditorWindow : Window
    {
        public IngresoManualStockEditorWindow()
        {
            InitializeComponent();
        }

        private void ProductoBusquedaListBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ProductoStockBusqueda? producto = null;

            if (e.OriginalSource is FrameworkElement elemento)
            {
                producto = elemento.DataContext as ProductoStockBusqueda;
            }

            if (sender is not ListBox listBox ||
                listBox.DataContext is not IngresoManualStockDetalleViewModel detalle)
            {
                return;
            }

            producto ??= listBox.SelectedItem as ProductoStockBusqueda;

            if (producto == null)
            {
                return;
            }

            detalle.AsignarProducto(producto);
        }
    }
}
