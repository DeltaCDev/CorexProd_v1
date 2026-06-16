using CorexProd.Entidad.Entidades;
using CorexProd.WPF.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace CorexProd.WPF.Modules.Ventas.ViewModels
{
    public class ProformaDetalleItemViewModel : BaseViewModel
    {
        private int _idProformaDetalle;
        private Producto? _productoSeleccionado;
        private Producto? _productoResaltado;
        private string _textoBusquedaProducto = string.Empty;
        private bool _productoDropdownAbierto;
        private bool _actualizandoTextoDesdeSeleccion;
        private decimal _cantidad = 1;
        private decimal _precioUnitario;
        private decimal _descuento;
        private string _observacion = string.Empty;
        private readonly ObservableCollection<Producto> _productos;

        public ObservableCollection<Producto> ProductosFiltrados { get; } = [];
        public event Action? TotalesActualizados;
        public event Action<ProformaDetalleItemViewModel, Producto>? ProductoCambiado;

        public ProformaDetalleItemViewModel(ObservableCollection<Producto> productos)
        {
            _productos = productos;
        }

        public int IdProformaDetalle
        {
            get => _idProformaDetalle;
            set
            {
                _idProformaDetalle = value;
                OnPropertyChanged();
            }
        }

        public Producto? ProductoSeleccionado
        {
            get => _productoSeleccionado;
            set
            {
                if (_productoSeleccionado?.IdProducto == value?.IdProducto)
                {
                    return;
                }

                _productoSeleccionado = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IdProducto));

                if (value != null)
                {
                    ActualizarTextoProductoSeleccionado(value.ProductoBusqueda);
                    ProductoCambiado?.Invoke(this, value);
                }
            }
        }

        public int IdProducto => ProductoSeleccionado?.IdProducto ?? 0;

        public Producto? ProductoResaltado
        {
            get => _productoResaltado;
            set
            {
                _productoResaltado = value;
                OnPropertyChanged();
            }
        }

        public string TextoBusquedaProducto
        {
            get => _textoBusquedaProducto;
            set
            {
                _textoBusquedaProducto = value ?? string.Empty;
                OnPropertyChanged();
                FiltrarProductos();
            }
        }

        public bool ProductoDropdownAbierto
        {
            get => _productoDropdownAbierto;
            set
            {
                _productoDropdownAbierto = value;
                OnPropertyChanged();
            }
        }

        public void SeleccionarProductoBusqueda()
        {
            Producto? producto = ProductoResaltado ?? ProductosFiltrados.FirstOrDefault();

            if (producto == null)
            {
                return;
            }

            ProductoDropdownAbierto = false;
            ProductoSeleccionado = producto;
        }

        public decimal Cantidad
        {
            get => _cantidad;
            set
            {
                _cantidad = value;
                OnPropertyChanged();
                NotificarImporte();
            }
        }

        public decimal PrecioUnitario
        {
            get => _precioUnitario;
            set
            {
                _precioUnitario = value;
                OnPropertyChanged();
                NotificarImporte();
            }
        }

        public decimal Descuento
        {
            get => _descuento;
            set
            {
                _descuento = value;
                OnPropertyChanged();
                NotificarImporte();
            }
        }

        public decimal Importe => Math.Max(0, (Cantidad * PrecioUnitario) - Descuento);

        public string Observacion
        {
            get => _observacion;
            set
            {
                _observacion = value;
                OnPropertyChanged();
            }
        }

        public void CargarProducto(int idProducto)
        {
            _productoSeleccionado = _productos.FirstOrDefault(p => p.IdProducto == idProducto);
            _textoBusquedaProducto = _productoSeleccionado?.ProductoBusqueda ?? string.Empty;
            ProductosFiltrados.Clear();
            ProductoResaltado = null;

            if (_productoSeleccionado != null)
            {
                ProductosFiltrados.Add(_productoSeleccionado);
                ProductoResaltado = _productoSeleccionado;
            }

            OnPropertyChanged(nameof(ProductoSeleccionado));
            OnPropertyChanged(nameof(IdProducto));
            OnPropertyChanged(nameof(TextoBusquedaProducto));
        }

        public ProformaDetalle CrearDetalle()
        {
            return new ProformaDetalle
            {
                IdProformaDetalle = IdProformaDetalle,
                IdProducto = IdProducto,
                Cantidad = Cantidad,
                PrecioUnitario = PrecioUnitario,
                Descuento = Descuento,
                Importe = Importe,
                Observacion = Observacion
            };
        }

        private void NotificarImporte()
        {
            OnPropertyChanged(nameof(Importe));
            TotalesActualizados?.Invoke();
        }

        private void FiltrarProductos()
        {
            if (_actualizandoTextoDesdeSeleccion)
            {
                return;
            }

            string texto = TextoBusquedaProducto.Trim();

            if (ProductoSeleccionado != null && !texto.Equals(ProductoSeleccionado.ProductoBusqueda, StringComparison.OrdinalIgnoreCase))
            {
                _productoSeleccionado = null;
                OnPropertyChanged(nameof(ProductoSeleccionado));
                OnPropertyChanged(nameof(IdProducto));
            }

            ProductosFiltrados.Clear();
            ProductoResaltado = null;

            if (texto.Length < 3)
            {
                ProductoDropdownAbierto = false;
                return;
            }

            foreach (Producto producto in _productos
                .Where(p => p.Codigo.Contains(texto, StringComparison.OrdinalIgnoreCase)
                    || p.NombreProducto.Contains(texto, StringComparison.OrdinalIgnoreCase))
                .Take(30))
            {
                ProductosFiltrados.Add(producto);
            }

            ProductoResaltado = ProductosFiltrados.FirstOrDefault();
            ProductoDropdownAbierto = ProductosFiltrados.Count > 0;
        }

        private void ActualizarTextoProductoSeleccionado(string texto)
        {
            _actualizandoTextoDesdeSeleccion = true;
            _textoBusquedaProducto = texto;
            OnPropertyChanged(nameof(TextoBusquedaProducto));
            _actualizandoTextoDesdeSeleccion = false;
        }
    }
}
