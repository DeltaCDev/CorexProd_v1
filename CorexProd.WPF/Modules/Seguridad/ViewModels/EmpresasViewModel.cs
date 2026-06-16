using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.ViewModels;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Seguridad.ViewModels
{
    public class EmpresasViewModel : BaseViewModel
    {
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
        private string _logo = string.Empty;
        private string _codigoCliente = string.Empty;
        private string _licenciaActivacion = string.Empty;
        private bool _esPredeterminada;
        private bool _estado = true;
        private Empresa? _empresaSeleccionada;

        public ObservableCollection<Empresa> Empresas { get; } = [];

        public int IdEmpresa
        {
            get => _idEmpresa;
            set
            {
                _idEmpresa = value;
                OnPropertyChanged();
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

        public string Logo
        {
            get => _logo;
            set
            {
                _logo = value;
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

        public Empresa? EmpresaSeleccionada
        {
            get => _empresaSeleccionada;
            set
            {
                _empresaSeleccionada = value;
                OnPropertyChanged();

                if (_empresaSeleccionada != null)
                {
                    CargarFormulario(_empresaSeleccionada);
                }
            }
        }

        public ICommand GuardarCommand { get; }
        public ICommand LimpiarCommand { get; }
        public ICommand EliminarCommand { get; }
        public ICommand SeleccionarLogoCommand { get; }
        public ICommand ValidarLicenciaCommand { get; }

        public EmpresasViewModel()
        {
            GuardarCommand = new RelayCommand(_ => Guardar());
            LimpiarCommand = new RelayCommand(_ => Limpiar());
            EliminarCommand = new RelayCommand(parametro => Eliminar(parametro));
            SeleccionarLogoCommand = new RelayCommand(_ => SeleccionarLogo());
            ValidarLicenciaCommand = new RelayCommand(_ => NotificationService.Info("Activacion de licencia en mantenimiento"));

            CargarEmpresas();
            EsPredeterminada = Empresas.Count == 0;
        }

        private void CargarEmpresas()
        {
            Empresas.Clear();

            foreach (Empresa empresa in _empresaNegocio.Listar())
            {
                Empresas.Add(empresa);
            }
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
                NotificationService.Success(mensaje);
                CargarEmpresas();
                Limpiar();
            }
            else
            {
                NotificationService.Warning(mensaje);
            }
        }

        private void Eliminar(object? parametro)
        {
            if (parametro == null || !int.TryParse(parametro.ToString(), out int idEmpresa))
            {
                NotificationService.Warning("Debe seleccionar una empresa");
                return;
            }

            bool confirmar = ConfirmDialogService.Confirmar(
                "¿Esta seguro de eliminar esta empresa?",
                "Confirmar eliminacion");

            if (!confirmar)
            {
                return;
            }

            string mensaje = _empresaNegocio.Eliminar(idEmpresa);

            if (mensaje.Contains("correctamente"))
            {
                NotificationService.Success(mensaje);
                CargarEmpresas();
                Limpiar();
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
                Logo = dialog.FileName;
            }
        }

        private void CargarFormulario(Empresa empresa)
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
            CodigoCliente = empresa.CodigoCliente;
            LicenciaActivacion = empresa.LicenciaActivacion;
            EsPredeterminada = empresa.EsPredeterminada;
            Estado = empresa.Estado;
        }

        private void Limpiar()
        {
            IdEmpresa = 0;
            Ruc = string.Empty;
            Nombre = string.Empty;
            NombreComercial = string.Empty;
            Telefono = string.Empty;
            Correo = string.Empty;
            Departamento = string.Empty;
            Provincia = string.Empty;
            Distrito = string.Empty;
            Direccion = string.Empty;
            Logo = string.Empty;
            CodigoCliente = string.Empty;
            LicenciaActivacion = string.Empty;
            EsPredeterminada = Empresas.Count == 0;
            Estado = true;
            EmpresaSeleccionada = null;
        }
    }
}
