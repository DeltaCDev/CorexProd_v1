using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Helpers;
using System.Windows;
using System.Windows.Controls;

namespace CorexProd.WPF.Modules.Tesoreria.Views
{
    public partial class CuentaPorPagarDetalleWindow : Window
    {
        private readonly CuentaPorPagarNegocio _negocio = new();
        private int _idCuentaPorPagar;

        public CuentaPorPagarDetalleWindow(CuentaPorPagar cuenta)
        {
            InitializeComponent();
            CargarCuenta(cuenta);
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AnularPago_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: CuentaPorPagarPago pago })
                return;

            if (pago.EstaAnulado)
            {
                NotificationService.Warning("El pago seleccionado ya se encuentra anulado.");
                return;
            }

            CuentaPorPagar? cuentaActual = DataContext as CuentaPorPagar;
            if (cuentaActual == null)
            {
                NotificationService.Warning("No se pudo identificar la cuenta por pagar.");
                return;
            }

            bool confirmar = ConfirmDialogService.Confirmar(
                "¿Está seguro de anular este pago? Esta acción recalculará el saldo pendiente de la cuota y su estado.",
                "Anular pago");

            if (!confirmar)
                return;

            AnularPagoCuentaPorPagarWindow ventana = new(cuentaActual.NombreProveedor, pago)
            {
                Owner = this
            };

            if (ventana.ShowDialog() != true)
                return;

            try
            {
                string usuario = SessionManager.UsuarioActual?.NombreCompleto
                    ?? SessionManager.UsuarioActual?.NombreUsuario
                    ?? "Sistema";

                CuentaPorPagarPagoResultado resultado = _negocio.AnularPago(pago.IdPago, ventana.MotivoAnulacion, usuario);
                if (!resultado.Resultado)
                {
                    NotificationService.Warning(resultado.Mensaje);
                    return;
                }

                NotificationService.Success(resultado.Mensaje);
                RefrescarCuenta();
            }
            catch (System.Exception ex)
            {
                NotificationService.Error($"No se pudo anular el pago: {ex.Message}");
            }
        }

        private void RefrescarCuenta()
        {
            CuentaPorPagar? cuenta = _negocio.Obtener(_idCuentaPorPagar);
            if (cuenta == null)
            {
                NotificationService.Warning("No se pudo refrescar el detalle de la cuenta por pagar.");
                return;
            }

            CargarCuenta(cuenta);
        }

        private void CargarCuenta(CuentaPorPagar cuenta)
        {
            _idCuentaPorPagar = cuenta.IdCuentaPorPagar;
            DataContext = cuenta;
        }
    }
}
