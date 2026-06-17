using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Almacen.ViewModels
{
    public class IngresoManualStockEditorViewModel : BaseViewModel
    {
        private readonly IngresoManualStockNegocio _negocio = new();
        private readonly IngresoManualStock? _ingresoOriginal;
        private DateTime? _fechaEmision = DateTime.Today;
        private ProveedorStock? _proveedorSeleccionado;
        private TipoDocumentoStock? _tipoDocumentoSeleccionado;
        private string _tipoNumeracion = "Automatica";
        private string _serie = "IMS01";
        private string _numero = "Automatico";
        private AlmacenStock? _almacenSeleccionado;
        private string _observacion = string.Empty;
        private bool _isSaving;
        private Visibility _proveedorRapidoVisibility = Visibility.Collapsed;

        public IngresoManualStockEditorViewModel(IngresoManualStock? ingreso)
        {
            _ingresoOriginal = ingreso;
            Titulo = ingreso == null ? "Nuevo Ingreso Manual de Stock" : "Editar Ingreso Manual de Stock";

            GuardarCommand = new RelayCommand(_ => Guardar(), _ => !IsSaving);
            CancelarCommand = new RelayCommand(_ => CerrarVentana?.Invoke());
            AgregarProductoCommand = new RelayCommand(_ => AgregarProducto());
            QuitarProductoCommand = new RelayCommand(parametro => QuitarProducto(parametro));
            MostrarProveedorRapidoCommand = new RelayCommand(_ => ProveedorRapidoVisibility = Visibility.Visible);
            CancelarProveedorRapidoCommand = new RelayCommand(_ => LimpiarProveedorRapido());
            GuardarProveedorRapidoCommand = new RelayCommand(_ => GuardarProveedorRapido());

            if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                CargarCombos();
                CargarIngreso(ingreso);
            }
        }

        public string Titulo { get; }
        public bool Guardado { get; private set; }
        public Action? CerrarVentana { get; set; }

        public ObservableCollection<ProveedorStock> Proveedores { get; } = [];
        public ObservableCollection<AlmacenStock> Almacenes { get; } = [];
        public ObservableCollection<TipoDocumentoStock> TiposDocumento { get; } = [];
        public ObservableCollection<string> TiposNumeracion { get; } = ["Automatica", "Manual"];
        public ObservableCollection<IngresoManualStockDetalleViewModel> Detalles { get; } = [];

        public DateTime? FechaEmision
        {
            get => _fechaEmision;
            set { _fechaEmision = value; OnPropertyChanged(); }
        }

        public ProveedorStock? ProveedorSeleccionado
        {
            get => _proveedorSeleccionado;
            set { _proveedorSeleccionado = value; OnPropertyChanged(); }
        }

        public TipoDocumentoStock? TipoDocumentoSeleccionado
        {
            get => _tipoDocumentoSeleccionado;
            set { _tipoDocumentoSeleccionado = value; OnPropertyChanged(); }
        }

        public string TipoNumeracion
        {
            get => _tipoNumeracion;
            set
            {
                _tipoNumeracion = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SerieNumeroReadOnly));
                if (SerieNumeroReadOnly)
                {
                    Serie = string.IsNullOrWhiteSpace(Serie) ? "IMS01" : Serie;
                    Numero = _ingresoOriginal?.Numero ?? "Automatico";
                }
            }
        }

        public bool SerieNumeroReadOnly => TipoNumeracion.Equals("Automatica", StringComparison.OrdinalIgnoreCase);

        public string Serie
        {
            get => _serie;
            set { _serie = value; OnPropertyChanged(); }
        }

        public string Numero
        {
            get => _numero;
            set { _numero = value; OnPropertyChanged(); }
        }

        public AlmacenStock? AlmacenSeleccionado
        {
            get => _almacenSeleccionado;
            set
            {
                if (_almacenSeleccionado != null && value != null && _almacenSeleccionado.IdAlmacen != value.IdAlmacen && Detalles.Count > 0)
                {
                    bool confirmar = ConfirmDialogService.Confirmar(
                        "Cambiar el almacen actualizara el stock mostrado en los productos. ¿Desea continuar?",
                        "Cambiar almacen");

                    if (!confirmar)
                    {
                        OnPropertyChanged();
                        return;
                    }
                }

                _almacenSeleccionado = value;
                OnPropertyChanged();
                ActualizarStockDetalles();
            }
        }

        public string Observacion
        {
            get => _observacion;
            set { _observacion = value; OnPropertyChanged(); }
        }

        public decimal Subtotal => Detalles.Sum(d => d.Cantidad * d.PrecioUnitario);
        public decimal DescuentoTotal => Detalles.Sum(d => d.Descuento);
        public decimal Total => Detalles.Sum(d => d.Importe);

        public bool IsSaving
        {
            get => _isSaving;
            set { _isSaving = value; OnPropertyChanged(); }
        }

        public Visibility ProveedorRapidoVisibility
        {
            get => _proveedorRapidoVisibility;
            set { _proveedorRapidoVisibility = value; OnPropertyChanged(); }
        }

        public string NuevoProveedorTipoDocumento { get; set; } = "RUC";
        public string NuevoProveedorNumeroDocumento { get; set; } = string.Empty;
        public string NuevoProveedorNombre { get; set; } = string.Empty;
        public string NuevoProveedorTelefono { get; set; } = string.Empty;
        public string NuevoProveedorCorreo { get; set; } = string.Empty;
        public string NuevoProveedorDireccion { get; set; } = string.Empty;

        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }
        public ICommand AgregarProductoCommand { get; }
        public ICommand QuitarProductoCommand { get; }
        public ICommand MostrarProveedorRapidoCommand { get; }
        public ICommand CancelarProveedorRapidoCommand { get; }
        public ICommand GuardarProveedorRapidoCommand { get; }

        private void CargarCombos()
        {
            Proveedores.Clear();
            foreach (ProveedorStock proveedor in _negocio.ListarProveedores())
            {
                Proveedores.Add(proveedor);
            }

            Almacenes.Clear();
            foreach (AlmacenStock almacen in _negocio.ListarAlmacenes())
            {
                Almacenes.Add(almacen);
            }

            TiposDocumento.Clear();
            foreach (TipoDocumentoStock tipo in _negocio.ListarTiposDocumento())
            {
                TiposDocumento.Add(tipo);
            }

            ProveedorSeleccionado ??= Proveedores.FirstOrDefault();
            AlmacenSeleccionado ??= Almacenes.FirstOrDefault();
            TipoDocumentoSeleccionado ??= TiposDocumento.FirstOrDefault();
        }

        private void CargarIngreso(IngresoManualStock? ingreso)
        {
            if (ingreso == null)
            {
                AgregarProducto();
                return;
            }

            FechaEmision = ingreso.FechaEmision;
            ProveedorSeleccionado = Proveedores.FirstOrDefault(p => p.IdProveedor == ingreso.IdProveedor);
            TipoDocumentoSeleccionado = TiposDocumento.FirstOrDefault(t => t.IdTipoDocumento == ingreso.IdTipoDocumento);
            TipoNumeracion = ingreso.TipoNumeracion;
            Serie = ingreso.Serie;
            Numero = ingreso.Numero;
            AlmacenSeleccionado = Almacenes.FirstOrDefault(a => a.IdAlmacen == ingreso.IdAlmacen);
            Observacion = ingreso.Observacion;

            Detalles.Clear();
            foreach (IngresoManualStockDetalle detalle in ingreso.Detalles)
            {
                Detalles.Add(IngresoManualStockDetalleViewModel.FromEntity(detalle, RecalcularTotales, BuscarProductos));
            }

            RecalcularTotales();
        }

        private void AgregarProducto()
        {
            Detalles.Add(new IngresoManualStockDetalleViewModel(RecalcularTotales, BuscarProductos));
            RecalcularTotales();
        }

        private void QuitarProducto(object? parametro)
        {
            if (parametro is IngresoManualStockDetalleViewModel detalle)
            {
                Detalles.Remove(detalle);
                RecalcularTotales();
            }
        }

        private ObservableCollection<ProductoStockBusqueda> BuscarProductos(string texto)
        {
            ObservableCollection<ProductoStockBusqueda> productos = [];
            int idAlmacen = AlmacenSeleccionado?.IdAlmacen ?? 0;

            foreach (ProductoStockBusqueda producto in _negocio.BuscarProductos(idAlmacen, texto))
            {
                if (!Detalles.Any(d => d.IdProducto == producto.IdProducto))
                {
                    productos.Add(producto);
                }
            }

            return productos;
        }

        private void ActualizarStockDetalles()
        {
            if (AlmacenSeleccionado == null)
            {
                return;
            }

            foreach (IngresoManualStockDetalleViewModel detalle in Detalles.Where(d => d.IdProducto > 0))
            {
                ProductoStockBusqueda? producto = _negocio.BuscarProductos(AlmacenSeleccionado.IdAlmacen, detalle.CodigoProducto)
                    .FirstOrDefault(p => p.IdProducto == detalle.IdProducto);

                if (producto != null)
                {
                    detalle.ActualizarStock(producto.StockActual);
                }
            }
        }

        private void RecalcularTotales()
        {
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(DescuentoTotal));
            OnPropertyChanged(nameof(Total));
        }

        private void GuardarProveedorRapido()
        {
            ProveedorStock proveedor = new()
            {
                TipoDocumento = NuevoProveedorTipoDocumento,
                NumeroDocumento = NuevoProveedorNumeroDocumento,
                NombreRazonSocial = NuevoProveedorNombre,
                Telefono = NuevoProveedorTelefono,
                Correo = NuevoProveedorCorreo,
                Direccion = NuevoProveedorDireccion
            };

            int idProveedor = _negocio.RegistrarProveedorRapido(proveedor, out string mensaje);

            if (idProveedor <= 0)
            {
                NotificationService.Warning(mensaje);
                return;
            }

            CargarCombos();
            ProveedorSeleccionado = Proveedores.FirstOrDefault(p => p.IdProveedor == idProveedor);
            NotificationService.Success(mensaje);
            LimpiarProveedorRapido();
        }

        private void LimpiarProveedorRapido()
        {
            ProveedorRapidoVisibility = Visibility.Collapsed;
            NuevoProveedorTipoDocumento = "RUC";
            NuevoProveedorNumeroDocumento = string.Empty;
            NuevoProveedorNombre = string.Empty;
            NuevoProveedorTelefono = string.Empty;
            NuevoProveedorCorreo = string.Empty;
            NuevoProveedorDireccion = string.Empty;
            OnPropertyChanged(nameof(NuevoProveedorTipoDocumento));
            OnPropertyChanged(nameof(NuevoProveedorNumeroDocumento));
            OnPropertyChanged(nameof(NuevoProveedorNombre));
            OnPropertyChanged(nameof(NuevoProveedorTelefono));
            OnPropertyChanged(nameof(NuevoProveedorCorreo));
            OnPropertyChanged(nameof(NuevoProveedorDireccion));
        }

        private void Guardar()
        {
            if (IsSaving)
            {
                return;
            }

            foreach (IngresoManualStockDetalleViewModel detalle in Detalles.Where(d => d.IdProducto == 0).ToList())
            {
                Detalles.Remove(detalle);
            }

            IngresoManualStock ingreso = new()
            {
                IdIngresoManualStock = _ingresoOriginal?.IdIngresoManualStock ?? 0,
                FechaEmision = FechaEmision ?? DateTime.Today,
                IdProveedor = ProveedorSeleccionado?.IdProveedor ?? 0,
                IdTipoDocumento = TipoDocumentoSeleccionado?.IdTipoDocumento ?? 0,
                TipoNumeracion = TipoNumeracion,
                Serie = SerieNumeroReadOnly ? "IMS01" : Serie,
                Numero = SerieNumeroReadOnly ? string.Empty : Numero,
                IdAlmacen = AlmacenSeleccionado?.IdAlmacen ?? 0,
                Observacion = Observacion,
                Detalles = Detalles.Select(d => d.ToEntity()).ToList()
            };

            if (ingreso.Detalles.GroupBy(d => d.IdProducto).Any(g => g.Count() > 1))
            {
                NotificationService.Warning("No se permiten productos repetidos");
                return;
            }

            try
            {
                IsSaving = true;
                string usuario = SessionManager.UsuarioActual?.NombreUsuario ?? "Sistema";
                string mensaje = _negocio.Guardar(ingreso, usuario);

                if (mensaje.Contains("correctamente", StringComparison.OrdinalIgnoreCase))
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
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudo guardar el ingreso manual: {ex.Message}");
            }
            finally
            {
                IsSaving = false;
            }
        }
    }
}
