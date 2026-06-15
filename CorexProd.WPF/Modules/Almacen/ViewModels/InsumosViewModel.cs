using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Linq;
using System;

namespace CorexProd.WPF.ViewModels
{
    public class InsumosViewModel : BaseViewModel
    {
        private readonly InsumoNegocio _insumoNegocio = new();
        private readonly CategoriaInsumoNegocio _categoriaNegocio = new();
        private readonly UnidadMedidaNegocio _unidadMedidaNegocio = new();

        public ObservableCollection<Insumo> Insumos { get; set; } = [];
        public ObservableCollection<CategoriaInsumo> Categorias { get; set; } = [];
        public ObservableCollection<UnidadMedida> UnidadesMedida { get; set; } = [];

        private Insumo? _insumoSeleccionado;
        public Insumo? InsumoSeleccionado
        {
            get => _insumoSeleccionado;
            set
            {
                _insumoSeleccionado = value;
                OnPropertyChanged();

                if (value != null)
                {
                    IdInsumo = value.IdInsumo;
                    Codigo = value.Codigo;
                    NombreInsumo = value.NombreInsumo;
                    IdCategoriaInsumo = value.IdCategoriaInsumo;
                    IdUnidadMedida = value.IdUnidadMedida;
                    StockMinimo = value.StockMinimo;
                    Estado = value.Estado;
                }
            }
        }

        private int _idInsumo;
        public int IdInsumo
        {
            get => _idInsumo;
            set { _idInsumo = value; OnPropertyChanged(); }
        }

        private string _codigo = string.Empty;
        public string Codigo
        {
            get => _codigo;
            set { _codigo = value; OnPropertyChanged(); }
        }

        private string _nombreInsumo = string.Empty;
        public string NombreInsumo
        {
            get => _nombreInsumo;
            set { _nombreInsumo = value; OnPropertyChanged(); }
        }

        private int _idCategoriaInsumo;
        public int IdCategoriaInsumo
        {
            get => _idCategoriaInsumo;
            set { _idCategoriaInsumo = value; OnPropertyChanged(); }
        }

        private int _idUnidadMedida;
        public int IdUnidadMedida
        {
            get => _idUnidadMedida;
            set { _idUnidadMedida = value; OnPropertyChanged(); }
        }

        private decimal _stockMinimo;
        public decimal StockMinimo
        {
            get => _stockMinimo;
            set { _stockMinimo = value; OnPropertyChanged(); }
        }

        private bool _estado = true;
        public bool Estado
        {
            get => _estado;
            set { _estado = value; OnPropertyChanged(); }
        }

        public ICommand NuevoCommand { get; }
        public ICommand GuardarCommand { get; }
        public ICommand EditarCommand { get; }
        public ICommand EliminarCommand { get; }

        public InsumosViewModel()
        {
            NuevoCommand = new RelayCommand(_ => Limpiar());
            GuardarCommand = new RelayCommand(_ => Guardar());
            EditarCommand = new RelayCommand(_ => Editar());
            EliminarCommand = new RelayCommand(_ => Eliminar());

            CargarCombos();
            CargarInsumos();
        }

        private void CargarCombos()
        {
            Categorias = new ObservableCollection<CategoriaInsumo>(
                _categoriaNegocio.Listar().Where(c => c.Estado)
            );

            UnidadesMedida = new ObservableCollection<UnidadMedida>(
                _unidadMedidaNegocio.Listar().Where(u => u.Estado)
            );

            OnPropertyChanged(nameof(Categorias));
            OnPropertyChanged(nameof(UnidadesMedida));
        }

        private void CargarInsumos()
        {
            Insumos = new ObservableCollection<Insumo>(_insumoNegocio.Listar());
            OnPropertyChanged(nameof(Insumos));
        }

        private void Guardar()
        {
            try
            {
                Insumo insumo = new()
                {
                    Codigo = Codigo?.Trim().ToUpper() ?? string.Empty,
                    NombreInsumo = NombreInsumo?.Trim().ToUpper() ?? string.Empty,
                    IdCategoriaInsumo = IdCategoriaInsumo,
                    IdUnidadMedida = IdUnidadMedida,
                    StockMinimo = StockMinimo,
                    Estado = true
                };

                _insumoNegocio.Registrar(insumo);

                NotificationService.Success("Insumo registrado correctamente.");
                CargarInsumos();
                Limpiar();
            }
            catch (Exception ex)
            {
                NotificationService.Error(ex.Message);
            }
        }

        private void Editar()
        {
            try
            {
                if (IdInsumo <= 0)
                {
                    NotificationService.Warning("Seleccione un insumo.");
                    return;
                }

                bool confirma = ConfirmDialogService.Confirmar(
                    "Editar insumo",
                    "¿Está seguro de editar este insumo?"
                );

                if (!confirma)
                    return;

                Insumo insumo = new()
                {
                    IdInsumo = IdInsumo,
                    Codigo = Codigo?.Trim().ToUpper() ?? string.Empty,
                    NombreInsumo = NombreInsumo?.Trim().ToUpper() ?? string.Empty,
                    IdCategoriaInsumo = IdCategoriaInsumo,
                    IdUnidadMedida = IdUnidadMedida,
                    StockMinimo = StockMinimo,
                    Estado = Estado
                };

                _insumoNegocio.Editar(insumo);

                NotificationService.Success("Insumo actualizado correctamente.");
                CargarInsumos();
                Limpiar();
            }
            catch (Exception ex)
            {
                NotificationService.Error(ex.Message);
            }
        }

        private void Eliminar()
        {
            try
            {
                if (InsumoSeleccionado == null)
                {
                    NotificationService.Warning("Seleccione un insumo.");
                    return;
                }

                bool confirma = ConfirmDialogService.Confirmar(
                    "Eliminar insumo",
                    "¿Está seguro de eliminar este insumo?"
                );

                if (!confirma)
                    return;

                _insumoNegocio.Eliminar(InsumoSeleccionado);

                NotificationService.Success("Insumo eliminado correctamente.");
                CargarInsumos();
                Limpiar();
            }
            catch (Exception ex)
            {
                NotificationService.Error(ex.Message);
            }
        }

        private void Limpiar()
        {
            IdInsumo = 0;
            Codigo = string.Empty;
            NombreInsumo = string.Empty;
            IdCategoriaInsumo = 0;
            IdUnidadMedida = 0;
            StockMinimo = 0;
            Estado = true;
            InsumoSeleccionado = null;
        }
    }
}