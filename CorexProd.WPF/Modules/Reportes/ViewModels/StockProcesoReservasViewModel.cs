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
    public class StockProcesoReservasViewModel : BaseViewModel
    {
        private readonly StockProductoNegocio _stockProductoNegocio = new();
        private readonly List<StockProcesoReservaReporte> _reservasBase = [];
        private string _textoBusqueda = string.Empty;
        private string _areaBusqueda = string.Empty;
        private decimal _cantidadTotal;
        private decimal _cantidadUsadaTotal;
        private decimal _cantidadDisponibleTotal;
        private DateTime? _fechaDesde;
        private DateTime? _fechaHasta;

        public ObservableCollection<StockProcesoReservaReporte> Reservas { get; } = [];

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

        public string AreaBusqueda
        {
            get => _areaBusqueda;
            set
            {
                _areaBusqueda = value;
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

        public decimal CantidadUsadaTotal
        {
            get => _cantidadUsadaTotal;
            set
            {
                _cantidadUsadaTotal = value;
                OnPropertyChanged();
            }
        }

        public decimal CantidadDisponibleTotal
        {
            get => _cantidadDisponibleTotal;
            set
            {
                _cantidadDisponibleTotal = value;
                OnPropertyChanged();
            }
        }

        public DateTime? FechaDesde
        {
            get => _fechaDesde;
            set
            {
                _fechaDesde = value;
                OnPropertyChanged();
                Filtrar();
            }
        }

        public DateTime? FechaHasta
        {
            get => _fechaHasta;
            set
            {
                _fechaHasta = value;
                OnPropertyChanged();
                Filtrar();
            }
        }

        public int TotalRegistros => Reservas.Count;
        public int TotalProductosReservados => Reservas.Select(x => x.IdProducto).Distinct().Count();
        public ICommand BuscarCommand { get; }
        public ICommand LimpiarCommand { get; }
        public ICommand ActualizarCommand { get; }

        public StockProcesoReservasViewModel()
        {
            BuscarCommand = new RelayCommand(_ => Filtrar());
            LimpiarCommand = new RelayCommand(_ => Limpiar());
            ActualizarCommand = new RelayCommand(_ => CargarDatos());
            CargarDatos();
        }

        public StockProcesoReservasViewModel(string filtroInicial) : this()
        {
            _textoBusqueda = filtroInicial;
            OnPropertyChanged(nameof(TextoBusqueda));
            Filtrar();
        }

        private void CargarDatos()
        {
            _reservasBase.Clear();
            _reservasBase.AddRange(_stockProductoNegocio.ListarReservasProceso());
            Filtrar();
        }

        private void Filtrar()
        {
            string texto = TextoBusqueda.Trim();
            string area = AreaBusqueda.Trim();
            DateTime? desde = FechaDesde?.Date;
            DateTime? hasta = FechaHasta?.Date.AddDays(1).AddTicks(-1);

            List<StockProcesoReservaReporte> filtradas = _reservasBase
                .Where(x =>
                    (string.IsNullOrWhiteSpace(texto)
                        || x.CodigoProducto.Contains(texto, StringComparison.OrdinalIgnoreCase)
                        || x.NombreProducto.Contains(texto, StringComparison.OrdinalIgnoreCase)
                        || x.NumeroOT.Contains(texto, StringComparison.OrdinalIgnoreCase))
                    && (string.IsNullOrWhiteSpace(area)
                        || x.NombreArea.Contains(area, StringComparison.OrdinalIgnoreCase))
                    && (!desde.HasValue || x.FechaRegistro >= desde.Value)
                    && (!hasta.HasValue || x.FechaRegistro <= hasta.Value))
                .ToList();

            Reservas.Clear();
            foreach (StockProcesoReservaReporte reserva in filtradas)
                Reservas.Add(reserva);

            CantidadTotal = Reservas.Sum(x => x.CantidadReservada);
            CantidadUsadaTotal = Reservas.Sum(x => x.CantidadAplicada);
            CantidadDisponibleTotal = Reservas.Sum(x => x.CantidadDisponible);
            OnPropertyChanged(nameof(TotalRegistros));
            OnPropertyChanged(nameof(TotalProductosReservados));
        }

        private void Limpiar()
        {
            _textoBusqueda = string.Empty;
            _areaBusqueda = string.Empty;
            _fechaDesde = null;
            _fechaHasta = null;
            OnPropertyChanged(nameof(TextoBusqueda));
            OnPropertyChanged(nameof(AreaBusqueda));
            OnPropertyChanged(nameof(FechaDesde));
            OnPropertyChanged(nameof(FechaHasta));
            Filtrar();
        }
    }
}
