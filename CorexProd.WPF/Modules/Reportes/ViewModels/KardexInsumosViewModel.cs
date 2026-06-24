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
    public class KardexInsumosViewModel : BaseViewModel
    {
        private readonly KardexInsumoReporteNegocio _kardexNegocio = new();
        private readonly StockInsumoNegocio _stockInsumoNegocio = new();
        private readonly List<StockInsumo> _insumosBase = [];

        private DateTime? _fechaDesde = new(DateTime.Today.Year, DateTime.Today.Month, 1);
        private DateTime? _fechaHasta = DateTime.Today;
        private MovimientoFiltro _tipoMovimientoSeleccionado = new("Todos", string.Empty);
        private AlmacenStock? _almacenSeleccionado;
        private StockInsumo? _insumoSeleccionado;
        private string _insumoTexto = string.Empty;
        private string _limiteSeleccionado = "Todos";
        private bool _insumosDropDownAbierto;
        private bool _actualizandoInsumoDesdeSeleccion;

        public ObservableCollection<KardexInsumoReporte> MovimientosGeneral { get; } = [];
        public ObservableCollection<KardexInsumoReporte> MovimientosInsumo { get; } = [];
        public ObservableCollection<MovimientoFiltro> TiposMovimiento { get; } = [];
        public ObservableCollection<AlmacenStock> Almacenes { get; } = [];
        public ObservableCollection<StockInsumo> InsumosFiltrados { get; } = [];
        public ObservableCollection<string> Limites { get; } = ["Todos", "50", "100", "200", "500"];

        public ICommand BuscarGeneralCommand { get; }
        public ICommand BuscarInsumoCommand { get; }
        public ICommand LimpiarCommand { get; }

        public DateTime? FechaDesde { get => _fechaDesde; set { _fechaDesde = value; OnPropertyChanged(); } }
        public DateTime? FechaHasta { get => _fechaHasta; set { _fechaHasta = value; OnPropertyChanged(); } }
        public MovimientoFiltro TipoMovimientoSeleccionado { get => _tipoMovimientoSeleccionado; set { _tipoMovimientoSeleccionado = value; OnPropertyChanged(); } }
        public AlmacenStock? AlmacenSeleccionado { get => _almacenSeleccionado; set { _almacenSeleccionado = value; OnPropertyChanged(); } }
        public string LimiteSeleccionado { get => _limiteSeleccionado; set { _limiteSeleccionado = value; OnPropertyChanged(); } }
        public bool InsumosDropDownAbierto { get => _insumosDropDownAbierto; set { _insumosDropDownAbierto = value; OnPropertyChanged(); } }

        public StockInsumo? InsumoSeleccionado
        {
            get => _insumoSeleccionado;
            set
            {
                _insumoSeleccionado = value;
                OnPropertyChanged();

                if (value != null)
                {
                    _actualizandoInsumoDesdeSeleccion = true;
                    InsumoTexto = value.InsumoBusqueda;
                    _actualizandoInsumoDesdeSeleccion = false;
                    InsumosDropDownAbierto = false;
                }
            }
        }

        public string InsumoTexto
        {
            get => _insumoTexto;
            set
            {
                _insumoTexto = value;
                OnPropertyChanged();
                FiltrarInsumos();
            }
        }

        public decimal TotalEntradasInsumo => MovimientosInsumo.Sum(x => x.Entrada);
        public decimal TotalSalidasInsumo => MovimientosInsumo.Sum(x => x.Salida);
        public decimal TotalDevolucionesInsumo => MovimientosInsumo.Sum(x => x.Devolucion);
        public decimal StockFinalInsumo => MovimientosInsumo.FirstOrDefault()?.Stock ?? 0;
        public int TotalRegistrosGeneral => MovimientosGeneral.Count;
        public int TotalRegistrosInsumo => MovimientosInsumo.Count;

        public KardexInsumosViewModel()
        {
            BuscarGeneralCommand = new RelayCommand(_ => BuscarGeneral());
            BuscarInsumoCommand = new RelayCommand(_ => BuscarInsumo());
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

            _insumosBase.Clear();
            _insumosBase.AddRange(_stockInsumoNegocio.Listar());

            foreach (StockInsumo insumo in _insumosBase.Take(20))
                InsumosFiltrados.Add(insumo);
        }

        private void BuscarGeneral()
        {
            MovimientosGeneral.Clear();
            foreach (KardexInsumoReporte movimiento in Consultar(null))
                MovimientosGeneral.Add(movimiento);
            OnPropertyChanged(nameof(TotalRegistrosGeneral));
        }

        private void BuscarInsumo()
        {
            if (InsumoSeleccionado == null)
            {
                NotificationService.Warning("Escriba mínimo 3 caracteres y seleccione un insumo para consultar el Kardex.");
                return;
            }

            MovimientosInsumo.Clear();
            foreach (KardexInsumoReporte movimiento in Consultar(InsumoSeleccionado.IdInsumo))
                MovimientosInsumo.Add(movimiento);

            OnPropertyChanged(nameof(TotalEntradasInsumo));
            OnPropertyChanged(nameof(TotalSalidasInsumo));
            OnPropertyChanged(nameof(TotalDevolucionesInsumo));
            OnPropertyChanged(nameof(StockFinalInsumo));
            OnPropertyChanged(nameof(TotalRegistrosInsumo));
        }

        private List<KardexInsumoReporte> Consultar(int? idInsumo)
        {
            int? idAlmacen = AlmacenSeleccionado?.IdAlmacen > 0 ? AlmacenSeleccionado.IdAlmacen : null;
            string tipo = TipoMovimientoSeleccionado?.Codigo ?? string.Empty;
            int? limite = int.TryParse(LimiteSeleccionado, out int valor) ? valor : null;

            return _kardexNegocio.Listar(FechaDesde, FechaHasta, idAlmacen, idInsumo, tipo, limite);
        }

        private void Limpiar()
        {
            FechaDesde = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            FechaHasta = DateTime.Today;
            TipoMovimientoSeleccionado = TiposMovimiento.First();
            AlmacenSeleccionado = Almacenes.FirstOrDefault();
            LimiteSeleccionado = "Todos";
            InsumoSeleccionado = null;
            InsumoTexto = string.Empty;
            InsumosDropDownAbierto = false;
            BuscarGeneral();
            MovimientosInsumo.Clear();
        }

        private void FiltrarInsumos()
        {
            if (_actualizandoInsumoDesdeSeleccion)
                return;

            InsumosFiltrados.Clear();
            string texto = InsumoTexto?.Trim() ?? string.Empty;

            if (texto.Length < 3)
            {
                InsumoSeleccionado = null;
                InsumosDropDownAbierto = false;
                return;
            }

            foreach (StockInsumo insumo in _insumosBase
                .Where(x => x.InsumoBusqueda.Contains(texto, StringComparison.OrdinalIgnoreCase))
                .Take(30))
            {
                InsumosFiltrados.Add(insumo);
            }

            if (InsumoSeleccionado != null
                && !InsumoSeleccionado.InsumoBusqueda.Contains(texto, StringComparison.OrdinalIgnoreCase))
            {
                _insumoSeleccionado = null;
                OnPropertyChanged(nameof(InsumoSeleccionado));
            }

            InsumosDropDownAbierto = InsumosFiltrados.Count > 0;
        }

        private static string FormatearMovimiento(string movimiento)
        {
            string limpio = (movimiento ?? string.Empty).Replace("_", " ").Trim().ToLowerInvariant();
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(limpio);
        }

        public record MovimientoFiltro(string Nombre, string Codigo);
    }
}
