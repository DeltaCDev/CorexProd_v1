using CorexProd.Entidad.Entidades;
using CorexProd.WPF.Modules.Almacen.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Almacen.Views
{
    public partial class IngresoManualStockInsumoEditorWindow : Window
    {
        public IngresoManualStockInsumoEditorWindow()
        {
            InitializeComponent();
        }

        private void InsumoBusquedaListBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            InsumoStockBusqueda? insumo = null;

            if (e.OriginalSource is FrameworkElement elemento)
            {
                insumo = elemento.DataContext as InsumoStockBusqueda;
            }

            if (sender is not ListBox listBox ||
                listBox.DataContext is not IngresoManualStockInsumoDetalleViewModel detalle)
            {
                return;
            }

            insumo ??= listBox.SelectedItem as InsumoStockBusqueda;

            if (insumo == null)
            {
                return;
            }

            detalle.AsignarInsumo(insumo);
        }
    }
}


