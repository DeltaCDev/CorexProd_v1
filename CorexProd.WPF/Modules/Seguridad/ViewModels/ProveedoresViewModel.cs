using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.ViewModels;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Seguridad.ViewModels
{
    public class ProveedoresViewModel : BaseViewModel
    {
        private const string ApiUrlPredeterminada = "https://ruc.com.pe/api/v1/consultas";
        private const string ApiTokenPredeterminado = "0a682fbe-009d-4758-aad1-2ff1092ab7c2-838d6cd5-620a-4add-9632-a3b37c5ae216";

        private static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        private readonly ProveedorNegocio _proveedorNegocio = new();

        private int _idProveedor;
        private string _tipoDocumento = "RUC";
        private string _numeroDocumento = string.Empty;
        private string _nombreRazonSocial = string.Empty;
        private string _direccion = string.Empty;
        private string _telefono = string.Empty;
        private string _correo = string.Empty;
        private bool _estado = true;
        private bool _mostrarFormulario;
        private bool _isConsultandoDocumento;
        private ProveedorStock? _proveedorSeleccionado;

        public ObservableCollection<ProveedorStock> Proveedores { get; set; } = [];

        public int IdProveedor
        {
            get => _idProveedor;
            set
            {
                _idProveedor = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TituloFormulario));
            }
        }

        public string TipoDocumento
        {
            get => _tipoDocumento;
            set { _tipoDocumento = value; OnPropertyChanged(); }
        }

        public string NumeroDocumento
        {
            get => _numeroDocumento;
            set { _numeroDocumento = value; OnPropertyChanged(); }
        }

        public string NombreRazonSocial
        {
            get => _nombreRazonSocial;
            set { _nombreRazonSocial = value; OnPropertyChanged(); }
        }

        public string Direccion
        {
            get => _direccion;
            set { _direccion = value; OnPropertyChanged(); }
        }

        public string Telefono
        {
            get => _telefono;
            set { _telefono = value; OnPropertyChanged(); }
        }

        public string Correo
        {
            get => _correo;
            set { _correo = value; OnPropertyChanged(); }
        }

        public bool Estado
        {
            get => _estado;
            set { _estado = value; OnPropertyChanged(); }
        }

        public bool MostrarFormulario
        {
            get => _mostrarFormulario;
            set
            {
                _mostrarFormulario = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FormularioVisibility));
            }
        }

        public Visibility FormularioVisibility => MostrarFormulario ? Visibility.Visible : Visibility.Collapsed;

        public string TituloFormulario => IdProveedor > 0 ? "Editar proveedor" : "Nuevo proveedor";

        public bool IsConsultandoDocumento
        {
            get => _isConsultandoDocumento;
            set
            {
                _isConsultandoDocumento = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ProveedorStock? ProveedorSeleccionado
        {
            get => _proveedorSeleccionado;
            set { _proveedorSeleccionado = value; OnPropertyChanged(); }
        }

        public ICommand NuevoCommand { get; }
        public ICommand EditarCommand { get; }
        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }
        public ICommand EliminarCommand { get; }
        public ICommand ConsultarDocumentoCommand { get; }

        public ProveedoresViewModel()
        {
            NuevoCommand = new RelayCommand(_ => Nuevo());
            EditarCommand = new RelayCommand(parametro => Editar(parametro));
            GuardarCommand = new RelayCommand(_ => Guardar());
            CancelarCommand = new RelayCommand(_ => Cancelar());
            EliminarCommand = new RelayCommand(parametro => Eliminar(parametro));
            ConsultarDocumentoCommand = new RelayCommand(
                async _ => await ConsultarDocumentoAsync(),
                _ => !IsConsultandoDocumento);

            CargarProveedores();
        }

        private void CargarProveedores()
        {
            Proveedores.Clear();

            foreach (ProveedorStock proveedor in _proveedorNegocio.Listar())
            {
                Proveedores.Add(proveedor);
            }
        }

        private void Nuevo()
        {
            LimpiarCampos();
            MostrarFormulario = true;
        }

        private void Editar(object? parametro)
        {
            if (parametro is not ProveedorStock proveedor)
            {
                NotificationService.Warning("Debe seleccionar un proveedor");
                return;
            }

            IdProveedor = proveedor.IdProveedor;
            TipoDocumento = proveedor.TipoDocumento;
            NumeroDocumento = proveedor.NumeroDocumento;
            NombreRazonSocial = proveedor.NombreRazonSocial;
            Direccion = proveedor.Direccion;
            Telefono = proveedor.Telefono;
            Correo = proveedor.Correo;
            Estado = proveedor.Estado;
            ProveedorSeleccionado = proveedor;
            MostrarFormulario = true;
        }

        private void Guardar()
        {
            if (IdProveedor > 0)
            {
                bool confirmar = ConfirmDialogService.Confirmar(
                    "¿Desea actualizar la informacion del proveedor?",
                    "Confirmar actualizacion");

                if (!confirmar)
                {
                    return;
                }
            }

            ProveedorStock proveedor = new()
            {
                IdProveedor = IdProveedor,
                TipoDocumento = TipoDocumento,
                NumeroDocumento = NumeroDocumento,
                NombreRazonSocial = NombreRazonSocial,
                Direccion = Direccion,
                Telefono = Telefono,
                Correo = Correo,
                Estado = Estado
            };

            string mensaje = _proveedorNegocio.Guardar(proveedor);

            if (mensaje.Contains("correctamente"))
            {
                NotificationService.Success(mensaje);
                CargarProveedores();
                Cancelar();
            }
            else
            {
                NotificationService.Warning(mensaje);
            }
        }

        private void Eliminar(object? parametro)
        {
            if (parametro == null || !int.TryParse(parametro.ToString(), out int idProveedor))
            {
                NotificationService.Warning("Id de proveedor invalido");
                return;
            }

            bool confirmar = ConfirmDialogService.Confirmar(
                "¿Está seguro de eliminar este proveedor?",
                "Confirmar eliminacion");

            if (!confirmar)
            {
                return;
            }

            string mensaje = _proveedorNegocio.Eliminar(idProveedor);

            if (mensaje.Contains("correctamente"))
            {
                NotificationService.Success(mensaje);
                CargarProveedores();
                Cancelar();
            }
            else
            {
                NotificationService.Warning(mensaje);
            }
        }

        private async Task ConsultarDocumentoAsync()
        {
            string tipoDocumento = TipoDocumento.Trim().ToUpperInvariant();
            string numeroDocumento = NumeroDocumento.Trim();

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
                IsConsultandoDocumento = true;

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

                NombreRazonSocial = nombre;

                if (!string.IsNullOrWhiteSpace(direccion))
                {
                    Direccion = direccion;
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
                IsConsultandoDocumento = false;
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

        private void Cancelar()
        {
            LimpiarCampos();
            MostrarFormulario = false;
        }

        private void LimpiarCampos()
        {
            IdProveedor = 0;
            TipoDocumento = "RUC";
            NumeroDocumento = string.Empty;
            NombreRazonSocial = string.Empty;
            Direccion = string.Empty;
            Telefono = string.Empty;
            Correo = string.Empty;
            Estado = true;
            ProveedorSeleccionado = null;
        }
    }
}
