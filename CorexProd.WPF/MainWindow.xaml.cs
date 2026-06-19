using CorexProd.WPF.Helpers;
using CorexProd.WPF.ViewModels;
using Microsoft.Win32;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace CorexProd.WPF
{
    public partial class MainWindow : Window
    {
        private DataGrid? _tablaActiva;
        private INotifyCollectionChanged? _itemsObservables;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();

            ((INotifyPropertyChanged)DataContext).PropertyChanged += MainViewModel_PropertyChanged;
            AddHandler(Mouse.PreviewMouseMoveEvent, new MouseEventHandler(MainWindow_PreviewMouseMove), true);
            Loaded += (_, _) => ProgramarDeteccionDeTabla();
        }

        private void MainViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.VistaActual))
            {
                EstablecerTablaActiva(null);
                ProgramarDeteccionDeTabla();
            }
        }

        private void ProgramarDeteccionDeTabla()
        {
            Dispatcher.BeginInvoke(DetectarTablaPrincipal, DispatcherPriority.Loaded);
        }

        private void DetectarTablaPrincipal()
        {
            DataGrid? tabla = BuscarDescendientes<DataGrid>(ContenidoPrincipal)
                .Where(dataGrid => dataGrid.IsVisible)
                .OrderByDescending(dataGrid => dataGrid.ActualWidth * dataGrid.ActualHeight)
                .FirstOrDefault();

            EstablecerTablaActiva(tabla);
        }

        private void MainWindow_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            DependencyObject? actual = e.OriginalSource as DependencyObject;
            while (actual != null)
            {
                if (actual is DataGrid tabla)
                {
                    EstablecerTablaActiva(tabla);
                    return;
                }

                actual = ObtenerPadre(actual);
            }
        }

        private void EstablecerTablaActiva(DataGrid? tabla)
        {
            if (ReferenceEquals(_tablaActiva, tabla))
            {
                ActualizarCantidadItems();
                return;
            }

            if (_itemsObservables != null)
            {
                _itemsObservables.CollectionChanged -= Items_CollectionChanged;
            }

            _tablaActiva = tabla;
            _itemsObservables = tabla?.Items as INotifyCollectionChanged;

            if (_itemsObservables != null)
            {
                _itemsObservables.CollectionChanged += Items_CollectionChanged;
            }

            ListadoHerramientas.Visibility = tabla == null ? Visibility.Collapsed : Visibility.Visible;
            ActualizarCantidadItems();
        }

        private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => ActualizarCantidadItems();

        private void ActualizarCantidadItems()
        {
            if (_tablaActiva == null)
            {
                CantidadItemsText.Text = string.Empty;
                return;
            }

            int cantidad = _tablaActiva.Items.Cast<object>().Count(item => item != CollectionView.NewItemPlaceholder);
            CantidadItemsText.Text = cantidad == 1 ? "1 ítem listado" : $"{cantidad} ítems listados";
        }

        private void ExportarListado_Click(object sender, RoutedEventArgs e)
        {
            if (_tablaActiva == null)
            {
                return;
            }

            object? comando = _tablaActiva.DataContext?.GetType().GetProperty("ExportarCommand")?.GetValue(_tablaActiva.DataContext);
            if (comando is ICommand exportarCommand && exportarCommand.CanExecute(null))
            {
                exportarCommand.Execute(null);
                return;
            }

            ExportarTablaGenerica(_tablaActiva);
        }

        private void ExportarTablaGenerica(DataGrid tabla)
        {
            List<(string Encabezado, Binding Binding, bool EsBooleano)> columnas = tabla.Columns
                .Where(columna => columna.Visibility == Visibility.Visible)
                .OrderBy(columna => columna.DisplayIndex)
                .Select(columna =>
                {
                    string encabezado = columna.Header?.ToString()?.Trim() ?? string.Empty;
                    Binding? binding = (columna as DataGridBoundColumn)?.Binding as Binding;
                    return (Encabezado: encabezado, Binding: binding, EsBooleano: columna is DataGridCheckBoxColumn);
                })
                .Where(columna => !string.IsNullOrWhiteSpace(columna.Encabezado)
                                  && !columna.Encabezado.Equals("Acciones", StringComparison.OrdinalIgnoreCase)
                                  && columna.Binding?.Path?.Path != null)
                .Select(columna => (columna.Encabezado, columna.Binding!, columna.EsBooleano))
                .ToList();

            List<object> items = tabla.Items.Cast<object>()
                .Where(item => item != CollectionView.NewItemPlaceholder)
                .ToList();

            if (items.Count == 0)
            {
                NotificationService.Warning("No hay ítems para exportar.");
                return;
            }

            if (columnas.Count == 0)
            {
                NotificationService.Warning("La tabla no contiene columnas exportables.");
                return;
            }

            SaveFileDialog dialog = new()
            {
                Title = $"Exportar {((MainViewModel)DataContext).Titulo}",
                FileName = $"{NombreArchivoSeguro(((MainViewModel)DataContext).Titulo)}_{DateTime.Now:yyyyMMdd_HHmm}.csv",
                Filter = "Archivo CSV para Excel (*.csv)|*.csv",
                DefaultExt = ".csv"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            StringBuilder csv = new();
            csv.AppendLine(string.Join(";", columnas.Select(columna => EscaparCsv(columna.Encabezado))));

            foreach (object item in items)
            {
                csv.AppendLine(string.Join(";", columnas.Select(columna =>
                {
                    object? valor = ObtenerValor(item, columna.Binding.Path.Path);
                    string texto = columna.EsBooleano && valor is bool activo
                        ? activo ? "Sí" : "No"
                        : Convert.ToString(valor, CultureInfo.CurrentCulture) ?? string.Empty;
                    return EscaparCsv(texto);
                })));
            }

            File.WriteAllText(dialog.FileName, csv.ToString(), new UTF8Encoding(true));
            NotificationService.Success($"Se exportaron {items.Count} ítems correctamente.");
        }

        private static object? ObtenerValor(object objeto, string ruta)
        {
            object? valor = objeto;
            foreach (string propiedad in ruta.Split('.'))
            {
                if (valor == null)
                {
                    return null;
                }

                valor = valor.GetType().GetProperty(propiedad)?.GetValue(valor);
            }

            return valor;
        }

        private static string EscaparCsv(string valor) => $"\"{valor.Replace("\"", "\"\"")}\"";

        private static string NombreArchivoSeguro(string valor)
        {
            char[] invalidos = Path.GetInvalidFileNameChars();
            return string.Concat(valor.Select(caracter => invalidos.Contains(caracter) ? '_' : caracter));
        }

        private static IEnumerable<T> BuscarDescendientes<T>(DependencyObject padre) where T : DependencyObject
        {
            int cantidad = VisualTreeHelper.GetChildrenCount(padre);
            for (int i = 0; i < cantidad; i++)
            {
                DependencyObject hijo = VisualTreeHelper.GetChild(padre, i);
                if (hijo is T coincidencia)
                {
                    yield return coincidencia;
                }

                foreach (T descendiente in BuscarDescendientes<T>(hijo))
                {
                    yield return descendiente;
                }
            }
        }

        private static DependencyObject? ObtenerPadre(DependencyObject elemento) =>
            elemento is Visual ? VisualTreeHelper.GetParent(elemento) : LogicalTreeHelper.GetParent(elemento);

        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (DataContext is MainViewModel { OmitirConfirmacionCierre: true })
            {
                return;
            }

            bool confirmar = ConfirmDialogService.Confirmar(
                "¿Desea cerrar el sistema?",
                "Cerrar sistema");

            if (!confirmar)
            {
                e.Cancel = true;
            }
        }
    }
}
