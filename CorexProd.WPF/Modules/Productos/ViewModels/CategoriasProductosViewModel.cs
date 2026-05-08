using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.ViewModels;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Productos.ViewModels
{
    public class CategoriasProductosViewModel : BaseViewModel
    {
        private readonly CategoriaProductoNegocio _categoriaProductoNegocio = new();

        private int _idCategoriaProducto;
        private string _nombreCategoria = string.Empty;
        private string _descripcion = string.Empty;
        private bool _estado = true;
        private CategoriaProducto? _categoriaSeleccionada;

        public ObservableCollection<CategoriaProducto> Categorias { get; set; } = [];

        public int IdCategoriaProducto
        {
            get => _idCategoriaProducto;
            set
            {
                _idCategoriaProducto = value;
                OnPropertyChanged();
            }
        }

        public string NombreCategoria
        {
            get => _nombreCategoria;
            set
            {
                _nombreCategoria = value;
                OnPropertyChanged();
            }
        }

        public string Descripcion
        {
            get => _descripcion;
            set
            {
                _descripcion = value;
                OnPropertyChanged();
            }
        }

        public bool Estado
        {
            get => _estado;
            set
            {
                _estado = value;
                OnPropertyChanged();
            }
        }

        public CategoriaProducto? CategoriaSeleccionada
        {
            get => _categoriaSeleccionada;
            set
            {
                _categoriaSeleccionada = value;
                OnPropertyChanged();

                if (_categoriaSeleccionada != null)
                {
                    IdCategoriaProducto = _categoriaSeleccionada.IdCategoriaProducto;
                    NombreCategoria = _categoriaSeleccionada.NombreCategoria;
                    Descripcion = _categoriaSeleccionada.Descripcion;
                    Estado = _categoriaSeleccionada.Estado;
                }
            }
        }

        public ICommand GuardarCommand { get; }
        public ICommand LimpiarCommand { get; }
        public ICommand EliminarCommand { get; }

        public CategoriasProductosViewModel()
        {
            GuardarCommand = new RelayCommand(_ => Guardar());
            LimpiarCommand = new RelayCommand(_ => Limpiar());
            EliminarCommand = new RelayCommand(parametro => Eliminar(parametro));

            CargarCategorias();
        }

        private void CargarCategorias()
        {
            Categorias.Clear();

            foreach (CategoriaProducto categoria in _categoriaProductoNegocio.Listar())
            {
                Categorias.Add(categoria);
            }
        }

        private void Guardar()
        {
            if (IdCategoriaProducto > 0)
            {
                bool confirmar = ConfirmDialogService.Confirmar(
                    "¿Desea actualizar la información de la categoría?",
                    "Confirmar actualización");

                if (!confirmar)
                {
                    return;
                }
            }

            CategoriaProducto categoria = new()
            {
                IdCategoriaProducto = IdCategoriaProducto,
                NombreCategoria = NombreCategoria,
                Descripcion = Descripcion,
                Estado = Estado
            };

            string mensaje = _categoriaProductoNegocio.Guardar(categoria);

            if (mensaje.Contains("correctamente"))
            {
                NotificationService.Success(mensaje);
                CargarCategorias();
                Limpiar();
            }
            else
            {
                NotificationService.Warning(mensaje);
            }
        }

        private void Eliminar(object? parametro)
        {
            if (parametro == null)
            {
                NotificationService.Warning("Debe seleccionar una categoría.");
                return;
            }

            if (!int.TryParse(parametro.ToString(), out int idCategoriaProducto))
            {
                NotificationService.Warning("Id de categoría inválido.");
                return;
            }

            bool confirmar = ConfirmDialogService.Confirmar(
                "¿Está seguro de eliminar esta categoría?",
                "Confirmar eliminación");

            if (!confirmar)
            {
                return;
            }

            string mensaje = _categoriaProductoNegocio.Eliminar(idCategoriaProducto);

            if (mensaje.Contains("correctamente"))
            {
                NotificationService.Success(mensaje);
                CargarCategorias();
                Limpiar();
            }
            else
            {
                NotificationService.Warning(mensaje);
            }
        }

        private void Limpiar()
        {
            IdCategoriaProducto = 0;
            NombreCategoria = string.Empty;
            Descripcion = string.Empty;
            Estado = true;
            CategoriaSeleccionada = null;
        }
    }
}