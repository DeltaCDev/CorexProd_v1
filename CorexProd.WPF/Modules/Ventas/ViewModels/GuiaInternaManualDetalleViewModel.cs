using CorexProd.Entidad.Entidades;
using CorexProd.WPF.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace CorexProd.WPF.Modules.Ventas.ViewModels
{
    public class GuiaInternaManualDetalleViewModel : BaseViewModel
    {
        private readonly Func<string, ObservableCollection<ProductoStockBusqueda>> _buscarProductos;
        private string _textoBusquedaProducto = string.Empty;
        private bool _productoDropdownAbierto;
        private decimal _cantidadDespachar = 1;
        private string _observacion = string.Empty;

        public GuiaInternaManualDetalleViewModel(Func<string, ObservableCollection<ProductoStockBusqueda>> buscarProductos)
        {
            _buscarProductos = buscarProductos;
        }

        public ObservableCollection<ProductoStockBusqueda> ProductosFiltrados { get; } = [];
        public int IdProducto { get; private set; }
        public string CodigoProducto { get; private set; } = string.Empty;
        public string NombreProducto { get; private set; } = string.Empty;
        public int IdUnidadMedida { get; private set; }
        public string NombreUnidad { get; private set; } = string.Empty;
        public decimal StockActual { get; private set; }

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

        public bool ProductoDropdownAbierto
        {
            get => _productoDropdownAbierto;
            set { _productoDropdownAbierto = value; OnPropertyChanged(); }
        }

        public decimal CantidadDespachar
        {
            get => _cantidadDespachar;
            set { _cantidadDespachar = value; OnPropertyChanged(); }
        }

        public string Observacion
        {
            get => _observacion;
            set { _observacion = value; OnPropertyChanged(); }
        }

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
            OnPropertyChanged(nameof(StockActual));
            OnPropertyChanged(nameof(TextoBusquedaProducto));
        }

        public GuiaInternaDetalle ToEntity() => new()
        {
            IdProducto = IdProducto,
            CodigoProducto = CodigoProducto,
            NombreProducto = NombreProducto,
            IdUnidadMedida = IdUnidadMedida,
            NombreUnidad = NombreUnidad,
            StockActual = StockActual,
            CantidadPendiente = StockActual,
            CantidadDespachar = CantidadDespachar,
            Observacion = Observacion
        };

        private void Buscar()
        {
            ProductosFiltrados.Clear();
            if (TextoBusquedaProducto.Length < 2)
            {
                ProductoDropdownAbierto = false;
                return;
            }

            foreach (ProductoStockBusqueda producto in _buscarProductos(TextoBusquedaProducto).Take(25))
                ProductosFiltrados.Add(producto);

            ProductoDropdownAbierto = ProductosFiltrados.Count > 0;
        }
    }
}
