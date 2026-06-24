using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace CorexProd.WPF.Modules.Produccion.Views
{
    public partial class OrdenTrabajoCrearWindow : Window
    {
        private readonly OrdenCompraInterna _oci;
        private readonly List<OrdenTrabajoValidacionProducto> _productos;
        private readonly OrdenTrabajoNegocio _negocio = new();
        private readonly ParametroNegocio _parametroNegocio = new();
        private bool _guardando;

        public OrdenTrabajoCrearWindow(
            OrdenCompraInterna oci,
            IEnumerable<OrdenTrabajoValidacionProducto> productos)
        {
            InitializeComponent();
            _oci = oci;
            _productos = productos.ToList();
            CabeceraText.Text = $"OCI {oci.NumeroOci} | Cliente: {oci.NombreCliente}";
            OrdenCompraText.Text = oci.OrdenCompraCliente;
            DetallesGrid.ItemsSource = _productos;
        }

        private void Observacion_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not OrdenTrabajoValidacionProducto producto)
                return;

            string observacion = string.IsNullOrWhiteSpace(producto.Observacion)
                ? "El producto no tiene observaciones registradas."
                : producto.Observacion;
            MessageBox.Show(
                observacion,
                $"Observaciones - {producto.CodigoProducto}",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ImprimirFicha_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not OrdenTrabajoValidacionProducto producto)
                return;

            try
            {
                if (string.IsNullOrWhiteSpace(producto.CodigoProducto))
                {
                    NotificationService.Warning("El producto no tiene codigo para buscar su ficha tecnica.");
                    return;
                }

                var parametroRuta = _parametroNegocio.ObtenerPorCodigo("RUTA_FICHA_TECNICA");

                if (parametroRuta == null)
                {
                    NotificationService.Warning("No existe el parametro RUTA_FICHA_TECNICA.");
                    return;
                }

                string rutaBase = parametroRuta.ValorParametro;

                if (string.IsNullOrWhiteSpace(rutaBase))
                {
                    NotificationService.Warning("La ruta de fichas tecnicas esta vacia.");
                    return;
                }

                string rutaPdf = Path.Combine(rutaBase, $"{producto.CodigoProducto}.pdf");

                if (!File.Exists(rutaPdf))
                {
                    NotificationService.Warning($"No hay ficha tecnica en la ruta:\n{rutaPdf}");
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = rutaPdf,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                NotificationService.Error($"Error al abrir la ficha tecnica:\n{ex.Message}");
            }
        }

        private void VerDetalle_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not OrdenTrabajoValidacionProducto producto)
                return;

            IReadOnlyCollection<OrdenTrabajoInsumoDetalle> insumos =
                _negocio.DetalleInsumos(producto.IdOrdenCompraInternaDetalle);
            new DetalleInsumosProductoWindow(producto, _oci.OrdenCompraCliente, insumos)
            {
                Owner = this
            }.ShowDialog();
        }

        private void Crear_Click(object sender, RoutedEventArgs e)
        {
            if (_guardando) return;

            try
            {
                _guardando = true;
                GuardarButton.IsEnabled = false;

                List<OrdenTrabajoPlanificacion> items = _productos.Select(producto =>
                    new OrdenTrabajoPlanificacion
                    {
                        IdOrdenCompraInternaDetalle = producto.IdOrdenCompraInternaDetalle,
                        CantidadPlanificada = producto.CantidadRequerida
                    }).ToList();

                int idUsuario = SessionManager.UsuarioActual?.IdUsuario ?? 0;
                (int _, string numero) = _negocio.Crear(
                    _oci.IdOrdenCompraInterna,
                    idUsuario,
                    ObservacionText.Text.Trim(),
                    items);

                NotificationService.Success(
                    $"Se creo {numero} con {items.Count} producto(s). La OCI paso a PROCESO.");
                DialogResult = true;
            }
            catch (Exception ex)
            {
                NotificationService.Error(ex.Message);
            }
            finally
            {
                _guardando = false;
                GuardarButton.IsEnabled = true;
            }
        }
    }
}
