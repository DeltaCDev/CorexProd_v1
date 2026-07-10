using CorexProd.Entidad.Entidades;
using CorexProd.WPF.Modules.Ventas.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace CorexProd.WPF.Modules.Ventas.Views
{
    public partial class OrdenesCompraInternaView : UserControl
    {
        public OrdenesCompraInternaView()
        {
            InitializeComponent();
            AgregarColumnaFechaEntrega();
            DataContext = new OrdenesCompraInternaViewModel();
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

        private static T? BuscarDescendiente<T>(DependencyObject origen) where T : DependencyObject
        {
            foreach (object hijo in LogicalTreeHelper.GetChildren(origen))
            {
                if (hijo is T encontrado)
                    return encontrado;

                if (hijo is DependencyObject dependencia)
                {
                    T? resultado = BuscarDescendiente<T>(dependencia);
                    if (resultado != null)
                        return resultado;
                }
            }

            return null;
        }
    }
}
