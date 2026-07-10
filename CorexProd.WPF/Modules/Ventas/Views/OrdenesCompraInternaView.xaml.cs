using CorexProd.Entidad.Entidades;
using CorexProd.WPF.Modules.Ventas.ViewModels;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace CorexProd.WPF.Modules.Ventas.Views
{
    public partial class OrdenesCompraInternaView : UserControl
    {
        public OrdenesCompraInternaView()
        {
            InitializeComponent();
            MejorarCabecera();
            ConfigurarTabla();
            AgregarColumnaFechaEntrega();
            DataContext = new OrdenesCompraInternaViewModel();

            Loaded += (_, _) => OcultarBotonesImprimir(this);
        }

        private void MejorarCabecera()
        {
            TextBlock? titulo = BuscarDescendiente<TextBlock>(
                this,
                texto => string.Equals(
                    texto.Text,
                    "Gestiona las ordenes de compra",
                    StringComparison.OrdinalIgnoreCase));

            if (titulo == null)
                return;

            titulo.Text = "Lista de Órdenes de Compra Clientes";
            titulo.FontSize = 17;
            titulo.FontWeight = FontWeights.SemiBold;
            titulo.Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42));
            titulo.Margin = new Thickness(0, 2, 0, 0);
        }

        private void ConfigurarTabla()
        {
            DataGrid? tabla = BuscarDescendiente<DataGrid>(this);
            if (tabla == null)
                return;

            foreach (DataGridColumn columna in tabla.Columns)
            {
                if (string.Equals(columna.Header?.ToString(), "Acciones", StringComparison.OrdinalIgnoreCase))
                {
                    columna.Width = new DataGridLength(345);
                    break;
                }
            }

            tabla.LoadingRow += Tabla_LoadingRow;
        }

        private static void Tabla_LoadingRow(object? sender, DataGridRowEventArgs e)
        {
            e.Row.Dispatcher.BeginInvoke(
                new Action(() => OcultarBotonesImprimir(e.Row)),
                DispatcherPriority.Loaded);
        }

        private static void OcultarBotonesImprimir(DependencyObject origen)
        {
            foreach (Button boton in BuscarDescendientesVisuales<Button>(origen))
            {
                if (!string.Equals(boton.ToolTip?.ToString(), "Imprimir", StringComparison.OrdinalIgnoreCase))
                    continue;

                boton.Visibility = Visibility.Collapsed;
                boton.Margin = new Thickness(0);
            }
        }

        private void AgregarColumnaFechaEntrega()
        {
            DataGrid? tabla = BuscarDescendiente<DataGrid>(this);
            if (tabla == null)
                return;

            foreach (DataGridColumn columna in tabla.Columns)
            {
                if (string.Equals(columna.Header?.ToString(), "Fecha entrega", StringComparison.OrdinalIgnoreCase))
                    return;
            }

            int indiceFechaEmision = -1;
            for (int i = 0; i < tabla.Columns.Count; i++)
            {
                if (string.Equals(tabla.Columns[i].Header?.ToString(), "Fecha emisión", StringComparison.OrdinalIgnoreCase))
                {
                    indiceFechaEmision = i;
                    break;
                }
            }

            DataGridTextColumn fechaEntrega = new()
            {
                Header = "Fecha entrega",
                Binding = new Binding(nameof(OrdenCompraInterna.FechaEntrega))
                {
                    StringFormat = "dd/MM/yyyy"
                },
                Width = 105,
                ElementStyle = TryFindResource("Celda") as Style
            };

            tabla.Columns.Insert(indiceFechaEmision >= 0 ? indiceFechaEmision + 1 : tabla.Columns.Count, fechaEntrega);
        }

        private static T? BuscarDescendiente<T>(DependencyObject origen) where T : DependencyObject =>
            BuscarDescendiente<T>(origen, _ => true);

        private static T? BuscarDescendiente<T>(DependencyObject origen, Func<T, bool> condicion)
            where T : DependencyObject
        {
            foreach (object hijo in LogicalTreeHelper.GetChildren(origen))
            {
                if (hijo is T encontrado && condicion(encontrado))
                    return encontrado;

                if (hijo is DependencyObject dependencia)
                {
                    T? resultado = BuscarDescendiente(dependencia, condicion);
                    if (resultado != null)
                        return resultado;
                }
            }

            return null;
        }

        private static IEnumerable<T> BuscarDescendientesVisuales<T>(DependencyObject origen)
            where T : DependencyObject
        {
            int cantidad = VisualTreeHelper.GetChildrenCount(origen);
            for (int i = 0; i < cantidad; i++)
            {
                DependencyObject hijo = VisualTreeHelper.GetChild(origen, i);
                if (hijo is T encontrado)
                    yield return encontrado;

                foreach (T descendiente in BuscarDescendientesVisuales<T>(hijo))
                    yield return descendiente;
            }
        }
    }
}
