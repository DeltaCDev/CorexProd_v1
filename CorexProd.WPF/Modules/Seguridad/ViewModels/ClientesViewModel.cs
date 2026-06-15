using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.ViewModels;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Seguridad.ViewModels
{
    public class ClientesViewModel : BaseViewModel
    {
        private readonly ClienteNegocio _clienteNegocio = new();

        private int _idCliente;
        private string _tipoDocumento = "DNI";
        private string _numeroDocumento = string.Empty;
        private string _nombreRazonSocial = string.Empty;
        private string _direccion = string.Empty;
        private string _telefono = string.Empty;
        private string _correo = string.Empty;
        private bool _estado = true;
        private bool _mostrarFormulario;
        private Cliente? _clienteSeleccionado;

        public ObservableCollection<Cliente> Clientes { get; set; } = [];

        public int IdCliente
        {
            get => _idCliente;
            set
            {
                _idCliente = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TituloFormulario));
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

        public string NombreRazonSocial
        {
            get => _nombreRazonSocial;
            set
            {
                _nombreRazonSocial = value;
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

        public bool Estado
        {
            get => _estado;
            set
            {
                _estado = value;
                OnPropertyChanged();
            }
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

        public string TituloFormulario => IdCliente > 0 ? "Editar cliente" : "Nuevo cliente";

        public Cliente? ClienteSeleccionado
        {
            get => _clienteSeleccionado;
            set
            {
                _clienteSeleccionado = value;
                OnPropertyChanged();
            }
        }

        public ICommand NuevoCommand { get; }
        public ICommand EditarCommand { get; }
        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }
        public ICommand EliminarCommand { get; }
        public ICommand ConsultarDocumentoCommand { get; }

        public ClientesViewModel()
        {
            NuevoCommand = new RelayCommand(_ => Nuevo());
            EditarCommand = new RelayCommand(parametro => Editar(parametro));
            GuardarCommand = new RelayCommand(_ => Guardar());
            CancelarCommand = new RelayCommand(_ => Cancelar());
            EliminarCommand = new RelayCommand(parametro => Eliminar(parametro));
            ConsultarDocumentoCommand = new RelayCommand(_ => ConsultarDocumento());

            CargarClientes();
        }

        private void CargarClientes()
        {
            Clientes.Clear();

            foreach (Cliente cliente in _clienteNegocio.Listar())
            {
                Clientes.Add(cliente);
            }
        }

        private void Nuevo()
        {
            LimpiarCampos();
            MostrarFormulario = true;
        }

        private void Editar(object? parametro)
        {
            if (parametro is not Cliente cliente)
            {
                NotificationService.Warning("Debe seleccionar un cliente");
                return;
            }

            IdCliente = cliente.IdCliente;
            TipoDocumento = cliente.TipoDocumento;
            NumeroDocumento = cliente.NumeroDocumento;
            NombreRazonSocial = cliente.NombreRazonSocial;
            Direccion = cliente.Direccion;
            Telefono = cliente.Telefono;
            Correo = cliente.Correo;
            Estado = cliente.Estado;
            ClienteSeleccionado = cliente;
            MostrarFormulario = true;
        }

        private void Guardar()
        {
            if (IdCliente > 0)
            {
                bool confirmar = ConfirmDialogService.Confirmar(
                    "¿Desea actualizar la información del cliente?",
                    "Confirmar actualización");

                if (!confirmar)
                {
                    return;
                }
            }

            Cliente cliente = new()
            {
                IdCliente = IdCliente,
                TipoDocumento = TipoDocumento,
                NumeroDocumento = NumeroDocumento,
                NombreRazonSocial = NombreRazonSocial,
                Direccion = Direccion,
                Telefono = Telefono,
                Correo = Correo,
                Estado = Estado
            };

            string mensaje = _clienteNegocio.Guardar(cliente);

            if (mensaje.Contains("correctamente"))
            {
                NotificationService.Success(mensaje);
                CargarClientes();
                Cancelar();
            }
            else
            {
                NotificationService.Warning(mensaje);
            }
        }

        private void Eliminar(object? parametro)
        {
            if (parametro == null || !int.TryParse(parametro.ToString(), out int idCliente))
            {
                NotificationService.Warning("Id de cliente inválido");
                return;
            }

            bool confirmar = ConfirmDialogService.Confirmar(
                "¿Está seguro de eliminar este cliente?",
                "Confirmar eliminación");

            if (!confirmar)
            {
                return;
            }

            string mensaje = _clienteNegocio.Eliminar(idCliente);

            if (mensaje.Contains("correctamente"))
            {
                NotificationService.Success(mensaje);
                CargarClientes();
                Cancelar();
            }
            else
            {
                NotificationService.Warning(mensaje);
            }
        }

        private void ConsultarDocumento()
        {
            NotificationService.Info("Consulta de API en mantenimiento");
        }

        private void Cancelar()
        {
            LimpiarCampos();
            MostrarFormulario = false;
        }

        private void LimpiarCampos()
        {
            IdCliente = 0;
            TipoDocumento = "DNI";
            NumeroDocumento = string.Empty;
            NombreRazonSocial = string.Empty;
            Direccion = string.Empty;
            Telefono = string.Empty;
            Correo = string.Empty;
            Estado = true;
            ClienteSeleccionado = null;
        }
    }
}
