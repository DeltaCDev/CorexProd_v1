using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Almacen.ViewModels
{
    public class IngresoManualStockInsumoEditorViewModel : BaseViewModel
    {
        private const string ApiUrlPredeterminada = "https://ruc.com.pe/api/v1/consultas";
        private const string ApiTokenPredeterminado = "0a682fbe-009d-4758-aad1-2ff1092ab7c2-838d6cd5-620a-4add-9632-a3b37c5ae216";

        private static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        private readonly IngresoManualStockInsumoNegocio _negocio = new();
        private readonly IngresoManualStockInsumo? _ingresoOriginal;
        private DateTime? _fechaEmision = DateTime.Today;
        private ProveedorStock? _proveedorSeleccionado;
        private TipoDocumentoStock? _tipoDocumentoSeleccionado;
        private string _tipoNumeracion = "Automatica";
        private string _serie = string.Empty;
        private string _serieAutomatica = string.Empty;
        private string _numero = "Automatico";
        private AlmacenStock? _almacenSeleccionado;
        private string _observacion = string.Empty;
        private bool _isSaving;
        private bool _isConsultandoProveedor;
        private Visibility _proveedorRapidoVisibility = Visibility.Collapsed;

        public IngresoManualStockInsumoEditorViewModel(IngresoManualStockInsumo? ingreso)
        {
            _ingresoOriginal = ingreso;
            Titulo = ingreso == null ? "Nuevo Ingreso Manual de Stock de Insumos" : "Editar Ingreso Manual de Stock de Insumos";

            GuardarCommand = new RelayCommand(_ => Guardar(), _ => !IsSaving);
            CancelarCommand = new RelayCommand(_ => CerrarVentana?.Invoke());
            AgregarInsumoCommand = new RelayCommand(_ => AgregarInsumo());
            QuitarInsumoCommand = new RelayCommand(parametro => QuitarInsumo(parametro));
            MostrarProveedorRapidoCommand = new RelayCommand(_ => ProveedorRapidoVisibility = Visibility.Visible);
            CancelarProveedorRapidoCommand = new RelayCommand(_ => LimpiarProveedorRapido());
            GuardarProveedorRapidoCommand = new RelayCommand(_ => GuardarProveedorRapido());
            ConsultarProveedorDocumentoCommand = new RelayCommand(
                async _ => await ConsultarProveedorDocumentoAsync(),
                _ => !IsConsultandoProveedor);

            if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                _serieAutomatica = new SerieCorrelativoNegocio().Listar("INGRESO_INSUMOS")
                    .FirstOrDefault(s => s.Activa && s.Predeterminada)?.Serie ?? string.Empty;
                if (ingreso == null) Serie = _serieAutomatica;
                CargarCombos();
                CargarIngreso(ingreso);
            }
        }

        public string Titulo { get; }
        public bool Guardado { get; private set; }
        public Action? CerrarVentana { get; set; }

        public ObservableCollection<ProveedorStock> Proveedores { get; } = [];
        public ObservableCollection<AlmacenStock> Almacenes { get; } = [];
        public ObservableCollection<TipoDocumentoStock> TiposDocumento { get; } = [];
        public ObservableCollection<string> TiposNumeracion { get; } = ["Automatica", "Manual"];
        public ObservableCollection<IngresoManualStockInsumoDetalleViewModel> Detalles { get; } = [];

        public DateTime? FechaEmision
        {
            get => _fechaEmision;
            set { _fechaEmision = value; OnPropertyChanged(); }
        }

        public ProveedorStock? ProveedorSeleccionado
        {
            get => _proveedorSeleccionado;
            set { _proveedorSeleccionado = value; OnPropertyChanged(); }
        }

        public TipoDocumentoStock? TipoDocumentoSeleccionado
        {
            get => _tipoDocumentoSeleccionado;
            set { _tipoDocumentoSeleccionado = value; OnPropertyChanged(); }
        }

        public string TipoNumeracion
        {
            get => _tipoNumeracion;
            set
            {
                _tipoNumeracion = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SerieNumeroReadOnly));
                if (SerieNumeroReadOnly)
                {
                    Serie = string.IsNullOrWhiteSpace(Serie) ? _serieAutomatica : Serie;
                    Numero = _ingresoOriginal?.Numero ?? "Automatico";
                }
            }
        }

        public bool SerieNumeroReadOnly => TipoNumeracion.Equals("Automatica", StringComparison.OrdinalIgnoreCase);

        public string Serie
        {
            get => _serie;
            set { _serie = value; OnPropertyChanged(); }
        }

        public string Numero
        {
            get => _numero;
            set { _numero = value; OnPropertyChanged(); }
        }

        public AlmacenStock? AlmacenSeleccionado
        {
            get => _almacenSeleccionado;
            set
            {
                if (_almacenSeleccionado != null && value != null && _almacenSeleccionado.IdAlmacen != value.IdAlmacen && Detalles.Count > 0)
                {
                    bool confirmar = ConfirmDialogService.Confirmar(
                        "Cambiar el almacen actualizara el stock mostrado en los insumos. Â¿Desea continuar?",
                        "Cambiar almacen");

                    if (!confirmar)
                    {
                        OnPropertyChanged();
                        return;
                    }
                }

                _almacenSeleccionado = value;
                OnPropertyChanged();
                ActualizarStockDetalles();
            }
        }

        public string Observacion
        {
            get => _observacion;
            set { _observacion = value; OnPropertyChanged(); }
        }

        public decimal Subtotal => Detalles.Sum(d => d.Cantidad * d.PrecioUnitario);
        public decimal DescuentoTotal => Detalles.Sum(d => d.Descuento);
        public decimal Total => Detalles.Sum(d => d.Importe);

        public bool IsSaving
        {
            get => _isSaving;
            set { _isSaving = value; OnPropertyChanged(); }
        }

        public bool IsConsultandoProveedor
        {
            get => _isConsultandoProveedor;
            set
            {
                _isConsultandoProveedor = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public Visibility ProveedorRapidoVisibility
        {
            get => _proveedorRapidoVisibility;
            set { _proveedorRapidoVisibility = value; OnPropertyChanged(); }
        }

        public string NuevoProveedorTipoDocumento { get; set; } = "RUC";
        public string NuevoProveedorNumeroDocumento { get; set; } = string.Empty;
        public string NuevoProveedorNombre { get; set; } = string.Empty;
        public string NuevoProveedorTelefono { get; set; } = string.Empty;
        public string NuevoProveedorCorreo { get; set; } = string.Empty;
        public string NuevoProveedorDireccion { get; set; } = string.Empty;

        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }
        public ICommand AgregarInsumoCommand { get; }
        public ICommand QuitarInsumoCommand { get; }
        public ICommand MostrarProveedorRapidoCommand { get; }
        public ICommand CancelarProveedorRapidoCommand { get; }
        public ICommand GuardarProveedorRapidoCommand { get; }
        public ICommand ConsultarProveedorDocumentoCommand { get; }

        private void CargarCombos()
        {
            Proveedores.Clear();
            foreach (ProveedorStock proveedor in _negocio.ListarProveedores())
            {
                Proveedores.Add(proveedor);
            }

            Almacenes.Clear();
            foreach (AlmacenStock almacen in _negocio.ListarAlmacenes())
            {
                Almacenes.Add(almacen);
            }

            TiposDocumento.Clear();
            foreach (TipoDocumentoStock tipo in _negocio.ListarTiposDocumento())
            {
                TiposDocumento.Add(tipo);
            }

            ProveedorSeleccionado ??= Proveedores.FirstOrDefault();
            AlmacenSeleccionado ??= Almacenes.FirstOrDefault();
            TipoDocumentoSeleccionado ??= TiposDocumento.FirstOrDefault();
        }

        private void CargarIngreso(IngresoManualStockInsumo? ingreso)
        {
            if (ingreso == null)
            {
                AgregarInsumo();
                return;
            }

            FechaEmision = ingreso.FechaEmision;
            ProveedorSeleccionado = Proveedores.FirstOrDefault(p => p.IdProveedor == ingreso.IdProveedor);
            TipoDocumentoSeleccionado = TiposDocumento.FirstOrDefault(t => t.IdTipoDocumento == ingreso.IdTipoDocumento);
            TipoNumeracion = ingreso.TipoNumeracion;
            Serie = ingreso.Serie;
            Numero = ingreso.Numero;
            AlmacenSeleccionado = Almacenes.FirstOrDefault(a => a.IdAlmacen == ingreso.IdAlmacen);
            Observacion = ingreso.Observacion;

            Detalles.Clear();
            foreach (IngresoManualStockInsumoDetalle detalle in ingreso.Detalles)
            {
                Detalles.Add(IngresoManualStockInsumoDetalleViewModel.FromEntity(detalle, RecalcularTotales, BuscarInsumos));
            }

            RecalcularTotales();
        }

        private void AgregarInsumo()
        {
            Detalles.Add(new IngresoManualStockInsumoDetalleViewModel(RecalcularTotales, BuscarInsumos));
            RecalcularTotales();
        }

        private void QuitarInsumo(object? parametro)
        {
            if (parametro is IngresoManualStockInsumoDetalleViewModel detalle)
            {
                Detalles.Remove(detalle);
                RecalcularTotales();
            }
        }

        private ObservableCollection<InsumoStockBusqueda> BuscarInsumos(string texto)
        {
            ObservableCollection<InsumoStockBusqueda> insumos = [];
            int idAlmacen = AlmacenSeleccionado?.IdAlmacen ?? 0;

            foreach (InsumoStockBusqueda insumo in _negocio.BuscarInsumos(idAlmacen, texto))
            {
                if (!Detalles.Any(d => d.IdInsumo == insumo.IdInsumo))
                {
                    insumos.Add(insumo);
                }
            }

            return insumos;
        }

        private void ActualizarStockDetalles()
        {
            if (AlmacenSeleccionado == null)
            {
                return;
            }

            foreach (IngresoManualStockInsumoDetalleViewModel detalle in Detalles.Where(d => d.IdInsumo > 0))
            {
                InsumoStockBusqueda? insumo = _negocio.BuscarInsumos(AlmacenSeleccionado.IdAlmacen, detalle.CodigoInsumo)
                    .FirstOrDefault(p => p.IdInsumo == detalle.IdInsumo);

                if (insumo != null)
                {
                    detalle.ActualizarStock(insumo.StockActual);
                }
            }
        }

        private void RecalcularTotales()
        {
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(DescuentoTotal));
            OnPropertyChanged(nameof(Total));
        }

        private void GuardarProveedorRapido()
        {
            ProveedorStock proveedor = new()
            {
                TipoDocumento = NuevoProveedorTipoDocumento,
                NumeroDocumento = NuevoProveedorNumeroDocumento,
                NombreRazonSocial = NuevoProveedorNombre,
                Telefono = NuevoProveedorTelefono,
                Correo = NuevoProveedorCorreo,
                Direccion = NuevoProveedorDireccion
            };

            int idProveedor = _negocio.RegistrarProveedorRapido(proveedor, out string mensaje);

            if (idProveedor <= 0)
            {
                NotificationService.Warning(mensaje);
                return;
            }

            CargarCombos();
            ProveedorSeleccionado = Proveedores.FirstOrDefault(p => p.IdProveedor == idProveedor);
            NotificationService.Success(mensaje);
            LimpiarProveedorRapido();
        }

        private async Task ConsultarProveedorDocumentoAsync()
        {
            string tipoDocumento = NuevoProveedorTipoDocumento.Trim().ToUpperInvariant();
            string numeroDocumento = NuevoProveedorNumeroDocumento.Trim();

            if (tipoDocumento != "DNI" && tipoDocumento != "RUC")
            {
                NotificationService.Warning("La consulta solo esta disponible para DNI y RUC");
                return;
            }

            int longitudEsperada = tipoDocumento == "DNI" ? 8 : 11;

            if (numeroDocumento.Length != longitudEsperada || !numeroDocumento.All(char.IsDigit))
            {
                NotificationService.Warning($"Ingrese un {tipoDocumento} valido de {longitudEsperada} digitos");
                return;
            }

            try
            {
                IsConsultandoProveedor = true;

                using JsonDocument respuesta = await ConsultarApiDocumentoAsync(tipoDocumento, numeroDocumento);
                JsonElement raiz = respuesta.RootElement;

                if (!ObtenerBooleano(raiz, "success"))
                {
                    NotificationService.Warning(ObtenerTexto(raiz, "message", "mensaje", "error") ?? "No se encontro informacion para el documento");
                    return;
                }

                string? nombre = ObtenerTexto(raiz, "nombre_completo", "nombre_o_razon_social", "razon_social", "nombre");
                string? direccion = ObtenerTexto(raiz, "direccion", "domicilio_fiscal");

                if (string.IsNullOrWhiteSpace(nombre))
                {
                    NotificationService.Warning("La API respondio correctamente, pero no envio nombre o razon social");
                    return;
                }

                NuevoProveedorNombre = nombre;
                OnPropertyChanged(nameof(NuevoProveedorNombre));

                if (!string.IsNullOrWhiteSpace(direccion))
                {
                    NuevoProveedorDireccion = direccion;
                    OnPropertyChanged(nameof(NuevoProveedorDireccion));
                }

                NotificationService.Success("Datos consultados correctamente");
            }
            catch (TaskCanceledException)
            {
                NotificationService.Error("La consulta demoro demasiado. Intente nuevamente");
            }
            catch (HttpRequestException)
            {
                NotificationService.Error("No se pudo conectar con la API de consulta");
            }
            catch (JsonException)
            {
                NotificationService.Error("La API devolvio una respuesta no valida");
            }
            finally
            {
                IsConsultandoProveedor = false;
            }
        }

        private static async Task<JsonDocument> ConsultarApiDocumentoAsync(string tipoDocumento, string numeroDocumento)
        {
            string apiUrl = ConfigurationManager.AppSettings["RucComPeApiUrl"] ?? ApiUrlPredeterminada;
            string token = ConfigurationManager.AppSettings["RucComPeApiToken"] ?? ApiTokenPredeterminado;
            string campoDocumento = tipoDocumento == "RUC" ? "ruc" : "dni";

            var payload = new Dictionary<string, string>
            {
                ["token"] = token,
                [campoDocumento] = numeroDocumento
            };

            string json = JsonSerializer.Serialize(payload);
            using StringContent contenido = new(json, Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await HttpClient.PostAsync(apiUrl, contenido);

            response.EnsureSuccessStatusCode();

            await using Stream stream = await response.Content.ReadAsStreamAsync();
            return await JsonDocument.ParseAsync(stream);
        }

        private static bool ObtenerBooleano(JsonElement elemento, string propiedad)
        {
            if (!elemento.TryGetProperty(propiedad, out JsonElement valor))
            {
                return false;
            }

            return valor.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String => bool.TryParse(valor.GetString(), out bool resultado) && resultado,
                _ => false
            };
        }

        private static string? ObtenerTexto(JsonElement elemento, params string[] propiedades)
        {
            foreach (string propiedad in propiedades)
            {
                if (elemento.TryGetProperty(propiedad, out JsonElement valor) && valor.ValueKind == JsonValueKind.String)
                {
                    return valor.GetString();
                }
            }

            return null;
        }

        private void LimpiarProveedorRapido()
        {
            ProveedorRapidoVisibility = Visibility.Collapsed;
            NuevoProveedorTipoDocumento = "RUC";
            NuevoProveedorNumeroDocumento = string.Empty;
            NuevoProveedorNombre = string.Empty;
            NuevoProveedorTelefono = string.Empty;
            NuevoProveedorCorreo = string.Empty;
            NuevoProveedorDireccion = string.Empty;
            OnPropertyChanged(nameof(NuevoProveedorTipoDocumento));
            OnPropertyChanged(nameof(NuevoProveedorNumeroDocumento));
            OnPropertyChanged(nameof(NuevoProveedorNombre));
            OnPropertyChanged(nameof(NuevoProveedorTelefono));
            OnPropertyChanged(nameof(NuevoProveedorCorreo));
            OnPropertyChanged(nameof(NuevoProveedorDireccion));
        }

        private void Guardar()
        {
            if (IsSaving)
            {
                return;
            }

            foreach (IngresoManualStockInsumoDetalleViewModel detalle in Detalles.Where(d => d.IdInsumo == 0).ToList())
            {
                Detalles.Remove(detalle);
            }

            IngresoManualStockInsumo ingreso = new()
            {
                IdIngresoManualStockInsumo = _ingresoOriginal?.IdIngresoManualStockInsumo ?? 0,
                FechaEmision = FechaEmision ?? DateTime.Today,
                IdProveedor = ProveedorSeleccionado?.IdProveedor ?? 0,
                IdTipoDocumento = TipoDocumentoSeleccionado?.IdTipoDocumento ?? 0,
                TipoNumeracion = TipoNumeracion,
                Serie = Serie,
                Numero = SerieNumeroReadOnly ? string.Empty : Numero,
                IdAlmacen = AlmacenSeleccionado?.IdAlmacen ?? 0,
                Observacion = Observacion,
                Detalles = Detalles.Select(d => d.ToEntity()).ToList()
            };

            if (ingreso.Detalles.GroupBy(d => d.IdInsumo).Any(g => g.Count() > 1))
            {
                NotificationService.Warning("No se permiten insumos repetidos");
                return;
            }

            try
            {
                IsSaving = true;
                string usuario = SessionManager.UsuarioActual?.NombreUsuario ?? "Sistema";
                string mensaje = _negocio.Guardar(ingreso, usuario);

                if (mensaje.Contains("correctamente", StringComparison.OrdinalIgnoreCase))
                {
                    Guardado = true;
                    NotificationService.Success(mensaje);
                    CerrarVentana?.Invoke();
                }
                else
                {
                    NotificationService.Warning(mensaje);
                }
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudo guardar el ingreso manual: {ex.Message}");
            }
            finally
            {
                IsSaving = false;
            }
        }
    }
}




