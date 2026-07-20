using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.Modules.OrdenesServicio.Views;
using CorexProd.WPF.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.OrdenesServicio.ViewModels
{
    public class OrdenesServicioViewModel : BaseViewModel
    {
        private readonly OrdenServicioNegocio _negocio = new();
        private readonly UsuarioNegocio _usuarioNegocio = new();
        private readonly ProveedorNegocio _proveedorNegocio = new();
        private readonly ClienteNegocio _clienteNegocio = new();
        private string _textoBusqueda = string.Empty;
        private string _estadoFiltro = "Todos";
        private OrdenServicio? _ordenSeleccionada;
        private bool _mostrarFormulario;
        private int _idOrdenServicio;
        private DateTime _fecha = DateTime.Today;
        private DateTime? _fechaComprometida;
        private ProveedorStock? _proveedorSeleccionado;
        private TipoServicio? _tipoServicioSeleccionado;
        private Cliente? _clienteSeleccionado;
        private string _cliente = string.Empty;
        private string _ociRelacionada = string.Empty;
        private string _otRelacionada = string.Empty;
        private string _responsable = string.Empty;
        private string _formaPago = "Contado";
        private string _formaPagoTipo = "Contado";
        private string _detalleCredito = string.Empty;
        private string _observacionesInternas = string.Empty;
        private string _observaciones = string.Empty;
        private string _detalleProducto = string.Empty;
        private string _detalleDescripcion = string.Empty;
        private decimal _detalleCantidad = 1;
        private string _detalleUnidad = "UND";
        private decimal _detallePrecioUnitario;
        private OrdenServicioDetalle? _detalleEnEdicion;
        private decimal _aCuenta;
        private decimal _pagoImporte;
        private string _pagoMedio = "Transferencia";
        private string _pagoOperacion = string.Empty;
        private string _pagoObservacion = string.Empty;
        private bool _mostrarMovimientos;
        private string _tituloMovimientos = string.Empty;
        private string _tipoMovimientoActual = string.Empty;
        private string _fotoTitulo = string.Empty;
        private string _fotoDescripcion = string.Empty;
        private string _fotoUbicacionPdf = "Abajo";
        private string _distribucionFotosPdf = "1 x 2";
        private TipoServicio? _tipoSeleccionado;
        private int _tipoId;
        private string _tipoCodigo = string.Empty;
        private string _tipoNombre = string.Empty;
        private string _tipoDescripcion = string.Empty;
        private bool _tipoRequiereEntrega;
        private bool _tipoEstado = true;
        private bool _mostrarNuevoProveedor;
        private string _nuevoProveedorTipoDocumento = "S/N";
        private string _nuevoProveedorNumeroDocumento = string.Empty;
        private string _nuevoProveedorNombre = string.Empty;
        private string _nuevoProveedorDireccion = string.Empty;
        private string _nuevoProveedorTelefono = string.Empty;
        private string _nuevoProveedorCorreo = string.Empty;
        private bool _mostrarNuevoTipoServicio;
        private string _nuevoTipoCodigo = string.Empty;
        private string _nuevoTipoNombre = string.Empty;
        private string _nuevoTipoDescripcion = string.Empty;
        private bool _nuevoTipoRequiereEntrega;

        public ObservableCollection<OrdenServicio> Ordenes { get; } = [];
        public ObservableCollection<ProveedorStock> Proveedores { get; } = [];
        public ObservableCollection<Cliente> Clientes { get; } = [];
        public ObservableCollection<Cliente> ClientesCoincidencias { get; } = [];
        public ObservableCollection<TipoServicio> TiposServicio { get; } = [];
        public ObservableCollection<OrdenServicioDetalle> DetallesFormulario { get; } = [];
        public ObservableCollection<OrdenServicioDetalle> DetallesFormularioVista { get; } = [];
        public ObservableCollection<OrdenServicioMovimiento> MovimientosFormulario { get; } = [];
        public ObservableCollection<OrdenServicioFoto> FotosOrdenSeleccionada { get; } = [];
        public ObservableCollection<TipoServicio> TiposListado { get; } = [];
        public ICollectionView OrdenesVista { get; }
        public string[] Estados { get; } = ["Todos", "Borrador", "Aprobada", "Enviada al proveedor", "Recepcion Parcial", "Recibida", "Pendiente de Pago", "Pagada", "Anulada"];
        public string[] FormasPago { get; } = ["Contado", "Credito"];
        public string[] UbicacionesFotoPdf { get; } = ["Abajo", "Antes del resumen", "Pagina final"];
        public string[] DistribucionesFotosPdf { get; } = ["1 x 2", "2 x 2", "2 x 4"];

        public string TextoBusqueda
        {
            get => _textoBusqueda;
            set { _textoBusqueda = value ?? string.Empty; OnPropertyChanged(); CargarOrdenes(); }
        }

        public string EstadoFiltro
        {
            get => _estadoFiltro;
            set { _estadoFiltro = value ?? "Todos"; OnPropertyChanged(); CargarOrdenes(); }
        }

        public OrdenServicio? OrdenSeleccionada
        {
            get => _ordenSeleccionada;
            set
            {
                _ordenSeleccionada = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PuedeRegistrarPago));
                RefrescarFotosSeleccionadas();
            }
        }

        public bool MostrarFormulario
        {
            get => _mostrarFormulario;
            set { _mostrarFormulario = value; OnPropertyChanged(); OnPropertyChanged(nameof(FormularioVisibility)); }
        }

        public Visibility FormularioVisibility => MostrarFormulario ? Visibility.Visible : Visibility.Collapsed;
        public string TituloFormulario => _idOrdenServicio > 0 ? "Editar orden de servicio" : "Nueva orden de servicio";
        public bool PuedeRegistrarPago => OrdenSeleccionada?.PuedePagar == true;
        public bool MostrarMovimientos
        {
            get => _mostrarMovimientos;
            set { _mostrarMovimientos = value; OnPropertyChanged(); OnPropertyChanged(nameof(MovimientosVisibility)); }
        }
        public Visibility MovimientosVisibility => MostrarMovimientos ? Visibility.Visible : Visibility.Collapsed;
        public string TituloMovimientos { get => _tituloMovimientos; set { _tituloMovimientos = value; OnPropertyChanged(); } }

        public DateTime Fecha { get => _fecha; set { _fecha = value; OnPropertyChanged(); } }
        public DateTime? FechaComprometida { get => _fechaComprometida; set { _fechaComprometida = value; OnPropertyChanged(); } }
        public ProveedorStock? ProveedorSeleccionado { get => _proveedorSeleccionado; set { _proveedorSeleccionado = value; OnPropertyChanged(); } }
        public TipoServicio? TipoServicioSeleccionado { get => _tipoServicioSeleccionado; set { _tipoServicioSeleccionado = value; OnPropertyChanged(); } }
        public Cliente? ClienteSeleccionado
        {
            get => _clienteSeleccionado;
            set
            {
                _clienteSeleccionado = value;
                OnPropertyChanged();
                if (value != null)
                {
                    _cliente = value.ClienteBusqueda;
                    OnPropertyChanged(nameof(Cliente));
                    OnPropertyChanged(nameof(MostrarCoincidenciasCliente));
                }
            }
        }
        public string Cliente
        {
            get => _cliente;
            set
            {
                _cliente = value ?? string.Empty;
                if (_clienteSeleccionado?.ClienteBusqueda != _cliente)
                {
                    _clienteSeleccionado = null;
                    OnPropertyChanged(nameof(ClienteSeleccionado));
                }
                OnPropertyChanged();
                if (_clienteSeleccionado == null)
                    ActualizarCoincidenciasCliente();
                OnPropertyChanged(nameof(MostrarCoincidenciasCliente));
            }
        }
        public string OciRelacionada { get => _ociRelacionada; set { _ociRelacionada = value ?? string.Empty; OnPropertyChanged(); } }
        public string OtRelacionada { get => _otRelacionada; set { _otRelacionada = value ?? string.Empty; OnPropertyChanged(); } }
        public string Responsable { get => _responsable; set { _responsable = value ?? string.Empty; OnPropertyChanged(); } }
        public string FormaPago { get => _formaPago; set { _formaPago = value ?? string.Empty; OnPropertyChanged(); } }
        public string FormaPagoTipo
        {
            get => _formaPagoTipo;
            set
            {
                _formaPagoTipo = string.IsNullOrWhiteSpace(value) ? "Contado" : value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EsContado));
                OnPropertyChanged(nameof(EsCredito));
                OnPropertyChanged(nameof(CreditoDetalleVisibility));
                ActualizarFormaPago();
            }
        }
        public string DetalleCredito
        {
            get => _detalleCredito;
            set
            {
                _detalleCredito = value ?? string.Empty;
                OnPropertyChanged();
                ActualizarFormaPago();
            }
        }
        public string ObservacionesInternas { get => _observacionesInternas; set { _observacionesInternas = value ?? string.Empty; OnPropertyChanged(); } }
        public string Observaciones { get => _observaciones; set { _observaciones = value ?? string.Empty; OnPropertyChanged(); } }
        public string DetalleProducto { get => _detalleProducto; set { _detalleProducto = value ?? string.Empty; OnPropertyChanged(); } }
        public string DetalleDescripcion { get => _detalleDescripcion; set { _detalleDescripcion = value ?? string.Empty; OnPropertyChanged(); } }
        public decimal DetalleCantidad { get => _detalleCantidad; set { _detalleCantidad = value; OnPropertyChanged(); OnPropertyChanged(nameof(DetalleTotal)); } }
        public string DetalleUnidad { get => _detalleUnidad; set { _detalleUnidad = value ?? "UND"; OnPropertyChanged(); } }
        public decimal DetallePrecioUnitario { get => _detallePrecioUnitario; set { _detallePrecioUnitario = value; OnPropertyChanged(); OnPropertyChanged(nameof(DetalleTotal)); } }
        public decimal DetalleTotal => Math.Round(DetalleCantidad * DetallePrecioUnitario, 2);
        public string DetalleBotonTexto => _detalleEnEdicion == null ? "+  Agregar" : "Actualizar";
        public decimal ACuenta
        {
            get => _aCuenta;
            set
            {
                _aCuenta = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SaldoPendienteFormulario));
            }
        }
        public decimal PagoImporte { get => _pagoImporte; set { _pagoImporte = value; OnPropertyChanged(); } }
        public string PagoMedio { get => _pagoMedio; set { _pagoMedio = value ?? string.Empty; OnPropertyChanged(); } }
        public string PagoOperacion { get => _pagoOperacion; set { _pagoOperacion = value ?? string.Empty; OnPropertyChanged(); } }
        public string PagoObservacion { get => _pagoObservacion; set { _pagoObservacion = value ?? string.Empty; OnPropertyChanged(); } }
        public string FotoTitulo { get => _fotoTitulo; set { _fotoTitulo = value ?? string.Empty; OnPropertyChanged(); } }
        public string FotoDescripcion { get => _fotoDescripcion; set { _fotoDescripcion = value ?? string.Empty; OnPropertyChanged(); } }
        public string FotoUbicacionPdf { get => _fotoUbicacionPdf; set { _fotoUbicacionPdf = value ?? "Abajo"; OnPropertyChanged(); } }
        public string DistribucionFotosPdf { get => _distribucionFotosPdf; set { _distribucionFotosPdf = string.IsNullOrWhiteSpace(value) ? "1 x 2" : value; OnPropertyChanged(); } }

        public TipoServicio? TipoSeleccionado
        {
            get => _tipoSeleccionado;
            set { _tipoSeleccionado = value; OnPropertyChanged(); CargarTipoEnFormulario(value); }
        }

        public string TipoCodigo { get => _tipoCodigo; set { _tipoCodigo = value ?? string.Empty; OnPropertyChanged(); } }
        public string TipoNombre { get => _tipoNombre; set { _tipoNombre = value ?? string.Empty; OnPropertyChanged(); } }
        public string TipoDescripcion { get => _tipoDescripcion; set { _tipoDescripcion = value ?? string.Empty; OnPropertyChanged(); } }
        public bool TipoRequiereEntrega { get => _tipoRequiereEntrega; set { _tipoRequiereEntrega = value; OnPropertyChanged(); } }
        public bool TipoEstado { get => _tipoEstado; set { _tipoEstado = value; OnPropertyChanged(); } }
        public bool MostrarNuevoProveedor
        {
            get => _mostrarNuevoProveedor;
            set { _mostrarNuevoProveedor = value; OnPropertyChanged(); OnPropertyChanged(nameof(NuevoProveedorVisibility)); }
        }
        public Visibility NuevoProveedorVisibility => MostrarNuevoProveedor ? Visibility.Visible : Visibility.Collapsed;
        public string NuevoProveedorTipoDocumento { get => _nuevoProveedorTipoDocumento; set { _nuevoProveedorTipoDocumento = value ?? "S/N"; OnPropertyChanged(); } }
        public string NuevoProveedorNumeroDocumento { get => _nuevoProveedorNumeroDocumento; set { _nuevoProveedorNumeroDocumento = value ?? string.Empty; OnPropertyChanged(); } }
        public string NuevoProveedorNombre { get => _nuevoProveedorNombre; set { _nuevoProveedorNombre = value ?? string.Empty; OnPropertyChanged(); } }
        public string NuevoProveedorDireccion { get => _nuevoProveedorDireccion; set { _nuevoProveedorDireccion = value ?? string.Empty; OnPropertyChanged(); } }
        public string NuevoProveedorTelefono { get => _nuevoProveedorTelefono; set { _nuevoProveedorTelefono = value ?? string.Empty; OnPropertyChanged(); } }
        public string NuevoProveedorCorreo { get => _nuevoProveedorCorreo; set { _nuevoProveedorCorreo = value ?? string.Empty; OnPropertyChanged(); } }
        public bool MostrarNuevoTipoServicio
        {
            get => _mostrarNuevoTipoServicio;
            set { _mostrarNuevoTipoServicio = value; OnPropertyChanged(); OnPropertyChanged(nameof(NuevoTipoServicioVisibility)); }
        }
        public Visibility NuevoTipoServicioVisibility => MostrarNuevoTipoServicio ? Visibility.Visible : Visibility.Collapsed;
        public string NuevoTipoCodigo { get => _nuevoTipoCodigo; set { _nuevoTipoCodigo = value ?? string.Empty; OnPropertyChanged(); } }
        public string NuevoTipoNombre { get => _nuevoTipoNombre; set { _nuevoTipoNombre = value ?? string.Empty; OnPropertyChanged(); } }
        public string NuevoTipoDescripcion { get => _nuevoTipoDescripcion; set { _nuevoTipoDescripcion = value ?? string.Empty; OnPropertyChanged(); } }
        public bool NuevoTipoRequiereEntrega { get => _nuevoTipoRequiereEntrega; set { _nuevoTipoRequiereEntrega = value; OnPropertyChanged(); } }

        public bool EsContado
        {
            get => FormaPagoTipo.Equals("Contado", StringComparison.OrdinalIgnoreCase);
            set
            {
                if (value)
                    FormaPagoTipo = "Contado";
            }
        }
        public bool EsCredito
        {
            get => FormaPagoTipo.Equals("Credito", StringComparison.OrdinalIgnoreCase);
            set
            {
                if (value)
                    FormaPagoTipo = "Credito";
            }
        }
        public Visibility CreditoDetalleVisibility => EsCredito ? Visibility.Visible : Visibility.Collapsed;
        public bool MostrarCoincidenciasCliente => ClienteSeleccionado == null && Cliente.Trim().Length >= 3 && ClientesCoincidencias.Count > 0;
        public Visibility FotoOrdenNuevaVisibility => _idOrdenServicio == 0 ? Visibility.Visible : Visibility.Collapsed;
        public decimal TotalFormulario => DetallesFormulario.Sum(x => x.Total);
        public decimal SaldoPendienteFormulario => Math.Max(0, TotalFormulario - ACuenta);
        public string Resumen => $"{Ordenes.Count} ordenes listadas";

        public ICommand NuevoCommand { get; }
        public ICommand VerCommand { get; }
        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }
        public ICommand EditarCommand { get; }
        public ICommand AprobarCommand { get; }
        public ICommand CopiarCommand { get; }
        public ICommand AnularCommand { get; }
        public ICommand ActualizarCommand { get; }
        public ICommand AgregarDetalleCommand { get; }
        public ICommand EditarDetalleCommand { get; }
        public ICommand QuitarDetalleCommand { get; }
        public ICommand RegistrarPagoCommand { get; }
        public ICommand HistorialCommand { get; }
        public ICommand ImprimirCommand { get; }
        public ICommand PrepararEntregaCommand { get; }
        public ICommand PrepararRecepcionCommand { get; }
        public ICommand ConfirmarMovimientoCommand { get; }
        public ICommand CancelarMovimientoCommand { get; }
        public ICommand AgregarFotoCommand { get; }
        public ICommand VerFotoCommand { get; }
        public ICommand EliminarFotoCommand { get; }
        public ICommand SubirFotoCommand { get; }
        public ICommand BajarFotoCommand { get; }
        public ICommand MostrarNuevoProveedorCommand { get; }
        public ICommand GuardarNuevoProveedorCommand { get; }
        public ICommand CancelarNuevoProveedorCommand { get; }
        public ICommand MostrarNuevoTipoServicioCommand { get; }
        public ICommand GuardarNuevoTipoServicioCommand { get; }
        public ICommand CancelarNuevoTipoServicioCommand { get; }
        public ICommand NuevoTipoCommand { get; }
        public ICommand GuardarTipoCommand { get; }

        public OrdenesServicioViewModel()
        {
            OrdenesVista = CollectionViewSource.GetDefaultView(Ordenes);
            NuevoCommand = new RelayCommand(_ => Nuevo());
            VerCommand = new RelayCommand(x => Ver(x as OrdenServicio));
            GuardarCommand = new RelayCommand(_ => Guardar());
            CancelarCommand = new RelayCommand(_ => Cancelar());
            EditarCommand = new RelayCommand(x => Editar(x as OrdenServicio));
            AprobarCommand = new RelayCommand(x => Aprobar(x as OrdenServicio));
            CopiarCommand = new RelayCommand(x => Copiar(x as OrdenServicio));
            AnularCommand = new RelayCommand(x => Anular(x as OrdenServicio));
            ActualizarCommand = new RelayCommand(_ => CargarTodo());
            AgregarDetalleCommand = new RelayCommand(_ => AgregarDetalle());
            EditarDetalleCommand = new RelayCommand(x => EditarDetalle(x as OrdenServicioDetalle));
            QuitarDetalleCommand = new RelayCommand(x => QuitarDetalle(x as OrdenServicioDetalle));
            RegistrarPagoCommand = new RelayCommand(x => RegistrarPago(x as OrdenServicio));
            HistorialCommand = new RelayCommand(x => VerHistorial(x as OrdenServicio));
            ImprimirCommand = new RelayCommand(x => Imprimir(x as OrdenServicio));
            PrepararEntregaCommand = new RelayCommand(x => PrepararEntrega(x as OrdenServicio));
            PrepararRecepcionCommand = new RelayCommand(x => PrepararRecepcion(x as OrdenServicio));
            ConfirmarMovimientoCommand = new RelayCommand(_ => ConfirmarMovimiento());
            CancelarMovimientoCommand = new RelayCommand(_ => MostrarMovimientos = false);
            AgregarFotoCommand = new RelayCommand(_ => AgregarFoto());
            VerFotoCommand = new RelayCommand(x => VerFoto(x as OrdenServicioFoto));
            EliminarFotoCommand = new RelayCommand(x => EliminarFoto(x as OrdenServicioFoto));
            SubirFotoCommand = new RelayCommand(x => MoverFoto(x as OrdenServicioFoto, -1));
            BajarFotoCommand = new RelayCommand(x => MoverFoto(x as OrdenServicioFoto, 1));
            MostrarNuevoProveedorCommand = new RelayCommand(_ => MostrarNuevoProveedor = true);
            GuardarNuevoProveedorCommand = new RelayCommand(_ => GuardarNuevoProveedor());
            CancelarNuevoProveedorCommand = new RelayCommand(_ => LimpiarNuevoProveedor());
            MostrarNuevoTipoServicioCommand = new RelayCommand(_ => MostrarNuevoTipoServicio = true);
            GuardarNuevoTipoServicioCommand = new RelayCommand(_ => GuardarNuevoTipoServicio());
            CancelarNuevoTipoServicioCommand = new RelayCommand(_ => LimpiarNuevoTipoServicio());
            NuevoTipoCommand = new RelayCommand(_ => LimpiarTipo());
            GuardarTipoCommand = new RelayCommand(_ => GuardarTipo());
            DetallesFormulario.CollectionChanged += (_, _) =>
            {
                NotificarTotales();
                RefrescarDetallesVista();
            };
            RefrescarDetallesVista();
            CargarTodo();
        }

        private void CargarTodo()
        {
            CargarCatalogos();
            CargarOrdenes();
        }

        private void CargarCatalogos()
        {
            Proveedores.Clear();
            foreach (ProveedorStock proveedor in _proveedorNegocio.Listar().Where(x => x.Estado))
                Proveedores.Add(proveedor);

            Clientes.Clear();
            foreach (Cliente cliente in _clienteNegocio.Listar().Where(x => x.Estado))
                Clientes.Add(cliente);

            TiposServicio.Clear();
            TiposListado.Clear();
            foreach (TipoServicio tipo in _negocio.ListarTiposServicio())
            {
                TiposListado.Add(tipo);
                if (tipo.Estado)
                    TiposServicio.Add(tipo);
            }
        }

        private void CargarOrdenes()
        {
            Ordenes.Clear();
            foreach (OrdenServicio orden in _negocio.Listar(TextoBusqueda, EstadoFiltro))
                Ordenes.Add(orden);
            OrdenesVista.Refresh();
            OnPropertyChanged(nameof(Resumen));
        }

        private void Nuevo()
        {
            _idOrdenServicio = 0;
            OrdenSeleccionada = null;
            Fecha = DateTime.Today;
            FechaComprometida = null;
            ProveedorSeleccionado = null;
            TipoServicioSeleccionado = null;
            ClienteSeleccionado = null;
            Cliente = string.Empty;
            OciRelacionada = string.Empty;
            OtRelacionada = string.Empty;
            Responsable = SessionManager.UsuarioActual?.NombreCompleto ?? string.Empty;
            FormaPagoTipo = "Contado";
            DetalleCredito = string.Empty;
            ACuenta = 0;
            ObservacionesInternas = string.Empty;
            Observaciones = string.Empty;
            DistribucionFotosPdf = "1 x 2";
            DetalleProducto = string.Empty;
            DetalleDescripcion = string.Empty;
            DetalleCantidad = 1;
            DetalleUnidad = "UND";
            DetallePrecioUnitario = 0;
            _detalleEnEdicion = null;
            OnPropertyChanged(nameof(DetalleBotonTexto));
            DetallesFormulario.Clear();
            LimpiarNuevoProveedor();
            LimpiarNuevoTipoServicio();
            MostrarFormulario = true;
            OnPropertyChanged(nameof(TituloFormulario));
            OnPropertyChanged(nameof(FotoOrdenNuevaVisibility));
            AbrirEditor();
        }

        private void Editar(OrdenServicio? orden)
        {
            if (orden == null)
            {
                NotificationService.Warning("Debe seleccionar una orden.");
                return;
            }
            OrdenServicio? completa = _negocio.Obtener(orden.IdOrdenServicio);
            if (completa == null)
            {
                NotificationService.Warning("No se encontro la orden seleccionada.");
                return;
            }
            if (!completa.PuedeEditar)
            {
                NotificationService.Warning("Solo se puede editar una orden en estado Borrador.");
                return;
            }

            _idOrdenServicio = completa.IdOrdenServicio;
            OrdenSeleccionada = completa;
            Fecha = completa.Fecha;
            FechaComprometida = completa.FechaComprometida;
            ProveedorSeleccionado = Proveedores.FirstOrDefault(x => x.IdProveedor == completa.IdProveedor);
            TipoServicioSeleccionado = TiposServicio.FirstOrDefault(x => x.IdTipoServicio == completa.IdTipoServicio);
            ClienteSeleccionado = null;
            Cliente = completa.Cliente;
            OciRelacionada = completa.OciRelacionada;
            OtRelacionada = completa.OtRelacionada;
            Responsable = completa.Responsable;
            CargarFormaPago(completa.FormaPago);
            ACuenta = 0;
            ObservacionesInternas = completa.ObservacionesInternas;
            Observaciones = completa.Observaciones;
            DistribucionFotosPdf = completa.DistribucionFotosPdf;
            DetalleProducto = string.Empty;
            DetalleDescripcion = string.Empty;
            DetalleCantidad = 1;
            DetalleUnidad = "UND";
            DetallePrecioUnitario = 0;
            _detalleEnEdicion = null;
            OnPropertyChanged(nameof(DetalleBotonTexto));
            DetallesFormulario.Clear();
            foreach (OrdenServicioDetalle detalle in completa.Detalles)
                DetallesFormulario.Add(detalle);
            LimpiarNuevoProveedor();
            LimpiarNuevoTipoServicio();
            MostrarFormulario = true;
            OnPropertyChanged(nameof(TituloFormulario));
            OnPropertyChanged(nameof(FotoOrdenNuevaVisibility));
            AbrirEditor();
        }

        private void Guardar()
        {
            OrdenServicio orden = new()
            {
                IdOrdenServicio = _idOrdenServicio,
                Fecha = Fecha,
                FechaComprometida = FechaComprometida,
                IdProveedor = ProveedorSeleccionado?.IdProveedor ?? 0,
                IdTipoServicio = TipoServicioSeleccionado?.IdTipoServicio ?? 0,
                Cliente = Cliente,
                OciRelacionada = OciRelacionada,
                OtRelacionada = OtRelacionada,
                Responsable = Responsable,
                FormaPago = FormaPago,
                ObservacionesInternas = ObservacionesInternas,
                DistribucionFotosPdf = DistribucionFotosPdf,
                ACuenta = ACuenta,
                Observaciones = Observaciones,
                UsuarioRegistro = SessionManager.UsuarioActual?.NombreCompleto ?? "Sistema",
                Detalles = DetallesFormulario.ToList()
            };

            string mensaje = _negocio.Guardar(orden);
            MostrarResultado(mensaje);
            if (mensaje.Contains("correctamente", StringComparison.OrdinalIgnoreCase))
            {
                RegistrarFotosPendientes(orden);
                Cancelar();
                CargarOrdenes();
            }
        }

        private void AgregarDetalle()
        {
            OrdenServicioDetalle detalle = new()
            {
                IdOrdenServicioDetalle = _detalleEnEdicion?.IdOrdenServicioDetalle ?? 0,
                IdOrdenServicio = _detalleEnEdicion?.IdOrdenServicio ?? 0,
                IdProducto = _detalleEnEdicion?.IdProducto,
                Producto = DetalleProducto,
                Descripcion = string.IsNullOrWhiteSpace(DetalleDescripcion) ? DetalleProducto : DetalleDescripcion.Trim(),
                Cantidad = DetalleCantidad,
                Unidad = DetalleUnidad,
                PrecioUnitario = DetallePrecioUnitario,
                Total = Math.Round(DetalleCantidad * DetallePrecioUnitario, 2),
                Observaciones = _detalleEnEdicion?.Observaciones ?? string.Empty
            };

            if (string.IsNullOrWhiteSpace(detalle.Producto) || detalle.Cantidad <= 0)
            {
                NotificationService.Warning("Ingrese producto y cantidad valida.");
                return;
            }

            if (_detalleEnEdicion == null)
            {
                DetallesFormulario.Add(detalle);
            }
            else
            {
                int index = DetallesFormulario.IndexOf(_detalleEnEdicion);
                if (index >= 0)
                {
                    DetallesFormulario.RemoveAt(index);
                    DetallesFormulario.Insert(index, detalle);
                }
                _detalleEnEdicion = null;
            }
            DetalleProducto = string.Empty;
            DetalleDescripcion = string.Empty;
            DetalleCantidad = 1;
            DetalleUnidad = "UND";
            DetallePrecioUnitario = 0;
            OnPropertyChanged(nameof(DetalleBotonTexto));
        }

        private void EditarDetalle(OrdenServicioDetalle? detalle)
        {
            if (detalle == null || detalle.EsFilaRelleno)
                return;

            _detalleEnEdicion = detalle;
            DetalleProducto = detalle.Producto;
            DetalleDescripcion = detalle.Descripcion;
            DetalleCantidad = detalle.Cantidad;
            DetalleUnidad = detalle.Unidad;
            DetallePrecioUnitario = detalle.PrecioUnitario;
            OnPropertyChanged(nameof(DetalleBotonTexto));
        }

        private void QuitarDetalle(OrdenServicioDetalle? detalle)
        {
            if (detalle != null && !detalle.EsFilaRelleno)
            {
                if (ReferenceEquals(_detalleEnEdicion, detalle))
                {
                    _detalleEnEdicion = null;
                    DetalleProducto = string.Empty;
                    DetalleDescripcion = string.Empty;
                    DetalleCantidad = 1;
                    DetalleUnidad = "UND";
                    DetallePrecioUnitario = 0;
                    OnPropertyChanged(nameof(DetalleBotonTexto));
                }
                DetallesFormulario.Remove(detalle);
            }
        }

        private void Aprobar(OrdenServicio? orden)
        {
            if (orden == null)
                return;

            OrdenServicioAprobacionWindow ventana = new(SessionManager.UsuarioActual?.NombreUsuario ?? string.Empty)
            {
                Owner = Application.Current.MainWindow
            };

            if (ventana.ShowDialog() != true || !ventana.Confirmado)
                return;

            string validacion = _usuarioNegocio.ValidarAprobacionOs(ventana.UsuarioAprobador, ventana.ClaveAprobacion);
            if (!validacion.Equals("OK", StringComparison.OrdinalIgnoreCase))
            {
                NotificationService.Warning(validacion);
                return;
            }

            string mensaje = _negocio.Aprobar(orden.IdOrdenServicio, ventana.UsuarioAprobador, ventana.ClaveAprobacion);
            MostrarResultado(mensaje);
            CargarOrdenes();
        }

        private void Copiar(OrdenServicio? orden)
        {
            if (orden == null)
                return;

            OrdenServicio? completa = _negocio.Obtener(orden.IdOrdenServicio);
            if (completa == null)
            {
                NotificationService.Warning("No se encontro la orden a copiar.");
                return;
            }

            _idOrdenServicio = 0;
            OrdenSeleccionada = null;
            Fecha = DateTime.Today;
            FechaComprometida = completa.FechaComprometida;
            ProveedorSeleccionado = Proveedores.FirstOrDefault(x => x.IdProveedor == completa.IdProveedor);
            TipoServicioSeleccionado = TiposServicio.FirstOrDefault(x => x.IdTipoServicio == completa.IdTipoServicio);
            ClienteSeleccionado = null;
            Cliente = completa.Cliente;
            OciRelacionada = completa.OciRelacionada;
            OtRelacionada = completa.OtRelacionada;
            Responsable = SessionManager.UsuarioActual?.NombreCompleto ?? completa.Responsable;
            CargarFormaPago(completa.FormaPago);
            ACuenta = 0;
            ObservacionesInternas = completa.ObservacionesInternas;
            Observaciones = completa.Observaciones;
            DistribucionFotosPdf = completa.DistribucionFotosPdf;
            DetalleProducto = string.Empty;
            DetalleDescripcion = string.Empty;
            DetalleCantidad = 1;
            DetalleUnidad = "UND";
            DetallePrecioUnitario = 0;
            _detalleEnEdicion = null;
            OnPropertyChanged(nameof(DetalleBotonTexto));
            DetallesFormulario.Clear();
            foreach (OrdenServicioDetalle detalle in completa.Detalles)
            {
                DetallesFormulario.Add(new OrdenServicioDetalle
                {
                    Producto = detalle.Producto,
                    Descripcion = detalle.Descripcion,
                    Cantidad = detalle.Cantidad,
                    Unidad = detalle.Unidad,
                    PrecioUnitario = detalle.PrecioUnitario,
                    Total = Math.Round(detalle.Cantidad * detalle.PrecioUnitario, 2),
                    Observaciones = detalle.Observaciones
                });
            }

            FotosOrdenSeleccionada.Clear();
            CopiarFotosParaNuevaOrden(completa);
            MostrarFormulario = true;
            OnPropertyChanged(nameof(TituloFormulario));
            OnPropertyChanged(nameof(FotoOrdenNuevaVisibility));
            AbrirEditor();
        }

        private void Anular(OrdenServicio? orden)
        {
            if (orden == null)
                return;
            if (!ConfirmDialogService.Confirmar("¿Desea anular la orden de servicio seleccionada?", "Anular orden"))
                return;
            string mensaje = _negocio.Anular(orden.IdOrdenServicio, SessionManager.UsuarioActual?.NombreCompleto ?? "Sistema", "Anulado desde el modulo de ordenes de servicio");
            MostrarResultado(mensaje);
            CargarOrdenes();
        }

        private void Ver(OrdenServicio? orden)
        {
            if (orden == null)
            {
                NotificationService.Warning("Debe seleccionar una orden.");
                return;
            }

            OrdenSeleccionada = orden;
            OrdenServicio? completa = _negocio.Obtener(orden.IdOrdenServicio);
            if (completa == null)
            {
                NotificationService.Warning("No se encontro la orden seleccionada.");
                return;
            }

            OrdenServicioDetalleWindow ventana = new(completa)
            {
                Owner = Application.Current.MainWindow
            };
            ventana.ShowDialog();
        }

        private void RegistrarPago(OrdenServicio? orden = null)
        {
            if (orden != null)
                OrdenSeleccionada = orden;

            if (OrdenSeleccionada == null)
            {
                NotificationService.Warning("Seleccione una orden para registrar el pago.");
                return;
            }

            OrdenServicio? completa = _negocio.Obtener(OrdenSeleccionada.IdOrdenServicio);
            if (completa == null)
            {
                NotificationService.Warning("No se encontro la orden seleccionada.");
                return;
            }

            if (!completa.PuedePagar)
            {
                NotificationService.Warning("La orden no tiene saldo pendiente para pago.");
                return;
            }

            OrdenServicioPagoWindow ventana = new(completa)
            {
                Owner = Application.Current.MainWindow
            };

            if (ventana.ShowDialog() != true || !ventana.Confirmado || ventana.Pago == null)
                return;

            ventana.Pago.UsuarioRegistro = SessionManager.UsuarioActual?.NombreCompleto ?? "Sistema";
            OrdenServicioPago pago = new()
            {
                IdOrdenServicio = ventana.Pago.IdOrdenServicio,
                Fecha = ventana.Pago.Fecha,
                TipoPago = ventana.Pago.TipoPago,
                Importe = ventana.Pago.Importe,
                MedioPago = ventana.Pago.MedioPago,
                NumeroOperacion = ventana.Pago.NumeroOperacion,
                Observacion = ventana.Pago.Observacion,
                UsuarioRegistro = ventana.Pago.UsuarioRegistro
            };
            string mensaje = _negocio.RegistrarPago(pago);
            MostrarResultado(mensaje);
            if (mensaje.Contains("correctamente", StringComparison.OrdinalIgnoreCase))
            {
                PagoImporte = 0;
                PagoOperacion = string.Empty;
                PagoObservacion = string.Empty;
                CargarOrdenes();
            }
        }

        private void VerHistorial(OrdenServicio? orden)
        {
            if (orden == null)
            {
                NotificationService.Warning("Seleccione una orden.");
                return;
            }

            OrdenServicio? completa = _negocio.Obtener(orden.IdOrdenServicio);
            if (completa == null)
            {
                NotificationService.Warning("No se encontro la orden seleccionada.");
                return;
            }

            OrdenServicioDetalleWindow ventana = new(completa, tabSeleccionado: 4)
            {
                Owner = Application.Current.MainWindow
            };
            ventana.ShowDialog();
        }

        private void Imprimir(OrdenServicio? orden)
        {
            if (orden == null)
            {
                NotificationService.Warning("Seleccione una orden para imprimir.");
                return;
            }

            OrdenServicio? completa = _negocio.Obtener(orden.IdOrdenServicio);
            if (completa == null)
            {
                NotificationService.Warning("No se encontro la orden seleccionada.");
                return;
            }

            SaveFileDialog dialog = new()
            {
                Title = "Guardar Orden de Servicio",
                FileName = $"OrdenServicio_{completa.NumeroOrden}.pdf",
                Filter = "PDF|*.pdf"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                Empresa empresa = new EmpresaNegocio().ObtenerPredeterminada() ?? new Empresa { Nombre = "Delta Confecciones" };
                OrdenServicioPdfExporter.Exportar(dialog.FileName, empresa, completa, incluirFotos: true);
                NotificationService.Success("PDF generado correctamente.");
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudo generar el PDF: {ex.Message}");
            }
        }

        private void PrepararEntrega(OrdenServicio? orden)
        {
            if (orden == null)
            {
                NotificationService.Warning("Seleccione una orden.");
                return;
            }

            if (!orden.RequiereEntrega)
            {
                NotificationService.Warning("El tipo de servicio no requiere entrega al proveedor.");
                return;
            }

            MovimientosFormulario.Clear();
            foreach (OrdenServicioMovimiento item in _negocio.PrepararEntrega(orden.IdOrdenServicio))
                MovimientosFormulario.Add(item);
            _tipoMovimientoActual = "Entrega";
            TituloMovimientos = $"Registrar entrega - {orden.NumeroOrden}";
            MostrarMovimientos = true;
        }

        private void PrepararRecepcion(OrdenServicio? orden)
        {
            if (orden == null)
            {
                NotificationService.Warning("Seleccione una orden.");
                return;
            }

            MovimientosFormulario.Clear();
            foreach (OrdenServicioMovimiento item in _negocio.PrepararRecepcion(orden.IdOrdenServicio))
                MovimientosFormulario.Add(item);
            _tipoMovimientoActual = "Recepcion";
            TituloMovimientos = $"Registrar recepcion - {orden.NumeroOrden}";
            MostrarMovimientos = true;
        }

        private void ConfirmarMovimiento()
        {
            if (OrdenSeleccionada == null)
            {
                NotificationService.Warning("Seleccione una orden.");
                return;
            }

            string usuario = SessionManager.UsuarioActual?.NombreCompleto ?? "Sistema";
            string mensaje = _tipoMovimientoActual.Equals("Entrega", StringComparison.OrdinalIgnoreCase)
                ? _negocio.RegistrarEntrega(OrdenSeleccionada.IdOrdenServicio, MovimientosFormulario, usuario)
                : _negocio.RegistrarRecepcion(OrdenSeleccionada.IdOrdenServicio, MovimientosFormulario, usuario);

            MostrarResultado(mensaje);
            if (mensaje.Contains("correctamente", StringComparison.OrdinalIgnoreCase))
            {
                MostrarMovimientos = false;
                CargarOrdenes();
            }
        }

        private void AgregarFoto()
        {
            OpenFileDialog dialog = new()
            {
                Title = "Seleccionar fotografia",
                Filter = "Imagenes|*.jpg;*.jpeg;*.png;*.bmp|Todos los archivos|*.*"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                bool esPendiente = OrdenSeleccionada == null || _idOrdenServicio == 0;
                string carpeta = esPendiente
                    ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OrdenesServicioFotos", "Pendientes")
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OrdenesServicioFotos", OrdenSeleccionada!.NumeroOrden);
                Directory.CreateDirectory(carpeta);
                string nombre = $"{DateTime.Now:yyyyMMddHHmmssfff}_{Path.GetFileName(dialog.FileName)}";
                string destino = Path.Combine(carpeta, nombre);
                File.Copy(dialog.FileName, destino, overwrite: true);

                OrdenServicioFoto foto = new()
                {
                    IdOrdenServicio = esPendiente ? 0 : OrdenSeleccionada!.IdOrdenServicio,
                    RutaArchivo = destino,
                    NombreArchivo = nombre,
                    Titulo = FotoTitulo,
                    UbicacionPdf = FotoUbicacionPdf,
                    Descripcion = FotoDescripcion,
                    Orden = FotosOrdenSeleccionada.Count + 1,
                    UsuarioRegistro = SessionManager.UsuarioActual?.NombreCompleto ?? "Sistema"
                };

                if (esPendiente)
                {
                    FotosOrdenSeleccionada.Add(foto);
                    NotificationService.Success("Foto agregada al formulario. Se registrara al guardar la orden.");
                }
                else
                {
                    string mensaje = _negocio.RegistrarFoto(foto);
                    MostrarResultado(mensaje);
                    RefrescarOrdenSeleccionada();
                }

                FotoTitulo = string.Empty;
                FotoDescripcion = string.Empty;
                FotoUbicacionPdf = "Abajo";
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudo copiar la imagen: {ex.Message}");
            }
        }

        private static void VerFoto(OrdenServicioFoto? foto)
        {
            if (foto == null || !File.Exists(foto.RutaArchivo))
            {
                NotificationService.Warning("No se encontro la imagen.");
                return;
            }

            Process.Start(new ProcessStartInfo(foto.RutaArchivo) { UseShellExecute = true });
        }

        private void EliminarFoto(OrdenServicioFoto? foto)
        {
            if (foto == null)
                return;
            if (foto.IdOrdenServicioFoto == 0)
            {
                FotosOrdenSeleccionada.Remove(foto);
                try
                {
                    if (File.Exists(foto.RutaArchivo))
                        File.Delete(foto.RutaArchivo);
                }
                catch
                {
                    // Si el archivo temporal esta bloqueado, basta con quitarlo del formulario.
                }
                return;
            }
            string mensaje = _negocio.EliminarFoto(foto.IdOrdenServicioFoto, SessionManager.UsuarioActual?.NombreCompleto ?? "Sistema");
            MostrarResultado(mensaje);
            RefrescarOrdenSeleccionada();
        }

        private void MoverFoto(OrdenServicioFoto? foto, int direccion)
        {
            if (foto == null)
                return;

            int indiceActual = FotosOrdenSeleccionada.IndexOf(foto);
            int indiceNuevo = indiceActual + direccion;
            if (indiceActual < 0 || indiceNuevo < 0 || indiceNuevo >= FotosOrdenSeleccionada.Count)
                return;

            FotosOrdenSeleccionada.Move(indiceActual, indiceNuevo);
            ActualizarOrdenFotosLocales();
            if (_idOrdenServicio > 0 && FotosOrdenSeleccionada.All(x => x.IdOrdenServicioFoto > 0))
            {
                string mensaje = _negocio.ActualizarOrdenFotos(_idOrdenServicio, FotosOrdenSeleccionada, SessionManager.UsuarioActual?.NombreCompleto ?? "Sistema");
                if (!mensaje.Contains("correctamente", StringComparison.OrdinalIgnoreCase))
                    NotificationService.Warning(mensaje);
            }
        }

        private void ActualizarOrdenFotosLocales()
        {
            for (int i = 0; i < FotosOrdenSeleccionada.Count; i++)
                FotosOrdenSeleccionada[i].Orden = i + 1;
        }

        private void CopiarFotosParaNuevaOrden(OrdenServicio origen)
        {
            if (origen.Fotos.Count == 0)
                return;

            string carpetaPendientes = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OrdenesServicioFotos", "Pendientes");
            Directory.CreateDirectory(carpetaPendientes);
            int omitidas = 0;

            foreach (OrdenServicioFoto fotoOrigen in origen.Fotos
                         .OrderBy(x => x.Orden <= 0 ? int.MaxValue : x.Orden)
                         .ThenBy(x => x.IdOrdenServicioFoto))
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(fotoOrigen.RutaArchivo) || !File.Exists(fotoOrigen.RutaArchivo))
                    {
                        omitidas++;
                        continue;
                    }

                    string extension = Path.GetExtension(fotoOrigen.RutaArchivo);
                    string nombreBase = Path.GetFileNameWithoutExtension(fotoOrigen.NombreArchivo);
                    if (string.IsNullOrWhiteSpace(nombreBase))
                        nombreBase = Path.GetFileNameWithoutExtension(fotoOrigen.RutaArchivo);
                    string nombre = $"{DateTime.Now:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}_{nombreBase}{extension}";
                    string destino = Path.Combine(carpetaPendientes, nombre);
                    File.Copy(fotoOrigen.RutaArchivo, destino, overwrite: false);

                    FotosOrdenSeleccionada.Add(new OrdenServicioFoto
                    {
                        IdOrdenServicio = 0,
                        RutaArchivo = destino,
                        NombreArchivo = nombre,
                        Titulo = fotoOrigen.Titulo,
                        UbicacionPdf = string.IsNullOrWhiteSpace(fotoOrigen.UbicacionPdf) ? "Abajo" : fotoOrigen.UbicacionPdf,
                        Descripcion = fotoOrigen.Descripcion,
                        Orden = FotosOrdenSeleccionada.Count + 1,
                        UsuarioRegistro = SessionManager.UsuarioActual?.NombreCompleto ?? "Sistema"
                    });
                }
                catch
                {
                    omitidas++;
                }
            }

            if (omitidas > 0)
                NotificationService.Warning($"Se copio la OS, pero {omitidas} foto(s) no se encontraron o no se pudieron copiar.");
        }

        private void RefrescarOrdenSeleccionada()
        {
            if (OrdenSeleccionada == null)
                return;
            OrdenSeleccionada = _negocio.Obtener(OrdenSeleccionada.IdOrdenServicio) ?? OrdenSeleccionada;
            RefrescarFotosSeleccionadas();
        }

        private void RefrescarFotosSeleccionadas()
        {
            FotosOrdenSeleccionada.Clear();
            if (OrdenSeleccionada == null)
                return;

            OrdenServicio? completa = _negocio.Obtener(OrdenSeleccionada.IdOrdenServicio);
            if (completa == null)
                return;
            foreach (OrdenServicioFoto foto in completa.Fotos)
                FotosOrdenSeleccionada.Add(foto);
        }

        private void RegistrarFotosPendientes(OrdenServicio orden)
        {
            if (orden.IdOrdenServicio <= 0)
                return;

            ActualizarOrdenFotosLocales();
            foreach (OrdenServicioFoto foto in FotosOrdenSeleccionada.Where(x => x.IdOrdenServicioFoto == 0).ToList())
            {
                try
                {
                    string carpeta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OrdenesServicioFotos", orden.NumeroOrden);
                    Directory.CreateDirectory(carpeta);
                    string nombre = string.IsNullOrWhiteSpace(foto.NombreArchivo)
                        ? $"{DateTime.Now:yyyyMMddHHmmssfff}_{Path.GetFileName(foto.RutaArchivo)}"
                        : foto.NombreArchivo;
                    string destino = Path.Combine(carpeta, nombre);
                    if (!foto.RutaArchivo.Equals(destino, StringComparison.OrdinalIgnoreCase))
                    {
                        if (File.Exists(destino))
                            destino = Path.Combine(carpeta, $"{DateTime.Now:yyyyMMddHHmmssfff}_{nombre}");
                        File.Move(foto.RutaArchivo, destino);
                    }

                    foto.IdOrdenServicio = orden.IdOrdenServicio;
                    foto.RutaArchivo = destino;
                    foto.NombreArchivo = Path.GetFileName(destino);
                    foto.UsuarioRegistro = SessionManager.UsuarioActual?.NombreCompleto ?? "Sistema";
                    string mensajeFoto = _negocio.RegistrarFoto(foto);
                    if (!mensajeFoto.Contains("correctamente", StringComparison.OrdinalIgnoreCase))
                        NotificationService.Warning(mensajeFoto);
                }
                catch (Exception ex)
                {
                    NotificationService.Warning($"La orden se guardo, pero no se pudo registrar una foto: {ex.Message}");
                }
            }
        }

        private void Cancelar()
        {
            MostrarFormulario = false;
            _idOrdenServicio = 0;
            OnPropertyChanged(nameof(TituloFormulario));
            OnPropertyChanged(nameof(FotoOrdenNuevaVisibility));
        }

        private void AbrirEditor()
        {
            try
            {
                OrdenServicioEditorWindow ventana = new(this)
                {
                    Owner = Application.Current.MainWindow
                };
                ventana.ShowDialog();
            }
            catch (Exception ex)
            {
                MostrarFormulario = false;
                NotificationService.Error($"No se pudo abrir la orden de servicio: {ex.Message}");
            }
        }

        private void CargarTipoEnFormulario(TipoServicio? tipo)
        {
            if (tipo == null)
                return;
            _tipoId = tipo.IdTipoServicio;
            TipoCodigo = tipo.Codigo;
            TipoNombre = tipo.Nombre;
            TipoDescripcion = tipo.Descripcion;
            TipoRequiereEntrega = tipo.RequiereEntrega;
            TipoEstado = tipo.Estado;
        }

        private void LimpiarTipo()
        {
            _tipoId = 0;
            TipoSeleccionado = null;
            TipoCodigo = string.Empty;
            TipoNombre = string.Empty;
            TipoDescripcion = string.Empty;
            TipoRequiereEntrega = false;
            TipoEstado = true;
        }

        private void GuardarTipo()
        {
            TipoServicio tipo = new()
            {
                IdTipoServicio = _tipoId,
                Codigo = TipoCodigo,
                Nombre = TipoNombre,
                Descripcion = TipoDescripcion,
                RequiereEntrega = TipoRequiereEntrega,
                Estado = TipoEstado
            };
            string mensaje = _negocio.GuardarTipoServicio(tipo);
            MostrarResultado(mensaje);
            CargarCatalogos();
            LimpiarTipo();
        }

        private void GuardarNuevoProveedor()
        {
            ProveedorStock proveedor = new()
            {
                TipoDocumento = string.IsNullOrWhiteSpace(NuevoProveedorTipoDocumento) ? "S/N" : NuevoProveedorTipoDocumento,
                NumeroDocumento = NuevoProveedorNumeroDocumento,
                NombreRazonSocial = NuevoProveedorNombre,
                Direccion = NuevoProveedorDireccion,
                Telefono = NuevoProveedorTelefono,
                Correo = NuevoProveedorCorreo,
                Estado = true
            };

            string mensaje = _proveedorNegocio.Guardar(proveedor);
            MostrarResultado(mensaje);
            if (!mensaje.Contains("correctamente", StringComparison.OrdinalIgnoreCase))
                return;

            string numero = NuevoProveedorNumeroDocumento.Trim();
            string nombre = NuevoProveedorNombre.Trim();
            CargarCatalogos();
            ProveedorSeleccionado = Proveedores.FirstOrDefault(x =>
                (!string.IsNullOrWhiteSpace(numero) && x.NumeroDocumento.Equals(numero, StringComparison.OrdinalIgnoreCase)) ||
                x.NombreRazonSocial.Equals(nombre, StringComparison.OrdinalIgnoreCase));
            LimpiarNuevoProveedor();
        }

        private void LimpiarNuevoProveedor()
        {
            MostrarNuevoProveedor = false;
            NuevoProveedorTipoDocumento = "S/N";
            NuevoProveedorNumeroDocumento = string.Empty;
            NuevoProveedorNombre = string.Empty;
            NuevoProveedorDireccion = string.Empty;
            NuevoProveedorTelefono = string.Empty;
            NuevoProveedorCorreo = string.Empty;
        }

        private void GuardarNuevoTipoServicio()
        {
            TipoServicio tipo = new()
            {
                Codigo = GenerarCodigoTipoServicio(NuevoTipoNombre),
                Nombre = NuevoTipoNombre,
                Descripcion = string.Empty,
                RequiereEntrega = false,
                Estado = true
            };

            string mensaje = _negocio.GuardarTipoServicio(tipo);
            MostrarResultado(mensaje);
            if (!mensaje.Contains("correctamente", StringComparison.OrdinalIgnoreCase))
                return;

            string codigo = NuevoTipoCodigo.Trim();
            string nombre = NuevoTipoNombre.Trim();
            CargarCatalogos();
            TipoServicioSeleccionado = TiposServicio.FirstOrDefault(x =>
                (!string.IsNullOrWhiteSpace(codigo) && x.Codigo.Equals(codigo, StringComparison.OrdinalIgnoreCase)) ||
                x.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase));
            LimpiarNuevoTipoServicio();
        }

        private void LimpiarNuevoTipoServicio()
        {
            MostrarNuevoTipoServicio = false;
            NuevoTipoCodigo = string.Empty;
            NuevoTipoNombre = string.Empty;
            NuevoTipoDescripcion = string.Empty;
            NuevoTipoRequiereEntrega = false;
        }

        private static string GenerarCodigoTipoServicio(string nombre)
        {
            string limpio = new string((nombre ?? string.Empty)
                .ToUpperInvariant()
                .Where(char.IsLetterOrDigit)
                .Take(18)
                .ToArray());
            return string.IsNullOrWhiteSpace(limpio) ? "SERVICIO" : limpio;
        }

        private void NotificarTotales()
        {
            OnPropertyChanged(nameof(TotalFormulario));
            OnPropertyChanged(nameof(SaldoPendienteFormulario));
        }

        private void RefrescarDetallesVista()
        {
            DetallesFormularioVista.Clear();
            foreach (OrdenServicioDetalle detalle in DetallesFormulario)
                DetallesFormularioVista.Add(detalle);

            int filasRelleno = Math.Max(0, 4 - DetallesFormularioVista.Count);
            for (int i = 0; i < filasRelleno; i++)
                DetallesFormularioVista.Add(new OrdenServicioDetalle { EsFilaRelleno = true });
        }

        public void RecalcularDetallesFormulario()
        {
            foreach (OrdenServicioDetalle detalle in DetallesFormulario)
                detalle.Total = Math.Round(detalle.Cantidad * detalle.PrecioUnitario, 2);
            NotificarTotales();
            RefrescarDetallesVista();
        }

        private void ActualizarCoincidenciasCliente()
        {
            ClientesCoincidencias.Clear();
            string filtro = Cliente.Trim();
            if (filtro.Length < 3)
                return;

            foreach (Cliente cliente in Clientes
                         .Where(x => Contiene(x.NombreRazonSocial, filtro) || Contiene(x.NumeroDocumento, filtro))
                         .Take(20))
            {
                ClientesCoincidencias.Add(cliente);
            }
        }

        private static bool Contiene(string valor, string filtro) =>
            !string.IsNullOrWhiteSpace(valor) && valor.Contains(filtro, StringComparison.OrdinalIgnoreCase);

        private void ActualizarFormaPago()
        {
            FormaPago = EsCredito && !string.IsNullOrWhiteSpace(DetalleCredito)
                ? $"Credito: {DetalleCredito.Trim()}"
                : FormaPagoTipo;
        }

        private void CargarFormaPago(string formaPago)
        {
            if (formaPago.StartsWith("Credito:", StringComparison.OrdinalIgnoreCase))
            {
                FormaPagoTipo = "Credito";
                DetalleCredito = formaPago["Credito:".Length..].Trim();
                return;
            }

            FormaPagoTipo = formaPago.Equals("Credito", StringComparison.OrdinalIgnoreCase) ? "Credito" : "Contado";
            DetalleCredito = string.Empty;
        }

        private static void MostrarResultado(string mensaje)
        {
            if (mensaje.Contains("correctamente", StringComparison.OrdinalIgnoreCase))
                NotificationService.Success(mensaje);
            else
                NotificationService.Warning(mensaje);
        }
    }
}
