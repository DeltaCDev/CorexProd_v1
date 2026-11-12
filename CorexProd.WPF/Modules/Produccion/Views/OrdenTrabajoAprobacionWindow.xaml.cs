using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace CorexProd.WPF.Modules.Produccion.Views
{
    public partial class OrdenTrabajoAprobacionWindow : Window
    {
        private readonly OrdenTrabajo _ot;
        private readonly OrdenTrabajoNegocio _negocio = new();
        public OrdenTrabajoAprobacionWindow(OrdenTrabajo ot)
        {
            InitializeComponent(); _ot = ot;
            CabeceraText.Text = $"OT {_ot.NumeroOT} | OCI {_ot.NumeroOci}";
            ClienteText.Text = _ot.NombreCliente; OcText.Text = _ot.OrdenCompraCliente;
            FechaText.Text = (_ot.FechaRegistro == default ? _ot.FechaEmision : _ot.FechaRegistro).ToString("dd/MM/yyyy HH:mm");
            EstadoText.Text = _ot.EstadoAprobacion;
            DetalleGrid.ItemsSource = _ot.Detalles.Select(x => new AprobacionItem
            {
                Codigo = x.CodigoProducto, Producto = x.NombreProducto, Requerida = x.CantidadRequerida,
                Adicional = x.CantidadAdicionalStock, Total = x.CantidadPlanificada,
                Faltante = Math.Max(0, x.CantidadPlanificada - x.CantidadAdicionalStock),
                StockActual = Math.Max(0, x.CantidadRequerida - Math.Max(0, x.CantidadPlanificada - x.CantidadAdicionalStock))
            }).ToList();
        }
        private void Aprobar_Click(object sender, RoutedEventArgs e)
        {
            AutorizacionOperacionWindow auth = new("Aprobación de Gerencia", soloAprobadores: true) { Owner = this };
            if (auth.ShowDialog() != true) return;
            Usuario? gerente = _negocio.AutenticarAprobadorGerencia(auth.Usuario, auth.Clave);
            if (gerente == null) { MessageBox.Show(this, "El usuario no tiene autorización de Gerencia o la contraseña no es válida.", "Aprobación", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            try { _negocio.AprobarGerencia(_ot.IdOrdenTrabajo, gerente.IdUsuario, auth.Observacion); MessageBox.Show(this, "OT aprobada correctamente. Ya puede pasar a Corte.", "Aprobación", MessageBoxButton.OK, MessageBoxImage.Information); DialogResult = true; }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Aprobación", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
        private void Rechazar_Click(object sender, RoutedEventArgs e)
        {
            AutorizacionOperacionWindow auth = new("Rechazo de OT por Gerencia", soloAprobadores: true) { Owner = this };
            if (auth.ShowDialog() != true || string.IsNullOrWhiteSpace(auth.Observacion)) { if (auth.DialogResult == true) MessageBox.Show(this, "Ingrese el motivo del rechazo en la observación.", "Rechazo", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            Usuario? gerente = _negocio.AutenticarAprobadorGerencia(auth.Usuario, auth.Clave);
            if (gerente == null) { MessageBox.Show(this, "El usuario no tiene autorización de Gerencia o la contraseña no es válida.", "Rechazo", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            try { _negocio.RechazarGerencia(_ot.IdOrdenTrabajo, gerente.IdUsuario, auth.Observacion); MessageBox.Show(this, "OT rechazada.", "Rechazo", MessageBoxButton.OK, MessageBoxImage.Information); DialogResult = true; }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Rechazo", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
        private sealed class AprobacionItem { public string Codigo { get; init; } = string.Empty; public string Producto { get; init; } = string.Empty; public decimal Requerida { get; init; } public decimal StockActual { get; init; } public decimal Faltante { get; init; } public decimal Adicional { get; init; } public decimal Total { get; init; } }
    }
}
