using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Helpers;
using System;
using System.Linq;
using System.Windows;

namespace CorexProd.WPF.Modules.Tesoreria.Views
{
    public partial class TarjetaCreditoEditorWindow : Window
    {
        private readonly TarjetaCreditoNegocio _negocio = new();
        private readonly TarjetaCredito _tarjeta;

        public TarjetaCreditoEditorWindow(TarjetaCredito? tarjeta)
        {
            InitializeComponent();
            _tarjeta = tarjeta == null ? new() : _negocio.ObtenerTarjeta(tarjeta.IdTarjetaCredito) ?? tarjeta;
            Cargar();
        }

        private void Cargar()
        {
            try
            {
                EntidadComboBox.ItemsSource = _negocio.ListarEntidadesFinancieras().OrderBy(x => x.Nombre).ToList();
                EmpresaComboBox.ItemsSource = _negocio.ListarEmpresas().OrderBy(x => x.Nombre).ToList();
                EntidadComboBox.SelectedValue = _tarjeta.IdEntidadFinanciera;
                EmpresaComboBox.SelectedValue = _tarjeta.IdEmpresaResponsable;
                AliasTextBox.Text = _tarjeta.Alias;
                Ultimos4TextBox.Text = _tarjeta.Ultimos4;
                TitularTextBox.Text = _tarjeta.Titular;
                PenCheckBox.IsChecked = _tarjeta.MonedasHabilitadas.Contains("PEN");
                UsdCheckBox.IsChecked = _tarjeta.MonedasHabilitadas.Contains("USD");
                DiaCorteTextBox.Text = _tarjeta.DiaCorte?.ToString() ?? string.Empty;
                DiaPagoTextBox.Text = _tarjeta.DiaPago?.ToString() ?? string.Empty;
                EstadoComboBox.SelectedValue = _tarjeta.Estado;
                ObservacionTextBox.Text = _tarjeta.Observacion;
            }
            catch (Exception ex)
            {
                NotificationService.Error(ex.Message);
            }
        }

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            _tarjeta.MonedasHabilitadas = [];
            if (PenCheckBox.IsChecked == true)
                _tarjeta.MonedasHabilitadas.Add("PEN");
            if (UsdCheckBox.IsChecked == true)
                _tarjeta.MonedasHabilitadas.Add("USD");

            _tarjeta.IdEntidadFinanciera = (int)(EntidadComboBox.SelectedValue ?? 0);
            _tarjeta.IdEmpresaResponsable = EmpresaComboBox.SelectedValue as int?;
            _tarjeta.EmpresaResponsable = (EmpresaComboBox.SelectedItem as Empresa)?.Nombre ?? string.Empty;
            _tarjeta.Alias = AliasTextBox.Text;
            _tarjeta.Ultimos4 = Ultimos4TextBox.Text;
            _tarjeta.Titular = TitularTextBox.Text;
            _tarjeta.DiaCorte = byte.TryParse(DiaCorteTextBox.Text, out byte corte) ? corte : null;
            _tarjeta.DiaPago = byte.TryParse(DiaPagoTextBox.Text, out byte pago) ? pago : null;
            _tarjeta.Estado = (EstadoComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "ACTIVA";
            _tarjeta.Observacion = ObservacionTextBox.Text;

            TarjetaCreditoResultado resultado = _negocio.GuardarTarjeta(
                _tarjeta, SessionManager.UsuarioActual?.NombreUsuario ?? "Sistema");
            if (resultado.Resultado)
            {
                NotificationService.Success(resultado.Mensaje);
                DialogResult = true;
            }
            else
                NotificationService.Warning(resultado.Mensaje);
        }
    }
}
