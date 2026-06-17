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
    public class StockInsumosViewModel : BaseViewModel
    {
        private readonly StockInsumoNegocio _stockInsumoNegocio = new();
        private readonly CategoriaInsumoNegocio _categoriaInsumoNegocio = new();
        private readonly List<StockInsumo> _stockInsumos = [];
        private string _textoBusqueda = string.Empty;
        private int _idCategoriaSeleccionada;
        private decimal _cantidadTotal;

        public ObservableCollection<StockInsumo> StockInsumos { get; } = [];
        public ObservableCollection<CategoriaInsumo> Categorias { get; } = [];

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

        public int TotalInsumos => StockInsumos.Count;

        public ICommand BuscarCommand { get; }
        public ICommand LimpiarCommand { get; }
        public ICommand ActualizarCommand { get; }

        public StockInsumosViewModel()
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
            Categorias.Add(new CategoriaInsumo
            {
                IdCategoriaInsumo = 0,
                NombreCategoria = "Todas las categorias"
            });

            foreach (CategoriaInsumo categoria in _categoriaInsumoNegocio.Listar())
            {
                if (categoria.Estado)
                {
                    Categorias.Add(categoria);
                }
            }
        }

        private void CargarDatos()
        {
            _stockInsumos.Clear();
            _stockInsumos.AddRange(_stockInsumoNegocio.Listar());
            Filtrar();
        }

        private void Filtrar()
        {
            string texto = TextoBusqueda.Trim();

            List<StockInsumo> filtrados = _stockInsumos
                .Where(insumo =>
                    (IdCategoriaSeleccionada == 0 || insumo.IdCategoriaInsumo == IdCategoriaSeleccionada)
                    && (string.IsNullOrWhiteSpace(texto)
                        || insumo.Codigo.Contains(texto, StringComparison.OrdinalIgnoreCase)
                        || insumo.NombreInsumo.Contains(texto, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(insumo => insumo.NombreInsumo)
                .ToList();

            StockInsumos.Clear();

            foreach (StockInsumo insumo in filtrados)
            {
                StockInsumos.Add(insumo);
            }

            CantidadTotal = StockInsumos.Sum(insumo => insumo.Cantidad);
            OnPropertyChanged(nameof(TotalInsumos));
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
