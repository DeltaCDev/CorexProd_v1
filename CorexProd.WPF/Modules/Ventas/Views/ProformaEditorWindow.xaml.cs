using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Modules.Ventas.ViewModels;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CorexProd.WPF.Modules.Ventas.Views
{
    public partial class ProformaEditorWindow : Window
    {
        private readonly OrdenCompraEntregaNegocio _entregaNegocio = new();
        private DatePicker? _fechaEmisionPicker;
        private DatePicker? _fechaEntregaPicker;
        private Button? _guardarButton;
        private bool _esOrdenCompra;

        public ProformaEditorWindow()
        {
            InitializeComponent();
            Loaded += ProformaEditorWindow_Loaded;
        }

        private void ProformaEditorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ProformaEditorViewModel viewModel)
                return;

            _esOrdenCompra = viewModel.Titulo.Contains("Orden de Compra", StringComparison.OrdinalIgnoreCase);
            if (!_esOrdenCompra)
                return;

            DatePicker[] fechas = BuscarDescendientes<DatePicker>(this).Take(2).ToArray();
            if (fechas.Length < 2)
                return;

            _fechaEmisionPicker = fechas[0];
            _fechaEntregaPicker = fechas[1];

            TextBlock? etiqueta = BuscarDescendientes<TextBlock>(this)
                .FirstOrDefault(x => string.Equals(x.Text, "Fecha Vencimiento", StringComparison.OrdinalIgnoreCase));
            if (etiqueta != null)
                etiqueta.Text = "Fecha Entrega *";

            _fechaEntregaPicker.ClearValue(DatePicker.SelectedDateProperty);
            DateTime fechaBase = (_fechaEmisionPicker.SelectedDate ?? viewModel.FechaEmision).Date;
            DateTime? fechaRegistrada = viewModel.IdOrdenCompraInterna > 0
                ? _entregaNegocio.ObtenerFechaEntrega(viewModel.IdOrdenCompraInterna)
                : null;
            _fechaEntregaPicker.SelectedDate = fechaRegistrada?.Date ?? fechaBase.AddDays(1);

            _guardarButton = BuscarDescendientes<Button>(this)
                .FirstOrDefault(x => string.Equals(x.Content?.ToString(), "Guardar", StringComparison.OrdinalIgnoreCase));
            if (_guardarButton != null)
            {
                _guardarButton.Command = null;
                _guardarButton.Click -= GuardarOrdenCompra_Click;
                _guardarButton.Click += GuardarOrdenCompra_Click;
            }
        }

        private void GuardarOrdenCompra_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ProformaEditorViewModel viewModel)
                return;

            ConfirmarEdicionDetalle();

            if (!_esOrdenCompra)
            {
                if (viewModel.GuardarCommand.CanExecute(null))
                    viewModel.GuardarCommand.Execute(null);
                return;
            }

            DateTime fechaEmision = (_fechaEmisionPicker?.SelectedDate ?? viewModel.FechaEmision).Date;
            DateTime? fechaEntrega = _fechaEntregaPicker?.SelectedDate;

            if (!fechaEntrega.HasValue)
            {
                MessageBox.Show(
                    "Debe seleccionar la fecha de entrega.",
                    "Fecha no válida",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                _fechaEntregaPicker?.Focus();
                return;
            }

            if (fechaEntrega.Value.Date <= fechaEmision)
            {
                MessageBox.Show(
                    "La fecha de entrega debe ser diferente y posterior a la fecha de emisión.",
                    "Fecha no válida",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                _fechaEntregaPicker?.Focus();
                return;
            }

            if (!viewModel.GuardarCommand.CanExecute(null))
                return;

            viewModel.GuardarCommand.Execute(null);

            if (!viewModel.Guardado)
                return;

            try
            {
                _entregaNegocio.GuardarFechaEntrega(
                    viewModel.IdOrdenCompraInterna,
                    viewModel.SerieNumero,
                    fechaEmision,
                    fechaEntrega.Value.Date);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"La OC fue guardada, pero no se pudo registrar la fecha de entrega: {ex.Message}",
                    "Fecha de entrega",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void ConfirmarEdicionDetalle()
        {
            foreach (DataGrid grid in BuscarDescendientes<DataGrid>(this))
            {
                grid.CommitEdit(DataGridEditingUnit.Cell, true);
                grid.CommitEdit(DataGridEditingUnit.Row, true);
            }
        }

        private void ProductoBusquedaTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            if (((FrameworkElement)sender).DataContext is ProformaDetalleItemViewModel item)
            {
                item.SeleccionarProductoBusqueda();
                e.Handled = true;
            }
        }

        private void ClienteBusquedaTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            if (DataContext is ProformaEditorViewModel viewModel)
            {
                viewModel.SeleccionarClienteBusqueda();
                e.Handled = true;
            }
        }

        private void ClienteBusquedaListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            if (DataContext is ProformaEditorViewModel viewModel)
            {
                viewModel.SeleccionarClienteBusqueda();
                e.Handled = true;
            }
        }

        private void ClienteBusquedaListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ProformaEditorViewModel viewModel
                && BuscarAncestro<ListBoxItem>((DependencyObject)e.OriginalSource)?.DataContext is CorexProd.Entidad.Entidades.Cliente cliente)
            {
                viewModel.SeleccionarClienteBusqueda(cliente);
            }
        }

        private void ClienteBusquedaListBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ListBoxItem? itemSeleccionado = BuscarAncestro<ListBoxItem>((DependencyObject)e.OriginalSource);
            if (sender is not ListBox || itemSeleccionado?.DataContext is not CorexProd.Entidad.Entidades.Cliente cliente)
                return;

            if (DataContext is ProformaEditorViewModel viewModel)
            {
                viewModel.SeleccionarClienteBusqueda(cliente);
                e.Handled = true;
            }
        }

        private void ProductoBusquedaListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            if (((FrameworkElement)sender).DataContext is ProformaDetalleItemViewModel item)
            {
                item.SeleccionarProductoBusqueda();
                e.Handled = true;
            }
        }

        private void ProductoBusquedaListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is ProformaDetalleItemViewModel item
                && BuscarAncestro<ListBoxItem>((DependencyObject)e.OriginalSource)?.DataContext is CorexProd.Entidad.Entidades.Producto producto)
            {
                item.SeleccionarProductoBusqueda(producto);
            }
        }

        private void ProductoBusquedaListBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ListBoxItem? itemSeleccionado = BuscarAncestro<ListBoxItem>((DependencyObject)e.OriginalSource);
            if (sender is not ListBox listBox
                || itemSeleccionado?.DataContext is not CorexProd.Entidad.Entidades.Producto producto)
                return;

            if (listBox.DataContext is ProformaDetalleItemViewModel item)
            {
                item.SeleccionarProductoBusqueda(producto);
                e.Handled = true;
            }
        }

        private static T? BuscarAncestro<T>(DependencyObject origen)
            where T : DependencyObject
        {
            DependencyObject? actual = origen;
            while (actual != null)
            {
                if (actual is T encontrado)
                    return encontrado;
                actual = VisualTreeHelper.GetParent(actual);
            }
            return null;
        }

        private static System.Collections.Generic.IEnumerable<T> BuscarDescendientes<T>(DependencyObject origen)
            where T : DependencyObject
        {
            int cantidad = VisualTreeHelper.GetChildrenCount(origen);
            for (int i = 0; i < cantidad; i++)
            {
                DependencyObject hijo = VisualTreeHelper.GetChild(origen, i);
                if (hijo is T encontrado)
                    yield return encontrado;

                foreach (T descendiente in BuscarDescendientes<T>(hijo))
                    yield return descendiente;
            }
        }
    }
}
