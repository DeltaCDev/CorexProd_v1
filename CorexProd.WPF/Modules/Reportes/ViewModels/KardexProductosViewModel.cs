using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Reportes.ViewModels
{
    public class KardexProductosViewModel : BaseViewModel
    {
        private readonly KardexProductoReporteNegocio _kardexNegocio = new();
        private readonly StockProductoNegocio _stockProductoNegocio = new();

        private DateTime? _fechaDesde = new(DateTime.Today.Year, DateTime.Today.Month, 1);
        private DateTime? _fechaHasta = DateTime.Today;
        private MovimientoFiltro _tipoMovimientoSeleccionado = new("Todos", string.Empty);
        private AlmacenStock? _almacenSeleccionado;
        private StockProducto? _productoSeleccionado;
        private string _productoTexto = string.Empty;
        private string _limiteSeleccionado = "Todos";
        private readonly List<StockProducto> _productosBase = [];
        private bool _productosDropDownAbierto;
        private bool _actualizandoProductoDesdeSeleccion;

        public ObservableCollection<KardexProductoReporte> MovimientosGeneral { get; } = [];
        public ObservableCollection<KardexProductoReporte> MovimientosProducto { get; } = [];
        public ObservableCollection<MovimientoFiltro> TiposMovimiento { get; } = [];
        public ObservableCollection<AlmacenStock> Almacenes { get; } = [];
        public ObservableCollection<StockProducto> Productos { get; } = [];
        public ObservableCollection<StockProducto> ProductosFiltrados { get; } = [];
        public ObservableCollection<string> Limites { get; } = ["Todos", "50", "100", "200", "500"];

        public ICommand BuscarGeneralCommand { get; }
        public ICommand BuscarProductoCommand { get; }
        public ICommand LimpiarCommand { get; }

        public DateTime? FechaDesde
        {
            get => _fechaDesde;
            set { _fechaDesde = value; OnPropertyChanged(); }
        }

        public DateTime? FechaHasta
        {
            get => _fechaHasta;
            set { _fechaHasta = value; OnPropertyChanged(); }
        }

        public MovimientoFiltro TipoMovimientoSeleccionado
        {
            get => _tipoMovimientoSeleccionado;
            set { _tipoMovimientoSeleccionado = value; OnPropertyChanged(); }
        }

        public AlmacenStock? AlmacenSeleccionado
        {
            get => _almacenSeleccionado;
            set { _almacenSeleccionado = value; OnPropertyChanged(); }
        }

        public StockProducto? ProductoSeleccionado
        {
            get => _productoSeleccionado;
            set
            {
                _productoSeleccionado = value;
                OnPropertyChanged();

                if (value != null)
                {
                    _actualizandoProductoDesdeSeleccion = true;
                    ProductoTexto = value.ProductoBusqueda;
                    _actualizandoProductoDesdeSeleccion = false;
                    ProductosDropDownAbierto = false;
                }
            }
        }

        public string ProductoTexto
        {
            get => _productoTexto;
            set
            {
                _productoTexto = value;
                OnPropertyChanged();
                FiltrarProductos();
            }
        }

        public bool ProductosDropDownAbierto
        {
            get => _productosDropDownAbierto;
            set { _productosDropDownAbierto = value; OnPropertyChanged(); }
        }

        public string LimiteSeleccionado
        {
            get => _limiteSeleccionado;
            set { _limiteSeleccionado = value; OnPropertyChanged(); }
        }

        public decimal TotalEntradasProducto => MovimientosProducto.Sum(x => x.Entrada);
        public decimal TotalSalidasProducto => MovimientosProducto.Sum(x => x.Salida);
        public decimal TotalDevolucionesProducto => MovimientosProducto.Sum(x => x.Devolucion);
        public decimal StockFinalProducto => MovimientosProducto.FirstOrDefault()?.Stock ?? 0;
        public int TotalRegistrosGeneral => MovimientosGeneral.Count;
        public int TotalRegistrosProducto => MovimientosProducto.Count;

        public KardexProductosViewModel()
        {
            BuscarGeneralCommand = new RelayCommand(_ => BuscarGeneral());
            BuscarProductoCommand = new RelayCommand(_ => BuscarProducto());
            LimpiarCommand = new RelayCommand(_ => Limpiar());

            CargarCombos();
            BuscarGeneral();
        }

        private void CargarCombos()
        {
            TiposMovimiento.Add(new MovimientoFiltro("Todos", string.Empty));
            foreach (string tipo in _kardexNegocio.ListarTiposMovimiento())
                TiposMovimiento.Add(new MovimientoFiltro(FormatearMovimiento(tipo), tipo));

            Almacenes.Add(new AlmacenStock { IdAlmacen = 0, NombreAlmacen = "Todos los almacenes" });
            foreach (AlmacenStock almacen in _kardexNegocio.ListarAlmacenes())
                Almacenes.Add(almacen);
            AlmacenSeleccionado = Almacenes.FirstOrDefault();

            _productosBase.Clear();
            _productosBase.AddRange(_stockProductoNegocio.Listar());

            foreach (StockProducto producto in _productosBase.Take(20))
            {
                Productos.Add(producto);
                ProductosFiltrados.Add(producto);
            }
        }

        private void BuscarGeneral()
        {
            MovimientosGeneral.Clear();

            foreach (KardexProductoReporte movimiento in Consultar(null))
                MovimientosGeneral.Add(movimiento);

            OnPropertyChanged(nameof(TotalRegistrosGeneral));
        }

        private void BuscarProducto()
        {
            if (ProductoSeleccionado == null)
            {
                NotificationService.Warning("Escriba mínimo 3 caracteres y seleccione un producto para consultar el Kardex.");
                return;
            }

            MovimientosProducto.Clear();

            foreach (KardexProductoReporte movimiento in Consultar(ProductoSeleccionado.IdProducto))
                MovimientosProducto.Add(movimiento);

            OnPropertyChanged(nameof(TotalEntradasProducto));
            OnPropertyChanged(nameof(TotalSalidasProducto));
            OnPropertyChanged(nameof(TotalDevolucionesProducto));
            OnPropertyChanged(nameof(StockFinalProducto));
            OnPropertyChanged(nameof(TotalRegistrosProducto));
        }

        private List<KardexProductoReporte> Consultar(int? idProducto)
        {
            int? idAlmacen = AlmacenSeleccionado?.IdAlmacen > 0 ? AlmacenSeleccionado.IdAlmacen : null;
            string tipo = TipoMovimientoSeleccionado?.Codigo ?? string.Empty;
            int? limite = int.TryParse(LimiteSeleccionado, out int valor) ? valor : null;

            return _kardexNegocio.Listar(FechaDesde, FechaHasta, idAlmacen, idProducto, tipo, limite);
        }

        private void Limpiar()
        {
            FechaDesde = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            FechaHasta = DateTime.Today;
            TipoMovimientoSeleccionado = TiposMovimiento.First();
            AlmacenSeleccionado = Almacenes.FirstOrDefault();
            LimiteSeleccionado = "Todos";
            ProductoSeleccionado = null;
            ProductoTexto = string.Empty;
            ProductosDropDownAbierto = false;
            BuscarGeneral();
            MovimientosProducto.Clear();
        }

        private void FiltrarProductos()
        {
            if (_actualizandoProductoDesdeSeleccion)
                return;

            ProductosFiltrados.Clear();

            string texto = ProductoTexto?.Trim() ?? string.Empty;

            if (texto.Length < 3)
            {
                ProductoSeleccionado = null;
                ProductosDropDownAbierto = false;
                return;
            }

            foreach (StockProducto producto in _productosBase
                .Where(x => x.ProductoBusqueda.Contains(texto, StringComparison.OrdinalIgnoreCase))
                .Take(30))
            {
                ProductosFiltrados.Add(producto);
            }

            if (ProductoSeleccionado != null
                && !ProductoSeleccionado.ProductoBusqueda.Contains(texto, StringComparison.OrdinalIgnoreCase))
            {
                _productoSeleccionado = null;
                OnPropertyChanged(nameof(ProductoSeleccionado));
            }

            ProductosDropDownAbierto = ProductosFiltrados.Count > 0;
        }

        private static string FormatearMovimiento(string movimiento)
        {
            string limpio = (movimiento ?? string.Empty).Replace("_", " ").Trim().ToLowerInvariant();
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(limpio);
        }

        public record MovimientoFiltro(string Nombre, string Codigo);
    }
}
