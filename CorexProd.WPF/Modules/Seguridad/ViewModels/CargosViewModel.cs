using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows;
using CorexProd.WPF.Modules.Seguridad.Views;

namespace CorexProd.WPF.Modules.Seguridad.ViewModels
{
    public class CargosViewModel : INotifyPropertyChanged
    {
        private readonly CargoNegocio _cargoNegocio = new CargoNegocio();

        private int _idCargo;
        private string _nombreCargo = string.Empty;
        private bool _estado = true;
        private Cargo? _cargoSeleccionado;

        public ObservableCollection<Cargo> Cargos { get; set; }

        public int IdCargo
        {
            get => _idCargo;
            set
            {
                _idCargo = value;
                OnPropertyChanged();
            }
        }

        public string NombreCargo
        {
            get => _nombreCargo;
            set
            {
                _nombreCargo = value;
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

        public Cargo? CargoSeleccionado
        {
            get => _cargoSeleccionado;
            set
            {
                _cargoSeleccionado = value;
                OnPropertyChanged();

                if (_cargoSeleccionado != null)
                {
                    IdCargo = _cargoSeleccionado.IdCargo;
                    NombreCargo = _cargoSeleccionado.NombreCargo;
                    Estado = _cargoSeleccionado.Estado;
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
        public string TituloEditor => IdCargo > 0 ? "Editar Cargo" : "Nuevo Cargo";
        public string ResumenRegistros => $"Mostrando {Cargos.Count} cargos";

        public CargosViewModel()
        {
            Cargos = new ObservableCollection<Cargo>();

            GuardarCommand = new RelayCommand(_ => Guardar());
            LimpiarCommand = new RelayCommand(_ => Limpiar());
            EliminarCommand = new RelayCommand(id => Eliminar(id));
            NuevoCommand = new RelayCommand(_ => AbrirEditor(null));
            EditarCommand = new RelayCommand(parametro => Editar(parametro));
            RefrescarCommand = new RelayCommand(_ => CargarCargos());
            CerrarCommand = new RelayCommand(_ => CerrarVentana?.Invoke());

            CargarCargos();
        }

        private void CargarCargos()
        {
            Cargos.Clear();

            foreach (var cargo in _cargoNegocio.Listar())
            {
                Cargos.Add(cargo);
            }

            OnPropertyChanged(nameof(ResumenRegistros));
        }

        private void Guardar()
        {
            if (IdCargo > 0)
            {
                if (IdCargo > 0)
                {
                    bool confirmar = ConfirmDialogService.Confirmar(
                        "¿Desea actualizar la información del cargo?",
                        "Confirmar actualización");

                    if (!confirmar)
                    {
                        return;
                    }
                }
            }
            Cargo cargo = new Cargo
            {
                IdCargo = IdCargo,
                NombreCargo = NombreCargo,
                Estado = Estado
            };

            string mensaje = _cargoNegocio.Guardar(cargo);

            if (mensaje.Contains("correctamente"))
            {
                NotificationService.Success(mensaje);
                CargarCargos();
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
                NotificationService.Warning("Debe seleccionar un cargo");
                return;
            }

            if (!int.TryParse(parametro.ToString(), out int idCargo))
            {
                NotificationService.Warning("Id de cargo inválido");
                return;
            }
            bool confirmar = ConfirmDialogService.Confirmar(
                "¿Está seguro de eliminar este cargo?",
                "Confirmar eliminación");

            if (!confirmar)
            {
                return;
            }

            string mensaje = _cargoNegocio.Eliminar(idCargo);

            if (mensaje.Contains("correctamente"))
            {
                NotificationService.Success(mensaje);
                CargarCargos();
                Limpiar();
            }
            else
            {
                NotificationService.Warning(mensaje);
            }
        }

        private void Limpiar()
        {
            IdCargo = 0;
            NombreCargo = string.Empty;
            Estado = true;
            CargoSeleccionado = null;
            OnPropertyChanged(nameof(TituloEditor));
        }

        private void AbrirEditor(Cargo? cargo)
        {
            CargosViewModel viewModel = new();

            if (cargo != null)
            {
                viewModel.IdCargo = cargo.IdCargo;
                viewModel.NombreCargo = cargo.NombreCargo;
                viewModel.Estado = cargo.Estado;
                viewModel.OnPropertyChanged(nameof(TituloEditor));
            }

            CargoEditorWindow ventana = new()
            {
                DataContext = viewModel,
                Owner = Application.Current.MainWindow
            };

            viewModel.CerrarVentana = ventana.Close;
            ventana.ShowDialog();

            if (viewModel.Guardado)
            {
                CargarCargos();
                Limpiar();
            }
        }

        private void Editar(object? parametro)
        {
            if (parametro is not Cargo cargo)
            {
                NotificationService.Warning("Debe seleccionar un cargo");
                return;
            }

            AbrirEditor(cargo);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propiedad = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propiedad));
        }
    }
}
