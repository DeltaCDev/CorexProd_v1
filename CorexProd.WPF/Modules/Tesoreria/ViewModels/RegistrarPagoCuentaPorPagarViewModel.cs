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
    public class RegistrarPagoCuentaPorPagarViewModel : BaseViewModel
    {
        private readonly CuentaPorPagarNegocio _negocio = new();
        private readonly CultureInfo _cultura = new("es-PE");
        private readonly CuentaPorPagarProgramacionItem _cuota;
        private BancoPagoItem? _bancoSeleccionado;
        private CuentaBancariaPagoItem? _cuentaSeleccionada;
        private DateTime _fechaPago = DateTime.Today;
        private decimal _importe;
        private string _numeroOperacion = string.Empty;
        private string _observacion = string.Empty;
        private bool _isSaving;

        public RegistrarPagoCuentaPorPagarViewModel(CuentaPorPagarProgramacionItem cuota)
        {
            _cuota = cuota;
            _importe = cuota.SaldoPendiente;

            RegistrarCommand = new RelayCommand(_ => Registrar(), _ => !IsSaving);
            CancelarCommand = new RelayCommand(_ => CerrarVentana?.Invoke(false));

            CargarBancos();
            CargarCuentas();
        }

        public ObservableCollection<BancoPagoItem> Bancos { get; } = [];
        public ObservableCollection<CuentaBancariaPagoItem> CuentasBancarias { get; } = [];
        public ICommand RegistrarCommand { get; }
        public ICommand CancelarCommand { get; }
        public Action<bool>? CerrarVentana { get; set; }
        public bool Guardado { get; private set; }

        public string Proveedor => _cuota.NombreProveedor;
        public string TipoObligacion => _cuota.TipoObligacion;
        public string NumeroLetra => _cuota.ReferenciaObligacion;
        public string CuotaTexto => _cuota.CuotaTexto;
        public DateTime FechaVencimiento => _cuota.FechaVencimiento;
        public string Moneda => _cuota.Moneda;
        public string ImporteCuotaTexto => _cuota.ImporteTexto;
        public string TotalPagadoTexto => _cuota.TotalPagadoTexto;
        public string SaldoPendienteTexto => _cuota.SaldoPendienteTexto;

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

        public decimal Importe
        {
            get => _importe;
            set { _importe = value; OnPropertyChanged(); }
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

        public bool IsSaving
        {
            get => _isSaving;
            set { _isSaving = value; OnPropertyChanged(); }
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
                IsSaving = true;
                string usuario = SessionManager.UsuarioActual?.NombreCompleto
                    ?? SessionManager.UsuarioActual?.NombreUsuario
                    ?? "Sistema";

                CuentaPorPagarPago pago = new()
                {
                    IdCuota = _cuota.IdCuota,
                    FechaPago = FechaPago,
                    Importe = Importe,
                    IdBanco = BancoSeleccionado?.IdBanco,
                    IdCuentaBancaria = CuentaSeleccionada?.IdCuentaBancaria,
                    NumeroOperacion = NumeroOperacion,
                    Observacion = Observacion
                };

                CuentaPorPagarPagoResultado resultado = _negocio.RegistrarPago(pago, usuario);
                if (resultado.Resultado)
                {
                    Guardado = true;
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
                IsSaving = false;
            }
        }

        private string Validar()
        {
            if (_cuota.IdCuota <= 0)
                return "Debe seleccionar una cuota valida.";

            if (FechaPago == default)
                return "Debe ingresar la fecha de pago.";

            if (Importe <= 0)
                return "El importe del pago debe ser mayor a cero.";

            if (Importe > _cuota.SaldoPendiente)
                return $"El importe del pago no puede superar el saldo pendiente de {_cuota.SimboloMoneda} {_cuota.SaldoPendiente.ToString("N2", _cultura)}.";

            if (CuentaSeleccionada?.IdCuentaBancaria <= 0)
                return "La cuenta bancaria seleccionada no es valida.";

            return string.Empty;
        }
    }

    public class BancoPagoItem
    {
        public BancoPagoItem(int? idBanco, string nombre)
        {
            IdBanco = idBanco;
            Nombre = nombre;
        }

        public int? IdBanco { get; }
        public string Nombre { get; }
    }

    public class CuentaBancariaPagoItem
    {
        public CuentaBancariaPagoItem(int? idCuentaBancaria, string nombre)
        {
            IdCuentaBancaria = idCuentaBancaria;
            Nombre = nombre;
        }

        public int? IdCuentaBancaria { get; }
        public string Nombre { get; }
    }
}
