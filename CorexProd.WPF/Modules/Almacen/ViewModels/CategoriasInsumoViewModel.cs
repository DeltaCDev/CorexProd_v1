using CorexProd.Entidad;
using CorexProd.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace CorexProd.WPF.ViewModels
{
    public class CategoriasInsumoViewModel : BaseViewModel
    {
        private readonly CategoriaInsumoNegocio _categoriaNegocio = new();

        private ObservableCollection<CategoriaInsumo> _categorias;
        public ObservableCollection<CategoriaInsumo> Categorias
        {
            get => _categorias;
            set
            {
                _categorias = value;
                OnPropertyChanged();
            }
        }

        private CategoriaInsumo _categoriaSeleccionada;
        public CategoriaInsumo CategoriaSeleccionada
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
            set
            {
                _idCategoriaInsumo = value;
                OnPropertyChanged();
            }
        }

        private string _nombreCategoria;
        public string NombreCategoria
        {
            get => _nombreCategoria;
            set
            {
                _nombreCategoria = value;
                OnPropertyChanged();
            }
        }

        private string _descripcion;
        public string Descripcion
        {
            get => _descripcion;
            set
            {
                _descripcion = value;
                OnPropertyChanged();
            }
        }

        private bool _estado = true;
        public bool Estado
        {
            get => _estado;
            set
            {
                _estado = value;
                OnPropertyChanged();
            }
        }

        public ICommand NuevoCommand { get; }
        public ICommand GuardarCommand { get; }
        public ICommand EditarCommand { get; }
        public ICommand EliminarCommand { get; }

        public CategoriasInsumoViewModel()
        {
            NuevoCommand = new RelayCommand(_ => Limpiar());
            GuardarCommand = new RelayCommand(_ => Guardar());
            EditarCommand = new RelayCommand(_ => Editar());
            EliminarCommand = new RelayCommand(_ => Eliminar());

            CargarCategorias();
        }

        private void CargarCategorias()
        {
            Categorias = new ObservableCollection<CategoriaInsumo>(_categoriaNegocio.Listar());
        }

        private void Guardar()
        {
            try
            {
                CategoriaInsumo categoria = new()
                {
                    NombreCategoria = NombreCategoria?.Trim().ToUpper(),
                    Descripcion = Descripcion?.Trim(),
                    Estado = true
                };

                _categoriaNegocio.Registrar(categoria);

                NotificationService.Success("Categoría registrada correctamente.");
                CargarCategorias();
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
                if (IdCategoriaInsumo <= 0)
                {
                    NotificationService.Warning("Seleccione una categoría.");
                    return;
                }

                bool confirma = ConfirmDialogService.Confirm(
                    "Editar categoría",
                    "¿Está seguro de editar esta categoría?"
                );

                if (!confirma)
                    return;

                CategoriaInsumo categoria = new()
                {
                    IdCategoriaInsumo = IdCategoriaInsumo,
                    NombreCategoria = NombreCategoria?.Trim().ToUpper(),
                    Descripcion = Descripcion?.Trim(),
                    Estado = Estado
                };

                _categoriaNegocio.Editar(categoria);

                NotificationService.Success("Categoría actualizada correctamente.");
                CargarCategorias();
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
                if (CategoriaSeleccionada == null)
                {
                    NotificationService.Warning("Seleccione una categoría.");
                    return;
                }

                bool confirma = ConfirmDialogService.Confirm(
                    "Eliminar categoría",
                    "¿Está seguro de eliminar esta categoría?"
                );

                if (!confirma)
                    return;

                _categoriaNegocio.Eliminar(CategoriaSeleccionada);

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
        }
    }
}