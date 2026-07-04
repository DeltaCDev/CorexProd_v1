using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CorexProd.WPF.Modules.Produccion.Views
{
    public partial class NuevaOrdenTrabajoWindow : Window
    {
        private readonly OrdenTrabajoNegocio _otNegocio = new();
        private readonly OrdenCompraInternaNegocio _ociNegocio = new();
        private readonly List<OrdenTrabajo> _ordenes;
        private List<OrdenTrabajo> _regularizacionItems = [];
        private List<OrdenTrabajoValidacionProducto> _productos = [];

        public NuevaOrdenTrabajoWindow(IEnumerable<OrdenTrabajo> ordenes)
        {
            InitializeComponent();
            _ordenes = ordenes.ToList();
            CargarRegularizacion();
        }

        private void CargarRegularizacion()
        {
            string texto = BuscarTextBox.Text.Trim();
            IEnumerable<OrdenTrabajo> candidatas = _ordenes
                .Where(x => x.IdOrdenCompraInterna > 0
                    && !string.IsNullOrWhiteSpace(x.NumeroOci)
                    && x.IdOrdenTrabajoRelacionada == null
                    && x.TotalPendiente > 0
                    && EsOtRegularizable(x.EstadoOperativo)
                    && !TieneRegularizacionActiva(x.IdOrdenTrabajo));

            if (!string.IsNullOrWhiteSpace(texto))
            {
                candidatas = candidatas.Where(x =>
                    x.NumeroOci.Contains(texto, StringComparison.OrdinalIgnoreCase)
                    || x.NumeroOT.Contains(texto, StringComparison.OrdinalIgnoreCase)
                    || x.NombreCliente.Contains(texto, StringComparison.OrdinalIgnoreCase)
                    || x.OrdenCompraCliente.Contains(texto, StringComparison.OrdinalIgnoreCase));
            }

            _regularizacionItems = candidatas
                .OrderByDescending(x => x.FechaRegistro)
                .ToList();

            OtGrid.ItemsSource = _regularizacionItems;
            ResumenText.Text = $"{_regularizacionItems.Count} OT disponibles para regularizacion";
            if (_regularizacionItems.Count > 0) OtGrid.SelectedIndex = 0;
        }

        private void OtGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _productos = [];
            if (OtGrid.SelectedItem is not OrdenTrabajo ot)
            {
                ProductosGrid.ItemsSource = null;
                GenerarButton.IsEnabled = false;
                return;
            }

            try
            {
                _productos = _otNegocio.ListarPendientesRegularizacion(ot.IdOrdenTrabajo);
                ProductosGrid.ItemsSource = _productos;
                GenerarButton.IsEnabled = _productos.Count > 0;
            }
            catch (Exception ex)
            {
                GenerarButton.IsEnabled = false;
                NotificationService.Error($"No se pudo calcular el faltante: {ex.Message}");
            }
        }

        private void Buscar_Changed(object sender, TextChangedEventArgs e) => CargarRegularizacion();

        private void Regularizacion_Click(object sender, RoutedEventArgs e) => CargarRegularizacion();

        private void Stock_Click(object sender, RoutedEventArgs e)
        {
            NotificationService.Warning("El abastecimiento de stock requiere habilitar OT manual sin OCI en la base de datos.");
        }

        private void Generar_Click(object sender, RoutedEventArgs e)
        {
            if (OtGrid.SelectedItem is not OrdenTrabajo ot)
            {
                NotificationService.Warning("Seleccione una OT pendiente.");
                return;
            }

            if (_productos.Count == 0)
            {
                NotificationService.Warning("La OT seleccionada no tiene productos con cantidad faltante.");
                return;
            }

            OrdenCompraInterna? oci = _ociNegocio.Obtener(ot.IdOrdenCompraInterna);
            if (oci == null)
            {
                NotificationService.Warning("No se encontro la OCI seleccionada.");
                return;
            }

            ValidacionInsumosWindow validacion = new(oci, _productos) { Owner = this };
            if (validacion.ShowDialog() != true) return;

            OrdenTrabajoCrearWindow crear = new(oci, _productos, ot)
            {
                Owner = this
            };

            if (crear.ShowDialog() == true)
                DialogResult = true;
        }

        private static bool EsOtRegularizable(string estado) =>
            estado.Trim().ToUpperInvariant() is "TERMINADO PARCIAL";

        private bool TieneRegularizacionActiva(int idOrdenTrabajo) =>
            _ordenes.Any(x =>
                x.IdOrdenTrabajoRelacionada == idOrdenTrabajo
                && x.EstadoOperativo.Trim().ToUpperInvariant() is "PENDIENTE" or "EMITIDA" or "EN PROCESO" or "EN_PROCESO");
    }
}
