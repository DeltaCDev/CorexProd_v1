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
        private decimal _cantidad = 1;
        private decimal _precioUnitario;
        private decimal _descuento;
        private string _observacion = string.Empty;

        public ObservableCollection<Producto> Productos { get; }
        public event Action? TotalesActualizados;
        public event Action<ProformaDetalleItemViewModel, Producto>? ProductoCambiado;

        public ProformaDetalleItemViewModel(ObservableCollection<Producto> productos)
        {
            Productos = productos;
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
                    ProductoCambiado?.Invoke(this, value);
                }
            }
        }

        public int IdProducto => ProductoSeleccionado?.IdProducto ?? 0;

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
            _productoSeleccionado = Productos.FirstOrDefault(p => p.IdProducto == idProducto);
            OnPropertyChanged(nameof(ProductoSeleccionado));
            OnPropertyChanged(nameof(IdProducto));
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
    }
}
