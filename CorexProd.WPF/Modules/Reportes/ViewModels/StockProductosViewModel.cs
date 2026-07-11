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
        private readonly StockReservaNegocio _stockReservaNegocio = new();
        private readonly CategoriaProductoNegocio _categoriaProductoNegocio = new();
        private readonly List<StockProductoDisponibilidadItem> _stockProductos = [];
        private string _textoBusqueda = string.Empty;
        private string _etiquetaBusqueda = string.Empty;
        private int _idCategoriaSeleccionada;
        private decimal _cantidadTotal;
        private StockProductoDisponibilidadItem? _stockProductoSeleccionado;
        private string _resumenHistorial = "Seleccione un producto para ver su historial de reservas.";

        public ObservableCollection<StockProductoDisponibilidadItem> StockProductos { get; } = [];
        public ObservableCollection<StockReservaHistorico> HistorialReservas { get; } = [];
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

        public string EtiquetaBusqueda
        {
            get => _etiquetaBusqueda;
            set
            {
                _etiquetaBusqueda = value;
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

        public StockProductoDisponibilidadItem? StockProductoSeleccionado
        {
            get => _stockProductoSeleccionado;
            set
            {
                _stockProductoSeleccionado = value;
                OnPropertyChanged();
                CargarHistorialSeleccionado();
            }
        }

        public string ResumenHistorial
        {
            get => _resumenHistorial;
            set
            {
                _resumenHistorial = value;
                OnPropertyChanged();
            }
        }

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
            List<StockProducto> catalogo = _stockProductoNegocio.Listar();
            Dictionary<int, StockProducto> porProducto = catalogo
                .GroupBy(producto => producto.IdProducto)
                .ToDictionary(grupo => grupo.Key, grupo => grupo.First());

            foreach (StockDisponibilidad disponibilidad in _stockReservaNegocio.ListarDisponibilidad())
            {
                porProducto.TryGetValue(disponibilidad.IdProducto, out StockProducto? producto);
                _stockProductos.Add(StockProductoDisponibilidadItem.From(disponibilidad, producto));
            }

            if (_stockProductos.Count == 0)
            {
                _stockProductos.AddRange(catalogo.Select(StockProductoDisponibilidadItem.From));
            }

            Filtrar();
        }

        private void Filtrar()
        {
            string texto = TextoBusqueda.Trim();
            string etiqueta = EtiquetaBusqueda.Trim();

            List<StockProductoDisponibilidadItem> filtrados = _stockProductos
                .Where(producto =>
                    (IdCategoriaSeleccionada == 0 || producto.IdCategoriaProducto == IdCategoriaSeleccionada)
                    && (string.IsNullOrWhiteSpace(texto)
                        || producto.Codigo.Contains(texto, StringComparison.OrdinalIgnoreCase)
                        || producto.NombreProducto.Contains(texto, StringComparison.OrdinalIgnoreCase))
                    && (string.IsNullOrWhiteSpace(etiqueta)
                        || producto.EtiquetaCliente.Contains(etiqueta, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            StockProductos.Clear();

            foreach (StockProductoDisponibilidadItem producto in filtrados)
            {
                StockProductos.Add(producto);
            }

            CantidadTotal = StockProductos.Sum(producto => producto.StockDisponible);
            OnPropertyChanged(nameof(TotalProductos));
            if (StockProductoSeleccionado is not null && !StockProductos.Contains(StockProductoSeleccionado))
                StockProductoSeleccionado = null;
        }

        private void LimpiarFiltros()
        {
            _textoBusqueda = string.Empty;
            _etiquetaBusqueda = string.Empty;
            _idCategoriaSeleccionada = 0;
            OnPropertyChanged(nameof(TextoBusqueda));
            OnPropertyChanged(nameof(EtiquetaBusqueda));
            OnPropertyChanged(nameof(IdCategoriaSeleccionada));
            Filtrar();
        }

        private void CargarHistorialSeleccionado()
        {
            HistorialReservas.Clear();

            if (StockProductoSeleccionado is null)
            {
                ResumenHistorial = "Seleccione un producto para ver su historial de reservas.";
                return;
            }

            try
            {
                List<StockReservaHistorico> historial = _stockReservaNegocio.ListarHistorico(
                    idProducto: StockProductoSeleccionado.IdProducto,
                    idAlmacen: StockProductoSeleccionado.IdAlmacen,
                    top: 100);

                foreach (StockReservaHistorico item in historial)
                    HistorialReservas.Add(item);

                ResumenHistorial = $"{HistorialReservas.Count} movimiento(s) para {StockProductoSeleccionado.Codigo}.";
            }
            catch (Exception ex)
            {
                ResumenHistorial = $"No se pudo cargar el historial: {ex.Message}";
            }
        }

        public sealed class StockProductoDisponibilidadItem
        {
            public int IdProducto { get; init; }
            public int? IdAlmacen { get; init; }
            public int IdCategoriaProducto { get; init; }
            public string Codigo { get; init; } = string.Empty;
            public string NombreProducto { get; init; } = string.Empty;
            public string EtiquetaCliente { get; init; } = string.Empty;
            public string NombreCategoria { get; init; } = string.Empty;
            public string NombreAlmacen { get; init; } = string.Empty;
            public decimal StockFisico { get; init; }
            public decimal StockReservado { get; init; }
            public decimal StockDisponible { get; init; }
            public decimal Cantidad => StockDisponible;

            public static StockProductoDisponibilidadItem From(StockDisponibilidad disponibilidad, StockProducto? producto) => new()
            {
                IdProducto = disponibilidad.IdProducto,
                IdAlmacen = disponibilidad.IdAlmacen > 0 ? disponibilidad.IdAlmacen : null,
                IdCategoriaProducto = producto?.IdCategoriaProducto ?? 0,
                Codigo = disponibilidad.Codigo,
                NombreProducto = disponibilidad.NombreProducto,
                EtiquetaCliente = disponibilidad.EtiquetaCliente,
                NombreCategoria = producto?.NombreCategoria ?? string.Empty,
                NombreAlmacen = disponibilidad.NombreAlmacen,
                StockFisico = disponibilidad.StockFisico,
                StockReservado = disponibilidad.StockReservado,
                StockDisponible = disponibilidad.StockDisponible
            };

            public static StockProductoDisponibilidadItem From(StockProducto producto) => new()
            {
                IdProducto = producto.IdProducto,
                IdAlmacen = null,
                IdCategoriaProducto = producto.IdCategoriaProducto,
                Codigo = producto.Codigo,
                NombreProducto = producto.NombreProducto,
                EtiquetaCliente = producto.EtiquetaCliente,
                NombreCategoria = producto.NombreCategoria,
                NombreAlmacen = string.Empty,
                StockFisico = producto.Cantidad,
                StockReservado = 0,
                StockDisponible = producto.Cantidad
            };
        }
    }
}
