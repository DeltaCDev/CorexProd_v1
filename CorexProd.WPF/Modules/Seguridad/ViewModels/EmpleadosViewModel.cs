using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.Modules.Seguridad.Views;
using CorexProd.WPF.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Seguridad.ViewModels
{
    public class EmpleadosViewModel : BaseViewModel
    {
        private const string ApiUrlPredeterminada = "https://ruc.com.pe/api/v1/consultas";
        private const string ApiTokenPredeterminado = "0a682fbe-009d-4758-aad1-2ff1092ab7c2-838d6cd5-620a-4add-9632-a3b37c5ae216";

        private static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        private readonly EmpleadoNegocio _empleadoNegocio = new();
        private readonly CargoNegocio _cargoNegocio = new();

        private int _idEmpleado;
        private string _tipoDocumento = "DNI";
        private string _numeroDocumento = string.Empty;
        private string _nombre = string.Empty;
        private string _apellido = string.Empty;
        private string _sexo = "Masculino";
        private string _telefono = string.Empty;
        private string _email = string.Empty;
        private string _direccion = string.Empty;
        private int _idCargo;
        private DateTime? _fechaNacimiento;
        private bool _estado = true;
        private bool _isConsultandoDocumento;
        private Empleado? _empleadoSeleccionado;

        public ObservableCollection<Empleado> Empleados { get; set; } = [];
        public ObservableCollection<Cargo> Cargos { get; set; } = [];

        public int IdEmpleado
        {
            get => _idEmpleado;
            set
            {
                _idEmpleado = value;
                OnPropertyChanged();
            }
        }

        public string TipoDocumento
        {
            get => _tipoDocumento;
            set
            {
                _tipoDocumento = value;
                OnPropertyChanged();
            }
        }

        public string NumeroDocumento
        {
            get => _numeroDocumento;
            set
            {
                _numeroDocumento = value;
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

        public string Apellido
        {
            get => _apellido;
            set
            {
                _apellido = value;
                OnPropertyChanged();
            }
        }

        public string Sexo
        {
            get => _sexo;
            set
            {
                _sexo = value;
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

        public string Email
        {
            get => _email;
            set
            {
                _email = value;
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

        public int IdCargo
        {
            get => _idCargo;
            set
            {
                _idCargo = value;
                OnPropertyChanged();
            }
        }

        public DateTime? FechaNacimiento
        {
            get => _fechaNacimiento;
            set
            {
                _fechaNacimiento = value;
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

        public Empleado? EmpleadoSeleccionado
        {
            get => _empleadoSeleccionado;
            set
            {
                _empleadoSeleccionado = value;
                OnPropertyChanged();

                if (_empleadoSeleccionado != null)
                {
                    IdEmpleado = _empleadoSeleccionado.IdEmpleado;
                    TipoDocumento = _empleadoSeleccionado.TipoDocumento;
                    NumeroDocumento = _empleadoSeleccionado.NumeroDocumento;
                    Nombre = _empleadoSeleccionado.Nombre;
                    Apellido = _empleadoSeleccionado.Apellido;
                    Sexo = _empleadoSeleccionado.Sexo;
                    Telefono = _empleadoSeleccionado.Telefono;
                    Email = _empleadoSeleccionado.Email;
                    Direccion = _empleadoSeleccionado.Direccion;
                    IdCargo = _empleadoSeleccionado.IdCargo;
                    FechaNacimiento = _empleadoSeleccionado.FechaNacimiento;
                    Estado = _empleadoSeleccionado.Estado;
                }
            }
        }

        public ICommand GuardarCommand { get; }
        public ICommand LimpiarCommand { get; }
        public ICommand EliminarCommand { get; }
        public ICommand NuevoCommand { get; }
        public ICommand EditarCommand { get; }
        public ICommand RefrescarCommand { get; }
        public ICommand CerrarCommand { get; }
        public ICommand ConsultarDocumentoCommand { get; }

        public Action? CerrarVentana { get; set; }
        public bool Guardado { get; private set; }
        public string TituloEditor => IdEmpleado > 0 ? "Editar Empleado" : "Nuevo Empleado";
        public string ResumenRegistros => $"Mostrando {Empleados.Count} empleados";

        public EmpleadosViewModel()
        {
            GuardarCommand = new RelayCommand(_ => Guardar());
            LimpiarCommand = new RelayCommand(_ => Limpiar());
            EliminarCommand = new RelayCommand(parametro => Eliminar(parametro));
            NuevoCommand = new RelayCommand(_ => AbrirEditor(null));
            EditarCommand = new RelayCommand(parametro => Editar(parametro));
            RefrescarCommand = new RelayCommand(_ => Refrescar());
            CerrarCommand = new RelayCommand(_ => CerrarVentana?.Invoke());
            ConsultarDocumentoCommand = new RelayCommand(
                async _ => await ConsultarDocumentoAsync(),
                _ => !IsConsultandoDocumento);

            CargarCargos();
            CargarEmpleados();
        }

        private void CargarCargos()
        {
            Cargos.Clear();

            foreach (Cargo cargo in _cargoNegocio.Listar())
            {
                if (cargo.Estado)
                {
                    Cargos.Add(cargo);
                }
            }
        }

        private void CargarEmpleados()
        {
            Empleados.Clear();

            foreach (Empleado empleado in _empleadoNegocio.Listar())
            {
                Empleados.Add(empleado);
            }

            OnPropertyChanged(nameof(ResumenRegistros));
        }

        private async Task ConsultarDocumentoAsync()
        {
            string tipoDocumento = TipoDocumento.Trim().ToUpperInvariant();
            string numeroDocumento = NumeroDocumento.Trim();

            if (tipoDocumento != "DNI")
            {
                NotificationService.Warning("La consulta solo esta disponible para DNI");
                return;
            }

            if (numeroDocumento.Length != 8 || !numeroDocumento.All(char.IsDigit))
            {
                NotificationService.Warning("Ingrese un DNI valido de 8 digitos");
                return;
            }

            try
            {
                IsConsultandoDocumento = true;

                using JsonDocument respuesta = await ConsultarApiDocumentoAsync(numeroDocumento);
                JsonElement raiz = respuesta.RootElement;

                if (!ObtenerBooleano(raiz, "success"))
                {
                    NotificationService.Warning(ObtenerTexto(raiz, "message", "mensaje", "error") ?? "No se encontro informacion para el DNI");
                    return;
                }

                (string nombres, string apellidos) = ObtenerNombreYApellidos(raiz);

                if (string.IsNullOrWhiteSpace(nombres) || string.IsNullOrWhiteSpace(apellidos))
                {
                    NotificationService.Warning("La API respondio correctamente, pero no envio nombres y apellidos completos");
                    return;
                }

                Nombre = nombres;
                Apellido = apellidos;

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

        private static async Task<JsonDocument> ConsultarApiDocumentoAsync(string numeroDocumento)
        {
            string apiUrl = ConfigurationManager.AppSettings["RucComPeApiUrl"] ?? ApiUrlPredeterminada;
            string token = ConfigurationManager.AppSettings["RucComPeApiToken"] ?? ApiTokenPredeterminado;

            var payload = new Dictionary<string, string>
            {
                ["token"] = token,
                ["dni"] = numeroDocumento
            };

            string json = JsonSerializer.Serialize(payload);
            using StringContent contenido = new(json, Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await HttpClient.PostAsync(apiUrl, contenido);

            await using Stream stream = await response.Content.ReadAsStreamAsync();
            return await JsonDocument.ParseAsync(stream);
        }

        private static (string nombres, string apellidos) ObtenerNombreYApellidos(JsonElement raiz)
        {
            string? nombres = ObtenerTexto(raiz, "nombres");
            string? apellidoPaterno = ObtenerTexto(raiz, "apellido_paterno", "ap_paterno", "paterno");
            string? apellidoMaterno = ObtenerTexto(raiz, "apellido_materno", "ap_materno", "materno");

            if (!string.IsNullOrWhiteSpace(nombres)
                && (!string.IsNullOrWhiteSpace(apellidoPaterno) || !string.IsNullOrWhiteSpace(apellidoMaterno)))
            {
                return (NormalizarEspacios(nombres), NormalizarEspacios($"{apellidoPaterno} {apellidoMaterno}"));
            }

            string? nombreCompleto = ObtenerTexto(raiz, "nombre_completo", "nombre");
            return SepararNombreCompleto(nombreCompleto);
        }

        private static (string nombres, string apellidos) SepararNombreCompleto(string? nombreCompleto)
        {
            string[] partes = NormalizarEspacios(nombreCompleto)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (partes.Length < 3)
            {
                return (NormalizarEspacios(nombreCompleto), string.Empty);
            }

            string apellidos = string.Join(" ", partes.Take(2));
            string nombres = string.Join(" ", partes.Skip(2));

            return (nombres, apellidos);
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

        private static string NormalizarEspacios(string? valor)
        {
            return string.Join(" ", (valor ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        private void Guardar()
        {
            if (IdEmpleado > 0)
            {
                bool confirmar = ConfirmDialogService.Confirmar(
                    "¿Desea actualizar la información del empleado?",
                    "Confirmar actualización");

                if (!confirmar)
                {
                    return;
                }
            }
            Empleado empleado = new()
            {
                IdEmpleado = IdEmpleado,
                TipoDocumento = TipoDocumento,
                NumeroDocumento = NumeroDocumento,
                Nombre = Nombre,
                Apellido = Apellido,
                Sexo = Sexo,
                Telefono = Telefono,
                Email = Email,
                Direccion = Direccion,
                IdCargo = IdCargo,
                FechaNacimiento = FechaNacimiento,
                Estado = Estado
            };

            string mensaje = _empleadoNegocio.Guardar(empleado);

            if (mensaje.Contains("correctamente"))
            {
                NotificationService.Success(mensaje);
                CargarEmpleados();
                Guardado = true;
                CerrarVentana?.Invoke();
            }
            else
            {
                NotificationService.Warning(mensaje);
            }
        }

        private void Eliminar(object? parametro)
        {
            if (parametro == null)
            {
                NotificationService.Warning("Debe seleccionar un empleado");
                return;
            }

            if (!int.TryParse(parametro.ToString(), out int idEmpleado))
            {
                NotificationService.Warning("Id de empleado inválido");
                return;
            }
            bool confirmar = ConfirmDialogService.Confirmar(
                "¿Está seguro de eliminar este empleado?",
                "Confirmar eliminación");

            if (!confirmar)
            {
                return;
            }

            string mensaje = _empleadoNegocio.Eliminar(idEmpleado);

            if (mensaje.Contains("correctamente"))
            {
                NotificationService.Success(mensaje);
                CargarEmpleados();
                Limpiar();
            }
            else
            {
                NotificationService.Warning(mensaje);
            }
        }

        private void Limpiar()
        {
            IdEmpleado = 0;
            TipoDocumento = "DNI";
            NumeroDocumento = string.Empty;
            Nombre = string.Empty;
            Apellido = string.Empty;
            Sexo = "Masculino";
            Telefono = string.Empty;
            Email = string.Empty;
            Direccion = string.Empty;
            IdCargo = 0;
            FechaNacimiento = null;
            Estado = true;
            EmpleadoSeleccionado = null;
            OnPropertyChanged(nameof(TituloEditor));
        }

        private void Refrescar()
        {
            CargarCargos();
            CargarEmpleados();
        }

        private void AbrirEditor(Empleado? empleado)
        {
            EmpleadosViewModel viewModel = new();

            if (empleado != null)
            {
                viewModel.IdEmpleado = empleado.IdEmpleado;
                viewModel.TipoDocumento = empleado.TipoDocumento;
                viewModel.NumeroDocumento = empleado.NumeroDocumento;
                viewModel.Nombre = empleado.Nombre;
                viewModel.Apellido = empleado.Apellido;
                viewModel.Sexo = empleado.Sexo;
                viewModel.Telefono = empleado.Telefono;
                viewModel.Email = empleado.Email;
                viewModel.Direccion = empleado.Direccion;
                viewModel.IdCargo = empleado.IdCargo;
                viewModel.FechaNacimiento = empleado.FechaNacimiento;
                viewModel.Estado = empleado.Estado;
                viewModel.OnPropertyChanged(nameof(TituloEditor));
            }

            EmpleadoEditorWindow ventana = new()
            {
                DataContext = viewModel,
                Owner = Application.Current.MainWindow
            };

            viewModel.CerrarVentana = ventana.Close;
            ventana.ShowDialog();

            if (viewModel.Guardado)
            {
                Refrescar();
                Limpiar();
            }
        }

        private void Editar(object? parametro)
        {
            if (parametro is not Empleado empleado)
            {
                NotificationService.Warning("Debe seleccionar un empleado");
                return;
            }

            AbrirEditor(empleado);
        }
    }
}
