using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Produccion.Views
{
    public partial class OrdenTrabajoManualWindow : Window
    {
        private readonly ProductoNegocio _productoNegocio = new();
        private readonly OrdenTrabajoNegocio _ordenTrabajoNegocio = new();
        private readonly List<Producto> _productos;

        public ObservableCollection<DetalleManualItem> Detalles { get; } = [];

        public OrdenTrabajoManualWindow()
        {
            InitializeComponent();
            DataContext = this;
            _productos = _productoNegocio.Listar()
                .Where(x => x.Estado)
                .OrderBy(x => x.Codigo)
                .ThenBy(x => x.NombreProducto)
                .ToList();
            FiltrarProductos();
        }

        private void BuscarProducto_Changed(object sender, TextChangedEventArgs e) => FiltrarProductos();

        private void FiltrarProductos()
        {
            string texto = BuscarProductoTextBox.Text.Trim();
            IEnumerable<Producto> consulta = _productos;
            if (!string.IsNullOrWhiteSpace(texto))
            {
                consulta = consulta.Where(x =>
                    x.Codigo.Contains(texto, StringComparison.OrdinalIgnoreCase)
                    || x.NombreProducto.Contains(texto, StringComparison.OrdinalIgnoreCase)
                    || x.EtiquetaCliente.Contains(texto, StringComparison.OrdinalIgnoreCase));
            }

            ProductosListBox.ItemsSource = consulta.Take(150).ToList();
            if (ProductosListBox.Items.Count > 0)
                ProductosListBox.SelectedIndex = 0;
        }

        private void ProductosListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e) => AgregarSeleccionado();
        private void Agregar_Click(object sender, RoutedEventArgs e) => AgregarSeleccionado();

        private void AgregarSeleccionado()
        {
            if (ProductosListBox.SelectedItem is not Producto producto)
            {
                NotificationService.Warning("Seleccione un producto.");
                return;
            }

            if (!decimal.TryParse(CantidadTextBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal cantidad) || cantidad <= 0)
            {
                NotificationService.Warning("Ingrese una cantidad mayor que cero.");
                CantidadTextBox.Focus();
                return;
            }

            DetalleManualItem? existente = Detalles.FirstOrDefault(x => x.IdProducto == producto.IdProducto);
            if (existente != null)
                existente.Cantidad += cantidad;
            else
                Detalles.Add(new DetalleManualItem(producto, cantidad));

            DetallesGrid.Items.Refresh();
            CantidadTextBox.Clear();
            CantidadTextBox.Focus();
        }

        private void Quitar_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is DetalleManualItem item)
                Detalles.Remove(item);
        }

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DetallesGrid.CommitEdit();
                List<OrdenTrabajoManualPlanificacion> plan = Detalles
                    .Where(x => x.Cantidad > 0)
                    .Select(x => new OrdenTrabajoManualPlanificacion
                    {
                        IdProducto = x.IdProducto,
                        CantidadPlanificada = x.Cantidad
                    })
                    .ToList();

                if (plan.Count == 0)
                {
                    NotificationService.Warning("Agregue al menos un producto con cantidad mayor que cero.");
                    return;
                }

                List<OrdenTrabajoValidacionProducto> validacion = _ordenTrabajoNegocio.ValidarInsumosManual(plan);
                OrdenCompraInterna resumen = new()
                {
                    NumeroOci = "OT MANUAL",
                    NombreCliente = "ABASTECIMIENTO DE STOCK"
                };

                ValidacionInsumosWindow ventanaValidacion = new(resumen, validacion)
                {
                    Owner = this
                };
                if (ventanaValidacion.ShowDialog() != true)
                    return;

                int idUsuario = SessionManager.UsuarioActual?.IdUsuario ?? 0;
                (int idOrdenTrabajo, string numero) = _ordenTrabajoNegocio.CrearManual(idUsuario, ObservacionTextBox.Text.Trim(), plan);
                OrdenTrabajo? otCreada = _ordenTrabajoNegocio.Obtener(idOrdenTrabajo);
                if (otCreada != null)
                    MobileNotificationPublisher.OtNueva(otCreada, "Desktop - OT manual");

                new DocumentoGeneradoResumenWindow(
                    "OT Manual generada correctamente",
                    $"Se genero la OT correctamente: {numero}.",
                    "Para producir los siguientes productos:",
                    Detalles.Select(x => new DocumentoGeneradoProducto
                    {
                        Codigo = x.Codigo,
                        Producto = x.NombreProducto,
                        Cantidad = x.Cantidad
                    }))
                {
                    Owner = this
                }.ShowDialog();

                DialogResult = true;
            }
            catch (Exception ex)
            {
                NotificationService.Error(ex.Message);
            }
        }
    }

    public class DetalleManualItem
    {
        public int IdProducto { get; }
        public string Codigo { get; }
        public string NombreProducto { get; }
        public decimal Cantidad { get; set; }

        public DetalleManualItem(Producto producto, decimal cantidad)
        {
            IdProducto = producto.IdProducto;
            Codigo = producto.Codigo;
            NombreProducto = producto.NombreProducto;
            Cantidad = cantidad;
        }
    }
}
