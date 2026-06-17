using CorexProd.Entidad.Entidades;
using CorexProd.WPF.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace CorexProd.WPF.Modules.Almacen.ViewModels
{
    public class IngresoManualStockDetalleViewModel : BaseViewModel
    {
        private readonly Action _recalcularTotales;
        private readonly Func<string, ObservableCollection<ProductoStockBusqueda>> _buscarProductos;
        private ProductoStockBusqueda? _productoSeleccionado;
        private string _textoBusquedaProducto = string.Empty;
        private bool _productoDropdownAbierto;
        private decimal _stockActual;
        private decimal _cantidad = 1;
        private decimal _precioUnitario;
        private decimal _descuento;

        public ObservableCollection<ProductoStockBusqueda> ProductosFiltrados { get; } = [];

        public IngresoManualStockDetalleViewModel(
            Action recalcularTotales,
            Func<string, ObservableCollection<ProductoStockBusqueda>> buscarProductos)
        {
            _recalcularTotales = recalcularTotales;
            _buscarProductos = buscarProductos;
        }

        public int IdProducto { get; private set; }
        public string CodigoProducto { get; private set; } = string.Empty;
        public string NombreProducto { get; private set; } = string.Empty;
        public int IdUnidadMedida { get; private set; }
        public string NombreUnidad { get; private set; } = string.Empty;

        public string TextoBusquedaProducto
        {
            get => _textoBusquedaProducto;
            set
            {
                _textoBusquedaProducto = value;
                OnPropertyChanged();
                Buscar();
            }
        }

        public ProductoStockBusqueda? ProductoSeleccionado
        {
            get => _productoSeleccionado;
            set
            {
                _productoSeleccionado = value;
                OnPropertyChanged();

                if (value != null)
                {
                    AsignarProducto(value);
                }
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

        public decimal StockActual
        {
            get => _stockActual;
            set
            {
                _stockActual = value;
                OnPropertyChanged();
            }
        }

        public decimal Cantidad
        {
            get => _cantidad;
            set
            {
                _cantidad = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Importe));
                _recalcularTotales();
            }
        }

        public decimal PrecioUnitario
        {
            get => _precioUnitario;
            set
            {
                _precioUnitario = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Importe));
                _recalcularTotales();
            }
        }

        public decimal Descuento
        {
            get => _descuento;
            set
            {
                _descuento = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Importe));
                _recalcularTotales();
            }
        }

        public decimal Importe => Math.Max(0, (Cantidad * PrecioUnitario) - Descuento);

        public void AsignarProducto(ProductoStockBusqueda producto)
        {
            IdProducto = producto.IdProducto;
            CodigoProducto = producto.Codigo;
            NombreProducto = producto.NombreProducto;
            IdUnidadMedida = producto.IdUnidadMedida;
            NombreUnidad = producto.NombreUnidad;
            StockActual = producto.StockActual;
            _textoBusquedaProducto = $"{producto.Codigo} - {producto.NombreProducto}";
            ProductoDropdownAbierto = false;

            OnPropertyChanged(nameof(IdProducto));
            OnPropertyChanged(nameof(CodigoProducto));
            OnPropertyChanged(nameof(NombreProducto));
            OnPropertyChanged(nameof(IdUnidadMedida));
            OnPropertyChanged(nameof(NombreUnidad));
            OnPropertyChanged(nameof(TextoBusquedaProducto));
            _recalcularTotales();
        }

        public void ActualizarStock(decimal stock)
        {
            StockActual = stock;
        }

        public IngresoManualStockDetalle ToEntity()
        {
            return new IngresoManualStockDetalle
            {
                IdProducto = IdProducto,
                CodigoProducto = CodigoProducto,
                NombreProducto = NombreProducto,
                IdUnidadMedida = IdUnidadMedida,
                NombreUnidad = NombreUnidad,
                StockActual = StockActual,
                Cantidad = Cantidad,
                PrecioUnitario = PrecioUnitario,
                Descuento = Descuento,
                Importe = Importe
            };
        }

        public static IngresoManualStockDetalleViewModel FromEntity(
            IngresoManualStockDetalle detalle,
            Action recalcularTotales,
            Func<string, ObservableCollection<ProductoStockBusqueda>> buscarProductos)
        {
            IngresoManualStockDetalleViewModel vm = new(recalcularTotales, buscarProductos);
            vm.IdProducto = detalle.IdProducto;
            vm.CodigoProducto = detalle.CodigoProducto;
            vm.NombreProducto = detalle.NombreProducto;
            vm.IdUnidadMedida = detalle.IdUnidadMedida;
            vm.NombreUnidad = detalle.NombreUnidad;
            vm.StockActual = detalle.StockActual;
            vm.Cantidad = detalle.Cantidad;
            vm.PrecioUnitario = detalle.PrecioUnitario;
            vm.Descuento = detalle.Descuento;
            vm._textoBusquedaProducto = $"{detalle.CodigoProducto} - {detalle.NombreProducto}";
            return vm;
        }

        private void Buscar()
        {
            ProductosFiltrados.Clear();

            if (TextoBusquedaProducto.Length < 2)
            {
                ProductoDropdownAbierto = false;
                return;
            }

            foreach (ProductoStockBusqueda producto in _buscarProductos(TextoBusquedaProducto).Take(25))
            {
                ProductosFiltrados.Add(producto);
            }

            ProductoDropdownAbierto = ProductosFiltrados.Count > 0;
        }
    }
}
