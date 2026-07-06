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

        public int TotalRegistros => Reservas.Count;
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

            List<StockProcesoReservaReporte> filtradas = _reservasBase
                .Where(x =>
                    (string.IsNullOrWhiteSpace(texto)
                        || x.CodigoProducto.Contains(texto, StringComparison.OrdinalIgnoreCase)
                        || x.NombreProducto.Contains(texto, StringComparison.OrdinalIgnoreCase)
                        || x.NumeroOT.Contains(texto, StringComparison.OrdinalIgnoreCase))
                    && (string.IsNullOrWhiteSpace(area)
                        || x.NombreArea.Contains(area, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            Reservas.Clear();
            foreach (StockProcesoReservaReporte reserva in filtradas)
                Reservas.Add(reserva);

            CantidadTotal = Reservas.Sum(x => x.CantidadDisponible);
            OnPropertyChanged(nameof(TotalRegistros));
        }

        private void Limpiar()
        {
            _textoBusqueda = string.Empty;
            _areaBusqueda = string.Empty;
            OnPropertyChanged(nameof(TextoBusqueda));
            OnPropertyChanged(nameof(AreaBusqueda));
            Filtrar();
        }
    }
}
