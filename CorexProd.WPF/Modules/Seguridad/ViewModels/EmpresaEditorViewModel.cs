using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Seguridad.ViewModels
{
    public class EmpresaEditorViewModel : BaseViewModel
    {
        private const string ApiUrlPredeterminada = "https://ruc.com.pe/api/v1/consultas";
        private const string ApiTokenPredeterminado = "0a682fbe-009d-4758-aad1-2ff1092ab7c2-838d6cd5-620a-4add-9632-a3b37c5ae216";

        private static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        private readonly EmpresaNegocio _empresaNegocio = new();

        private int _idEmpresa;
        private string _ruc = string.Empty;
        private string _nombre = string.Empty;
        private string _nombreComercial = string.Empty;
        private string _telefono = string.Empty;
        private string _correo = string.Empty;
        private string _departamento = string.Empty;
        private string _provincia = string.Empty;
        private string _distrito = string.Empty;
        private string _direccion = string.Empty;
        private byte[]? _logo;
        private string _logoResumen = "Sin logo seleccionado";
        private string _codigoCliente = string.Empty;
        private string _licenciaActivacion = string.Empty;
        private bool _esPredeterminada;
        private bool _estado = true;
        private bool _isConsultandoRuc;

        public Action? CerrarVentana { get; set; }
        public bool Guardado { get; private set; }
        public string Titulo => IdEmpresa > 0 ? "Editar Empresa" : "Nueva Empresa";

        public int IdEmpresa
        {
            get => _idEmpresa;
            set
            {
                _idEmpresa = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Titulo));
            }
        }

        public string Ruc
        {
            get => _ruc;
            set
            {
                _ruc = value;
                OnPropertyChanged();
            }
        }

        public string Nombre
        {
            get => _nombre;
            set
            {
                _nombre = value;
                OnPropertyChanged();
            }
        }

        public string NombreComercial
        {
            get => _nombreComercial;
            set
            {
                _nombreComercial = value;
                OnPropertyChanged();
            }
        }

        public string Telefono
        {
            get => _telefono;
            set
            {
                _telefono = value;
                OnPropertyChanged();
            }
        }

        public string Correo
        {
            get => _correo;
            set
            {
                _correo = value;
                OnPropertyChanged();
            }
        }

        public string Departamento
        {
            get => _departamento;
            set
            {
                _departamento = value;
                OnPropertyChanged();
            }
        }

        public string Provincia
        {
            get => _provincia;
            set
            {
                _provincia = value;
                OnPropertyChanged();
            }
        }

        public string Distrito
        {
            get => _distrito;
            set
            {
                _distrito = value;
                OnPropertyChanged();
            }
        }

        public string Direccion
        {
            get => _direccion;
            set
            {
                _direccion = value;
                OnPropertyChanged();
            }
        }

        public byte[]? Logo
        {
            get => _logo;
            set
            {
                _logo = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneLogo));
            }
        }

        public bool TieneLogo => Logo?.Length > 0;

        public string LogoResumen
        {
            get => _logoResumen;
            set
            {
                _logoResumen = value;
                OnPropertyChanged();
            }
        }

        public string CodigoCliente
        {
            get => _codigoCliente;
            set
            {
                _codigoCliente = value;
                OnPropertyChanged();
            }
        }

        public string LicenciaActivacion
        {
            get => _licenciaActivacion;
            set
            {
                _licenciaActivacion = value;
                OnPropertyChanged();
            }
        }

        public bool EsPredeterminada
        {
            get => _esPredeterminada;
            set
            {
                _esPredeterminada = value;
                OnPropertyChanged();
            }
        }

        public bool Estado
        {
            get => _estado;
            set
            {
                _estado = value;
                OnPropertyChanged();
            }
        }

        public bool IsConsultandoRuc
        {
            get => _isConsultandoRuc;
            set
            {
                _isConsultandoRuc = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }
        public ICommand SeleccionarLogoCommand { get; }
        public ICommand ValidarLicenciaCommand { get; }
        public ICommand ConsultarRucCommand { get; }

        public EmpresaEditorViewModel(Empresa? empresa = null, bool marcarPredeterminada = false)
        {
            GuardarCommand = new RelayCommand(_ => Guardar());
            CancelarCommand = new RelayCommand(_ => CerrarVentana?.Invoke());
            SeleccionarLogoCommand = new RelayCommand(_ => SeleccionarLogo());
            ValidarLicenciaCommand = new RelayCommand(_ => NotificationService.Info("Activacion de licencia en mantenimiento"));
            ConsultarRucCommand = new RelayCommand(
                async _ => await ConsultarRucAsync(),
                _ => !IsConsultandoRuc);

            if (empresa != null)
            {
                CargarEmpresa(empresa);
            }
            else
            {
                EsPredeterminada = marcarPredeterminada;
                Estado = true;
            }
        }

        private async Task ConsultarRucAsync()
        {
            string ruc = Ruc.Trim();

            if (ruc.Length != 11 || !ruc.All(char.IsDigit))
            {
                NotificationService.Warning("Ingrese un RUC valido de 11 digitos");
                return;
            }

            try
            {
                IsConsultandoRuc = true;

                using JsonDocument respuesta = await ConsultarApiRucAsync(ruc);
                JsonElement raiz = respuesta.RootElement;

                if (!ObtenerBooleano(raiz, "success"))
                {
                    NotificationService.Warning(ObtenerTexto(raiz, "message", "mensaje", "error") ?? "No se encontro informacion para el RUC");
                    return;
                }

                string? nombre = ObtenerTexto(raiz, "nombre_o_razon_social", "razon_social", "nombre", "nombre_completo");

                if (string.IsNullOrWhiteSpace(nombre))
                {
                    NotificationService.Warning("La API respondio correctamente, pero no envio la razon social");
                    return;
                }

                Nombre = nombre;
                NombreComercial = ObtenerTexto(raiz, "nombre_comercial") ?? NombreComercial;
                Direccion = ObtenerTexto(raiz, "direccion", "domicilio_fiscal") ?? Direccion;
                Departamento = ObtenerTexto(raiz, "departamento") ?? Departamento;
                Provincia = ObtenerTexto(raiz, "provincia") ?? Provincia;
                Distrito = ObtenerTexto(raiz, "distrito") ?? Distrito;

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
                IsConsultandoRuc = false;
            }
        }

        private static async Task<JsonDocument> ConsultarApiRucAsync(string ruc)
        {
            string apiUrl = ConfigurationManager.AppSettings["RucComPeApiUrl"] ?? ApiUrlPredeterminada;
            string token = ConfigurationManager.AppSettings["RucComPeApiToken"] ?? ApiTokenPredeterminado;

            var payload = new Dictionary<string, string>
            {
                ["token"] = token,
                ["ruc"] = ruc
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

        private void Guardar()
        {
            if (IdEmpresa > 0)
            {
                bool confirmar = ConfirmDialogService.Confirmar(
                    "¿Desea actualizar la informacion de la empresa?",
                    "Confirmar actualizacion");

                if (!confirmar)
                {
                    return;
                }
            }

            Empresa empresa = new()
            {
                IdEmpresa = IdEmpresa,
                Ruc = Ruc,
                Nombre = Nombre,
                NombreComercial = NombreComercial,
                Telefono = Telefono,
                Correo = Correo,
                Departamento = Departamento,
                Provincia = Provincia,
                Distrito = Distrito,
                Direccion = Direccion,
                Logo = Logo,
                CodigoCliente = CodigoCliente,
                LicenciaActivacion = LicenciaActivacion,
                EsPredeterminada = EsPredeterminada,
                Estado = Estado
            };

            string mensaje = _empresaNegocio.Guardar(empresa);

            if (mensaje.Contains("correctamente"))
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

        private void SeleccionarLogo()
        {
            OpenFileDialog dialog = new()
            {
                Title = "Seleccionar logo",
                Filter = "Imagenes|*.png;*.jpg;*.jpeg;*.bmp|Todos los archivos|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                Logo = File.ReadAllBytes(dialog.FileName);
                LogoResumen = Path.GetFileName(dialog.FileName);
            }
        }

        private void CargarEmpresa(Empresa empresa)
        {
            IdEmpresa = empresa.IdEmpresa;
            Ruc = empresa.Ruc;
            Nombre = empresa.Nombre;
            NombreComercial = empresa.NombreComercial;
            Telefono = empresa.Telefono;
            Correo = empresa.Correo;
            Departamento = empresa.Departamento;
            Provincia = empresa.Provincia;
            Distrito = empresa.Distrito;
            Direccion = empresa.Direccion;
            Logo = empresa.Logo;
            LogoResumen = TieneLogo ? "Logo guardado en BD" : "Sin logo seleccionado";
            CodigoCliente = empresa.CodigoCliente;
            LicenciaActivacion = empresa.LicenciaActivacion;
            EsPredeterminada = empresa.EsPredeterminada;
            Estado = empresa.Estado;
        }
    }
}
