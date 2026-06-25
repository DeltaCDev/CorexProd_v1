using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.Modules.Productos.Views;
using CorexProd.WPF.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Productos.ViewModels
{
    public class SuperCategoriasProductosViewModel : BaseViewModel
    {
        private readonly SuperCategoriaProductoNegocio _superCategoriaProductoNegocio = new();

        private int _idSuperCategoriaProducto;
        private string _nombreSuperCategoria = string.Empty;
        private string _descripcion = string.Empty;
        private bool _estado = true;
        private SuperCategoriaProducto? _superCategoriaSeleccionada;

        public ObservableCollection<SuperCategoriaProducto> SuperCategorias { get; set; } = [];

        public int IdSuperCategoriaProducto
        {
            get => _idSuperCategoriaProducto;
            set
            {
                _idSuperCategoriaProducto = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TituloEditor));
            }
        }

        public string NombreSuperCategoria
        {
            get => _nombreSuperCategoria;
            set
            {
                _nombreSuperCategoria = value;
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

        public SuperCategoriaProducto? SuperCategoriaSeleccionada
        {
            get => _superCategoriaSeleccionada;
            set
            {
                _superCategoriaSeleccionada = value;
                OnPropertyChanged();

                if (_superCategoriaSeleccionada != null)
                {
                    IdSuperCategoriaProducto = _superCategoriaSeleccionada.IdSuperCategoriaProducto;
                    NombreSuperCategoria = _superCategoriaSeleccionada.NombreSuperCategoria;
                    Descripcion = _superCategoriaSeleccionada.Descripcion;
                    Estado = _superCategoriaSeleccionada.Estado;
                }
            }
        }

        public ICommand GuardarCommand { get; }
        public ICommand LimpiarCommand { get; }
        public ICommand EliminarCommand { get; }
        public ICommand NuevoCommand { get; }
        public ICommand EditarCommand { get; }
        public ICommand RefrescarCommand { get; }
        public ICommand CerrarCommand { get; }

        public Action? CerrarVentana { get; set; }
        public bool Guardado { get; private set; }
        public string TituloEditor => IdSuperCategoriaProducto > 0 ? "Editar Supercategoría" : "Nueva Supercategoría";
        public string ResumenRegistros => $"Mostrando {SuperCategorias.Count} supercategorías";

        public SuperCategoriasProductosViewModel()
        {
            GuardarCommand = new RelayCommand(_ => Guardar());
            LimpiarCommand = new RelayCommand(_ => Limpiar());
            EliminarCommand = new RelayCommand(parametro => Eliminar(parametro));
            NuevoCommand = new RelayCommand(_ => AbrirEditor(null));
            EditarCommand = new RelayCommand(parametro => Editar(parametro));
            RefrescarCommand = new RelayCommand(_ => CargarSuperCategorias());
            CerrarCommand = new RelayCommand(_ => CerrarVentana?.Invoke());

            CargarSuperCategorias();
        }

        private void CargarSuperCategorias()
        {
            SuperCategorias.Clear();

            foreach (SuperCategoriaProducto superCategoria in _superCategoriaProductoNegocio.Listar())
            {
                SuperCategorias.Add(superCategoria);
            }

            OnPropertyChanged(nameof(ResumenRegistros));
        }

        private void Guardar()
        {
            if (IdSuperCategoriaProducto > 0)
            {
                bool confirmar = ConfirmDialogService.Confirmar(
                    "¿Desea actualizar la información de la supercategoría?",
                    "Confirmar actualización");

                if (!confirmar)
                {
                    return;
                }
            }

            SuperCategoriaProducto superCategoria = new()
            {
                IdSuperCategoriaProducto = IdSuperCategoriaProducto,
                NombreSuperCategoria = NombreSuperCategoria,
                Descripcion = Descripcion,
                Estado = Estado
            };

            string mensaje = _superCategoriaProductoNegocio.Guardar(superCategoria);

            if (mensaje.Contains("correctamente"))
            {
                NotificationService.Success(mensaje);
                CargarSuperCategorias();
                Guardado = true;
                CerrarVentana?.Invoke();
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
                NotificationService.Warning("Debe seleccionar una supercategoría.");
                return;
            }

            if (!int.TryParse(parametro.ToString(), out int idSuperCategoriaProducto))
            {
                NotificationService.Warning("Id de supercategoría inválido.");
                return;
            }

            bool confirmar = ConfirmDialogService.Confirmar(
                "¿Está seguro de eliminar esta supercategoría?",
                "Confirmar eliminación");

            if (!confirmar)
            {
                return;
            }

            string mensaje = _superCategoriaProductoNegocio.Eliminar(idSuperCategoriaProducto);

            if (mensaje.Contains("correctamente"))
            {
                NotificationService.Success(mensaje);
                CargarSuperCategorias();
                Limpiar();
            }
            else
            {
                NotificationService.Warning(mensaje);
            }
        }

        private void Limpiar()
        {
            IdSuperCategoriaProducto = 0;
            NombreSuperCategoria = string.Empty;
            Descripcion = string.Empty;
            Estado = true;
            SuperCategoriaSeleccionada = null;
        }

        private void AbrirEditor(SuperCategoriaProducto? superCategoria)
        {
            SuperCategoriasProductosViewModel viewModel = new();

            if (superCategoria != null)
            {
                viewModel.IdSuperCategoriaProducto = superCategoria.IdSuperCategoriaProducto;
                viewModel.NombreSuperCategoria = superCategoria.NombreSuperCategoria;
                viewModel.Descripcion = superCategoria.Descripcion;
                viewModel.Estado = superCategoria.Estado;
            }

            SuperCategoriaProductoEditorWindow ventana = new()
            {
                DataContext = viewModel,
                Owner = Application.Current.MainWindow
            };

            viewModel.CerrarVentana = ventana.Close;
            ventana.ShowDialog();

            if (viewModel.Guardado)
            {
                CargarSuperCategorias();
                Limpiar();
            }
        }

        private void Editar(object? parametro)
        {
            if (parametro is not SuperCategoriaProducto superCategoria)
            {
                NotificationService.Warning("Debe seleccionar una supercategoría.");
                return;
            }

            AbrirEditor(superCategoria);
        }
    }
}
