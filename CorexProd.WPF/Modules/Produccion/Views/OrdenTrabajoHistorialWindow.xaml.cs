using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Windows;

namespace CorexProd.WPF.Modules.Produccion.Views
{
    public partial class OrdenTrabajoHistorialWindow : Window
    {
        private readonly OrdenTrabajo _ot;
        private readonly OrdenTrabajoNegocio _negocio = new();

        public OrdenTrabajoHistorialWindow(OrdenTrabajo ot)
        {
            InitializeComponent();
            _ot = ot;
            Cargar();
        }

        private void Cargar()
        {
            OtText.Text = $"OT: {_ot.NumeroOT}";
            OciText.Text = $"Orden de Compra Cliente: {_ot.OrdenCompraCliente}";
            ClienteText.Text = $"Cliente: {_ot.NombreCliente}";

            try
            {
                List<OrdenTrabajoMovimiento> movimientos = _negocio.ListarMovimientos(_ot.IdOrdenTrabajo);
                MovimientosGrid.ItemsSource = movimientos;
                ResumenText.Text = $"Registros: {movimientos.Count}";
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudo cargar el historial: {ex.Message}");
                ResumenText.Text = "Registros: 0";
            }
        }
    }
}
