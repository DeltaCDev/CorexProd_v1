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
    public class UsuariosViewModel : BaseViewModel
    {
        private readonly UsuarioNegocio _usuarioNegocio = new();
        private readonly EmpleadoNegocio _empleadoNegocio = new();
        private readonly RolNegocio _rolNegocio = new();

        private int _idUsuario;
        private int _idEmpleado;
        private string _nombreUsuario = string.Empty;
        private string _clave = string.Empty;
        private int _idRol;
        private bool _estado = true;
        private Usuario? _usuarioSeleccionado;

        public ObservableCollection<Usuario> Usuarios { get; set; } = [];
        public ObservableCollection<Empleado> Empleados { get; set; } = [];
        public ObservableCollection<Rol> Roles { get; set; } = [];

        public int IdUsuario
        {
            get => _idUsuario;
            set { _idUsuario = value; OnPropertyChanged(); }
        }

        public int IdEmpleado
        {
            get => _idEmpleado;
            set { _idEmpleado = value; OnPropertyChanged(); }
        }

        public string NombreUsuario
        {
            get => _nombreUsuario;
            set { _nombreUsuario = value; OnPropertyChanged(); }
        }

        public string Clave
        {
            get => _clave;
            set { _clave = value; OnPropertyChanged(); }
        }

        public int IdRol
        {
            get => _idRol;
            set { _idRol = value; OnPropertyChanged(); }
        }

        public bool Estado
        {
            get => _estado;
            set { _estado = value; OnPropertyChanged(); }
        }

        public Usuario? UsuarioSeleccionado
        {
            get => _usuarioSeleccionado;
            set
            {
                _usuarioSeleccionado = value;
                OnPropertyChanged();

                if (_usuarioSeleccionado != null)
                {
                    IdUsuario = _usuarioSeleccionado.IdUsuario;
                    IdEmpleado = _usuarioSeleccionado.IdEmpleado;
                    NombreUsuario = _usuarioSeleccionado.NombreUsuario;
                    Clave = string.Empty;
                    IdRol = _usuarioSeleccionado.IdRol;
                    Estado = _usuarioSeleccionado.Estado;
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
        public string TituloEditor => IdUsuario > 0 ? "Editar Usuario" : "Nuevo Usuario";
        public string ResumenRegistros => $"Mostrando {Usuarios.Count} usuarios";

        public UsuariosViewModel()
        {
            GuardarCommand = new RelayCommand(_ => Guardar());
            LimpiarCommand = new RelayCommand(_ => Limpiar());
            EliminarCommand = new RelayCommand(parametro => Eliminar(parametro));
            NuevoCommand = new RelayCommand(_ => AbrirEditor(null));
            EditarCommand = new RelayCommand(parametro => Editar(parametro));
            RefrescarCommand = new RelayCommand(_ => Refrescar());
            CerrarCommand = new RelayCommand(_ => CerrarVentana?.Invoke());

            CargarEmpleados();
            CargarRoles();
            CargarUsuarios();
        }

        private void CargarUsuarios()
        {
            Usuarios.Clear();

            foreach (Usuario usuario in _usuarioNegocio.Listar())
            {
                Usuarios.Add(usuario);
            }

            OnPropertyChanged(nameof(ResumenRegistros));
        }

        private void CargarEmpleados()
        {
            Empleados.Clear();

            foreach (Empleado empleado in _empleadoNegocio.Listar())
            {
                if (empleado.Estado)
                {
                    Empleados.Add(empleado);
                }
            }
        }

        private void CargarRoles()
        {
            Roles.Clear();

            foreach (Rol rol in _rolNegocio.Listar())
            {
                if (rol.Estado)
                {
                    Roles.Add(rol);
                }
            }
        }

        private void Guardar()
        {
            if (IdUsuario > 0)
            {
                bool confirmar = ConfirmDialogService.Confirmar(
                    "¿Desea actualizar la información del usuario?",
                    "Confirmar actualización");

                if (!confirmar)
                {
                    return;
                }
            }

            Usuario usuario = new()
            {
                IdUsuario = IdUsuario,
                IdEmpleado = IdEmpleado,
                NombreUsuario = NombreUsuario,
                Clave = Clave,
                IdRol = IdRol,
                Estado = Estado
            };

            string usuarioAuditoria = SessionManager.UsuarioActual?.NombreUsuario ?? "Sistema";

            string mensaje = _usuarioNegocio.Guardar(usuario, usuarioAuditoria);

            if (mensaje.Contains("correctamente"))
            {
                NotificationService.Success(mensaje);
                CargarUsuarios();
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
                NotificationService.Warning("Debe seleccionar un usuario");
                return;
            }

            if (!int.TryParse(parametro.ToString(), out int idUsuario))
            {
                NotificationService.Warning("Id de usuario inválido");
                return;
            }

            bool confirmar = ConfirmDialogService.Confirmar(
                "¿Está seguro de eliminar este usuario?",
                "Confirmar eliminación");

            if (!confirmar)
            {
                return;
            }

            string mensaje = _usuarioNegocio.Eliminar(idUsuario);

            if (mensaje.Contains("correctamente"))
            {
                NotificationService.Success(mensaje);
                CargarUsuarios();
                Limpiar();
            }
            else
            {
                NotificationService.Warning(mensaje);
            }
        }

        private void Limpiar()
        {
            IdUsuario = 0;
            IdEmpleado = 0;
            NombreUsuario = string.Empty;
            Clave = string.Empty;
            IdRol = 0;
            Estado = true;
            UsuarioSeleccionado = null;
            OnPropertyChanged(nameof(TituloEditor));
        }

        private void Refrescar()
        {
            CargarEmpleados();
            CargarRoles();
            CargarUsuarios();
        }

        private void AbrirEditor(Usuario? usuario)
        {
            UsuariosViewModel viewModel = new();

            if (usuario != null)
            {
                viewModel.IdUsuario = usuario.IdUsuario;
                viewModel.IdEmpleado = usuario.IdEmpleado;
                viewModel.NombreUsuario = usuario.NombreUsuario;
                viewModel.Clave = string.Empty;
                viewModel.IdRol = usuario.IdRol;
                viewModel.Estado = usuario.Estado;
                viewModel.OnPropertyChanged(nameof(TituloEditor));
            }

            UsuarioEditorWindow ventana = new()
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
            if (parametro is not Usuario usuario)
            {
                NotificationService.Warning("Debe seleccionar un usuario");
                return;
            }

            AbrirEditor(usuario);
        }
    }
}
