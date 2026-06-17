using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Reportes.ViewModels
{
    public class StockProductosViewModel : BaseViewModel
    {
        private readonly StockProductoNegocio _stockProductoNegocio = new();
        private readonly CategoriaProductoNegocio _categoriaProductoNegocio = new();
        private readonly List<StockProducto> _stockProductos = [];
        private string _textoBusqueda = string.Empty;
        private int _idCategoriaSeleccionada;
        private decimal _cantidadTotal;

        public ObservableCollection<StockProducto> StockProductos { get; } = [];
        public ObservableCollection<CategoriaProducto> Categorias { get; } = [];

        public string TextoBusqueda
        {
            get => _textoBusqueda;
            set
            {
                _textoBusqueda = value;
                OnPropertyChanged();
                Filtrar();
            }
        }

        public int IdCategoriaSeleccionada
        {
            get => _idCategoriaSeleccionada;
            set
            {
                _idCategoriaSeleccionada = value;
                OnPropertyChanged();
                Filtrar();
            }
        }

        public decimal CantidadTotal
        {
            get => _cantidadTotal;
            set
            {
                _cantidadTotal = value;
                OnPropertyChanged();
            }
        }

        public int TotalProductos => StockProductos.Count;

        public ICommand BuscarCommand { get; }
        public ICommand LimpiarCommand { get; }
        public ICommand ActualizarCommand { get; }

        public StockProductosViewModel()
        {
            BuscarCommand = new RelayCommand(_ => Filtrar());
            LimpiarCommand = new RelayCommand(_ => LimpiarFiltros());
            ActualizarCommand = new RelayCommand(_ => CargarDatos());

            CargarCategorias();
            CargarDatos();
        }

        private void CargarCategorias()
        {
            Categorias.Clear();
            Categorias.Add(new CategoriaProducto
            {
                IdCategoriaProducto = 0,
                NombreCategoria = "Todas las categorías"
            });

            foreach (CategoriaProducto categoria in _categoriaProductoNegocio.Listar())
            {
                if (categoria.Estado)
                {
                    Categorias.Add(categoria);
                }
            }
        }

        private void CargarDatos()
        {
            _stockProductos.Clear();
            _stockProductos.AddRange(_stockProductoNegocio.Listar());
            Filtrar();
        }

        private void Filtrar()
        {
            string texto = TextoBusqueda.Trim();

            List<StockProducto> filtrados = _stockProductos
                .Where(producto =>
                    (IdCategoriaSeleccionada == 0 || producto.IdCategoriaProducto == IdCategoriaSeleccionada)
                    && (string.IsNullOrWhiteSpace(texto)
                        || producto.Codigo.Contains(texto, StringComparison.OrdinalIgnoreCase)
                        || producto.NombreProducto.Contains(texto, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(producto => producto.NombreProducto)
                .ToList();

            StockProductos.Clear();

            foreach (StockProducto producto in filtrados)
            {
                StockProductos.Add(producto);
            }

            CantidadTotal = StockProductos.Sum(producto => producto.Cantidad);
            OnPropertyChanged(nameof(TotalProductos));
        }

        private void LimpiarFiltros()
        {
            _textoBusqueda = string.Empty;
            _idCategoriaSeleccionada = 0;
            OnPropertyChanged(nameof(TextoBusqueda));
            OnPropertyChanged(nameof(IdCategoriaSeleccionada));
            Filtrar();
        }
    }
}
