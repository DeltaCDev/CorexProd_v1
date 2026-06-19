using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Helpers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CorexProd.WPF.Modules.Ventas.Views
{
    public partial class GuiaInternaEditorWindow : Window
    {
        private readonly GuiaInternaNegocio _negocio = new();
        private readonly IngresoManualStockNegocio _almacenNegocio = new();
        private readonly UsuarioNegocio _usuarioNegocio = new();
        private GuiaInterna _guia;
        private bool _cargando;

        public GuiaInternaEditorWindow(GuiaInterna guia)
        {
            InitializeComponent();
            _guia = guia;
            _guia.UsuarioEmisor = SessionManager.UsuarioActual?.NombreUsuario ?? "Sistema";
            EmisorText.Text = SessionManager.UsuarioActual?.NombreCompleto ?? _guia.UsuarioEmisor;
            CargarCombos();
            DataContext = _guia;
        }

        private void CargarCombos()
        {
            _cargando = true;
            List<AlmacenStock> almacenes = _almacenNegocio.ListarAlmacenes().Where(a => a.Estado).ToList();
            AlmacenCombo.ItemsSource = almacenes;
            AlmacenCombo.SelectedItem = almacenes.FirstOrDefault(a => a.IdAlmacen == _guia.IdAlmacen);

            List<Usuario> usuarios = _usuarioNegocio.Listar().Where(u => u.Estado).ToList();
            AutorizadorCombo.ItemsSource = usuarios;
            AutorizadorCombo.SelectedItem = usuarios.FirstOrDefault(u => u.NombreUsuario == _guia.UsuarioEmisor) ?? usuarios.FirstOrDefault();
            _cargando = false;
        }

        private void AlmacenCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_cargando || AlmacenCombo.SelectedItem is not AlmacenStock almacen || almacen.IdAlmacen == _guia.IdAlmacen) return;
            GuiaInterna? actualizada = _negocio.Preparar(_guia.IdOrdenCompraInterna, almacen.IdAlmacen);
            if (actualizada == null)
            {
                NotificationService.Warning("No se pudo cargar el stock del almacén seleccionado.");
                return;
            }

            actualizada.UsuarioEmisor = _guia.UsuarioEmisor;
            actualizada.UsuarioAutorizador = _guia.UsuarioAutorizador;
            actualizada.FechaEmision = _guia.FechaEmision;
            actualizada.Observacion = _guia.Observacion;
            _guia = actualizada;
            DataContext = _guia;
        }

        private void Emitir_Click(object sender, RoutedEventArgs e)
        {
            DetallesGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            DetallesGrid.CommitEdit(DataGridEditingUnit.Row, true);
            GuiaInternaDetalle? invalido = _guia.Detalles.FirstOrDefault(d => d.CantidadDespachar > d.CantidadMaxima);
            if (invalido != null)
            {
                MostrarCantidadMaxima(invalido);
                return;
            }

            if (AlmacenCombo.SelectedItem is AlmacenStock almacen)
            {
                _guia.IdAlmacen = almacen.IdAlmacen;
                _guia.NombreAlmacen = almacen.NombreAlmacen;
            }
            if (AutorizadorCombo.SelectedItem is Usuario autorizador)
                _guia.UsuarioAutorizador = autorizador.NombreUsuario;

            string mensaje = _negocio.Emitir(_guia, out string numeroGuia);
            if (!mensaje.Contains("correctamente", System.StringComparison.OrdinalIgnoreCase))
            {
                NotificationService.Warning(mensaje);
                return;
            }

            NotificationService.Success($"{mensaje} Stock y kardex actualizados.");
            _guia.NumeroGuia = numeroGuia;
            DialogResult = true;
        }

        private void CantidadDespachar_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox { DataContext: GuiaInternaDetalle detalle } textBox
                || !decimal.TryParse(textBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal cantidad)
                || cantidad <= detalle.CantidadMaxima)
                return;

            detalle.CantidadDespachar = detalle.CantidadMaxima;
            textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();
            MostrarCantidadMaxima(detalle);
        }

        private static void MostrarCantidadMaxima(GuiaInternaDetalle detalle) =>
            NotificationService.Warning($"La cantidad máxima permitida para {detalle.CodigoProducto} es {detalle.CantidadMaxima:N2}.");

        private void Volver_Click(object sender, RoutedEventArgs e) => Close();
    }
}
