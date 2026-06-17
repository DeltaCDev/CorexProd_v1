using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.Views;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace CorexProd.WPF.ViewModels
{
    public class CategoriasInsumoViewModel : BaseViewModel
    {
        private readonly CategoriaInsumoNegocio _categoriaNegocio = new();

        public ObservableCollection<CategoriaInsumo> Categorias { get; set; } = [];

        private CategoriaInsumo? _categoriaSeleccionada;
        public CategoriaInsumo? CategoriaSeleccionada
        {
            get => _categoriaSeleccionada;
            set
            {
                _categoriaSeleccionada = value;
                OnPropertyChanged();

                if (value != null)
                {
                    IdCategoriaInsumo = value.IdCategoriaInsumo;
                    NombreCategoria = value.NombreCategoria;
                    Descripcion = value.Descripcion;
                    Estado = value.Estado;
                }
            }
        }

        private int _idCategoriaInsumo;
        public int IdCategoriaInsumo
        {
            get => _idCategoriaInsumo;
            set { _idCategoriaInsumo = value; OnPropertyChanged(); }
        }

        private string _nombreCategoria = string.Empty;
        public string NombreCategoria
        {
            get => _nombreCategoria;
            set { _nombreCategoria = value; OnPropertyChanged(); }
        }

        private string _descripcion = string.Empty;
        public string Descripcion
        {
            get => _descripcion;
            set { _descripcion = value; OnPropertyChanged(); }
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
        public string TituloEditor => IdCategoriaInsumo > 0 ? "Editar Categoria de Insumo" : "Nueva Categoria de Insumo";
        public string ResumenRegistros => $"Mostrando {Categorias.Count} categorias de insumos";

        public CategoriasInsumoViewModel()
        {
            NuevoCommand = new RelayCommand(_ => AbrirEditor(null));
            GuardarCommand = new RelayCommand(_ => Guardar());
            EditarCommand = new RelayCommand(parametro => Editar(parametro));
            EliminarCommand = new RelayCommand(parametro => Eliminar(parametro));
            LimpiarCommand = new RelayCommand(_ => Limpiar());
            RefrescarCommand = new RelayCommand(_ => CargarCategorias());
            CerrarCommand = new RelayCommand(_ => CerrarVentana?.Invoke());

            CargarCategorias();
        }

        private void CargarCategorias()
        {
            Categorias = new ObservableCollection<CategoriaInsumo>(_categoriaNegocio.Listar());
            OnPropertyChanged(nameof(Categorias));
            OnPropertyChanged(nameof(ResumenRegistros));
        }

        private void Guardar()
        {
            try
            {
                if (IdCategoriaInsumo > 0)
                {
                    bool confirma = ConfirmDialogService.Confirmar(
                        "¿Está seguro de editar esta categoría?",
                        "Editar categoría");

                    if (!confirma)
                    {
                        return;
                    }
                }

                CategoriaInsumo categoria = new()
                {
                    IdCategoriaInsumo = IdCategoriaInsumo,
                    NombreCategoria = NombreCategoria?.Trim().ToUpper() ?? string.Empty,
                    Descripcion = Descripcion?.Trim() ?? string.Empty,
                    Estado = Estado
                };

                if (IdCategoriaInsumo > 0)
                {
                    _categoriaNegocio.Editar(categoria);
                    NotificationService.Success("Categoría actualizada correctamente.");
                }
                else
                {
                    _categoriaNegocio.Registrar(categoria);
                    NotificationService.Success("Categoría registrada correctamente.");
                }

                Guardado = true;
                CargarCategorias();
                CerrarVentana?.Invoke();
            }
            catch (Exception ex)
            {
                NotificationService.Error(ex.Message);
            }
        }

        private void Editar(object? parametro)
        {
            if (parametro is not CategoriaInsumo categoria)
            {
                NotificationService.Warning("Seleccione una categoría.");
                return;
            }

            AbrirEditor(categoria);
        }

        private void Eliminar(object? parametro)
        {
            try
            {
                CategoriaInsumo? categoria = parametro as CategoriaInsumo ?? CategoriaSeleccionada;

                if (categoria == null)
                {
                    NotificationService.Warning("Seleccione una categoría.");
                    return;
                }

                bool confirma = ConfirmDialogService.Confirmar(
                    "¿Está seguro de eliminar esta categoría?",
                    "Eliminar categoría");

                if (!confirma)
                    return;

                _categoriaNegocio.Eliminar(categoria);

                NotificationService.Success("Categoría eliminada correctamente.");
                CargarCategorias();
                Limpiar();
            }
            catch (Exception ex)
            {
                NotificationService.Error(ex.Message);
            }
        }

        private void Limpiar()
        {
            IdCategoriaInsumo = 0;
            NombreCategoria = string.Empty;
            Descripcion = string.Empty;
            Estado = true;
            CategoriaSeleccionada = null;
            OnPropertyChanged(nameof(TituloEditor));
        }

        private void AbrirEditor(CategoriaInsumo? categoria)
        {
            CategoriasInsumoViewModel viewModel = new();

            if (categoria != null)
            {
                viewModel.IdCategoriaInsumo = categoria.IdCategoriaInsumo;
                viewModel.NombreCategoria = categoria.NombreCategoria;
                viewModel.Descripcion = categoria.Descripcion;
                viewModel.Estado = categoria.Estado;
                viewModel.OnPropertyChanged(nameof(TituloEditor));
            }

            CategoriaInsumoEditorWindow ventana = new()
            {
                DataContext = viewModel,
                Owner = Application.Current.MainWindow
            };

            viewModel.CerrarVentana = ventana.Close;
            ventana.ShowDialog();

            if (viewModel.Guardado)
            {
                CargarCategorias();
                Limpiar();
            }
        }
    }
}
