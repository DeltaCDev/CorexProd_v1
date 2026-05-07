using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.ViewModels;
using System;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Seguridad.ViewModels
{
    public class CambiarClaveViewModel : BaseViewModel
    {
        private readonly UsuarioNegocio _usuarioNegocio = new();

        public event Func<(string claveActual, string claveNueva, string confirmarClave)>? SolicitarClaves;
        public event Action? LimpiarPasswordBoxes;

        public ICommand CambiarClaveCommand { get; }
        public ICommand LimpiarCommand { get; }

        public CambiarClaveViewModel()
        {
            CambiarClaveCommand = new RelayCommand(_ => CambiarClave());
            LimpiarCommand = new RelayCommand(_ => Limpiar());
        }

        private void CambiarClave()
        {
            if (SessionManager.UsuarioActual == null)
            {
                NotificationService.Warning("No hay usuario en sesión.");
                return;
            }

            var claves = SolicitarClaves?.Invoke();

            if (claves == null)
            {
                NotificationService.Warning("No se pudieron obtener las claves.");
                return;
            }

            bool confirmar = ConfirmDialogService.Confirmar(
                "¿Desea cambiar su clave de acceso?",
                "Confirmar cambio de clave");

            if (!confirmar)
            {
                return;
            }

            string mensaje = _usuarioNegocio.CambiarClave(
                SessionManager.UsuarioActual.IdUsuario,
                claves.Value.claveActual,
                claves.Value.claveNueva,
                claves.Value.confirmarClave
            );

            if (mensaje.Contains("correctamente"))
            {
                NotificationService.Success(mensaje);
                Limpiar();
            }
            else
            {
                NotificationService.Warning(mensaje);
            }
        }

        private void Limpiar()
        {
            LimpiarPasswordBoxes?.Invoke();
        }
    }
}