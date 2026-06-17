using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.Modules.Seguridad.Views;
using CorexProd.WPF.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Seguridad.ViewModels
{
    public class EmpleadosViewModel : BaseViewModel
    {
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
