using CorexProd.Entidad.Entidades;
using CorexProd.WPF.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace CorexProd.WPF.Modules.Almacen.ViewModels
{
    public class IngresoManualStockInsumoDetalleViewModel : BaseViewModel
    {
        private readonly Action _recalcularTotales;
        private readonly Func<string, ObservableCollection<InsumoStockBusqueda>> _buscarInsumos;
        private InsumoStockBusqueda? _insumoSeleccionado;
        private string _textoBusquedaInsumo = string.Empty;
        private bool _insumoDropdownAbierto;
        private decimal _stockActual;
        private decimal _cantidad = 1;
        private decimal _precioUnitario;
        private decimal _descuento;

        public ObservableCollection<InsumoStockBusqueda> InsumosFiltrados { get; } = [];

        public IngresoManualStockInsumoDetalleViewModel(
            Action recalcularTotales,
            Func<string, ObservableCollection<InsumoStockBusqueda>> buscarInsumos)
        {
            _recalcularTotales = recalcularTotales;
            _buscarInsumos = buscarInsumos;
        }

        public int IdInsumo { get; private set; }
        public string CodigoInsumo { get; private set; } = string.Empty;
        public string NombreInsumo { get; private set; } = string.Empty;
        public int IdUnidadMedida { get; private set; }
        public string NombreUnidad { get; private set; } = string.Empty;

        public string TextoBusquedaInsumo
        {
            get => _textoBusquedaInsumo;
            set
            {
                _textoBusquedaInsumo = value;
                OnPropertyChanged();
                Buscar();
            }
        }

        public InsumoStockBusqueda? InsumoSeleccionado
        {
            get => _insumoSeleccionado;
            set
            {
                _insumoSeleccionado = value;
                OnPropertyChanged();

                if (value != null)
                {
                    AsignarInsumo(value);
                }
            }
        }

        public bool InsumoDropdownAbierto
        {
            get => _insumoDropdownAbierto;
            set
            {
                _insumoDropdownAbierto = value;
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

        public void AsignarInsumo(InsumoStockBusqueda insumo)
        {
            IdInsumo = insumo.IdInsumo;
            CodigoInsumo = insumo.Codigo;
            NombreInsumo = insumo.NombreInsumo;
            IdUnidadMedida = insumo.IdUnidadMedida;
            NombreUnidad = insumo.NombreUnidad;
            StockActual = insumo.StockActual;
            _textoBusquedaInsumo = $"{insumo.Codigo} - {insumo.NombreInsumo}";
            InsumoDropdownAbierto = false;

            OnPropertyChanged(nameof(IdInsumo));
            OnPropertyChanged(nameof(CodigoInsumo));
            OnPropertyChanged(nameof(NombreInsumo));
            OnPropertyChanged(nameof(IdUnidadMedida));
            OnPropertyChanged(nameof(NombreUnidad));
            OnPropertyChanged(nameof(TextoBusquedaInsumo));
            _recalcularTotales();
        }

        public void ActualizarStock(decimal stock)
        {
            StockActual = stock;
        }

        public IngresoManualStockInsumoDetalle ToEntity()
        {
            return new IngresoManualStockInsumoDetalle
            {
                IdInsumo = IdInsumo,
                CodigoInsumo = CodigoInsumo,
                NombreInsumo = NombreInsumo,
                IdUnidadMedida = IdUnidadMedida,
                NombreUnidad = NombreUnidad,
                StockActual = StockActual,
                Cantidad = Cantidad,
                PrecioUnitario = PrecioUnitario,
                Descuento = Descuento,
                Importe = Importe
            };
        }

        public static IngresoManualStockInsumoDetalleViewModel FromEntity(
            IngresoManualStockInsumoDetalle detalle,
            Action recalcularTotales,
            Func<string, ObservableCollection<InsumoStockBusqueda>> buscarInsumos)
        {
            IngresoManualStockInsumoDetalleViewModel vm = new(recalcularTotales, buscarInsumos);
            vm.IdInsumo = detalle.IdInsumo;
            vm.CodigoInsumo = detalle.CodigoInsumo;
            vm.NombreInsumo = detalle.NombreInsumo;
            vm.IdUnidadMedida = detalle.IdUnidadMedida;
            vm.NombreUnidad = detalle.NombreUnidad;
            vm.StockActual = detalle.StockActual;
            vm.Cantidad = detalle.Cantidad;
            vm.PrecioUnitario = detalle.PrecioUnitario;
            vm.Descuento = detalle.Descuento;
            vm._textoBusquedaInsumo = $"{detalle.CodigoInsumo} - {detalle.NombreInsumo}";
            return vm;
        }

        private void Buscar()
        {
            InsumosFiltrados.Clear();

            if (TextoBusquedaInsumo.Length < 2)
            {
                InsumoDropdownAbierto = false;
                return;
            }

            foreach (InsumoStockBusqueda insumo in _buscarInsumos(TextoBusquedaInsumo).Take(25))
            {
                InsumosFiltrados.Add(insumo);
            }

            InsumoDropdownAbierto = InsumosFiltrados.Count > 0;
        }
    }
}


