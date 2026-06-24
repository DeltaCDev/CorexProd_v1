using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Windows;

namespace CorexProd.WPF.Modules.Produccion.Views
{
    public partial class OrdenTrabajoKardexWindow : Window
    {
        private readonly OrdenTrabajo _ot;
        private readonly OrdenTrabajoNegocio _negocio = new();

        public OrdenTrabajoKardexWindow(OrdenTrabajo ot)
        {
            InitializeComponent();
            _ot = ot;
            Cargar();
        }

        private void Cargar()
        {
            OtText.Text = $"Orden de Trabajo: {_ot.NumeroOT}";
            FechaOtText.Text = $"Fecha OT: {_ot.FechaEmision:dd/MM/yyyy}";
            FechaGeneracionText.Text = $"Fecha de generacion: {DateTime.Now:dd/MM/yyyy HH:mm}";

            try
            {
                List<OrdenTrabajoKardexIngreso> ingresos = _negocio.ListarIngresosKardex(_ot.IdOrdenTrabajo);
                IngresosGrid.ItemsSource = ingresos;
                TotalText.Text = $"Total movimientos generados: {ingresos.Count}";
                if (ingresos.Count > 0)
                    IngresosGrid.ScrollIntoView(ingresos[0]);
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudo cargar el Kardex de la OT: {ex.Message}");
                TotalText.Text = "Total movimientos generados: 0";
            }
        }
    }
}
