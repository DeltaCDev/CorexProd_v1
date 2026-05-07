using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows;

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

        public CargosViewModel()
        {
            Cargos = new ObservableCollection<Cargo>();

            GuardarCommand = new RelayCommand(_ => Guardar());
            LimpiarCommand = new RelayCommand(_ => Limpiar());
            EliminarCommand = new RelayCommand(id => Eliminar(id));

            CargarCargos();
        }

        private void CargarCargos()
        {
            Cargos.Clear();

            foreach (var cargo in _cargoNegocio.Listar())
            {
                Cargos.Add(cargo);
            }
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
                NotificationService.Warning("Debe seleccionar un cargo");
                return;
            }

            if (!int.TryParse(parametro.ToString(), out int idCargo))
            {
                NotificationService.Warning("Id de cargo inválido");
                return;
            }
            if (IdCargo > 0)
            {
                bool confirmar = ConfirmDialogService.Confirmar(
                    "¿Está seguro de eliminar este cargo?",
                    "Confirmar eliminación");

                if (!confirmar)
                {
                    return;
                }
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
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propiedad = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propiedad));
        }
    }
}