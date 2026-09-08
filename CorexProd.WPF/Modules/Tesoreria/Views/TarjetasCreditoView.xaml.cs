using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CorexProd.WPF.Modules.Tesoreria.Views
{
    public partial class TarjetasCreditoView : UserControl
    {
        private readonly TarjetaCreditoNegocio _negocio = new();
        private bool _inicializada;

        public TarjetasCreditoView()
        {
            InitializeComponent();
            _inicializada = true;
            CargarEntidades();
            Cargar();
        }
        private void CargarEntidades()
        {
            try { EntidadComboBox.ItemsSource = new[] { new EntidadFinanciera { IdEntidadFinanciera = 0, Nombre = "Todas" } }.Concat(_negocio.ListarEntidadesFinancieras().OrderBy(x => x.Nombre)).ToList(); EntidadComboBox.SelectedIndex = 0; }
            catch (Exception ex) { NotificationService.Error($"No se pudieron cargar las entidades: {ex.Message}"); }
        }
        private void Cargar()
        {
            if (!_inicializada)
                return;

            try
            {
                string estado = (EstadoComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todos";
                int? entidad = EntidadComboBox.SelectedValue is int id && id > 0 ? id : null;
                List<TarjetaCredito> tarjetas = _negocio.ListarTarjetas(TextoTextBox.Text, estado == "Todos" ? null : estado, entidad);
                TarjetasDataGrid.ItemsSource = tarjetas;
                ResumenTextBlock.Text = $"{tarjetas.Count} tarjeta(s) encontrada(s)";
            }
            catch (Exception ex)
            {
                NotificationService.Error(EsProcedimientoNoInstalado(ex, "USP_TES_TARJETA_LISTAR")
                    ? "No se pudo cargar Tarjetas de Crédito. Verifique que el procedimiento USP_TES_TARJETA_LISTAR esté instalado en la base de datos."
                    : $"No se pudieron cargar las tarjetas: {ex.Message}");
            }
        }
        private void Filtro_Changed(object sender, RoutedEventArgs e)
        {
            if (_inicializada)
                Cargar();
        }
        private void Actualizar_Click(object sender, RoutedEventArgs e) => Cargar();
        private void Nueva_Click(object sender, RoutedEventArgs e) { AbrirEditor(null); }
        private void Ver_Click(object sender, RoutedEventArgs e) => AbrirPeriodos((sender as FrameworkElement)?.Tag as TarjetaCredito);
        private void Periodos_Click(object sender, RoutedEventArgs e) => AbrirPeriodos((sender as FrameworkElement)?.Tag as TarjetaCredito);
        private void Editar_Click(object sender, RoutedEventArgs e)
        {
            TarjetaCredito? tarjeta = (sender as FrameworkElement)?.Tag as TarjetaCredito;
            if (tarjeta?.EstaAnulada == true) { NotificationService.Warning("Una tarjeta anulada no puede editarse."); return; }
            AbrirEditor(tarjeta);
        }
        private void AbrirEditor(TarjetaCredito? tarjeta) { TarjetaCreditoEditorWindow w = new(tarjeta) { Owner = Window.GetWindow(this) }; if (w.ShowDialog() == true) Cargar(); }
        private void AbrirPeriodos(TarjetaCredito? tarjeta) { if (tarjeta == null) return; TarjetaCreditoDetalleWindow w = new(tarjeta.IdTarjetaCredito) { Owner = Window.GetWindow(this) }; w.ShowDialog(); Cargar(); }
        private void Estado_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is not TarjetaCredito tarjeta || tarjeta.EstaAnulada) { NotificationService.Warning("Una tarjeta anulada no puede cambiar de estado."); return; }
            tarjeta.Estado = tarjeta.EstaActiva ? "INACTIVA" : "ACTIVA"; tarjeta.Activo = tarjeta.EstaActiva;
            TarjetaCreditoResultado r = _negocio.GuardarTarjeta(tarjeta, Usuario()); if (r.Resultado) { NotificationService.Success(r.Mensaje); Cargar(); } else NotificationService.Warning(r.Mensaje);
        }
        private void Anular_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is not TarjetaCredito tarjeta || tarjeta.EstaAnulada) return;
            if (!ConfirmDialogService.Confirmar($"¿Desea anular la tarjeta {tarjeta.DescripcionTarjeta}?", "Confirmar anulación")) return;
            MotivoAnulacionWindow ventana = new("Anular tarjeta", tarjeta.DescripcionTarjeta) { Owner = Window.GetWindow(this) };
            if (ventana.ShowDialog() != true) return;
            TarjetaCreditoResultado r = _negocio.AnularTarjeta(tarjeta.IdTarjetaCredito, Usuario(), ventana.Motivo); if (r.Resultado) { NotificationService.Success(r.Mensaje); Cargar(); } else NotificationService.Warning(r.Mensaje);
        }
        private static string Usuario() => SessionManager.UsuarioActual?.NombreCompleto ?? SessionManager.UsuarioActual?.NombreUsuario ?? "Sistema";
        private static bool EsProcedimientoNoInstalado(Exception ex, string procedimiento) =>
            ex.Message.Contains(procedimiento, StringComparison.OrdinalIgnoreCase) &&
            (ex.Message.Contains("procedure", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("procedimiento", StringComparison.OrdinalIgnoreCase));
    }
}
