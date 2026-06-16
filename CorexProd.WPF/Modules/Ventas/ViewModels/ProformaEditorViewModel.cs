using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Ventas.ViewModels
{
    public class ProformaEditorViewModel : BaseViewModel
    {
        private readonly ProformaNegocio _proformaNegocio = new();
        private readonly ClienteNegocio _clienteNegocio = new();
        private readonly ProductoNegocio _productoNegocio = new();

        private int _idProforma;
        private string _serieNumero = string.Empty;
        private DateTime _fechaEmision = DateTime.Today;
        private DateTime _fechaVencimiento = DateTime.Today;
        private string _ordenCompraCliente = string.Empty;
        private Cliente? _clienteSeleccionado;
        private string _observacion = string.Empty;
        private bool _mostrarClienteRapido;
        private string _nuevoClienteTipoDocumento = "RUC";
        private string _nuevoClienteNumeroDocumento = string.Empty;
        private string _nuevoClienteNombre = string.Empty;
        private string _nuevoClienteDireccion = string.Empty;
        private string _nuevoClienteTelefono = string.Empty;
        private string _nuevoClienteCorreo = string.Empty;

        public ObservableCollection<Cliente> Clientes { get; } = [];
        public ObservableCollection<Producto> Productos { get; } = [];
        public ObservableCollection<ProformaDetalleItemViewModel> Detalles { get; } = [];

        public Action? CerrarVentana { get; set; }
        public bool Guardado { get; private set; }

        public int IdProforma
        {
            get => _idProforma;
            set
            {
                _idProforma = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Titulo));
            }
        }

        public string Titulo => IdProforma > 0 ? "Editar Proforma" : "Nueva Proforma";

        public string SerieNumero
        {
            get => _serieNumero;
            set
            {
                _serieNumero = value;
                OnPropertyChanged();
            }
        }

        public DateTime FechaEmision
        {
            get => _fechaEmision;
            set
            {
                _fechaEmision = value;
                OnPropertyChanged();
            }
        }

        public DateTime FechaVencimiento
        {
            get => _fechaVencimiento;
            set
            {
                _fechaVencimiento = value;
                OnPropertyChanged();
            }
        }

        public string OrdenCompraCliente
        {
            get => _ordenCompraCliente;
            set
            {
                _ordenCompraCliente = value;
                OnPropertyChanged();
            }
        }

        public Cliente? ClienteSeleccionado
        {
            get => _clienteSeleccionado;
            set
            {
                _clienteSeleccionado = value;
                OnPropertyChanged();
            }
        }

        public string Observacion
        {
            get => _observacion;
            set
            {
                _observacion = value;
                OnPropertyChanged();
            }
        }

        public bool MostrarClienteRapido
        {
            get => _mostrarClienteRapido;
            set
            {
                _mostrarClienteRapido = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ClienteRapidoVisibility));
            }
        }

        public Visibility ClienteRapidoVisibility => MostrarClienteRapido ? Visibility.Visible : Visibility.Collapsed;

        public string NuevoClienteTipoDocumento
        {
            get => _nuevoClienteTipoDocumento;
            set
            {
                _nuevoClienteTipoDocumento = value;
                OnPropertyChanged();
            }
        }

        public string NuevoClienteNumeroDocumento
        {
            get => _nuevoClienteNumeroDocumento;
            set
            {
                _nuevoClienteNumeroDocumento = value;
                OnPropertyChanged();
            }
        }

        public string NuevoClienteNombre
        {
            get => _nuevoClienteNombre;
            set
            {
                _nuevoClienteNombre = value;
                OnPropertyChanged();
            }
        }

        public string NuevoClienteDireccion
        {
            get => _nuevoClienteDireccion;
            set
            {
                _nuevoClienteDireccion = value;
                OnPropertyChanged();
            }
        }

        public string NuevoClienteTelefono
        {
            get => _nuevoClienteTelefono;
            set
            {
                _nuevoClienteTelefono = value;
                OnPropertyChanged();
            }
        }

        public string NuevoClienteCorreo
        {
            get => _nuevoClienteCorreo;
            set
            {
                _nuevoClienteCorreo = value;
                OnPropertyChanged();
            }
        }

        public decimal Subtotal => Detalles.Sum(d => d.Cantidad * d.PrecioUnitario);
        public decimal Descuento => Detalles.Sum(d => d.Descuento);
        public decimal Igv => 0;
        public decimal Total => Detalles.Sum(d => d.Importe);

        public ICommand AgregarProductoCommand { get; }
        public ICommand QuitarProductoCommand { get; }
        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }
        public ICommand MostrarClienteRapidoCommand { get; }
        public ICommand GuardarClienteRapidoCommand { get; }
        public ICommand CancelarClienteRapidoCommand { get; }

        public ProformaEditorViewModel(Proforma? proforma = null, bool copiar = false)
        {
            AgregarProductoCommand = new RelayCommand(_ => AgregarFila());
            QuitarProductoCommand = new RelayCommand(parametro => QuitarFila(parametro));
            GuardarCommand = new RelayCommand(_ => Guardar());
            CancelarCommand = new RelayCommand(_ => CerrarVentana?.Invoke());
            MostrarClienteRapidoCommand = new RelayCommand(_ => MostrarClienteRapido = true);
            GuardarClienteRapidoCommand = new RelayCommand(_ => GuardarClienteRapido());
            CancelarClienteRapidoCommand = new RelayCommand(_ => LimpiarClienteRapido());

            CargarClientes();
            CargarProductos();

            if (proforma != null)
            {
                CargarProforma(proforma, copiar);
            }
            else
            {
                AgregarFila();
            }
        }

        private void CargarClientes()
        {
            Clientes.Clear();

            foreach (Cliente cliente in _clienteNegocio.Listar().Where(c => c.Estado))
            {
                Clientes.Add(cliente);
            }
        }

        private void CargarProductos()
        {
            Productos.Clear();

            foreach (Producto producto in _productoNegocio.Listar().Where(p => p.Estado))
            {
                Productos.Add(producto);
            }
        }

        private void CargarProforma(Proforma proforma, bool copiar)
        {
            IdProforma = copiar ? 0 : proforma.IdProforma;
            SerieNumero = copiar ? string.Empty : proforma.SerieNumero;
            FechaEmision = DateTime.Today;
            FechaVencimiento = DateTime.Today;
            OrdenCompraCliente = proforma.OrdenCompraCliente;
            ClienteSeleccionado = Clientes.FirstOrDefault(c => c.IdCliente == proforma.IdCliente);
            Observacion = proforma.Observacion;

            Detalles.Clear();

            foreach (ProformaDetalle detalle in proforma.Detalles)
            {
                ProformaDetalleItemViewModel item = CrearFilaDetalle();
                item.IdProformaDetalle = copiar ? 0 : detalle.IdProformaDetalle;
                item.CargarProducto(detalle.IdProducto);
                item.Cantidad = detalle.Cantidad;
                item.PrecioUnitario = detalle.PrecioUnitario;
                item.Descuento = detalle.Descuento;
                item.Observacion = detalle.Observacion;
                Detalles.Add(item);
            }

            if (Detalles.Count == 0)
            {
                AgregarFila();
            }

            NotificarTotales();
        }

        private void AgregarFila()
        {
            Detalles.Add(CrearFilaDetalle());
            NotificarTotales();
        }

        private ProformaDetalleItemViewModel CrearFilaDetalle()
        {
            ProformaDetalleItemViewModel item = new(Productos);
            item.TotalesActualizados += NotificarTotales;
            item.ProductoCambiado += ValidarProductoRepetido;
            return item;
        }

        private void QuitarFila(object? parametro)
        {
            if (parametro is not ProformaDetalleItemViewModel item)
            {
                return;
            }

            if (Detalles.Count == 1)
            {
                NotificationService.Warning("Debe mantener al menos una fila de producto");
                return;
            }

            Detalles.Remove(item);
            NotificarTotales();
        }

        private void ValidarProductoRepetido(ProformaDetalleItemViewModel filaActual, Producto producto)
        {
            ProformaDetalleItemViewModel? filaExistente = Detalles
                .FirstOrDefault(d => !ReferenceEquals(d, filaActual) && d.IdProducto == producto.IdProducto);

            if (filaExistente == null)
            {
                return;
            }

            bool unir = ConfirmDialogService.Confirmar(
                "El producto ya existe en otra fila. ¿Desea unir la cantidad a la fila anterior?",
                "Producto repetido");

            if (unir)
            {
                filaExistente.Cantidad += filaActual.Cantidad;
                filaActual.CargarProducto(0);
                NotificarTotales();
            }
        }

        private void GuardarClienteRapido()
        {
            Cliente cliente = new()
            {
                TipoDocumento = NuevoClienteTipoDocumento,
                NumeroDocumento = NuevoClienteNumeroDocumento,
                NombreRazonSocial = NuevoClienteNombre,
                Direccion = NuevoClienteDireccion,
                Telefono = NuevoClienteTelefono,
                Correo = NuevoClienteCorreo,
                Estado = true
            };

            string mensaje = _clienteNegocio.Guardar(cliente);

            if (!mensaje.Contains("correctamente"))
            {
                NotificationService.Warning(mensaje);
                return;
            }

            NotificationService.Success(mensaje);
            CargarClientes();
            ClienteSeleccionado = Clientes
                .FirstOrDefault(c => c.NumeroDocumento == NuevoClienteNumeroDocumento.Trim()
                    && c.NombreRazonSocial == NuevoClienteNombre.Trim());
            LimpiarClienteRapido();
        }

        private void LimpiarClienteRapido()
        {
            MostrarClienteRapido = false;
            NuevoClienteTipoDocumento = "RUC";
            NuevoClienteNumeroDocumento = string.Empty;
            NuevoClienteNombre = string.Empty;
            NuevoClienteDireccion = string.Empty;
            NuevoClienteTelefono = string.Empty;
            NuevoClienteCorreo = string.Empty;
        }

        private void Guardar()
        {
            Proforma proforma = new()
            {
                IdProforma = IdProforma,
                FechaEmision = FechaEmision,
                FechaVencimiento = FechaVencimiento,
                OrdenCompraCliente = OrdenCompraCliente,
                IdCliente = ClienteSeleccionado?.IdCliente ?? 0,
                Observacion = Observacion,
                Detalles = Detalles.Select(d => d.CrearDetalle()).ToList()
            };

            string mensaje = _proformaNegocio.Guardar(proforma);

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

        private void NotificarTotales()
        {
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(Descuento));
            OnPropertyChanged(nameof(Igv));
            OnPropertyChanged(nameof(Total));
        }
    }
}
