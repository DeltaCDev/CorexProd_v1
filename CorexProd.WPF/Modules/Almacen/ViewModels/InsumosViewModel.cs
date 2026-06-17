using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.Views;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Linq;
using System;
using System.Windows;

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
        public ICommand LimpiarCommand { get; }
        public ICommand RefrescarCommand { get; }
        public ICommand CerrarCommand { get; }

        public Action? CerrarVentana { get; set; }
        public bool Guardado { get; private set; }
        public string TituloEditor => IdInsumo > 0 ? "Editar Insumo" : "Nuevo Insumo";
        public string ResumenRegistros => $"Mostrando {Insumos.Count} insumos";

        public InsumosViewModel()
        {
            NuevoCommand = new RelayCommand(_ => AbrirEditor(null));
            GuardarCommand = new RelayCommand(_ => Guardar());
            EditarCommand = new RelayCommand(parametro => Editar(parametro));
            EliminarCommand = new RelayCommand(parametro => Eliminar(parametro));
            LimpiarCommand = new RelayCommand(_ => Limpiar());
            RefrescarCommand = new RelayCommand(_ => Refrescar());
            CerrarCommand = new RelayCommand(_ => CerrarVentana?.Invoke());

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
            OnPropertyChanged(nameof(ResumenRegistros));
        }

        private void Guardar()
        {
            try
            {
                if (IdInsumo > 0)
                {
                    bool confirma = ConfirmDialogService.Confirmar(
                        "¿Está seguro de editar este insumo?",
                        "Editar insumo");

                    if (!confirma)
                    {
                        return;
                    }
                }

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

                if (IdInsumo > 0)
                {
                    _insumoNegocio.Editar(insumo);
                    NotificationService.Success("Insumo actualizado correctamente.");
                }
                else
                {
                    _insumoNegocio.Registrar(insumo);
                    NotificationService.Success("Insumo registrado correctamente.");
                }

                Guardado = true;
                CargarInsumos();
                CerrarVentana?.Invoke();
            }
            catch (Exception ex)
            {
                NotificationService.Error(ex.Message);
            }
        }

        private void Editar(object? parametro)
        {
            if (parametro is not Insumo insumo)
            {
                NotificationService.Warning("Seleccione un insumo.");
                return;
            }

            AbrirEditor(insumo);
        }

        private void Eliminar(object? parametro)
        {
            try
            {
                Insumo? insumo = parametro as Insumo ?? InsumoSeleccionado;

                if (insumo == null)
                {
                    NotificationService.Warning("Seleccione un insumo.");
                    return;
                }

                bool confirma = ConfirmDialogService.Confirmar(
                    "¿Está seguro de eliminar este insumo?",
                    "Eliminar insumo");

                if (!confirma)
                    return;

                _insumoNegocio.Eliminar(insumo);

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
            OnPropertyChanged(nameof(TituloEditor));
        }

        private void Refrescar()
        {
            CargarCombos();
            CargarInsumos();
        }

        private void AbrirEditor(Insumo? insumo)
        {
            InsumosViewModel viewModel = new();

            if (insumo != null)
            {
                viewModel.IdInsumo = insumo.IdInsumo;
                viewModel.Codigo = insumo.Codigo;
                viewModel.NombreInsumo = insumo.NombreInsumo;
                viewModel.IdCategoriaInsumo = insumo.IdCategoriaInsumo;
                viewModel.IdUnidadMedida = insumo.IdUnidadMedida;
                viewModel.StockMinimo = insumo.StockMinimo;
                viewModel.Estado = insumo.Estado;
                viewModel.OnPropertyChanged(nameof(TituloEditor));
            }

            InsumoEditorWindow ventana = new()
            {
                DataContext = viewModel,
                Owner = Application.Current.MainWindow
            };

            viewModel.CerrarVentana = ventana.Close;
            ventana.ShowDialog();

            if (viewModel.Guardado)
            {
                Refrescar();
                Limpiar();
            }
        }
    }
}
