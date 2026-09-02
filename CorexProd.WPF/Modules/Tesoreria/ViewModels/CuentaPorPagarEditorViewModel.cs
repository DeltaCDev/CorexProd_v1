using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Tesoreria.ViewModels
{
    public class CuentaPorPagarEditorViewModel : BaseViewModel
    {
        private readonly CuentaPorPagarNegocio _negocio = new();
        private readonly ProveedorNegocio _proveedorNegocio = new();
        private ProveedorStock? _proveedorSeleccionado;
        private TipoObligacion? _tipoObligacionSeleccionado;
        private DateTime? _fechaDocumento = DateTime.Today;
        private string _moneda = "PEN";
        private decimal _importeTotal;
        private string _observacion = string.Empty;
        private bool _isSaving;

        public CuentaPorPagarEditorViewModel()
        {
            GuardarCommand = new RelayCommand(_ => Guardar(), _ => !IsSaving);
            CancelarCommand = new RelayCommand(_ => CerrarVentana?.Invoke(false));
            AgregarDocumentoCommand = new RelayCommand(_ => AgregarDocumento());
            QuitarDocumentoCommand = new RelayCommand(parametro => QuitarDocumento(parametro));
            AgregarCuotaCommand = new RelayCommand(_ => AgregarCuota());
            QuitarCuotaCommand = new RelayCommand(parametro => QuitarCuota(parametro));

            CargarCombos();
            AgregarDocumento();
            AgregarCuota();
        }

        public string Titulo => "Nueva Cuenta por Pagar";
        public bool Guardado { get; private set; }
        public Action<bool>? CerrarVentana { get; set; }
        public ObservableCollection<ProveedorStock> Proveedores { get; } = [];
        public ObservableCollection<TipoObligacion> TiposObligacion { get; } = [];
        public ObservableCollection<TipoDocumentoStock> TiposDocumento { get; } = [];
        public ObservableCollection<string> Monedas { get; } = ["PEN", "USD"];
        public ObservableCollection<CuentaPorPagarDocumentoItemViewModel> Documentos { get; } = [];
        public ObservableCollection<CuentaPorPagarCuotaItemViewModel> Cuotas { get; } = [];

        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }
        public ICommand AgregarDocumentoCommand { get; }
        public ICommand QuitarDocumentoCommand { get; }
        public ICommand AgregarCuotaCommand { get; }
        public ICommand QuitarCuotaCommand { get; }

        public ProveedorStock? ProveedorSeleccionado
        {
            get => _proveedorSeleccionado;
            set { _proveedorSeleccionado = value; OnPropertyChanged(); }
        }

        public TipoObligacion? TipoObligacionSeleccionado
        {
            get => _tipoObligacionSeleccionado;
            set
            {
                _tipoObligacionSeleccionado = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EsFacturaCredito));
                OnPropertyChanged(nameof(EsLetrasPorPagar));
                OnPropertyChanged(nameof(TituloCuotas));
                SincronizarTipoCuotas();
            }
        }

        public DateTime? FechaDocumento
        {
            get => _fechaDocumento;
            set { _fechaDocumento = value; OnPropertyChanged(); }
        }

        public string Moneda
        {
            get => _moneda;
            set
            {
                _moneda = string.IsNullOrWhiteSpace(value) ? "PEN" : value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SimboloMoneda));
                NotificarTotales();
            }
        }

        public decimal ImporteTotal
        {
            get => _importeTotal;
            set
            {
                _importeTotal = value;
                OnPropertyChanged();
                NotificarTotales();
            }
        }

        public string Observacion
        {
            get => _observacion;
            set { _observacion = value ?? string.Empty; OnPropertyChanged(); }
        }

        public decimal TotalDocumentos => Math.Round(Documentos.Sum(d => d.Importe), 2);
        public decimal TotalFacturas => Math.Round(Documentos.Where(d => d.FactorEfecto == 1).Sum(d => d.Importe), 2);
        public decimal TotalNotasCredito => Math.Round(Documentos.Where(d => d.FactorEfecto == -1).Sum(d => d.Importe), 2);
        public decimal TotalNetoPorPagar => Math.Round(TotalFacturas - TotalNotasCredito, 2);
        public decimal TotalCuotas => Math.Round(Cuotas.Sum(c => c.Importe), 2);
        public decimal DiferenciaCuotas => Math.Round(ImporteTotal - TotalCuotas, 2);
        public decimal DiferenciaDocumentos => Math.Round(ImporteTotal - TotalNetoPorPagar, 2);
        public bool EsFacturaCredito => TipoObligacionSeleccionado?.Codigo.Equals("FACTURA_CREDITO", StringComparison.OrdinalIgnoreCase) == true;
        public bool EsLetrasPorPagar => !EsFacturaCredito;
        public string TituloCuotas => EsFacturaCredito ? "CUOTAS DE FACTURA" : "LETRAS / CUOTAS";
        public string SimboloMoneda => Moneda.Trim().ToUpperInvariant() switch
        {
            "USD" => "US$",
            "EUR" => "EUR",
            _ => "S/"
        };
        public string ImporteTotalTexto => FormatearMoneda(ImporteTotal);
        public string TotalFacturasTexto => FormatearMoneda(TotalFacturas);
        public string TotalNotasCreditoTexto => FormatearMoneda(TotalNotasCredito);
        public string TotalNetoPorPagarTexto => FormatearMoneda(TotalNetoPorPagar);
        public string TotalCuotasTexto => FormatearMoneda(TotalCuotas);
        public string DiferenciaCuotasTexto => FormatearMoneda(DiferenciaCuotas);

        public bool IsSaving
        {
            get => _isSaving;
            set
            {
                _isSaving = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private void CargarCombos()
        {
            Proveedores.Clear();
            foreach (ProveedorStock proveedor in _proveedorNegocio.Listar())
                Proveedores.Add(proveedor);

            TiposObligacion.Clear();
            foreach (TipoObligacion tipo in _negocio.ListarTiposObligacion())
                TiposObligacion.Add(tipo);

            TiposDocumento.Clear();
            foreach (TipoDocumentoStock tipo in _negocio.ListarTiposDocumento())
                TiposDocumento.Add(tipo);

            ProveedorSeleccionado = Proveedores.FirstOrDefault();
            TipoObligacionSeleccionado = TiposObligacion.FirstOrDefault(t => t.Codigo.Equals("LETRA", StringComparison.OrdinalIgnoreCase))
                ?? TiposObligacion.FirstOrDefault();
        }

        private void AgregarDocumento()
        {
            CuentaPorPagarDocumentoItemViewModel documento = new(NotificarTotales)
            {
                FechaEmision = FechaDocumento ?? DateTime.Today,
                TipoDocumentoSeleccionado = TiposDocumento.FirstOrDefault(t => t.NombreTipoDocumento.Equals("Factura", StringComparison.OrdinalIgnoreCase))
                    ?? TiposDocumento.FirstOrDefault()
            };
            Documentos.Add(documento);
            NotificarTotales();
        }

        private void QuitarDocumento(object? parametro)
        {
            if (parametro is CuentaPorPagarDocumentoItemViewModel documento)
            {
                Documentos.Remove(documento);
                NotificarTotales();
            }
        }

        private void AgregarCuota()
        {
            int numero = Cuotas.Count + 1;
            CuentaPorPagarCuotaItemViewModel cuota = new(NotificarTotales)
            {
                NumeroCuota = numero,
                TotalCuotas = Math.Max(numero, Cuotas.Count + 1),
                FechaGiro = FechaDocumento ?? DateTime.Today,
                FechaVencimiento = FechaDocumento ?? DateTime.Today
            };
            Cuotas.Add(cuota);
            ActualizarTotalCuotasFormulario();
            NotificarTotales();
        }

        private void QuitarCuota(object? parametro)
        {
            if (parametro is CuentaPorPagarCuotaItemViewModel cuota)
            {
                Cuotas.Remove(cuota);
                RenumerarCuotas();
                NotificarTotales();
            }
        }

        private void RenumerarCuotas()
        {
            int total = Cuotas.Count;
            for (int i = 0; i < Cuotas.Count; i++)
            {
                Cuotas[i].NumeroCuota = i + 1;
                Cuotas[i].TotalCuotas = total;
            }
        }

        private void ActualizarTotalCuotasFormulario()
        {
            int total = Cuotas.Count;
            foreach (CuentaPorPagarCuotaItemViewModel cuota in Cuotas)
                cuota.TotalCuotas = total;
        }

        private void Guardar()
        {
            string validacion = ValidarFormulario();
            if (!string.IsNullOrWhiteSpace(validacion))
            {
                NotificationService.Warning(validacion);
                return;
            }

            CuentaPorPagar cuenta = new()
            {
                IdProveedor = ProveedorSeleccionado?.IdProveedor ?? 0,
                IdTipoObligacion = TipoObligacionSeleccionado?.IdTipoObligacion ?? 0,
                CodigoTipoObligacion = TipoObligacionSeleccionado?.Codigo ?? string.Empty,
                TipoObligacion = TipoObligacionSeleccionado?.Nombre ?? string.Empty,
                FechaDocumento = FechaDocumento ?? DateTime.Today,
                Moneda = Moneda,
                ImporteTotal = TotalNetoPorPagar,
                OrigenTipo = "MANUAL",
                Observacion = Observacion,
                Documentos = Documentos.Select(d => d.ToEntity()).ToList(),
                Cuotas = Cuotas.Select(c => c.ToEntity()).ToList()
            };

            try
            {
                IsSaving = true;
                string usuario = SessionManager.UsuarioActual?.NombreCompleto
                    ?? SessionManager.UsuarioActual?.NombreUsuario
                    ?? "Sistema";
                CuentaPorPagarResultado resultado = _negocio.Guardar(cuenta, usuario);

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
                NotificationService.Error($"No se pudo guardar la cuenta por pagar: {ex.Message}");
            }
            finally
            {
                IsSaving = false;
            }
        }

        private string ValidarFormulario()
        {
            if (ProveedorSeleccionado == null)
                return "Debe seleccionar un proveedor.";

            if (TipoObligacionSeleccionado == null)
                return "Debe seleccionar un tipo de obligacion.";

            if (FechaDocumento == null)
                return "Debe ingresar una fecha de documento valida.";

            if (Moneda is not ("PEN" or "USD"))
                return "Debe seleccionar una moneda valida.";

            if (ImporteTotal <= 0)
                return "El total neto por pagar debe ser mayor a cero.";

            if (Documentos.Count == 0)
                return "Debe agregar al menos un documento.";

            if (Cuotas.Count == 0)
                return "Debe agregar al menos una letra o cuota.";

            if (Documentos.Any(d => d.TipoDocumentoSeleccionado == null || d.FechaEmision == null || d.Importe <= 0))
                return "Cada documento debe tener tipo, fecha e importe mayor a cero.";

            if (TotalFacturas <= 0)
                return "Debe registrar al menos una factura o documento positivo.";

            if (TotalNotasCredito > TotalFacturas)
                return "El total de notas de credito no puede ser mayor al total de facturas.";

            if (TotalNetoPorPagar <= 0)
                return "El total neto por pagar debe ser mayor a cero.";

            if (Cuotas.Any(c => c.NumeroCuota <= 0 || c.TotalCuotas <= 0 || c.Importe <= 0))
                return "Cada cuota debe tener numeracion valida e importe mayor a cero.";

            if (EsLetrasPorPagar && Cuotas.Any(c => string.IsNullOrWhiteSpace(c.NumeroLetra)))
                return "El numero de letra es obligatorio para Letras por Pagar.";

            if (EsLetrasPorPagar && Cuotas.Any(c => c.FechaGiro == null))
                return "Cada letra debe tener fecha de giro.";

            if (Cuotas.Any(c => c.FechaVencimiento == null))
                return "Cada cuota debe tener fecha de giro y vencimiento.";

            if (Cuotas.Any(c => c.FechaGiro.HasValue && c.FechaVencimiento!.Value.Date < c.FechaGiro.Value.Date))
                return "La fecha de vencimiento no puede ser anterior a la fecha de giro.";

            if (EsLetrasPorPagar
                && Cuotas.Where(c => !string.IsNullOrWhiteSpace(c.NumeroLetra))
                    .GroupBy(c => c.NumeroLetra.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Any(g => g.Count() > 1))
            {
                return "No se puede repetir el numero de letra dentro de la misma cuenta.";
            }

            if (Math.Abs(DiferenciaCuotas) > 0.01m)
                return "La suma de cuotas debe ser igual al importe total.";

            return string.Empty;
        }

        private void NotificarTotales()
        {
            _importeTotal = TotalNetoPorPagar;
            OnPropertyChanged(nameof(ImporteTotal));
            OnPropertyChanged(nameof(ImporteTotalTexto));
            OnPropertyChanged(nameof(TotalDocumentos));
            OnPropertyChanged(nameof(TotalFacturas));
            OnPropertyChanged(nameof(TotalFacturasTexto));
            OnPropertyChanged(nameof(TotalNotasCredito));
            OnPropertyChanged(nameof(TotalNotasCreditoTexto));
            OnPropertyChanged(nameof(TotalNetoPorPagar));
            OnPropertyChanged(nameof(TotalNetoPorPagarTexto));
            OnPropertyChanged(nameof(TotalCuotas));
            OnPropertyChanged(nameof(TotalCuotasTexto));
            OnPropertyChanged(nameof(DiferenciaCuotas));
            OnPropertyChanged(nameof(DiferenciaCuotasTexto));
            OnPropertyChanged(nameof(DiferenciaDocumentos));
        }

        private string FormatearMoneda(decimal valor) => $"{SimboloMoneda} {valor:N2}";

        private void SincronizarTipoCuotas()
        {
            foreach (CuentaPorPagarCuotaItemViewModel cuota in Cuotas)
            {
                cuota.TipoCuota = EsFacturaCredito ? "CUOTA_FACTURA" : "LETRA";
                if (EsFacturaCredito && cuota.FechaGiro == null)
                    cuota.FechaGiro = cuota.FechaVencimiento;
            }
        }
    }

    public class CuentaPorPagarDocumentoItemViewModel : INotifyPropertyChanged
    {
        private readonly Action _totalesCambiaron;
        private TipoDocumentoStock? _tipoDocumentoSeleccionado;
        private string _serie = string.Empty;
        private string _numero = string.Empty;
        private DateTime? _fechaEmision = DateTime.Today;
        private decimal _importe;
        private string _observacion = string.Empty;

        public CuentaPorPagarDocumentoItemViewModel(Action totalesCambiaron)
        {
            _totalesCambiaron = totalesCambiaron;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public TipoDocumentoStock? TipoDocumentoSeleccionado
        {
            get => _tipoDocumentoSeleccionado;
            set
            {
                _tipoDocumentoSeleccionado = value;
                FactorEfecto = EsNotaCredito(value) ? (short)-1 : (short)1;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EfectoTexto));
                OnPropertyChanged(nameof(ImporteConEfectoTexto));
                _totalesCambiaron();
            }
        }
        public string Serie { get => _serie; set { _serie = value ?? string.Empty; OnPropertyChanged(); OnPropertyChanged(nameof(NumeroDocumento)); } }
        public string Numero { get => _numero; set { _numero = value ?? string.Empty; OnPropertyChanged(); OnPropertyChanged(nameof(NumeroDocumento)); } }
        public string NumeroDocumento => string.IsNullOrWhiteSpace(Serie) ? Numero : $"{Serie}-{Numero}";
        public DateTime? FechaEmision { get => _fechaEmision; set { _fechaEmision = value; OnPropertyChanged(); } }
        public decimal Importe { get => _importe; set { _importe = value; OnPropertyChanged(); OnPropertyChanged(nameof(ImporteConEfectoTexto)); _totalesCambiaron(); } }
        public string Observacion { get => _observacion; set { _observacion = value ?? string.Empty; OnPropertyChanged(); } }
        public short FactorEfecto { get; private set; } = 1;
        public string EfectoTexto => FactorEfecto < 0 ? $"- {Importe:N2}" : $"+ {Importe:N2}";
        public string ImporteConEfectoTexto => EfectoTexto;

        public CuentaPorPagarDocumento ToEntity() => new()
        {
            IdTipoDocumento = TipoDocumentoSeleccionado?.IdTipoDocumento ?? 0,
            Serie = Serie,
            Numero = Numero,
            NumeroDocumento = NumeroDocumento,
            FechaEmision = FechaEmision ?? DateTime.Today,
            Importe = Importe,
            FactorEfecto = FactorEfecto,
            Observacion = Observacion
        };

        private static bool EsNotaCredito(TipoDocumentoStock? tipo)
        {
            string nombre = tipo?.NombreTipoDocumento?.Trim().ToUpperInvariant() ?? string.Empty;
            return nombre.Contains("NOTA DE CREDITO") || nombre.Contains("NOTA DE CR");
        }

        private void OnPropertyChanged([CallerMemberName] string? propiedad = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propiedad));
        }
    }

    public class CuentaPorPagarCuotaItemViewModel : INotifyPropertyChanged
    {
        private readonly Action _totalesCambiaron;
        private string _numeroLetra = string.Empty;
        private string _tipoCuota = "LETRA";
        private int _numeroCuota;
        private int _totalCuotas;
        private DateTime? _fechaGiro = DateTime.Today;
        private DateTime? _fechaVencimiento = DateTime.Today;
        private decimal _importe;
        private string _observacion = string.Empty;

        public CuentaPorPagarCuotaItemViewModel(Action totalesCambiaron)
        {
            _totalesCambiaron = totalesCambiaron;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public string NumeroLetra { get => _numeroLetra; set { _numeroLetra = value ?? string.Empty; OnPropertyChanged(); } }
        public string TipoCuota { get => _tipoCuota; set { _tipoCuota = string.IsNullOrWhiteSpace(value) ? "LETRA" : value; OnPropertyChanged(); } }
        public int NumeroCuota { get => _numeroCuota; set { _numeroCuota = value; OnPropertyChanged(); } }
        public int TotalCuotas { get => _totalCuotas; set { _totalCuotas = value; OnPropertyChanged(); } }
        public DateTime? FechaGiro { get => _fechaGiro; set { _fechaGiro = value; OnPropertyChanged(); } }
        public DateTime? FechaVencimiento { get => _fechaVencimiento; set { _fechaVencimiento = value; OnPropertyChanged(); } }
        public decimal Importe { get => _importe; set { _importe = value; OnPropertyChanged(); _totalesCambiaron(); } }
        public string Observacion { get => _observacion; set { _observacion = value ?? string.Empty; OnPropertyChanged(); } }

        public CuentaPorPagarCuota ToEntity() => new()
        {
            NumeroCuota = NumeroCuota,
            TotalCuotas = TotalCuotas,
            NumeroLetra = NumeroLetra,
            TipoCuota = TipoCuota,
            FechaGiro = FechaGiro,
            FechaVencimiento = FechaVencimiento ?? DateTime.Today,
            Importe = Importe,
            Observacion = Observacion
        };

        private void OnPropertyChanged([CallerMemberName] string? propiedad = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propiedad));
        }
    }
}
