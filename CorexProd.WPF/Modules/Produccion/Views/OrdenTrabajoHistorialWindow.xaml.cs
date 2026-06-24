using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Helpers;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace CorexProd.WPF.Modules.Produccion.Views
{
    public partial class OrdenTrabajoHistorialWindow : Window
    {
        private readonly OrdenTrabajo _ot;
        private readonly OrdenTrabajoNegocio _negocio = new();
        private List<OrdenTrabajoMovimiento> _movimientos = [];
        private List<OrdenTrabajoMovimiento> _filtrados = [];
        private bool _cargandoFiltros;

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
            FechaOtText.Text = $"Registro OT: {_ot.FechaRegistro:dd/MM/yyyy HH:mm:ss}";

            try
            {
                _movimientos = _negocio.ListarMovimientos(_ot.IdOrdenTrabajo);
                CargarFiltros();
                AplicarFiltros();
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudo cargar el historial: {ex.Message}");
                ResumenText.Text = "Registros: 0";
            }
        }

        private void CargarFiltros()
        {
            _cargandoFiltros = true;
            ProductoCombo.ItemsSource = new[] { "Todos" }
                .Concat(_movimientos.Select(x => x.Producto).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x))
                .ToList();
            UsuarioCombo.ItemsSource = new[] { "Todos" }
                .Concat(_movimientos.Select(x => x.Usuario).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x))
                .ToList();
            TipoCombo.ItemsSource = new[] { "Todos" }
                .Concat(_movimientos.Select(x => x.Accion).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x))
                .ToList();
            ProductoCombo.SelectedIndex = 0;
            UsuarioCombo.SelectedIndex = 0;
            TipoCombo.SelectedIndex = 0;
            _cargandoFiltros = false;
        }

        private void AplicarFiltros()
        {
            if (_cargandoFiltros) return;

            IEnumerable<OrdenTrabajoMovimiento> query = _movimientos;
            string producto = ProductoCombo.SelectedItem?.ToString() ?? "Todos";
            string usuario = UsuarioCombo.SelectedItem?.ToString() ?? "Todos";
            string tipo = TipoCombo.SelectedItem?.ToString() ?? "Todos";

            if (producto != "Todos") query = query.Where(x => x.Producto == producto);
            if (usuario != "Todos") query = query.Where(x => x.Usuario == usuario);
            if (tipo != "Todos") query = query.Where(x => x.Accion == tipo);
            if (FechaDesdePicker.SelectedDate is DateTime desde) query = query.Where(x => x.FechaHora.Date >= desde.Date);
            if (FechaHastaPicker.SelectedDate is DateTime hasta) query = query.Where(x => x.FechaHora.Date <= hasta.Date);

            _filtrados = query.OrderByDescending(x => x.FechaHora).ToList();
            MovimientosGrid.ItemsSource = _filtrados;
            ResumenText.Text = $"Registros: {_filtrados.Count}";
            if (_filtrados.Count > 0) MovimientosGrid.ScrollIntoView(_filtrados[0]);
        }

        private void Filtro_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => AplicarFiltros();
        private void Fecha_Changed(object? sender, System.Windows.Controls.SelectionChangedEventArgs e) => AplicarFiltros();

        private void Limpiar_Click(object sender, RoutedEventArgs e)
        {
            _cargandoFiltros = true;
            ProductoCombo.SelectedIndex = 0;
            UsuarioCombo.SelectedIndex = 0;
            TipoCombo.SelectedIndex = 0;
            FechaDesdePicker.SelectedDate = null;
            FechaHastaPicker.SelectedDate = null;
            _cargandoFiltros = false;
            AplicarFiltros();
        }

        private void Exportar_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new()
            {
                Title = "Exportar historial de movimientos",
                FileName = $"Historial_{_ot.NumeroOT}_{DateTime.Now:yyyyMMdd_HHmm}.csv",
                Filter = "Archivo CSV para Excel (*.csv)|*.csv"
            };
            if (dialog.ShowDialog(this) != true) return;

            StringBuilder sb = new();
            sb.AppendLine("Fecha/Hora;Codigo;Producto;Origen;Destino;Cantidad;Accion;Usuario");
            foreach (OrdenTrabajoMovimiento m in _filtrados)
            {
                sb.AppendLine($"{m.FechaHora:dd/MM/yyyy HH:mm:ss};{Csv(m.CodigoProducto)};{Csv(m.NombreProducto)};{Csv(m.Origen)};{Csv(m.Destino)};{m.Cantidad:N3};{Csv(m.Accion)};{Csv(m.Usuario)}");
            }
            File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
            NotificationService.Success("Historial exportado correctamente.");
        }

        private static string Csv(string value) => (value ?? string.Empty).Replace(";", ",").Replace("\r", " ").Replace("\n", " ");
    }
}
