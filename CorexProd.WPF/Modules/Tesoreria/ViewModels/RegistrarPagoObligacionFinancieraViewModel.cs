using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Tesoreria.ViewModels
{
    public class RegistrarPagoObligacionFinancieraViewModel : BaseViewModel
    {
        private readonly ObligacionFinancieraNegocio _negocio = new();
        private readonly CultureInfo _cultura = new("es-PE");
        private readonly ObligacionFinanciera _prestamo;
        private readonly ObligacionFinancieraCuota _cuota;
        private BancoPagoItem? _bancoSeleccionado;
        private CuentaBancariaPagoItem? _cuentaSeleccionada;
        private DateTime _fechaPago = DateTime.Today;
        private decimal _monto;
        private string _numeroOperacion = string.Empty;
        private string _observacion = string.Empty;
        private bool _guardando;

        public RegistrarPagoObligacionFinancieraViewModel(ObligacionFinanciera prestamo, ObligacionFinancieraCuota cuota)
        {
            _prestamo = prestamo;
            _cuota = cuota;
            _monto = cuota.SaldoPendiente;

            RegistrarCommand = new RelayCommand(_ => Registrar(), _ => !Guardando);
            CancelarCommand = new RelayCommand(_ => CerrarVentana?.Invoke(false));

            CargarBancos();
            CargarCuentas();
        }

        public ObservableCollection<BancoPagoItem> Bancos { get; } = [];
        public ObservableCollection<CuentaBancariaPagoItem> CuentasBancarias { get; } = [];
        public ICommand RegistrarCommand { get; }
        public ICommand CancelarCommand { get; }
        public Action<bool>? CerrarVentana { get; set; }

        public string EntidadFinanciera => _prestamo.EntidadFinanciera;
        public string Titular => _prestamo.NombreTitular;
        public string CuotaTexto => _cuota.ReferenciaCuota;
        public DateTime FechaVencimiento => _cuota.FechaVencimiento;
        public string Moneda => _prestamo.Moneda;
        public string ImporteTexto => $"{Moneda} {_cuota.Importe.ToString("N2", _cultura)}";
        public string TotalPagadoTexto => $"{Moneda} {_cuota.TotalPagado.ToString("N2", _cultura)}";
        public string SaldoPendienteTexto => $"{Moneda} {_cuota.SaldoPendiente.ToString("N2", _cultura)}";

        public BancoPagoItem? BancoSeleccionado
        {
            get => _bancoSeleccionado;
            set
            {
                _bancoSeleccionado = value;
                OnPropertyChanged();
                CargarCuentas();
            }
        }

        public CuentaBancariaPagoItem? CuentaSeleccionada
        {
            get => _cuentaSeleccionada;
            set { _cuentaSeleccionada = value; OnPropertyChanged(); }
        }

        public DateTime FechaPago
        {
            get => _fechaPago;
            set { _fechaPago = value.Date; OnPropertyChanged(); }
        }

        public decimal Monto
        {
            get => _monto;
            set { _monto = value; OnPropertyChanged(); }
        }

        public string NumeroOperacion
        {
            get => _numeroOperacion;
            set { _numeroOperacion = value ?? string.Empty; OnPropertyChanged(); }
        }

        public string Observacion
        {
            get => _observacion;
            set { _observacion = value ?? string.Empty; OnPropertyChanged(); }
        }

        public bool Guardando
        {
            get => _guardando;
            set { _guardando = value; OnPropertyChanged(); }
        }

        private void CargarBancos()
        {
            try
            {
                Bancos.Clear();
                Bancos.Add(new BancoPagoItem(null, "Sin banco"));

                foreach (BancoTesoreria banco in _negocio.ListarBancos().OrderBy(b => b.Nombre))
                    Bancos.Add(new BancoPagoItem(banco.IdBanco, banco.BancoBusqueda));

                BancoSeleccionado = Bancos.FirstOrDefault();
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudieron cargar los bancos: {ex.Message}");
            }
        }

        private void CargarCuentas()
        {
            try
            {
                CuentasBancarias.Clear();
                CuentasBancarias.Add(new CuentaBancariaPagoItem(null, "Sin cuenta bancaria"));

                int? idBanco = BancoSeleccionado?.IdBanco;
                foreach (CuentaBancariaTesoreria cuenta in _negocio.ListarCuentasBancarias(idBanco)
                    .Where(c => c.Moneda.Equals(Moneda, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(c => c.Banco)
                    .ThenBy(c => c.NombreCuenta))
                {
                    CuentasBancarias.Add(new CuentaBancariaPagoItem(cuenta.IdCuentaBancaria, cuenta.CuentaBusqueda));
                }

                CuentaSeleccionada = CuentasBancarias.FirstOrDefault();
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudieron cargar las cuentas bancarias: {ex.Message}");
            }
        }

        private void Registrar()
        {
            string validacion = Validar();
            if (!string.IsNullOrWhiteSpace(validacion))
            {
                NotificationService.Warning(validacion);
                return;
            }

            try
            {
                Guardando = true;
                string usuario = SessionManager.UsuarioActual?.NombreCompleto
                    ?? SessionManager.UsuarioActual?.NombreUsuario
                    ?? "Sistema";

                ObligacionFinancieraPago pago = new()
                {
                    IdCuota = _cuota.IdCuotaObligacionFinanciera,
                    FechaPago = FechaPago,
                    Monto = Monto,
                    IdBanco = BancoSeleccionado?.IdBanco,
                    IdCuentaBancaria = CuentaSeleccionada?.IdCuentaBancaria,
                    NumeroOperacion = NumeroOperacion,
                    Observacion = Observacion
                };

                ObligacionFinancieraPagoResultado resultado = _negocio.RegistrarPago(pago, usuario);
                if (resultado.Resultado)
                {
                    NotificationService.Success(resultado.Mensaje);
                    CerrarVentana?.Invoke(true);
                }
                else
                {
                    NotificationService.Warning(resultado.Mensaje);
                }
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudo registrar el pago: {ex.Message}");
            }
            finally
            {
                Guardando = false;
            }
        }

        private string Validar()
        {
            if (_cuota.IdCuotaObligacionFinanciera <= 0)
                return "Debe seleccionar una cuota valida.";

            if (FechaPago == default)
                return "Debe ingresar la fecha de pago.";

            if (Monto <= 0)
                return "El monto del pago debe ser mayor a cero.";

            if (Monto > _cuota.SaldoPendiente)
                return $"El monto del pago no puede superar el saldo pendiente de {Moneda} {_cuota.SaldoPendiente.ToString("N2", _cultura)}.";

            return string.Empty;
        }
    }
}
