using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.ViewModels;
using Microsoft.Win32;
using System;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Seguridad.ViewModels
{
    public class EmpresaEditorViewModel : BaseViewModel
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

        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }
        public ICommand SeleccionarLogoCommand { get; }
        public ICommand ValidarLicenciaCommand { get; }

        public EmpresaEditorViewModel(Empresa? empresa = null, bool marcarPredeterminada = false)
        {
            GuardarCommand = new RelayCommand(_ => Guardar());
            CancelarCommand = new RelayCommand(_ => CerrarVentana?.Invoke());
            SeleccionarLogoCommand = new RelayCommand(_ => SeleccionarLogo());
            ValidarLicenciaCommand = new RelayCommand(_ => NotificationService.Info("Activacion de licencia en mantenimiento"));

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
                Logo = dialog.FileName;
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
            CodigoCliente = empresa.CodigoCliente;
            LicenciaActivacion = empresa.LicenciaActivacion;
            EsPredeterminada = empresa.EsPredeterminada;
            Estado = empresa.Estado;
        }
    }
}
