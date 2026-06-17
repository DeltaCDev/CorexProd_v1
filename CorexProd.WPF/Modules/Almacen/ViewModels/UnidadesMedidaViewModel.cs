using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.Modules.Almacen.Views;
using CorexProd.WPF.ViewModels;
using System.Collections.ObjectModel;
using System;
using System.Windows;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Almacen.ViewModels
{
    public class UnidadesMedidaViewModel : BaseViewModel
    {
        private readonly UnidadMedidaNegocio _unidadMedidaNegocio = new();

        private int _idUnidadMedida;
        private string _nombreUnidad = string.Empty;
        private string _abreviatura = string.Empty;
        private bool _estado = true;
        private UnidadMedida? _unidadSeleccionada;

        public ObservableCollection<UnidadMedida> UnidadesMedida { get; set; } = [];

        public int IdUnidadMedida
        {
            get => _idUnidadMedida;
            set
            {
                _idUnidadMedida = value;
                OnPropertyChanged();
            }
        }

        public string NombreUnidad
        {
            get => _nombreUnidad;
            set
            {
                _nombreUnidad = value;
                OnPropertyChanged();
            }
        }

        public string Abreviatura
        {
            get => _abreviatura;
            set
            {
                _abreviatura = value;
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

        public UnidadMedida? UnidadSeleccionada
        {
            get => _unidadSeleccionada;
            set
            {
                _unidadSeleccionada = value;
                OnPropertyChanged();

                if (_unidadSeleccionada != null)
                {
                    IdUnidadMedida = _unidadSeleccionada.IdUnidadMedida;
                    NombreUnidad = _unidadSeleccionada.NombreUnidad;
                    Abreviatura = _unidadSeleccionada.Abreviatura;
                    Estado = _unidadSeleccionada.Estado;
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
        public string TituloEditor => IdUnidadMedida > 0 ? "Editar Unidad de Medida" : "Nueva Unidad de Medida";
        public string ResumenRegistros => $"Mostrando {UnidadesMedida.Count} unidades de medida";

        public UnidadesMedidaViewModel()
        {
            GuardarCommand = new RelayCommand(_ => Guardar());
            LimpiarCommand = new RelayCommand(_ => Limpiar());
            EliminarCommand = new RelayCommand(parametro => Eliminar(parametro));
            NuevoCommand = new RelayCommand(_ => AbrirEditor(null));
            EditarCommand = new RelayCommand(parametro => Editar(parametro));
            RefrescarCommand = new RelayCommand(_ => CargarUnidadesMedida());
            CerrarCommand = new RelayCommand(_ => CerrarVentana?.Invoke());

            CargarUnidadesMedida();
        }

        private void CargarUnidadesMedida()
        {
            UnidadesMedida.Clear();

            foreach (UnidadMedida unidad in _unidadMedidaNegocio.Listar())
            {
                UnidadesMedida.Add(unidad);
            }

            OnPropertyChanged(nameof(ResumenRegistros));
        }

        private void Guardar()
        {
            if (IdUnidadMedida > 0)
            {
                bool confirmar = ConfirmDialogService.Confirmar(
                    "¿Desea actualizar la información de la unidad de medida?",
                    "Confirmar actualización");

                if (!confirmar)
                {
                    return;
                }
            }

            UnidadMedida unidad = new()
            {
                IdUnidadMedida = IdUnidadMedida,
                NombreUnidad = NombreUnidad,
                Abreviatura = Abreviatura,
                Estado = Estado
            };

            string mensaje = _unidadMedidaNegocio.Guardar(unidad);

            if (mensaje.Contains("correctamente"))
            {
                NotificationService.Success(mensaje);
                CargarUnidadesMedida();
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
                NotificationService.Warning("Debe seleccionar una unidad de medida.");
                return;
            }

            if (!int.TryParse(parametro.ToString(), out int idUnidadMedida))
            {
                NotificationService.Warning("Id de unidad de medida inválido.");
                return;
            }

            bool confirmar = ConfirmDialogService.Confirmar(
                "¿Está seguro de eliminar esta unidad de medida?",
                "Confirmar eliminación");

            if (!confirmar)
            {
                return;
            }

            string mensaje = _unidadMedidaNegocio.Eliminar(idUnidadMedida);

            if (mensaje.Contains("correctamente"))
            {
                NotificationService.Success(mensaje);
                CargarUnidadesMedida();
                Limpiar();
            }
            else
            {
                NotificationService.Warning(mensaje);
            }
        }

        private void Limpiar()
        {
            IdUnidadMedida = 0;
            NombreUnidad = string.Empty;
            Abreviatura = string.Empty;
            Estado = true;
            UnidadSeleccionada = null;
            OnPropertyChanged(nameof(TituloEditor));
        }

        private void AbrirEditor(UnidadMedida? unidad)
        {
            UnidadesMedidaViewModel viewModel = new();

            if (unidad != null)
            {
                viewModel.IdUnidadMedida = unidad.IdUnidadMedida;
                viewModel.NombreUnidad = unidad.NombreUnidad;
                viewModel.Abreviatura = unidad.Abreviatura;
                viewModel.Estado = unidad.Estado;
                viewModel.OnPropertyChanged(nameof(TituloEditor));
            }

            UnidadMedidaEditorWindow ventana = new()
            {
                DataContext = viewModel,
                Owner = Application.Current.MainWindow
            };

            viewModel.CerrarVentana = ventana.Close;
            ventana.ShowDialog();

            if (viewModel.Guardado)
            {
                CargarUnidadesMedida();
                Limpiar();
            }
        }

        private void Editar(object? parametro)
        {
            if (parametro is not UnidadMedida unidad)
            {
                NotificationService.Warning("Debe seleccionar una unidad de medida.");
                return;
            }

            AbrirEditor(unidad);
        }
    }
}
