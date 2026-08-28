using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.Modules.Tesoreria.Views;
using CorexProd.WPF.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Tesoreria.ViewModels
{
    public class CuentasPorPagarViewModel : BaseViewModel
    {
        private readonly CuentaPorPagarNegocio _negocio = new();
        private string _textoBusqueda = string.Empty;
        private string _estadoFiltro = "Todos";
        private CuentaPorPagarListado? _cuentaSeleccionada;

        public CuentasPorPagarViewModel()
        {
            NuevaCommand = new RelayCommand(_ => Nueva());
            ActualizarCommand = new RelayCommand(_ => CargarCuentas());
            VerCommand = new RelayCommand(parametro => Ver(parametro));
            AnularCommand = new RelayCommand(parametro => Anular(parametro));
            CargarCuentas();
        }

        public ObservableCollection<CuentaPorPagarListado> Cuentas { get; } = [];
        public string[] Estados { get; } = ["Todos", "PENDIENTE", "ANULADA"];
        public ICommand NuevaCommand { get; }
        public ICommand ActualizarCommand { get; }
        public ICommand VerCommand { get; }
        public ICommand AnularCommand { get; }

        public string TextoBusqueda
        {
            get => _textoBusqueda;
            set
            {
                _textoBusqueda = value ?? string.Empty;
                OnPropertyChanged();
                CargarCuentas();
            }
        }

        public string EstadoFiltro
        {
            get => _estadoFiltro;
            set
            {
                _estadoFiltro = string.IsNullOrWhiteSpace(value) ? "Todos" : value;
                OnPropertyChanged();
                CargarCuentas();
            }
        }

        public CuentaPorPagarListado? CuentaSeleccionada
        {
            get => _cuentaSeleccionada;
            set { _cuentaSeleccionada = value; OnPropertyChanged(); }
        }

        public string Resumen => $"{Cuentas.Count} cuenta(s) encontradas";

        private void CargarCuentas()
        {
            try
            {
                Cuentas.Clear();
                foreach (CuentaPorPagarListado cuenta in _negocio.Listar(null, null, null, EstadoFiltro, TextoBusqueda))
                    Cuentas.Add(cuenta);

                OnPropertyChanged(nameof(Resumen));
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudieron listar las cuentas por pagar: {ex.Message}");
            }
        }

        private void Nueva()
        {
            CuentaPorPagarEditorWindow ventana = new()
            {
                Owner = Application.Current.MainWindow
            };

            if (ventana.ShowDialog() == true)
                CargarCuentas();
        }

        private void Ver(object? parametro)
        {
            CuentaPorPagarListado? cuenta = parametro as CuentaPorPagarListado ?? CuentaSeleccionada;
            if (cuenta == null)
            {
                NotificationService.Warning("Debe seleccionar una cuenta por pagar.");
                return;
            }

            try
            {
                CuentaPorPagar? detalle = _negocio.Obtener(cuenta.IdCuentaPorPagar);
                if (detalle == null)
                {
                    NotificationService.Warning("No se encontro la cuenta por pagar seleccionada.");
                    return;
                }

                CuentaPorPagarDetalleWindow ventana = new(detalle)
                {
                    Owner = Application.Current.MainWindow
                };
                ventana.ShowDialog();
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudo abrir el detalle: {ex.Message}");
            }
        }

        private void Anular(object? parametro)
        {
            CuentaPorPagarListado? cuenta = parametro as CuentaPorPagarListado ?? CuentaSeleccionada;
            if (cuenta == null)
            {
                NotificationService.Warning("Debe seleccionar una cuenta por pagar.");
                return;
            }

            if (cuenta.Estado.Equals("ANULADA", StringComparison.OrdinalIgnoreCase))
            {
                NotificationService.Warning("La cuenta por pagar ya se encuentra anulada.");
                return;
            }

            bool confirmar = ConfirmDialogService.Confirmar(
                $"¿Desea anular la cuenta por pagar de {cuenta.NombreProveedor}?",
                "Anular cuenta por pagar");

            if (!confirmar)
                return;

            AnularCuentaPorPagarWindow ventana = new(cuenta.NombreProveedor)
            {
                Owner = Application.Current.MainWindow
            };

            if (ventana.ShowDialog() != true)
                return;

            try
            {
                string usuario = SessionManager.UsuarioActual?.NombreCompleto
                    ?? SessionManager.UsuarioActual?.NombreUsuario
                    ?? "Sistema";
                CuentaPorPagarResultado resultado = _negocio.Anular(cuenta.IdCuentaPorPagar, usuario, ventana.MotivoAnulacion);

                if (resultado.Resultado)
                {
                    NotificationService.Success(resultado.Mensaje);
                    CargarCuentas();
                }
                else
                {
                    NotificationService.Warning(resultado.Mensaje);
                }
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudo anular la cuenta por pagar: {ex.Message}");
            }
        }
    }
}
